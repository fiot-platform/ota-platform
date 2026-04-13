using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OTA.API.Models.DTOs;
using OTA.API.Models.Entities;
using OTA.API.Models.Settings;
using OTA.API.Repositories.Interfaces;
using OTA.API.Services.Interfaces;

namespace OTA.API.Services
{
    /// <summary>
    /// Handles user authentication, JWT token generation, and refresh token lifecycle.
    /// Uses BCrypt.Net for password hashing and Microsoft.IdentityModel.Tokens for JWT signing.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IGiteaApiService _giteaApiService;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthService> _logger;

        /// <summary>
        /// Initialises a new instance of <see cref="AuthService"/>.
        /// </summary>
        public AuthService(
            IUserRepository userRepository,
            IGiteaApiService giteaApiService,
            IOptions<JwtSettings> jwtSettings,
            ILogger<AuthService> logger)
        {
            _userRepository  = userRepository  ?? throw new ArgumentNullException(nameof(userRepository));
            _giteaApiService = giteaApiService ?? throw new ArgumentNullException(nameof(giteaApiService));
            _jwtSettings     = jwtSettings?.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
            _logger          = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.Email)) throw new ArgumentException("Email is required.", nameof(request));
            if (string.IsNullOrWhiteSpace(request.Password)) throw new ArgumentException("Password is required.", nameof(request));

            try
            {
                var normalizedEmail = request.Email.Trim().ToLowerInvariant();
                var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("Login failed: email '{Email}' not found in OTA database. Checking Gitea...", request.Email);

                    // ── Gitea fallback: check if the account exists there ────────────
                    var giteaUser = await _giteaApiService.FindUserByEmailAsync(normalizedEmail, cancellationToken);

                    if (giteaUser != null)
                    {
                        // Email found in Gitea — now verify the password against Gitea
                        var giteaPasswordValid = await _giteaApiService.VerifyUserCredentialsAsync(
                            giteaUser.Login, request.Password, cancellationToken);

                        if (giteaPasswordValid)
                        {
                            _logger.LogWarning(
                                "Gitea user '{Login}' authenticated but has no OTA Platform account.",
                                giteaUser.Login);
                            throw new UnauthorizedAccessException(
                                "Your Gitea account was verified but is not registered in OTA Platform. Please contact your administrator.");
                        }
                        else
                        {
                            _logger.LogWarning("Login failed: Gitea user '{Login}' found but password incorrect.", giteaUser.Login);
                            throw new UnauthorizedAccessException("Incorrect password. Please try again.");
                        }
                    }

                    throw new UnauthorizedAccessException("No account found with that email address.");
                }

                if (!user.IsActive)
                {
                    _logger.LogWarning("Login failed: user '{Email}' is deactivated.", request.Email);
                    throw new UnauthorizedAccessException("Account is deactivated. Please contact your administrator.");
                }

                if (!VerifyPassword(request.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Login failed: invalid password for user '{Email}'.", request.Email);
                    throw new UnauthorizedAccessException("Incorrect password. Please try again.");
                }

                var accessToken = GenerateJwtToken(user);
                var refreshToken = GenerateRefreshToken();
                var refreshTokenExpiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays);

                await _userRepository.UpdateRefreshTokenAsync(user.Id, refreshToken, refreshTokenExpiry, cancellationToken);

                _logger.LogInformation("User '{Email}' logged in successfully.", user.Email);

                return new LoginResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
                    User = new UserDto
                    {
                        UserId = user.Id,
                        Name = user.Name,
                        Email = user.Email,
                        Role = user.Role.ToString(),
                        CustomerId = user.CustomerId,
                        IsActive = user.IsActive
                    }
                };
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login for '{Email}'.", request.Email);
                throw new InvalidOperationException("An error occurred during authentication. Please try again.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                throw new ArgumentException("RefreshToken is required.", nameof(request));

            try
            {
                var user = await _userRepository.FindByRefreshTokenAsync(request.RefreshToken, cancellationToken);
                if (user == null || !user.IsActive)
                    throw new UnauthorizedAccessException("User not found or account is deactivated.");

                if (user.RefreshTokenExpiry < DateTime.UtcNow)
                    throw new UnauthorizedAccessException("Refresh token is invalid or has expired.");

                var newAccessToken = GenerateJwtToken(user);
                var newRefreshToken = GenerateRefreshToken();
                var newRefreshTokenExpiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays);

                await _userRepository.UpdateRefreshTokenAsync(user.Id, newRefreshToken, newRefreshTokenExpiry, cancellationToken);

                _logger.LogInformation("Refresh token issued for user '{Email}'.", user.Email);

                return new LoginResponse
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
                    User = new UserDto
                    {
                        UserId = user.Id,
                        Name = user.Name,
                        Email = user.Email,
                        Role = user.Role.ToString(),
                        CustomerId = user.CustomerId,
                        IsActive = user.IsActive
                    }
                };
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during token refresh.");
                throw new InvalidOperationException("An error occurred during token refresh.", ex);
            }
        }

        /// <inheritdoc/>
        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password must not be null or empty.", nameof(password));

            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        /// <inheritdoc/>
        public bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying password hash.");
                return false;
            }
        }

        /// <inheritdoc/>
        public string GenerateJwtToken(UserEntity user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat,
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("role", user.Role.ToString()),
                new Claim("fullName", user.Name)
            };

            if (!string.IsNullOrWhiteSpace(user.CustomerId))
                claims.Add(new Claim("customerId", user.CustomerId));

            if (user.ProjectScope != null && user.ProjectScope.Count > 0)
                claims.Add(new Claim("projectScope", string.Join(",", user.ProjectScope)));

            var expiry = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiry,
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // ── Private helpers ────────────────────────────────────────────────────────────

        private string GenerateRefreshToken()
        {
            var bytes = new byte[64];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = false, // Intentionally allow expired tokens for refresh
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret)),
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var principal = handler.ValidateToken(token, tokenValidationParameters, out var securityToken);

                if (securityToken is not JwtSecurityToken jwt ||
                    !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
                {
                    throw new SecurityTokenException("Invalid token algorithm.");
                }

                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract principal from expired token.");
                throw new UnauthorizedAccessException("Invalid access token.", ex);
            }
        }
    }
}

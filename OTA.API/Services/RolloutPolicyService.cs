using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OTA.API.Models.DTOs;
using OTA.API.Models.Entities;
using OTA.API.Models.Enums;
using OTA.API.Repositories.Interfaces;
using OTA.API.Services.Interfaces;

namespace OTA.API.Services
{
    /// <summary>
    /// Implements rollout policy CRUD operations.
    /// Prevents deletion of policies referenced by active rollouts.
    /// </summary>
    public class RolloutPolicyService : IRolloutPolicyService
    {
        private readonly IRolloutPolicyRepository _policyRepository;
        private readonly IRolloutRepository _rolloutRepository;
        private readonly IAuditService _auditService;
        private readonly ILogger<RolloutPolicyService> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>Initialises a new instance of <see cref="RolloutPolicyService"/>.</summary>
        public RolloutPolicyService(
            IRolloutPolicyRepository policyRepository,
            IRolloutRepository rolloutRepository,
            IAuditService auditService,
            ILogger<RolloutPolicyService> logger)
        {
            _policyRepository = policyRepository ?? throw new ArgumentNullException(nameof(policyRepository));
            _rolloutRepository = rolloutRepository ?? throw new ArgumentNullException(nameof(rolloutRepository));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<RolloutPolicyDto> CreatePolicyAsync(
            CreateRolloutPolicyRequest request,
            string callerUserId,
            string callerEmail,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.Name)) throw new ArgumentException("Policy name is required.");

            var existing = await _policyRepository.GetByNameAsync(request.Name, cancellationToken);
            if (existing != null)
                throw new InvalidOperationException($"A rollout policy named '{request.Name}' already exists.");

            ValidatePolicyValues(request.CanaryPercentage, request.BatchSize, request.ConcurrencyLimit);

            var policy = new RolloutPolicyEntity
            {
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                CanaryPercentage = request.CanaryPercentage,
                BatchSize = request.BatchSize,
                ConcurrencyLimit = request.ConcurrencyLimit,
                AllowDowngrade = request.AllowDowngrade,
                RetryLimit = request.RetryLimit,
                RetryDelaySeconds = request.RetryDelaySeconds,
                IsActive = true,
                CreatedByUserId = callerUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _policyRepository.InsertAsync(policy, cancellationToken);

            _logger.LogInformation("Rollout policy '{Name}' created by '{Email}'.", policy.Name, callerEmail);

            await _auditService.LogActionAsync(
                AuditAction.RolloutPolicyCreated,
                callerUserId, callerEmail, UserRole.SuperAdmin,
                "RolloutPolicy", policy.Id,
                null,
                JsonSerializer.Serialize(new { policy.Id, policy.Name }, _jsonOptions),
                ipAddress,
                cancellationToken: cancellationToken);

            return MapToDto(policy);
        }

        /// <inheritdoc/>
        public async Task<RolloutPolicyDto> UpdatePolicyAsync(
            string policyId,
            UpdateRolloutPolicyRequest request,
            string callerUserId,
            string callerEmail,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(policyId)) throw new ArgumentException("PolicyId is required.", nameof(policyId));
            if (request == null) throw new ArgumentNullException(nameof(request));

            var policy = await _policyRepository.GetByIdAsync(policyId, cancellationToken)
                ?? throw new KeyNotFoundException($"Rollout policy '{policyId}' not found.");

            var oldSnapshot = JsonSerializer.Serialize(new
            {
                policy.Name, policy.CanaryPercentage, policy.BatchSize,
                policy.ConcurrencyLimit, policy.AllowDowngrade
            }, _jsonOptions);

            if (!string.IsNullOrWhiteSpace(request.Name) && request.Name != policy.Name)
            {
                var nameConflict = await _policyRepository.GetByNameAsync(request.Name, cancellationToken);
                if (nameConflict != null && nameConflict.Id != policyId)
                    throw new InvalidOperationException($"A rollout policy named '{request.Name}' already exists.");
                policy.Name = request.Name.Trim();
            }

            if (request.Description != null) policy.Description = request.Description.Trim();
            if (request.CanaryPercentage.HasValue) policy.CanaryPercentage = request.CanaryPercentage.Value;
            if (request.BatchSize.HasValue) policy.BatchSize = request.BatchSize.Value;
            if (request.ConcurrencyLimit.HasValue) policy.ConcurrencyLimit = request.ConcurrencyLimit.Value;
            if (request.AllowDowngrade.HasValue) policy.AllowDowngrade = request.AllowDowngrade.Value;
            if (request.RetryLimit.HasValue) policy.RetryLimit = request.RetryLimit.Value;
            if (request.RetryDelaySeconds.HasValue) policy.RetryDelaySeconds = request.RetryDelaySeconds.Value;
            if (request.IsActive.HasValue) policy.IsActive = request.IsActive.Value;

            ValidatePolicyValues(policy.CanaryPercentage, policy.BatchSize, policy.ConcurrencyLimit);

            policy.UpdatedAt = DateTime.UtcNow;
            await _policyRepository.UpdateAsync(policyId, policy, cancellationToken);

            _logger.LogInformation("Rollout policy '{PolicyId}' updated by '{Email}'.", policyId, callerEmail);

            await _auditService.LogActionAsync(
                AuditAction.RolloutPolicyUpdated,
                callerUserId, callerEmail, UserRole.SuperAdmin,
                "RolloutPolicy", policyId,
                oldSnapshot,
                JsonSerializer.Serialize(new { policy.Name, policy.CanaryPercentage, policy.BatchSize }, _jsonOptions),
                ipAddress,
                cancellationToken: cancellationToken);

            return MapToDto(policy);
        }

        /// <inheritdoc/>
        public async Task<RolloutPolicyDto?> GetPolicyByIdAsync(string policyId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(policyId)) throw new ArgumentException("PolicyId is required.", nameof(policyId));
            var policy = await _policyRepository.GetByIdAsync(policyId, cancellationToken);
            return policy == null ? null : MapToDto(policy);
        }

        /// <inheritdoc/>
        public async Task<List<RolloutPolicyDto>> GetActivePoliciesAsync(CancellationToken cancellationToken = default)
        {
            var policies = await _policyRepository.GetActiveAsync(cancellationToken);
            return policies.Select(MapToDto).ToList();
        }

        /// <inheritdoc/>
        public async Task DeletePolicyAsync(
            string policyId,
            string callerUserId,
            string callerEmail,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(policyId)) throw new ArgumentException("PolicyId is required.", nameof(policyId));

            var policy = await _policyRepository.GetByIdAsync(policyId, cancellationToken)
                ?? throw new KeyNotFoundException($"Rollout policy '{policyId}' not found.");

            // Check if any active rollout references this policy
            var activeRollouts = await _rolloutRepository.GetByStatusAsync(RolloutStatus.Active, cancellationToken);
            var pausedRollouts = await _rolloutRepository.GetByStatusAsync(RolloutStatus.Paused, cancellationToken);
            var allActive = activeRollouts.Concat(pausedRollouts);

            if (allActive.Any(r => r.PolicyId == policyId))
                throw new InvalidOperationException($"Rollout policy '{policy.Name}' is in use by active or paused rollouts and cannot be deleted.");

            await _policyRepository.DeleteAsync(policyId, cancellationToken);

            _logger.LogInformation("Rollout policy '{PolicyId}' deleted by '{Email}'.", policyId, callerEmail);

            await _auditService.LogActionAsync(
                AuditAction.RolloutPolicyDeleted,
                callerUserId, callerEmail, UserRole.SuperAdmin,
                "RolloutPolicy", policyId,
                JsonSerializer.Serialize(new { policy.Name }, _jsonOptions),
                null,
                ipAddress,
                cancellationToken: cancellationToken);
        }

        // ── Private helpers ─────────────────────────────────────────────────────────

        private static void ValidatePolicyValues(int canaryPercentage, int batchSize, int concurrencyLimit)
        {
            if (canaryPercentage < 1 || canaryPercentage > 100)
                throw new ArgumentException("CanaryPercentage must be between 1 and 100.");
            if (batchSize < 1)
                throw new ArgumentException("BatchSize must be at least 1.");
            if (concurrencyLimit < 1)
                throw new ArgumentException("ConcurrencyLimit must be at least 1.");
        }

        private static RolloutPolicyDto MapToDto(RolloutPolicyEntity p) => new RolloutPolicyDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            CanaryPercentage = p.CanaryPercentage,
            BatchSize = p.BatchSize,
            ConcurrencyLimit = p.ConcurrencyLimit,
            AllowDowngrade = p.AllowDowngrade,
            RetryLimit = p.RetryLimit,
            RetryDelaySeconds = p.RetryDelaySeconds,
            IsActive = p.IsActive,
            CreatedByUserId = p.CreatedByUserId,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        };
    }
}

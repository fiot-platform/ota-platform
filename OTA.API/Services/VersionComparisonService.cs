using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using OTA.API.Models.Entities;
using OTA.API.Services.Interfaces;

namespace OTA.API.Services
{
    /// <summary>
    /// Implements semantic version comparison and firmware-to-device compatibility checks.
    /// Parses version strings according to SemVer 2.0 (MAJOR.MINOR.PATCH[-prerelease][+build]).
    /// Pre-release versions are considered lower precedence than the corresponding release version.
    /// </summary>
    public class VersionComparisonService : IVersionComparisonService
    {
        private readonly ILogger<VersionComparisonService> _logger;

        // Matches: MAJOR.MINOR.PATCH with optional pre-release (-alpha.1) and build (+build.1)
        private static readonly Regex _semverRegex = new Regex(
            @"^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?:-(?<prerelease>[a-zA-Z0-9\-\.]+))?(?:\+(?<build>[a-zA-Z0-9\-\.]+))?$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>Initialises a new instance of <see cref="VersionComparisonService"/>.</summary>
        public VersionComparisonService(ILogger<VersionComparisonService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public bool IsNewerVersion(string current, string candidate)
        {
            if (string.IsNullOrWhiteSpace(current) || string.IsNullOrWhiteSpace(candidate))
                return false;

            try
            {
                return CompareVersions(candidate, current) > 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Version comparison failed for current='{Current}', candidate='{Candidate}'.", current, candidate);
                return false;
            }
        }

        /// <inheritdoc/>
        public bool IsCompatible(FirmwareVersionEntity firmware, DeviceEntity device)
        {
            if (firmware == null) throw new ArgumentNullException(nameof(firmware));
            if (device == null) throw new ArgumentNullException(nameof(device));

            // If no restrictions are specified, firmware is universally compatible
            bool modelOk = firmware.SupportedModels == null || firmware.SupportedModels.Count == 0
                || firmware.SupportedModels.Contains(device.Model, StringComparer.OrdinalIgnoreCase);

            bool hwOk = firmware.SupportedHardwareRevisions == null || firmware.SupportedHardwareRevisions.Count == 0
                || firmware.SupportedHardwareRevisions.Contains(device.HardwareRevision, StringComparer.OrdinalIgnoreCase);

            return modelOk && hwOk;
        }

        /// <inheritdoc/>
        public bool CanDowngrade(string current, string target, RolloutPolicyEntity policy)
        {
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            if (!policy.AllowDowngrade) return false;

            try
            {
                // Downgrade means target is older than current
                return CompareVersions(target, current) < 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Downgrade check failed for current='{Current}', target='{Target}'.", current, target);
                return false;
            }
        }

        /// <inheritdoc/>
        public int CompareVersions(string v1, string v2)
        {
            if (string.IsNullOrWhiteSpace(v1)) throw new ArgumentException("v1 must not be null or empty.", nameof(v1));
            if (string.IsNullOrWhiteSpace(v2)) throw new ArgumentException("v2 must not be null or empty.", nameof(v2));

            var sv1 = Parse(v1.TrimStart('v', 'V'));
            var sv2 = Parse(v2.TrimStart('v', 'V'));

            // Compare MAJOR
            int result = sv1.Major.CompareTo(sv2.Major);
            if (result != 0) return result;

            // Compare MINOR
            result = sv1.Minor.CompareTo(sv2.Minor);
            if (result != 0) return result;

            // Compare PATCH
            result = sv1.Patch.CompareTo(sv2.Patch);
            if (result != 0) return result;

            // Pre-release: a version without pre-release > one with pre-release (SemVer spec)
            if (sv1.PreRelease == null && sv2.PreRelease != null) return 1;
            if (sv1.PreRelease != null && sv2.PreRelease == null) return -1;
            if (sv1.PreRelease != null && sv2.PreRelease != null)
                return ComparePreRelease(sv1.PreRelease, sv2.PreRelease);

            return 0;
        }

        // ── Private helpers ─────────────────────────────────────────────────────────

        private struct SemVer
        {
            public int Major;
            public int Minor;
            public int Patch;
            public string? PreRelease;
        }

        private SemVer Parse(string version)
        {
            var match = _semverRegex.Match(version);
            if (!match.Success)
            {
                // Fallback: try simple MAJOR.MINOR.PATCH without strict regex
                var parts = version.Split('.');
                if (parts.Length >= 3 &&
                    int.TryParse(parts[0], out var maj) &&
                    int.TryParse(parts[1], out var min) &&
                    int.TryParse(parts[2].Split('-')[0], out var pat))
                {
                    return new SemVer { Major = maj, Minor = min, Patch = pat };
                }

                throw new ArgumentException($"'{version}' is not a valid semantic version string.");
            }

            return new SemVer
            {
                Major = int.Parse(match.Groups["major"].Value),
                Minor = int.Parse(match.Groups["minor"].Value),
                Patch = int.Parse(match.Groups["patch"].Value),
                PreRelease = match.Groups["prerelease"].Success ? match.Groups["prerelease"].Value : null
            };
        }

        /// <summary>
        /// Compares two pre-release identifier strings according to SemVer 2.0 precedence rules.
        /// Numeric identifiers have lower precedence than alphanumeric. Shorter field counts have lower precedence.
        /// </summary>
        private static int ComparePreRelease(string pr1, string pr2)
        {
            var ids1 = pr1.Split('.');
            var ids2 = pr2.Split('.');

            int length = Math.Min(ids1.Length, ids2.Length);
            for (int i = 0; i < length; i++)
            {
                bool isNum1 = int.TryParse(ids1[i], out var num1);
                bool isNum2 = int.TryParse(ids2[i], out var num2);

                if (isNum1 && isNum2)
                {
                    int cmp = num1.CompareTo(num2);
                    if (cmp != 0) return cmp;
                }
                else if (isNum1 && !isNum2)
                {
                    return -1; // Numeric < alphanumeric
                }
                else if (!isNum1 && isNum2)
                {
                    return 1;
                }
                else
                {
                    int cmp = string.Compare(ids1[i], ids2[i], StringComparison.Ordinal);
                    if (cmp != 0) return cmp;
                }
            }

            return ids1.Length.CompareTo(ids2.Length);
        }
    }
}

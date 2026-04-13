using OTA.API.Models.Entities;

namespace OTA.API.Services.Interfaces
{
    /// <summary>
    /// Service interface for semantic version comparison and firmware-to-device compatibility checks.
    /// All version strings are expected to follow SemVer 2.0 format (MAJOR.MINOR.PATCH[-prerelease][+build]).
    /// </summary>
    public interface IVersionComparisonService
    {
        /// <summary>
        /// Determines whether the candidate firmware version is strictly newer than the current version.
        /// Parses both strings as SemVer and performs a full precedence comparison.
        /// </summary>
        /// <param name="current">The device's currently installed firmware version string.</param>
        /// <param name="candidate">The firmware version string being evaluated.</param>
        /// <returns>True if <paramref name="candidate"/> is a higher precedence version than <paramref name="current"/>; otherwise false.</returns>
        bool IsNewerVersion(string current, string candidate);

        /// <summary>
        /// Determines whether a firmware build is compatible with the given device based on the
        /// firmware's SupportedModels and SupportedHardwareRevisions lists.
        /// </summary>
        /// <param name="firmware">The firmware entity containing compatibility constraints.</param>
        /// <param name="device">The target device entity whose model and hardware revision are checked.</param>
        /// <returns>True if the device model and hardware revision are listed in the firmware's supported sets; otherwise false.</returns>
        bool IsCompatible(FirmwareVersionEntity firmware, DeviceEntity device);

        /// <summary>
        /// Determines whether a downgrade from the current version to the target version is permitted
        /// based on the rollout policy's AllowDowngrade flag and the version comparison result.
        /// </summary>
        /// <param name="current">The device's currently installed firmware version string.</param>
        /// <param name="target">The firmware version string being targeted.</param>
        /// <param name="policy">The rollout policy entity containing the downgrade permission flag.</param>
        /// <returns>True if the policy permits downgrades and the target version is older than current; otherwise false.</returns>
        bool CanDowngrade(string current, string target, RolloutPolicyEntity policy);

        /// <summary>
        /// Compares two semantic version strings.
        /// </summary>
        /// <param name="v1">The first version string.</param>
        /// <param name="v2">The second version string.</param>
        /// <returns>
        /// A negative integer if v1 is older than v2;
        /// zero if they are equal in precedence;
        /// a positive integer if v1 is newer than v2.
        /// </returns>
        int CompareVersions(string v1, string v2);
    }
}

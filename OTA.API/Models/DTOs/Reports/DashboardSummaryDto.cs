namespace OTA.API.Models.DTOs.Reports
{
    public class DashboardSummaryDto
    {
        public int TotalDevices { get; set; }
        public int ActiveDevices { get; set; }
        public int OfflineDevices { get; set; }
        public int SuspendedDevices { get; set; }
        public int TotalFirmwareVersions { get; set; }
        public int PendingApprovalFirmware { get; set; }
        public int ApprovedFirmware { get; set; }
        public int ActiveRollouts { get; set; }
        public int CompletedRollouts { get; set; }
        public int FailedRollouts { get; set; }
        public int TotalProjects { get; set; }
        public int TotalRepositories { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    public class FirmwareTrendDto
    {
        public string Period { get; set; } = string.Empty;
        public int TotalSubmitted { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
        public int QaVerified { get; set; }
        public double ApprovalRatePercent { get; set; }
    }

    public class RolloutSuccessRateDto
    {
        public string Period { get; set; } = string.Empty;
        public int TotalRollouts { get; set; }
        public int Completed { get; set; }
        public int Failed { get; set; }
        public int Cancelled { get; set; }
        public double SuccessRatePercent { get; set; }
        public int TotalDevicesTargeted { get; set; }
        public int TotalDevicesSucceeded { get; set; }
    }

    public class DeviceUpdateStatusDto
    {
        public int TotalDevices { get; set; }
        public int UpToDate { get; set; }
        public int PendingUpdate { get; set; }
        public int UpdateFailed { get; set; }
        public int Offline { get; set; }
        public List<FirmwareVersionDistributionDto> VersionDistribution { get; set; } = new();
    }

    public class FirmwareVersionDistributionDto
    {
        public string FirmwareVersion { get; set; } = string.Empty;
        public int DeviceCount { get; set; }
        public double PercentageOfFleet { get; set; }
    }
}

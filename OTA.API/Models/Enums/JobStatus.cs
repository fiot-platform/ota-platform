namespace OTA.API.Models.Enums
{
    /// <summary>
    /// Status of an individual OTA update job targeting a single device.
    /// </summary>
    public enum JobStatus
    {
        /// <summary>Job has been scheduled but not yet started.</summary>
        Pending = 0,

        /// <summary>Firmware download is in progress on the device.</summary>
        Downloading = 1,

        /// <summary>Firmware installation is in progress on the device.</summary>
        Installing = 2,

        /// <summary>Job completed successfully and device confirmed update.</summary>
        Success = 3,

        /// <summary>Job failed after the maximum number of retry attempts.</summary>
        Failed = 4,

        /// <summary>Job was manually retried after a failure.</summary>
        Retrying = 5,

        /// <summary>Job was cancelled before completion.</summary>
        Cancelled = 6
    }
}

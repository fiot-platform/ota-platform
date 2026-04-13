namespace OTA.API.Models.Enums
{
    /// <summary>Lifecycle status of a QA testing session for a firmware version.</summary>
    public enum QASessionStatus
    {
        NotStarted,
        InProgress,
        BugListRaised,
        Complete,
        Fail
    }

    /// <summary>Severity classification for a bug raised during QA testing.</summary>
    public enum BugSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>Resolution status of an individual bug in a QA session.</summary>
    public enum BugStatus
    {
        Open,
        InProgress,
        Resolved,
        WontFix
    }
}

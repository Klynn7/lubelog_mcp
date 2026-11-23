namespace LubeLogMCP.Models
{
    public enum ReminderUrgency
    {
        NotUrgent = 0,
        Urgent = 1,
        VeryUrgent = 2,
        PastDue = 3
    }
    public enum ReminderMetric
    {
        Date = 0,
        Odometer = 1,
        Both = 2
    }
    /// <summary>
    /// Record types a plan can become. LubeLogger rejects any other ImportMode for plans.
    /// </summary>
    public enum PlanType
    {
        ServiceRecord = 0,
        RepairRecord = 1,
        UpgradeRecord = 4
    }
    public enum PlanPriority
    {
        Critical = 0,
        Normal = 1,
        Low = 2
    }
    /// <summary>
    /// Plan progress values the API accepts. "Done" exists in LubeLogger but is rejected through the API.
    /// </summary>
    public enum PlanProgress
    {
        Backlog = 0,
        InProgress = 1,
        Testing = 2
    }
}

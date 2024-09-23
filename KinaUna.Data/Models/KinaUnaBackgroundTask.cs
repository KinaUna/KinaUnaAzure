using System;

namespace KinaUna.Data.Models;

public class KinaUnaBackgroundTask
{
    public string TaskName { get; set; } = "";
    public string TaskDescription { get; set; } = "";
    public DateTime LastRun { get; set; } = DateTime.UtcNow;
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(5);
    public bool IsRunning { get; set; }
}
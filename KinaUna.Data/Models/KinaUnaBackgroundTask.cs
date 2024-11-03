using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KinaUna.Data.Models;

public class KinaUnaBackgroundTask
{
    [Key]
    public int TaskId { get; set; }

    [MaxLength(256)]
    public string TaskName { get; set; } = "";

    [MaxLength(4096)]
    public string TaskDescription { get; set; } = "";

    [MaxLength(4096)]
    public string ApiEndpoint { get; set; } = "";

    [MaxLength(4096)]
    public string Parameters { get; set; } = "";
    public DateTime LastRun { get; set; } = DateTime.UtcNow;
    public TimeSpan Interval { get; set; } = TimeSpan.FromDays(1);
    public bool IsRunning { get; set; }
    public bool IsEnabled { get; set; }

    [NotMapped]
    public int IntervalMinutes
    {
        get => (int)Interval.TotalMinutes;

        set => Interval = TimeSpan.FromMinutes(value);
    }
}
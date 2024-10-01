using System;

namespace KinaUna.Data.Models.DTOs
{
    public class BackgroundTaskDto
    {
        public int TaskId { get; set; }
        public string TaskName { get; set; } = "";
        public string TaskDescription { get; set; } = "";
        public string Command { get; set; } = "";
        public string Parameters { get; set; } = "";
        public string LastRun { get; set; } = "";
        public int Interval { get; set; } = 24 * 60; // In minutes.
        public bool IsRunning { get; set; }
        public bool IsEnabled { get; set; }

        public static implicit operator BackgroundTaskDto(KinaUnaBackgroundTask backgroundTask)
        {
            return new BackgroundTaskDto
            {
                TaskId = backgroundTask.TaskId,
                TaskName = backgroundTask.TaskName,
                TaskDescription = backgroundTask.TaskDescription,
                Command = backgroundTask.ApiEndpoint,
                Parameters = backgroundTask.Parameters,
                LastRun = backgroundTask.LastRun.ToString("dd/MM/yyyy HH:mm:ss"),
                Interval = backgroundTask.IntervalMinutes,
                IsRunning = backgroundTask.IsRunning,
                IsEnabled = backgroundTask.IsEnabled
            };
        }

        public static implicit operator KinaUnaBackgroundTask(BackgroundTaskDto backgroundTaskDto)
        {
            bool tryParse = DateTime.TryParse(backgroundTaskDto.LastRun, out DateTime lastRun);
            return new KinaUnaBackgroundTask
            {
                TaskId = backgroundTaskDto.TaskId,
                TaskName = backgroundTaskDto.TaskName,
                TaskDescription = backgroundTaskDto.TaskDescription,
                ApiEndpoint = backgroundTaskDto.Command,
                Parameters = backgroundTaskDto.Parameters,
                LastRun = tryParse ? lastRun : DateTime.MinValue,
                IntervalMinutes = backgroundTaskDto.Interval,
                IsRunning = backgroundTaskDto.IsRunning,
                IsEnabled = backgroundTaskDto.IsEnabled
            };
        }
    }
    
}

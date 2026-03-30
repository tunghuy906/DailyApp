using System;
using System.Collections.Generic;

namespace DailyPlannerApp.Models
{
    public class TaskItem
    {
        public bool IsDone { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime Deadline { get; set; }
        public List<string> SubTasks { get; set; } = new List<string>();
        public List<bool> SubTaskDone { get; set; } = new List<bool>();
    }
}
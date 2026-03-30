using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DailyPlannerApp.Models;

namespace DailyPlannerApp.Services
{
    public class TaskService
    {
        private string filePath = "tasks.json";

        public void Save(List<TaskItem> tasks)
        {
            var json = JsonSerializer.Serialize(tasks);
            File.WriteAllText(filePath, json);
        }

        public List<TaskItem> Load()
        {
            if (!File.Exists(filePath))
                return new List<TaskItem>();

            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<TaskItem>>(json);
        }
    }
}
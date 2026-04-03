using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DailyPlannerApp.Models;

namespace DailyPlannerApp.Services
{
    public class TaskService
    {
        private string filePath;

        public TaskService()
        {
            string oneDrive = Environment.GetEnvironmentVariable("OneDrive");

            if (string.IsNullOrEmpty(oneDrive))
            {
                string userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                oneDrive = Path.Combine(userPath, "OneDrive");
            }

            filePath = Path.Combine(oneDrive, "tasks.json");

            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, "[]");
            }
        }

        public void Save(List<TaskItem> tasks)
        {
            var json = JsonSerializer.Serialize(tasks, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public List<TaskItem> Load()
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<TaskItem>>(json) ?? new List<TaskItem>();
        }
    }
}
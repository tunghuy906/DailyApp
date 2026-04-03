using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DailyPlannerApp.Models;

namespace DailyPlannerApp.Services
{
    public class VocabService
    {
        private string filePath;

        private static readonly JsonSerializerOptions _readOpts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly JsonSerializerOptions _writeOpts = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public VocabService()
        {
            string oneDrive = Environment.GetEnvironmentVariable("OneDrive");

            if (string.IsNullOrEmpty(oneDrive))
            {
                string userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                oneDrive = Path.Combine(userPath, "OneDrive");
            }

            filePath = Path.Combine(oneDrive, "vocab.json");

            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, "[]");
            }
        }

        public List<VocabItem> Load()
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<VocabItem>>(json, _readOpts) ?? new List<VocabItem>();
        }

        public void Save(List<VocabItem> items)
        {
            var json = JsonSerializer.Serialize(items, _writeOpts);
            File.WriteAllText(filePath, json);
        }
    }
}
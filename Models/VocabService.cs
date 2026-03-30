using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DailyPlannerApp.Models;

namespace DailyPlannerApp.Services
{
    public class VocabService
    {
        private readonly string _filePath = "vocab.json";

        public List<VocabItem> Load()
        {
            if (!File.Exists(_filePath)) return new List<VocabItem>();
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<VocabItem>>(json) ?? new List<VocabItem>();
        }

        public void Save(List<VocabItem> items)
        {
            var json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
    }
}
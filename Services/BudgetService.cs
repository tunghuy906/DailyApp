using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DailyPlannerApp
{
    public class BudgetData
    {
        public List<BudgetItem> Items { get; set; } = new();
        public decimal BudgetLimit { get; set; } = 0;
    }

    public class BudgetService
    {
        private string filePath;

        public BudgetService()
        {
            string oneDrive = Environment.GetEnvironmentVariable("OneDrive");

            if (string.IsNullOrEmpty(oneDrive))
            {
                string userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                oneDrive = Path.Combine(userPath, "OneDrive");
            }

            filePath = Path.Combine(oneDrive, "budget.json");

            if (!File.Exists(filePath))
            {
                var emptyData = new BudgetData();
                File.WriteAllText(filePath, JsonSerializer.Serialize(emptyData));
            }
        }

        public BudgetData Load()
        {
            try
            {
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<BudgetData>(json) ?? new BudgetData();
            }
            catch
            {
                return new BudgetData();
            }
        }

        public void Save(List<BudgetItem> items, decimal budgetLimit)
        {
            try
            {
                var data = new BudgetData
                {
                    Items = items,
                    BudgetLimit = budgetLimit
                };

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Save error: {ex.Message}");
            }
        }
    }
}
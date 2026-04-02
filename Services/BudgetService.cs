using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DailyPlannerApp
{
    // Wrapper lưu cả danh sách chi tiêu lẫn ngân sách tháng vào 1 file
    public class BudgetData
    {
        public List<BudgetItem> Items       { get; set; } = new();
        public decimal          BudgetLimit { get; set; } = 0;
    }

    public class BudgetService
    {
        // Lưu trong thư mục project (cùng chỗ với tasks.json, vocab_1000.json)
        private static readonly string SavePath = Path.Combine(
            Directory.GetCurrentDirectory(), "budget.json");

        public BudgetData Load()
        {
            try
            {
                if (File.Exists(SavePath))
                    return JsonSerializer.Deserialize<BudgetData>(File.ReadAllText(SavePath)) ?? new();
            }
            catch { }
            return new();
        }

        public void Save(List<BudgetItem> items, decimal budgetLimit)
        {
            try
            {
                var data = new BudgetData { Items = items, BudgetLimit = budgetLimit };
                File.WriteAllText(SavePath, JsonSerializer.Serialize(data,
                    new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Save error: {ex.Message}");
            }
        }
    }
}
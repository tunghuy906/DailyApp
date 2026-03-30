using System;

namespace DailyPlannerApp.Models
{
    public class VocabItem
    {
        public string Word { get; set; } = "";
        public string Phonetic { get; set; } = "";   // Phiên âm
        public string Meaning { get; set; } = "";    // Nghĩa tiếng Việt
        public string Example { get; set; } = "";    // Ví dụ câu
        public DateTime AddedDate { get; set; } = DateTime.Now;
    }
}
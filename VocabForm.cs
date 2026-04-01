using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using DailyPlannerApp.Models;
using DailyPlannerApp.Services;

namespace DailyPlannerApp
{
    public class MasterWord
    {
        public int Stt { get; set; }
        public string Word { get; set; } = "";
        public string Meaning { get; set; } = "";
        public string Example { get; set; } = "";
        public string Topic { get; set; } = "";
    }

    public class VocabForm : Form
    {
        static readonly Color C_ACCENT = Color.FromArgb(37, 99, 235);
        static readonly Color C_BG = Color.FromArgb(245, 246, 250);
        static readonly Color C_WHITE = Color.White;
        static readonly Color C_TEXT = Color.FromArgb(30, 41, 59);
        static readonly Color C_SUB = Color.FromArgb(100, 116, 139);
        static readonly Color C_BORDER = Color.FromArgb(226, 232, 240);
        static readonly Color C_GREEN = Color.FromArgb(22, 163, 74);
        static readonly Color C_RED = Color.FromArgb(220, 38, 38);

        private readonly VocabService _service = new VocabService();
        private BindingList<VocabItem> _allItems;
        private BindingList<VocabItem> _viewItems;
        private List<MasterWord> _master = new List<MasterWord>();

        private const string PROGRESS_FILE = "vocab_progress.json";
        private int _dailyStartIndex = 0;
        private string _lastLearnDate = "";

        private TabControl tabs;

        // Tab 1
        private TextBox txtWord, txtMeaning, txtExample;
        private Button btnSave, btnClear;
        private TextBox txtSearch;
        private DataGridView grid;
        private Label lblCount;
        private Button btnDelete;

        // Tab 2
        private Panel daily10Panel;

        // Tab 3
        private Label lblQuizScore, lblQuizTopic, lblQuizWord, lblQuizAnswer;
        private Button btnReveal, btnCorrect, btnWrong, btnNextQuiz;
        private MasterWord _currentQuizWord;
        private int _quizCorrect, _quizTotal;
        private bool _quizRevealed;

        public VocabForm()
        {
            LoadMaster();
            LoadProgress();
            BuildUI();
            LoadAll();
        }

        // ── Master data ───────────────────────────────────────────────
        void LoadMaster()
        {
            if (!File.Exists("vocab_1000.json")) return;
            try
            {
                var json = File.ReadAllText("vocab_1000.json");
                _master = JsonSerializer.Deserialize<List<MasterWord>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new List<MasterWord>();
            }
            catch { }
        }

        void LoadProgress()
        {
            try
            {
                if (File.Exists(PROGRESS_FILE))
                {
                    var doc = JsonDocument.Parse(File.ReadAllText(PROGRESS_FILE));
                    _dailyStartIndex = doc.RootElement.GetProperty("startIndex").GetInt32();
                    _lastLearnDate = doc.RootElement.GetProperty("lastDate").GetString() ?? "";
                }
            }
            catch { }
            string today = DateTime.Today.ToString("yyyy-MM-dd");
            if (_lastLearnDate != today)
            {
                if (_lastLearnDate != "" && _master.Count > 0)
                    _dailyStartIndex = (_dailyStartIndex + 10) % _master.Count;
                _lastLearnDate = today;
                SaveProgress();
            }
        }

        void SaveProgress()
        {
            File.WriteAllText(PROGRESS_FILE, JsonSerializer.Serialize(
                new { startIndex = _dailyStartIndex, lastDate = _lastLearnDate },
                new JsonSerializerOptions { WriteIndented = true }));
        }

        List<MasterWord> GetTodayWords()
        {
            if (_master.Count == 0) return new List<MasterWord>();
            return Enumerable.Range(0, 10)
                .Select(i => _master[(_dailyStartIndex + i) % _master.Count])
                .ToList();
        }

        // ── UI ────────────────────────────────────────────────────────
        void BuildUI()
        {
            Text = "Vocabulary Notebook";
            Size = new Size(950, 590);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = C_BG;

            var header = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = C_ACCENT };
            var lblH = new Label
            {
                Text = "Vocabulary Notebook",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0)
            };
            header.Controls.Add(lblH);
            Controls.Add(header);

            tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10), Padding = new Point(16, 6) };
            tabs.TabPages.Add(BuildNotesTab());
            tabs.TabPages.Add(BuildDaily10Tab());
            tabs.TabPages.Add(BuildQuizTab());
            tabs.SelectedIndexChanged += (s, e) =>
            {
                if (tabs.SelectedIndex == 1) RefreshDaily10();
                if (tabs.SelectedIndex == 2) StartQuiz();
            };
            Controls.Add(tabs);
        }

        TabPage BuildNotesTab()
        {
            var page = new TabPage("  My Notes  ") { BackColor = C_BG };
            var left = new Panel { Left = 0, Top = 0, Width = 250, Height = 500, BackColor = C_WHITE };
            left.Paint += (s, e) => { using var p = new Pen(C_BORDER); e.Graphics.DrawLine(p, left.Width - 1, 0, left.Width - 1, left.Height); };
            page.Controls.Add(left);

            int y = 25;
            left.Controls.Add(FL("WORD", y)); y += 20;
            txtWord = TB(y, 218); left.Controls.Add(txtWord); y += 34;
            left.Controls.Add(FL("MEANING  (Vietnamese)", y)); y += 20;
            txtMeaning = TB(y, 218, 54, true); left.Controls.Add(txtMeaning); y += 64;
            left.Controls.Add(FL("EXAMPLE SENTENCE", y)); y += 20;
            txtExample = TB(y, 218, 72, true); left.Controls.Add(txtExample); y += 82;
            btnSave = AB("Add Word", C_ACCENT, y, 218); btnSave.Click += BtnSave_Click; left.Controls.Add(btnSave); y += 42;
            btnClear = AB("Clear", C_SUB, y, 218); btnClear.Click += (s, e) => ClearInputs(); left.Controls.Add(btnClear);

            var right = new Panel { Left = 252, Top = 0, Width = 650, Height = 500 };
            page.Controls.Add(right);

            txtSearch = new TextBox { Left = 12, Top = 25, Width = 300, Font = new Font("Segoe UI", 10), PlaceholderText = "Search word or meaning..." };
            txtSearch.TextChanged += (s, e) => ApplyFilter();
            right.Controls.Add(txtSearch);

            btnDelete = new Button
            {
                Left = 320,
                Top = 25,
                Width = 90,
                Height = 30,
                Text = "Delete",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(254, 242, 242),
                ForeColor = C_RED,
                FlatStyle = FlatStyle.Flat
            };
            btnDelete.FlatAppearance.BorderSize = 1; btnDelete.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202);
            btnDelete.Click += BtnDelete_Click;
            right.Controls.Add(btnDelete);

            grid = new DataGridView
            {
                Left = 12,
                Top = 60,
                Width = 630,
                Height = 410,
                BackgroundColor = C_WHITE,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true,
                Font = new Font("Segoe UI", 10),
                EnableHeadersVisualStyles = false,
                GridColor = C_BORDER,
                ColumnHeadersHeight = 36,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
            };
            grid.RowTemplate.Height = 32;
            // === Cải thiện tiêu đề cột (Header) ===
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(37, 99, 235);   // Màu xanh accent đẹp (giống nút Add)
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;                    // Chữ trắng rõ ràng
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.ColumnHeadersHeight = 40;                                                 // Tăng chiều cao header cho thoáng
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;          // Đường viền rõ
            grid.EnableHeadersVisualStyles = false;                                        // Bắt buộc phải có dòng này!
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(255, 237, 213);
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(154, 52, 0);
            grid.DefaultCellStyle.Padding = new Padding(4, 0, 0, 0);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            grid.CellDoubleClick += Grid_CellDoubleClick;
            right.Controls.Add(grid);

            lblCount = new Label { Left = 12, Top = grid.Bottom + 6, Width = 400, Font = new Font("Segoe UI", 9), ForeColor = C_SUB };
            right.Controls.Add(lblCount);
            return page;
        }

        TabPage BuildDaily10Tab()
        {
            var page = new TabPage("  Daily 10  ") { BackColor = C_BG };
            daily10Panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(16) };
            page.Controls.Add(daily10Panel);
            return page;
        }

        void RefreshDaily10()
        {
            daily10Panel.Controls.Clear();
            var words = GetTodayWords();
            int day = (_dailyStartIndex / 10) + 1;
            int total = (int)Math.Ceiling(_master.Count / 10.0);

            var lblDay = new Label
            {
                Text = string.Format("Today's 10 words  —  Day {0} / {1}   (#{2} to #{3})",
                    day, total, _dailyStartIndex + 1, Math.Min(_dailyStartIndex + 10, _master.Count)),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = C_ACCENT,
                Left = 0,
                Top = 0,
                Width = 860,
                Height = 28
            };
            daily10Panel.Controls.Add(lblDay);

            var barBg = new Panel { Left = 0, Top = 34, Width = 860, Height = 6, BackColor = C_BORDER };
            var barFg = new Panel
            {
                Left = 0,
                Top = 0,
                Width = (int)(860.0 * _dailyStartIndex / Math.Max(_master.Count, 1)),
                Height = 6,
                BackColor = C_ACCENT
            };
            barBg.Controls.Add(barFg);
            daily10Panel.Controls.Add(barBg);

            var myWords = _allItems.Select(v => v.Word.Trim().ToLower()).ToHashSet();
            int y = 50;
            for (int i = 0; i < words.Count; i++)
            {
                var card = BuildWordCard(words[i], i + 1, y, myWords);
                daily10Panel.Controls.Add(card);
                y += card.Height + 8;
            }
            int written = words.Count(w => myWords.Contains(w.Word.ToLower()));
            var lblW = new Label
            {
                Text = string.Format("You've written {0}/10 of today's words in your notes.", written),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                ForeColor = written == 10 ? C_GREEN : C_SUB,
                Left = 0,
                Top = y + 4,
                Width = 860,
                Height = 22
            };
            daily10Panel.Controls.Add(lblW);
        }

        Panel BuildWordCard(MasterWord w, int num, int top, HashSet<string> myWords)
        {
            bool already = myWords.Contains(w.Word.ToLower());
            var card = new Panel { Left = 0, Top = top, Width = 860, Height = 56, BackColor = C_WHITE };
            card.Paint += (s, e) => { using var p = new Pen(C_BORDER); e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1); };

            var badge = new Label
            {
                Text = num.ToString(),
                Left = 10,
                Top = 14,
                Width = 28,
                Height = 28,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = C_ACCENT,
                BackColor = Color.FromArgb(239, 246, 255)
            };
            card.Controls.Add(badge);

            var lblWord = new Label
            {
                Text = w.Word,
                Left = 46,
                Top = 10,
                Width = 200,
                Height = 22,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = C_TEXT
            };
            card.Controls.Add(lblWord);

            var chip = new Label
            {
                Text = w.Topic,
                Left = 46,
                Top = 32,
                Width = 120,
                Height = 16,
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = C_SUB
            };
            card.Controls.Add(chip);

            var lblMeaning = new Label
            {
                Text = w.Meaning,
                Left = 260,
                Top = 18,
                Width = 360,
                Font = new Font("Segoe UI", 10.5f),
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            card.Controls.Add(lblMeaning);

            var btn = new Button
            {
                Text = already ? "Written" : "Write it",
                Left = 636,
                Top = 13,
                Width = 100,
                Height = 30,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                BackColor = already ? Color.FromArgb(240, 253, 244) : Color.FromArgb(239, 246, 255),
                ForeColor = already ? C_GREEN : C_ACCENT,
                FlatStyle = FlatStyle.Flat,
                Enabled = !already,
                Cursor = already ? Cursors.Default : Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = already ? Color.FromArgb(187, 247, 208) : Color.FromArgb(191, 219, 254);
            btn.Click += (s, e) => { txtWord.Text = w.Word; txtMeaning.Text = w.Meaning; txtExample.Text = w.Example; tabs.SelectedIndex = 0; txtWord.Focus(); };
            card.Controls.Add(btn);
            return card;
        }

        TabPage BuildQuizTab()
        {
            var page = new TabPage("  Quiz  ") { BackColor = C_BG };
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(30) };
            page.Controls.Add(panel);

            lblQuizScore = new Label { Left = 30, Top = 20, Width = 800, Height = 24, Font = new Font("Segoe UI", 10), ForeColor = C_SUB };
            panel.Controls.Add(lblQuizScore);

            lblQuizTopic = new Label { Left = 30, Top = 58, Width = 400, Height = 22, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = C_ACCENT };
            panel.Controls.Add(lblQuizTopic);

            lblQuizWord = new Label
            {
                Left = 30,
                Top = 86,
                Width = 800,
                Height = 70,
                Font = new Font("Segoe UI", 32, FontStyle.Bold),
                ForeColor = C_TEXT,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft
            };
            panel.Controls.Add(lblQuizWord);

            var lblDir = new Label
            {
                Text = "What is the Vietnamese meaning?",
                Left = 30,
                Top = 162,
                Width = 500,
                Height = 22,
                Font = new Font("Segoe UI", 10, FontStyle.Italic),
                ForeColor = C_SUB
            };
            panel.Controls.Add(lblDir);

            lblQuizAnswer = new Label { Left = 30, Top = 194, Width = 800, Height = 46, Font = new Font("Segoe UI", 16), ForeColor = C_GREEN, Text = "" };
            panel.Controls.Add(lblQuizAnswer);

            btnReveal = AB("Show Answer", C_ACCENT, 250, 160); btnReveal.Left = 30; panel.Controls.Add(btnReveal);
            btnCorrect = AB("Correct", C_GREEN, 250, 140); btnCorrect.Left = 200; btnCorrect.Enabled = false; panel.Controls.Add(btnCorrect);
            btnWrong = AB("Wrong", C_RED, 250, 120); btnWrong.Left = 350; btnWrong.Enabled = false; panel.Controls.Add(btnWrong);
            btnNextQuiz = AB("Skip", C_SUB, 250, 100); btnNextQuiz.Left = 480; panel.Controls.Add(btnNextQuiz);

            btnReveal.Click += (s, e) => { if (_currentQuizWord == null || _quizRevealed) return; lblQuizAnswer.Text = _currentQuizWord.Meaning; _quizRevealed = true; btnReveal.Enabled = false; btnCorrect.Enabled = true; btnWrong.Enabled = true; };
            btnCorrect.Click += (s, e) => { _quizCorrect++; _quizTotal++; UpdateQuizScore(); LoadNextQuizWord(); };
            btnWrong.Click += (s, e) => { _quizTotal++; UpdateQuizScore(); LoadNextQuizWord(); };
            btnNextQuiz.Click += (s, e) => LoadNextQuizWord();
            return page;
        }

        void StartQuiz() { _quizCorrect = 0; _quizTotal = 0; UpdateQuizScore(); LoadNextQuizWord(); }

        void LoadNextQuizWord()
        {
            if (_master.Count == 0) { lblQuizWord.Text = "(No master list)"; return; }
            var pool = GetTodayWords();
            _currentQuizWord = pool[new Random().Next(pool.Count)];
            lblQuizWord.Text = _currentQuizWord.Word;
            lblQuizTopic.Text = string.Format("Topic: {0}   #{1}", _currentQuizWord.Topic, _currentQuizWord.Stt);
            lblQuizAnswer.Text = ""; _quizRevealed = false;
            btnReveal.Enabled = true; btnCorrect.Enabled = false; btnWrong.Enabled = false;
        }

        void UpdateQuizScore()
        {
            lblQuizScore.Text = _quizTotal == 0 ? "Answer questions to see your score"
                : string.Format("Score:  {0} / {1}   ({2}%)", _quizCorrect, _quizTotal, (int)(100.0 * _quizCorrect / _quizTotal));
        }

        // ── Notes data ────────────────────────────────────────────────
        void LoadAll() { _allItems = new BindingList<VocabItem>(_service.Load()); ApplyFilter(); }

        void ApplyFilter()
        {
            string kw = txtSearch?.Text.Trim().ToLower() ?? "";
            var result = string.IsNullOrEmpty(kw) ? _allItems.ToList()
                : _allItems.Where(v => v.Word.ToLower().Contains(kw) || v.Meaning.ToLower().Contains(kw) || v.Example.ToLower().Contains(kw)).ToList();
            _viewItems = new BindingList<VocabItem>(result);
            if (grid.DataSource == null) { grid.DataSource = _viewItems; SetupColumns(); }
            else grid.DataSource = _viewItems;
            lblCount.Text = string.Format("Total: {0} words   |   Showing: {1}", _allItems.Count, _viewItems.Count);
        }

        void SetupColumns()
        {
            foreach (DataGridViewColumn c in grid.Columns) c.Visible = false;
            ShowCol("Word", "Word", 130); ShowCol("Meaning", "Meaning (VN)", 170);
            ShowCol("Example", "Example", 250); ShowCol("AddedDate", "Added", 80);
            grid.Columns["AddedDate"].DefaultCellStyle.Format = "dd/MM/yy";
        }

        void ShowCol(string name, string header, int width)
        {
            if (!grid.Columns.Contains(name)) return;
            grid.Columns[name].Visible = true; grid.Columns[name].HeaderText = header;
            grid.Columns[name].Width = width; grid.Columns[name].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        }

        void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtWord.Text)) { Warn("Please enter the word."); return; }
            if (string.IsNullOrWhiteSpace(txtMeaning.Text)) { Warn("Please enter the meaning."); return; }

            string inputWord = txtWord.Text.Trim();
            var master = _master.FirstOrDefault(m => string.Equals(m.Word, inputWord, StringComparison.OrdinalIgnoreCase));
            if (master != null)
            {
                bool ok = master.Meaning.ToLower().Split(new[] { ',', ' ', '/' }, StringSplitOptions.RemoveEmptyEntries)
                    .Any(part => txtMeaning.Text.ToLower().Contains(part));
                if (!ok)
                {
                    var r = MessageBox.Show(
                        string.Format("Heads up!\n\nYou wrote:  \"{0}\"\nMaster list says:  \"{1}\"\n\nSave anyway?",
                            txtMeaning.Text.Trim(), master.Meaning),
                        "Meaning mismatch", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (r == DialogResult.No) return;
                }
                else
                    MessageBox.Show("Correct! Matches the master list.", "Good job!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            _allItems.Add(new VocabItem { Word = inputWord, Meaning = txtMeaning.Text.Trim(), Example = txtExample.Text.Trim(), AddedDate = DateTime.Now });
            _service.Save(_allItems.ToList()); ApplyFilter(); ClearInputs();
        }

        void BtnDelete_Click(object sender, EventArgs e)
        {
            var item = grid.CurrentRow?.DataBoundItem as VocabItem;
            if (item == null) return;
            if (MessageBox.Show(string.Format("Delete \"{0}\"?", item.Word), "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            { _allItems.Remove(item); _service.Save(_allItems.ToList()); ApplyFilter(); }
        }

        void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var item = grid.Rows[e.RowIndex].DataBoundItem as VocabItem;
            if (item == null) return;
            OpenEditPopup(item);
        }

        void OpenEditPopup(VocabItem item)
        {
            using var popup = new Form
            {
                Text = string.Format("Edit — {0}", item.Word),
                Size = new Size(420, 310),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = C_WHITE
            };
            popup.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 5, BackColor = C_ACCENT });
            int y = 18;
            popup.Controls.Add(PL("WORD", y)); y += 20;
            var pWord = PT(y, 370); popup.Controls.Add(pWord); pWord.Text = item.Word; y += 34;
            popup.Controls.Add(PL("MEANING  (Vietnamese)", y)); y += 20;
            var pMeaning = PT(y, 370, 52, true); popup.Controls.Add(pMeaning); pMeaning.Text = item.Meaning; y += 62;
            popup.Controls.Add(PL("EXAMPLE SENTENCE", y)); y += 20;
            var pExample = PT(y, 370, 52, true); popup.Controls.Add(pExample); pExample.Text = item.Example; y += 62;
            var ok = AB("Save", C_ACCENT, y, 160); ok.Left = 20; ok.DialogResult = DialogResult.OK;
            var cancel = AB("Cancel", C_SUB, y, 140); cancel.Left = 190; cancel.DialogResult = DialogResult.Cancel;
            popup.Controls.Add(ok); popup.Controls.Add(cancel);
            popup.AcceptButton = ok; popup.CancelButton = cancel;
            ok.Click += (s, ev) =>
            {
                if (string.IsNullOrWhiteSpace(pWord.Text)) { Warn("Word cannot be empty."); popup.DialogResult = DialogResult.None; return; }
                if (string.IsNullOrWhiteSpace(pMeaning.Text)) { Warn("Meaning cannot be empty."); popup.DialogResult = DialogResult.None; return; }
            };
            if (popup.ShowDialog(this) == DialogResult.OK)
            {
                item.Word = pWord.Text.Trim(); item.Meaning = pMeaning.Text.Trim(); item.Example = pExample.Text.Trim();
                _service.Save(_allItems.ToList()); ApplyFilter();
            }
        }

        void ClearInputs() { txtWord.Clear(); txtMeaning.Clear(); txtExample.Clear(); }

        void Warn(string msg) => MessageBox.Show(msg, "Missing field", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        Label FL(string text, int top) => new Label { Text = text, Left = 16, Top = top, Width = 218, Height = 18, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = C_SUB };
        TextBox TB(int top, int width, int height = 26, bool multiline = false) => new TextBox { Left = 16, Top = top, Width = width, Height = height, Multiline = multiline, Font = new Font("Segoe UI", 10), BorderStyle = BorderStyle.FixedSingle };
        Button AB(string text, Color color, int top, int width) => new Button { Text = text, Left = 16, Top = top, Width = width, Height = 34, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, FlatAppearance = { BorderSize = 0 }, Cursor = Cursors.Hand };
        static Label PL(string text, int top) => new Label { Text = text, Left = 20, Top = top, Width = 370, Height = 18, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(90, 90, 90) };
        static TextBox PT(int top, int width, int height = 26, bool multiline = false) => new TextBox { Left = 20, Top = top, Width = width, Height = height, Multiline = multiline, ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None, Font = new Font("Segoe UI", 10), BorderStyle = BorderStyle.FixedSingle };
    }
}
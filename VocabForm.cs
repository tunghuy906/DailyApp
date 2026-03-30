using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DailyPlannerApp.Models;
using DailyPlannerApp.Services;

namespace DailyPlannerApp
{
    public class VocabForm : Form
    {
        private readonly VocabService _service = new VocabService();
        private BindingList<VocabItem> _allItems;
        private BindingList<VocabItem> _viewItems;

        // Input
        private TextBox txtWord;
        private TextBox txtPhonetic;
        private TextBox txtMeaning;
        private TextBox txtExample;
        private Button btnSave;
        private Button btnClear;

        // List
        private TextBox txtSearch;
        private DataGridView grid;
        private Label lblCount;
        private Button btnDelete;

        private VocabItem _editingItem = null;

        public VocabForm()
        {
            BuildUI();
            LoadAll();
        }

        // ═══════════════════════════════════════════════════════════════
        //  UI
        // ═══════════════════════════════════════════════════════════════
        void BuildUI()
        {
            Text = "📖 Vocabulary Notebook";
            Size = new Size(860, 520);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Color.FromArgb(245, 247, 250);

            // Header
            var header = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = Color.FromArgb(30, 136, 229) };
            var lblHeader = new Label
            {
                Text = "📖  Vocabulary Notebook",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 0, 0)
            };
            header.Controls.Add(lblHeader);
            Controls.Add(header);

            // ── Left panel (input) ──────────────────────────────────────
            var left = new Panel
            {
                Left = 0,
                Top = 46,
                Width = 260,
                Height = ClientSize.Height - 46,
                BackColor = Color.White
            };
            Controls.Add(left);

            int y = 16;

            left.Controls.Add(SectionLabel("WORD", y)); y += 22;
            txtWord = InputBox(y, 228); left.Controls.Add(txtWord); y += 34;

            left.Controls.Add(SectionLabel("PHONETIC  (e.g. /θɪŋk/)", y)); y += 22;
            txtPhonetic = InputBox(y, 228); left.Controls.Add(txtPhonetic); y += 34;

            left.Controls.Add(SectionLabel("MEANING  (Vietnamese)", y)); y += 22;
            txtMeaning = InputBox(y, 228, 54, true); left.Controls.Add(txtMeaning); y += 64;

            left.Controls.Add(SectionLabel("EXAMPLE SENTENCE", y)); y += 22;
            txtExample = InputBox(y, 228, 72, true); left.Controls.Add(txtExample); y += 82;

            btnSave = ActionButton("➕  Add Word", Color.FromArgb(30, 136, 229), y, 228);
            btnSave.Click += BtnSave_Click;
            left.Controls.Add(btnSave); y += 42;

            btnClear = ActionButton("✖  Clear", Color.FromArgb(120, 120, 120), y, 228);
            btnClear.Click += (s, e) => ClearInputs();
            left.Controls.Add(btnClear);

            // ── Right panel (list) ──────────────────────────────────────
            var right = new Panel
            {
                Left = 262,
                Top = 46,
                Width = ClientSize.Width - 262,
                Height = ClientSize.Height - 46
            };
            Controls.Add(right);

            // Search bar
            var searchPanel = new Panel { Left = 12, Top = 12, Width = right.Width - 24, Height = 34 };
            right.Controls.Add(searchPanel);

            txtSearch = new TextBox
            {
                Left = 0,
                Top = 3,
                Width = 320,
                Font = new Font("Segoe UI", 10),
                PlaceholderText = "🔍  Search word or meaning..."
            };
            txtSearch.TextChanged += (s, e) => ApplyFilter();
            searchPanel.Controls.Add(txtSearch);

            btnDelete = new Button
            {
                Left = 330,
                Top = 1,
                Width = 90,
                Height = 32,
                Text = "🗑  Delete",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(211, 47, 47),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.Click += BtnDelete_Click;
            searchPanel.Controls.Add(btnDelete);

            // Grid
            grid = new DataGridView
            {
                Left = 12,
                Top = 56,
                Width = right.Width - 24,
                Height = right.Height - 96,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true,
                Font = new Font("Segoe UI", 10),
                EnableHeadersVisualStyles = false,
                GridColor = Color.FromArgb(220, 220, 220)
            };
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 136, 229);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(30, 136, 229);
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 245, 255);
            grid.CellDoubleClick += Grid_CellDoubleClick;
            right.Controls.Add(grid);

            lblCount = new Label
            {
                Left = 12,
                Top = grid.Bottom + 6,
                Width = 400,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray
            };
            right.Controls.Add(lblCount);
            grid.AllowUserToResizeRows = false;
            grid.AllowUserToResizeColumns = false;
            grid.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        }

        // ═══════════════════════════════════════════════════════════════
        //  DATA
        // ═══════════════════════════════════════════════════════════════
        void LoadAll()
        {
            _allItems = new BindingList<VocabItem>(_service.Load());
            ApplyFilter();
        }

        void ApplyFilter()
        {
            string kw = txtSearch?.Text.Trim().ToLower() ?? "";

            var result = string.IsNullOrEmpty(kw)
                ? _allItems.ToList()
                : _allItems.Where(v =>
                    v.Word.ToLower().Contains(kw) ||
                    v.Meaning.ToLower().Contains(kw) ||
                    v.Example.ToLower().Contains(kw)).ToList();

            _viewItems = new BindingList<VocabItem>(result);

            if (grid.DataSource == null)
            {
                grid.DataSource = _viewItems;
                SetupColumns();
            }
            else
            {
                grid.DataSource = _viewItems;
            }

            lblCount.Text = $"Total: {_allItems.Count} words    |    Showing: {_viewItems.Count}";
        }

        void SetupColumns()
        {
            foreach (DataGridViewColumn col in grid.Columns)
                col.Visible = false;

            ShowCol("Word", "Word", 130);
            ShowCol("Phonetic", "Phonetic", 120);
            ShowCol("Meaning", "Meaning (VN)", 160);
            ShowCol("Example", "Example", 210);
            ShowCol("AddedDate", "Added", 80);

            grid.Columns["AddedDate"].DefaultCellStyle.Format = "dd/MM/yy";
        }

        void ShowCol(string name, string header, int width)
        {
            if (!grid.Columns.Contains(name)) return;
            grid.Columns[name].Visible = true;
            grid.Columns[name].HeaderText = header;
            grid.Columns[name].Width = width;
            grid.Columns[name].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        }

        // ═══════════════════════════════════════════════════════════════
        //  EVENTS
        // ═══════════════════════════════════════════════════════════════
        void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtWord.Text))
            {
                MessageBox.Show("Please enter the word.", "Missing field",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtMeaning.Text))
            {
                MessageBox.Show("Please enter the meaning.", "Missing field",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_editingItem != null)
            {
                _editingItem.Word = txtWord.Text.Trim();
                _editingItem.Phonetic = txtPhonetic.Text.Trim();
                _editingItem.Meaning = txtMeaning.Text.Trim();
                _editingItem.Example = txtExample.Text.Trim();
                _editingItem = null;
            }
            else
            {
                _allItems.Add(new VocabItem
                {
                    Word = txtWord.Text.Trim(),
                    Phonetic = txtPhonetic.Text.Trim(),
                    Meaning = txtMeaning.Text.Trim(),
                    Example = txtExample.Text.Trim(),
                    AddedDate = DateTime.Now
                });
            }

            _service.Save(_allItems.ToList());
            ApplyFilter();
            ClearInputs();
        }

        void BtnDelete_Click(object sender, EventArgs e)
        {
            var item = grid.CurrentRow?.DataBoundItem as VocabItem;
            if (item == null) return;

            if (MessageBox.Show($"Delete \"{item.Word}\"?", "Confirm",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _allItems.Remove(item);
                _service.Save(_allItems.ToList());
                ApplyFilter();
            }
        }

        // Double-click → load lên form để sửa
        void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var item = grid.Rows[e.RowIndex].DataBoundItem as VocabItem;
            if (item == null) return;

            _editingItem = item;
            txtWord.Text = item.Word;
            txtPhonetic.Text = item.Phonetic;
            txtMeaning.Text = item.Meaning;
            txtExample.Text = item.Example;

            btnSave.Text = "💾  Save Changes";
            btnClear.Text = "✖  Cancel Edit";
            txtWord.Focus();
        }

        // ═══════════════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════════════
        void ClearInputs()
        {
            _editingItem = null;
            txtWord.Clear(); txtPhonetic.Clear();
            txtMeaning.Clear(); txtExample.Clear();
            btnSave.Text = "➕  Add Word";
            btnClear.Text = "✖  Clear";
        }

        Label SectionLabel(string text, int top) => new Label
        {
            Text = text,
            Left = 16,
            Top = top,
            Width = 228,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(90, 90, 90)
        };

        TextBox InputBox(int top, int width, int height = 26, bool multiline = false) =>
            new TextBox
            {
                Left = 16,
                Top = top,
                Width = width,
                Height = height,
                Multiline = multiline,
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle
            };

        Button ActionButton(string text, Color color, int top, int width) =>
            new Button
            {
                Text = text,
                Left = 16,
                Top = top,
                Width = width,
                Height = 36,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 }
            };
    }
}
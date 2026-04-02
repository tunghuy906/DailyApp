using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DailyPlannerApp
{
    // ══════════════════════════════════════════════════════════════════════
    //  MODEL
    // ══════════════════════════════════════════════════════════════════════
    public class BudgetItem
    {
        public string   Task     { get; set; } = "";
        public decimal  Amount   { get; set; }
        public string   Category { get; set; } = "Other";
        public string   Note     { get; set; } = "";
        public DateTime Date     { get; set; } = DateTime.Today;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  FORM
    // ══════════════════════════════════════════════════════════════════════
    public class BudgetForm : Form
    {
        // ── Palette ────────────────────────────────────────────────────
        static readonly Color C_BG      = Color.FromArgb(245, 246, 250);
        static readonly Color C_SIDEBAR = Color.FromArgb(255, 255, 255);
        static readonly Color C_HEADER  = Color.DarkGreen;
        static readonly Color C_ACCENT  = Color.FromArgb(59,  130, 246);
        static readonly Color C_DANGER  = Color.FromArgb(220, 38,  38);
        static readonly Color C_TEXT    = Color.FromArgb(30,  41,  59);
        static readonly Color C_SUBTEXT = Color.FromArgb(100, 116, 139);
        static readonly Color C_BORDER  = Color.FromArgb(226, 232, 240);
        static readonly Color C_GREEN   = Color.FromArgb(22,  163, 74);
        static readonly Color C_ROW_ALT = Color.FromArgb(248, 250, 252);

        static readonly string[] Categories = { "Food", "Transport", "Health", "Education", "Entertainment", "Bills", "Shopping", "Other" };

        readonly BudgetService service = new();

        // ── Controls ──────────────────────────────────────────────────
        TextBox        txtTask;
        TextBox        txtAmount;
        TextBox        txtNote;
        ComboBox       cmbCategory;
        DateTimePicker dtDate;
        DataGridView   grid;
        Label          lblTotal;
        Label          lblBudgetLeft;
        TextBox        txtBudgetLimit;
        ComboBox       cmbMonth;
        ComboBox       cmbYear;

        List<BudgetItem> items       = new();
        decimal          budgetLimit = 0;

        public BudgetForm()
        {
            Text          = "💰 Monthly Budget";
            Size          = new Size(1000, 680);
            MinimumSize   = new Size(900, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor     = C_BG;
            Font          = new Font("Segoe UI", 10);
            AutoScaleMode = AutoScaleMode.Font;

            BuildUI();
            var data    = service.Load();
            items       = data.Items;
            budgetLimit = data.BudgetLimit;
            if (budgetLimit > 0)
                txtBudgetLimit.Text = budgetLimit.ToString("N0").Replace(",", "");
            RefreshGrid();
        }

        // ══════════════════════════════════════════════════════════════
        //  BUILD UI
        // ══════════════════════════════════════════════════════════════
        void BuildUI()
        {
            // ── Header ─────────────────────────────────────────────────
            var header = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = C_HEADER };
            var lblTitle = new Label
            {
                Text      = "💰  Monthly Budget Tracker",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 15, FontStyle.Bold),
                Dock      = DockStyle.Left,
                Width     = 360,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(16, 0, 0, 0)
            };

            // Month / Year filter
            var filterPanel = new Panel { Dock = DockStyle.Right, Width = 300, BackColor = C_HEADER };
            var lblFilter = new Label
            {
                Text = "Month:", ForeColor = Color.White,
                Font = new Font("Segoe UI", 9), Left = 8, Top = 18, Width = 48, Height = 20
            };
            cmbMonth = new ComboBox
            {
                Left = 60, Top = 16, Width = 90, DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };
            for (int m = 1; m <= 12; m++) cmbMonth.Items.Add($"Month {m}");
            cmbMonth.SelectedIndex = DateTime.Today.Month - 1;

            var lblYear = new Label
            {
                Text = "Year:", ForeColor = Color.White,
                Font = new Font("Segoe UI", 9), Left = 160, Top = 18, Width = 36, Height = 20
            };
            cmbYear = new ComboBox
            {
                Left = 200, Top = 16, Width = 80, DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };
            for (int y = DateTime.Today.Year - 2; y <= DateTime.Today.Year + 1; y++) cmbYear.Items.Add(y);
            cmbYear.SelectedItem = DateTime.Today.Year;

            cmbMonth.SelectedIndexChanged += (s, e) => RefreshGrid();
            cmbYear.SelectedIndexChanged  += (s, e) => RefreshGrid();

            filterPanel.Controls.AddRange(new Control[] { lblFilter, cmbMonth, lblYear, cmbYear });
            header.Controls.Add(filterPanel);
            header.Controls.Add(lblTitle);

            // ── Sidebar ────────────────────────────────────────────────
            var sidebar = new Panel { Dock = DockStyle.Left, Width = 240, BackColor = C_SIDEBAR };
            sidebar.Paint += (s, e) =>
            {
                using var p = new Pen(C_BORDER, 1);
                e.Graphics.DrawLine(p, sidebar.Width - 1, 0, sidebar.Width - 1, sidebar.Height);
            };
            BuildSidebar(sidebar);

            // ── Main content ───────────────────────────────────────────
            var content = new Panel { Dock = DockStyle.Fill, BackColor = C_BG, Padding = new Padding(12) };

            // Summary bar
            var summaryBar = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.White };
            summaryBar.Paint += (s, e) =>
            {
                using var p = new Pen(C_BORDER);
                e.Graphics.DrawLine(p, 0, 0, summaryBar.Width, 0);
            };
            lblTotal = new Label
            {
                Text      = "Total spent: 0 ₫",
                Font      = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = C_DANGER,
                Dock      = DockStyle.Left,
                Width     = 340,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(16, 0, 0, 0)
            };
            lblBudgetLeft = new Label
            {
                Text      = "Budget: not set",
                Font      = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = C_SUBTEXT,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            summaryBar.Controls.Add(lblBudgetLeft);
            summaryBar.Controls.Add(lblTotal);

            // ── Grid ───────────────────────────────────────────────────
            grid = new DataGridView
            {
                Dock                        = DockStyle.Fill,
                BackgroundColor             = Color.White,
                BorderStyle                 = BorderStyle.None,
                RowHeadersVisible           = false,
                AllowUserToAddRows          = false,
                AllowUserToDeleteRows       = false,
                AllowUserToResizeRows       = false,
                AllowUserToResizeColumns    = false,   // ← khóa kéo dãn cột
                SelectionMode               = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode         = DataGridViewAutoSizeColumnsMode.None, // ← fixed width
                EnableHeadersVisualStyles   = false,
                GridColor                   = C_BORDER,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight         = 38,
                RowTemplate                 = { Height = 34 },
                CellBorderStyle             = DataGridViewCellBorderStyle.SingleHorizontal,
                ReadOnly                    = true
            };

            grid.ColumnHeadersDefaultCellStyle.BackColor  = Color.FromArgb(37, 99, 235);
            grid.ColumnHeadersDefaultCellStyle.ForeColor  = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font       = new Font("Segoe UI", 9, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Alignment  = DataGridViewContentAlignment.MiddleCenter;
            grid.ColumnHeadersBorderStyle                 = DataGridViewHeaderBorderStyle.Single;

            grid.DefaultCellStyle.Font               = new Font("Segoe UI", 10);
            grid.DefaultCellStyle.ForeColor          = C_TEXT;
            grid.DefaultCellStyle.BackColor          = Color.White;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 64, 175);
            grid.DefaultCellStyle.Padding            = new Padding(6, 0, 0, 0);
            grid.AlternatingRowsDefaultCellStyle.BackColor = C_ROW_ALT;

            // Fixed-width columns (no stretch)
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate",     HeaderText = "Date",        Width = 100 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTask",     HeaderText = "Task",        Width = 200 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCategory", HeaderText = "Category",    Width = 110 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAmount",   HeaderText = "Amount (₫)",  Width = 130 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNote",     HeaderText = "Note",        Width = 220 });

            // Last column fills remaining space but is still not user-resizable
            grid.Columns["colNote"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            foreach (DataGridViewColumn col in grid.Columns)
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.Columns["colAmount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            // ── Click on Amount cell → show detail popup ───────────────
            grid.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                if (grid.Columns[e.ColumnIndex].Name == "colAmount")
                    ShowDetail(e.RowIndex);
            };

            // Cursor hint on Amount column
            grid.CellMouseEnter += (s, e) =>
            {
                if (e.RowIndex >= 0 && grid.Columns[e.ColumnIndex].Name == "colAmount")
                    grid.Cursor = Cursors.Hand;
            };
            grid.CellMouseLeave += (s, e) => grid.Cursor = Cursors.Default;

            content.Controls.Add(grid);
            content.Controls.Add(summaryBar);

            Controls.Add(content);
            Controls.Add(sidebar);
            Controls.Add(header);
        }

        void BuildSidebar(Panel sidebar)
        {
            int y = 16;

            sidebar.Controls.Add(SLabel("ADD EXPENSE", y, true)); y += 28;

            sidebar.Controls.Add(SLabel("Task / Goal", y)); y += 20;
            txtTask = new TextBox
            {
                Left = 12, Top = y, Width = 210, Height = 26,
                Font = new Font("Segoe UI", 10), BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White, ForeColor = C_TEXT
            };
            sidebar.Controls.Add(txtTask); y += 34;

            sidebar.Controls.Add(SLabel("Amount (VND)", y)); y += 20;
            txtAmount = new TextBox
            {
                Left = 12, Top = y, Width = 210, Height = 26,
                Font = new Font("Segoe UI", 10), BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White, ForeColor = C_TEXT,
                PlaceholderText = "e.g. 150000"
            };
            sidebar.Controls.Add(txtAmount); y += 34;

            sidebar.Controls.Add(SLabel("Category", y)); y += 20;
            cmbCategory = new ComboBox
            {
                Left = 12, Top = y, Width = 210, DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10)
            };
            foreach (var c in Categories) cmbCategory.Items.Add(c);
            cmbCategory.SelectedIndex = 0;
            sidebar.Controls.Add(cmbCategory); y += 34;

            sidebar.Controls.Add(SLabel("Date", y)); y += 20;
            dtDate = new DateTimePicker
            {
                Left = 12, Top = y, Width = 210,
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 9.5f),
                Value = DateTime.Today
            };
            sidebar.Controls.Add(dtDate); y += 34;

            sidebar.Controls.Add(SLabel("Note (optional)", y)); y += 20;
            txtNote = new TextBox
            {
                Left = 12, Top = y, Width = 210, Height = 60,
                Multiline = true, Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White, ForeColor = C_TEXT
            };
            sidebar.Controls.Add(txtNote); y += 70;

            // ── Add button — same style as Delete ─────────────────────
            var btnAdd = new Button
            {
                Left = 12, Top = y, Width = 210, Height = 34,
                Text = "＋  Add Expense",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(240, 253, 244),
                ForeColor = C_GREEN,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btnAdd.FlatAppearance.BorderSize  = 1;
            btnAdd.FlatAppearance.BorderColor = Color.FromArgb(187, 247, 208);
            btnAdd.Click += BtnAdd_Click;
            sidebar.Controls.Add(btnAdd); y += 44;

            var div = new Panel { Left = 12, Top = y, Width = 210, Height = 1, BackColor = C_BORDER };
            sidebar.Controls.Add(div); y += 12;

            // ── Delete button ──────────────────────────────────────────
            var btnDelete = new Button
            {
                Left = 12, Top = y, Width = 210, Height = 34,
                Text = "🗑  Delete Selected",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(254, 242, 242),
                ForeColor = C_DANGER, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btnDelete.FlatAppearance.BorderSize  = 1;
            btnDelete.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202);
            btnDelete.Click += BtnDelete_Click;
            sidebar.Controls.Add(btnDelete); y += 44;

            var div2 = new Panel { Left = 12, Top = y, Width = 210, Height = 1, BackColor = C_BORDER };
            sidebar.Controls.Add(div2); y += 12;

            // ── Exit button ────────────────────────────────────────────
            var btnExit = new Button
            {
                Left = 12, Top = y, Width = 210, Height = 34,
                Text = "✖  Exit",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(255, 251, 235),
                ForeColor = Color.FromArgb(184, 134, 11),
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btnExit.FlatAppearance.BorderSize  = 1;
            btnExit.FlatAppearance.BorderColor = Color.FromArgb(252, 211, 77);
            btnExit.Click += (s, e) => Close();
            sidebar.Controls.Add(btnExit); y += 44;

            var div3 = new Panel { Left = 12, Top = y, Width = 210, Height = 1, BackColor = C_BORDER };
            sidebar.Controls.Add(div3); y += 12;

            // ── Monthly budget limit ───────────────────────────────────
            sidebar.Controls.Add(SLabel("🎯  MONTHLY BUDGET (₫)", y, true)); y += 24;
            txtBudgetLimit = new TextBox
            {
                Left = 12, Top = y, Width = 160, Height = 26,
                Font = new Font("Segoe UI", 10), BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White, ForeColor = C_TEXT,
                PlaceholderText = "0"
            };
            sidebar.Controls.Add(txtBudgetLimit);

            var btnSetLimit = new Button
            {
                Left = 178, Top = y, Width = 44, Height = 26,
                Text = "✓", Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(240, 253, 244), ForeColor = C_GREEN,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btnSetLimit.FlatAppearance.BorderSize = 1;
            btnSetLimit.FlatAppearance.BorderColor = Color.FromArgb(187, 247, 208);
            btnSetLimit.Click += (s, e) =>
            {
                string raw = txtBudgetLimit.Text.Replace(",", "").Replace(".", "").Trim();
                if (decimal.TryParse(raw, out decimal lim))
                {
                    budgetLimit = lim;
                    service.Save(items, budgetLimit);
                    RefreshSummary(GetFilteredItems());
                }
                else MessageBox.Show("Please enter a valid number.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            };
            sidebar.Controls.Add(btnSetLimit);
        }

        Label SLabel(string text, int top, bool accent = false) => new Label
        {
            Text = text, Left = 12, Top = top, Width = 218, Height = accent ? 20 : 18,
            Font = new Font("Segoe UI", accent ? 8f : 8.5f, accent ? FontStyle.Bold : FontStyle.Regular),
            ForeColor = accent ? C_ACCENT : C_SUBTEXT
        };

        // ══════════════════════════════════════════════════════════════
        //  DETAIL POPUP
        // ══════════════════════════════════════════════════════════════
        void ShowDetail(int rowIndex)
        {
            var filtered = GetFilteredItems();
            if (rowIndex < 0 || rowIndex >= filtered.Count) return;
            var item = filtered[rowIndex];

            var (catBg, catFg) = CategoryColor(item.Category);

            using var popup = new Form
            {
                Text            = "Expense Detail",
                Size            = new Size(420, 320),
                StartPosition   = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false, MinimizeBox = false,
                BackColor       = Color.White
            };

            // Top accent bar
            var bar = new Panel { Dock = DockStyle.Top, Height = 5, BackColor = C_GREEN };
            popup.Controls.Add(bar);

            // Category badge
            var badge = new Panel { Left = 24, Top = 18, Width = 100, Height = 24, BackColor = catBg };
            var lblBadge = new Label
            {
                Text = item.Category, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = catFg
            };
            badge.Controls.Add(lblBadge);
            badge.Paint += (s, e) =>
            {
                using var p = new Pen(catFg, 1);
                e.Graphics.DrawRectangle(p, 0, 0, badge.Width - 1, badge.Height - 1);
            };
            popup.Controls.Add(badge);

            // Amount — big display
            var lblAmount = new Label
            {
                Text      = item.Amount.ToString("N0") + " ₫",
                Font      = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = C_DANGER,
                Left = 24, Top = 48, Width = 370, Height = 44,
                TextAlign = ContentAlignment.MiddleLeft
            };
            popup.Controls.Add(lblAmount);

            // Divider
            var divLine = new Panel { Left = 24, Top = 96, Width = 368, Height = 1, BackColor = C_BORDER };
            popup.Controls.Add(divLine);

            // Info rows
            int iy = 108;
            void AddRow(string label, string value)
            {
                popup.Controls.Add(new Label
                {
                    Text = label, Left = 24, Top = iy, Width = 100, Height = 22,
                    Font = new Font("Segoe UI", 9), ForeColor = C_SUBTEXT,
                    TextAlign = ContentAlignment.MiddleLeft
                });
                popup.Controls.Add(new Label
                {
                    Text = value, Left = 128, Top = iy, Width = 264, Height = 22,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = C_TEXT,
                    TextAlign = ContentAlignment.MiddleLeft
                });
            }

            AddRow("Task:",     item.Task);                           iy += 26;
            AddRow("Date:",     item.Date.ToString("dd/MM/yyyy"));    iy += 26;
            AddRow("Category:", item.Category);                       iy += 26;
            AddRow("Note:",     string.IsNullOrWhiteSpace(item.Note) ? "—" : item.Note); iy += 26;

            // Close button
            var btnClose = new Button
            {
                Text = "Close", Left = 290, Top = iy + 8, Width = 100, Height = 32,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(239, 246, 255), ForeColor = C_ACCENT,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize  = 1;
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(147, 197, 253);
            btnClose.Click += (s, e) => popup.Close();
            popup.Controls.Add(btnClose);

            popup.ShowDialog(this);
        }

        // ══════════════════════════════════════════════════════════════
        //  DATA LOGIC
        // ══════════════════════════════════════════════════════════════
        void BtnAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTask.Text))
            {
                MessageBox.Show("Please enter a task name.", "Missing info", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string raw = txtAmount.Text.Replace(",", "").Replace(".", "").Trim();
            if (!decimal.TryParse(raw, out decimal amount) || amount < 0)
            {
                MessageBox.Show("Invalid amount.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            items.Add(new BudgetItem
            {
                Task     = txtTask.Text.Trim(),
                Amount   = amount,
                Category = cmbCategory.SelectedItem?.ToString() ?? "Other",
                Note     = txtNote.Text.Trim(),
                Date     = dtDate.Value.Date
            });

            service.Save(items, budgetLimit);
            RefreshGrid();
            txtTask.Clear();
            txtAmount.Clear();
            txtNote.Clear();
        }

        void BtnDelete_Click(object sender, EventArgs e)
        {
            if (grid.CurrentRow == null) return;
            int idx      = grid.CurrentRow.Index;
            var filtered = GetFilteredItems();
            if (idx < 0 || idx >= filtered.Count) return;

            var toRemove = filtered[idx];
            if (MessageBox.Show($"Delete: {toRemove.Task}?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                items.Remove(toRemove);
                service.Save(items, budgetLimit);
                RefreshGrid();
            }
        }

        List<BudgetItem> GetFilteredItems()
        {
            int month = cmbMonth.SelectedIndex + 1;
            int year  = (int)(cmbYear.SelectedItem ?? DateTime.Today.Year);
            return items.Where(i => i.Date.Month == month && i.Date.Year == year)
                        .OrderByDescending(i => i.Date)
                        .ToList();
        }

        void RefreshGrid()
        {
            var filtered = GetFilteredItems();
            grid.Rows.Clear();

            foreach (var item in filtered)
            {
                int row = grid.Rows.Add(
                    item.Date.ToString("dd/MM/yyyy"),
                    item.Task,
                    item.Category,
                    item.Amount.ToString("N0") + " ₫",
                    item.Note
                );
                var (bg, fg) = CategoryColor(item.Category);
                grid.Rows[row].Cells["colCategory"].Style.BackColor = bg;
                grid.Rows[row].Cells["colCategory"].Style.ForeColor = fg;

                // Underline + blue the Amount cell as a clickable hint
                grid.Rows[row].Cells["colAmount"].Style.ForeColor = C_ACCENT;
                grid.Rows[row].Cells["colAmount"].Style.Font      = new Font("Segoe UI", 10, FontStyle.Underline);
            }

            RefreshSummary(filtered);
        }

        void RefreshSummary(List<BudgetItem> filtered)
        {
            decimal total = filtered.Sum(i => i.Amount);
            lblTotal.Text = $"Total spent: {total:N0} ₫";

            if (budgetLimit > 0)
            {
                decimal left = budgetLimit - total;
                lblBudgetLeft.Text      = left >= 0
                    ? $"Remaining: {left:N0} ₫  ✓"
                    : $"Over budget: {Math.Abs(left):N0} ₫  ⚠";
                lblBudgetLeft.ForeColor = left >= 0 ? C_GREEN : C_DANGER;
            }
            else
            {
                lblBudgetLeft.Text      = "Budget: not set";
                lblBudgetLeft.ForeColor = C_SUBTEXT;
            }
        }

        (Color bg, Color fg) CategoryColor(string cat) => cat switch
        {
            "Food"          => (Color.FromArgb(254, 243, 199), Color.FromArgb(146, 64,  14)),
            "Transport"     => (Color.FromArgb(219, 234, 254), Color.FromArgb(30,  64,  175)),
            "Health"        => (Color.FromArgb(220, 252, 231), Color.FromArgb(20,  83,  45)),
            "Education"     => (Color.FromArgb(237, 233, 254), Color.FromArgb(76,  29,  149)),
            "Entertainment" => (Color.FromArgb(255, 228, 230), Color.FromArgb(136, 19,  55)),
            "Bills"         => (Color.FromArgb(255, 237, 213), Color.FromArgb(124, 45,  18)),
            "Shopping"      => (Color.FromArgb(204, 251, 241), Color.FromArgb(17,  94,  89)),
            _               => (Color.FromArgb(241, 245, 249), Color.FromArgb(71,  85,  105))
        };

    }
}
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using DailyPlannerApp.Models;
using DailyPlannerApp.Services;
using System.ComponentModel;
using System.Linq;

namespace DailyPlannerApp
{
    public partial class Form1 : Form
    {
        // ── Palette ────────────────────────────────────────────────────
        static readonly Color C_BG       = Color.FromArgb(245, 246, 250);
        static readonly Color C_SIDEBAR  = Color.FromArgb(255, 255, 255);
        static readonly Color C_HEADER   = Color.FromArgb(37,  99,  235);
        static readonly Color C_ACCENT   = Color.FromArgb(59,  130, 246);
        static readonly Color C_DANGER   = Color.FromArgb(220, 38,  38);
        static readonly Color C_TEXT     = Color.FromArgb(30,  41,  59);
        static readonly Color C_SUBTEXT  = Color.FromArgb(100, 116, 139);
        static readonly Color C_BORDER   = Color.FromArgb(226, 232, 240);
        static readonly Color C_ROW_ALT  = Color.FromArgb(248, 250, 252);

        // ── Fields ─────────────────────────────────────────────────────
        TextBox          txtTitle;
        TextBox          txtDescription;
        Button           btnAdd;
        DataGridView     grid;
        BindingList<TaskItem> tasks = new BindingList<TaskItem>();
        TaskService      service   = new TaskService();
        Timer            timer;
        NotifyIcon       notify;
        DateTimePicker   dtStart;
        DateTimePicker   dtDeadline;
        Label            lblClock;
        Timer            clockTimer;

        public Form1()
        {
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize    = new Size(900, 650);

            InitUI();

            try
            {
                tasks = new BindingList<TaskItem>(service.Load());
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading tasks: {ex.Message}");
            }

            timer = new Timer { Interval = 1000 };
            timer.Tick += Timer_Tick;
            timer.Start();

            notify = new NotifyIcon { Icon = SystemIcons.Information, Visible = true };

            clockTimer = new Timer { Interval = 1000 };
            clockTimer.Tick += ClockTimer_Tick;
            clockTimer.Start();
        }

        // ═══════════════════════════════════════════════════════════════
        //  BUILD UI
        // ═══════════════════════════════════════════════════════════════
        void InitUI()
        {
            Text            = "Daily Planner";
            Width           = 900;
            Height          = 650;
            MinimumSize     = new Size(900, 650);
            BackColor       = C_BG;
            Font            = new Font("Segoe UI", 10);
            StartPosition   = FormStartPosition.CenterScreen;

            // ── Header bar ─────────────────────────────────────────────
            var header = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 56,
                BackColor = Color.DarkGreen
            };
            header.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(30, 0, 0, 0), 1);
                e.Graphics.DrawLine(p, 0, header.Height - 1, header.Width, header.Height - 1);
            };

            var lblAppName = new Label
            {
                Text      = "📋  Daily Planner",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 15, FontStyle.Bold),
                Dock      = DockStyle.Left,
                Width     = 260,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(16, 0, 0, 0)
            };

            lblClock = new Label
            {
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 11, FontStyle.Regular),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(8, 0, 0, 0)
            };

            header.Controls.Add(lblClock);
            header.Controls.Add(lblAppName);

            // ── Sidebar (left) ─────────────────────────────────────────
            var sidebar = new Panel
            {
                Dock      = DockStyle.Left,
                Width     = 240,
                BackColor = C_SIDEBAR,
                Padding   = new Padding(0)
            };
            sidebar.Paint += (s, e) =>
            {
                using var p = new Pen(C_BORDER, 1);
                e.Graphics.DrawLine(p, sidebar.Width - 1, 0, sidebar.Width - 1, sidebar.Height);
            };

            // ── Main content area ──────────────────────────────────────
            var content = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = C_BG,
                Padding   = new Padding(16)
            };

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
                AllowUserToResizeColumns    = false,
                SelectionMode               = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode         = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles   = false,
                GridColor                   = C_BORDER,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight         = 38,
                RowTemplate                 = { Height = 34 },
                CellBorderStyle             = DataGridViewCellBorderStyle.SingleHorizontal
            };

            grid.ColumnHeadersVisible      = true;
            grid.EnableHeadersVisualStyles = false;

            grid.ColumnHeadersDefaultCellStyle.BackColor  = C_HEADER;
            grid.ColumnHeadersDefaultCellStyle.ForeColor  = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font       = new Font("Segoe UI", 9, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Alignment  = DataGridViewContentAlignment.MiddleCenter;
            grid.ColumnHeadersDefaultCellStyle.Padding    = new Padding(8, 0, 0, 0);
            grid.ColumnHeadersBorderStyle                 = DataGridViewHeaderBorderStyle.Single;

            grid.DefaultCellStyle.Font               = new Font("Segoe UI", 10);
            grid.DefaultCellStyle.ForeColor          = C_TEXT;
            grid.DefaultCellStyle.BackColor          = Color.White;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 64, 175);
            grid.DefaultCellStyle.Padding            = new Padding(6, 0, 0, 0);

            grid.AlternatingRowsDefaultCellStyle.BackColor = C_ROW_ALT;

            grid.RowPrePaint                  += Grid_RowPrePaint;
            grid.CellValueChanged             += Grid_CellValueChanged;
            grid.CurrentCellDirtyStateChanged += Grid_CurrentCellDirtyStateChanged;
            grid.CellDoubleClick              += Grid_CellDoubleClick;

            content.Controls.Add(grid);
            BuildSidebar(sidebar);

            // ✅ Đúng thứ tự Dock: Fill → Left → Top
            Controls.Add(content);
            Controls.Add(sidebar);
            Controls.Add(header);
        }

        // ═══════════════════════════════════════════════════════════════
        //  SIDEBAR
        // ═══════════════════════════════════════════════════════════════
        void BuildSidebar(Panel sidebar)
        {
            int y = 16;

            sidebar.Controls.Add(SidebarSectionLabel("NEW TASK", y)); y += 28;

            sidebar.Controls.Add(SidebarFieldLabel("Title", y)); y += 20;
            txtTitle = SidebarTextBox(y, 208);
            sidebar.Controls.Add(txtTitle); y += 32;

            sidebar.Controls.Add(SidebarFieldLabel("Sub-tasks  (one per line)", y)); y += 20;
            txtDescription = new TextBox
            {
                Left        = 10,
                Top         = y,
                Width       = 208,
                Height      = 100,
                Multiline   = true,
                ScrollBars  = ScrollBars.Vertical,
                Font        = new Font("Segoe UI", 11f),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor   = Color.White,
                ForeColor   = C_TEXT
            };
            sidebar.Controls.Add(txtDescription); y += 110;

            sidebar.Controls.Add(SidebarFieldLabel("Start time", y)); y += 20;
            dtStart = SidebarDatePicker(y);
            sidebar.Controls.Add(dtStart); y += 32;

            sidebar.Controls.Add(SidebarFieldLabel("Deadline", y)); y += 20;
            dtDeadline = SidebarDatePicker(y);
            sidebar.Controls.Add(dtDeadline); y += 38;

            // ── Row 1: Add + Delete side by side ──────────────────────
            btnAdd = new Button
            {
                Left      = 12,
                Top       = y,
                Width     = 100,
                Height    = 34,
                Text      = "＋ Add",
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(239, 246, 255),
                ForeColor = Color.FromArgb(37, 99, 235),
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            btnAdd.FlatAppearance.BorderSize  = 1;
            btnAdd.FlatAppearance.BorderColor = Color.FromArgb(147, 197, 253);
            btnAdd.Click += BtnAdd_Click;
            sidebar.Controls.Add(btnAdd);

            var btnDelete = new Button
            {
                Left      = 118,
                Top       = y,
                Width     = 102,
                Height    = 34,
                Text      = "🗑 Delete",
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(254, 242, 242),
                ForeColor = C_DANGER,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            btnDelete.FlatAppearance.BorderSize  = 1;
            btnDelete.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202);
            btnDelete.Click += (s, e) =>
            {
                var task = grid.CurrentRow?.DataBoundItem as TaskItem;
                if (task != null)
                {
                    tasks.Remove(task);
                    service.Save(tasks.ToList());
                }
            };
            sidebar.Controls.Add(btnDelete); y += 42;

            // ── Row 2: Vocabulary + Budget side by side ────────────────
            var btnVocab = new Button
            {
                Left      = 12,
                Top       = y,
                Width     = 100,
                Height    = 34,
                Text      = "📖 Vocab",
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(240, 249, 255),
                ForeColor = Color.FromArgb(3, 105, 161),
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            btnVocab.FlatAppearance.BorderSize  = 1;
            btnVocab.FlatAppearance.BorderColor = Color.FromArgb(186, 230, 253);
            btnVocab.Click += (s, e) => new VocabForm().Show();
            sidebar.Controls.Add(btnVocab);

            var btnBudget = new Button
            {
                Left      = 118,
                Top       = y,
                Width     = 102,
                Height    = 34,
                Text      = "💰 Budget",
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(240, 253, 244),
                ForeColor = Color.FromArgb(22, 163, 74),
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            btnBudget.FlatAppearance.BorderSize  = 1;
            btnBudget.FlatAppearance.BorderColor = Color.FromArgb(187, 247, 208);
            btnBudget.Click += (s, e) => new BudgetForm().Show();
            sidebar.Controls.Add(btnBudget); y += 42;

            BuildLegend(sidebar);
        }

        void BuildLegend(Panel sidebar)
        {
            int y = sidebar.Height + 325;

            var divTop = new Panel { Left = 16, Top = y, Width = 208, Height = 1, BackColor = C_BORDER };
            sidebar.Controls.Add(divTop);

            sidebar.Controls.Add(SidebarFieldLabel("STATUS LEGEND", y)); y += 20;

            LegendRow(sidebar, Color.FromArgb(254, 202, 202), "Overdue",     y); y += 22;
            LegendRow(sidebar, Color.FromArgb(254, 240, 138), "Due soon",    y); y += 22;
            LegendRow(sidebar, Color.FromArgb(187, 247, 208), "Done",        y); y += 22;
            LegendRow(sidebar, Color.FromArgb(226, 232, 240), "In progress", y);
        }

        void LegendRow(Panel parent, Color dot, string text, int y)
        {
            var circle = new Label { Left = 16, Top = y + 2, Width = 12, Height = 12, BackColor = dot };
            circle.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var b = new SolidBrush(dot);
                e.Graphics.FillEllipse(b, 0, 0, 11, 11);
            };
            var lbl = new Label
            {
                Left      = 34,
                Top       = y,
                Width     = 180,
                Text      = text,
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = C_SUBTEXT
            };
            parent.Controls.Add(circle);
            parent.Controls.Add(lbl);
        }

        // ── Sidebar helpers ────────────────────────────────────────────
        Label SidebarSectionLabel(string text, int top) => new Label
        {
            Text = text, Left = 16, Top = top, Width = 208,
            Font = new Font("Segoe UI", 8, FontStyle.Bold),
            ForeColor = C_ACCENT, Height = 20
        };

        Label SidebarFieldLabel(string text, int top) => new Label
        {
            Text = text, Left = 16, Top = top, Width = 208,
            Font = new Font("Segoe UI", 10f,FontStyle.Bold),
            ForeColor = C_SUBTEXT, Height = 18
        };

        TextBox SidebarTextBox(int top, int width) => new TextBox
        {
            Left = 10, Top = top, Width = width, Height = 26,
            Font = new Font("Segoe UI", 10),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White, ForeColor = C_TEXT
        };

        DateTimePicker SidebarDatePicker(int top) => new DateTimePicker
        {
            Left = 16, Top = top, Width = 208,
            Format       = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy  HH:mm",
            ShowUpDown   = true,
            Font         = new Font("Segoe UI", 9.5f)
        };

        // ═══════════════════════════════════════════════════════════════
        //  DATA
        // ═══════════════════════════════════════════════════════════════
        void LoadData()
        {
            try
            {
                grid.AutoGenerateColumns = true;
                if (grid.DataSource == null)
                    grid.DataSource = tasks;

                System.Threading.Thread.Sleep(50);

                if (grid.Columns.Contains("IsDone"))      grid.Columns["IsDone"].HeaderText      = "✓";
                if (grid.Columns.Contains("Title"))       grid.Columns["Title"].HeaderText        = "Title";
                if (grid.Columns.Contains("Description")) grid.Columns["Description"].HeaderText  = "Sub-tasks";
                if (grid.Columns.Contains("StartTime"))   grid.Columns["StartTime"].HeaderText    = "Start";
                if (grid.Columns.Contains("Deadline"))    grid.Columns["Deadline"].HeaderText     = "Deadline";

                if (grid.Columns.Contains("StartTime")) grid.Columns["StartTime"].DefaultCellStyle.Format = "dd/MM  HH:mm";
                if (grid.Columns.Contains("Deadline"))  grid.Columns["Deadline"].DefaultCellStyle.Format  = "dd/MM  HH:mm";

                if (grid.Columns.Contains("SubTasks"))    grid.Columns["SubTasks"].Visible    = false;
                if (grid.Columns.Contains("SubTaskDone")) grid.Columns["SubTaskDone"].Visible = false;

                grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                if (grid.Columns.Contains("IsDone"))    grid.Columns["IsDone"].FillWeight    = 25;
                if (grid.Columns.Contains("Title"))     grid.Columns["Title"].FillWeight     = 100;
                if (grid.Columns.Contains("StartTime")) grid.Columns["StartTime"].FillWeight = 90;
                if (grid.Columns.Contains("Deadline"))  grid.Columns["Deadline"].FillWeight  = 90;

                foreach (DataGridViewColumn col in grid.Columns)
                    col.ReadOnly = true;

                if (grid.Columns.Contains("IsDone")) grid.Columns["IsDone"].ReadOnly = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in LoadData: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  EVENTS
        // ═══════════════════════════════════════════════════════════════
        void BtnAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text)) return;

            var subTasks = txtDescription.Text
                .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            tasks.Add(new TaskItem
            {
                IsDone      = false,
                Title       = txtTitle.Text.Trim(),
                Description = txtDescription.Text,
                StartTime   = dtStart.Value,
                Deadline    = dtDeadline.Value,
                SubTasks    = subTasks,
                SubTaskDone = subTasks.Select(_ => false).ToList()
            });

            service.Save(tasks.ToList());
            txtTitle.Clear();
            txtDescription.Clear();
        }

        void Timer_Tick(object sender, EventArgs e)
        {
            foreach (var task in tasks)
            {
                if (!task.IsDone && IsTimeToNotify(task.Deadline))
                {
                    notify.BalloonTipTitle = "⏰ Task reminder";
                    notify.BalloonTipText  = task.Title;
                    notify.ShowBalloonTip(3000);
                    grid.Refresh();
                }
            }
        }

        bool IsTimeToNotify(DateTime taskTime)
        {
            var diff = (taskTime - DateTime.Now).TotalMinutes;
            return diff <= 5 && diff > 4.9;
        }

        void ClockTimer_Tick(object sender, EventArgs e)
        {
            lblClock.Text = DateTime.Now.ToString("HH:mm:ss   —   dddd, dd/MM/yyyy");
        }

        void Grid_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            var row  = grid.Rows[e.RowIndex];
            var task = row.DataBoundItem as TaskItem;
            if (task == null) return;

            if (task.IsDone)
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(240, 253, 244);
                row.DefaultCellStyle.ForeColor = Color.FromArgb(134, 239, 172);
                row.DefaultCellStyle.Font      = new Font(grid.Font, FontStyle.Strikeout);
            }
            else if (task.Deadline < DateTime.Now)
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(255, 241, 242);
                row.DefaultCellStyle.ForeColor = C_TEXT;
                row.DefaultCellStyle.Font      = new Font(grid.Font, FontStyle.Regular);
            }
            else if ((task.Deadline - DateTime.Now).TotalHours <= 1)
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(254, 252, 232);
                row.DefaultCellStyle.ForeColor = C_TEXT;
                row.DefaultCellStyle.Font      = new Font(grid.Font, FontStyle.Regular);
            }
            else
            {
                row.DefaultCellStyle.BackColor = e.RowIndex % 2 == 0 ? Color.White : C_ROW_ALT;
                row.DefaultCellStyle.ForeColor = C_TEXT;
                row.DefaultCellStyle.Font      = new Font(grid.Font, FontStyle.Regular);
            }
        }

        void Grid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (grid.IsCurrentCellDirty)
                grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        void Grid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (grid.Columns[e.ColumnIndex].Name == "IsDone")
            {
                var task = grid.Rows[e.RowIndex].DataBoundItem as TaskItem;
                if (task != null)
                {
                    // Tick toàn bộ sub-tasks theo trạng thái IsDone
                    if (task.SubTaskDone != null && task.SubTaskDone.Count > 0)
                        for (int i = 0; i < task.SubTaskDone.Count; i++)
                            task.SubTaskDone[i] = task.IsDone;

                    service.Save(tasks.ToList());
                    grid.Refresh();
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  DETAIL POPUP (double-click)
        // ═══════════════════════════════════════════════════════════════
        void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= grid.Rows.Count) return;
            var task = grid.Rows[e.RowIndex].DataBoundItem as TaskItem;
            if (task == null) return;

            string statusText; Color statusColor; Color statusBg;
            if (task.IsDone)
            {
                statusText  = "✓  DONE";
                statusColor = Color.FromArgb(22, 163, 74);
                statusBg    = Color.FromArgb(240, 253, 244);
            }
            else if (task.Deadline < DateTime.Now)
            {
                statusText  = "⚠  OVERDUE";
                statusColor = C_DANGER;
                statusBg    = Color.FromArgb(255, 241, 242);
            }
            else if ((task.Deadline - DateTime.Now).TotalHours <= 1)
            {
                statusText  = "⏱  DUE SOON";
                statusColor = Color.FromArgb(202, 138, 4);
                statusBg    = Color.FromArgb(254, 252, 232);
            }
            else
            {
                statusText  = "▶  IN PROGRESS";
                statusColor = C_ACCENT;
                statusBg    = Color.FromArgb(239, 246, 255);
            }

            using var detailForm = new Form
            {
                Text            = "Task Details",
                Size            = new Size(540, 540),
                StartPosition   = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false,
                BackColor       = Color.White
            };

            var bar = new Panel { Dock = DockStyle.Top, Height = 6, BackColor = statusColor };
            detailForm.Controls.Add(bar);

            var badge = new Panel { Left = 24, Top = 18, Width = 140, Height = 26, BackColor = statusBg };
            var lblBadge = new Label
            {
                Text      = statusText,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = statusColor
            };
            badge.Controls.Add(lblBadge);
            badge.Paint += (s, ev) =>
            {
                using var p = new Pen(statusColor, 1);
                ev.Graphics.DrawRectangle(p, 0, 0, badge.Width - 1, badge.Height - 1);
            };
            detailForm.Controls.Add(badge);

            var lblTitle = new Label
            {
                Text         = task.Title,
                Font         = new Font("Segoe UI", 17, FontStyle.Bold),
                ForeColor    = C_TEXT,
                Left         = 24,
                Top          = 52,
                Width        = 490,
                Height       = 42,
                AutoEllipsis = true
            };
            detailForm.Controls.Add(lblTitle);

            var divLine = new Panel { Left = 24, Top = 96, Width = 490, Height = 1, BackColor = C_BORDER };
            detailForm.Controls.Add(divLine);

            var lblSubTitle = new Label
            {
                Text      = "SUB-TASKS",
                Font      = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = C_ACCENT,
                Left      = 24,
                Top       = 106,
                Width     = 490,
                Height    = 18
            };
            detailForm.Controls.Add(lblSubTitle);

            var checklist = new CheckedListBox
            {
                Left         = 24,
                Top          = 128,
                Width        = 490,
                Height       = 160,
                Font         = new Font("Segoe UI", 10),
                BorderStyle  = BorderStyle.FixedSingle,
                BackColor    = Color.FromArgb(248, 250, 252),
                ForeColor    = C_TEXT,
                CheckOnClick = true
            };
            if (task.SubTasks != null && task.SubTasks.Count > 0)
            {
                if (task.SubTaskDone == null || task.SubTaskDone.Count != task.SubTasks.Count)
                    task.SubTaskDone = task.SubTasks.Select(_ => false).ToList();
                for (int i = 0; i < task.SubTasks.Count; i++)
                    checklist.Items.Add(task.SubTasks[i], task.SubTaskDone[i]);
            }
            else
                checklist.Items.Add("(No sub-tasks)", false);

            checklist.ItemCheck += (s, ev) =>
            {
                if (task.SubTaskDone == null || ev.Index >= task.SubTaskDone.Count) return;
                task.SubTaskDone[ev.Index] = (ev.NewValue == CheckState.Checked);
                task.IsDone = task.SubTaskDone.All(x => x);
                service.Save(tasks.ToList());
                grid.Refresh();
                grid.InvalidateRow(e.RowIndex);
            };
            detailForm.Controls.Add(checklist);

            var btnEdit = new Button
            {
                Text      = "✏  Edit Sub-tasks",
                Left      = 24,
                Top       = 298,
                Width     = 150,
                Height    = 32,
                Font      = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(255, 247, 237),
                ForeColor = Color.FromArgb(194, 65, 12),
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            btnEdit.FlatAppearance.BorderSize  = 1;
            btnEdit.FlatAppearance.BorderColor = Color.FromArgb(254, 215, 170);
            btnEdit.Click += (s, ev) =>
            {
                using var editForm = new Form
                {
                    Text            = "Edit Sub-tasks",
                    Size            = new Size(500, 340),
                    StartPosition   = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    BackColor       = Color.White
                };
                var txtEdit = new TextBox
                {
                    Left        = 20,
                    Top         = 20,
                    Width       = 450,
                    Height      = 200,
                    Multiline   = true,
                    ScrollBars  = ScrollBars.Vertical,
                    Font        = new Font("Segoe UI", 10),
                    BorderStyle = BorderStyle.FixedSingle,
                    Text        = task.Description ?? ""
                };
                var btnSave2      = MakePopupButton("💾  Save",   Color.FromArgb(22, 163, 74),   236, 290, 130);
                var btnCancelEdit = MakePopupButton("✖  Cancel", Color.FromArgb(107, 114, 128), 140, 290, 100);

                btnSave2.Click += (ss, eee) =>
                {
                    string newText = txtEdit.Text.Trim();
                    var newSubs = newText
                        .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToList();

                    task.Description = newText;
                    task.SubTasks    = newSubs;
                    task.SubTaskDone = newSubs.Select(_ => false).ToList();
                    task.IsDone      = false;
                    service.Save(tasks.ToList());
                    grid.Refresh();

                    checklist.Items.Clear();
                    foreach (var t in newSubs) checklist.Items.Add(t, false);
                    if (newSubs.Count == 0) checklist.Items.Add("(No sub-tasks)", false);

                    editForm.Close();
                };
                btnCancelEdit.Click += (ss, eee) => editForm.Close();
                editForm.Controls.AddRange(new Control[] { txtEdit, btnSave2, btnCancelEdit });
                editForm.ShowDialog();
            };
            detailForm.Controls.Add(btnEdit);

            var infoPanel = new Panel
            {
                Left      = 24,
                Top       = 342,
                Width     = 490,
                Height    = 56,
                BackColor = Color.FromArgb(248, 250, 252)
            };
            infoPanel.Paint += (s, ev) =>
            {
                using var p = new Pen(C_BORDER);
                ev.Graphics.DrawRectangle(p, 0, 0, infoPanel.Width - 1, infoPanel.Height - 1);
            };
            var lblStartInfo = new Label
            {
                Text      = $"🟢  Start:     {task.StartTime:dd/MM/yyyy  HH:mm}",
                Left      = 12,
                Top       = 8,
                Width     = 460,
                Font      = new Font("Segoe UI", 10),
                ForeColor = C_TEXT
            };
            var lblDeadlineInfo = new Label
            {
                Text      = $"🔴  Deadline:  {task.Deadline:dd/MM/yyyy  HH:mm}",
                Left      = 12,
                Top       = 30,
                Width     = 460,
                Font      = new Font("Segoe UI", 10),
                ForeColor = C_TEXT
            };
            infoPanel.Controls.Add(lblStartInfo);
            infoPanel.Controls.Add(lblDeadlineInfo);
            detailForm.Controls.Add(infoPanel);

            var btnClose = MakePopupButton("✖  Close", C_ACCENT, 390, 410, 120);
            btnClose.Click += (s, ev) => detailForm.Close();
            detailForm.Controls.Add(btnClose);

            detailForm.ShowDialog();
            grid.Rows[e.RowIndex].DefaultCellStyle.Font = null;
        }

        Button MakePopupButton(string text, Color color, int left, int top, int width) =>
            new Button
            {
                Text      = text,
                Left      = left,
                Top       = top,
                Width     = width,
                Height    = 36,
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
    }
}
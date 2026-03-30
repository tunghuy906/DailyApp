using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using DailyPlannerApp.Models;
using DailyPlannerApp.Services;
using System.ComponentModel;
using System.Linq;
namespace DailyPlannerApp
{
    public partial class Form1 : Form
    {
        TextBox txtTitle;
        TextBox txtDescription;
        Button btnAdd;
        DataGridView grid;
        BindingList<TaskItem> tasks = new BindingList<TaskItem>();
        TaskService service = new TaskService();
        Timer timer;
        NotifyIcon notify;
        DateTimePicker dtStart;
        DateTimePicker dtDeadline;
        Label lblClock;
        Timer clockTimer;
        public Form1()
        {
            InitUI();

            tasks = new BindingList<TaskItem>(service.Load());
            LoadData();
            timer = new Timer();
            timer.Interval = 1000; // check mỗi 1 giây
            timer.Tick += Timer_Tick;
            timer.Start();
            notify = new NotifyIcon();
            notify.Icon = SystemIcons.Information;
            notify.Visible = true;
            clockTimer = new Timer();
            clockTimer.Interval = 1000; // 1 giây
            clockTimer.Tick += ClockTimer_Tick;
            clockTimer.Start();
        }

        void InitUI()
        {
            this.Text = "Daily Planner";
            this.Width = 810;
            this.Height = 500;

            txtTitle = new TextBox() { Left = 20, Top = 60, Width = 200 };
            txtDescription = new TextBox() { Left = 20, Top = 100, Width = 200, Height = 150, Multiline = true };
            btnAdd = new Button() { Left = 20, Top = 350, Width = 200, Height = 30, Text = "Add" };
            btnAdd.Click += BtnAdd_Click;
            grid = new DataGridView()
            {
                Left = 250,
                Top = 60,
                Width = 500,
                Height = 360,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            Button btnDelete = new Button() { Left = 20, Top = 390, Width = 200, Height = 30, Text = "Delete" };
            btnDelete.Click += (s, e) =>
                {
                    var task = grid.CurrentRow?.DataBoundItem as TaskItem;

                    if (task != null)
                    {
                        tasks.Remove(task);
                        service.Save(tasks.ToList());
                    }
                };
            dtStart = new DateTimePicker()
            {
                Left = 20,
                Top = 270,
                Width = 200,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy HH:mm",
                ShowUpDown = true
            };

            dtDeadline = new DateTimePicker()
            {
                Left = 20,
                Top = 310,
                Width = 200,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy HH:mm",
                ShowUpDown = true
            };
            lblClock = new Label()
            {
                Dock = DockStyle.Top,
                Height = 60,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30)
            };

            this.Controls.Add(lblClock);
            this.Controls.SetChildIndex(lblClock, 0);
            this.Controls.Add(btnDelete);
            this.Controls.Add(txtTitle);
            this.Controls.Add(txtDescription);
            this.Controls.Add(dtStart);
            this.Controls.Add(dtDeadline);
            this.Controls.Add(btnAdd);
            this.Controls.Add(grid);

            //grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AllowUserToResizeRows = false;
            grid.AllowUserToResizeColumns = false;
            grid.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.CellDoubleClick += Grid_CellDoubleClick;

            txtTitle.BorderStyle = BorderStyle.FixedSingle;
            txtDescription.BorderStyle = BorderStyle.FixedSingle;

            txtTitle.Font = new Font("Segoe UI", 10);
            txtDescription.Font = new Font("Segoe UI", 10);
            btnAdd.BackColor = Color.FromArgb(0, 120, 215);
            btnAdd.ForeColor = Color.White;
            btnAdd.FlatStyle = FlatStyle.Flat;

            btnAdd.FlatAppearance.BorderSize = 0;
            btnDelete.BackColor = Color.FromArgb(200, 50, 50);
            btnDelete.ForeColor = Color.White;
            btnDelete.FlatStyle = FlatStyle.Flat;
            btnDelete.FlatAppearance.BorderSize = 0;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.None;
            grid.EnableHeadersVisualStyles = false;

            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.Gray;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);

            grid.DefaultCellStyle.Font = new Font("Segoe UI", 10);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.RowPrePaint += Grid_RowPrePaint;
            grid.Refresh();
            grid.CellValueChanged += Grid_CellValueChanged;
            grid.CurrentCellDirtyStateChanged += Grid_CurrentCellDirtyStateChanged;


            grid.RowHeadersVisible = false;
        }

        void BtnAdd_Click(object sender, EventArgs e)
        {
            var subTasks = txtDescription.Text
                .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            var task = new TaskItem()
            {
                IsDone = false,
                Title = txtTitle.Text,
                Description = txtDescription.Text,
                StartTime = dtStart.Value,
                Deadline = dtDeadline.Value,
                SubTasks = subTasks,

                // 🔥 GÁN Ở ĐÂY
                SubTaskDone = subTasks.Select(x => false).ToList()
            };

            tasks.Add(task);
            service.Save(tasks.ToList());
        }

        void LoadData()
        {
            if (grid.DataSource == null)
            {
                grid.DataSource = tasks;
            }

            // 🔥 Đổi tên cột cho ngắn gọn
            grid.Columns["IsDone"].HeaderText = "Status";
            grid.Columns["Title"].HeaderText = "Title";
            grid.Columns["Description"].HeaderText = "Tasks";
            grid.Columns["StartTime"].HeaderText = "Start";
            grid.Columns["Deadline"].HeaderText = "Deadline";

            // 🔥 Format thời gian
            grid.Columns["StartTime"].DefaultCellStyle.Format = "dd/MM HH:mm";
            grid.Columns["Deadline"].DefaultCellStyle.Format = "dd/MM HH:mm";

            // 🔥 Auto fit đẹp
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            foreach (DataGridViewColumn col in grid.Columns)
            {
                col.ReadOnly = true; // khóa tất cả
            }

            // 🔥 MỞ RIÊNG checkbox
            grid.Columns["IsDone"].ReadOnly = false;
        }
        void Timer_Tick(object sender, EventArgs e)
        {
            foreach (var task in tasks)
            {
                if (!task.IsDone && IsTimeToNotify(task.Deadline))
                {
                    notify.BalloonTipTitle = "Nhắc việc";
                    notify.BalloonTipText = task.Title;
                    notify.ShowBalloonTip(3000);

                    task.IsDone = true;
                    service.Save(tasks.ToList());
                    grid.Refresh();
                }
            }
        }
        bool IsTimeToNotify(DateTime taskTime)
        {
            DateTime now = DateTime.Now;

            return (taskTime - now).TotalMinutes <= 5 &&
                   (taskTime - now).TotalMinutes > 4.9;
        }
        void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= grid.Rows.Count) return;

            var task = grid.Rows[e.RowIndex].DataBoundItem as TaskItem;
            if (task == null) return;

            // Xác định trạng thái và màu sắc
            string statusText = "";
            Color statusColor = Color.Gray;

            if (task.IsDone)
            {
                statusText = "DONE";
                statusColor = Color.FromArgb(46, 204, 113);
            }
            else if (task.Deadline < DateTime.Now)
            {
                statusText = "OVERDUE";
                statusColor = Color.FromArgb(231, 76, 60);
            }
            else if ((task.Deadline - DateTime.Now).TotalHours <= 1)
            {
                statusText = "DUE SOON";
                statusColor = Color.FromArgb(241, 196, 15);
            }
            else
            {
                statusText = "DOING";
                statusColor = Color.FromArgb(52, 152, 219);
            }

            using (var detailForm = new Form())
            {
                detailForm.Text = "Task Details";
                detailForm.Size = new Size(560, 560);
                detailForm.StartPosition = FormStartPosition.CenterParent;
                detailForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                detailForm.MaximizeBox = false;
                detailForm.MinimizeBox = false;
                detailForm.BackColor = Color.White;

                var topBar = new Panel { Height = 8, Dock = DockStyle.Top, BackColor = statusColor };
                detailForm.Controls.Add(topBar);

                var lblTitle = new Label
                {
                    Text = task.Title,
                    Font = new Font("Segoe UI", 18, FontStyle.Bold),
                    ForeColor = Color.FromArgb(30, 30, 30),
                    Left = 25,
                    Top = 25,
                    Width = 500,
                    Height = 50
                };

                var lblDescTitle = new Label
                {
                    Text = "Task:",
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    ForeColor = Color.FromArgb(70, 70, 70),
                    Left = 18,
                    Top = 80,
                    Width = 500
                };

                // CheckedListBox
                CheckedListBox checklist = new CheckedListBox
                {
                    Left = 25,
                    Top = 110,
                    Width = 500,
                    Height = 170,
                    Font = new Font("Segoe UI", 10),
                    BorderStyle = BorderStyle.FixedSingle
                };

                if (task.SubTasks != null && task.SubTasks.Count > 0)
                {
                    if (task.SubTaskDone == null || task.SubTaskDone.Count != task.SubTasks.Count)
                        task.SubTaskDone = task.SubTasks.Select(_ => false).ToList();

                    for (int i = 0; i < task.SubTasks.Count; i++)
                    {
                        checklist.Items.Add($"{i + 1}. {task.SubTasks[i]}", task.SubTaskDone[i]);
                    }
                }
                else
                {
                    checklist.Items.Add("(Không có mô tả)", false);
                }

                checklist.ItemCheck += (s, ev) =>
                {
                    if (task.SubTaskDone == null || ev.Index >= task.SubTaskDone.Count) return;

                    task.SubTaskDone[ev.Index] = (ev.NewValue == CheckState.Checked);

                    // Cập nhật IsDone
                    bool allDone = task.SubTaskDone.All(x => x);
                    task.IsDone = allDone;

                    service.Save(tasks.ToList());

                    // 🔥 Fix quan trọng: Refresh mạnh để bỏ gạch ngang ngay lập tức
                    grid.Refresh();
                    grid.InvalidateRow(e.RowIndex);   // Thêm dòng này
                };

                // Nút Sửa mô tả
                Button btnEditDescription = new Button
                {
                    Text = "✏️ Edit Tasks",
                    Left = 25,
                    Top = 290,
                    Width = 160,
                    Height = 38,
                    BackColor = Color.Orange,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                };
                btnEditDescription.FlatAppearance.BorderSize = 0;

                btnEditDescription.Click += (s, ev) =>
                {
                    using (var editForm = new Form())
                    {
                        editForm.Text = "Task Correction Table";
                        editForm.Size = new Size(520, 380);
                        editForm.StartPosition = FormStartPosition.CenterParent;
                        editForm.FormBorderStyle = FormBorderStyle.FixedDialog;

                        var txtEdit = new TextBox
                        {
                            Multiline = true,
                            ScrollBars = ScrollBars.Vertical,
                            Font = new Font("Segoe UI", 10),
                            Left = 20,
                            Top = 20,
                            Width = 470,
                            Height = 250,
                            Text = task.Description ?? ""
                        };

                        var btnSave = new Button
                        {
                            Text = "Save",
                            Left = 290,
                            Top = 285,
                            Width = 110,
                            Height = 40,
                            BackColor = Color.YellowGreen,
                            ForeColor = Color.White,
                            FlatStyle = FlatStyle.Flat
                        };

                        var btnCancel = new Button { Text = "Cancel", Left = 170, Top = 285, Width = 100, Height = 40 };

                        btnSave.Click += (ss, eee) =>
                        {
                            string newText = txtEdit.Text.Trim();
                            var newSubTasks = newText.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                                                    .Select(x => x.Trim())
                                                    .Where(x => !string.IsNullOrWhiteSpace(x))
                                                    .ToList();

                            task.Description = newText;
                            task.SubTasks = newSubTasks;
                            task.SubTaskDone = newSubTasks.Select(_ => false).ToList();
                            task.IsDone = false; 

                            service.Save(tasks.ToList());
                            grid.Refresh();

                            MessageBox.Show("New task saved!", "Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            editForm.Close();
                        };

                        btnCancel.Click += (ss, eee) => editForm.Close();

                        editForm.Controls.Add(txtEdit);
                        editForm.Controls.Add(btnSave);
                        editForm.Controls.Add(btnCancel);
                        editForm.ShowDialog();
                    }

                    // Refresh checklist sau khi sửa
                    checklist.Items.Clear();
                    if (task.SubTasks?.Count > 0)
                    {
                        for (int i = 0; i < task.SubTasks.Count; i++)
                            checklist.Items.Add(task.SubTasks[i], task.SubTaskDone[i]);
                    }
                    else
                        checklist.Items.Add("(No sub-tasks available)", false);
                };

                // Các label thời gian và status (điều chỉnh Top cho phù hợp)
                var lblStart = new Label { Text = $"Start:   {task.StartTime:dd/MM/yyyy HH:mm}", Left = 25, Top = 340, Width = 500, Font = new Font("Segoe UI", 10.5f) };
                var lblDeadline = new Label { Text = $"Deadline:  {task.Deadline:dd/MM/yyyy HH:mm}", Left = 25, Top = 365, Width = 500, Font = new Font("Segoe UI", 10.5f) };

                var statusPanel = new Panel { Left = 25, Top = 395, Width = 500, Height = 40 };
                var statusDot = new Label { Text = "●", Font = new Font("Segoe UI", 19, FontStyle.Bold), ForeColor = statusColor, Left = 5, Top = -6, Width = 34, Height = 34 };
                var lblStatus = new Label { Text = statusText, Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = statusColor, Left = 35, Top = 8, Width = 450 };
                statusPanel.Controls.Add(statusDot);
                statusPanel.Controls.Add(lblStatus);

                var btnClose = new Button 
                {
                    Text = "Exit",
                    Width = 130,
                    Height = 42,
                    Left = 400,
                    Top = 445,
                    BackColor = Color.FromArgb(0, 122, 204),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                };
                btnClose.Click += (s, ev) => detailForm.Close();

                // Add controls
                detailForm.Controls.Add(lblTitle);
                detailForm.Controls.Add(lblDescTitle);
                detailForm.Controls.Add(checklist);
                detailForm.Controls.Add(btnEditDescription);
                detailForm.Controls.Add(lblStart);
                detailForm.Controls.Add(lblDeadline);
                detailForm.Controls.Add(statusPanel);
                detailForm.Controls.Add(btnClose);

                detailForm.ShowDialog();
                grid.Rows[e.RowIndex].DefaultCellStyle.Font = null; // reset font trước
            }
        }
        void ClockTimer_Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;

            lblClock.Text = now.ToString("HH:mm:ss  -  dd/MM/yyyy");
            lblClock.ForeColor = Color.Green;
        }
        void Grid_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            var row = grid.Rows[e.RowIndex];
            var task = row.DataBoundItem as TaskItem;

            if (task == null) return;

            // 🔴 Quá hạn
            if (!task.IsDone && task.Deadline < DateTime.Now)
            {
                row.DefaultCellStyle.BackColor = Color.LightCoral;
            }
            // 🟡 Sắp đến (trong 1 giờ)
            else if (!task.IsDone && (task.Deadline - DateTime.Now).TotalHours <= 1)
            {
                row.DefaultCellStyle.BackColor = Color.Khaki;
            }
            // 🟢 Đã hoàn thành
            else if (task.IsDone)
            {
                row.DefaultCellStyle.BackColor = Color.LightGreen;
                row.DefaultCellStyle.Font = new Font(grid.Font, FontStyle.Strikeout);
            }
            else
            {
                row.DefaultCellStyle.BackColor = Color.White;
                row.DefaultCellStyle.Font = new Font(grid.Font, FontStyle.Regular);
            }
        }
        void Grid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (grid.IsCurrentCellDirty)
            {
                grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }
        void Grid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (grid.Columns[e.ColumnIndex].Name == "IsDone")
            {
                var task = grid.Rows[e.RowIndex].DataBoundItem as TaskItem;

                if (task != null)
                {
                    service.Save(tasks.ToList());
                }
            }
        }
    }
}
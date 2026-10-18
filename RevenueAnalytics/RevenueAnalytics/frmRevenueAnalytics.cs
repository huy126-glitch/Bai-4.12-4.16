
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HospitalMDI
{
    public class frmRevenueAnalytics : Form
    {
        private DateTimePicker dtpFromDate;
        private DateTimePicker dtpToDate;
        private ComboBox cboBranch;
        private Button btnAnalyze;
        private Button btnCancel;
        private ProgressBar pgbLoading;
        private Label lblProcessingStatus;
        private DataGridView dgvRevenueData;

        private CancellationTokenSource cts;
        private bool isRunning = false;

        public frmRevenueAnalytics()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            Text = "Bảng điều khiển thống kê doanh thu";
            Size = new Size(1050, 650);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(800, 500);

            Label lblTitle = new Label
            {
                Text = "THỐNG KÊ DOANH THU",
                Font = new Font("Arial", 18, FontStyle.Bold),
                Location = new Point(20, 15),
                AutoSize = true
            };

            Label lblFrom = new Label
            {
                Text = "Từ ngày:",
                Location = new Point(20, 70),
                AutoSize = true
            };

            dtpFromDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Location = new Point(90, 65),
                Width = 130
            };

            Label lblTo = new Label
            {
                Text = "Đến ngày:",
                Location = new Point(240, 70),
                AutoSize = true
            };

            dtpToDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Location = new Point(315, 65),
                Width = 130,
                Value = DateTime.Today
            };

            dtpFromDate.Value = DateTime.Today.AddDays(-30);

            Label lblBranch = new Label
            {
                Text = "Chi nhánh:",
                Location = new Point(465, 70),
                AutoSize = true
            };

            cboBranch = new ComboBox
            {
                Location = new Point(540, 65),
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            cboBranch.Items.AddRange(new object[]
            {
                "Tất cả chi nhánh",
                "Chi nhánh Hà Nội",
                "Chi nhánh TP.HCM",
                "Chi nhánh Đà Nẵng"
            });
            cboBranch.SelectedIndex = 0;

            btnAnalyze = new Button
            {
                Text = "Thống Kê Doanh Thu",
                Location = new Point(710, 62),
                Size = new Size(160, 32)
            };
            btnAnalyze.Click += btnAnalyze_Click;

            btnCancel = new Button
            {
                Text = "Hủy Thao Tác",
                Location = new Point(880, 62),
                Size = new Size(120, 32),
                Enabled = false
            };
            btnCancel.Click += btnCancel_Click;

            pgbLoading = new ProgressBar
            {
                Location = new Point(20, 115),
                Size = new Size(980, 22),
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Anchor = AnchorStyles.Top | AnchorStyles.Left |
                         AnchorStyles.Right
            };

            lblProcessingStatus = new Label
            {
                Text = "Sẵn sàng thống kê.",
                Location = new Point(20, 145),
                AutoSize = true
            };

            dgvRevenueData = new DataGridView
            {
                Location = new Point(20, 180),
                Size = new Size(980, 400),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                         AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode =
                    DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode =
                    DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };

            dgvRevenueData.Columns.Add("TransactionId", "Mã giao dịch");
            dgvRevenueData.Columns.Add("Date", "Ngày giao dịch");
            dgvRevenueData.Columns.Add("Branch", "Chi nhánh");
            dgvRevenueData.Columns.Add("Description", "Nội dung");
            dgvRevenueData.Columns.Add("Amount", "Doanh thu (VNĐ)");

            Controls.AddRange(new Control[]
            {
                lblTitle, lblFrom, dtpFromDate,
                lblTo, dtpToDate, lblBranch,
                cboBranch, btnAnalyze, btnCancel,
                pgbLoading, lblProcessingStatus, dgvRevenueData
            });

            FormClosing += frmRevenueAnalytics_FormClosing;
        }

        private async void btnAnalyze_Click(object sender, EventArgs e)
        {
            if (isRunning)
                return;

            DateTime fromDate = dtpFromDate.Value.Date;
            DateTime toDate = dtpToDate.Value.Date;
            string branch = cboBranch.SelectedItem?.ToString()
                            ?? "Tất cả chi nhánh";

            if (fromDate > toDate)
            {
                MessageBox.Show(
                    "Ngày bắt đầu không được lớn hơn ngày kết thúc!",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;

            isRunning = true;
            btnAnalyze.Enabled = false;
            btnCancel.Enabled = true;
            pgbLoading.Value = 0;
            lblProcessingStatus.Text = "Đang chuẩn bị thống kê...";
            dgvRevenueData.Rows.Clear();

            // Bộ đếm tiến độ cập nhật từ UI Thread.
            using (System.Windows.Forms.Timer progressTimer =
                   new System.Windows.Forms.Timer())
            {
                progressTimer.Interval = 100;
                progressTimer.Tick += (s, args) =>
                {
                    if (isRunning && pgbLoading.Value < 95)
                    {
                        pgbLoading.Value =
                            Math.Min(95, pgbLoading.Value + 2);

                        lblProcessingStatus.Text =
                            $"Đang xử lý dữ liệu... {pgbLoading.Value}%";
                    }
                };

                progressTimer.Start();

                try
                {
                    // Công việc nặng chạy ở Background Thread.
                    List<RevenueTransaction> results =
                        await Task.Run(async () =>
                        {
                            var list = new List<RevenueTransaction>();

                            // Giả lập truy xuất và tổng hợp dữ liệu
                            // trong khoảng 4–6 giây.
                            int totalSteps = 50;

                            for (int i = 1; i <= totalSteps; i++)
                            {
                                token.ThrowIfCancellationRequested();

                                await Task.Delay(100, token);

                                // Giả lập dữ liệu giao dịch.
                                // Trong ứng dụng thật, thay bằng truy vấn CSDL.
                                if (i % 2 == 0)
                                {
                                    DateTime date = fromDate.AddDays(
                                        (i - 1) % ((toDate - fromDate).Days + 1));

                                    string transactionBranch =
                                        i % 3 == 0
                                            ? "Chi nhánh Hà Nội"
                                            : i % 3 == 1
                                                ? "Chi nhánh TP.HCM"
                                                : "Chi nhánh Đà Nẵng";

                                    if (branch == "Tất cả chi nhánh" ||
                                        branch == transactionBranch)
                                    {
                                        list.Add(new RevenueTransaction
                                        {
                                            TransactionId = "GD" +
                                                i.ToString("D4"),
                                            Date = date,
                                            Branch = transactionBranch,
                                            Description =
                                                "Giao dịch bán hàng",
                                            Amount = 500000 +
                                                (i * 125000)
                                        });
                                    }
                                }
                            }

                            token.ThrowIfCancellationRequested();
                            return list;
                        }, token);

                    pgbLoading.Value = 100;
                    lblProcessingStatus.Text =
                        $"Hoàn tất! Tìm thấy {results.Count} giao dịch.";

                    foreach (var item in results)
                    {
                        dgvRevenueData.Rows.Add(
                            item.TransactionId,
                            item.Date.ToString("dd/MM/yyyy"),
                            item.Branch,
                            item.Description,
                            item.Amount.ToString("N0"));
                    }

                    if (results.Count > 0)
                    {
                        decimal totalRevenue =
                            results.Sum(x => x.Amount);

                        lblProcessingStatus.Text +=
                            $" Tổng doanh thu: {totalRevenue:N0} VNĐ.";
                    }
                }
                catch (OperationCanceledException)
                {
                    pgbLoading.Value = 0;
                    lblProcessingStatus.Text = "Đã hủy thao tác!";
                    MessageBox.Show(
                        "Đã hủy thao tác!",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    lblProcessingStatus.Text = "Có lỗi xảy ra.";
                    MessageBox.Show(
                        "Lỗi: " + ex.Message,
                        "Lỗi",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                finally
                {
                    progressTimer.Stop();
                    isRunning = false;
                    btnAnalyze.Enabled = true;
                    btnCancel.Enabled = false;

                    cts?.Dispose();
                    cts = null;
                }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (isRunning && cts != null)
            {
                cts.Cancel();
                btnCancel.Enabled = false;
                lblProcessingStatus.Text = "Đang hủy thao tác...";
            }
        }

        private void frmRevenueAnalytics_FormClosing(
            object sender, FormClosingEventArgs e)
        {
            if (isRunning && cts != null)
            {
                cts.Cancel();
            }
        }
    }

    public class RevenueTransaction
    {
        public string TransactionId { get; set; }
        public DateTime Date { get; set; }
        public string Branch { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
    }
}

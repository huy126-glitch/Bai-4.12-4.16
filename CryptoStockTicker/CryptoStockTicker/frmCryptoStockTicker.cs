
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HospitalMDI
{
    public class frmCryptoStockTicker : Form
    {
        private Button btnStart;
        private Button btnPause;
        private Label lblBTCPrice;
        private Label lblTitle;
        private DataGridView dgvHistoryLog;

        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblLastUpdate;
        private ToolStripStatusLabel lblSyncCount;

        private CancellationTokenSource cts;
        private bool isMonitoring = false;

        private decimal currentPrice = 65000m;
        private int syncCount = 0;
        private readonly Random random = new Random();

        public frmCryptoStockTicker()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            Text = "Giám sát tỷ giá tài chính thời gian thực";
            Size = new Size(950, 600);
            MinimumSize = new Size(750, 450);
            StartPosition = FormStartPosition.CenterScreen;

            lblTitle = new Label
            {
                Text = "GIÁM SÁT TỶ GIÁ BTC/USD",
                Font = new Font("Arial", 18, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 20)
            };

            btnStart = new Button
            {
                Text = "Bắt đầu Giám sát",
                Location = new Point(20, 70),
                Size = new Size(160, 35)
            };
            btnStart.Click += btnStart_Click;

            btnPause = new Button
            {
                Text = "Tạm dừng",
                Location = new Point(195, 70),
                Size = new Size(120, 35),
                Enabled = false
            };
            btnPause.Click += btnPause_Click;

            lblBTCPrice = new Label
            {
                Text = $"BTC/USD: {currentPrice:N2} USD",
                Font = new Font("Arial", 24, FontStyle.Bold),
                ForeColor = Color.Green,
                AutoSize = true,
                Location = new Point(20, 125)
            };

            dgvHistoryLog = new DataGridView
            {
                Location = new Point(20, 190),
                Size = new Size(890, 300),
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

            dgvHistoryLog.Columns.Add("Time", "Thời gian");
            dgvHistoryLog.Columns.Add("OldPrice", "Giá trước (USD)");
            dgvHistoryLog.Columns.Add("NewPrice", "Giá mới (USD)");
            dgvHistoryLog.Columns.Add("Change", "Biến động");
            dgvHistoryLog.Columns.Add("Status", "Trạng thái");

            statusStrip = new StatusStrip();

            lblLastUpdate = new ToolStripStatusLabel
            {
                Text = "Cập nhật cuối: Chưa có"
            };

            lblSyncCount = new ToolStripStatusLabel
            {
                Text = "Số lượt đồng bộ: 0",
                Spring = true,
                TextAlign = ContentAlignment.MiddleRight
            };

            statusStrip.Items.Add(lblLastUpdate);
            statusStrip.Items.Add(
                new ToolStripStatusLabel(" | "));
            statusStrip.Items.Add(lblSyncCount);

            Controls.Add(lblTitle);
            Controls.Add(btnStart);
            Controls.Add(btnPause);
            Controls.Add(lblBTCPrice);
            Controls.Add(dgvHistoryLog);
            Controls.Add(statusStrip);

            FormClosing += frmCryptoStockTicker_FormClosing;
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            if (isMonitoring)
                return;

            isMonitoring = true;
            btnStart.Enabled = false;
            btnPause.Enabled = true;

            cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;

            lblLastUpdate.Text = "Đang khởi động...";

            try
            {
                await Task.Run(async () =>
                {
                    while (true)
                    {
                        token.ThrowIfCancellationRequested();

                        // Chờ 2 giây giữa các lần cập nhật.
                        await Task.Delay(2000, token);

                        token.ThrowIfCancellationRequested();

                        // Sinh tỷ giá giả lập trên thread nền.
                        decimal oldPrice = currentPrice;

                        decimal change =
                            (decimal)(random.NextDouble() * 1000 - 500);

                        decimal newPrice =
                            Math.Max(1m, oldPrice + change);

                        currentPrice = newPrice;

                        DateTime updateTime = DateTime.Now;
                        decimal difference = newPrice - oldPrice;

                        string changeText =
                            difference > 0 ? "Tăng" :
                            difference < 0 ? "Giảm" :
                            "Không đổi";

                        // Chuyển việc cập nhật giao diện về UI Thread.
                        UpdateUI(
                            oldPrice,
                            newPrice,
                            difference,
                            changeText,
                            updateTime
                        );
                    }
                }, token);
            }
            catch (OperationCanceledException)
            {
                // Đã yêu cầu dừng vòng lặp.
            }
            catch (Exception ex)
            {
                if (!IsDisposed && IsHandleCreated)
                {
                    MessageBox.Show(
                        "Lỗi giám sát: " + ex.Message,
                        "Lỗi",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
            finally
            {
                isMonitoring = false;

                if (!IsDisposed && IsHandleCreated)
                {
                    btnStart.Enabled = true;
                    btnPause.Enabled = false;
                    lblLastUpdate.Text += " (Đã dừng)";
                }

                if (cts != null)
                {
                    cts.Dispose();
                    cts = null;
                }
            }
        }

        private void UpdateUI(
            decimal oldPrice,
            decimal newPrice,
            decimal difference,
            string changeText,
            DateTime updateTime)
        {
            // Kiểm tra có đang chạy trên thread phụ hay không.
            if (this.InvokeRequired)
            {
                if (IsDisposed || !IsHandleCreated)
                    return;

                try
                {
                    this.Invoke(new Action(() =>
                    {
                        UpdateUI(
                            oldPrice,
                            newPrice,
                            difference,
                            changeText,
                            updateTime
                        );
                    }));
                }
                catch (ObjectDisposedException)
                {
                    // Form đã bị đóng hoặc hủy.
                }
                catch (InvalidOperationException)
                {
                    // Không thể Invoke khi Form đang đóng.
                }

                return;
            }

            // Từ đây trở xuống chỉ chạy trên UI Thread.
            if (IsDisposed)
                return;

            lblBTCPrice.Text =
                $"BTC/USD: {newPrice:N2} USD";

            if (difference > 0)
                lblBTCPrice.ForeColor = Color.Green;
            else if (difference < 0)
                lblBTCPrice.ForeColor = Color.Red;
            else
                lblBTCPrice.ForeColor = Color.Gray;

            syncCount++;

            lblLastUpdate.Text =
                $"Cập nhật cuối: {updateTime:HH:mm:ss}";

            lblSyncCount.Text =
                $"Số lượt đồng bộ: {syncCount}";

            dgvHistoryLog.Rows.Insert(
                0,
                updateTime.ToString("HH:mm:ss"),
                oldPrice.ToString("N2"),
                newPrice.ToString("N2"),
                (difference >= 0 ? "+" : "") +
                    difference.ToString("N2"),
                changeText
            );

            // Giới hạn 200 dòng lịch sử gần nhất.
            while (dgvHistoryLog.Rows.Count > 200)
            {
                dgvHistoryLog.Rows.RemoveAt(
                    dgvHistoryLog.Rows.Count - 1
                );
            }

            dgvHistoryLog.Rows[0].DefaultCellStyle.ForeColor =
                difference > 0 ? Color.Green :
                difference < 0 ? Color.Red :
                Color.Gray;
        }

        private void btnPause_Click(object sender, EventArgs e)
        {
            if (isMonitoring && cts != null)
            {
                btnPause.Enabled = false;
                lblLastUpdate.Text = "Đang tạm dừng...";
                cts.Cancel();
            }
        }

        private void frmCryptoStockTicker_FormClosing(
            object sender,
            FormClosingEventArgs e)
        {
            if (cts != null)
            {
                cts.Cancel();
            }
        }
    }
}

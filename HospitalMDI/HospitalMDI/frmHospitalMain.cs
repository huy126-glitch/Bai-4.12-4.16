
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace HospitalMDI
{
    public class frmHospitalMain : Form
    {
        private MenuStrip menuStrip;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblDoctor;
        private ToolStripStatusLabel lblStatus;
        private ToolStripProgressBar progressBar;
        private System.Windows.Forms.Timer timer;

        public frmHospitalMain()
        {
            InitializeMainForm();
            CreateMenu();
            CreateStatusStrip();
            CreateTimer();
        }

        private void InitializeMainForm()
        {
            Text = "HỆ THỐNG QUẢN LÝ BỆNH VIỆN";
            Width = 1200;
            Height = 750;
            StartPosition = FormStartPosition.CenterScreen;
            IsMdiContainer = true;
            BackColor = Color.WhiteSmoke;
            WindowState = FormWindowState.Maximized;
            MainMenuStrip = null;
        }

        private void CreateMenu()
        {
            menuStrip = new MenuStrip();

            // Menu Hệ thống
            ToolStripMenuItem mnuSystem =
                new ToolStripMenuItem("Hệ thống");

            ToolStripMenuItem mnuLogout =
                new ToolStripMenuItem("Đăng xuất");

            ToolStripMenuItem mnuReload =
                new ToolStripMenuItem("Nạp lại dữ liệu");

            ToolStripMenuItem mnuExit =
                new ToolStripMenuItem("Thoát");

            mnuLogout.Click += (s, e) => Logout();
            mnuReload.Click += (s, e) => ReloadData();
            mnuExit.Click += (s, e) => Close();

            mnuSystem.DropDownItems.Add(mnuLogout);
            mnuSystem.DropDownItems.Add(mnuReload);
            mnuSystem.DropDownItems.Add(new ToolStripSeparator());
            mnuSystem.DropDownItems.Add(mnuExit);

            // Menu Quản lý
            ToolStripMenuItem mnuManage =
                new ToolStripMenuItem("Quản lý");

            ToolStripMenuItem mnuPatients =
                new ToolStripMenuItem("Hồ sơ Bệnh nhân");

            ToolStripMenuItem mnuDoctors =
                new ToolStripMenuItem("Danh mục Bác sĩ / Lịch trực");

            ToolStripMenuItem mnuDepartments =
                new ToolStripMenuItem("Danh mục Khoa/Phòng");

            mnuPatients.Click += (s, e) =>
                OpenChildForm<frmPatientProfiles>();

            mnuDoctors.Click += (s, e) =>
                OpenChildForm<frmDoctorSchedule>();

            mnuDepartments.Click += (s, e) =>
                OpenDepartmentForm();

            mnuManage.DropDownItems.Add(mnuPatients);
            mnuManage.DropDownItems.Add(mnuDoctors);
            mnuManage.DropDownItems.Add(mnuDepartments);

            // Menu Cửa sổ
            ToolStripMenuItem mnuWindow =
                new ToolStripMenuItem("Cửa sổ");

            ToolStripMenuItem mnuCascade =
                new ToolStripMenuItem("Sắp xếp xếp tầng");

            ToolStripMenuItem mnuHorizontal =
                new ToolStripMenuItem("Xếp ngang");

            ToolStripMenuItem mnuVertical =
                new ToolStripMenuItem("Xếp dọc");

            ToolStripMenuItem mnuCloseAll =
                new ToolStripMenuItem("Đóng tất cả");

            mnuCascade.Click += (s, e) =>
                LayoutMdi(MdiLayout.Cascade);

            mnuHorizontal.Click += (s, e) =>
                LayoutMdi(MdiLayout.TileHorizontal);

            mnuVertical.Click += (s, e) =>
                LayoutMdi(MdiLayout.TileVertical);

            mnuCloseAll.Click += (s, e) => CloseAllChildren();

            mnuWindow.DropDownItems.Add(mnuCascade);
            mnuWindow.DropDownItems.Add(mnuHorizontal);
            mnuWindow.DropDownItems.Add(mnuVertical);
            mnuWindow.DropDownItems.Add(new ToolStripSeparator());
            mnuWindow.DropDownItems.Add(mnuCloseAll);

            menuStrip.Items.Add(mnuSystem);
            menuStrip.Items.Add(mnuManage);
            menuStrip.Items.Add(mnuWindow);

            Controls.Add(menuStrip);
            MainMenuStrip = menuStrip;
        }

        private void CreateStatusStrip()
        {
            statusStrip = new StatusStrip();

            lblDoctor = new ToolStripStatusLabel(
                "Bác sĩ trực: Nguyễn Văn An");

            lblStatus = new ToolStripStatusLabel(
                "Sẵn sàng");

            progressBar = new ToolStripProgressBar();
            progressBar.Minimum = 0;
            progressBar.Maximum = 100;
            progressBar.Value = 0;
            progressBar.Visible = false;
            progressBar.Width = 150;

            ToolStripStatusLabel spring =
                new ToolStripStatusLabel();

            spring.Spring = true;

            statusStrip.Items.Add(lblDoctor);
            statusStrip.Items.Add(new ToolStripStatusLabel("|"));
            statusStrip.Items.Add(lblStatus);
            statusStrip.Items.Add(spring);
            statusStrip.Items.Add(progressBar);

            Controls.Add(statusStrip);
        }

        private void CreateTimer()
        {
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000;

            timer.Tick += (s, e) =>
            {
                lblStatus.Text =
                    "Thời gian: " +
                    DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            };

            timer.Start();
        }

        // Mở Form con và ngăn mở trùng
        private void OpenChildForm<T>() where T : Form, new()
        {
            foreach (Form child in MdiChildren)
            {
                if (child is T)
                {
                    if (child.WindowState == FormWindowState.Minimized)
                        child.WindowState = FormWindowState.Normal;

                    child.Activate();
                    return;
                }
            }

            T form = new T();
            form.MdiParent = this;
            form.Show();
        }

        private void CloseAllChildren()
        {
            // Dùng ToArray để tránh thay đổi danh sách trong lúc duyệt.
            foreach (Form child in MdiChildren.ToArray())
            {
                child.Close();
            }
        }

        private void ReloadData()
        {
            progressBar.Visible = true;
            progressBar.Value = 0;
            lblStatus.Text = "Đang nạp dữ liệu...";

            System.Windows.Forms.Timer reloadTimer = new System.Windows.Forms.Timer();
            reloadTimer.Interval = 30;

            reloadTimer.Tick += (s, e) =>
            {
                if (progressBar.Value < 100)
                {
                    progressBar.Value =
                        Math.Min(100, progressBar.Value + 10);
                }
                else
                {
                    reloadTimer.Stop();
                    reloadTimer.Dispose();

                    progressBar.Visible = false;
                    lblStatus.Text = "Nạp dữ liệu hoàn tất";

                    MessageBox.Show(
                        "Đã nạp lại dữ liệu minh họa.",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            };

            reloadTimer.Start();
        }

        private void Logout()
        {
            DialogResult result = MessageBox.Show(
                "Bạn có muốn đăng xuất khỏi hệ thống?",
                "Xác nhận đăng xuất",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                CloseAllChildren();
                lblDoctor.Text = "Bác sĩ trực: Chưa đăng nhập";
                lblStatus.Text = "Đã đăng xuất";
            }
        }

        private void OpenDepartmentForm()
        {
            MessageBox.Show(
                "Chức năng Danh mục Khoa/Phòng chưa được triển khai.",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            timer?.Stop();
            timer?.Dispose();
            base.OnFormClosed(e);
        }
    }
}

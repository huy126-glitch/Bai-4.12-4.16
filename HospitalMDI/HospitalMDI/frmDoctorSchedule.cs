
using System;
using System.Drawing;
using System.Windows.Forms;

namespace HospitalMDI
{
    public class frmDoctorSchedule : Form
    {
        private DataGridView dgvSchedule;

        public frmDoctorSchedule()
        {
            InitializeForm();
            LoadSchedule();
        }

        private void InitializeForm()
        {
            Text = "Lịch trực Bác sĩ";
            Width = 800;
            Height = 450;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;
        }

        private void LoadSchedule()
        {
            Label title = new Label
            {
                Text = "DANH SÁCH LỊCH TRỰC BÁC SĨ",
                Font = new Font("Arial", 15, FontStyle.Bold),
                ForeColor = Color.DarkBlue,
                AutoSize = true,
                Location = new Point(20, 20)
            };

            Controls.Add(title);

            dgvSchedule = new DataGridView
            {
                Location = new Point(20, 65),
                Size = new Size(740, 300),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode =
                    DataGridViewAutoSizeColumnsMode.Fill
            };

            dgvSchedule.Columns.Add("DoctorId", "Mã bác sĩ");
            dgvSchedule.Columns.Add("DoctorName", "Tên bác sĩ");
            dgvSchedule.Columns.Add("Department", "Khoa/Phòng");
            dgvSchedule.Columns.Add("Shift", "Ca trực");
            dgvSchedule.Columns.Add("Date", "Ngày trực");

            dgvSchedule.Rows.Add(
                "BS001", "Nguyễn Văn An",
                "Khoa Nội", "Sáng", "18/10/2026");

            dgvSchedule.Rows.Add(
                "BS002", "Trần Thị Bình",
                "Khoa Ngoại", "Chiều", "18/10/2026");

            dgvSchedule.Rows.Add(
                "BS003", "Lê Minh Cường",
                "Cấp cứu", "Đêm", "18/10/2026");

            dgvSchedule.Rows.Add(
                "BS004", "Phạm Thu Hà",
                "Khoa Nhi", "Sáng", "19/10/2026");

            Controls.Add(dgvSchedule);
        }
    }
}

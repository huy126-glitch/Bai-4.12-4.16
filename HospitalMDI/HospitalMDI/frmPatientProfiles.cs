
using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing;

namespace HospitalMDI
{
    public class frmPatientProfiles : Form
    {
        private DataGridView dgvPatients;
        private TextBox txtId;
        private TextBox txtName;
        private TextBox txtAge;
        private TextBox txtSearch;
        private ComboBox cboGender;
        private BindingList<Patient> patients;
        private BindingSource source;

        public frmPatientProfiles()
        {
            InitializeForm();
            CreateControls();
            LoadSampleData();
        }

        private void InitializeForm()
        {
            Text = "Hồ sơ Bệnh nhân";
            Width = 900;
            Height = 550;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;
        }

        private void CreateControls()
        {
            Label title = new Label
            {
                Text = "QUẢN LÝ HỒ SƠ BỆNH NHÂN",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = Color.DarkBlue,
                AutoSize = true,
                Location = new Point(20, 15)
            };

            Controls.Add(title);

            Controls.Add(CreateLabel("Mã bệnh nhân:", 20, 65));
            txtId = CreateTextBox(140, 62, 180);

            Controls.Add(CreateLabel("Họ và tên:", 20, 105));
            txtName = CreateTextBox(140, 102, 180);

            Controls.Add(CreateLabel("Tuổi:", 350, 65));
            txtAge = CreateTextBox(420, 62, 100);

            Controls.Add(CreateLabel("Giới tính:", 350, 105));
            cboGender = new ComboBox
            {
                Location = new Point(420, 102),
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboGender.Items.AddRange(new object[]
            {
                "Nam", "Nữ", "Khác"
            });
            cboGender.SelectedIndex = 0;
            Controls.Add(cboGender);

            Button btnAdd = new Button
            {
                Text = "Thêm bệnh nhân",
                Location = new Point(580, 62),
                Width = 140,
                Height = 32
            };
            btnAdd.Click += (s, e) => AddPatient();
            Controls.Add(btnAdd);

            Button btnDelete = new Button
            {
                Text = "Xóa bệnh nhân",
                Location = new Point(580, 102),
                Width = 140,
                Height = 32
            };
            btnDelete.Click += (s, e) => DeletePatient();
            Controls.Add(btnDelete);

            Controls.Add(CreateLabel("Tìm kiếm:", 20, 155));
            txtSearch = CreateTextBox(140, 152, 250);

            Button btnSearch = new Button
            {
                Text = "Tìm",
                Location = new Point(400, 150),
                Width = 80
            };
            btnSearch.Click += (s, e) => SearchPatient();
            Controls.Add(btnSearch);

            Button btnShowAll = new Button
            {
                Text = "Hiển thị tất cả",
                Location = new Point(490, 150),
                Width = 130
            };
            btnShowAll.Click += (s, e) =>
            {
                source.DataSource = patients;
                txtSearch.Clear();
            };
            Controls.Add(btnShowAll);

            dgvPatients = new DataGridView
            {
                Location = new Point(20, 195),
                Size = new Size(830, 290),
                AutoGenerateColumns = true,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode =
                    DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode =
                    DataGridViewAutoSizeColumnsMode.Fill
            };

            Controls.Add(dgvPatients);
        }

        private Label CreateLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                Font = new Font("Arial", 10)
            };
        }

        private TextBox CreateTextBox(int x, int y, int width)
        {
            TextBox txt = new TextBox
            {
                Location = new Point(x, y),
                Width = width
            };

            Controls.Add(txt);
            return txt;
        }

        private void LoadSampleData()
        {
            patients = new BindingList<Patient>
            {
                new Patient
                {
                    Id = "BN001",
                    Name = "Nguyễn Văn Bình",
                    Age = 35,
                    Gender = "Nam"
                },
                new Patient
                {
                    Id = "BN002",
                    Name = "Trần Thị Hoa",
                    Age = 28,
                    Gender = "Nữ"
                },
                new Patient
                {
                    Id = "BN003",
                    Name = "Lê Minh Tuấn",
                    Age = 42,
                    Gender = "Nam"
                }
            };

            source = new BindingSource();
            source.DataSource = patients;
            dgvPatients.DataSource = source;
        }

        private void AddPatient()
        {
            string id = txtId.Text.Trim();
            string name = txtName.Text.Trim();

            if (id == "" || name == "")
            {
                MessageBox.Show(
                    "Vui lòng nhập mã và họ tên bệnh nhân.");
                return;
            }

            if (!int.TryParse(txtAge.Text, out int age) || age < 0)
            {
                MessageBox.Show("Tuổi không hợp lệ.");
                return;
            }

            foreach (Patient p in patients)
            {
                if (p.Id.Equals(
                    id, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Mã bệnh nhân đã tồn tại.");
                    return;
                }
            }

            patients.Add(new Patient
            {
                Id = id,
                Name = name,
                Age = age,
                Gender = cboGender.Text
            });

            txtId.Clear();
            txtName.Clear();
            txtAge.Clear();

            source.DataSource = patients;
        }

        private void DeletePatient()
        {
            if (dgvPatients.CurrentRow == null)
                return;

            Patient patient =
                dgvPatients.CurrentRow.DataBoundItem as Patient;

            if (patient == null)
                return;

            DialogResult result = MessageBox.Show(
                "Bạn có chắc muốn xóa bệnh nhân " + patient.Name + "?",
                "Xác nhận",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
                patients.Remove(patient);
        }

        private void SearchPatient()
        {
            string keyword = txtSearch.Text.Trim();

            var result = new BindingList<Patient>();

            foreach (Patient p in patients)
            {
                if (p.Id.IndexOf(
                        keyword, StringComparison.OrdinalIgnoreCase) >= 0
                    || p.Name.IndexOf(
                        keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    result.Add(p);
                }
            }

            source.DataSource = result;
        }
    }

    public class Patient
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }
    }
}

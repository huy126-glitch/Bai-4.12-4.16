
using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace GymMemberManager
{
    public class MemberModel : INotifyPropertyChanged
    {
        private string memberId = "";
        private string fullName = "";
        private string phone = "";
        private string package = "Theo tháng";
        private DateTime startDate = DateTime.Today;
        private decimal fee;

        public string MemberId
        {
            get => memberId;
            set => SetField(ref memberId, value);
        }

        public string FullName
        {
            get => fullName;
            set => SetField(ref fullName, value);
        }

        public string Phone
        {
            get => phone;
            set => SetField(ref phone, value);
        }

        public string Package
        {
            get => package;
            set => SetField(ref package, value);
        }

        public DateTime StartDate
        {
            get => startDate;
            set => SetField(ref startDate, value);
        }

        public decimal Fee
        {
            get => fee;
            set => SetField(ref fee, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void SetField<T>(
            ref T field,
            T value,
            [System.Runtime.CompilerServices.CallerMemberName]
            string? propertyName = null)
        {
            if (Equals(field, value))
                return;

            field = value;
            PropertyChanged?.Invoke(
                this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class GymMemberForm : Form
    {
        private readonly BindingList<MemberModel> members = new();
        private readonly BindingSource memberSource = new();
        private readonly ErrorProvider errors = new();

        private readonly TextBox txtMemberId = new();
        private readonly TextBox txtFullName = new();
        private readonly TextBox txtPhone = new();
        private readonly TextBox txtFee = new();

        private readonly ComboBox cboPackage = new();
        private readonly DateTimePicker dtpStartDate = new();
        private readonly DataGridView dgvMembers = new();

        private readonly ToolStripStatusLabel statusLabel = new();

        private MemberModel? selectedMember;
        private bool loadingControls;

        public GymMemberForm()
        {
            Text = "Gym Member Manager - Quản lý hội viên";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1100, 700);
            MinimumSize = new Size(850, 550);
            Font = new Font("Segoe UI", 10);
            AutoScaleMode = AutoScaleMode.Font;

            errors.ContainerControl = this;
            errors.BlinkStyle = ErrorBlinkStyle.NeverBlink;

            BuildInterface();
            ConfigureGrid();

            memberSource.DataSource = members;
            dgvMembers.DataSource = memberSource;

            // Tự động điền dữ liệu lên Form khi chọn dòng.
            dgvMembers.SelectionChanged += (s, e) =>
            {
                if (!loadingControls)
                    LoadSelectedMember();
            };

            UpdateStatus();
            GenerateNextMemberId();
        }

        private void BuildInterface()
        {
            TableLayoutPanel root = new()
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 1,
                RowCount = 2
            };

            root.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            root.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 285));

            root.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            // Khu vực nhập liệu
            GroupBox groupInput = new()
            {
                Text = "Thông tin hội viên",
                Dock = DockStyle.Fill,
                Padding = new Padding(12)
            };

            TableLayoutPanel fields = new()
            {
                Dock = DockStyle.Top,
                Height = 220,
                ColumnCount = 2,
                RowCount = 6
            };

            fields.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 155));

            fields.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            for (int i = 0; i < 6; i++)
            {
                fields.RowStyles.Add(
                    new RowStyle(SizeType.Percent, 100f / 6));
            }

            txtMemberId.ReadOnly = true;
            txtMemberId.Dock = DockStyle.Fill;

            txtFullName.Dock = DockStyle.Fill;
            txtPhone.Dock = DockStyle.Fill;
            txtFee.Dock = DockStyle.Fill;

            cboPackage.Dock = DockStyle.Fill;
            cboPackage.DropDownStyle = ComboBoxStyle.DropDownList;
            cboPackage.Items.AddRange(new object[]
            {
                "Theo tháng", "6 tháng", "1 năm"
            });
            cboPackage.SelectedIndex = 0;

            dtpStartDate.Dock = DockStyle.Fill;
            dtpStartDate.Format = DateTimePickerFormat.Custom;
            dtpStartDate.CustomFormat = "dd/MM/yyyy";

            AddField(fields, "Mã hội viên:", txtMemberId, 0);
            AddField(fields, "Họ và tên:", txtFullName, 1);
            AddField(fields, "Số điện thoại:", txtPhone, 2);
            AddField(fields, "Gói tập:", cboPackage, 3);
            AddField(fields, "Ngày đăng ký:", dtpStartDate, 4);
            AddField(fields, "Phí dịch vụ (VNĐ):", txtFee, 5);

            FlowLayoutPanel buttons = new()
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };

            Button btnAdd = MakeButton("Thêm mới", Color.SeaGreen);
            Button btnEdit = MakeButton("Sửa thông tin", Color.RoyalBlue);
            Button btnDelete = MakeButton("Xóa hội viên", Color.IndianRed);
            Button btnClear = MakeButton("Làm mới", Color.DimGray);

            btnAdd.Click += (s, e) => AddMember();
            btnEdit.Click += (s, e) => EditMember();
            btnDelete.Click += (s, e) => DeleteMember();
            btnClear.Click += (s, e) => ClearForm();

            buttons.Controls.AddRange(new Control[]
            {
                btnAdd, btnEdit, btnDelete, btnClear
            });

            groupInput.Controls.Add(fields);
            groupInput.Controls.Add(buttons);

            // Khu vực DataGridView
            GroupBox groupGrid = new()
            {
                Text = "Danh sách hội viên",
                Dock = DockStyle.Fill,
                Padding = new Padding(6)
            };

            dgvMembers.Dock = DockStyle.Fill;
            dgvMembers.AutoGenerateColumns = false;
            dgvMembers.SelectionMode =
                DataGridViewSelectionMode.FullRowSelect;
            dgvMembers.MultiSelect = false;
            dgvMembers.ReadOnly = true;
            dgvMembers.AllowUserToAddRows = false;
            dgvMembers.AllowUserToDeleteRows = false;
            dgvMembers.RowHeadersVisible = false;
            dgvMembers.AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill;
            dgvMembers.BackgroundColor = Color.White;

            groupGrid.Controls.Add(dgvMembers);

            root.Controls.Add(groupInput, 0, 0);
            root.Controls.Add(groupGrid, 0, 1);

            StatusStrip status = new();
            status.Items.Add(statusLabel);

            Controls.Add(root);
            Controls.Add(status);
        }

        private void ConfigureGrid()
        {
            dgvMembers.Columns.Clear();

            AddColumn("MemberId", "Mã HV", 75);
            AddColumn("FullName", "Tên hội viên", 160);
            AddColumn("Phone", "SĐT", 110);
            AddColumn("Package", "Gói tập", 100);
            AddColumn("StartDate", "Ngày ĐK", 100, "dd/MM/yyyy");
            AddColumn("Fee", "Phí dịch vụ (VNĐ)", 120, "N0");
        }

        private void AddColumn(
            string property,
            string header,
            float fillWeight,
            string? format = null)
        {
            DataGridViewTextBoxColumn column = new()
            {
                Name = property,
                HeaderText = header,
                DataPropertyName = property,
                FillWeight = fillWeight,
                SortMode = DataGridViewColumnSortMode.Automatic
            };

            if (format != null)
                column.DefaultCellStyle.Format = format;

            dgvMembers.Columns.Add(column);
        }

        private void AddField(
            TableLayoutPanel panel,
            string label,
            Control control,
            int row)
        {
            panel.Controls.Add(new Label
            {
                Text = label,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, row);

            control.Margin = new Padding(4, 5, 12, 5);
            panel.Controls.Add(control, 1, row);
        }

        private Button MakeButton(string text, Color color)
        {
            return new Button
            {
                Text = text,
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                AutoSize = true,
                MinimumSize = new Size(115, 34),
                Margin = new Padding(4)
            };
        }

        // Tạo mã hội viên tiếp theo: HV001, HV002...
        private string NextMemberId()
        {
            int maxNumber = members
                .Select(m =>
                {
                    string digits = m.MemberId.Length > 2
                        ? m.MemberId.Substring(2)
                        : "";

                    return int.TryParse(digits, out int number)
                        ? number
                        : 0;
                })
                .DefaultIfEmpty(0)
                .Max();

            return $"HV{maxNumber + 1:000}";
        }

        private void GenerateNextMemberId()
        {
            txtMemberId.Text = NextMemberId();
        }

        private bool ValidateInputs()
        {
            errors.Clear();
            bool valid = true;

            string name = txtFullName.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string feeText = txtFee.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                errors.SetError(
                    txtFullName, "Vui lòng nhập họ tên hội viên.");
                valid = false;
            }
            else if (!Regex.IsMatch(
                name, @"^[\p{L}\p{M}]+(?:[ '\u2019-][\p{L}\p{M}]+)*"
                + @"(?:\s+[\p{L}\p{M}]+(?:[ '\u2019-][\p{L}\p{M}]+)*)*$"))
            {
                errors.SetError(
                    txtFullName,
                    "Tên chỉ được chứa chữ cái, dấu cách, dấu nháy hoặc gạch nối.");
                valid = false;
            }

            if (!Regex.IsMatch(phone, @"^\d{10}$"))
            {
                errors.SetError(
                    txtPhone, "Số điện thoại phải gồm đúng 10 chữ số.");
                valid = false;
            }

            if (!decimal.TryParse(
                feeText,
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out decimal fee) || fee <= 0)
            {
                errors.SetError(
                    txtFee, "Phí dịch vụ phải là số nguyên dương.");
                valid = false;
            }

            if (cboPackage.SelectedIndex < 0)
            {
                errors.SetError(
                    cboPackage, "Vui lòng chọn gói tập.");
                valid = false;
            }

            return valid;
        }

        private MemberModel ReadForm()
        {
            decimal.TryParse(
                txtFee.Text.Trim(),
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out decimal fee);

            return new MemberModel
            {
                MemberId = txtMemberId.Text.Trim(),
                FullName = txtFullName.Text.Trim(),
                Phone = txtPhone.Text.Trim(),
                Package = cboPackage.Text,
                StartDate = dtpStartDate.Value.Date,
                Fee = fee
            };
        }

        private void AddMember()
        {
            if (!ValidateInputs())
                return;

            MemberModel member = ReadForm();
            members.Add(member);

            memberSource.ResetBindings(false);
            SelectMember(member);
            UpdateStatus();

            MessageBox.Show(
                "Thêm hội viên thành công.",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            ClearForm();
        }

        private void EditMember()
        {
            if (selectedMember == null)
            {
                MessageBox.Show("Vui lòng chọn hội viên cần sửa.");
                return;
            }

            if (!ValidateInputs())
                return;

            MemberModel edited = ReadForm();

            // Cập nhật trực tiếp đối tượng đang có trong BindingList.
            // Không xóa rồi nạp lại toàn bộ danh sách.
            selectedMember.FullName = edited.FullName;
            selectedMember.Phone = edited.Phone;
            selectedMember.Package = edited.Package;
            selectedMember.StartDate = edited.StartDate;
            selectedMember.Fee = edited.Fee;

            memberSource.ResetCurrentItem();
            memberSource.ResetBindings(false);

            SelectMember(selectedMember);
            UpdateStatus();

            MessageBox.Show(
                "Cập nhật thông tin hội viên thành công.",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void DeleteMember()
        {
            if (selectedMember == null)
            {
                MessageBox.Show("Vui lòng chọn hội viên cần xóa.");
                return;
            }

            DialogResult result = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa hội viên "
                + $"{selectedMember.MemberId} - "
                + $"{selectedMember.FullName}?",
                "Xác nhận xóa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                return;

            members.Remove(selectedMember);

            selectedMember = null;
            memberSource.ResetBindings(false);
            UpdateStatus();
            ClearForm();
        }

        private void LoadSelectedMember()
        {
            MemberModel? member =
                dgvMembers.CurrentRow?.DataBoundItem as MemberModel;

            if (member == null)
                return;

            loadingControls = true;

            selectedMember = member;

            txtMemberId.Text = member.MemberId;
            txtFullName.Text = member.FullName;
            txtPhone.Text = member.Phone;
            txtFee.Text = member.Fee.ToString("0");

            cboPackage.SelectedItem = member.Package;
            dtpStartDate.Value = member.StartDate;

            errors.Clear();
            loadingControls = false;
        }

        private void SelectMember(MemberModel member)
        {
            foreach (DataGridViewRow row in dgvMembers.Rows)
            {
                if (ReferenceEquals(row.DataBoundItem, member))
                {
                    row.Selected = true;
                    dgvMembers.CurrentCell = row.Cells[0];
                    selectedMember = member;
                    return;
                }
            }
        }

        private void ClearForm()
        {
            loadingControls = true;

            selectedMember = null;
            errors.Clear();

            GenerateNextMemberId();

            txtFullName.Clear();
            txtPhone.Clear();
            txtFee.Clear();

            cboPackage.SelectedIndex = 0;
            dtpStartDate.Value = DateTime.Today;

            dgvMembers.ClearSelection();

            loadingControls = false;
            txtFullName.Focus();
        }

        private void UpdateStatus()
        {
            statusLabel.Text =
                $"Tổng số hội viên: {members.Count}";
        }
    }
}

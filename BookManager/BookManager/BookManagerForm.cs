
using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BookManager
{
    public class BookModel : INotifyPropertyChanged
    {
        private string bookId = "";
        private string title = "";
        private string author = "";
        private string category = "";
        private int quantity;
        private decimal unitPrice;
        private string coverPath = "";

        public string BookId
        {
            get => bookId;
            set => SetField(ref bookId, value);
        }

        public string Title
        {
            get => title;
            set => SetField(ref title, value);
        }

        public string Author
        {
            get => author;
            set => SetField(ref author, value);
        }

        public string Category
        {
            get => category;
            set => SetField(ref category, value);
        }

        public int Quantity
        {
            get => quantity;
            set => SetField(ref quantity, value);
        }

        public decimal UnitPrice
        {
            get => unitPrice;
            set => SetField(ref unitPrice, value);
        }

        public string CoverPath
        {
            get => coverPath;
            set => SetField(ref coverPath, value);
        }

        public decimal TotalValue => Quantity * UnitPrice;

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

    public class BookManagerForm : Form
    {
        private readonly BindingList<BookModel> books = new();
        private readonly BindingSource bookSource = new();
        private readonly ErrorProvider errors = new();

        private readonly TextBox txtBookId = new();
        private readonly TextBox txtTitle = new();
        private readonly TextBox txtAuthor = new();
        private readonly TextBox txtCategory = new();
        private readonly TextBox txtQuantity = new();
        private readonly TextBox txtPrice = new();
        private readonly TextBox txtSearch = new();

        private readonly PictureBox picBookCover = new();
        private readonly DataGridView dgvBooks = new();

        private readonly ToolStripStatusLabel countLabel = new();
        private readonly ToolStripStatusLabel valueLabel = new();
        private readonly ToolStripStatusLabel timeLabel = new();

        private readonly System.Windows.Forms.Timer clockTimer = new();

        private string currentCoverPath = "";
        private BookModel? selectedBook;
        private bool loadingControls;

        public BookManagerForm()
        {
            Text = "Book Manager - Quản lý tài liệu nhà sách";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1250, 800);
            MinimumSize = new Size(950, 650);
            Font = new Font("Segoe UI", 10);
            AutoScaleMode = AutoScaleMode.Font;

            errors.ContainerControl = this;
            errors.BlinkStyle = ErrorBlinkStyle.NeverBlink;

            BuildInterface();
            ConfigureGrid();

            bookSource.DataSource = books;
            dgvBooks.DataSource = bookSource;

            dgvBooks.SelectionChanged += (s, e) =>
            {
                if (!loadingControls)
                    LoadSelectedBook();
            };

            txtSearch.TextChanged += (s, e) => ApplySearch();

            books.ListChanged += (s, e) => UpdateStatus();

            UpdateStatus();

            clockTimer.Interval = 1000;
            clockTimer.Tick += (s, e) =>
            {
                timeLabel.Text =
                    "Thời gian: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            };
            clockTimer.Start();

            FormClosed += (s, e) => clockTimer.Dispose();
        }

        // ================= GIAO DIỆN =================

        private void BuildInterface()
        {
            MenuStrip menu = new();

            ToolStripMenuItem fileMenu = new("File");
            ToolStripMenuItem openMenu = new("Mở file CSV");
            ToolStripMenuItem saveMenu = new("Lưu file CSV");
            ToolStripMenuItem exitMenu = new("Thoát");

            openMenu.Click += (s, e) => ImportCsv();
            saveMenu.Click += (s, e) => ExportCsv();
            exitMenu.Click += (s, e) => Close();

            fileMenu.DropDownItems.Add(openMenu);
            fileMenu.DropDownItems.Add(saveMenu);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(exitMenu);

            ToolStripMenuItem editMenu = new("Edit");
            ToolStripMenuItem addMenu = new("Thêm");
            ToolStripMenuItem deleteMenu = new("Xóa");
            ToolStripMenuItem searchMenu = new("Tìm kiếm");

            addMenu.Click += (s, e) => AddBook();
            deleteMenu.Click += (s, e) => DeleteBook();
            searchMenu.Click += (s, e) =>
            {
                txtSearch.Focus();
                txtSearch.SelectAll();
            };

            editMenu.DropDownItems.Add(addMenu);
            editMenu.DropDownItems.Add(deleteMenu);
            editMenu.DropDownItems.Add(searchMenu);

            menu.Items.Add(fileMenu);
            menu.Items.Add(editMenu);
            MainMenuStrip = menu;

            ToolStrip toolStrip = new();

            ToolStripButton btnOpen = new("Mở file");
            ToolStripButton btnSave = new("Lưu file");
            ToolStripButton btnAdd = new("Thêm mới");
            ToolStripButton btnUpdate = new("Sửa");
            ToolStripButton btnDelete = new("Xóa");

            btnOpen.ToolTipText = "Mở file CSV";
            btnSave.ToolTipText = "Lưu danh sách ra CSV";
            btnAdd.ToolTipText = "Thêm sách mới";

            btnOpen.Click += (s, e) => ImportCsv();
            btnSave.Click += (s, e) => ExportCsv();
            btnAdd.Click += (s, e) => AddBook();
            btnUpdate.Click += (s, e) => UpdateBook();
            btnDelete.Click += (s, e) => DeleteBook();

            toolStrip.Items.Add(btnOpen);
            toolStrip.Items.Add(btnSave);
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(btnAdd);
            toolStrip.Items.Add(btnUpdate);
            toolStrip.Items.Add(btnDelete);

            StatusStrip statusStrip = new();
            statusStrip.Items.Add(countLabel);
            statusStrip.Items.Add(new ToolStripSeparator());
            statusStrip.Items.Add(valueLabel);
            statusStrip.Items.Add(new ToolStripSeparator());
            statusStrip.Items.Add(timeLabel);

            TableLayoutPanel root = new()
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                ColumnCount = 1,
                RowCount = 3
            };

            root.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            root.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 315));

            root.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 48));

            root.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            // Khu vực chi tiết sách
            GroupBox detailGroup = new()
            {
                Text = "Thông tin sách",
                Dock = DockStyle.Fill,
                Padding = new Padding(8)
            };

            TableLayoutPanel detail = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1
            };

            detail.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 42));

            detail.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 38));

            detail.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 20));

            // Các trường nhập liệu
            TableLayoutPanel fields = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6,
                Padding = new Padding(4)
            };

            fields.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 125));

            fields.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            for (int i = 0; i < 6; i++)
                fields.RowStyles.Add(
                    new RowStyle(SizeType.Percent, 100f / 6));

            txtBookId.ReadOnly = true;

            AddField(fields, "Mã sách:", txtBookId, 0);
            AddField(fields, "Tên sách:", txtTitle, 1);
            AddField(fields, "Tác giả:", txtAuthor, 2);
            AddField(fields, "Thể loại:", txtCategory, 3);
            AddField(fields, "Số lượng:", txtQuantity, 4);
            AddField(fields, "Đơn giá (VNĐ):", txtPrice, 5);

            // Khu vực ảnh bìa
            TableLayoutPanel imagePanel = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(5)
            };

            imagePanel.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 28));

            imagePanel.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            imagePanel.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 38));

            Label imageLabel = new()
            {
                Text = "Ảnh bìa sách:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            picBookCover.Dock = DockStyle.Fill;
            picBookCover.SizeMode = PictureBoxSizeMode.Zoom;
            picBookCover.BorderStyle = BorderStyle.FixedSingle;
            picBookCover.BackColor = Color.WhiteSmoke;

            Button btnCover = new()
            {
                Text = "Tải ảnh bìa",
                Dock = DockStyle.Fill
            };
            btnCover.Click += (s, e) => ChooseCover();

            imagePanel.Controls.Add(imageLabel, 0, 0);
            imagePanel.Controls.Add(picBookCover, 0, 1);
            imagePanel.Controls.Add(btnCover, 0, 2);

            // Hướng dẫn nhanh
            Label hint = new()
            {
                Text = "Nhập thông tin sách ở cột bên trái.\n\n"
                     + "Chọn một dòng trong bảng để xem hoặc sửa.\n\n"
                     + "Dữ liệu có thể nhập/xuất bằng file CSV.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopLeft,
                Padding = new Padding(8)
            };

            detail.Controls.Add(fields, 0, 0);
            detail.Controls.Add(hint, 1, 0);
            detail.Controls.Add(imagePanel, 2, 0);

            detailGroup.Controls.Add(detail);
            root.Controls.Add(detailGroup, 0, 0);

            // Thanh tìm kiếm
            TableLayoutPanel searchPanel = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0, 6, 0, 6)
            };

            searchPanel.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 110));

            searchPanel.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            Label searchLabel = new()
            {
                Text = "Tìm sách:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            txtSearch.Dock = DockStyle.Fill;
            txtSearch.PlaceholderText =
                "Nhập mã sách, tên sách, tác giả hoặc thể loại...";

            searchPanel.Controls.Add(searchLabel, 0, 0);
            searchPanel.Controls.Add(txtSearch, 1, 0);

            root.Controls.Add(searchPanel, 0, 1);

            // Bảng danh sách
            GroupBox gridGroup = new()
            {
                Text = "Danh sách tài liệu sách",
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };

            dgvBooks.Dock = DockStyle.Fill;
            dgvBooks.AutoGenerateColumns = false;
            dgvBooks.SelectionMode =
                DataGridViewSelectionMode.FullRowSelect;
            dgvBooks.MultiSelect = false;
            dgvBooks.ReadOnly = true;
            dgvBooks.AllowUserToAddRows = false;
            dgvBooks.AllowUserToDeleteRows = false;
            dgvBooks.RowHeadersVisible = false;
            dgvBooks.AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill;
            dgvBooks.BackgroundColor = Color.White;

            gridGroup.Controls.Add(dgvBooks);
            root.Controls.Add(gridGroup, 0, 2);

            Controls.Add(root);
            Controls.Add(toolStrip);
            Controls.Add(statusStrip);
            Controls.Add(menu);
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

            control.Dock = DockStyle.Fill;
            control.Margin = new Padding(4, 5, 8, 5);
            panel.Controls.Add(control, 1, row);
        }

        private void ConfigureGrid()
        {
            dgvBooks.Columns.Clear();

            AddColumn("BookId", "Mã sách", 75);
            AddColumn("Title", "Tên sách", 170);
            AddColumn("Author", "Tác giả", 120);
            AddColumn("Category", "Thể loại", 100);
            AddColumn("Quantity", "Số lượng", 70, "N0");
            AddColumn("UnitPrice", "Đơn giá (VNĐ)", 105, "N0");
            AddColumn("TotalValue", "Giá trị kho (VNĐ)", 120, "N0");
            AddColumn("CoverPath", "Ảnh bìa", 100);
        }

        private void AddColumn(
            string property,
            string header,
            float weight,
            string? format = null)
        {
            DataGridViewTextBoxColumn column = new()
            {
                Name = property,
                HeaderText = header,
                DataPropertyName = property,
                FillWeight = weight,
                SortMode = DataGridViewColumnSortMode.Automatic
            };

            if (format != null)
                column.DefaultCellStyle.Format = format;

            dgvBooks.Columns.Add(column);
        }

        // ================= NGHIỆP VỤ SÁCH =================

        private string NextBookId()
        {
            int max = books
                .Select(b =>
                {
                    string digits = b.BookId.StartsWith("S")
                        ? b.BookId.Substring(1)
                        : b.BookId;

                    return int.TryParse(digits, out int n) ? n : 0;
                })
                .DefaultIfEmpty(0)
                .Max();

            return $"S{max + 1:000}";
        }

        private bool ValidateInputs()
        {
            errors.Clear();
            bool valid = true;

            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                errors.SetError(txtTitle, "Vui lòng nhập tên sách.");
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(txtAuthor.Text))
            {
                errors.SetError(txtAuthor, "Vui lòng nhập tác giả.");
                valid = false;
            }

            if (!int.TryParse(txtQuantity.Text.Trim(), out int quantity)
                || quantity < 0)
            {
                errors.SetError(
                    txtQuantity, "Số lượng phải là số nguyên không âm.");
                valid = false;
            }

            if (!decimal.TryParse(
                txtPrice.Text.Trim(),
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out decimal price) || price < 0)
            {
                errors.SetError(
                    txtPrice, "Đơn giá phải là số không âm.");
                valid = false;
            }

            return valid;
        }

        private BookModel ReadForm()
        {
            int.TryParse(txtQuantity.Text.Trim(), out int quantity);

            decimal.TryParse(
                txtPrice.Text.Trim(),
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out decimal price);

            return new BookModel
            {
                BookId = txtBookId.Text.Trim(),
                Title = txtTitle.Text.Trim(),
                Author = txtAuthor.Text.Trim(),
                Category = txtCategory.Text.Trim(),
                Quantity = quantity,
                UnitPrice = price,
                CoverPath = currentCoverPath
            };
        }

        private void AddBook()
        {
            if (!ValidateInputs())
                return;

            BookModel book = ReadForm();
            books.Add(book);

            bookSource.ResetBindings(false);
            SelectBook(book);
            UpdateStatus();

            MessageBox.Show(
                "Thêm sách thành công.",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            ClearForm();
        }

        private void UpdateBook()
        {
            if (selectedBook == null)
            {
                MessageBox.Show("Vui lòng chọn sách cần sửa.");
                return;
            }

            if (!ValidateInputs())
                return;

            BookModel edited = ReadForm();

            selectedBook.Title = edited.Title;
            selectedBook.Author = edited.Author;
            selectedBook.Category = edited.Category;
            selectedBook.Quantity = edited.Quantity;
            selectedBook.UnitPrice = edited.UnitPrice;
            selectedBook.CoverPath = edited.CoverPath;

            bookSource.ResetCurrentItem();
            bookSource.ResetBindings(false);
            UpdateStatus();
            SelectBook(selectedBook);
        }

        private void DeleteBook()
        {
            if (selectedBook == null)
            {
                MessageBox.Show("Vui lòng chọn sách cần xóa.");
                return;
            }

            if (MessageBox.Show(
                $"Bạn có chắc muốn xóa sách '{selectedBook.Title}'?",
                "Xác nhận xóa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            books.Remove(selectedBook);
            selectedBook = null;

            bookSource.ResetBindings(false);
            UpdateStatus();
            ClearForm();
        }

        private void LoadSelectedBook()
        {
            BookModel? book =
                dgvBooks.CurrentRow?.DataBoundItem as BookModel;

            if (book == null)
                return;

            loadingControls = true;
            selectedBook = book;

            txtBookId.Text = book.BookId;
            txtTitle.Text = book.Title;
            txtAuthor.Text = book.Author;
            txtCategory.Text = book.Category;
            txtQuantity.Text = book.Quantity.ToString();
            txtPrice.Text = book.UnitPrice.ToString(
                CultureInfo.CurrentCulture);

            currentCoverPath = book.CoverPath;
            LoadCover(currentCoverPath);

            errors.Clear();
            loadingControls = false;
        }

        private void SelectBook(BookModel book)
        {
            foreach (DataGridViewRow row in dgvBooks.Rows)
            {
                if (ReferenceEquals(row.DataBoundItem, book))
                {
                    row.Selected = true;
                    dgvBooks.CurrentCell = row.Cells[0];
                    selectedBook = book;
                    return;
                }
            }
        }

        private void ClearForm()
        {
            loadingControls = true;
            selectedBook = null;
            errors.Clear();

            txtBookId.Text = NextBookId();
            txtTitle.Clear();
            txtAuthor.Clear();
            txtCategory.Clear();
            txtQuantity.Text = "0";
            txtPrice.Text = "0";

            currentCoverPath = "";

            Image? old = picBookCover.Image;
            picBookCover.Image = null;
            old?.Dispose();

            dgvBooks.ClearSelection();
            loadingControls = false;
        }

        // ================= ẢNH BÌA =================

        private void ChooseCover()
        {
            using OpenFileDialog dialog = new()
            {
                Title = "Chọn ảnh bìa sách",
                Filter = "Ảnh bìa (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
                Multiselect = false
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                LoadCover(dialog.FileName);
                currentCoverPath = dialog.FileName;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể tải ảnh: " + ex.Message,
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void LoadCover(string path)
        {
            Image? old = picBookCover.Image;
            picBookCover.Image = null;
            old?.Dispose();

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return;

            using Image source = Image.FromFile(path);
            picBookCover.Image = new Bitmap(source);
            picBookCover.SizeMode = PictureBoxSizeMode.Zoom;
        }

        // ================= TÌM KIẾM =================

        private void ApplySearch()
        {
            string keyword = txtSearch.Text.Trim();

            var result = books
                .Where(b =>
                    b.BookId.Contains(keyword,
                        StringComparison.OrdinalIgnoreCase)
                    || b.Title.Contains(keyword,
                        StringComparison.OrdinalIgnoreCase)
                    || b.Author.Contains(keyword,
                        StringComparison.OrdinalIgnoreCase)
                    || b.Category.Contains(keyword,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Chỉ thay đổi danh sách hiển thị, không xóa dữ liệu gốc.
            bookSource.DataSource = new BindingList<BookModel>(result);
            dgvBooks.DataSource = bookSource;
        }

        // ================= CSV: XUẤT / NHẬP =================

        private void ExportCsv()
        {
            using SaveFileDialog dialog = new()
            {
                Title = "Lưu danh sách sách",
                Filter = "CSV (*.csv)|*.csv",
                FileName = "BookReport.csv",
                DefaultExt = "csv",
                AddExtension = true
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                using StreamWriter writer = new(
                    dialog.FileName, false, new UTF8Encoding(true));

                writer.WriteLine(
                    "BookId,Title,Author,Category,Quantity,UnitPrice,CoverPath");

                foreach (BookModel b in books)
                {
                    writer.WriteLine(string.Join(",", new[]
                    {
                        Csv(b.BookId),
                        Csv(b.Title),
                        Csv(b.Author),
                        Csv(b.Category),
                        Csv(b.Quantity.ToString(CultureInfo.InvariantCulture)),
                        Csv(b.UnitPrice.ToString(CultureInfo.InvariantCulture)),
                        Csv(b.CoverPath)
                    }));
                }

                MessageBox.Show(
                    "Lưu file CSV thành công.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi lưu CSV: " + ex.Message);
            }
        }

        private void ImportCsv()
        {
            using OpenFileDialog dialog = new()
            {
                Title = "Mở danh sách sách CSV",
                Filter = "CSV (*.csv)|*.csv",
                Multiselect = false
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                string[] lines = File.ReadAllLines(
                    dialog.FileName, Encoding.UTF8);

                if (lines.Length <= 1)
                {
                    MessageBox.Show("File CSV không có dữ liệu.");
                    return;
                }

                BindingList<BookModel> imported = new();

                for (int i = 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i]))
                        continue;

                    var parts = ParseCsvLine(lines[i]);

                    if (parts.Count < 7)
                        throw new FormatException(
                            $"Dữ liệu không hợp lệ tại dòng {i + 1}.");

                    if (!int.TryParse(
                        parts[4], NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out int quantity)
                        || quantity < 0)
                    {
                        throw new FormatException(
                            $"Số lượng không hợp lệ tại dòng {i + 1}.");
                    }

                    if (!decimal.TryParse(
                        parts[5], NumberStyles.Number,
                        CultureInfo.InvariantCulture, out decimal price)
                        || price < 0)
                    {
                        throw new FormatException(
                            $"Đơn giá không hợp lệ tại dòng {i + 1}.");
                    }

                    imported.Add(new BookModel
                    {
                        BookId = parts[0],
                        Title = parts[1],
                        Author = parts[2],
                        Category = parts[3],
                        Quantity = quantity,
                        UnitPrice = price,
                        CoverPath = parts[6]
                    });
                }

                // Chỉ thay danh sách khi đã đọc và kiểm tra thành công.
                books.RaiseListChangedEvents = false;
                books.Clear();

                foreach (BookModel book in imported)
                    books.Add(book);

                books.RaiseListChangedEvents = true;
                books.ResetBindings();

                bookSource.DataSource = books;
                dgvBooks.DataSource = bookSource;

                ClearForm();
                UpdateStatus();

                MessageBox.Show(
                    $"Đã nhập {books.Count} đầu sách.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                books.RaiseListChangedEvents = true;
                books.ResetBindings();

                MessageBox.Show(
                    "Không thể nhập CSV:\n" + ex.Message,
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private string Csv(string value)
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        // Phân tích CSV có hỗ trợ dấu phẩy nằm bên trong dấu ngoặc kép.
        private System.Collections.Generic.List<string> ParseCsvLine(
            string line)
        {
            var fields = new System.Collections.Generic.List<string>();
            StringBuilder field = new();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length
                        && line[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(field.ToString());
                    field.Clear();
                }
                else
                {
                    field.Append(c);
                }
            }

            if (inQuotes)
                throw new FormatException("CSV có dấu ngoặc kép chưa đóng.");

            fields.Add(field.ToString());
            return fields;
        }

        // ================= STATUS STRIP =================

        private void UpdateStatus()
        {
            countLabel.Text = $"Tổng số đầu sách: {books.Count}";

            decimal totalValue = books.Sum(b => b.TotalValue);

            valueLabel.Text =
                $"Tổng giá trị kho: {totalValue:N0} VNĐ";

            timeLabel.Text =
                "Thời gian: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Image? image = picBookCover.Image;
            picBookCover.Image = null;
            image?.Dispose();

            base.OnFormClosed(e);
        }
    }
}

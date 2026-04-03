using System;
using System.Drawing;
using System.Windows.Forms;
using SportClubApp.Models;
using SportClubApp.Services;
using SportClubApp.Utils;

namespace SportClubApp.Forms
{
    public sealed class MainForm : Form
    {
        private readonly UserContext _user;
        private readonly AuthService _authService = new AuthService();
        private readonly CatalogService _catalogService = new CatalogService();
        private readonly OrderService _orderService = new OrderService();

        private readonly DataGridView _catalogGrid = BuildGrid();
        private readonly DataGridView _managerGrid = BuildGrid();
        private readonly DataGridView _cartGrid = BuildGrid();
        private readonly DataGridView _ordersGrid = BuildGrid();
        private readonly DataGridView _usersGrid = BuildGrid();

        private readonly TextBox _search = new TextBox();
        private readonly ComboBox _sort = new ComboBox();

        public MainForm(UserContext user)
        {
            _user = user;
            Text = $"Sport Club CRM — {_user.FullName} ({_user.Role})";
            Width = 1360;
            Height = 860;
            StartPosition = FormStartPosition.CenterScreen;
            Icon = SystemIcons.Application;
            BackColor = Color.WhiteSmoke;
            UiTheme.Apply(this);

            var tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(CreateCatalogTab());
            tabs.TabPages.Add(CreateCartTab());
            tabs.TabPages.Add(CreateOrdersTab());
            tabs.TabPages.Add(CreateProfileTab());
            if (_user.IsManagerOrAdmin) tabs.TabPages.Add(CreateManagerTab());
            if (_user.IsAdmin) tabs.TabPages.Add(CreateUsersTab());

            Controls.Add(tabs);
            LoadAll();
        }

        private static DataGridView BuildGrid()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = true,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        private TabPage CreateCatalogTab()
        {
            var tab = new TabPage("Каталог");
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            _sort.Items.AddRange(new object[] { "Name", "Price" });
            _sort.SelectedIndex = 0;
            _search.Width = 180;
            _search.MaxLength = 100;

            var refresh = Btn("Обновить", (_, __) => LoadCatalog());
            var add = Btn("В корзину", (_, __) => AddSelectedToCart());

            panel.Controls.Add(new Label { Text = "Поиск", Width = 50, TextAlign = ContentAlignment.MiddleLeft });
            panel.Controls.Add(_search);
            panel.Controls.Add(new Label { Text = "Сортировка", Width = 85, TextAlign = ContentAlignment.MiddleLeft });
            panel.Controls.Add(_sort);
            panel.Controls.Add(refresh);
            panel.Controls.Add(add);

            root.Controls.Add(panel, 0, 0);
            root.Controls.Add(_catalogGrid, 0, 1);
            tab.Controls.Add(root);
            return tab;
        }

        private TabPage CreateCartTab()
        {
            var tab = new TabPage("Корзина");
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            panel.Controls.Add(Btn("Удалить", (_, __) => RemoveFromCart()));
            panel.Controls.Add(Btn("Оформить заказ", (_, __) => Checkout()));
            root.Controls.Add(panel, 0, 0);
            root.Controls.Add(_cartGrid, 0, 1);
            tab.Controls.Add(root);
            return tab;
        }

        private TabPage CreateOrdersTab()
        {
            var tab = new TabPage("История заказов");
            tab.Controls.Add(_ordersGrid);
            return tab;
        }

        private TabPage CreateProfileTab()
        {
            var tab = new TabPage("Личный кабинет");
            var name = new TextBox { Text = _user.FullName, Width = 360, MaxLength = 120 };
            var email = new TextBox { Text = _user.Email, Width = 360, MaxLength = 120 };
            var phone = new TextBox { Text = _user.Phone, Width = 360, MaxLength = 20 };
            var role = new TextBox { Text = _user.Role, Width = 360, ReadOnly = true };
            name.KeyPress += NameKeyPress;
            phone.KeyPress += PhoneKeyPress;

            var p = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(30), ColumnCount = 2 };
            p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            p.Controls.Add(new Label { Text = "ФИО", AutoSize = true }, 0, 0); p.Controls.Add(name, 1, 0);
            p.Controls.Add(new Label { Text = "Email", AutoSize = true }, 0, 1); p.Controls.Add(email, 1, 1);
            p.Controls.Add(new Label { Text = "Телефон", AutoSize = true }, 0, 2); p.Controls.Add(phone, 1, 2);
            p.Controls.Add(new Label { Text = "Роль", AutoSize = true }, 0, 3); p.Controls.Add(role, 1, 3);
            p.Controls.Add(Btn("Сохранить профиль", (_, __) => TryRun(() => _authService.UpdateProfile(_user, name.Text, email.Text, phone.Text), "Профиль сохранён")), 1, 4);
            tab.Controls.Add(p);
            return tab;
        }

        private TabPage CreateManagerTab()
        {
            var tab = new TabPage("Менеджер");
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            panel.Controls.Add(Btn("Добавить", (_, __) => EditProduct(null)));
            panel.Controls.Add(Btn("Изменить", (_, __) => EditSelectedProduct()));
            if (_user.IsAdmin) panel.Controls.Add(Btn("Удалить", (_, __) => DeleteSelectedProduct()));

            root.Controls.Add(panel, 0, 0);
            root.Controls.Add(_managerGrid, 0, 1);
            tab.Controls.Add(root);
            return tab;
        }

        private TabPage CreateUsersTab()
        {
            var tab = new TabPage("Пользователи");
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            panel.Controls.Add(Btn("Роль 2 (Менеджер)", (_, __) => ChangeUserRole(RoleCodes.Manager)));
            panel.Controls.Add(Btn("Роль 1 (Пользователь)", (_, __) => ChangeUserRole(RoleCodes.User)));
            panel.Controls.Add(Btn("Роль 3 (Админ)", (_, __) => ChangeUserRole(RoleCodes.Administrator)));
            root.Controls.Add(panel, 0, 0);
            root.Controls.Add(_usersGrid, 0, 1);
            tab.Controls.Add(root);
            return tab;
        }

        private static Button Btn(string text, EventHandler onClick)
        {
            var b = new Button { Text = text, Height = 34, Width = 150, FlatStyle = FlatStyle.Flat, BackColor = Color.White };
            b.Click += onClick;
            return b;
        }

        private void LoadAll()
        {
            LoadCatalog();
            LoadCart();
            LoadOrders();
            LoadUsers();
        }

        private void LoadCatalog()
        {
            var list = _catalogService.GetProducts(_search.Text, _sort.Text);
            _catalogGrid.DataSource = list;
            _managerGrid.DataSource = _catalogService.GetProducts(_search.Text, _sort.Text);
            try
            {
                FormatProductGrid(_catalogGrid);
                FormatProductGrid(_managerGrid);
            }
            catch
            {
                // no-op: avoid UI crash on dynamic grid generation edge-cases
            }
        }

        private static void FormatProductGrid(DataGridView grid)
        {
            if (grid.Columns.Count == 0) return;

            SetHeader(grid, nameof(ProductItem.ProductId), "ID", 60);
            SetHeader(grid, nameof(ProductItem.Name), "Наименование", 220);
            SetHeader(grid, nameof(ProductItem.Category), "Категория", 130);
            SetHeader(grid, nameof(ProductItem.Price), "Цена", 100);
            SetHeader(grid, nameof(ProductItem.OldPrice), "Старая цена", 110);
            SetHeader(grid, nameof(ProductItem.DiscountPercent), "% скидки", 90);
            SetHeader(grid, nameof(ProductItem.ImagePath), "Путь к изображению", 220);
            SetHeader(grid, nameof(ProductItem.StockQty), "Остаток", 90);
            SetHeader(grid, nameof(ProductItem.DiscountedPrice), "Цена со скидкой", 120);

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.DataBoundItem is ProductItem p)
                {
                    if (p.Price > 1000)
                    {
                        row.DefaultCellStyle.BackColor = Color.LemonChiffon;
                        row.DefaultCellStyle.Font = new Font(grid.Font ?? Control.DefaultFont, FontStyle.Bold);
                    }

                    if (p.OldPrice.HasValue && p.DiscountPercent.HasValue && grid.Columns.Contains(nameof(ProductItem.OldPrice)))
                    {
                        var cell = row.Cells[nameof(ProductItem.OldPrice)];
                        if (cell != null) cell.Style.Font = new Font(grid.Font ?? Control.DefaultFont, FontStyle.Strikeout);
                    }
                }
            }
        }

        private static void SetHeader(DataGridView grid, string col, string title, int minWidth)
        {
            if (grid == null || string.IsNullOrWhiteSpace(col) || grid.Columns == null || !grid.Columns.Contains(col)) return;
            var column = grid.Columns[col];
            if (column == null) return;
            try
            {
                column.HeaderText = title ?? col;
                column.MinimumWidth = Math.Max(40, minWidth);
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
            catch
            {
                // ignore unstable column state during rebind
            }
        }

        private void AddSelectedToCart()
        {
            if (!(_catalogGrid.CurrentRow?.DataBoundItem is ProductItem p)) return;
            TryRun(() => _orderService.AddToCart(_user.UserId, p.ProductId), "Добавлено в корзину");
            LoadCart();
        }

        private void LoadCart() => _cartGrid.DataSource = _orderService.GetCart(_user.UserId);
        private void LoadOrders() => _ordersGrid.DataSource = _orderService.GetOrders(_user.UserId);

        private void RemoveFromCart()
        {
            if (!(_cartGrid.CurrentRow?.DataBoundItem is CartItem x)) return;
            TryRun(() => _orderService.RemoveFromCart(x.CartItemId), "Удалено из корзины");
            LoadCart();
        }

        private void Checkout()
        {
            TryRun(() => _orderService.Checkout(_user.UserId), "Заказ оформлен");
            LoadCart();
            LoadOrders();
        }

        private void EditProduct(ProductItem item)
        {
            using (var form = new ProductEditForm(item))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    TryRun(() => _catalogService.SaveProduct(form.Product), "Сохранено");
                    LoadCatalog();
                }
            }
        }

        private void EditSelectedProduct()
        {
            if (!(_managerGrid.CurrentRow?.DataBoundItem is ProductItem p)) return;
            EditProduct(p);
        }

        private void DeleteSelectedProduct()
        {
            if (!(_managerGrid.CurrentRow?.DataBoundItem is ProductItem p)) return;
            TryRun(() => _catalogService.DeleteProduct(p.ProductId), "Удалено");
            LoadCatalog();
        }

        private void LoadUsers()
        {
            if (!_user.IsAdmin) return;
            using (var conn = Data.Db.OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT u.Id,u.FullName,u.Email,u.Phone,u.RoleId,r.Name AS RoleName,u.CreatedAt
FROM dbo.Users u
LEFT JOIN dbo.Roles r ON r.Id = u.RoleId
ORDER BY u.Id";
                var table = new System.Data.DataTable();
                using (var da = new System.Data.SqlClient.SqlDataAdapter(cmd))
                {
                    da.Fill(table);
                }
                _usersGrid.DataSource = table;
                if (_usersGrid.Columns.Contains("FullName")) _usersGrid.Columns["FullName"].HeaderText = "ФИО";
                if (_usersGrid.Columns.Contains("RoleId")) _usersGrid.Columns["RoleId"].HeaderText = "Роль №";
                if (_usersGrid.Columns.Contains("RoleName")) _usersGrid.Columns["RoleName"].HeaderText = "Роль";
                if (_usersGrid.Columns.Contains("CreatedAt")) _usersGrid.Columns["CreatedAt"].HeaderText = "Создан";
                _usersGrid.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            }
        }

        private void ChangeUserRole(int roleId)
        {
            if (!_user.IsAdmin || _usersGrid.CurrentRow == null) return;
            var id = Convert.ToInt32(_usersGrid.CurrentRow.Cells["Id"].Value);
            TryRun(() =>
            {
                using (var conn = Data.Db.OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE dbo.Users SET RoleId=@r WHERE Id=@id";
                    cmd.Parameters.AddWithValue("@r", roleId);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }, "Роль обновлена");
            LoadUsers();
        }

        private void TryRun(Action action, string success)
        {
            try { action(); MessageBox.Show(success); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        private static void NameKeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;
            if (!(char.IsLetter(e.KeyChar) || e.KeyChar == ' ' || e.KeyChar == '-')) e.Handled = true;
        }

        private static void PhoneKeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;
            if (!(char.IsDigit(e.KeyChar) || e.KeyChar == '+' || e.KeyChar == '(' || e.KeyChar == ')' || e.KeyChar == '-' || e.KeyChar == ' ')) e.Handled = true;
        }
    }
}

using System;
using System.Drawing;
using System.Linq;
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

        private readonly DataGridView _catalogGrid = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = true };
        private readonly DataGridView _cartGrid = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = true };
        private readonly DataGridView _ordersGrid = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = true };
        private readonly DataGridView _usersGrid = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = true };

        private readonly TextBox _search = new TextBox();
        private readonly ComboBox _sort = new ComboBox();

        public MainForm(UserContext user)
        {
            _user = user;
            Text = $"Sport Club CRM — {_user.FullName} ({_user.Role})";
            Width = 1280; Height = 800; StartPosition = FormStartPosition.CenterScreen;
            Icon = SystemIcons.Application;
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

        private TabPage CreateCatalogTab()
        {
            var tab = new TabPage("Каталог");
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var panel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            _sort.Items.AddRange(new object[] { "Name", "Price" }); _sort.SelectedIndex = 0;
            var refresh = new Button { Text = "Обновить" }; refresh.Click += (_, __) => LoadCatalog();
            var add = new Button { Text = "В корзину" }; add.Click += (_, __) => AddSelectedToCart();
            panel.Controls.Add(new Label { Text = "Поиск" }); panel.Controls.Add(_search);
            panel.Controls.Add(new Label { Text = "Сортировка" }); panel.Controls.Add(_sort);
            panel.Controls.Add(refresh); panel.Controls.Add(add);

            root.Controls.Add(panel, 0, 0);
            root.Controls.Add(_catalogGrid, 0, 1);
            tab.Controls.Add(root);
            return tab;
        }

        private TabPage CreateCartTab()
        {
            var tab = new TabPage("Корзина");
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var panel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            var remove = new Button { Text = "Удалить" }; remove.Click += (_, __) => RemoveFromCart();
            var checkout = new Button { Text = "Оформить заказ" }; checkout.Click += (_, __) => Checkout();
            panel.Controls.Add(remove); panel.Controls.Add(checkout);
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
            var name = new TextBox { Text = _user.FullName, Width = 260 };
            var email = new TextBox { Text = _user.Email, Width = 260 };
            var phone = new TextBox { Text = _user.Phone, Width = 260 };
            var role = new TextBox { Text = _user.Role, Width = 260, ReadOnly = true };
            var p = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(20) };
            p.Controls.Add(new Label { Text = "ФИО" }); p.Controls.Add(name);
            p.Controls.Add(new Label { Text = "Email" }); p.Controls.Add(email);
            p.Controls.Add(new Label { Text = "Телефон" }); p.Controls.Add(phone);
            p.Controls.Add(new Label { Text = "Роль" }); p.Controls.Add(role);
            var save = new Button { Text = "Сохранить профиль", Width = 220, Height = 34 };
            save.Click += (_, __) =>
            {
                TryRun(() => _authService.UpdateProfile(_user, name.Text, email.Text, phone.Text), "Профиль сохранён");
            };
            p.Controls.Add(save);
            tab.Controls.Add(p);
            return tab;
        }

        private TabPage CreateManagerTab()
        {
            var tab = new TabPage("Менеджер");
            var panel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44 };
            var add = new Button { Text = "Добавить" }; add.Click += (_, __) => EditProduct(null);
            var edit = new Button { Text = "Изменить" }; edit.Click += (_, __) => EditSelectedProduct();
            panel.Controls.Add(add); panel.Controls.Add(edit);
            if (_user.IsAdmin)
            {
                var del = new Button { Text = "Удалить" }; del.Click += (_, __) => DeleteSelectedProduct();
                panel.Controls.Add(del);
            }

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.Controls.Add(panel, 0, 0);
            var allTable = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = true };
            allTable.DataSource = _catalogService.GetProducts();
            root.Controls.Add(allTable, 0, 1);
            tab.Controls.Add(root);
            return tab;
        }

        private TabPage CreateUsersTab()
        {
            var tab = new TabPage("Пользователи");
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var panel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            var updateRole = new Button { Text = "Сделать менеджером" };
            updateRole.Click += (_, __) => ChangeUserRole("Менеджер");
            var makeUser = new Button { Text = "Сделать пользователем" };
            makeUser.Click += (_, __) => ChangeUserRole("Пользователь");
            panel.Controls.Add(updateRole); panel.Controls.Add(makeUser);
            root.Controls.Add(panel, 0, 0);
            root.Controls.Add(_usersGrid, 0, 1);
            tab.Controls.Add(root);
            return tab;
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
            foreach (DataGridViewRow row in _catalogGrid.Rows)
            {
                if (row.DataBoundItem is ProductItem p)
                {
                    if (p.Price > 1000)
                    {
                        row.DefaultCellStyle.BackColor = Color.LemonChiffon;
                        row.DefaultCellStyle.Font = new Font(_catalogGrid.Font, FontStyle.Bold);
                    }

                    if (p.OldPrice.HasValue && p.DiscountPercent.HasValue)
                    {
                        row.Cells[nameof(ProductItem.OldPrice)].Style.Font = new Font(_catalogGrid.Font, FontStyle.Strikeout);
                    }
                }
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
            if (!(_catalogGrid.CurrentRow?.DataBoundItem is ProductItem p)) return;
            EditProduct(p);
        }

        private void DeleteSelectedProduct()
        {
            if (!(_catalogGrid.CurrentRow?.DataBoundItem is ProductItem p)) return;
            TryRun(() => _catalogService.DeleteProduct(p.ProductId), "Удалено");
            LoadCatalog();
        }

        private void LoadUsers()
        {
            if (!_user.IsAdmin) return;
            using (var conn = Data.Db.OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Id,FullName,Email,Phone,Role,CreatedAt FROM dbo.Users ORDER BY Id";
                var table = new System.Data.DataTable();
                using (var da = new System.Data.SqlClient.SqlDataAdapter(cmd))
                {
                    da.Fill(table);
                }
                _usersGrid.DataSource = table;
            }
        }

        private void ChangeUserRole(string role)
        {
            if (!_user.IsAdmin || _usersGrid.CurrentRow == null) return;
            var id = Convert.ToInt32(_usersGrid.CurrentRow.Cells["Id"].Value);
            TryRun(() =>
            {
                using (var conn = Data.Db.OpenConnection())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE dbo.Users SET Role=@r WHERE Id=@id";
                    cmd.Parameters.AddWithValue("@r", role);
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
    }
}

using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using SportClubApp.Models;
using SportClubApp.Services;
using SportClubApp.Utils;

namespace SportClubApp.Forms
{
    public sealed class AuthForm : Form
    {
        private readonly AuthService _authService = new AuthService();

        private readonly TextBox _login = new TextBox();
        private readonly TextBox _password = new TextBox();
        private readonly TextBox _rName = new TextBox();
        private readonly TextBox _rEmail = new TextBox();
        private readonly TextBox _rPhone = new TextBox();
        private readonly TextBox _rPass = new TextBox();

        public UserContext AuthenticatedUser { get; private set; }

        public AuthForm()
        {
            Text = "Вход / Регистрация";
            Width = 760;
            Height = 520;
            StartPosition = FormStartPosition.CenterScreen;
            Icon = SystemIcons.Shield;
            BackColor = Color.WhiteSmoke;
            UiTheme.Apply(this);

            var header = new Label
            {
                Dock = DockStyle.Top,
                Height = 52,
                Text = "SPORT CLUB CRM",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White
            };

            var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(20, 8) };
            tabs.TabPages.Add(CreateLoginTab());
            tabs.TabPages.Add(CreateRegisterTab());

            Controls.Add(tabs);
            Controls.Add(header);
        }

        private TabPage CreateLoginTab()
        {
            var tab = new TabPage("Вход");
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(30), ColumnCount = 2, RowCount = 4 };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _login.Width = 380; _password.Width = 380;
            _password.PasswordChar = '*';
            _login.MaxLength = 120;
            _password.MaxLength = 100;

            root.Controls.Add(new Label { Text = "Email или телефон", AutoSize = true }, 0, 0);
            root.Controls.Add(_login, 1, 0);
            root.Controls.Add(new Label { Text = "Пароль", AutoSize = true }, 0, 1);
            root.Controls.Add(_password, 1, 1);

            var btn = new Button { Text = "Войти", Width = 220, Height = 40, BackColor = Color.FromArgb(22, 163, 74), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btn.Click += (_, __) =>
            {
                try
                {
                    AuthenticatedUser = _authService.Login(_login.Text, _password.Text);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            };
            root.Controls.Add(btn, 1, 2);

            tab.Controls.Add(root);
            return tab;
        }

        private TabPage CreateRegisterTab()
        {
            var tab = new TabPage("Регистрация");
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(30), ColumnCount = 2, RowCount = 6 };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _rName.Width = _rEmail.Width = _rPhone.Width = _rPass.Width = 380;
            _rPass.PasswordChar = '*';

            _rName.MaxLength = 120;
            _rEmail.MaxLength = 120;
            _rPhone.MaxLength = 20;
            _rPass.MaxLength = 100;

            _rName.KeyPress += NameKeyPress;
            _rPhone.KeyPress += PhoneKeyPress;
            _rEmail.Leave += (_, __) =>
            {
                if (!string.IsNullOrWhiteSpace(_rEmail.Text) && !ValidationHelper.IsValidEmail(_rEmail.Text))
                {
                    MessageBox.Show("Некорректный email.");
                    _rEmail.Focus();
                }
            };

            root.Controls.Add(new Label { Text = "ФИО", AutoSize = true }, 0, 0);
            root.Controls.Add(_rName, 1, 0);
            root.Controls.Add(new Label { Text = "Email", AutoSize = true }, 0, 1);
            root.Controls.Add(_rEmail, 1, 1);
            root.Controls.Add(new Label { Text = "Телефон", AutoSize = true }, 0, 2);
            root.Controls.Add(_rPhone, 1, 2);
            root.Controls.Add(new Label { Text = "Пароль", AutoSize = true }, 0, 3);
            root.Controls.Add(_rPass, 1, 3);

            var btn = new Button { Text = "Создать аккаунт", Width = 220, Height = 40, BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btn.Click += (_, __) =>
            {
                try
                {
                    _authService.Register(_rName.Text, _rEmail.Text, _rPhone.Text, _rPass.Text);
                    MessageBox.Show("Регистрация успешна.");
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            };
            root.Controls.Add(btn, 1, 4);

            tab.Controls.Add(root);
            return tab;
        }

        private static void NameKeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;
            if (!Regex.IsMatch(e.KeyChar.ToString(), "[A-Za-zА-Яа-яЁё\\- ]")) e.Handled = true;
        }

        private static void PhoneKeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;
            if (!Regex.IsMatch(e.KeyChar.ToString(), "[0-9+()\\- ]")) e.Handled = true;
        }
    }
}

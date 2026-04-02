using System;
using System.Drawing;
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
            Width = 700; Height = 420;
            StartPosition = FormStartPosition.CenterScreen;
            Icon = SystemIcons.Shield;
            UiTheme.Apply(this);

            var tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(CreateLoginTab());
            tabs.TabPages.Add(CreateRegisterTab());
            Controls.Add(tabs);
        }

        private TabPage CreateLoginTab()
        {
            var tab = new TabPage("Вход");
            var p = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(20) };
            p.Controls.Add(new Label { Text = "Email или телефон" }); p.Controls.Add(_login);
            p.Controls.Add(new Label { Text = "Пароль" }); _password.PasswordChar = '*'; p.Controls.Add(_password);
            var btn = new Button { Text = "Войти", Width = 180, Height = 35 };
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
            p.Controls.Add(btn);
            tab.Controls.Add(p);
            return tab;
        }

        private TabPage CreateRegisterTab()
        {
            var tab = new TabPage("Регистрация");
            var p = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(20) };
            p.Controls.Add(new Label { Text = "ФИО" }); p.Controls.Add(_rName);
            p.Controls.Add(new Label { Text = "Email" }); p.Controls.Add(_rEmail);
            p.Controls.Add(new Label { Text = "Телефон" }); p.Controls.Add(_rPhone);
            p.Controls.Add(new Label { Text = "Пароль" }); _rPass.PasswordChar = '*'; p.Controls.Add(_rPass);
            var btn = new Button { Text = "Создать аккаунт", Width = 180, Height = 35 };
            btn.Click += (_, __) =>
            {
                try
                {
                    _authService.Register(_rName.Text, _rEmail.Text, _rPhone.Text, _rPass.Text);
                    MessageBox.Show("Регистрация успешна.");
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            };
            p.Controls.Add(btn);
            tab.Controls.Add(p);
            return tab;
        }
    }
}

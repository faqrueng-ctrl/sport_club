using System;
using System.Threading;
using System.Windows.Forms;
using SportClubApp.Forms;
using SportClubApp.Services;

namespace SportClubApp
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.ThreadException += OnUnhandled;
            AppDomain.CurrentDomain.UnhandledException += (_, e) => ShowError(e.ExceptionObject as Exception);

            try
            {
                new DatabaseInitializer().EnsureCreated();

                using (var auth = new AuthForm())
                {
                    if (auth.ShowDialog() != DialogResult.OK || auth.AuthenticatedUser == null)
                    {
                        return;
                    }

                    Application.Run(new MainForm(auth.AuthenticatedUser));
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private static void OnUnhandled(object sender, ThreadExceptionEventArgs e) => ShowError(e.Exception);

        private static void ShowError(Exception ex)
        {
            MessageBox.Show(ex?.Message ?? "Неизвестная ошибка.", "Ошибка приложения", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

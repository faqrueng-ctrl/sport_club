using System;
using System.Windows.Forms;
using SportClubApp.Services;

namespace SportClubApp.Forms
{
    public sealed class ReportsForm : Form
    {
        private readonly PayrollService _payrollService = new PayrollService();
        private readonly DataGridView _grid = new DataGridView();
        private readonly Button _refresh = new Button();

        public ReportsForm()
        {
            Text = "Отчеты администраторов и начисление зарплаты";
            Width = 850;
            Height = 500;
            StartPosition = FormStartPosition.CenterParent;

            _refresh.Text = "Обновить";
            _refresh.SetBounds(12, 12, 120, 30);
            _refresh.Click += (_, __) => LoadReport();

            _grid.SetBounds(12, 50, 810, 390);
            _grid.ReadOnly = true;
            _grid.AllowUserToAddRows = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            Controls.Add(_refresh);
            Controls.Add(_grid);

            Shown += (_, __) => LoadReport();
        }

        private void LoadReport()
        {
            try
            {
                _grid.DataSource = _payrollService.GetAdminReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Отчет не сформирован. Проверьте таблицы MemberWorkouts/Workouts/Trainers.\n\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}

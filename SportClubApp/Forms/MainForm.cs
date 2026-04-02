using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SportClubApp.Models;
using SportClubApp.Services;

namespace SportClubApp.Forms
{
    public sealed class MainForm : Form
    {
        private readonly ScheduleService _scheduleService = new ScheduleService();

        private readonly DataGridView _scheduleGrid = new DataGridView();
        private readonly Button _refreshButton = new Button();
        private readonly Button _sellMembershipButton = new Button();
        private readonly Button _reportButton = new Button();

        public MainForm()
        {
            Text = "Спортивный клуб — рабочее место администратора";
            Width = 1200;
            Height = 700;
            StartPosition = FormStartPosition.CenterScreen;

            _refreshButton.Text = "Обновить расписание";
            _refreshButton.Location = new Point(12, 12);
            _refreshButton.Click += (_, __) => LoadSchedule();

            _sellMembershipButton.Text = "Оформить абонемент";
            _sellMembershipButton.Location = new Point(190, 12);
            _sellMembershipButton.Click += (_, __) => OpenSellMembership();

            _reportButton.Text = "Отчеты и зарплата";
            _reportButton.Location = new Point(360, 12);
            _reportButton.Click += (_, __) => new ReportsForm().ShowDialog(this);

            _scheduleGrid.Location = new Point(12, 50);
            _scheduleGrid.Width = 1150;
            _scheduleGrid.Height = 590;
            _scheduleGrid.ReadOnly = true;
            _scheduleGrid.AllowUserToAddRows = false;
            _scheduleGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _scheduleGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            Controls.Add(_refreshButton);
            Controls.Add(_sellMembershipButton);
            Controls.Add(_reportButton);
            Controls.Add(_scheduleGrid);

            Shown += (_, __) => LoadSchedule();
        }

        private void OpenSellMembership()
        {
            using (var modal = new SellMembershipForm())
            {
                modal.ShowDialog(this);
                LoadSchedule();
            }
        }

        private void LoadSchedule()
        {
            try
            {
                var rows = _scheduleService.GetSchedule();
                _scheduleGrid.DataSource = rows;
                ColorOverbookedRows(rows);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка загрузки расписания. Проверьте таблицы Workouts/Trainers/WorkoutCategories/MemberWorkouts в БД sport_club.\n\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void ColorOverbookedRows(List<TrainingSessionView> rows)
        {
            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i].FreeSeats <= 0)
                {
                    _scheduleGrid.Rows[i].DefaultCellStyle.BackColor = Color.MistyRose;
                }
            }
        }
    }
}

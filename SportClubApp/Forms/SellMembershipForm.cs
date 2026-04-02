using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SportClubApp.Models;
using SportClubApp.Services;

namespace SportClubApp.Forms
{
    public sealed class SellMembershipForm : Form
    {
        private readonly ScheduleService _scheduleService = new ScheduleService();
        private readonly MembershipService _membershipService = new MembershipService();

        private readonly TextBox _fullNameBox = new TextBox();
        private readonly TextBox _phoneBox = new TextBox();
        private readonly TextBox _emailBox = new TextBox();
        private readonly DateTimePicker _validFrom = new DateTimePicker();
        private readonly DateTimePicker _validTo = new DateTimePicker();
        private readonly NumericUpDown _adminId = new NumericUpDown();
        private readonly CheckedListBox _sessions = new CheckedListBox();
        private readonly Button _loadSessionsButton = new Button();
        private readonly Button _bookButton = new Button();

        public SellMembershipForm()
        {
            Text = "Оформление абонемента";
            Width = 900;
            Height = 700;
            StartPosition = FormStartPosition.CenterParent;

            BuildLayout();
        }

        private void BuildLayout()
        {
            var y = 20;
            Controls.Add(CreateLabel("ФИО клиента", 20, y));
            _fullNameBox.SetBounds(180, y, 300, 24);
            Controls.Add(_fullNameBox);

            y += 35;
            Controls.Add(CreateLabel("Телефон", 20, y));
            _phoneBox.SetBounds(180, y, 200, 24);
            Controls.Add(_phoneBox);

            y += 35;
            Controls.Add(CreateLabel("Email", 20, y));
            _emailBox.SetBounds(180, y, 300, 24);
            Controls.Add(_emailBox);

            y += 35;
            Controls.Add(CreateLabel("Действует с", 20, y));
            _validFrom.SetBounds(180, y, 200, 24);
            Controls.Add(_validFrom);

            y += 35;
            Controls.Add(CreateLabel("Действует по", 20, y));
            _validTo.SetBounds(180, y, 200, 24);
            _validTo.Value = DateTime.Today.AddMonths(1);
            Controls.Add(_validTo);

            y += 35;
            Controls.Add(CreateLabel("ID сотрудника", 20, y));
            _adminId.SetBounds(180, y, 100, 24);
            _adminId.Minimum = 1;
            _adminId.Maximum = 9999;
            _adminId.Value = 1;
            Controls.Add(_adminId);

            y += 45;
            _loadSessionsButton.Text = "Загрузить тренировки";
            _loadSessionsButton.SetBounds(20, y, 180, 32);
            _loadSessionsButton.Click += (_, __) => LoadSessions();
            Controls.Add(_loadSessionsButton);

            _bookButton.Text = "Забронировать и оплатить";
            _bookButton.SetBounds(210, y, 220, 32);
            _bookButton.Click += (_, __) => BookAndPay();
            Controls.Add(_bookButton);

            y += 45;
            _sessions.SetBounds(20, y, 840, 560 - y);
            Controls.Add(_sessions);
        }

        private static Label CreateLabel(string text, int x, int y)
        {
            return new Label { Text = text, Left = x, Top = y + 4, Width = 150 };
        }

        private void LoadSessions()
        {
            _sessions.Items.Clear();
            var schedule = _scheduleService.GetSchedule().Where(s => s.FreeSeats > 0).ToList();
            foreach (var row in schedule)
            {
                _sessions.Items.Add(new SessionWrapper(row), false);
            }
        }

        private void BookAndPay()
        {
            try
            {
                var selected = _sessions.CheckedItems.Cast<SessionWrapper>().ToList();
                var request = new MembershipBookingRequest
                {
                    ClientFullName = _fullNameBox.Text.Trim(),
                    ClientPhone = _phoneBox.Text.Trim(),
                    ClientEmail = _emailBox.Text.Trim(),
                    ValidFrom = _validFrom.Value,
                    ValidTo = _validTo.Value,
                    AdministratorId = (int)_adminId.Value,
                    SessionIds = selected.Select(s => s.SessionId).ToList()
                };

                var card = _membershipService.SellMembership(request);
                MessageBox.Show(card, "Клиентская карта", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Оформление не выполнено.\n\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private sealed class SessionWrapper
        {
            public int SessionId { get; }
            private readonly string _view;

            public SessionWrapper(TrainingSessionView session)
            {
                SessionId = session.SessionId;
                _view = $"#{session.SessionId} | {session.Workout} | {session.Trainer} | {session.StartAt} | " +
                        $"{session.DurationMinutes} мин | {session.Price:C} | мест: {session.FreeSeats}";
            }

            public override string ToString() => _view;
        }
    }
}

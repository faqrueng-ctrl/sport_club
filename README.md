# SportClubApp (.NET Framework 4.8, WinForms)

Приложение для администраторов спортивного клуба:
- просмотр актуального расписания;
- продажа абонементов;
- бронирование мест на тренировках с блокировкой;
- фиксация оплаты на ресепшн и передачи денег в бухгалтерию;
- отчетность администраторов и расчет начислений.

## Подключение к БД
Используется LocalDB:

`Data Source=(localdb)\mssqllocaldb;Initial Catalog=sport_club;Integrated Security=True`

## Принятая структура таблиц
Приложение ожидает следующие таблицы:
- `training_type(id, name)`
- `coach(id, full_name)`
- `training_session(id, training_type_id, coach_id, start_at, duration_minutes, price, capacity, locked_seats, is_active)`
- `client(id, full_name, phone, email)`
- `membership(id, client_id, valid_from, valid_to, sold_by_admin_id, total_amount, created_at)`
- `booking_session(id, membership_id, session_id, booking_status, booked_at)`
- `payment(id, membership_id, paid_at, payment_status, transfer_to_accounting_status)`
- `administrator(id, full_name)`
- `admin_report(id, admin_id, membership_id, created_at)`

Если структура вашей БД на скриншоте отличается по именам таблиц/колонок — скорректируйте SQL в `Services/*`.

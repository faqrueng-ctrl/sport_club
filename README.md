# SportClubApp (.NET Framework 4.8, WinForms)

Приложение переписано под вашу БД `sport_club` с таблицами:
- `dbo.Exercises`
- `dbo.Members`
- `dbo.MemberWorkouts`
- `dbo.Reviews`
- `dbo.Tags`
- `dbo.Trainers`
- `dbo.WorkoutCategories`
- `dbo.WorkoutExercises`
- `dbo.WorkoutImages`
- `dbo.Workouts`
- `dbo.WorkoutSteps`
- `dbo.WorkoutTags`

## Подключение к БД
`Data Source=(localdb)\mssqllocaldb;Initial Catalog=sport_club;Integrated Security=True`

## Что делает приложение
- Загружает расписание из `Workouts` + `Trainers` + `WorkoutCategories`.
- Показывает занятые места по числу записей в `MemberWorkouts`.
- Продает абонемент: создает/обновляет `Members` и бронирует тренировки в `MemberWorkouts`.
- Блокирует двойную запись при ограничении мест (если в `Workouts` есть поле вместимости, например `Capacity/MaxMembers/MaxParticipants/Spots`).
- Формирует отчет по тренерам на основе `MemberWorkouts`.

## Важно
Код использует динамическое определение имен колонок через `INFORMATION_SCHEMA.COLUMNS`, чтобы работать с реальной схемой (например `Name`/`WorkoutName`, `FullName` или `FirstName+LastName`, и т.д.).

# SimRacingSchedule

Система управления расписанием сотрудников SimRacing-клуба. Включает REST API и Telegram-бот для администраторов и работников клуба.

## Стек технологий

| Слой | Технологии |
|---|---|
| Backend | .NET 8, ASP.NET Core |
| База данных | PostgreSQL, Entity Framework Core 8 (Npgsql) |
| Архитектура | Clean Architecture, CQRS (MediatR) |
| Telegram-бот | Telegram.Bot 22.x |
| Тесты | xUnit, Moq, FluentAssertions, EF Core InMemory |
| Инфраструктура | Docker, Docker Compose |
| CI/CD | GitLab CI |

## Возможности

**Панель администратора (Telegram)**
- Управление сотрудниками: создание профилей, привязка Telegram-аккаунтов, деактивация
- Управление сменами: создание, просмотр по неделям, отмена
- Обработка запросов на обмен сменами (одобрение / отклонение)

**Панель сотрудника (Telegram)**
- Просмотр личного расписания по неделям
- Создание запросов на обмен сменами с коллегами
- Просмотр входящих и исходящих запросов, ответ на них прямо из уведомления

**Уведомления**
- Уведомление о назначении / отмене смены
- Уведомление о новом запросе на обмен с кнопками «Принять» / «Отклонить»
- Напоминание о начале смены (настраиваемый интервал)

**REST API**
- CRUD для сотрудников, смен и запросов на обмен
- Swagger UI доступен по адресу `http://localhost:5000/swagger`

## Типы смен

| Тип | Время |
|---|---|
| Утренняя | 10:00 – 14:00 |
| Дневная | 14:00 – 18:00 |
| Вечерняя | 18:00 – 22:00 |
| Полный день | 13:00 – 22:00 |

## Структура проекта

```
SimRacingSchedule/
├── SimRacingSchedule.Api/               # ASP.NET Core Web API, контроллеры, Swagger
├── SimRacingSchedule.Application/       # CQRS: команды, запросы, DTO (MediatR)
├── SimRacingSchedule.Application.Telegram/ # Telegram-бот: хендлеры, уведомления
├── SimRacingSchedule.Core/              # Доменные сущности, интерфейсы, перечисления
├── SimRacingSchedule.Infrastructure/   # EF Core, репозитории, конфигурации БД
└── SimRacingSchedule.Tests/            # Юнит-тесты
```

## Getting Started

### Требования

- [Docker](https://www.docker.com/) и Docker Compose
- Telegram-бот (создать через [@BotFather](https://t.me/BotFather))

### 1. Клонировать репозиторий

```bash
git clone <repo-url>
cd simschedule
```

### 2. Настроить переменные окружения

Скопировать файл-шаблон и заполнить своими значениями:

```bash
cp .env.dist .env
```

```env
POSTGRES_HOST=db
POSTGRES_DB=simracing_schedule
POSTGRES_USER=your_user
POSTGRES_PASSWORD=your_password
TELEGRAM_BOT_TOKEN=your_bot_token
```

### 3. Запустить

```bash
docker compose up --build -d
```

После запуска:
- **API + Swagger**: http://localhost:5000/swagger
- **Adminer** (управление БД): http://localhost:8080

### 4. Первый запуск бота

1. Написать боту `/start` с Telegram-аккаунта
2. Бот отправит запрос на привязку всем администраторам
3. Администратор выбирает профиль сотрудника — аккаунт привязан

> Чтобы первый администратор мог войти в панель, нужно вручную создать запись сотрудника с `Role = 1` (Administrator) через Adminer или REST API, а затем выполнить `/start`.

## Разработка без Docker

```bash
# Запустить API локально
dotnet run --project SimRacingSchedule.Api

# Запустить тесты
dotnet test SimRacingSchedule.Tests
```

## CI/CD

Пайплайн GitLab CI (`.gitlab-ci.yml`) включает три стадии:

| Стадия | Что делает |
|---|---|
| `lint` | Статический анализ (SonarAnalyzer + StyleCop) |
| `test` | Прогон юнит-тестов (xUnit + EF Core InMemory) |
| `build` | Публикация артефакта (`dotnet publish`) |

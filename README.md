# EventTracker API

REST API сервис для управления мероприятиями и бронированиями.

## Функциональность

- Управление событиями (CRUD операции) с ограничением мест (только для администраторов)
- Фильтрация и пагинация событий (доступно всем)
- JWT-аутентификация и авторизация на основе ролей (`User`, `Admin`)
- Регистрация и вход пользователей
- Бронирование событий с контролем доступных мест
- Защитные бизнес-правила бронирования:
  - нельзя бронировать уже начавшиеся события
  - у одного пользователя не более **10 активных** броней (`Pending` / `Confirmed`)
  - отмена брони доступна владельцу брони или администратору
- Асинхронная обработка заявок на бронирование
- Фоновая параллельная обработка бронирований с защитой от овербукинга
- Потокобезопасные операции бронирования

## Требования

- .NET 10 SDK
- PostgreSQL (для хранения данных)

## Структура проекта

Проект организован по принципам **чистой архитектуры** и разделён на 4 отдельных слоя (сборки):

| Проект | Назначение | Зависимости |
|--------|-----------|-------------|
| `EventTrackerApi.Domain` | Доменные сущности (`Event`, `Booking`, `User`), перечисления (`BookingStatus`, `UserRole`), доменные исключения (`NoAvailableSeatsException`, `EventAlreadyStartedException`, `BookingLimitExceededException`, `ForbiddenOperationException`) | — |
| `EventTrackerApi.Application` | Use cases, сервисы (`EventService`, `BookingService`, `AuthService`), интерфейсы портов (`IEventRepository`, `IBookingRepository`, `IUserRepository`), DTO, мапперы | `Domain` |
| `EventTrackerApi.Infrastructure` | Реализации портов (`EventRepository`, `BookingRepository`, `UserRepository`), `DbContext`, миграции EF Core, сервисы безопасности (`PasswordHasher`, `TokenService`) | `Domain`, `Application` |
| `EventTrackerApi.Presentation` | Контроллеры, middleware, глобальная обработка исключений, composition root (`Program.cs`) | `Domain`, `Application`, `Infrastructure` |

**Ключевое правило:** `Application` не зависит от `Infrastructure` напрямую — только через интерфейсы портов. Инфраструктурные реализации подключаются в `Program.cs` через DI.

## Запуск проекта

### Настройка базы данных

По умолчанию приложение подключается к PostgreSQL на `localhost:5432` с параметрами:
- Database: `eventapi`
- Username: `postgres`
- Password: `postgres`

Строка подключения настраивается в `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=eventapi;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Secret": "your-32-char-min-secret-key-here!",
    "Issuer": "EventTrackerApi",
    "Audience": "EventTrackerClients",
    "ExpiryHours": 24
  }
}
```

> **Важно:** секретный ключ `Jwt:Secret` должен быть минимум 32 символа (требование HMAC-SHA256). В production его следует хранить в переменных окружения / secrets, а не в `appsettings.json`.

Схема базы данных управляется миграциями EF Core. При запуске приложения автоматически применяются все ожидающие миграции через `Migrate()`. Таблицы `events`, `bookings` и `users` создаются миграциями `InitialCreate` и `AddUsersAndBookingUserId`; настроены внешние ключи `bookings.event_id → events.id` и `bookings.user_id → users.id`.

### Миграции

Создание новой миграции (DbContext находится в `Infrastructure`, запуск из `Presentation`):
```bash
dotnet ef migrations add <MigrationName> --project EventTrackerApi.Infrastructure --startup-project EventTrackerApi.Presentation
```

Применение миграций вручную (опционально — приложение делает это автоматически при старте):
```bash
dotnet ef database update --project EventTrackerApi.Infrastructure --startup-project EventTrackerApi.Presentation
```

### Запуск

```bash
dotnet build EventTrackerApi.slnx
dotnet run --project EventTrackerApi.Presentation.csproj
```

После запуска API будет доступен по адресу: `http://localhost:5001`

Swagger UI: `http://localhost:5001/swagger`

## Запуск тестов

```bash
# Все тесты (юнит + интеграционные)
dotnet test EventTrackerApi.slnx

# Только юнит-тесты
dotnet test EventTrackerApi.Tests

# Только интеграционные тесты
dotnet test EventTrackerApi.IntegrationTests
```

> **Важно:** для запуска интеграционных тестов должен быть запущен **Docker** (Docker Desktop или Docker Engine). Тесты автоматически поднимают контейнер PostgreSQL через Testcontainers, применяют миграции и выполняют проверки на реальной базе данных.

## API Endpoints

### Аутентификация

Все запросы к управлению событиями (создание/изменение/удаление), созданию и отмене броней требуют JWT-токена. Получить токен можно через эндпоинты `/auth`.

#### Регистрация пользователя

```http
POST /auth/register
Content-Type: application/json

{
  "login": "user1",
  "password": "Str0ngP@ss!"
}
```

**Response:** `204 No Content`

> По умолчанию регистрируется пользователь с ролью `User`. При старте приложения автоматически создаётся seed-администратор (если его ещё нет в БД):
>
> | Поле | Значение |
> |------|----------|
> | Login | `admin` |
> | Password | `Pass@word1` |
> | Role | `Admin` |

#### Вход в систему

```http
POST /auth/login
Content-Type: application/json

{
  "login": "user1",
  "password": "Str0ngP@ss!"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

#### Использование токена

Передавайте токен в заголовке `Authorization`:

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

В Swagger UI нажмите кнопку **Authorize** и введите `Bearer {ваш_токен}`.

### Получить все события (с фильтрацией и пагинацией)

```http
GET /events?title=Meeting&from=2024-01-01&to=2024-12-31&page=1&pageSize=10
```

**Query Parameters:**

| Параметр | Тип | Описание | Обязательный |
|----------|-----|----------|--------------|
| `title` | string | Поиск по названию (частичное совпадение, регистронезависимый) | Нет |
| `from` | DateTime | События, начинающиеся не раньше указанной даты | Нет |
| `to` | DateTime | События, заканчивающиеся не позже указанной даты | Нет |
| `page` | int | Номер страницы (начиная с 1, по умолчанию 1) | Нет |
| `pageSize` | int | Количество элементов на странице (по умолчанию 10) | Нет |

**Response:**
```json
{
  "totalCount": 25,
  "items": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "title": "Team Meeting",
      "description": "Weekly team sync",
      "startAt": "2024-03-15T10:00:00",
      "endAt": "2024-03-15T11:00:00",
      "totalSeats": 100,
      "availableSeats": 95
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalPages": 3
}
```

### Получить событие по ID

```http
GET /events/{id}
```

### Создать событие

```http
POST /events
Content-Type: application/json

{
  "title": "Название мероприятия",
  "description": "Описание мероприятия",
  "startAt": "2026-03-15T10:00:00",
  "endAt": "2026-03-15T12:00:00",
  "totalSeats": 50
}
```

При создании события поле `totalSeats` обязательно и должно быть больше 0. Поле `availableSeats` инициализируется равным `totalSeats`.

### Обновить событие

```http
PUT /events/{id}
Content-Type: application/json

{
  "title": "Новое название",
  "description": "Новое описание",
  "startAt": "2026-03-15T14:00:00",
  "endAt": "2026-03-15T16:00:00"
}
```

### Удалить событие

```http
DELETE /events/{id}
```

## Бронирования

### Создать бронь для события

```http
POST /events/{id}/book
Authorization: Bearer {token}
```

Создаёт бронь для указанного события от имени текущего аутентифицированного пользователя. При успешном создании уменьшает `availableSeats` на 1. Возвращает `202 Accepted` с информацией о созданной брони и заголовком `Location` для отслеживания статуса.

**Response:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440001",
  "eventId": "550e8400-e29b-41d4-a716-446655440000",
  "userId": "550e8400-e29b-41d4-a716-446655440002",
  "status": "Pending",
  "createdAt": "2026-03-29T18:30:00Z",
  "processedAt": null
}
```

**HTTP Statuses:**
- `202 Accepted` - бронь создана и ожидает обработки
- `400 Bad Request` - событие уже началось
- `401 Unauthorized` - отсутствует или невалидный JWT
- `404 Not Found` - событие или пользователь не найден
- `409 Conflict` - нет свободных мест **или** превышен лимит активных броней (10)

### Отменить бронь

```http
DELETE /bookings/{id}
Authorization: Bearer {token}
```

Отменяет бронь. Доступно владельцу брони или администратору. При отмене активной брони (`Pending` / `Confirmed`) освобождает одно место на событии. Возвращает `204 No Content`.

**HTTP Statuses:**
- `204 No Content` - бронь отменена
- `401 Unauthorized` - отсутствует или невалидный JWT
- `403 Forbidden` - попытка отменить чужую бронь без роли `Admin`
- `404 Not Found` - бронь или пользователь не найден

### Получить бронь по ID

```http
GET /bookings/{id}
```

Возвращает текущий статус бронирования.

**Response:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440001",
  "eventId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Confirmed",
  "createdAt": "2026-03-29T18:30:00Z",
  "processedAt": "2026-03-29T18:32:15Z"
}
```

**Статусы бронирования:**
- `Pending` - бронь создана, ожидает обработки
- `Confirmed` - бронь подтверждена
- `Rejected` - бронь отклонена
- `Cancelled` - бронь отменена пользователем или администратором

## Валидация

- Поля `Title`, `StartAt`, `EndAt`, `TotalSeats` обязательны
- `EndAt` должен быть позже `StartAt`
- `TotalSeats` должен быть больше 0

## HTTP Статусы

- `200 OK` - успешный запрос
- `201 Created` - событие создано
- `202 Accepted` - бронь принята к обработке
- `204 No Content` - событие удалено или бронь отменена
- `400 Bad Request` - ошибка валидации или бронирование прошедшего события
- `401 Unauthorized` - отсутствует или невалидный JWT
- `403 Forbidden` - недостаточно прав (например, обычный пользователь пытается управлять событием или отменить чужую бронь)
- `404 Not Found` - ресурс не найден
- `409 Conflict` - нет свободных мест или превышен лимит активных броней
- `500 Internal Server Error` - внутренняя ошибка сервера

## Примитивы синхронизации

### `SemaphoreSlim` в `BookingService`

В `BookingService.CreateBookingAsync` используется `SemaphoreSlim` для защиты критической секции при асинхронных операциях с базой данных:

```csharp
private static readonly SemaphoreSlim BookingLock = new(1, 1);

await BookingLock.WaitAsync();
try
{
    var eventItem = await eventRepository.GetByIdAsync(eventId);
    if (eventItem is null)
        throw new KeyNotFoundException("Event not found");
    if (eventItem.StartAt <= DateTime.UtcNow)
        throw new EventAlreadyStartedException("Cannot book an event that has already started.");

    var activeBookings = await bookingRepository.GetActiveByUserIdAsync(userId);
    if (activeBookings.Count() >= MaxActiveBookingsPerUser)
        throw new BookingLimitExceededException($"User has reached the limit of {MaxActiveBookingsPerUser} active bookings.");

    if (!eventItem.TryReserveSeats())
        throw new NoAvailableSeatsException("No available seats for this event");

    var booking = new Booking(eventId, userId);
    await bookingRepository.AddAsync(booking);
    await bookingRepository.SaveChangesAsync();
}
finally
{
    BookingLock.Release();
}
```

`SemaphoreSlim` выбран вместо `lock`, потому что внутри критической секции используются `await`-вызовы к базе данных, а `lock` не поддерживает асинхронные операции.

## Фоновая обработка бронирований

При создании брони через `POST /events/{id}/book` она создаётся в статусе `Pending`. Фоновый сервис `BookingProcessingService` периодически проверяет наличие броней в этом статусе и обрабатывает их параллельно:

1. Для получения списка pending-броней создаётся отдельный `IServiceScope` с `AppDbContext`
2. Для обработки каждой брони создаётся свой `IServiceScope` и свой `AppDbContext`
3. Для каждой брони выполняется искусственная задержка (2 секунды), имитирующая обращение к внешней системе
4. Задержки всех броней выполняются параллельно
5. Если событие было удалено к моменту обработки — бронь отклоняется (`Rejected`)
6. При непредвиденной ошибке бронь отклоняется, а место возвращается в пул через `ReleaseSeats()`
7. При успехе бронь переводится в статус `Confirmed`, заполняется поле `ProcessedAt`

Интервал проверки: 5 секунд.

Использование отдельных scope для каждой брони необходимо, потому что `BackgroundService` — синглтон, а `DbContext` — scoped зависимость.

### Пример сценария использования

```bash
# 1. Регистрируем пользователя
curl -X POST http://localhost:5001/auth/register \
  -H "Content-Type: application/json" \
  -d '{"login":"user1","password":"Str0ngP@ss!"}'

# 2. Входим и получаем токен
TOKEN=$(curl -s -X POST http://localhost:5001/auth/login \
  -H "Content-Type: application/json" \
  -d '{"login":"user1","password":"Str0ngP@ss!"}' | jq -r '.token')

# 3. Получаем токен seed-администратора
ADMIN_TOKEN=$(curl -s -X POST http://localhost:5001/auth/login \
  -H "Content-Type: application/json" \
  -d '{"login":"admin","password":"Pass@word1"}' | jq -r '.token')

# 4. Создаём событие на 3 места (требуется роль Admin)
curl -X POST http://localhost:5001/events \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d '{"title":"Конференция","description":"IT конференция","startAt":"2026-04-15T10:00:00","endAt":"2026-04-15T18:00:00","totalSeats":3}'

# 5. Создаём три брони (все успешны, 202 Accepted)
curl -X POST http://localhost:5001/events/{event-id}/book \
  -H "Authorization: Bearer $TOKEN"
curl -X POST http://localhost:5001/events/{event-id}/book \
  -H "Authorization: Bearer $TOKEN"
curl -X POST http://localhost:5001/events/{event-id}/book \
  -H "Authorization: Bearer $TOKEN"

# 6. Четвёртая бронь вернёт 409 Conflict — мест больше нет
curl -X POST http://localhost:5001/events/{event-id}/book \
  -H "Authorization: Bearer $TOKEN"

# 7. Проверяем статус первой брони через несколько секунд — будет Confirmed
curl http://localhost:5001/bookings/{booking-id} \
  -H "Authorization: Bearer $TOKEN"

# 8. Отменяем свою бронь
curl -X DELETE http://localhost:5001/bookings/{booking-id} \
  -H "Authorization: Bearer $TOKEN"
```

## Пример защиты от овербукинга

Без синхронизации при 20 одновременных запросах на бронирование события на 5 мест могло бы создаться 20 броней, так как несколько потоков одновременно прочитали `availableSeats > 0`. 

Благодаря `SemaphoreSlim` в `BookingService` гарантируется, что:
- Проверка мест и их резервирование выполняются атомарно
- При 20 конкурентных запросах на событие на 5 мест создаётся ровно 5 успешных броней
- Остальные 15 запросов получают `409 Conflict`
- `availableSeats` корректно уменьшается до 0

Это поведение покрыто юнит-тестами на конкурентность с использованием InMemory базы данных EF Core.

## Обработка ошибок

При возникновении ошибок API возвращает единообразный JSON-ответ в формате ProblemDetails:

```json
{
  "status": 404,
  "title": "Событие не найдено",
  "detail": "Событие с идентификатором '...' не найдено."
}
```

Типы ошибок:
- `400` - Ошибка валидации или некорректные параметры
- `404` - Ресурс не найден
- `409` - Нет свободных мест
- `500` - Внутренняя ошибка сервера

## Фильтрация и пагинация

Фильтрация событий реализована с использованием LINQ:
- Фильтры применяются только если параметры переданы
- Все фильтры работают совместно (логическое И)
- Пагинация использует методы `Skip` и `Take`

Примеры запросов:

```bash
# Поиск по названию
curl "https://localhost:5001/events?title=Meeting"

# События в диапазоне дат
curl "https://localhost:5001/events?from=2024-01-01&to=2024-12-31"

# Вторая страница по 5 элементов
curl "https://localhost:5001/events?page=2&pageSize=5"

# Комбинированная фильтрация
curl "https://localhost:5001/events?title=Team&from=2024-01-01&page=1&pageSize=10"
```

## Примеры ошибок (плохие запросы)

### 400 Bad Request - Ошибка валидации

**Отсутствует обязательное поле:**
```http
POST /events
Content-Type: application/json

{
  "description": "Без названия",
  "startAt": "2026-03-15T10:00:00",
  "endAt": "2026-03-15T12:00:00"
}
```
**Ответ:**
```json
{
  "status": 400,
  "title": "Ошибка валидации",
  "detail": "Title is required."
}
```

**EndAt раньше StartAt:**
```http
POST /events
Content-Type: application/json

{
  "title": "Название",
  "startAt": "2026-03-15T12:00:00",
  "endAt": "2026-03-15T10:00:00",
  "totalSeats": 10
}
```
**Ответ:**
```json
{
  "status": 400,
  "title": "Ошибка валидации",
  "detail": "EndAt must be later than StartAt."
}
```

**TotalSeats меньше или равен 0:**
```http
POST /events
Content-Type: application/json

{
  "title": "Название",
  "startAt": "2026-03-15T10:00:00",
  "endAt": "2026-03-15T12:00:00",
  "totalSeats": 0
}
```
**Ответ:**
```json
{
  "status": 400,
  "title": "Ошибка валидации",
  "detail": "TotalSeats must be greater than 0."
}
```

### 404 Not Found - Ресурс не найден

**Получение несуществующего события:**
```http
GET /events/550e8400-e29b-41d4-a716-446655440000
```
**Ответ:**
```json
{
  "status": 404,
  "title": "Событие не найдено",
  "detail": "Событие с идентификатором '550e8400-e29b-41d4-a716-446655440000' не найдено."
}
```

### 400 Bad Request - Событие уже началось

```http
POST /events/550e8400-e29b-41d4-a716-446655440000/book
Authorization: Bearer {token}
```
**Ответ:**
```json
{
  "status": 400,
  "title": "Ошибка запроса",
  "detail": "Cannot book an event that has already started."
}
```

### 401 Unauthorized - Отсутствует токен

```http
POST /events/550e8400-e29b-41d4-a716-446655440000/book
```
**Ответ:**
```json
{
  "status": 401,
  "title": "Unauthorized",
  "detail": "You are not authorized to access this resource."
}
```

### 403 Forbidden - Недостаточно прав

```http
DELETE /bookings/550e8400-e29b-41d4-a716-446655440001
Authorization: Bearer {token-of-another-user}
```
**Ответ:**
```json
{
  "status": 403,
  "title": "Forbidden",
  "detail": "You can only cancel your own bookings."
}
```

### 409 Conflict - Нет свободных мест

```http
POST /events/550e8400-e29b-41d4-a716-446655440000/book
Authorization: Bearer {token}
```
**Ответ:**
```json
{
  "status": 409,
  "title": "Нет свободных мест",
  "detail": "No available seats for this event"
}
```

## Тестирование

Проект покрыт юнит-тестами с использованием xUnit и интеграционными тестами с реальной PostgreSQL через Testcontainers.

- **Юнит-тесты** (`EventTrackerApi.Tests`) — тестируют бизнес-логику сервисов с использованием InMemory-провайдера EF Core и Moq для контроллеров.
- **Интеграционные тесты** (`EventTrackerApi.IntegrationTests`) — тестируют слой доступа к данным (репозитории) на реальной PostgreSQL в Docker-контейнере. Между тестами база данных приводится к чистому состоянию (`EnsureDeleted` + `Migrate`), что гарантирует изолированность.

Запуск всех тестов:
```bash
dotnet test
```

Запуск с подробным выводом:
```bash
dotnet test --verbosity normal
```

### Покрытие тестами

#### Юнит-тесты

##### Тесты сервиса событий (EventServiceTests)
- Создание события с `TotalSeats`
- Получение всех событий с фильтрацией и пагинацией
- Получение события по ID
- Обновление существующего события
- Удаление существующего события
- Фильтрация по названию (частичное совпадение, регистронезависимая)
- Фильтрация по датам (from, to)
- Пагинация событий
- Комбинированная фильтрация
- Неуспешные сценарии (несуществующий ID, невалидный `TotalSeats`)

##### Тесты контроллера событий (EventsControllerTests)
- Получение списка с пагинацией и фильтрацией
- Получение события по ID (успех и не найдено)
- Создание события (CreatedAtAction)
- Обновление события (успех и не найдено)
- Удаление события (NoContent и NotFound)
- Создание брони (Accepted, NotFound, Conflict)
- Изоляция тестов через Mock<IEventService>

##### Тесты сервиса бронирований (BookingServiceTests)
- Создание брони уменьшает `AvailableSeats` на 1
- Создание нескольких броней до лимита — все успешны, уникальные ID
- Бронь связывается с пользователем через `UserId`
- После исчерпания мест выбрасывается `NoAvailableSeatsException`
- Бронирование несуществующего события — `KeyNotFoundException`
- Бронирование уже начавшегося события — `EventAlreadyStartedException`
- Превышение лимита активных броней (10) — `BookingLimitExceededException`
- Лимит активных броней действует только на одного пользователя
- Получение брони по ID
- Смена статуса брони (`Confirm` / `Reject` / `Cancel`)
- `ReleaseSeats` восстанавливает доступные места
- Брони для разных событий создаются корректно
- **Конкурентные тесты:**
  - Защита от овербукинга (5 мест, 20 запросов — ровно 5 успешных)
  - Уникальность ID при 10 конкурентных запросах

#### Интеграционные тесты (RepositoryIntegrationTests)

Интеграционные тесты выполняются на реальной PostgreSQL в Docker-контейнере (Testcontainers):

##### Репозиторий событий (EventRepository)
- `AddAsync` + `GetByIdAsync` — создание и получение события
- `GetByIdAsync` с несуществующим ID — возвращает `null`
- `GetEventsAsync` без фильтров — возвращает все события
- `GetEventsAsync` с фильтром по названию
- `GetEventsAsync` с фильтром по диапазону дат
- `GetEventsAsync` с пагинацией
- `SetValues` + `SaveChangesAsync` — обновление события
- `Remove` + `SaveChangesAsync` — удаление события

##### Репозиторий бронирований (BookingRepository)
- `AddAsync` + `GetByIdAsync` — создание и получение брони с загрузкой связанного события
- `GetPendingAsync` — возвращает только брони в статусе `Pending`

##### Проверка миграций
- Таблицы `events` и `bookings` созданы в схеме
- Внешний ключ `bookings.event_id → events.id` работает корректно
- Нарушение внешнего ключа вызывает `DbUpdateException`

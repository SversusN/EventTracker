# EventTracker — микросервисная система управления мероприятиями

Система управления мероприятиями и бронированиями, разделённая на три независимых микросервиса с асинхронным обменом сообщениями через Apache Kafka.

## Состав системы

| Сервис | Ответственность | База данных | Порт |
|--------|-----------------|-------------|------|
| **UsersService** | Регистрация, вход, выдача JWT | `eventtracker_users` | 5001 |
| **EventsService** | CRUD событий, учёт доступных мест, кеширование | `eventtracker_events` | 5002 |
| **BookingsService** | Создание и отмена броней | `eventtracker_bookings` | 5003 |
| **Kafka** | Брокер сообщений | — | 9092 |
| **Zookeeper** | Координация Kafka | — | 2181 |
| **Redis** | Распределённый кеш | — | 6379 |
| **Prometheus** | Сбор метрик | — | 9090 |
| **Grafana** | Визуализация метрик | — | 3000 |
| **Jaeger** | Распределённая трассировка | — | 16686 |

Каждый сервис построен по принципам чистой архитектуры и состоит из слоёв:

- `Domain` — доменные сущности, исключения, правила
- `Application` — use cases, порты, DTO, сервисы
- `Infrastructure` — EF Core, репозитории, Kafka producer/consumer, Redis, миграции
- `Presentation` — контроллеры, middleware, Swagger, DI

Общий контракт сообщений Kafka вынесен в разделяемый проект `EventTracker.Contracts`.

## Архитектура взаимодействия

```
┌─────────────────┐      ┌─────────────────┐      ┌─────────────────┐
│  UsersService   │      │  EventsService  │      │ BookingsService │
│  (Auth + JWT)   │      │  (Events CRUD)  │      │  (Bookings)     │
│     :5001       │      │     :5002       │      │     :5003       │
└────────┬────────┘      └────────┬────────┘      └────────┬────────┘
         │                        │                        │
         │ JWT token              │                        │
         ├────────────────────────┤                        │
         │                        │                        │
         │                        │  BookingConfirmed      │
         │                        │  (Kafka topic)         │
         │                        │◄───────────────────────┤
         │                        │                        │
```

### Поток BookingConfirmed

1. Пользователь создаёт бронь в `BookingsService` (`POST /bookings`).
2. Фоновый обработчик `BookingProcessingService` находит брони в статусе `Pending` и подтверждает их.
3. `BookingsService` сохраняет бронь со статусом `Confirmed`, а затем публикует в Kafka событие `BookingConfirmed` с ключом `EventId`.
4. `EventsService` подписан на топик `booking-confirmed`, получает событие и уменьшает количество доступных мест у соответствующего события.

Сервисы не вызывают друг друга по HTTP напрямую — весь обмен идёт через Kafka.

## Требования

- .NET 10 SDK
- Docker + Docker Compose

## Запуск

```bash
docker compose up -d
```

После запуска будут доступны:

- UsersService: `http://localhost:5001`
- EventsService: `http://localhost:5002`
- BookingsService: `http://localhost:5003`
- Kafka: `localhost:9092`
- Redis: `localhost:6379`
- Prometheus: `http://localhost:9090`
- Grafana: `http://localhost:3000` (admin / admin)
- Jaeger UI: `http://localhost:16686`

> Swagger доступен только в Development-окружении. При запуске через Docker Compose установите `ASPNETCORE_ENVIRONMENT=Development` или откройте swagger вручную, задав переменную окружения.

При первом старте `EventsService` создаёт Kafka-топик `booking-confirmed`, если он ещё не существует.

## Seed-данные

При старте `UsersService` автоматически создаётся администратор:

| Login | Password | Role |
|-------|----------|------|
| `admin` | `Pass@word1` | `Admin` |

## API Endpoints

### UsersService (`:5001`)

```http
POST /auth/register
Content-Type: application/json

{
  "login": "user1",
  "password": "Str0ngP@ss!",
  "role": "User"
}
```

```http
POST /auth/login
Content-Type: application/json

{
  "login": "admin",
  "password": "Pass@word1"
}
```

### EventsService (`:5002`)

```http
GET /events
```

```http
GET /events/top
```

```http
POST /events
Content-Type: application/json
Authorization: Bearer {admin-token}

{
  "title": "Конференция",
  "description": "IT конференция",
  "startAt": "2026-04-15T10:00:00",
  "endAt": "2026-04-15T18:00:00",
  "totalSeats": 3
}
```

### BookingsService (`:5003`)

```http
POST /bookings
Content-Type: application/json
Authorization: Bearer {user-token}

{
  "eventId": "{event-id}"
}
```

```http
DELETE /bookings/{booking-id}
Authorization: Bearer {user-token}
```

## Пример сценария

```bash
# 1. Логин администратора
ADMIN_TOKEN=$(curl -s -X POST http://localhost:5001/auth/login \
  -H "Content-Type: application/json" \
  -d '{"login":"admin","password":"Pass@word1"}' | jq -r '.token')

# 2. Создание события на 3 места
curl -X POST http://localhost:5002/events \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d '{"title":"Конференция","description":"IT конференция","startAt":"2026-04-15T10:00:00","endAt":"2026-04-15T18:00:00","totalSeats":3}'

# 3. Регистрация и логин пользователя
curl -X POST http://localhost:5001/auth/register \
  -H "Content-Type: application/json" \
  -d '{"login":"user1","password":"UserPass1!"}'

USER_TOKEN=$(curl -s -X POST http://localhost:5001/auth/login \
  -H "Content-Type: application/json" \
  -d '{"login":"user1","password":"UserPass1!"}' | jq -r '.token')

# 4. Создание брони
BOOKING=$(curl -s -X POST http://localhost:5003/bookings \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $USER_TOKEN" \
  -d '{"eventId":"{event-id}"}')

# 5. Через несколько секунд проверяем, что места уменьшились
curl http://localhost:5002/events/{event-id}

# 6. Проверяем топ-10 самых популярных событий
curl http://localhost:5002/events/top
```

## Кэширование

В `EventsService` подключён **Redis** для снижения нагрузки на базу данных при частых читающих запросах.

### Что кешируется

| Данные | Ключ | TTL | Стратегия обновления |
|--------|------|-----|----------------------|
| Отдельное событие | `event:{id}` | 5 минут | **Инвалидация при записи** — ключ удаляется при обновлении, удалении события и при обработке Kafka-сообщения `BookingConfirmed` |
| Топ-10 популярных событий | `events:top10` | 1 минута | Обновление по TTL |

### Почему выбрана именно такая стратегия

- **Отдельное событие** (`GET /events/{id}`): данные могут измениться в любой момент (администратор отредактировал описание, бронь уменьшила `availableSeats`). Заметное устаревание недопустимо, поэтому используется инвалидация при записи. После любой мутирующей операции ключ сбрасывается, и следующий запрос прогревает кеш из базы.
- **Топ-10** (`GET /events/top`): это агрегат, который меняется только при бронированиях. Небольшое устаревание (до 1 минуты) допустимо, поэтому достаточно TTL. Явная инвалидация при каждом `BookingConfirmed` была бы избыточной.

### Порядок операций

При изменении данных:
1. Сначала фиксируется изменение в PostgreSQL.
2. Только после успешного сохранения удаляется соответствующий ключ из Redis.

Если сервис упадёт между этими шагами, база останется в актуальном состоянии, а кеш обновится при следующем запросе.

### Устойчивость к сбоям Redis

Если Redis недоступен:
- `GET`-запросы автоматически идут в базу данных;
- `SET`/`REMOVE` игнорируются, ошибка логируется;
- клиент не получает ошибку.

### Конфигурация

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "Cache": {
    "EventTtlSeconds": 300,
    "TopEventsTtlSeconds": 60
  }
}
```

В Docker Compose значения переопределяются через переменные окружения:
- `Redis__ConnectionString`
- `Cache__EventTtlSeconds`
- `Cache__TopEventsTtlSeconds`

## Наблюдаемость

Все три микросервиса инструментированы с помощью **OpenTelemetry** и пишут структурированные логи в формате **Serilog Compact JSON**.

### Метрики (Prometheus)

Каждый сервис экспортирует метрики по пути `/metrics` в формате Prometheus:

| Сервис | URL |
|--------|-----|
| UsersService | `http://localhost:5001/metrics` |
| EventsService | `http://localhost:5002/metrics` |
| BookingsService | `http://localhost:5003/metrics` |

Собираемые метрики:

- `http_server_request_duration_seconds_*` — длительность HTTP-запросов
- `http_server_active_requests` — активные запросы
- `kestrel_*` — метрики Kestrel
- `dotnet_gc_collections_total` — сборки мусора
- `process_runtime_dotnet_memory_usage_bytes` — использование памяти

**Prometheus** доступен по адресу `http://localhost:9090`.

### Трассировка (Jaeger)

Трассировки отправляются по OTLP/gRPC в **Jaeger** (`http://jaeger:4317` внутри Docker Compose). В UI `http://localhost:16686` можно искать трейсы по сервисам:

- `users-service`
- `events-service`
- `bookings-service`

В трейсы попадают запросы ASP.NET Core, HTTP-клиенты и операции Entity Framework Core.

### Дашборды (Grafana)

В Grafana предустановлен дашборд **EventTracker Services** (`http://localhost:3000/d/eventtracker-services`). Для входа используйте логин/пароль `admin` / `admin`.

Дашборд содержит панели:

- HTTP Request Rate
- HTTP Error Rate (5xx)
- HTTP Request Latency (p50/p95/p99)
- Active HTTP Requests
- .NET Memory Usage
- .NET GC Collections

### Логирование (Serilog)

Все сервисы используют `Serilog.AspNetCore` с выводом в консоль в формате `CompactJsonFormatter`. Логи можно собирать через `docker compose logs` или перенаправлять в любую систему централизованного логирования.

Пример структурированной записи:

```json
{"@t":"2026-07-30T17:32:40.3314360Z","@mt":"Now listening on: {address}","address":"http://[::]:5001","SourceContext":"Microsoft.Hosting.Lifetime"}
```

### Конфигурация наблюдаемости

Конечная точка OTLP задаётся в `appsettings.json`:

```json
{
  "Otlp": {
    "Endpoint": "http://localhost:4317"
  }
}
```

В Docker Compose значение переопределяется через переменную окружения:

- `Otlp__Endpoint=http://jaeger:4317`

## Конфигурация JWT

Все три сервиса используют общие значения:

```json
{
  "Jwt": {
    "Secret": "YourSuperSecretKeyForEventTrackerMicroservicesDevelopmentOnly!",
    "Issuer": "EventTracker",
    "Audience": "EventTracker",
    "ExpiresHours": "24"
  }
}
```

В Docker Compose значения переопределяются через переменные окружения.

## Примечания

- `BookingsService` не уменьшает места у событий напрямую — это делает `EventsService` через Kafka.
- `EventsService` гарантирует создание топика `booking-confirmed` при старте.
- Ключ сообщения Kafka — `EventId`, что обеспечивает порядок обработки броней по одному событию.

## Тестирование

Проект покрыт юнит-тестами с использованием xUnit и Moq.

- **Юнит-тесты EventsService** (`src/EventTracker.EventsService/EventTracker.EventsService.Tests`) — покрывают логику кеширования: попадание в кеш, промах, инвалидация при обновлении/удалении, кеширование топ-10.

Запуск всех тестов микросервисов:
```bash
dotnet test EventTracker.Microservices.slnx
```

## Легаси

Монолитная версия приложения вынесена в папку [`legacy/`](legacy/README.md).

# EventTracker API

REST API сервис для управления мероприятиями.

## Требования

- .NET 10 SDK

## Запуск проекта

```bash
dotnet build
dotnet run
```

После запуска API будет доступен по адресу: `https://localhost:5001`

Swagger UI: `https://localhost:5001/swagger`

## API Endpoints

### Получить все события
```http
GET /events
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
  "endAt": "2026-03-15T12:00:00"
}
```

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

## Валидация

- Поля `Title`, `StartAt`, `EndAt` обязательны
- `EndAt` должен быть позже `StartAt`

## HTTP Статусы

- `200 OK` - успешный запрос
- `201 Created` - событие создано
- `204 No Content` - событие удалено
- `400 Bad Request` - ошибка валидации
- `404 Not Found` - событие не найдено

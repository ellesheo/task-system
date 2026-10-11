Проектная тема: сервис задач.

Система из нескольких сервисов, которая запускается через Docker Compose.
Пользователь создаёт пользователей и задачи, задачи сохраняются в PostgreSQL,
при создании задачи отправляется уведомление.

Сервисы, использованные в работе:
- gateway - API Gateway (единый вход, порт 8080), направляет запросы в сервисы
- users-service - пользователи (создать, получить), хранит их в PostgreSQL
- tasks-service - задачи (создать, получить), хранит их в PostgreSQL
  при создании задачи обращается к users-service и notifications-service по HTTP
- notifications-service - принимает уведомление и пишет его в лог
- db - PostgreSQL (одна база app на всю систему), данные в volume db_data

## Технологии
- .NET 9 (ASP.NET minimal API)
- PostgreSQL 16
- Entity Framework Core (Npgsql)
- YARP (reverse proxy для API Gateway)
- Docker, Docker Compose

## Запуск
```
docker compose up --build
```
Проверить сервисы:
```
docker compose ps
```
Логи:
```
docker compose logs
```
Остановить:
```
docker compose down
```

Все запросы идут через API Gateway на http://localhost:8080

1. Создать пользователя:
```
curl -X POST http://localhost:8080/users -H "Content-Type: application/json" -d "{\"name\":\"Алина\",\"email\":\"alina@example.com\"}"
```
2. Создать задачу для пользователя:
```
curl -X POST http://localhost:8080/tasks -H "Content-Type: application/json" -d "{\"title\":\"Сделать проект\",\"completed\":false,\"userId\":1}"
```
3. Получить задачи:
```
curl http://localhost:8080/tasks
```

Примеры запросов также в файле requests/requests.http

Сохранение данных
Данные базы хранятся в volume db_data. После docker compose down и docker compose up
созданные пользователи и задачи остаются.

Архитектура
Схемы C4 (Context, Container, Component) в папке docs.

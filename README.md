# MmProxy - Mattermost ↔ n8n Proxy

Минимальный прокси-сервис для интеграции Mattermost с n8n через WebSocket и HTTP API.

## Описание

MmProxy подключается к Mattermost через WebSocket, получает события (DM и треды), форвардит их в n8n webhook, 
и предоставляет HTTP API для отправки ответов обратно в Mattermost.

## Возможности

- ✅ WebSocket подключение к Mattermost с автопереподключением
- ✅ Фильтрация событий: DM и треды
- ✅ Форвард событий в n8n webhook с секретом
- ✅ HTTP API для ответа в треды
- ✅ HTTP API для получения тредов
- ✅ Healthcheck endpoint
- ✅ Docker support
- ✅ CI/CD через GitHub Actions

## Быстрый старт

### Локальная разработка

1. Клонируйте репозиторий:
```bash
git clone <repo-url>
cd hr-hahathon-smooth-operator
```

2. Создайте `.env` файл (используйте `.env.example` как шаблон):
```bash
cp .env.example .env
# Отредактируйте .env с вашими настройками
```

3. Запустите проект:
```bash
cd src/MmProxy
dotnet run
```

4. Проверьте healthcheck:
```bash
curl http://localhost:8080/health
```

### Docker

```bash
# Собрать образ
docker build -t mmproxy .

# Запустить контейнер
docker run -d \
  --name mmproxy \
  -p 8080:8080 \
  --env-file .env \
  mmproxy
```

## Деплой

См. [DEPLOYMENT.md](./DEPLOYMENT.md) для подробной инструкции по настройке CI/CD и деплою на VM.

## Переменные окружения

| Переменная | Описание | Обязательно |
|-----------|----------|-------------|
| `MATTERMOST_WS_URL` | WebSocket URL Mattermost | Да |
| `MATTERMOST_API_URL` | REST API URL Mattermost | Да |
| `MATTERMOST_BOT_TOKEN` | Token бота Mattermost | Да |
| `BOT_USER_ID` | ID бота (опционально) | Нет |
| `N8N_INBOUND_WEBHOOK_URL` | Webhook URL n8n | Да |
| `N8N_WEBHOOK_SECRET` | Секрет для webhook | Да |
| `SERVICE_API_KEY` | API ключ для HTTP API | Да |
| `SERVICE_PORT` | Порт сервиса (по умолчанию 8080) | Нет |

## HTTP API

### POST /answer
Отправить ответ в тред Mattermost.

**Headers:**
- `X-API-Key: <SERVICE_API_KEY>`

**Body:**
```json
{
  "channel_id": "...",
  "root_id": "...",
  "message": "..."
}
```

### POST /get_thread
Получить сообщения треда.

**Headers:**
- `X-API-Key: <SERVICE_API_KEY>`

**Body:**
```json
{
  "channel_id": "...",
  "root_id": "...",
  "limit": 50
}
```

### GET /health
Healthcheck endpoint.

## Архитектура

```
Mattermost (WebSocket) 
    ↓ events (posted)
MmProxy 
    ↓ webhook (HTTP POST)
n8n
    ↓ commands (HTTP POST)
MmProxy → Mattermost (REST API)
```

## Технологический стек

- .NET 9.0
- ASP.NET Core Minimal API
- Websocket.Client
- Docker
- GitHub Actions

## Документация

- [PRD](./prd.md) — Product Requirements Document
- [План реализации](./prd_plan.md) — Plan реализации по этапам
- [Deployment](./DEPLOYMENT.md) — Инструкции по деплою
- [Human Tasks](./human_tasks.md) — Задачи для ручной настройки

## Лицензия

MIT

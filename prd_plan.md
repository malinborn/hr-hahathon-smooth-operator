# PRD Plan: Mattermost ↔ n8n Proxy (Green Zone)

## Фокус и принципы
- **Green zone**: минимальный продукт, быстрый результат, простая архитектура.
- **Без распила задач**: делим только на логичные крупные этапы.
- **Без лишнего функционала**: делаем только то, что описано в PRD (см. prd.md).

## Этапы реализации
### Этап 1. WebSocket к Mattermost ✅
- Постоянный WebSocket к MM (events: `posted`).
- Автопереподключение с backoff 1→30 сек.
- Фильтрация событий: только DM (`channel_type = D`) и посты с `root_id` (треды).
- **Итог:** Реализовано в `src/MmProxy/Services/MattermostWebSocketClient.cs`. Тестировано подключение к MM. Тест-кейсы: TC-1.1–1.8 (подключение, reconnect, фильтрация DM/тредов, health endpoint).

### Этап 2. Докеризация и CI/CD на сервер ✅
- Dockerfile, healthcheck, минимальные логи.
- GHCR образ; compose на ВМ; GitHub Actions с автодеплоем (appleboy/ssh-action).
- **Итог:** Реализованы Dockerfile (multi-stage build), .dockerignore, docker-compose.yaml, GitHub Actions workflow (.github/workflows/deploy.yml). Созданы документы: DEPLOYMENT.md, .github/SETUP.md, обновлён README.md.

### Этап 3. Форвард событий в n8n ✅
- HTTP POST на `N8N_INBOUND_WEBHOOK_URL` с минимальным payload (см. prd.md).
- Заголовок `X-Webhook-Secret` обязателен.
- **Итог:** Реализован сервис `N8nWebhookForwarder`, который форвардит события из WebSocket в n8n. Добавлены модели `N8nWebhookPayload`, `N8nUser`, `N8nFile`. Настроен HttpClient с retry политикой для Mattermost API. События автоматически форвардятся при получении DM или сообщений в тредах.

### Этап 4. HTTP API для n8n ✅
- `POST /answer` — ответ в тред по `channel_id + root_id`.
- `POST /get_thread` — корневой пост и реплаи, `limit`, `order`.
- Авторизация всех запросов: `X-API-Key: <SERVICE_API_KEY>` → 401 при неверном ключе.
- **Итог:** Реализованы endpoints `/answer` и `/get_thread` с авторизацией через `X-API-Key`. Создан `MattermostApiService` для работы с Mattermost REST API, `ApiKeyAuthMiddleware` для проверки API ключа. Добавлена поддержка DM (Direct Messages). Документация в `API.md` и примеры в `api-examples.http`.

### Этап 5. Нефункциональные требования
- HTTP к MM: таймаут 10с; ретраи при 5xx/сетевых (до 3 попыток).
- Простые логи в stdout (info|error).
- Конфигурация через ENV: `MATTERMOST_WS_URL`, `MATTERMOST_API_URL`, `MATTERMOST_BOT_TOKEN`, `BOT_USER_ID?`, `N8N_INBOUND_WEBHOOK_URL`, `N8N_WEBHOOK_SECRET`, `SERVICE_API_KEY`, `SERVICE_PORT` (8080).

## Этап 6 — Расширение (после MVP)
- **DM ответы** в `POST /answer` по `user_id`/`username` (опционально для MVP).
- Мелкие улучшения стабильности и DX (лучшие логи, конфиг флагов и т.п.).

## Архитектура (минимальная)
- **Inbound**: WebSocket → Mattermost (events: `posted`).
- **Outbound**: HTTP → n8n webhook (минимальный payload + `X-Webhook-Secret`).
- **Control**: HTTP API → сервис → Mattermost REST (создание постов, чтение треда).

## Технологический стек
- Язык/платформа: .NET 8 (C#), минимальные API (ASP.NET Core).
- WebSocket клиент: `ClientWebSocket`/`Websocket.Client`.
- HTTP к MM и n8n: `HttpClient` + Polly (timeouts/retries).
- Сборка/рантайм: Docker. Деплой: GitHub Actions → GHCR → Ubuntu VM (docker compose).

## Пакеты и артефакты
- Dockerfile, docker-compose.yaml (на ВМ).
- CI/CD workflow (build → push → deploy через `appleboy/ssh-action`).
- .env (на ВМ) с переменными окружения из PRD.

## План работ (0.5–1 день)
- 1) WebSocket к MM: подключение, автопереподключение, фильтрация событий.
- 2) Докеризация и CI/CD на сервер: GHCR образ, compose на ВМ, автодеплой по push.
- 3) Форвард релевантных событий в n8n с `X-Webhook-Secret`.
- 4) HTTP API для n8n: `POST /answer` (тред) и `POST /get_thread` + `X-API-Key`.
- 5) НФТ: ретраи к MM, таймауты, простые логи.
- 6) Smoke‑тесты по критериям приёмки.

## Критерии приёмки (из prd.md)
- ЛС боту → событие уходит в n8n с корректным payload.
- `POST /answer` публикует ответ в тред (по `channel_id + root_id`).
- `POST /get_thread` отдаёт корень и реплаи в нужном порядке, `limit` работает.
- Неверный `X-API-Key` → 401.

## Риски и отсечения
- Вне scope: загрузка файлов (кроме `file_ids`), форматирование, мульти‑тенантность, UI, БД.
- Стабильность WS: закладываем backoff и ретраи, но без сложного оркестратора.

## Следующие шаги после MVP
- Включить DM‑ответы в `POST /answer`.
- Улучшить observability: структурные логи, trace id.
- Хелперы для тестов в n8n (mapping, фильтры, шаблоны).

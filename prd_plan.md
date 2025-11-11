# PRD Plan: Mattermost ↔ n8n Proxy (Green Zone)

## Фокус и принципы
- **Green zone**: минимальный продукт, быстрый результат, простая архитектура.
- **Без распила задач**: делим только на логичные крупные этапы.
- **Без лишнего функционала**: делаем только то, что описано в PRD (см. prd.md).

## Этап 1 — MVP (обязательный минимум)
- **WS‑интеграция с Mattermost**
  - Постоянный WebSocket к MM (events: `posted`), автопереподключение с backoff 1→30 сек.
  - Фильтрация событий: пропускать только DM (`channel_type = D`) и посты с `root_id` (треды).
- **Форвард в n8n**
  - HTTP POST на `N8N_INBOUND_WEBHOOK_URL` с минимальным payload (см. prd.md),
  - Заголовок `X-Webhook-Secret` обязателен.
- **HTTP API сервиса**
  - `POST /answer` — отправить ответ в тред по `channel_id + root_id`.
  - `POST /get_thread` — вернуть корневой пост и реплаи, `limit`, `order`.
  - Авторизация: `X-API-Key: <SERVICE_API_KEY>` → 401 при несовпадении.
- **Нефункциональные требования**
  - HTTP к MM: таймаут 10с; ретраи при 5xx/сетевых (до 3 попыток).
  - Простые логи в stdout (info|error).
- **Конфигурация (ENV)**
  - `MATTERMOST_WS_URL`, `MATTERMOST_API_URL`, `MATTERMOST_BOT_TOKEN`, `BOT_USER_ID?`,
    `N8N_INBOUND_WEBHOOK_URL`, `N8N_WEBHOOK_SECRET`, `SERVICE_API_KEY`, `SERVICE_PORT` (8080).

## Этап 2 — Расширение (после MVP)
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

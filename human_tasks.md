# Human Tasks: Mattermost ↔ n8n Proxy

## 1) Mattermost
- Создать бота или PAT для бота.
- Получить и передать значения:
  - `MATTERMOST_WS_URL` (wss://<host>/api/v4/websocket)
  - `MATTERMOST_API_URL` (https://<host>/api/v4)
  - `MATTERMOST_BOT_TOKEN` (Bot Token или PAT)
  - `BOT_USER_ID` (опционально; чтобы фильтровать собственные сообщения)
- Убедиться, что боту разрешены ЛС и он добавлен в нужные каналы (для тестов — любой канал с тредом).
- Подготовить тестовый тред: получить `channel_id` и `root_id` для проверки `POST /answer` и `POST /get_thread`.

## 2) n8n
- Создать workflow с входящим Webhook для приёма событий из сервиса.
- Сгенерировать и зафиксировать `N8N_WEBHOOK_SECRET`.
- Настроить проверку заголовка `X-Webhook-Secret` в workflow (отклонять, если секрет не совпадает).
- Дать рабочий `N8N_INBOUND_WEBHOOK_URL` (прод/стейдж), доступный из интернета/ВМ.
- На первых порах — выводить входящий payload в Debug для быстрой диагностики.

## 3) Инфраструктура и доступы
- Подготовить Ubuntu‑VM с доступом по SSH.
- Установить Docker и Docker Compose plugin.
- Создать каталог деплоя на ВМ (например, `/opt/mm-proxy`) и открыть порт `8080` (или настроить обратный прокси).
- Создать файл `/opt/mm-proxy/.env` со значениями переменных окружения:
  - `MATTERMOST_WS_URL`
  - `MATTERMOST_API_URL`
  - `MATTERMOST_BOT_TOKEN`
  - `BOT_USER_ID` (опц.)
  - `N8N_INBOUND_WEBHOOK_URL`
  - `N8N_WEBHOOK_SECRET`
  - `SERVICE_API_KEY`
  - `SERVICE_PORT=8080`
- Добавить секреты в GitHub Actions (репозиторий проекта):
  - `SSH_HOST`, `SSH_USER`, `SSH_KEY` — доступ к ВМ.
  - `GHCR_USERNAME`, `GHCR_TOKEN` — PAT с правами packages:read,write для `docker login` на ВМ.

## 4) Секреты и передача
- Сгенерировать `SERVICE_API_KEY` и безопасно передать (или сразу разместить в `.env` на ВМ).
- Все секреты не коммитить в репозиторий. Передавать через секрет‑хранилище (1Password/Vault) или Actions Secrets.

## 5) Доступности и сети
- Убедиться, что ВМ имеет исходящий доступ к:
  - Mattermost API/WebSocket по указанным хостам.
  - n8n webhook URL.
- Если используется wss/https с кастомными сертификатами — подтвердить доверие/маршрутизацию.

## 6) Проверка (ручной smoke‑тест)
- Написать ЛС боту → событие приходит в n8n, секрет валиден.
- Написать сообщение в тред → событие `thread_post` приходит в n8n.
- Вызвать `POST /answer` (тред) → сообщение публикуется в треде.
- Вызвать `POST /get_thread` → возвращаются корневой пост и реплаи в корректном порядке, `limit` работает.
- Отправить запрос к API с неверным `X-API-Key` → получить 401.

## 7) Что передать мне
- Хосты/URL: `MATTERMOST_WS_URL`, `MATTERMOST_API_URL`, `N8N_INBOUND_WEBHOOK_URL`.
- Токены/секреты: `MATTERMOST_BOT_TOKEN`, `N8N_WEBHOOK_SECRET`, `SERVICE_API_KEY` (лучше — уже в `.env` на ВМ).
- Тестовые идентификаторы: `channel_id`, `root_id` (для треда), `user_id`/`username` (для DM — если будем включать).
- Подтверждение готовности ВМ/SSH и созданных GitHub Secrets.

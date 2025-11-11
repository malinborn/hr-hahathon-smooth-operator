 # PRD: Mattermost ↔ n8n Proxy (MVP)

 Коротко: сервис-прокси держит постоянный WebSocket с Mattermost для приёма событий и предоставляет простой HTTP API для n8n: отправка ответа в тред или личку и получение сообщений треда. Без лишней логики.

 ## Цели
 - Ответы в тредах через бота (обязательно для MVP).
 - Личные сообщения (DM) — опционально для MVP.
 - Форвард входящих событий из Mattermost в n8n по вебхуку с обязательной проверкой `X-Webhook-Secret`.

 ## Объем (MVP)
- Постоянный WebSocket к Mattermost Real‑Time API с автопереподключением.
- Форвард событий в n8n (HTTP POST на `N8N_INBOUND_WEBHOOK_URL`) только для:
  - Сообщений в личных диалогах с ботом (channel_type = `D`).
  - Любых постов, у которых есть `root_id` (сообщения в тредах).
- HTTP API для n8n:
  - `POST /answer` — отправить сообщение в тред. DM — optional (если останется время).
  - `POST /get_thread` — получить сообщения треда по `root_id`.

## Минимальная архитектура
- Inbound: WebSocket → Mattermost (events: `posted`).
- Outbound: HTTP POST → `N8N_INBOUND_WEBHOOK_URL` с упрощённым payload.
- Control: HTTP API → наш сервис → Mattermost REST API (создание постов, чтение треда).

## Аутентификация API
- Все входящие запросы в наш HTTP API должны содержать заголовок `X-API-Key: <SERVICE_API_KEY>`.
- 401, если ключ отсутствует/неверный.

## Эндпоинты

### POST /answer
Отправляет сообщение либо в тред, либо в личку.

Вариант 1 — ответ в тред:
```json
{
  "mode": "thread",            // optional: если есть root_id, можно не указывать
  "channel_id": "string",      
  "root_id": "string",         // id корневого поста треда
  "text": "string",            // текст сообщения
  "props": { },                  // optional, произвольные пропсы MM
  "file_ids": ["id1", "id2"]  // optional, если уже есть загруженные файлы
}
```

Вариант 2 — личное сообщение (DM):
Опционально для MVP.
```json
{
  "mode": "dm",                // optional: если задан user_id/username, можно не указывать
  "user_id": "string",         // либо
  "username": "string",        // одно из двух
  "text": "string"
}
```

Ответ (оба варианта):
```json
{
  "ok": true,
  "post_id": "string",
  "channel_id": "string",
  "root_id": "string|null"
}
```

Ошибки: 400 (валидность), 401 (auth), 404 (не найден канал/пользователь/тред), 5xx.

### POST /get_thread
Возвращает корневой пост и список сообщений треда (по умолчанию по возрастанию времени).

Запрос:
```json
{
  "root_id": "string",
  "limit": 50,           // optional, по умолчанию 50
  "order": "asc"        // asc|desc, optional, по умолчанию asc
}
```

Ответ:
```json
{
  "ok": true,
  "root": {
    "id": "string",
    "channel_id": "string",
    "user_id": "string",
    "create_at": 1731240000000,
    "message": "string",
    "root_id": null,
    "props": {},
    "files": []
  },
  "replies": [
    {
      "id": "string",
      "channel_id": "string",
      "user_id": "string",
      "create_at": 1731243600000,
      "message": "string",
      "root_id": "string",
      "props": {},
      "files": []
    }
  ]
}
```

## Входящие события → n8n webhook
Для каждого релевантного post-события отправляем один HTTP POST на `N8N_INBOUND_WEBHOOK_URL`.

Payload (минимально достаточный):
```json
{
  "type": "dm|thread_post",
  "event_ts": 1731240000000,
  "post_id": "string",
  "root_id": "string|null",
  "channel_id": "string",
  "channel_type": "D|O|P",
  "user": { "id": "string", "username": "string" },
  "text": "string",
  "files": [
    { "id": "string", "name": "string" }
  ],
  "raw": {} // optional, оригинальное тело события
}
```
Обязательно: отправляем заголовок `X-Webhook-Secret: <N8N_WEBHOOK_SECRET>`. n8n должен валидировать его и отклонять запросы без корректного секрета.

## Конфигурация (ENV)
- `MATTERMOST_WS_URL` — wss://<host>/api/v4/websocket
- `MATTERMOST_API_URL` — https://<host>/api/v4
- `MATTERMOST_BOT_TOKEN` — токен бота (Personal Access Token или Bot Token)
- `BOT_USER_ID` — optional, чтобы фильтровать собственные сообщения
- `N8N_INBOUND_WEBHOOK_URL` — URL вебхука n8n для входящих событий
- `N8N_WEBHOOK_SECRET` — секрет, который отправляется в заголовке `X-Webhook-Secret`
- `SERVICE_API_KEY` — ключ для авторизации входящих запросов к нашему API
- `SERVICE_PORT` — порт HTTP (по умолчанию 8080)

## Нефункциональные требования (MVP)
- Автопереподключение WS с backoff 1→30 секунд.
- Таймауты HTTP к MM: 10 секунд; 3 попытки при 5xx/сетевых ошибках.
- Простые логи в stdout (info|error), без хранилищ и метрик.

## Критерии приёмки
- ЛС боту → событие приходит в n8n с корректным payload.
- Ответ через `POST /answer` публикуется:
  - в тред по `channel_id + root_id`;
  - Опционально: в ЛС по `user_id`/`username`.
- `POST /get_thread` отдаёт корневой пост и реплаи в нужном порядке, лимит применяется.
- Неверный `X-API-Key` → 401.

## Вне scope (MVP)
- Загрузка файлов и управление вложениями (кроме передачи `file_ids`).
- Форматирование текста/упоминаний (только «как есть»).
- Мульти‑тенантность, ACL, UI, персистентное хранилище.

## CI/CD (GitHub Actions)

- Регистр: GitHub Container Registry (GHCR) — `ghcr.io/<owner>/<repo>:<sha|latest>`.
- Pipeline: build → push → deploy.
- Deploy: `appleboy/ssh-action` на Ubuntu‑VM, команды `docker compose pull`, `down`, `up -d`.

### Workflow (минимум)
```yaml
name: ci-cd
on:
  push:
    branches: [main]
jobs:
  build:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write
    steps:
      - uses: actions/checkout@v4
      - run: echo "${{ secrets.GITHUB_TOKEN }}" | docker login ghcr.io -u "${{ github.actor }}" --password-stdin
      - run: |
          IMAGE=ghcr.io/${{ github.repository }}:${{ github.sha }}
          LATEST=ghcr.io/${{ github.repository }}:latest
          docker build -t "$IMAGE" -t "$LATEST" .
          docker push "$IMAGE"
          docker push "$LATEST"
  deploy:
    needs: build
    runs-on: ubuntu-latest
    steps:
      - name: Deploy over SSH
        uses: appleboy/ssh-action@v1.0.3
        with:
          host: ${{ secrets.SSH_HOST }}
          username: ${{ secrets.SSH_USER }}
          key: ${{ secrets.SSH_KEY }}
          script: |
            docker login ghcr.io -u "${{ secrets.GHCR_USERNAME }}" -p "${{ secrets.GHCR_TOKEN }}"
            cd /opt/mm-proxy
            docker compose pull
            docker compose down
            docker compose up -d
```

### Docker Compose (на ВМ)
```yaml
services:
  mm-n8n-proxy:
    image: ghcr.io/OWNER/REPO:latest
    env_file: .env
    ports:
      - "8080:8080"
    restart: unless-stopped
```

### Secrets (в GitHub Actions)
- `SSH_HOST`, `SSH_USER`, `SSH_KEY` — доступ к ВМ.
- `GHCR_USERNAME`, `GHCR_TOKEN` — для `docker login` на ВМ (PAT с правами packages:read,write).

## Срок и объём
- Реализация MVP: 0.5–1 рабочий день.
- Проверка интеграции с одним инстансом Mattermost и одним workflow в n8n.

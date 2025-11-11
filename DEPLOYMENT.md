# Deployment Guide

## Предварительные требования

### GitHub Secrets
Настройте следующие секреты в GitHub repository settings:

- `VM_HOST` — IP-адрес или hostname VM
- `VM_USERNAME` — username для SSH доступа
- `VM_SSH_KEY` — приватный SSH ключ для доступа к VM
- `VM_SSH_PORT` — (опционально) SSH порт, по умолчанию 22

### Настройка VM

1. Установите Docker и Docker Compose на VM:
```bash
# Ubuntu/Debian
sudo apt update
sudo apt install -y docker.io docker-compose-plugin

# Добавьте пользователя в группу docker
sudo usermod -aG docker $USER

# ВАЖНО: После этого нужно перелогиниться для применения изменений
# Выйдите и войдите снова, или используйте:
newgrp docker
```

2. Создайте директорию для проекта:
```bash
sudo mkdir -p /opt/mmproxy
sudo chown $USER:$USER /opt/mmproxy
cd /opt/mmproxy
```

3. Создайте `.env` файл на VM:
```bash
cd /opt/mmproxy
nano .env
```

> **Примечание:** Файл `docker-compose.yaml` будет автоматически создан на VM при первом деплое через GitHub Actions. Вам нужно создать только `.env` файл.

Добавьте следующие переменные окружения:
```bash
# Mattermost Configuration
MATTERMOST_WS_URL=wss://your-mattermost.com/api/v4/websocket
MATTERMOST_API_URL=https://your-mattermost.com/api/v4
MATTERMOST_BOT_TOKEN=your-bot-token
BOT_USER_ID=optional-bot-user-id

# n8n Configuration
N8N_INBOUND_WEBHOOK_URL=https://your-n8n.com/webhook/...
N8N_WEBHOOK_SECRET=your-webhook-secret

# Service Configuration
SERVICE_API_KEY=your-api-key
SERVICE_PORT=8080
```

## Локальное тестирование

Для локального тестирования Docker образа:

```bash
# Собрать образ
docker build -t mmproxy:test .

# Запустить контейнер
docker run -d \
  --name mmproxy-test \
  -p 8080:8080 \
  --env-file .env \
  mmproxy:test

# Проверить healthcheck
curl http://localhost:8080/health

# Посмотреть логи
docker logs -f mmproxy-test

# Остановить и удалить
docker stop mmproxy-test && docker rm mmproxy-test
```

## CI/CD Pipeline

Workflow автоматически запускается при push в ветки `main` или `master`.

### Этапы деплоя:

1. **Build and Push** — собирает Docker образ и пушит в GHCR
2. **Deploy** — подключается к VM по SSH и:
   - Создает/обновляет `docker-compose.yaml` на VM
   - Пуллит новый образ из GHCR
   - Перезапускает контейнер
   - Проверяет healthcheck

### Ручной запуск

Workflow можно запустить вручную через GitHub UI:
1. Перейдите в Actions
2. Выберите "Build and Deploy"
3. Нажмите "Run workflow"

## Проверка деплоя

После успешного деплоя проверьте статус:

```bash
# На VM
cd /opt/mmproxy

# Статус контейнера
docker compose ps

# Логи
docker compose logs -f

# Healthcheck
curl http://localhost:8080/health
```

## Откат (Rollback)

Если нужно откатиться к предыдущей версии:

```bash
# На VM
cd /opt/mmproxy

# Остановить текущую версию
docker compose down

# Найти предыдущий образ
docker images | grep mmproxy

# Изменить тег в docker-compose.yaml (или запустить предыдущий коммит CI/CD)
# Например: image: ghcr.io/your-repo/mmproxy:main-abc1234

# Запустить с предыдущим образом
docker compose up -d
```

## Troubleshooting

### Контейнер не стартует

```bash
# Проверить логи
docker compose logs

# Проверить переменные окружения
docker compose config
```

### Healthcheck fails

```bash
# Проверить что порт доступен
docker compose exec mmproxy curl http://localhost:8080/health

# Проверить логи приложения
docker compose logs mmproxy
```

### SSH deployment fails

Проверьте:
1. SSH ключ добавлен в GitHub Secrets
2. Пользователь имеет доступ к VM
3. Docker установлен на VM
4. **Пользователь добавлен в группу docker** и перелогинился
5. Директория `/opt/mmproxy` существует и доступна

# GitHub Setup Instructions

## Шаги для настройки CI/CD

### 1. Настройка GitHub Secrets

Перейдите в Settings → Secrets and variables → Actions и добавьте следующие secrets:

#### Required Secrets для деплоя:

1. **VM_HOST**
   - IP-адрес или hostname вашей VM
   - Пример: `192.168.1.100` или `myserver.example.com`

2. **VM_USERNAME**
   - Username для SSH доступа к VM
   - Пример: `deploy` или `ubuntu`

3. **VM_SSH_KEY**
   - Приватный SSH ключ для доступа к VM
   - Должен быть в формате RSA/ED25519
   - Пример генерации ключа:
     ```bash
     ssh-keygen -t ed25519 -C "github-actions-deploy" -f ~/.ssh/deploy_key
     # Скопируйте содержимое ~/.ssh/deploy_key (приватный ключ)
     # Добавьте публичный ключ на VM: ~/.ssh/authorized_keys
     ```

4. **VM_SSH_PORT** (опционально)
   - SSH порт, если отличается от 22
   - По умолчанию: `22`

### 2. Подготовка VM

На целевой VM выполните:

```bash
# Создайте директорию для проекта
sudo mkdir -p /opt/mmproxy
sudo chown $USER:$USER /opt/mmproxy
cd /opt/mmproxy

# Создайте .env файл с переменными окружения
# Примечание: docker-compose.yaml будет создан автоматически при первом деплое
```

### 3. Проверка настройки

После push в `main` или `master` ветку:

1. Перейдите в Actions
2. Проверьте что workflow "Build and Deploy" запустился
3. Убедитесь что оба job'а (build-and-push и deploy) выполнились успешно

### 4. Ручной запуск деплоя

Для тестирования можно запустить workflow вручную:

1. Перейдите в Actions
2. Выберите "Build and Deploy"
3. Нажмите "Run workflow"
4. Выберите ветку
5. Нажмите "Run workflow"

## Troubleshooting

### Ошибка: "Permission denied (publickey)"

Проверьте:
- Приватный ключ скопирован полностью (включая начало и конец)
- Публичный ключ добавлен в `~/.ssh/authorized_keys` на VM
- Права на файлы: `chmod 600 ~/.ssh/authorized_keys`

### Ошибка: "docker: command not found"

Docker не установлен на VM:
```bash
sudo apt update
sudo apt install -y docker.io docker-compose-plugin
```

### Ошибка: "permission denied while trying to connect to the Docker daemon socket"

Пользователь не добавлен в группу docker. Исправьте это:
```bash
sudo usermod -aG docker $USER
# ВАЖНО: После этого нужно перелогиниться или использовать:
newgrp docker
```

### Ошибка: "No such file or directory: /opt/mmproxy"

Создайте директорию на VM:
```bash
sudo mkdir -p /opt/mmproxy
sudo chown $USER:$USER /opt/mmproxy
```

### Образ не пушится в GHCR

Проверьте:
1. Package permissions в GitHub (Settings → Actions → General)
2. Workflow permissions (Read and write permissions)
3. Что репозиторий не private (или настроена видимость для GHCR)

## Дополнительные настройки

### Notifications

Добавьте Slack/Discord webhook для уведомлений о деплоях:

```yaml
- name: Notify on success
  if: success()
  uses: ...
```

### Multiple Environments

Для staging/production окружений создайте отдельные workflows или используйте environments в GitHub.

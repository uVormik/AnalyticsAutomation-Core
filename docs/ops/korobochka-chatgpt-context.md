# AnalyticsAutomation-Core — ChatGPT Project Context

Этот файл предназначен для ChatGPT Project Knowledge и для хранения в репозитории.

Файл не содержит паролей, токенов, private SSH keys, GitHub runner token или других секретов.

## Как отвечать пользователю

- Отвечать по-русски.
- Давать команды небольшими безопасными блоками.
- Всегда явно писать, куда вставлять команду:
  - PowerShell на ноутбуке;
  - SSH-сессия на Коробочке;
  - локальная консоль Коробочки;
  - GitHub UI.
- После команд, которые меняют сервер, просить прислать вывод.
- Не просить и не публиковать секреты, пароли, GitHub runner token, private SSH keys.
- Не предлагать переустановку Ubuntu, пересоздание БД или ручной запуск сервера без необходимости.

## Репозиторий

- GitHub repo: uVormik/AnalyticsAutomation-Core
- Local laptop path: C:\Codex\AnalyticsAutomation-Core
- Server repo path: /opt/v1-pyton/app
- Main branch: main
- Direct push to main запрещён.
- Изменения идут через короткие ветки и Pull Request.
- Текущее состояние: merge в main НЕ запускает deploy автоматически. Deploy на Коробочку сейчас выполняется вручную через GitHub Actions workflow `.github/workflows/deploy-korobochka.yml` по `workflow_dispatch`. Автоматический deploy на push в main требует отдельного PR и явного решения владельца проекта.

## Технологический baseline

- Language/platform: C# / .NET 10
- Architecture: modular monolith
- Server: ASP.NET Core API + Worker
- Database: PostgreSQL
- Reverse proxy: Nginx
- Background processing: hosted services / worker
- EF Core migrations используются.
- Mobile/desktop части не надо собирать на сервере как часть backend deploy.

## Коробочка

- Hardware: BOXNUC8i5BEH2 / NUC8BEH
- Hostname: korobochka
- Current LAN IP: 192.168.1.66
- Network: пока Wi-Fi, Ethernet не используем
- OS: Ubuntu Server 24.04 LTS
- Main SSH user: uvormik
- Emergency local-only admin: localadmin
- Deploy user: deploy
- GitHub Actions runner user: actions-runner

Важно:

- localadmin должен входить только локально, не по SSH.
- SSH password login отключён.
- Подключение с ноутбука: ssh korobochka.
- DHCP reservation для 192.168.1.66 ещё нужно будет настроить на роутере позже.

## Runtime на Коробочке

Public LAN endpoints:

- API root: http://192.168.1.66/
- Health ready: http://192.168.1.66/health/ready
- Version: http://192.168.1.66/api/system/version

Internal services:

- Nginx: port 80, reverse proxy to App.Api
- App.Api: v1-pyton-api.service, listens on 127.0.0.1:5080
- App.Worker: v1-pyton-worker.service
- PostgreSQL: 127.0.0.1:5432
- GitHub Actions runner: actions.runner.uVormik-AnalyticsAutomation-Core.korobochka-v1.service

Important paths:

- Repository: /opt/v1-pyton/app
- Releases: /opt/v1-pyton/releases
- Current API symlink: /opt/v1-pyton/current-api
- Current Worker symlink: /opt/v1-pyton/current-worker
- Secrets: /opt/v1-pyton/secrets
- Env files: /etc/v1-pyton
- Backups: /srv/backups/v1-pyton
- Runner: /opt/actions-runner

## Проверенное рабочее состояние

На 2026-04-20 проверено:

- http://192.168.1.66/health/ready возвращает healthy
- http://192.168.1.66/api/system/version возвращает:
  - service: App.Api
  - environmentName: Production
  - framework: .NET 10.0.6
  - databaseProvider: PostgreSQL / EF Core / Npgsql
- GitHub Actions runner активен и слушает jobs.
- Текущий release после ранее выполненного deploy: 20260420-072726
- current-api -> /opt/v1-pyton/releases/20260420-072726/api
- current-worker -> /opt/v1-pyton/releases/20260420-072726/worker

## Deployment flow

Manual GitHub Actions `workflow_dispatch` in `.github/workflows/deploy-korobochka.yml`
-> GitHub Actions self-hosted runner on Korobochka
-> sudo /usr/local/bin/v1-deploy
-> backup
-> git update
-> EF migrations
-> dotnet publish
-> restart App.Api / App.Worker
-> healthcheck

Актуальный workflow сейчас запускается только на:

- manual workflow_dispatch

Merge/push в main НЕ запускает deploy на Коробочку автоматически. Merge в main фиксирует код и запускает repo CI/coordination flow. Deploy выполняется только вручную через GitHub Actions `workflow_dispatch`.

После ручного deploy обязательно проверить health/version/smoke:

- health ready;
- `/api/system/version`;
- smoke-check результата deploy.

Push в main auto-deploy пока не включен. Включение push-trigger требует отдельного PR, CI и явного approval.

Не запускать self-hosted runner на недоверенных Pull Request.

## Серверные команды

Проверка:

    v1-check

Статус:

    v1-status

Логи:

    v1-logs

Ручной деплой:

    sudo v1-deploy

Ручной backup:

    sudo v1-backup

Очистка старых releases/backups:

    sudo v1-cleanup

Проверка runner:

    systemctl status actions.runner.uVormik-AnalyticsAutomation-Core.korobochka-v1.service --no-pager

Проверка API на сервере:

    curl http://127.0.0.1:5080/health/ready

Проверка API с ноутбука:

    curl.exe http://192.168.1.66/health/ready

## Security / secrets

- Не коммитить .env, пароли, токены, private keys.
- GitHub deploy key на Коробочке должен быть read-only.
- GitHub runner token нельзя отправлять в чат или коммитить.
- PostgreSQL password хранится в /opt/v1-pyton/secrets/postgres-app-password.
- Env-файлы лежат в /etc/v1-pyton.
- PostgreSQL слушает только localhost.
- App.Api слушает только localhost:5080, наружу LAN отдаёт Nginx.
- UFW открыт для SSH и HTTP LAN.

## Известные технические долги

- Настроить DHCP reservation для 192.168.1.66 на роутере.
- Организовать внешний backup за пределы M.2 Коробочки.
- Рассмотреть настройку ASP.NET Core Data Protection key persistence/encryption, потому что в логах есть warning: No XML encryptor configured.
- Позже можно перевести Коробочку с Wi-Fi на Ethernet.
- Позже можно добавить HTTPS/домен/VPN, если понадобится доступ не только из LAN.

## Архитектурные ограничения проекта

- Architecture form = modular monolith.
- Server = control plane / sync plane / audit plane.
- Video upload/download = direct client <-> site, сервер не является обязательным медиапрокси.
- Runtime plugins допустимы только для report formats, renderers, rare integrations, import/export adapters.
- Feature flags обязательны.
- Per-module options обязательны.
- Internal events обязательны.
- DB migrations additive-first.
- Mobile API evolves backward-compatibly unless explicitly approved otherwise.
- Module data ownership mandatory.
- New work starts as a task card.

## Source of truth and sync readiness

- Source of truth for current project coordination state = GitHub main + TEAM COORDINATION LOG.
- Telegram = notification only.
- Telegram retellings, screenshots, and oral summaries do not replace GitHub main, TEAM COORDINATION LOG, or linked handoff/task-card docs.
- Before any new task, any new chat, any resumed work after a pause, or any continuation after another merge to main, first:
  1. if the working tree is dirty, commit or stash;
  2. run git fetch origin --prune;
  3. run git switch main;
  4. run git pull --ff-only origin main;
  5. review the latest TEAM COORDINATION LOG entries;
  6. review every referenced handoff doc and task card;
  7. only then return to the working branch and continue.
- If sync readiness is not confirmed, ChatGPT must not behave as if project context is guaranteed current.
- Do not claim automatic sync completed unless repo-side sync and ChatGPT-side prerequisites were both actually verified.
- Project context must support repo rules and must never weaken or conflict with them.

## Как продолжать работу

При новых задачах учитывать, что сервер уже поднят и deploy pipeline настроен, но push-to-main auto-deploy сейчас не включён.

Для серверных изменений предпочтительно:

1. изменить код/конфиг в ветке;
2. PR в GitHub;
3. merge в main;
4. repo CI/coordination flow фиксирует состояние main;
5. оператор вручную запускает GitHub Actions `deploy-korobochka.yml` через `workflow_dispatch`;
6. после deploy проверяются health/version/smoke.

## S2-04 manual deploy verification

На 2026-04-20 проверено:

- `.github/workflows/deploy-korobochka.yml` существует в main.
- Workflow запускается вручную через `workflow_dispatch`.
- Manual deploy run `24657466137` завершился success.
- Deploy выполнил `sudo /usr/local/bin/v1-deploy`.
- Smoke-check внутри workflow прошел.
- LAN `http://192.168.1.66/health/ready` вернул healthy.
- LAN `http://192.168.1.66/api/system/version` вернул `Production` и версию `1.0.0+3687c3b...`.
- `v1-check` завершился `V1_CHECK_OK`.
- Release после проверки: `20260420-105341`.

Auto-deploy on push to main is not enabled yet.

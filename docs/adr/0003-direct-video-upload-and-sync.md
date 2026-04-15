# ADR 0003 — Direct Video Upload and Sync

- Status: Accepted
- Date: 2026-04-15

## Context
Главная бизнес-идея проекта — не превращать сервер в обязательный медиапрокси.
Нужно рано зафиксировать, как именно проходят upload/download, что делает сервер и как работает online/offline поведение.

## Decision
1. Видео upload/download идут напрямую между клиентом и сайтом:
   - upload = direct client -> site
   - download = direct site -> client
2. Сервер работает как control plane:
   - pre-upload check
   - pre-download authorization
   - upload/download receipts
   - sync
   - duplicate registry
   - fraud suspicion
   - incidents
   - audit
   - reports foundation
   - future 1C foundation
3. При online-сервере клиент обязан сделать PreUploadCheck до отправки видео на сайт.
4. После прямой загрузки на сайт клиент обязан отправить UploadReceipt на сервер.
5. Для online download клиент обязан:
   - запросить CreateDownloadIntent
   - скачать видео напрямую с сайта
   - отправить DownloadReceipt на сервер
6. Offline mode ограничен:
   - вход только под последней активной учеткой
   - доступен только ограниченный функционал, связанный с видео и необходимого для него минимума
   - абсолютная блокировка дублей в offline не гарантируется
7. При offline upload клиент хранит PendingSyncItem и отправляет синхронизацию после восстановления связи.
8. Сервер хранит receipts, sync state, duplicate registry, incidents и audit trail.

## Rationale
- Такой подход снижает серверную нагрузку и сетевую сложность.
- Сервер сохраняет бизнес-смысл и контроль, но не становится обязательным прокси для видео-байтов.
- Online/offline ограничения становятся явными и не “додумываются” на стороне клиентов.

## Consequences
- Backend API строится вокруг control-plane сценариев, а не вокруг проксирования медиа.
- UploadReceipt, DownloadReceipt, PendingSyncItem и sync — обязательные элементы модели.
- Интеграция с сайтом должна быть оформлена через backend abstraction/gateway, а не через случайные вызовы из разных мест.
- Nginx/API лимиты на payload должны соответствовать control-plane запросам, а не размеру видеофайлов.

## Non-goals
- Серверное хранение и потоковое проксирование всех видео по умолчанию.
- Offline download.
- Полноценная интеграция с 1C в рамках этого ADR.
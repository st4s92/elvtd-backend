# ELVTD Backend (.NET API)

> Zentrale API der ELVTD Finance Plattform — verwaltet Accounts, Copy-Trading, Server-Infrastruktur und orchestriert die Go Bridges über RabbitMQ.

## Architektur-Überblick

```
┌──────────────┐   HTTP    ┌──────────────────────────────────────────┐
│  Frontend    │──────────▶│            Backend (.NET 9)              │
│  (Symfony)   │           │                                          │
└──────────────┘           │  ┌─────────┐  ┌──────────┐  ┌────────┐ │
                           │  │Handlers │→ │Usecases  │→ │Repos   │ │
┌──────────────┐   HTTP    │  │(API)    │  │(Logik)   │  │(DB)    │ │
│  cTrader     │──────────▶│  └─────────┘  └──────────┘  └────────┘ │
│  Copier      │           │                                          │
└──────────────┘           │  ┌──────────────────────────────────┐   │
                           │  │  RabbitMQ Publisher/Consumer     │   │
┌──────────────┐  RabbitMQ │  │  (Heartbeats, Install, Restart) │   │
│  Go Bridges  │◀─────────▶│  └──────────────────────────────────┘   │
│  (Windows    │           │                                          │
│   VMs)       │           │  ┌───────────┐  ┌───────────┐          │
└──────────────┘           │  │  MySQL 8   │  │ RabbitMQ  │          │
                           │  │  (Docker)  │  │ 3.13      │          │
                           │  └───────────┘  └───────────┘          │
                           └──────────────────────────────────────────┘
```

## Rolle im ELVTD-Ökosystem

| Komponente | Repo | Aufgabe |
|------------|------|---------|
| Frontend | `elvtd-frontend` | Web-UI, Charts, Live-Trading |
| **Backend (dieses Repo)** | `elvtd-backend` | Datenhaltung, Business-Logik, Orchestrierung |
| Tools | `elvtd-tools` | Go Bridges, MT4/MT5 Installer, cTrader Copier |

Das Backend ist das **Gehirn** der Plattform:
- Speichert alle Accounts, Orders, Server, Copy-Trading-Beziehungen
- Orchestriert die Go Bridges (Install/Restart/Delete über RabbitMQ)
- Empfängt Heartbeats der VMs und trackt deren Gesundheit
- Verwaltet cTrader OAuth Tokens
- Stellt REST API für Frontend und cTrader Copier bereit

## Tech Stack

- **.NET 9.0** (C#, Minimal APIs)
- **MySQL 8.0.28** (Pomelo EF Core)
- **RabbitMQ 3.13** (Management UI auf Port 15672)
- **Entity Framework Core 9.0** (Code-First, Migrations)
- **Docker Compose** (4 Container: App, MySQL, RabbitMQ, Nginx)
- **cTrader.OpenAPI.Net 1.4.4** (OAuth + Account-Management)
- **Google Gemini 2.0 Flash** (AI Trading-Assistent)
- **BCrypt** (Passwort-Hashing)
- **Swagger/OpenAPI** (Dev-Dokumentation)

## Clean Architecture

```
Presentation/          # API-Schicht
├── Routes/            # Endpoint-Definitionen (Minimal API)
├── Handlers/          # Request-Handler (Controller-Äquivalent)
├── Messaging/         # RabbitMQ Consumer (Background Services)
└── Rest.cs            # DI Setup, Middleware, CORS

Application/           # Business-Logik
└── Usecases/          # TraderUsecase, CtraderUsecase, AiUsecase, UserUsecase

Infrastructure/        # Datenzugriff + externe Systeme
├── Repositories/      # EF Core Repositories (Generic CRUD)
└── Messaging/         # RabbitMQ Connection + Publisher

Model/                 # Domain-Entities + DTOs
Helper/                # AppDbContext, Utilities
```

## Datenbank-Modell

| Entity | Beschreibung | Wichtige Felder |
|--------|-------------|-----------------|
| **Account** | Trading-Account (MT4/MT5/cTrader) | account_number, platform_name, balance, equity, access_token, refresh_token, server_status |
| **Server** | Windows VM (Hyper-V) | server_ip, cpu_usage, ram_usage, active_terminals, uptime, status |
| **ServerAccount** | Account↔Server Zuordnung | server_id, account_id, installation_path, platform_pid |
| **Order** | Trade-Historie | order_ticket, order_symbol, order_lot, order_profit, master_order_id |
| **ActiveOrder** | Offene Positionen | account_id, master_order_id, order_symbol, order_lot |
| **MasterSlave** | Copy-Trading Beziehung | master_id, slave_id |
| **MasterSlavePair** | Symbol-Mapping im Copy-Trade | master_symbol, slave_symbol |
| **MasterSlaveConfig** | Copy-Trading Einstellungen | multiplier, copy_tolerance, status |
| **SymbolMap** | Broker-übergreifende Symbol-Übersetzung | broker_name, broker_symbol, canonical_symbol |
| **User** | Plattform-Benutzer | name, email, password, role_id |
| **AiChatSession/Message** | AI-Chat Historie | user_id, title, role, content, tokens_used |

Alle Entities haben Soft-Delete (`DeletedAt`) und Audit-Timestamps (`CreatedAt`, `UpdatedAt`).

## RabbitMQ Messaging

### Backend → Go Bridges (Published)

| Queue/Exchange | Routing | Beschreibung |
|---------------|---------|-------------|
| `platform.create` | Direct Queue | Account auf VM installieren |
| `platform.restart` | Direct Queue | Terminal neustarten |
| `platform.delete` | Direct Queue | Terminal löschen |
| `mt5.exchange` (Topic) | `mt5.receive.packet.{server}.{account}` | MT5-Befehle an spezifischen Terminal |
| `ctrader.exchange` (Topic) | `ctrader.receive.packet.{ctraderId}` | cTrader-Befehle an spezifischen Account |
| `ctrader.manage.account` | Direct Queue | cTrader Account-Management |

### Go Bridges → Backend (Consumed)

| Queue | Consumer | Beschreibung |
|-------|----------|-------------|
| `worker.heartbeat` | ServerHeartbeatConsumer | VM-Status (CPU, RAM, Uptime, Terminals) |
| `platform.created` | ServerPlatformCreatedConsumer | Installation erfolgreich (Pfad, PID) |
| `platform.restarted` | ServerPlatformRestartedConsumer | Terminal neugestartet |
| `platform.deleted` | ServerPlatformDeletedConsumer | Terminal gelöscht |

### Message-Flow: Account-Installation

```
Frontend: POST /api/trader/account
  → Backend: TraderHandler.AddAccount()
  → Backend: RabbitMqJobPublisher.PublishCreateJob()
  → RabbitMQ Queue: platform.create
  → Go Bridge: main.go consumer
  → Bridge: MT4Installer.Install() oder MT5Installer.Install()
  → Bridge: Publish result → platform.created
  → Backend: ServerPlatformCreatedConsumer
  → Backend: Update ServerAccount (path, PID, status)
```

## REST API Endpoints

### Accounts
```
POST   /api/trader/account                    — Account anlegen
GET    /api/trader/account/paginated          — Accounts auflisten (Filter, Pagination)
GET    /api/trader/account/{id}/detail        — Account-Details (Balance, Equity)
PUT    /api/trader/account/{id}               — Account aktualisieren
DELETE /api/trader/account/{id}               — Account löschen
POST   /api/trader/account/{id}/install       — Installation triggern
POST   /api/trader/account/{id}/restart       — Terminal neustarten
```

### Orders
```
GET    /api/trader/orders/paginated           — Orders auflisten (offen/geschlossen)
POST   /api/trader/orders                     — Order anlegen
DELETE /api/trader/orders/active-order/{id}    — Position schließen
PUT    /api/trader/bridge/slave-order         — Slave-Order Callback (von Bridge)
```

### Copy-Trading
```
POST   /api/trader/master-slave               — Beziehung anlegen
GET    /api/trader/master-slave/paginated     — Beziehungen auflisten
POST   /api/trader/master-slave-config        — Multiplier setzen
GET    /api/trader/copy-relations             — Alle Relationen (für cTrader Copier)
GET    /api/trader/symbol-map                 — Symbol-Mapping (für cTrader Copier)
```

### Server-Management
```
GET    /api/trader/servers                    — Alle Server
GET    /api/trader/servers/{id}               — Server-Details
POST   /api/trader/servers/reassign-stale     — Stale Accounts neu zuweisen
```

### cTrader OAuth
```
GET    /api/ctrader/auth                      — OAuth-URL generieren
POST   /api/ctrader/auth/callback             — Code → Token tauschen
POST   /api/ctrader/account/manual            — Account manuell anlegen
GET    /api/ctrader/bridge/accounts           — Accounts mit Tokens (für Bridge)
GET    /api/ctrader/token/{accountId}         — Token abrufen/refreshen
```

### AI Trading-Assistent
```
POST   /api/ai/chat/session                   — Chat-Session starten
GET    /api/ai/chat/session/{id}/messages     — Nachrichten abrufen
POST   /api/ai/chat/message                   — Nachricht senden (SSE Streaming)
GET    /api/ai/analysis/{accountId}           — Account-Analyse (Strategie, Risiko)
```

## Docker Setup

```yaml
services:
  app:        # .NET 9 Backend API (Port 5021)
  database:   # MySQL 8.0.28 (Port 3306)
  message:    # RabbitMQ 3.13 (Port 5672 AMQP, 15672 Management UI)
  frontend:   # Nginx (Port 80, dient SPA aus /dist)
```

### Deployment

```bash
ssh root@65.108.60.88
cd /root/elvtd-backend
git pull origin master
docker compose build app
docker compose up -d app
```

### Debugging

```bash
docker logs elvtd_backend --tail 50          # Letzte Logs
docker logs elvtd_backend -f                 # Live-Logs
docker exec -it elvtd_mysql mysql -u root -p # MySQL Shell
```

## Bekannte Probleme & Fixes

- **SIGSEGV 139:** RabbitMQ `IModel` ist nicht thread-safe. Jeder Consumer hat eigenen Channel. Publisher nutzt `lock`.
- **Async void Crash:** Alle `consumer.Received += async` Callbacks haben try-catch. Ohne → SIGSEGV.
- **Message-Stau nach Crash:** Heartbeats stauen sich in RabbitMQ. Beim Neustart werden alle sofort verarbeitet → DB Connection Pool Limit beachten.

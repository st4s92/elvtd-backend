# elvtd-backend Restore Plan

Disaster Recovery Guide für das elvtd-backend System.

**Zuletzt aktualisiert:** 27.03.2026

---

## Server-Informationen

| Komponente | Details |
|-----------|---------|
| **Server** | Hetzner Cloud (65.108.60.88) |
| **Betriebssystem** | Linux (Debian/Ubuntu) |
| **Orchestrierung** | Docker Compose |
| **Region** | Falkenstein |

---

## Aktuelle Infrastruktur

### Laufende Container

```
elvtd_backend      Go-Applikation              Port 5021
elvtd_frontend     nginx:alpine                Port 80
elvtd_mysql        mysql:8.0.28                Port 3306
elvtd_rabbitmq     rabbitmq:3.13-management    Ports 5672, 15672
```

### Docker Volumes

```
database_mysql      MySQL-Datenbankdaten
rabbitmq_data       RabbitMQ-Persistierung
```

### Backup-Speicherorte

```
/root/backups/mysql/      MySQL Dumps (täglich 3:00 Uhr, 7 Tage Aufbewahrung)
/root/backups/volumes/    Volume Snapshots (täglich 3:30 Uhr, 7 Tage Aufbewahrung)
```

### Monitoring

```
/root/scripts/monitor.sh      Alle 5 Minuten via Cron
/root/scripts/backup-mysql.sh Täglich 3:00 Uhr
/root/scripts/backup-volumes.sh Täglich 3:30 Uhr
```

Telegram Alerts via PineWebsocket Bot bei Container-Ausfall, DB-Fehler, Disk >85%, RAM >90%.

---

## Szenario 1: Ein Container ist ausgefallen

**Symptome:** Ein Service antwortet nicht, andere laufen normal.
**Hinweis:** Das Monitoring startet Container automatisch neu und benachrichtigt per Telegram.

### Manuelle Wiederherstellung

```bash
ssh root@65.108.60.88
cd /root/elvtd-backend

# Status prüfen
docker compose ps

# Container neustarten
docker compose restart <container_name>

# Logs prüfen
docker compose logs -f --tail=50 <container_name>

# Falls Neustart nicht hilft, Container neu bauen
docker compose up -d <container_name> --build
```

---

## Szenario 2: MySQL Datenbank korrupt

**Symptome:** Datenbankfehler, SQL-Fehler in Logs.

### Wiederherstellungsschritte

```bash
ssh root@65.108.60.88
cd /root/elvtd-backend

# 1. Verfügbare Backups anzeigen
ls -lh /root/backups/mysql/

# 2. Alle Container stoppen
docker compose down

# 3. Neuestes Backup auswählen
BACKUP_FILE=$(ls -t /root/backups/mysql/*.sql.gz | head -1)
echo "Verwende: $BACKUP_FILE"

# 4. Nur MySQL starten
docker compose up -d database
sleep 30

# 5. Backup einspielen
gunzip -c $BACKUP_FILE | docker exec -i elvtd_mysql mysql -u denieskb -p'1GY5yM5cizyv1tmI' elvtd_core

# 6. Verifizieren
docker exec elvtd_mysql mysql -u denieskb -p'1GY5yM5cizyv1tmI' -e "SELECT COUNT(*) FROM elvtd_core.orders;"

# 7. Alle Services starten
docker compose up -d
```

---

## Szenario 3: Server komplett ausgefallen (Neuaufbau)

### Schritt 1: Neuen Server bereitstellen

1. **Hetzner Cloud Console:** https://console.hetzner.cloud/
2. Neuen Server: Ubuntu 22.04, CX21+, Falkenstein
3. SSH Key hinzufügen
4. IP-Adresse notieren

### Schritt 2: System einrichten

```bash
ssh root@<neue_ip>

# Updates & Docker installieren
apt update && apt upgrade -y
apt install -y docker.io docker-compose-v2 git curl wget
systemctl start docker && systemctl enable docker
```

### Schritt 3: Repository klonen

```bash
cd /root
git clone git@github.com:st4s92/elvtd-backend.git
cd elvtd-backend
```

### Schritt 4: .env Datei wiederherstellen

Benötigte Variablen:

```
DOC_DB_DRIVER=mysql
DOC_DB_HOST=database
DOC_DB_PORT=3306
DOC_DB_NAME=elvtd_core
DOC_DB_USER=denieskb
DOC_DB_PASSWORD=<aus sicherem Speicher>
COPY_TOLERANCE_SECOND=<Wert>
MAX_SERVER_ACCOUNTS=<Wert>
RABBITMQ_HOST=message
RABBITMQ_PORT=5672
RABBITMQ_USER=<Wert>
RABBITMQ_PASS=<Wert>
GEMINI_API_KEY=<Wert>
```

### Schritt 5: Backups auf neuen Server kopieren

```bash
# Vom alten Server (falls erreichbar)
scp -r root@65.108.60.88:/root/backups /root/backups/

# Oder von lokalem Backup
scp -r /local/backups root@<neue_ip>:/root/backups/
```

### Schritt 6: MySQL Backup einspielen

```bash
cd /root/elvtd-backend

# MySQL starten
docker compose up -d database
sleep 30

# Backup einspielen
BACKUP_FILE=$(ls -t /root/backups/mysql/*.sql.gz | head -1)
gunzip -c $BACKUP_FILE | docker exec -i elvtd_mysql mysql -u denieskb -p'<password>' elvtd_core
```

### Schritt 7: Alle Services starten

```bash
docker compose up -d
docker compose ps
```

### Schritt 8: Monitoring + Backups einrichten

```bash
mkdir -p /root/scripts /root/backups/mysql /root/backups/volumes /root/logs

# Scripts vom Repo oder von einem Backup kopieren
# Cron einrichten:
crontab -e
# */5 * * * * /root/scripts/monitor.sh >> /root/logs/monitor.log 2>&1
# 0 3 * * * /root/scripts/backup-mysql.sh >> /root/logs/backup.log 2>&1
# 30 3 * * * /root/scripts/backup-volumes.sh >> /root/logs/backup.log 2>&1
```

### Schritt 9: DNS/IP aktualisieren

Falls die IP sich geändert hat, alle Referenzen aktualisieren.

### Schritt 10: Verifizierung

```bash
# Backend
curl http://<neue_ip>:5021/health

# Frontend
curl http://<neue_ip>/

# MySQL
docker exec elvtd_mysql mysqladmin ping -u denieskb -p'<password>'

# RabbitMQ
curl http://<neue_ip>:15672/

# Alle Container
docker compose ps
```

---

## Szenario 4: RabbitMQ Daten verloren

### Option A: Aus Backup

```bash
ssh root@65.108.60.88
cd /root/elvtd-backend

docker compose down

# Backup finden
BACKUP_FILE=$(ls -t /root/backups/volumes/rabbitmq*.tar.gz | head -1)

# Volume neu erstellen
docker volume rm elvtd-backend_rabbitmq_data
docker volume create elvtd-backend_rabbitmq_data

# Backup einspielen
MOUNT=$(docker volume inspect elvtd-backend_rabbitmq_data -f '{{.Mountpoint}}')
tar -xzf $BACKUP_FILE -C $MOUNT

docker compose up -d
```

### Option B: Neu aufsetzen

Queues werden vom Backend automatisch erstellt:

```bash
docker compose down
docker volume rm elvtd-backend_rabbitmq_data
docker compose up -d
sleep 20
docker compose logs elvtd_rabbitmq | grep -i "ready"
```

---

## Wichtige Dateien

| Datei | Beschreibung | Kritisch |
|-------|-------------|----------|
| `.env` | Alle Credentials | **JA** |
| `docker-compose.yml` | Container Konfiguration | Ja |
| `nginx.conf` | Frontend Konfiguration | Ja |
| `/root/scripts/*.sh` | Monitoring & Backup Scripts | Ja |

## Zugänge

| System | Zugang |
|--------|--------|
| **GitHub** | github.com/st4s92 |
| **Hetzner Cloud** | console.hetzner.cloud |
| **Telegram Bot** | PineWebsocket Bot (8739222448) |

---

## Notfall-Checkliste

- [ ] Server erreichbar (SSH)
- [ ] Alle 4 Container laufen
- [ ] MySQL antwortet
- [ ] Backend /health OK
- [ ] Frontend erreichbar
- [ ] Monitoring Cron aktiv
- [ ] Backup Cron aktiv
- [ ] DNS korrekt

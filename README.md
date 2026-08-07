# TeamManagement

Ekip/proje/görev yönetimi için JWT tabanlı auth, Kanban panosu, zaman
çizelgesi ve haftalık AI özetleri içeren tam-yığın (full-stack) bir
uygulama.

## Özellikler

- **Auth**: kayıt, giriş, şifre sıfırlama (JWT + ASP.NET Core Identity)
- **Ekip yönetimi**: ekip oluşturma, üye davet etme/kabul etme, roller
- **Proje & görev yönetimi**: proje/görev CRUD, görev ilerleme takibi, hata (bug) kaydı, yorumlar
- **Kanban panosu**: sürükle-bırak görev durumu değişimi (`@dnd-kit`)
- **Zaman çizelgesi (Timeline)**: proje/görev bazlı kronolojik görünüm
- **Gerçek zamanlı bildirimler**: SignalR ile anlık bildirim/güncelleme
- **Haftalık AI özet**: Gemini ile ekip özetleri üretimi + e-posta üzerinden
  haftalık plan/görev güncelleme akışı (IMAP dinleyici + SMTP gönderim)

## Mimari

Backend, Clean Architecture katmanlarına ayrılmış bir .NET çözümü:

```
TeamManagement.Api             → HTTP API, SignalR hub, DI kayıtları (Program.cs)
TeamManagement.Application     → arayüzler (interfaces), DTO'lar
TeamManagement.Domain          → entity'ler, enum'lar
TeamManagement.Infrastructure  → servis implementasyonları, EF Core persistence, background service'ler
TeamManagement.Tests           → unit testler
frontend/                      → React + TypeScript + Vite istemcisi
```

İki ayrı PostgreSQL veritabanı kullanılır: `IdentityConnection` (kullanıcı/kimlik
doğrulama) ve `AppConnection` (ekip/proje/görev verisi).

## Teknoloji Yığını

- **Backend**: .NET, ASP.NET Core Web API, EF Core (PostgreSQL), ASP.NET Core Identity, JWT, SignalR
- **Frontend**: React 19, TypeScript, Vite, Tailwind CSS, Zustand, React Router, `@dnd-kit`, `@microsoft/signalr`, Framer Motion
- **AI**: Google Gemini (haftalık özet ve e-posta içerik ayrıştırma)

## Geliştirme Ortamını Kurma

### 1. Veritabanı

```bash
docker compose up -d
```

Bu, `docker-compose.yml`'de tanımlı local PostgreSQL konteynerini `localhost:5433`'te ayağa kaldırır.

### 2. Backend

`TeamManagement.Api/appsettings.Development.json` dosyasını oluştur (bu dosya
`.gitignore`'da — repo'ya dahil değil) ve aşağıdaki alanları doldur:

```json
{
  "ConnectionStrings": {
    "IdentityConnection": "Host=localhost;Port=5433;Database=...;Username=...;Password=...",
    "AppConnection": "Host=localhost;Port=5433;Database=...;Username=...;Password=..."
  },
  "Jwt": { "Secret": "..." },
  "Gemini": { "ApiKey": "..." },
  "EmailSettings": { "SenderEmail": "...", "SenderPassword": "..." }
}
```

Sonra:

```bash
dotnet ef database update --project TeamManagement.Infrastructure --startup-project TeamManagement.Api --context TeamManagementIdentityDbContext
dotnet ef database update --project TeamManagement.Infrastructure --startup-project TeamManagement.Api --context TeamManagementAppDbContext
dotnet run --project TeamManagement.Api
```

### 3. Frontend

```bash
cd frontend
npm install
npm run dev
```

## Test

```bash
dotnet test
```

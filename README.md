# Plataforma de Eventos Online — Entrega del Reto Técnico

Este repositorio contiene los tres entregables solicitados:

- **A) Diagrama de arquitectura** → [`docs/architecture.md`](docs/architecture.md)
- **B) Backlog y Roadmap** → [`docs/backlog-roadmap.xlsx`](docs/backlog-roadmap.xlsx)
- **C) Código fuente del MVP** (2 APIs .NET + frontend React) → este mismo repo

## Estructura del repositorio

```
├── docs/
│   ├── architecture.md          # Diagrama y sustentación de arquitectura
│   └── backlog-roadmap.xlsx     # Backlog + Roadmap de 6 meses
├── db/
│   ├── init-eventservice.sql    # Script de inicialización de PostgreSQL (fallback sin EF)
│   └── init-notificationservice-README.md
├── backend/
│   ├── EventosPlatform.sln
│   ├── Shared/                  # Contratos de mensajería compartidos
│   ├── EventService/            # API 1: Clean Architecture + DDD
│   │   ├── src/
│   │   │   ├── EventService.Domain
│   │   │   ├── EventService.Application
│   │   │   ├── EventService.Infrastructure
│   │   │   └── EventService.Api
│   │   └── Dockerfile
│   └── NotificationService/     # API 2: Consumidor idempotente
│       ├── src/ (mismas capas)
│       └── Dockerfile
├── frontend/                    # React 18 + TS + Tailwind — pantalla "Registrar Evento"
└── docker-compose.yml           # Orquesta todo: RabbitMQ, Postgres, Mongo, Redis, Mailpit, APIs, frontend
```

## Stack técnico usado

- **Backend:** .NET 9, Clean Architecture + DDD, MediatR (CQRS), FluentValidation, EF Core, MassTransit + RabbitMQ, StackExchange.Redis, MongoDB.Driver, MailKit.
- **Persistencia:** PostgreSQL (EventService — datos relacionales, transacción evento+zonas) y MongoDB (NotificationService — escritura de alto volumen, esquema flexible).
- **Mensajería:** RabbitMQ, con reintentos (backoff exponencial) y Dead Letter Queue automática vía MassTransit.
- **Cache:** Redis (cache-aside en `GET /events`).
- **Frontend:** React 18 + TypeScript + Vite + Tailwind CSS.
- **Seguridad:** JWT (HS256 en el MVP local; en producción se recomienda RS256 vía IdP OIDC), autorización por roles, rate limiting básico, manejo de errores sin fuga de información.

## Cómo ejecutar todo con Docker Compose

Requisitos: Docker Desktop (o Podman) instalado.

```bash
git clone <este-repositorio>
cd reto-eventos
docker compose up --build
```

Servicios expuestos:

| Servicio | URL |
|---|---|
| Frontend (Registrar Evento) | http://localhost:5173 |
| EventService (Swagger) | http://localhost:8080/swagger |
| NotificationService (health) | http://localhost:8081/health |
| RabbitMQ Management UI | http://localhost:15672 (guest/guest) |
| Mailpit (correos capturados) | http://localhost:8025 |
| PostgreSQL | localhost:5432 (eventservice/devpassword) |
| MongoDB | localhost:27017 |
| Redis | localhost:6379 |

> En `docker-compose.yml`, `EventService` corre con `ASPNETCORE_ENVIRONMENT=Development`, lo que dispara `db.Database.Migrate()` automáticamente al iniciar — **no es necesario ejecutar migraciones a mano** para levantar el entorno de demo.

## Generar un JWT de prueba (rol Admin)

El MVP usa una clave simétrica de firma (`Jwt:SigningKey` en `appsettings.json`) para simplificar la demo. Para generar un token de prueba localmente (por ejemplo con `dotnet-jwt-cli`, jwt.io, o un pequeño script), los claims mínimos requeridos son:

```json
{
  "sub": "admin-demo",
  "role": "Admin",
  "iss": "eventos-platform",
  "aud": "eventos-platform-clients"
}
```

Firmar con HS256 usando la misma `SigningKey` configurada en `docker-compose.yml` (`CHANGE_ME_SUPER_SECRET_KEY_MIN_32_CHARS_LONG`). Copiar el token generado a `frontend/.env` en `VITE_DEMO_JWT`.

> En producción, este token lo emitiría un Identity Provider real (Cognito, Keycloak, Auth0) tras un login OIDC — el MVP simplifica este paso según lo indicado en el enunciado ("puede ser fijo para la demo").

## Ejecutar cada parte por separado (sin Docker)

### EventService

```bash
cd backend
dotnet restore EventosPlatform.sln

# Migraciones EF Core (requiere dotnet-ef instalado: dotnet tool install --global dotnet-ef)
cd EventService/src/EventService.Api
dotnet ef migrations add InitialCreate --project ../EventService.Infrastructure --startup-project .
dotnet ef database update --project ../EventService.Infrastructure --startup-project .

dotnet run
```

Requiere PostgreSQL, Redis y RabbitMQ corriendo localmente (pueden levantarse solo esos 3 servicios con `docker compose up rabbitmq postgres-events redis`).

### NotificationService

```bash
cd backend/NotificationService/src/NotificationService.Api
dotnet run
```

Requiere MongoDB, RabbitMQ y un servidor SMTP (Mailpit) corriendo (`docker compose up rabbitmq mongo mailpit`).

### Frontend

```bash
cd frontend
cp .env.example .env   # completar VITE_API_BASE_URL y VITE_DEMO_JWT
npm install
npm run dev
```

## Requisitos de mensajería — cómo se cumplen

- **Formato del mensaje:** `Shared/Contracts/EventCreatedMessage.cs` — incluye `messageId`, `eventId`, `name`, `occurredAt`, `correlationId`, `version`.
- **Idempotencia del consumidor:** `NotificationService.Application/Consumers/EventCreatedConsumer.cs` verifica `ExistsByMessageIdAsync` antes de procesar; `MessageId` tiene índice único en MongoDB.
- **Reintentos:** configurados en `NotificationService.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs` con `UseMessageRetry` (backoff exponencial, 5 intentos).
- **DLQ:** MassTransit mueve automáticamente los mensajes que agotan reintentos a la cola `notification-service-event-created_error`.

## Seguridad — cómo se cumple (bonus)

- JWT + roles (`AdminOnly`, `AnyUser`) en `EventService.Api/Program.cs`.
- Rate limiting global por IP (60 req/min).
- Manejo de errores centralizado que nunca expone stack traces ni detalles de BD.
- Sin logging de tokens/PII (`_logger` solo registra `eventId`, `correlationId`, contadores de reintento).

## Notas y decisiones de diseño

- Se usó **PostgreSQL** (no SQL Server) por ser open-source y sin licenciamiento, alineado con el stack containerizado del reto.
- Se usó **MongoDB** para NotificationService para demostrar el uso deliberado de NoSQL donde el caso de uso lo justifica (documentos de auditoría/notificación de escritura intensiva, sin necesidad de joins).
- El patrón **Outbox** (para atomicidad estricta entre el commit de BD y la publicación del evento) se documenta como recomendación de evolución en `docs/architecture.md`, pero no se implementó en el MVP para mantener el alcance de 2 días — la publicación actual es "best-effort post-commit", suficiente para validar el patrón de mensajería solicitado.

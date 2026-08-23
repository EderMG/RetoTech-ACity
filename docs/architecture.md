# Arquitectura — Plataforma de Eventos Online

## 1. Visión general

La plataforma se diseña como un **sistema de microservicios orientado a eventos**, desplegado en **AWS** (con opción híbrida para cargas on-premise si el cliente lo requiere), pensado para tres propiedades no negociables: **evitar sobreventa** (consistencia), **soportar picos de concurrencia** (preventas/lanzamientos) y **degradar de forma parcial** ante fallas (disponibilidad).

La comunicación entre servicios combina:
- **Síncrona (REST/HTTP + gRPC interno donde aplica):** para operaciones que requieren respuesta inmediata al usuario (login, búsqueda, checkout).
- **Asíncrona (eventos vía broker):** para todo lo que no bloquea la experiencia del usuario y necesita desacoplamiento, reintentos y trazabilidad (notificaciones, generación de tickets, auditoría, BI).

## 2. Microservicios propuestos

| Servicio | Responsabilidad | BD | Comunicación |
|---|---|---|---|
| **IdentityService** | Autenticación/autorización, OIDC/OAuth2, emisión de JWT, gestión de roles | PostgreSQL | Sync (login) |
| **EventService** | CRUD de eventos, zonas, precios, aforos, catálogo | PostgreSQL | Sync (CRUD/búsqueda) + Async (publica `EventCreated`, `EventPublished`) |
| **SearchService** | Búsqueda avanzada de eventos publicados, indexación | Elasticsearch/OpenSearch | Async (consume eventos de EventService), Sync (queries) |
| **InventoryService** | Control de aforo/disponibilidad por zona, reserva temporal de cupos | Redis (locks/contadores) + PostgreSQL (fuente de verdad) | Sync (reserva) + Async (libera cupos por timeout) |
| **OrderService** | Orquesta el flujo de compra/reserva (saga), estado del pedido | PostgreSQL | Sync (checkout) + Async (eventos de orden) |
| **PaymentService** | Integración con PSP externos, reembolsos | PostgreSQL (transacciones) | Sync (autorización) + Async (webhooks del PSP, resultado de pago) |
| **TicketingService** | Genera tickets QR/Barcode, valida check-in | PostgreSQL + S3 (PDFs/QRs) | Async (al confirmarse el pago) + Sync (validación en puerta, incluso offline-first) |
| **NotificationService** | Envío de email/SMS/push/WhatsApp, trazabilidad de envíos | MongoDB | Async (consumidor puro) |
| **UserService** | Perfiles de clientes, promotores, staff | PostgreSQL | Sync |
| **AuditService** | Trazabilidad centralizada de operaciones sensibles | MongoDB / OpenSearch | Async (consume de todos los servicios) |
| **BFF / API Gateway** | Punto de entrada único, rate limiting, agregación | — | Sync |

> Para el **MVP del reto** se construyen `EventService` y `NotificationService`, que son representativos del patrón que se replicaría en el resto de servicios (transacción local + publicación async + consumidor idempotente).

## 3. Diagrama de componentes (MVP + visión completa)

```mermaid
flowchart LR
    subgraph Cliente
        FE[React SPA<br/>Registrar Evento]
    end

    subgraph AWS["AWS / Nube"]
        GW[API Gateway / BFF]

        subgraph EventCtx["Contexto: Eventos"]
            ES[EventService<br/>.NET 9]
            PG1[(PostgreSQL<br/>eventservice)]
            RD[(Redis<br/>cache-aside)]
        end

        subgraph NotifCtx["Contexto: Notificaciones"]
            NS[NotificationService<br/>.NET 9]
            MG[(MongoDB<br/>notification_jobs)]
            SMTP[SMTP / SES]
        end

        MQ{{RabbitMQ / SQS+SNS<br/>Broker de mensajería}}
    end

    FE -->|HTTP + JWT| GW
    GW -->|POST /events<br/>GET /events| ES
    ES -->|read/write| PG1
    ES -->|cache-aside| RD
    ES -->|publica EventCreated| MQ
    MQ -->|consume EventCreated| NS
    NS -->|idempotencia + auditoría| MG
    NS -->|envía correo| SMTP
    MQ -.->|DLQ tras N reintentos| DLQ[(Dead Letter Queue)]
```

## 4. Flujo síncrono: Crear Evento

```mermaid
sequenceDiagram
    actor Admin
    participant FE as React SPA
    participant ES as EventService (API)
    participant DB as PostgreSQL
    participant MQ as RabbitMQ

    Admin->>FE: Completa formulario "Registrar Evento"
    FE->>ES: POST /events (JWT rol Admin)
    ES->>ES: Valida (FluentValidation) + construye aggregate (invariantes de dominio)
    ES->>DB: INSERT Event + Zones (transacción única)
    DB-->>ES: OK
    ES->>MQ: Publica EventCreated (async, no bloquea la respuesta)
    ES-->>FE: 201 Created + EventResponseDto
    FE-->>Admin: Confirmación en pantalla
```

## 5. Flujo asíncrono: Notificación con idempotencia, reintentos y DLQ

```mermaid
sequenceDiagram
    participant MQ as RabbitMQ
    participant NS as NotificationService (Consumer)
    participant MG as MongoDB
    participant SMTP as SMTP/SES

    MQ->>NS: EventCreated {messageId, eventId, ...}
    NS->>MG: ¿existe MessageId?
    alt Ya procesado
        MG-->>NS: Sí
        NS-->>MQ: ACK (descarta duplicado)
    else No procesado
        MG-->>NS: No
        NS->>MG: Insert NotificationJob (status=Received)
        NS->>SMTP: Enviar correo
        alt Envío exitoso
            SMTP-->>NS: OK
            NS->>MG: Update status=Processed
            NS-->>MQ: ACK
        else Falla el envío
            SMTP-->>NS: Error
            NS->>MG: Update status=Failed, retryCount++
            NS-->>MQ: NACK / re-throw
            MQ->>MQ: Reintento con backoff exponencial (hasta 5 veces)
            MQ->>MQ: Tras agotar reintentos → mueve a cola _error (DLQ)
        end
    end
```

## 6. Persistencia: SQL vs NoSQL por microservicio

| Servicio | Motor | Justificación |
|---|---|---|
| EventService | **PostgreSQL** | Datos altamente relacionales (Event ↔ Zones), transacciones ACID necesarias al crear evento + zonas atómicamente, consultas con filtros/rangos que se benefician de índices B-tree. |
| NotificationService | **MongoDB** | Documentos semi-estructurados por notificación, escritura de alto volumen sin necesidad de joins, esquema flexible ante nuevos canales (SMS/push/WhatsApp) sin migraciones. |
| InventoryService (visión completa) | **Redis + PostgreSQL** | Redis para contadores atómicos de aforo de baja latencia bajo alta concurrencia (`DECR`/Lua scripts); PostgreSQL como fuente de verdad durable. |
| SearchService (visión completa) | **OpenSearch/Elasticsearch** | Búsqueda de texto libre, facetas y ranking — no es el fuerte de una BD relacional. |

## 7. Autenticación, autorización y seguridad

- **OIDC/OAuth2** como estándar: un IdentityService actúa como Authorization Server (o se integra con un IdP externo tipo Keycloak/Cognito). Emite **JWT** firmados (RS256 en producción; HS256 simplificado en el MVP local).
- **Autorización por roles** vía claims del JWT: `Admin` puede `POST /events`; `Admin` y `User` pueden `GET /events`.
- **Boundaries de seguridad:**
  - El API Gateway valida el JWT antes de enrutar (defensa en profundidad además de la validación en cada servicio).
  - Cada microservicio valida su propio JWT — nunca confía ciegamente en la capa anterior (zero-trust interno).
  - Prevención de IDOR: los endpoints "por usuario" filtran siempre por el `sub` del token, nunca por un id recibido del cliente sin verificar pertenencia.
- **Manejo de errores seguro:** middleware global de excepciones que nunca expone stack traces ni detalles de infraestructura al cliente (ver `EventService.Api/Program.cs`).
- **Rate limiting:** limitador fijo por IP (60 req/min en el MVP) a nivel de gateway/API — mitigación básica ante abuso/DoS.
- **Logs:** nunca se registran tokens, contraseñas ni PII; solo identificadores técnicos (`correlationId`, `eventId`).

## 8. Resiliencia y alta concurrencia

- **Idempotencia del consumidor** (`MessageId` único indexado) evita efectos duplicados ante redelivery del broker.
- **Reintentos con backoff exponencial** a nivel de mensajería (MassTransit) y **Dead Letter Queue** para mensajes que agotan reintentos, evitando bloquear la cola principal.
- **Cache-aside con Redis** en `GET /events` para absorber picos de lectura sin golpear la BD en cada request.
- **Circuit breaker / retry con Polly** recomendado para llamadas salientes (PSP de pagos, servicios externos) en los servicios de checkout — no bloqueante para el resto del sistema si un proveedor externo degrada.
- **Escalado horizontal:** APIs stateless detrás de un load balancer (ALB) + auto scaling groups / Fargate; los consumidores de cola escalan de forma independiente según profundidad de cola (métricas de RabbitMQ/SQS).

## 9. Arquitectura en la nube (AWS)

```mermaid
flowchart TB
    Users((Usuarios)) --> CF[CloudFront + WAF]
    CF --> ALB[Application Load Balancer]
    ALB --> ECS1[ECS Fargate<br/>EventService]
    ALB --> ECS2[ECS Fargate<br/>NotificationService]
    ECS1 --> RDS[(RDS PostgreSQL<br/>Multi-AZ)]
    ECS1 --> EC[(ElastiCache Redis)]
    ECS2 --> DDB[(DynamoDB / DocumentDB)]
    ECS1 -->|publica| SNS[SNS Topic]
    SNS --> SQS1[SQS: notification-queue]
    SQS1 --> ECS2
    SQS1 -.->|DLQ| SQSDLQ[(SQS DLQ)]
    ECS1 --> S3[(S3: assets/QRs)]
    CW[CloudWatch<br/>Logs + Métricas + Alarmas] -.-> ECS1
    CW -.-> ECS2
```

En la variante **híbrida**, los componentes stateful sensibles (por ejemplo, cierta BD transaccional de pagos por cumplimiento normativo) permanecen on-premise y se conectan vía VPN/Direct Connect, mientras el resto de servicios stateless corre en AWS.

## 10. Sustentación breve

El diseño prioriza **desacoplamiento temporal** entre lo que el usuario necesita ver de inmediato (crear/listar eventos) y lo que puede procesarse en segundo plano (notificar, auditar). Esto permite que un pico de tráfico en la creación de eventos no degrade el envío de correos, y viceversa. La elección de PostgreSQL para EventService responde a la necesidad de una transacción atómica evento+zonas; MongoDB para NotificationService responde a un patrón de escritura de alto volumen sin necesidad de relaciones. La idempotencia y el patrón DLQ son la base para escalar este mismo patrón (mensaje → consumidor idempotente → efecto secundario) a los demás microservicios del dominio completo (tickets, auditoría, BI) sin duplicar lógica de infraestructura, solo el propio handler de negocio.

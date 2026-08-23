# 🧪 Reto Técnico (Líder Técnico) — Caso Practico Plataforma de Eventos


## 🧩 1) Contexto

Tu organización está construyendo una **Plataforma de Eventos Online** para gestionar eventos online que permita:
- **Publicar y administrar eventos** (con múltiples zonas, precios, capacidad, reglas).
- **Buscar avanzada de eventos publicados** con diferentes críterios de busqueda y respuesta rapida.
- **Vender y reservar tickets** con alta concurrencia (picos en preventa y lanzamientos).
- **Gestionar usuarios y accesos** (clientes, promotores, admins, staff).
- **Procesar pagos** con proveedores externos (PSP) y manejar reembolsos/compensaciones.
- **Entregar tickets** (QR/Barcode) y validar el ingreso (check-in).
- **Notificar** al usuario (email/SMS/push/whatsapp) y mantener trazabilidad.
- **Operar el sistema** con observabilidad, auditoría, seguridad y cumplimiento.

**La plataforma debe soportar:**
- **Alta demanda** en momentos críticos (apertura de ventas / eventos populares).
- **Consistencia** (evitar sobreventa).
- **Auditabilidad** (trazabilidad de operaciones).
- **Disponibilidad** (fallas parciales sin tumbar todo).
- **Evolución** (agregar promociones, marketplace, integraciones, BI).

**Actores** (quiénes usan la plataforma)
- **Cliente final**: realiza busquedas, compra/reserva tickets, recibe confirmaciones, descarga ticket.
- **Organizador/Promotor**: crea eventos, define zonas, precios, aforos, campañas.
- **Administrador**: gobierno, configuración, catálogos, fraude, soporte.
- **Staff de puerta**: valida tickets (check-in) en tiempo real o modo offline.
- **Sistemas externos**: PSP de pagos, servicio de mensajería, antifraude, BI/CRM.

---

## 🚀 2) Objetivo del reto
### Diseñar la arquitectura completa del sistema con las siguientes consideraciones:
- Diseño basado en microservicios y eventos con Broker de mensajería.
- Comunicación asincrona y sincrona entre los microservicios de acuerdo a cada caso de uso.
- Proponer los motores de BD SQL y NoSQL segun cada microservicio y el tipo de uso que tendra.
- Considerar autentificación y autorización con JWT y estandares como OIDC y OAuth 2.0.
- Considerar un diseño de arquitectura en Nube AWS o Híbrido.
- Considerar patrones de resiliencia y alta concurrencia.

### Elaborar un backlog y el plan de trabajo general (ROADMAP) con las siguientes consideraciones:
- Definir del backlog considerando las principales tareas.
- Considerar un marco de trabajo Scrum, con Sprint de 2 semanas.
- Considerar un equipo formado por 2 Backend, 2 Frontend, 1 FullStack, 2 QA, 1 UX, 1 UI, 1 Scrum Master, 1 Product Owner y tú como LT.
- Considerar que existen areas de soporte en TI como: Arquitectura, Base de datos, Seguridad, Plataforma, las cuales cumplen un papel importantes en todo el proceso de desarrollo. Ejemplo: Ajustes y Aprobación de la arquitecturas, aprovisionamiento de recursos On Premise y Nube, Administración del Firewall y politicas de seguridad, despliegues On Premise y Nube con flujos automatizados (Terraform, etc), otros.
- Considerar seguir proceso de desarrollo adecuado, considerando los entornos: DEV -> QA -> Staging -> Production, y la generación de los documentos técnicos y funcionales que se requieren elaborar.
- Considerar que se busca salir con una primera versión del sistema en un plazo de 6 meses.

### Hacer el desarrollo de un MVP inicial del proyecto:
- Se desarrollará **2 APIs .NET** que se comuniquen **asíncronamente con colas** (message broker) y persistan los datos en BD SQL o No SQL como **SQL Server o PostgreSQL**, más una **pantalla React mínima** para registrar un evento.
- Para el MVP técnico del reto, el objetivo es validar si el postulante puede diseñar y construir una solución **con arquitectura limpia & DDD**, **event-driven** y con un **frontend básico**.

---

## 🛠️ 3) Stack requerido & Herramientas y programas

### Herramientas y programas
- Diagramas: `Draw IO | Mermaid | Excalidraw`
- IDE: `Visual Studio | Visual Studio Code | Rider`
- IDE BD: `SSMS | pgAdmin | dbeaver | Mongo Atlas`
- Gestión de contenedores: `Docker Desktop o Podman Desktop`

### Stack Backend
- Framework .NET 9 o 10
- Librerias NET de apoyo: EF, MassTransit, Mediatr, AutoMapper, FluentValidation, Polly, Mailkit, etc.
- Comunicación asíncrona: **RabbitMQ** | **SQS | SNS de AWS** | **Otros**.
- Persistencia SQL: **SQL Server o PostgreSQL** (elige 1).
- Persistencia No SQL: **Mongo DB | Dynamo DB | Otros**
- Persistencia temporal: `Redis Cache` o `ElastiCache` de AWS.

### Stakc Frontend
- React 18+ y/o Next.js (TypeScript recomendado)
- Estilos con Tailwind CSS, CSS o Styled Components

### Infra local
- `docker-compose` o `podman compose` para levantar: `api-event`, `api-notifications`, `db(s)`, `rabbitmq`.

### Infra en Nube AWS
- Cuenta de capa gratuita en AWS para la administración de los servicios:
`Lambda`, `SQS`, `Fargate`, `Dynamo DB`, `RDS`, `S3`, etc.

---

## 🧪 4) Sobre el MVP (2 APIs)

### API 1 — `EventService`
**Responsable de:**
- Registrar eventos y zonas.
- Publicar eventos y notificar a los otros microsevicios de forma asincrona.

**Ejemplo de dominio:**
- `Event` (id, nombre, fecha, lugar, estado)
- `Zone` (id, eventId, nombre, precio, capacidad)

**Requerimientos funcionales**
1. **Crear Evento (Admin)**
   - Endpoint: `POST /events`
   - Crea evento + zonas en una transacción.
   - Publica mensaje `EventCreated` en cola (asíncrono).
2. **Listar Eventos**
   - Endpoint: `GET /events`
   - Incluir almanecamiento en cache con Redis
3. (Opcional) Obtener detalle
   - `GET /events/{id}`

### API 2 — `NotificationService`
**Responsable de:**
- Consumir mensajes del broker cuando se cree y publique un evento.
- Persistir un registro de la notificación en su propia DB (o su propio esquema) 
- Envío una notificación por correo usando Mailkit u otras librerias.

**Ejemplo:**
- `EventCreated` → genera `AuditLog` o `NotificationJob`.

**Requerimientos funcionales**
1. **Consumir `EventCreated`**
   - Guarda un registro en DB con:
     - eventId, nombre, timestamp, correlationId, payloadHash (o similar)
2. **Notificar por correo**
   - Enviar un correo con detalles del evento creado o publicado.

> Importante: el objetivo es validar **desacoplamiento**, **mensajería**, **consistencia**, **reintentos**, **idempotencia** y **observabilidad mínima**.

---

## ➡️ 5) Requisitos de mensajería (obligatorio)
1. **Cola/evento**
   - Tipo de mensaje mínimo:
     ```json
     {
       "messageId": "uuid",
       "eventId": "uuid",
       "name": "string",
       "occurredAt": "ISO-8601",
       "correlationId": "uuid",
       "version": 1
     }
     ```
2. **Idempotencia del consumidor**
   - `NotificationService` debe evitar procesar el mismo `messageId` dos veces.
3. **Reintentos**
   - Si falla el procesamiento, debe reintentarse (política simple).
4. **DLQ / Dead Letter (bonus fuerte)**
   - Si no se puede procesar tras N reintentos, mover a cola de muertos o registrar estado “Failed”.

---
## 🗄️ 6) Requisitos de Persistencia (obligatorio)
- Cada API debe persistir datos en su DB.
- Se permite:
  - **Una DB con schemas separados** (rápido para el reto), o
  - **DB por servicio** (ideal, recomendado).
- Se sugiere utiliza ORM como EF, para la capa de persistencia.
- Debe haber scripts de inicialización:
  - `db/init.sql` (o migraciones) para tablas mínimas.

---
## 🔒 7) Requisitos de seguridad (opcional bonus)
1. **Autenticación JWT**
   - Puede ser JWT “local” (issuer propio) o integración con un IdP (si lo dominas y lo justificas).
2. **Autorización por roles**
   - `Admin`: puede `POST /events`
   - `User`: puede `GET /events`
3. **Evitar IDOR**
   - Si implementas endpoints por usuario, asegurar que no pueda acceder a recursos ajenos.
4. **Manejo de errores seguro**
   - No filtrar stack traces ni detalles de DB.
5. **Protección anti-abuso (mínima)**
   - Rate limiting básico o al menos throttling por IP/usuario (puede ser simple).
6. **Logs sin datos sensibles**
   - No loguear tokens, passwords, ni PII.

---

## 🌐 8) Sobre el Frontend (React) — pantalla mínima
### Pantalla: “Registrar Evento”
- Formulario:
  - Nombre del evento
  - Fecha
  - Lugar
  - Zonas (lista editable): nombre, precio, capacidad
- Botón “Guardar”
- Consumir `POST /events` usando token JWT (puede ser fijo para la demo).

**Criterios:**
- Validación mínima (campos obligatorios, capacidad > 0, precio >= 0).
- Manejo de loading / error.

---

## ⏰ 9) Indicaciones del reto

- Duración: 2 días.
- Formato de entrega: Repositorio en GitHub o GitLab.
- Se debe incluir los archivos `Docker Compose` o `Podman Compose`, para el levantatamiento del los recursos.
- Se deben incluir los archivos `Dockerfile` para cada API segun corresponda.

---

## 10) Entregables finales
### A) Diagrama de arquitectura
Entregar un diagrama en `docs/architecture.md` o `docs/architectura.drawio` (o la imagen en `docs/diagrams/` + explicación) que incluya:
- Componentes y/o servicios empleados: API's, Broker, DB(s), etc.
- Listado de microservicios.
- Flujos: HTTP sync + eventos async
- Notas de seguridad: JWT, roles, boundaries
- Una breve sustenciación.

### B) Backlog y Plan de Trabajo general (ROADMAP)
- En formato excel u otras herramientas similares.
- Lista de tareas principales del Backlog.

### C) Código fuente del Backend y Frontend
- Se debe incluir los archivos **README** con las instrucciones para ejecutar el proyecto.
- Se debe incluir los `scripts` de BD para la creación del módelo de datos y para inicialiar datos, en caso de usar EF, en el `README` indicar las intrucciones de migración y carga de datos iniciales(Seek).
- Archivos `Dockerfile` y `Docker Compose` o `Podman Compose`.

**Todo los entregables deben subirse a un repositorio en GitHub | GitLab o BitBucket**

---

## ✅ ¡Éxitos en el reto!
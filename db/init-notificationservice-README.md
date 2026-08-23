# Inicialización de datos — NotificationService (MongoDB)

NotificationService usa **MongoDB** (NoSQL), por lo que no requiere un script SQL.
La colección `notification_jobs` se crea automáticamente al primer insert, y el
índice único sobre `MessageId` (garantía de idempotencia) se crea programáticamente
en `MongoNotificationJobRepository` al arrancar la aplicación — no requiere pasos manuales.

Si se desea inspeccionar la colección manualmente:

```js
use notificationservice
db.notification_jobs.find().pretty()
db.notification_jobs.getIndexes()
```

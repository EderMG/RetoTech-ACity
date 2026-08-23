using MongoDB.Driver;
using NotificationService.Domain.Abstractions;
using NotificationService.Domain.Entities;

namespace NotificationService.Infrastructure.Persistence;

/// <summary>
/// Persistencia en MongoDB (NoSQL) para NotificationService: encaja bien con el patrón
/// "registro semi-estructurado de eventos procesados" y evita acoplar el esquema al de EventService.
/// El índice único sobre MessageId es la garantía física de idempotencia.
/// </summary>
public class MongoNotificationJobRepository : INotificationJobRepository
{
    private readonly IMongoCollection<NotificationJob> _collection;

    public MongoNotificationJobRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<NotificationJob>("notification_jobs");
        var indexKeys = Builders<NotificationJob>.IndexKeys.Ascending(j => j.MessageId);
        _collection.Indexes.CreateOne(new CreateIndexModel<NotificationJob>(
            indexKeys, new CreateIndexOptions { Unique = true }));
    }

    public async Task<bool> ExistsByMessageIdAsync(Guid messageId, CancellationToken ct = default) =>
        await _collection.Find(j => j.MessageId == messageId).AnyAsync(ct);

    public async Task AddAsync(NotificationJob job, CancellationToken ct = default) =>
        await _collection.InsertOneAsync(job, cancellationToken: ct);

    public async Task UpdateAsync(NotificationJob job, CancellationToken ct = default) =>
        await _collection.ReplaceOneAsync(j => j.Id == job.Id, job, cancellationToken: ct);
}

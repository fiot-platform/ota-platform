using MongoDB.Bson;
using MongoDB.Driver;
using OTA.API.Models.Entities;
using OTA.API.Models.Enums;
using OTA.API.Repositories.Interfaces;

namespace OTA.API.Repositories
{
    /// <summary>
    /// MongoDB implementation of IQASessionRepository.
    /// Uses targeted $push/$pull/$set update operations to avoid full-document replacement
    /// on array mutations, preventing last-write-wins races.
    /// Collection: QASessions
    /// </summary>
    public class QASessionRepository : IQASessionRepository
    {
        private readonly IMongoCollection<QASessionEntity> _collection;

        public QASessionRepository(IMongoDatabase database)
        {
            _collection = database.GetCollection<QASessionEntity>("QASessions");
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            var indexModels = new List<CreateIndexModel<QASessionEntity>>
            {
                new CreateIndexModel<QASessionEntity>(
                    Builders<QASessionEntity>.IndexKeys.Ascending(q => q.FirmwareId),
                    new CreateIndexOptions { Name = "idx_qa_firmwareId", Unique = true }),
                new CreateIndexModel<QASessionEntity>(
                    Builders<QASessionEntity>.IndexKeys.Ascending(q => q.Status),
                    new CreateIndexOptions { Name = "idx_qa_status" }),
            };
            _collection.Indexes.CreateMany(indexModels);
        }

        private static FilterDefinition<QASessionEntity> ByObjectId(string id)
            => Builders<QASessionEntity>.Filter.Eq("_id", ObjectId.Parse(id));

        public async Task<QASessionEntity?> GetByFirmwareIdAsync(string firmwareId, CancellationToken cancellationToken = default)
        {
            var filter = Builders<QASessionEntity>.Filter.Eq(q => q.FirmwareId, firmwareId);
            return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        }

        public async Task InsertAsync(QASessionEntity entity, CancellationToken cancellationToken = default)
        {
            await _collection.InsertOneAsync(entity, null, cancellationToken);
        }

        public async Task UpdateStatusAsync(string id, QASessionStatus status, string? remarks, CancellationToken cancellationToken = default)
        {
            var update = Builders<QASessionEntity>.Update
                .Set(q => q.Status, status)
                .Set(q => q.UpdatedAt, DateTime.UtcNow);

            if (remarks != null)
                update = update.Set(q => q.Remarks, remarks);

            await _collection.UpdateOneAsync(ByObjectId(id), update, null, cancellationToken);
        }

        public async Task PushTestCaseDocumentAsync(string id, QADocumentItem doc, CancellationToken cancellationToken = default)
        {
            var update = Builders<QASessionEntity>.Update
                .Push(q => q.TestCaseDocuments, doc)
                .Set(q => q.UpdatedAt, DateTime.UtcNow);
            await _collection.UpdateOneAsync(ByObjectId(id), update, null, cancellationToken);
        }

        public async Task PushTestResultDocumentAsync(string id, QADocumentItem doc, CancellationToken cancellationToken = default)
        {
            var update = Builders<QASessionEntity>.Update
                .Push(q => q.TestResultDocuments, doc)
                .Set(q => q.UpdatedAt, DateTime.UtcNow);
            await _collection.UpdateOneAsync(ByObjectId(id), update, null, cancellationToken);
        }

        public async Task PullDocumentAsync(string id, string documentId, CancellationToken cancellationToken = default)
        {
            var docIdFilter = Builders<QADocumentItem>.Filter.Eq(d => d.DocumentId, documentId);
            var update = Builders<QASessionEntity>.Update
                .PullFilter(q => q.TestCaseDocuments, docIdFilter)
                .PullFilter(q => q.TestResultDocuments, docIdFilter)
                .Set(q => q.UpdatedAt, DateTime.UtcNow);
            await _collection.UpdateOneAsync(ByObjectId(id), update, null, cancellationToken);
        }

        public async Task PushBugAsync(string id, QABugItem bug, CancellationToken cancellationToken = default)
        {
            var update = Builders<QASessionEntity>.Update
                .Push(q => q.Bugs, bug)
                .Set(q => q.UpdatedAt, DateTime.UtcNow);
            await _collection.UpdateOneAsync(ByObjectId(id), update, null, cancellationToken);
        }

        public async Task UpdateBugAsync(string id, string bugId, QABugItem updated, CancellationToken cancellationToken = default)
        {
            // Pull the old bug and push the updated one — positional operator requires prior knowledge
            // of the array index which is not available without a fetch. Use pull+push as atomic pair.
            var pullFilter = Builders<QABugItem>.Filter.Eq(b => b.BugId, bugId);
            var update = Builders<QASessionEntity>.Update
                .PullFilter(q => q.Bugs, pullFilter)
                .Set(q => q.UpdatedAt, DateTime.UtcNow);
            await _collection.UpdateOneAsync(ByObjectId(id), update, null, cancellationToken);

            var pushUpdate = Builders<QASessionEntity>.Update
                .Push(q => q.Bugs, updated)
                .Set(q => q.UpdatedAt, DateTime.UtcNow);
            await _collection.UpdateOneAsync(ByObjectId(id), pushUpdate, null, cancellationToken);
        }

        public async Task PushEventLogAsync(string id, QAEventLogItem entry, CancellationToken cancellationToken = default)
        {
            var update = Builders<QASessionEntity>.Update
                .Push(q => q.EventLog, entry)
                .Set(q => q.UpdatedAt, DateTime.UtcNow);
            await _collection.UpdateOneAsync(ByObjectId(id), update, null, cancellationToken);
        }

        public async Task SetCompletedAsync(string id, QASessionStatus finalStatus, string? remarks, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var update = Builders<QASessionEntity>.Update
                .Set(q => q.Status, finalStatus)
                .Set(q => q.CompletedAt, now)
                .Set(q => q.UpdatedAt, now);
            if (remarks != null)
                update = update.Set(q => q.Remarks, remarks);
            await _collection.UpdateOneAsync(ByObjectId(id), update, null, cancellationToken);
        }
    }
}

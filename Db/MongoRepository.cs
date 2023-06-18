using MongoDB.Driver;
using System.Linq.Expressions;

namespace Scheduler.Db
{
    public class MongoRepository<TKey, TValue> : IRepository<TKey, TValue> where TValue : IEntity<TKey>
    {
        private readonly IMongoCollection<TValue> dbCollection;
        private readonly FilterDefinitionBuilder<TValue> _filterBuilder = Builders<TValue>.Filter;
        public FilterDefinitionBuilder<TValue> filter => _filterBuilder;

        public MongoRepository(IMongoDatabase database, string collectionName)
        {
            dbCollection = database.GetCollection<TValue>(collectionName);
        }

        public async Task<IReadOnlyCollection<TValue>> GetAll()
        {
            return await dbCollection.Find(filter.Empty).ToListAsync();
        }

        public async Task<IReadOnlyCollection<TValue>> GetAll(Expression<Func<TValue, bool>> filter)
        {
            return await dbCollection.Find(filter).ToListAsync();
        }

        public async Task<IReadOnlyCollection<TValue>> GetAll(FilterDefinition<TValue> filter)
        {
            return await dbCollection.Find(filter).ToListAsync();
        }

        public Task<TValue> Get(TKey id)
        {
            FilterDefinition<TValue> filter = _filterBuilder.Eq(e => e.Id, id);
            return dbCollection.Find(filter).FirstOrDefaultAsync();
        }

        public Task<TValue> Get(Expression<Func<TValue, bool>> filter)
        {
            return dbCollection.Find(filter).FirstOrDefaultAsync();
        }

        public Task Create(TValue entity)
        {
            return dbCollection.InsertOneAsync(entity);
        }

        public Task Update(TValue entity)
        {
            FilterDefinition<TValue> filter = _filterBuilder.Eq(e => e.Id, entity.Id);
            return dbCollection.ReplaceOneAsync(filter, entity);
        }

        public Task Remove(TKey id)
        {
            FilterDefinition<TValue> filter = _filterBuilder.Eq(e => e.Id, id);
            return dbCollection.DeleteOneAsync(filter);
        }
    }
}

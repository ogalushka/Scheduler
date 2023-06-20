using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using MongoDB.Driver;
using Scheduler.Settings;
using Microsoft.AspNetCore.DataProtection;

namespace Scheduler.Db
{
    public static class MongoSetup
    {
        public static void SetupDb(this IServiceCollection services)
        {
            BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));
            BsonSerializer.RegisterSerializer(new DateTimeOffsetSerializer(BsonType.String));
            services.AddDataProtection().PersistKeysToMongoDb(sp => sp.GetRequiredService<IMongoDatabase>());

            services.AddSingleton(sp =>
            {
                var config = sp.GetService<IConfiguration>();
                if (config == null)
                {
                    throw new Exception("Can't resolve IConfiguration from service provider");
                }

                var mongoSettings = config.GetSection(nameof(MongoSettings))?.Get<MongoSettings>();
                if (mongoSettings == null)
                {
                    throw new Exception("MongoDbSettings or ServiceSettings section missing from configuration");
                }

                var mongoClient = new MongoClient(mongoSettings.Connection);
                return mongoClient.GetDatabase(mongoSettings.Database);
            });
        }

        public static void SetupRepository<TKey, TValue>(this IServiceCollection services, string collectionName) where TValue : IEntity<TKey>
        {
            services.AddSingleton<IRepository<TKey, TValue>>(sp => {
                var database = sp.GetService<IMongoDatabase>();
                if (database == null)
                {
                    throw new Exception("Failed to resolve database from service provider");
                }
                return new MongoRepository<TKey, TValue>(database, collectionName);
            });
        }
    }
}

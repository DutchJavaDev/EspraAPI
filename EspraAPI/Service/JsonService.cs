using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using Sentry;
using System.Dynamic;
using System.Text.Json;

namespace EspraAPI.Service
{
    public class JsonService
    {
        private readonly string CollectionName;
        private IMongoCollection<JsonData>? JsonCollection;
        private IMongoDatabase Database { get; set; }

        [ActivatorUtilitiesConstructor]
        public JsonService(IMongoClient mongoClient, IConfiguration configuration)
        {
            Database = mongoClient.GetDatabase(configuration["MONGO:DATBASE"]);
            CollectionName = configuration["MONGO:JSON_COLLECTION"];
        }

        public JsonService(IMongoDatabase mongoDatabase, string collection)
        {
            Database = mongoDatabase;
            CollectionName = collection;
        }

        public async Task<JsonData> GetByIdAsync(string id)
        {
            return await JsonCollection.Find(i => i.Id.Equals(id)).FirstAsync();
        }

        public async Task<bool> AddAsync(string group, string content, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            JsonCollection = Database.GetCollection<JsonData>(CollectionName);

            await JsonCollection.InsertOneAsync(new JsonData
            {
                GroupId = group,
                Data = content,
                DateAdded = DateTime.Now,
                LastModified = DateTime.Now
            },
                cancellationToken: token);

            return true;
        }

        public async Task<IList<JsonData>> GetAsync(string group)
        {
            try
            {
                //token.ThrowIfCancellationRequested();
                JsonCollection = Database.GetCollection<JsonData>(CollectionName);

                await JsonCollection.Database.Client.StartSessionAsync();

                var collection = await JsonCollection.Find(i => i.GroupId == group).ToListAsync();

                return collection.ToList();
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
                return new List<JsonData>();
            }
        }

        public async Task<bool> UpdateAsync(string id, string data, CancellationToken token)
        {
            JsonCollection = Database.GetCollection<JsonData>(CollectionName);

            var old = (await JsonCollection.FindAsync(i => i.Id == id, cancellationToken: token)).First(cancellationToken: token);

            var filter = Builders<JsonData>.Filter.Eq(nameof(JsonData.Id), id);

            var update = Builders<JsonData>.Update.Set(nameof(JsonData.Data), data);

            var updateResult = await JsonCollection.
                UpdateOneAsync(filter, update);

            return updateResult.IsAcknowledged;
        }


        public async Task<bool> DeleteAsync(string id, CancellationToken token)
        {
            JsonCollection = Database.GetCollection<JsonData>(CollectionName);

            var deleteResult = await JsonCollection.DeleteOneAsync(i => i.Id == id, token);

            return deleteResult.IsAcknowledged;
        }
    }

    public class JsonData 
    {
        [BsonId]
        public string Id { get; set; } = string.Empty;

        public string GroupId { get; set; } = string.Empty;

        public string Data { get; set; } = string.Empty;

        public DateTime DateAdded { get; set; } = DateTime.Now;

        public DateTime LastModified { get; set; } = DateTime.Now;
    }
}

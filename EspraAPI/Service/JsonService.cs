using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
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

        public async Task<bool> AddAsync(string group, dynamic content, CancellationToken token)
        {
            if (content is not string)
                content = JsonSerializer.Serialize(content);

            token.ThrowIfCancellationRequested();

            JsonCollection = Database.GetCollection<JsonData>(CollectionName);

            await JsonCollection.InsertOneAsync(new JsonData
            {
                GroupId = group,
                Data = content,
                DateAdded = DateTime.Now.ToString(Util.DATE_FORMAT),
                LastModified = DateTime.Now.ToString(Util.DATE_FORMAT)

            },cancellationToken: token);

            return true;
        }

        public async Task<IList<JsonData>> GetAsync(string group, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            JsonCollection = Database.GetCollection<JsonData>(CollectionName);

            return await (await JsonCollection.FindAsync(i => i.GroupId == group, cancellationToken: token)).ToListAsync(cancellationToken: token);
        }

        public async Task<bool> UpdateAsync(string id, dynamic data, CancellationToken token)
        {
            if (data is not string)
                data = JsonSerializer.Serialize(data);

            JsonCollection = Database.GetCollection<JsonData>(CollectionName);

            var old = (await JsonCollection.FindAsync(i => i.Id == id, cancellationToken: token)).First(cancellationToken: token);

            var updateFilter = Builders<JsonData>.Filter.Eq(nameof(JsonData.Id), id);

            var dataUpdate = Builders<JsonData>.Update.Set(nameof(JsonData.Data), data);

            var lastEditUpdate = Builders<JsonData>.Update.Set(nameof(JsonData.LastModified), DateTime.Now.ToString(Util.DATE_FORMAT));

            var updateDefenitions = Builders<JsonData>.Update.Combine(dataUpdate, lastEditUpdate);                                

            var updateResult = await JsonCollection.
                UpdateOneAsync(updateFilter, updateDefenitions, cancellationToken: token);

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
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        public string GroupId { get; set; } = string.Empty;

        public string Data { get; set; } = string.Empty;

        public string DateAdded { get; set; } = string.Empty;

        public string LastModified { get; set; } = string.Empty;
    }
}

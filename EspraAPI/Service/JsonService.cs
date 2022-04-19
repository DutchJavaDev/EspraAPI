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
        public GroupService GroupService { get; private set; }

        [ActivatorUtilitiesConstructor]
        public JsonService(IMongoClient mongoClient, IConfiguration configuration, GroupService groupService)
        {
            Database = mongoClient.GetDatabase(configuration["MONGO:DATBASE"]);
            CollectionName = configuration["MONGO:JSON_COLLECTION"];
            GroupService = groupService;
        }

        public JsonService(IMongoDatabase mongoDatabase, string collection, GroupService groupService)
        {
            Database = mongoDatabase;
            CollectionName = collection;
            GroupService = groupService;
        }

        public async Task<JsonData> GetByIdAsync(string id, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            return await (await JsonCollection.FindAsync(i => i.Id.Equals(id), cancellationToken: token)).FirstAsync(token);
        }

        public async Task<bool> AddAsync(string group, dynamic content, CancellationToken token)
        {
            if (content is not string)
                content = JsonSerializer.Serialize(content);

            token.ThrowIfCancellationRequested();

            JsonCollection = Database.GetCollection<JsonData>(CollectionName);

            var jsondata = new JsonData
            {
                GroupId = group,
                Data = content,
                DateAdded = DateTime.Now.ToString(Util.DATE_FORMAT),
                LastModified = DateTime.Now.ToString(Util.DATE_FORMAT)

            };

            await JsonCollection.InsertOneAsync(jsondata,cancellationToken: token);

            return await GroupService.AddJsonIdAsync(group, jsondata.Id, token);
        }

        public async Task<IList<JsonData>> GetCollectionByGroupAsync(string group, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            JsonCollection = Database.GetCollection<JsonData>(CollectionName);

            return await (await JsonCollection.FindAsync(i => i.GroupId == group, cancellationToken: token)).ToListAsync(cancellationToken: token);
        }

        public async Task<bool> UpdateByIdAsync(string id, dynamic data, CancellationToken token)
        {
            if (data is not string)
                data = JsonSerializer.Serialize(data);

            JsonCollection = Database.GetCollection<JsonData>(CollectionName);

            var updateFilter = Builders<JsonData>.Filter.Eq(nameof(JsonData.Id), id);

            var dataUpdate = Builders<JsonData>.Update.Set(nameof(JsonData.Data), data);

            var lastEditUpdate = Builders<JsonData>.Update.Set(nameof(JsonData.LastModified), DateTime.Now.ToString(Util.DATE_FORMAT));

            var updateDefenitions = Builders<JsonData>.Update.Combine(dataUpdate, lastEditUpdate);                                

            var updateResult = await JsonCollection.
                UpdateOneAsync(updateFilter, updateDefenitions, cancellationToken: token);

            return updateResult.IsAcknowledged;
        }


        public async Task<bool> DeleteByIdAsync(string id, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            JsonCollection = Database.GetCollection<JsonData>(CollectionName);

            var jsonData = (await JsonCollection.FindAsync(i => i.Id == id, cancellationToken: token)).First(token);

            if (jsonData is null)
                return true;

            if ((await JsonCollection.DeleteOneAsync(i => i.Id == id, token)).IsAcknowledged)
            {
                await GroupService.RemoveJsonIdAsync(jsonData.GroupId, jsonData.Id, token);
            }

            return true;
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

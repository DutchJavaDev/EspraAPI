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

        public JsonService(IMongoClient mongoClient, IConfiguration configuration)
        {
            Database = mongoClient.GetDatabase(configuration["MONGO:DATBASE"]);
            CollectionName = configuration["MONGO:JSON_COLLECTION"];
        }

        public async Task<bool> Add(string group, object content, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                JsonCollection = Database.GetCollection<JsonData>(CollectionName);

                await JsonCollection.InsertOneAsync(new JsonData
                {
                    GroupId = group,
                    Data = JsonSerializer.Serialize(content.ToString()),
                    DateAdded = DateTime.Now,
                    LastModified = DateTime.Now
                },
                    cancellationToken: token);

                return true;
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
                return false;
            }
        }

        public async Task<IList<dynamic>> Get(string group)
        {
            try
            {
                //token.ThrowIfCancellationRequested();

                JsonCollection = Database.GetCollection<JsonData>(CollectionName);

            return (await JsonCollection.Find(i => i.GroupId == group).ToListAsync())
                    .Select(i => {

                        dynamic obj = new ExpandoObject();

                        obj.id = i.Id;
                        obj.group = i.GroupId;
                        obj.data = JsonSerializer.Deserialize<string>(i.Data);

                        return obj;
                    }).ToList();
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
                return new List<dynamic>();
            }
        }

        public async Task<bool> Update(string id, dynamic data, CancellationToken token, string overrwite = default)
        {
            return await Task.Run( async () => {

                var result = false;

                try
                {
                    token.ThrowIfCancellationRequested();

                    token.ThrowIfCancellationRequested();

                    JsonCollection = Database.GetCollection<JsonData>(CollectionName);


                    var old = (await JsonCollection.FindAsync(i => i.Id == id,cancellationToken: token)).First(cancellationToken: token);

                    var filter = Builders<JsonData>.Filter.Eq(nameof(JsonData.Id), id);


                    var update = Builders<JsonData>.Update.Set(nameof(JsonData.Data), data);

                    var updateResult = JsonCollection.UpdateOne(filter, update, cancellationToken: token);

                    return true;
                }
                catch (Exception e)
                {
                    if (e is not OperationCanceledException)
                        SentrySdk.CaptureException(e);

                    result = false;
                }

                return result;
            }, token);
        }

    }

    public class JsonData 
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        public string GroupId { get; set; } = string.Empty;

        public string Data { get; set; } = string.Empty;

        public string Json { get; set; } = string.Empty;

        public DateTime DateAdded { get; set; } = DateTime.Now;

        public DateTime LastModified { get; set; } = DateTime.Now;
    }
}

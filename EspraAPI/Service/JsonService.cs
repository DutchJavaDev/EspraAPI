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
        public IConfiguration Configuration;

        private IMongoCollection<JsonData> JsonCollection { get; set; }

        public JsonService(IConfiguration configuration)
        {
            Configuration = configuration;

            var url = Configuration["MONGO:DEV_URL"];
            var _database = Configuration["MONGO:DATBASE"];
            var _collection = Configuration["MONGO:JSON_COLLECTION"];

            var mongoClient = new MongoClient(url);
            var database = mongoClient.GetDatabase(_database);
            JsonCollection = database.GetCollection<JsonData>(_collection);
        }

        public async Task<bool> Add(string group, object data, CancellationToken token)
        {
            try
            {
                await JsonCollection.InsertOneAsync(new JsonData
                {
                    GroupId = group,
                    Data = JsonSerializer.Serialize(data),
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

        public async Task<IList<dynamic>> GetGroup(string group)
        {
            try
            {
                return (await JsonCollection.Find(i => i.GroupId == group).ToListAsync())
                    .Select(i => {
                        
                        dynamic obj = new ExpandoObject();
                        
                        obj.id = i.Id;
                        obj.group = i.GroupId;
                        obj.data = i.Data;

                        return obj;
                    }).ToList();
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
                return new List<dynamic>();
            }
        }

    }

    public class JsonData 
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        public string GroupId { get; set; } = string.Empty;

        public string Data { get; set; } = string.Empty;

        public DateTime DateAdded { get; set; } = DateTime.Now;

        public DateTime LastModified { get; set; } = DateTime.Now;
    }
}

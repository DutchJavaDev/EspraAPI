using MongoDB.Driver;
using Xunit;
using System;
using System.Threading;
using System.Threading.Tasks;
using EspraAPI.Service;
using System.Dynamic;
using System.Text.Json;
using System.Linq;

namespace EspraUnitTest
{
    public class JsonServiceUnitTest
    {
        private static JsonService JsonService { get; set; }
        private static IMongoClient client;
        private static string Database = "espra_test_db";
        private static string JsonCollection = "json_test_collection";
        private static Random Random = new Random();
        private static CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

        public JsonServiceUnitTest()
        {
            client = new MongoClient("mongodb://localhost:27017");
        }

        ~JsonServiceUnitTest() 
        {
            client.DropDatabase(Database);
        }

        [Fact(DisplayName = "Add jsondata en verfify that it has been created")] 
        public async void Add_JSON_Data()
        {
            var json = await CreateService();

            var addResult = await json.AddAsync("test", CreateObject(), CancellationTokenSource.Token);

            Assert.True(addResult);

            CancellationTokenSource = new CancellationTokenSource();

            var collection = await json.GetAsync("test", CancellationTokenSource.Token);

            Assert.Equal(1, collection.Count);
        }

        [Fact(DisplayName = "Get a jsondata object and verify the data")] 
        public async void Get_JSON_Data()
        {
            var json = await CreateService();

            var addResult = await json.AddAsync("test", CreateObject(), CancellationTokenSource.Token);

            Assert.True(addResult);

            CancellationTokenSource = new CancellationTokenSource();

            var getResult = await json.GetAsync("test", CancellationTokenSource.Token);

            Assert.Equal(1, getResult.Count);
             
            var obj = getResult[0];

            dynamic? data = JsonSerializer.Deserialize<ExpandoObject?>(obj.Data);

            Assert.Equal("TestObject", JsonSerializer.Deserialize<string>(data?.Name));
        }


        [Fact(DisplayName = "Update a exisitng jsondata by changing its Data field and verify that is has been updated")] 
        public async void Update_JSON_Data()
        {
            var json = await CreateService();

            var addResult = await json.AddAsync("test", CreateObject(), CancellationTokenSource.Token);

            Assert.True(addResult);

            CancellationTokenSource = new CancellationTokenSource();

            var getResult = await json.GetAsync("test", CancellationTokenSource.Token);

            Assert.Equal(1, getResult.Count);

            var obj = getResult[0];

            dynamic? data = JsonSerializer.Deserialize<ExpandoObject?>(obj.Data);

            Assert.Equal("TestObject", JsonSerializer.Deserialize<string>(data?.Name));

            CancellationTokenSource = new CancellationTokenSource();

            var id = obj.Id;

            var update = "My internal gu-data have been flipped upside down 8555";

            var updateResult = await json.UpdateAsync(id, update, CancellationTokenSource.Token);

            Assert.True(updateResult);


            var result = await json.GetByIdAsync(id);

            Assert.Equal(update, result.Data);

            CancellationTokenSource = new CancellationTokenSource();

            var collectionResult = await json.GetAsync("test", CancellationTokenSource.Token);

            Assert.True(collectionResult.Count > 0);

            result = collectionResult[0];

            Assert.Equal(update, result.Data);
        }

        [Fact(DisplayName = "Delete jsondata by its id")]
        public async void Delete_JSON_Data()
        {
            var json = await CreateService();

            var addResult = await json.AddAsync("test", CreateObject(), CancellationTokenSource.Token);

            Assert.True(addResult);

            CancellationTokenSource = new CancellationTokenSource();

            var getResult = await json.GetAsync("test", CancellationTokenSource.Token);

            Assert.Equal(1, getResult.Count);

            var obj = getResult[0];

            var id = obj.Id;

            CancellationTokenSource = new CancellationTokenSource();

            var deleteResult = await json.DeleteAsync(id, CancellationTokenSource.Token);

            Assert.True(deleteResult);

            CancellationTokenSource = new CancellationTokenSource();

            getResult = await json.GetAsync("test", CancellationTokenSource.Token);

            Assert.Empty(getResult);
        }

        private async static Task<JsonService> CreateService()
        {
            if (CancellationTokenSource != null)
                CancellationTokenSource = new CancellationTokenSource();

            if (client.ListDatabaseNames().ToList().Count > 0)
                client.DropDatabase(Database);

            var db = client.GetDatabase(Database);

            if(db != null)
                await db.DropCollectionAsync(JsonCollection);


            client.DropDatabase(Database);

            if (JsonService == null)
                JsonService = new JsonService(db ?? client.GetDatabase(Database), JsonCollection);

            return JsonService;

        }

        private static string CreateObject()
        {
            dynamic _object = new ExpandoObject();

            _object.Id = Random.Next();
            _object.Name = "TestObject";
            _object.Description = false;
            _object.Obj = new object();

            return JsonSerializer.Serialize(_object);

        }
    }
}
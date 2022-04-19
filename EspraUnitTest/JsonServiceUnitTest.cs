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
        private static JsonService? JsonService { get; set; }
        private static GroupService? GroupService { get; set; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private static IMongoClient Client;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private static readonly string Database = "espra_test_db";
        private static readonly string JsonCollection = "json_test_collection";
        private static readonly Random Random = new();
        private static CancellationTokenSource CancellationTokenSource = new();

        public JsonServiceUnitTest()
        {
            Client = new MongoClient("mongodb://localhost:27017");
        }

        ~JsonServiceUnitTest() 
        {
            Client.DropDatabase(Database);
        }

        [Fact(DisplayName = "Add jsondata en verfify that it has been created")] 
        public async Task Add_JSON_Data()
        {
            var json = await CreateService();

            var addResult = await json.AddAsync("test", CreateObject(), CancellationTokenSource.Token);

            Assert.True(addResult);

            CancellationTokenSource = new CancellationTokenSource();

            var collection = await json.GetCollectionByGroupAsync("test", CancellationTokenSource.Token);

            Assert.Equal(1, collection.Count);

            var group = await json.GroupService.GetGroupInfoAsync("test", CancellationTokenSource.Token);

            Assert.Equal(collection[0].Id, group.JsonIds[0]);
        }

        [Fact(DisplayName = "Get a jsondata object and verify the data")] 
        public async Task Get_JSON_Data()
        {
            var service = await CreateService();

            var addResult = await service.AddAsync("test", CreateObject(), CancellationTokenSource.Token);

            Assert.True(addResult);

            CancellationTokenSource = new CancellationTokenSource();

            var groupInfo = await service.GroupService.GetGroupInfoAsync("test", CancellationTokenSource.Token);

            var jsonData = await service.GetByIdAsync(groupInfo.JsonIds[0], CancellationTokenSource.Token);

            dynamic? data = JsonSerializer.Deserialize<ExpandoObject?>(jsonData.Data);

            Assert.Equal("TestObject", JsonSerializer.Deserialize<string>(data?.Name));
        }

        [Fact(DisplayName = "Update a exisitng jsondata by changing its Data field and verify that is has been updated")] 
        public async Task Update_JSON_Data()
        {
            var service = await CreateService();

            var addResult = await service.AddAsync("test", CreateObject(), CancellationTokenSource.Token);

            Assert.True(addResult);

            CancellationTokenSource = new CancellationTokenSource();

            var getResult = await service.GetCollectionByGroupAsync("test", CancellationTokenSource.Token);

            Assert.Equal(1, getResult.Count);

            var obj = getResult[0];

            dynamic? data = JsonSerializer.Deserialize<ExpandoObject?>(obj.Data);

            Assert.Equal("TestObject", JsonSerializer.Deserialize<string>(data?.Name));

            CancellationTokenSource = new CancellationTokenSource();

            var id = obj.Id;

            var update = "My internal gu-data have been flipped upside down 8555";

            var updateResult = await service.UpdateByIdAsync(id, update, CancellationTokenSource.Token);

            Assert.True(updateResult);

            var result = await service.GetByIdAsync(id, CancellationTokenSource.Token);

            Assert.Equal(update, result.Data);

            CancellationTokenSource = new CancellationTokenSource();

            var collectionResult = await service.GetCollectionByGroupAsync("test", CancellationTokenSource.Token);

            Assert.True(collectionResult.Count > 0);

            result = collectionResult[0];

            Assert.Equal(update, result.Data);
        }

        [Fact(DisplayName = "Delete jsondata by its id")]
        public async Task Delete_JSON_Data()
        {
            var service = await CreateService();

            var addResult = await service.AddAsync("test", CreateObject(), CancellationTokenSource.Token);

            Assert.True(addResult);

            CancellationTokenSource = new CancellationTokenSource();

            var getResult = await service.GetCollectionByGroupAsync("test", CancellationTokenSource.Token);

            Assert.Equal(1, getResult.Count);

            var obj = getResult[0];

            var id = obj.Id;

            CancellationTokenSource = new CancellationTokenSource();

            var deleteResult = await service.DeleteByIdAsync(id, CancellationTokenSource.Token);

            Assert.True(deleteResult);

            CancellationTokenSource = new CancellationTokenSource();

            getResult = await service.GetCollectionByGroupAsync("test", CancellationTokenSource.Token);

            Assert.Empty(getResult);

            var group = await service.GroupService.GetGroupInfoAsync("test", CancellationTokenSource.Token);

            Assert.Empty(group.JsonIds);
        }

        private async static Task<JsonService> CreateService()
        {
            if (CancellationTokenSource != null)
                CancellationTokenSource = new CancellationTokenSource();

            if (Client.ListDatabaseNames().ToList().Count > 0)
                Client.DropDatabase(Database);

            var db = Client.GetDatabase(Database);

            if(db != null)
                await db.DropCollectionAsync(JsonCollection);

            Client.DropDatabase(Database);

            if (GroupService == null)
                GroupService = new GroupService(db ?? Client.GetDatabase(Database));

            if (JsonService == null)
                JsonService = new JsonService(db ?? Client.GetDatabase(Database), JsonCollection, GroupService);

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
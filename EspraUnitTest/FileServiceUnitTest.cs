using MongoDB.Driver;
using Xunit;
using System;
using System.Threading;
using System.Threading.Tasks;
using EspraAPI.Service;
using System.IO;
using System.Linq;

namespace EspraUnitTest
{
    public class FileServiceUnitTest
    {
        private static FileService? FileService { get; set; }
        private static GroupService? GroupService { get; set; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private static IMongoClient Client;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private static readonly string Database = "espra_test_db";
        private static readonly string JsonCollection = "file_test_collection";
        private static readonly Random Random = new();
        private static CancellationTokenSource CancellationTokenSource = new();

        public FileServiceUnitTest()
        {
            Client = new MongoClient("mongodb://localhost:27017");
        }

        ~FileServiceUnitTest()
        {
            Client.DropDatabase(Database);
        }

        [Fact]
        public async void Path()
        {
            var path = Directory.GetCurrentDirectory();

            var dir = Directory.GetDirectories(path);

            var files = Directory.GetFiles(dir[4]);

            Assert.NotNull(files);
        }

        private async static Task<FileService> CreateService()
        {
            if (CancellationTokenSource != null)
                CancellationTokenSource = new CancellationTokenSource();

            if (Client.ListDatabaseNames().ToList().Count > 0)
                Client.DropDatabase(Database);

            var db = Client.GetDatabase(Database);

            if (db != null)
                await db.DropCollectionAsync(JsonCollection);

            Client.DropDatabase(Database);

            if (GroupService == null)
                GroupService = new GroupService(db ?? Client.GetDatabase(Database));

            if (FileService == null)
                FileService = new FileService(db ?? Client.GetDatabase(Database), JsonCollection, GroupService);

            return FileService;
        }
    }
}

using MongoDB.Driver;
using Xunit;
using System.Threading;
using System.Threading.Tasks;
using EspraAPI.Service;
using System.IO;
using System.Linq;
using static EspraAPI.Util;

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
        private static readonly string Group = "TestF";
        private static CancellationTokenSource CancellationTokenSource = new();

        public FileServiceUnitTest()
        {
            Client = new MongoClient("mongodb://localhost:27017");

            if (Client.ListDatabaseNames().ToList().Contains(Database))
            {
                Client.DropDatabase(Database);
            }
        }

        [Fact(DisplayName = "Upload documents and verify the MIMETYPE & bytes lenght")]
        public async Task Upload_Document() 
        {
            CancellationTokenSource = new CancellationTokenSource();

            var path = GetPathFor("Documents");

            var filePaths = Directory.GetFiles(path);

            var fileService = await CreateService();

            for (var i = 0; i < filePaths.Length; i++)
            {
                var bytes = File.ReadAllBytes(filePaths[i]);

                var extension = Path.GetExtension(filePaths[i]);

                Assert.True(await fileService.AddAsync(Group, extension, bytes, CancellationTokenSource.Token));

                var groupInfo = await fileService.GroupService.GetGroupInfoAsync(Group, CancellationTokenSource.Token);

                Assert.NotNull(groupInfo);

                var id = groupInfo.FileIds[i];

                var document = await fileService.GetDocumentByIdAsync(id, CancellationTokenSource.Token);

                Assert.Equal(GetDocumentMIMEType(extension), document.Item2);

                Assert.Equal(bytes.Length, document.Item1.Length);
            }

        }

        [Fact(DisplayName = "Upload images and verify the MIMETYPE & bytes lenght")]
        public async Task Upload_Image() 
        {
            CancellationTokenSource = new CancellationTokenSource();

            var path = GetPathFor("Images");

            var filePaths = Directory.GetFiles(path);

            var fileService = await CreateService();

            for (var i = 0; i < filePaths.Length; i++)
            {
                var bytes = File.ReadAllBytes(filePaths[i]);

                var extension = Path.GetExtension(filePaths[i]);

                Assert.True(await fileService.AddAsync(Group, extension, bytes, CancellationTokenSource.Token));

                var groupInfo = await fileService.GroupService.GetGroupInfoAsync(Group, CancellationTokenSource.Token);

                Assert.NotNull(groupInfo);

                var id = groupInfo.FileIds[i];

                var document = await fileService.GetImageByIdAsync(id, CancellationTokenSource.Token);

                Assert.Equal(GetImageMIMEType(extension), document.Item2);

                Assert.Equal(bytes.Length, document.Item1.Length);
            }

        }

        [Fact(DisplayName = "Upload a document and a image, delete them, verify that the are have been deleted")]
        public async Task Delete_Files()
        {
            CancellationTokenSource = new CancellationTokenSource();

            var imagesPath = GetPathFor("Images");
            var documentsPath = GetPathFor("Documents");

            var imagePath = Directory.GetFiles(imagesPath)[0];
            var imageExtension = Path.GetExtension(imagePath);
            var imageBytes = File.ReadAllBytes(imagePath);

            var documentPath = Directory.GetFiles(documentsPath)[0];
            var documentExtension = Path.GetExtension(documentPath);
            var documentBytes = File.ReadAllBytes(documentPath);

            var fileService = await CreateService();

            Assert.True(await fileService.AddAsync(Group, imageExtension, imageBytes, CancellationTokenSource.Token));
            Assert.True(await fileService.AddAsync(Group, documentExtension, documentBytes, CancellationTokenSource.Token));

            var groupInfo = await fileService.GroupService.GetGroupInfoAsync(Group, CancellationTokenSource.Token);

            Assert.NotEmpty(groupInfo.FileIds);

            foreach (var fileId in groupInfo.FileIds)
                await fileService.DeleteByIdAsync(fileId, CancellationTokenSource.Token);

            groupInfo = await fileService.GroupService.GetGroupInfoAsync(Group, CancellationTokenSource.Token);

            Assert.Empty(groupInfo.FileIds);
        }

        public static string GetPathFor(string directory)
        {
            var path = Directory.GetCurrentDirectory();

            var dir = Directory.GetDirectories(path);

            var fileDirectoryPath = dir.Where(i => i.EndsWith("Files")).FirstOrDefault();

            var dirPath = Path.Combine(fileDirectoryPath ?? string.Empty, directory);

            return dirPath;
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

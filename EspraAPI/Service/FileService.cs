using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace EspraAPI.Service
{
    public class FileService
    {
        private readonly string CollectionName;
        private IMongoCollection<FileData>? FileCollection;
        private IMongoDatabase Database { get; set; }
        public  GroupService GroupService { get; private set; }
        [ActivatorUtilitiesConstructor]
        public FileService(IMongoClient mongoClient, IConfiguration configuration, GroupService groupService)
        {
            Database = mongoClient.GetDatabase(configuration["MONGO:DATBASE"]);
            CollectionName = configuration["MONGO:FILE_COLLECTION"];
            GroupService = groupService;
        }

        public FileService(IMongoDatabase mongoDatabase, string collection, GroupService groupService)
        {
            Database = mongoDatabase;
            CollectionName = collection;
            GroupService = groupService;
        }

        public async Task<bool> AddAsync(string group, string extension, byte[] data, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            FileCollection = Database.GetCollection<FileData>(CollectionName);

            var fileData = new FileData
            {
                GroupId = group,
                Extension = extension,
                Data = data,
                DateAdded = DateTime.Now.ToString(Util.DATE_FORMAT),
                LastModified = DateTime.Now.ToString(Util.DATE_FORMAT)
            };

            await FileCollection.InsertOneAsync(fileData, cancellationToken: token);

            return true;
        }

        public async Task<(byte[], string)> GetDocumentById(string id, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            FileCollection = Database.GetCollection<FileData>(CollectionName);

            var document = await (await FileCollection.FindAsync(i => i.Id == id, cancellationToken: token)).FirstOrDefaultAsync(token);

            if (document != null)
                return (document.Data, Util.GetDocumentMIMEType(document.Extension));

            return (new byte[] { 0 }, "text/plain");
        }

        public async Task<(byte[], string)> GetImageById(string id, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            FileCollection = Database.GetCollection<FileData>(CollectionName);

            var document = await (await FileCollection.FindAsync(i => i.Id == id, cancellationToken: token)).FirstOrDefaultAsync(token);

            if (document != null)
                return (document.Data, Util.GetImageMIMEType(document.Extension));

            return (new byte[] { 0 }, "text/plain");
        }

        public async Task<byte[]> GetOne(string group, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            FileCollection = Database.GetCollection<FileData>(CollectionName);

            return (await FileCollection.FindAsync(i => i.GroupId == group, cancellationToken: token)).First(cancellationToken: token).Data;
        }
    }

    public class FileData
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        public string GroupId { get; set; } = string.Empty;

        public string Extension { get; set; } = string.Empty;

        public byte[] Data { get; set; } = new byte[] { 0 };

        public string DateAdded { get; set; } = string.Empty;

        public string LastModified { get; set; } = string.Empty;
    }
}

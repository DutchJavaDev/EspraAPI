using MongoDB.Driver;
using EspraAPI.Service.Models;

namespace EspraAPI.Service
{
    public class GroupService
    {
        private readonly string CollectionName = "groupinfo_storage";
        private IMongoCollection<GroupInfo>? GroupInfoCollection;
        private IMongoDatabase Database { get; set; }

        [ActivatorUtilitiesConstructor]
        public GroupService(IMongoClient mongoClient, IConfiguration configuration)
        {
            Database = mongoClient.GetDatabase(configuration["MONGO:DATBASE"]);
        }

        public GroupService(IMongoDatabase mongoDatabase)
        {
            Database = mongoDatabase;
        }

        public async Task<GroupInfo> GetGroupInfoAsync(string name, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            GroupInfoCollection = Database.GetCollection<GroupInfo>(CollectionName);

            return await (await GroupInfoCollection.FindAsync(i => i.GroupName == name, cancellationToken: token)).FirstAsync(cancellationToken: token);
        }

        public async Task<bool> AddJsonIdAsync(string groupName, string jsonId, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            GroupInfoCollection = Database.GetCollection<GroupInfo>(CollectionName);

            var group = await (await GroupInfoCollection.FindAsync(i => i.GroupName == groupName, cancellationToken: token)).FirstOrDefaultAsync(cancellationToken: token);

            if (group == null)
            {
                group = new GroupInfo { GroupName = groupName };

                group.JsonIds.Add(jsonId);

                await GroupInfoCollection.InsertOneAsync(group, cancellationToken: token);

                return true;
            }
            else
            {
                var jsonIds = group.JsonIds;

                jsonIds.Add(jsonId);

                var updateFilter = Builders<GroupInfo>.Filter.Eq(nameof(GroupInfo.GroupName), groupName);

                var jsonIdUpdate = Builders<GroupInfo>.Update.Set(nameof(GroupInfo.JsonIds), jsonIds);

                var updateResult = await GroupInfoCollection.UpdateOneAsync(updateFilter, jsonIdUpdate, cancellationToken: token);

                return updateResult.IsAcknowledged;
            }
        }

        public async Task<bool> RemoveJsonIdAsync(string groupName, string jsonId, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            GroupInfoCollection = Database.GetCollection<GroupInfo>(CollectionName);

            var group = await (await GroupInfoCollection.FindAsync(i => i.GroupName == groupName, cancellationToken: token)).FirstOrDefaultAsync(cancellationToken: token);

            if (group == null)
                return false;

            var jsonIds = group.JsonIds;

            jsonIds.Remove(jsonId);

            var updateFilter = Builders<GroupInfo>.Filter.Eq(nameof(GroupInfo.GroupName), groupName);

            var jsonIdUpdate = Builders<GroupInfo>.Update.Set(nameof(GroupInfo.JsonIds), jsonIds);

            var updateResult = await GroupInfoCollection.UpdateOneAsync(updateFilter, jsonIdUpdate, cancellationToken: token);

            return updateResult.IsAcknowledged;
        }

        public async Task<bool> AddFileIdAsync(string groupName, string fileId, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            GroupInfoCollection = Database.GetCollection<GroupInfo>(CollectionName);

            var group = await (await GroupInfoCollection.FindAsync(i => i.GroupName == groupName, cancellationToken: token)).FirstOrDefaultAsync(cancellationToken: token);

            if (group == null)
            {
                group = new GroupInfo { GroupName = groupName };

                group.JsonIds.Add(fileId);

                await GroupInfoCollection.InsertOneAsync(group, cancellationToken: token);

                return true;
            }
            else
            {
                var fileIds = group.FileIds;

                fileIds.Add(fileId);

                var updateFilter = Builders<GroupInfo>.Filter.Eq(nameof(GroupInfo.GroupName), groupName);

                var jsonIdUpdate = Builders<GroupInfo>.Update.Set(nameof(GroupInfo.FileIds), fileIds);

                var updateResult = await GroupInfoCollection.UpdateOneAsync(updateFilter, jsonIdUpdate, cancellationToken: token);

                return updateResult.IsAcknowledged;
            }
        }

        public async Task<bool> RemoveFileIdAsync(string groupName, string fileId, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            GroupInfoCollection = Database.GetCollection<GroupInfo>(CollectionName);

            var group = await (await GroupInfoCollection.FindAsync(i => i.GroupName == groupName, cancellationToken: token)).FirstOrDefaultAsync(cancellationToken: token);

            if (group == null)
                return false;

            var fileIds = group.FileIds;

            fileIds.Remove(fileId);

            var updateFilter = Builders<GroupInfo>.Filter.Eq(nameof(GroupInfo.GroupName), groupName);

            var jsonIdUpdate = Builders<GroupInfo>.Update.Set(nameof(GroupInfo.FileIds), fileIds);

            var updateResult = await GroupInfoCollection.UpdateOneAsync(updateFilter, jsonIdUpdate, cancellationToken: token);

            return updateResult.IsAcknowledged;
        }
    }
}

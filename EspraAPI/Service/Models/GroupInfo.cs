using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EspraAPI.Service.Models
{
    public class GroupInfo
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        public string GroupName { get; set; } = string.Empty;

        public IList<string> JsonIds { get; set; } = new List<string>();

        public IList<string> FileIds { get; set; } = new List<string>();
    }
}

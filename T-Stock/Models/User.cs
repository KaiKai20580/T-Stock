using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace T_Stock.Models
{
    [BsonIgnoreExtraElements]
    public class User
    {
        
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        

        [BsonElement("email")]
        public string Email { get; set; }

        [BsonElement("password")]
        public string Password { get; set; }

        [BsonElement("role")]
        public string Role { get; set; }

        public string ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }
    }
}
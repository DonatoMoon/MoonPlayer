using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MoonPlayer.Models
{
    public class AudioFile
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string FileName { get; set; }
        public string FileAuthor { get; set; }
        public string FileGenre { get; set; }
        public int FileYear { get; set; }
        public int FileDuration { get; set; }
        public string FilePath { get; set; }
        public bool IsSelected { get; set; }
    }
}

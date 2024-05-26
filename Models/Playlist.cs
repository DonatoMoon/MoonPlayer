using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace MoonPlayer.Models
{
    public class Playlist
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string Name { get; set; }
        public List<AudioFile> AudioFiles { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan TotalDuration { get; set; }

        public void CalculateTotalDuration()
        {
            TotalDuration = TimeSpan.Zero;
            foreach (var audioFile in AudioFiles)
            {
                TotalDuration += TimeSpan.FromSeconds(audioFile.FileDuration);
            }
        }
    }
}

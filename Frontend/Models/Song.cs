using System;

namespace Tune.Frontend.Models
{
    public class Song
    {
        public int Id { get; set; }
        public string NameOfSong { get; set; } = string.Empty;
        public string CreatorUsername { get; set; } = string.Empty;
        public double Length { get; set; }
        public byte[] Mp3Bytes { get; set; } = Array.Empty<byte>();
        public DateTime UploadedAt { get; set; }

        // Convenience property for UI display
        public string FormattedLength => TimeSpan.FromSeconds(Length).ToString(@"m\:ss");
    }
}

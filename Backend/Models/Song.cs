namespace Backend.Models
{
    public class Song
    {
        public int Id { get; set; }
        public string NameOfSong { get; set; }
        public string CreatorUsername { get; set; }
        public double Length { get; set; }
        public byte[] Mp3Bytes { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}

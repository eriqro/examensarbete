using System;
using System.IO;
using System.Reflection.Metadata;
using System.Windows.Media.Imaging;

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

        public string FormattedLength => TimeSpan.FromSeconds(Length).ToString(@"m\:ss");

        public byte[] ThumbnailBytes {get;set;} = Array.Empty<byte>(); 
        
        public BitmapImage? ThumbnailImage
        {
            get
            {
                if (ThumbnailBytes == null || ThumbnailBytes.Length == 0)
                    return null;

                try
                {
                    var image = new BitmapImage();
                    using (var mem = new MemoryStream(ThumbnailBytes))
                    {
                        mem.Position = 0;
                        image.BeginInit();
                        image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = mem;
                        image.EndInit();
                    }
                    image.Freeze();
                    return image;
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}

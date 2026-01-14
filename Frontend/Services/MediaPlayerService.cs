using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Tune.Frontend.Services
{
    public static class MediaPlayerService
    {
        private static MediaElement? _mediaElement;

        public static void Initialize(MediaElement mediaElement)
        {
            _mediaElement = mediaElement;
        }

        public static MediaElement? GetMediaElement()
        {
            return _mediaElement;
        }

        public static void PlaySong(string filePath)
        {
            if (_mediaElement != null)
            {
                _mediaElement.Source = new Uri(filePath);
                _mediaElement.Play();
            }
        }

        public static void Pause()
        {
            _mediaElement?.Pause();
        }

        public static void Play()
        {
            _mediaElement?.Play();
        }

        public static void Stop()
        {
            if (_mediaElement != null)
            {
                _mediaElement.Stop();
                _mediaElement.Source = null;
            }
        }
    }
}

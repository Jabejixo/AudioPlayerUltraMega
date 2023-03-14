using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace AudioPlayer
{
    public class Track
    {

        public BitmapImage Image { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Path { get; set; }
        public string TitleWithExe { get; set; }
        public Track(BitmapImage image, string title, string author, string path, string titleWithExe)
        {
            Image = image;
            Title = title;
            Author = author;
            Path = path;
            TitleWithExe = titleWithExe;
        }

    }
}

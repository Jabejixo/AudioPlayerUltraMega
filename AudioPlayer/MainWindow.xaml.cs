using MaterialDesignThemes.Wpf;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using TagLib;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static System.Net.WebRequestMethods;
using System.Collections.ObjectModel;

namespace AudioPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        //надо сделать еще одно окно куда будет выводиться вся информация о песне
        //поиск
        //избавиться от бага в папке с понравившимися песнями
        //добавить парочку полезных функций
        
        private TimeSpan position;
        private TimeSpan Time;
        private TimeSpan _remainingTime;
        public TimeSpan time
        {
            get { return Time; }
            set { Time = value; OnPropertyChanged("time"); }
        }

        public TimeSpan RemainingTime
        {
            get { return _remainingTime; }
            set { _remainingTime = value; OnPropertyChanged("RemainingTime"); }
        }

        public ObservableCollection<Track> Tracks = new();
        public ObservableCollection<Track> TracksCopy = new();
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
        }
        static int volumeLevel = 0;
        static int songindex = 0;
        static bool finded = false;
        static bool mix = false;
        static bool restart = false;
        static bool playOrStop;
        static string folderPath = "";
        static string path = @"C:\music\loved";
        static int rndindex = 0;
        string[] files;
        BitmapImage cover;
        System.Timers.Timer timer = new System.Timers.Timer(1000);
        Random rng = new Random();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MediaPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.NaturalDuration.HasTimeSpan) MediaSlider.Maximum = mediaPlayer.NaturalDuration.TimeSpan.Ticks;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                MediaSlider.Value = mediaPlayer.Position.Ticks;
                if (mediaPlayer.NaturalDuration.HasTimeSpan)
                {
                    RemainingTime = mediaPlayer.NaturalDuration.TimeSpan - mediaPlayer.Position;
                }
                time = mediaPlayer.Position;
                if (mediaPlayer.NaturalDuration.HasTimeSpan)
                {
                    if (time == mediaPlayer.NaturalDuration.TimeSpan) 
                        {
                            if (restart == false & mix == false) 
                            {
                                NextSong();
                            }
                            else if (restart == false & mix == true)
                            {
                                NextSong(rndindex);
                            }
                            else
                            {
                                RestartSong();
                            }
                        }
                }
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Unwrap_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                WindowState = WindowState.Maximized;
            }
            else
            {
                WindowState = WindowState.Normal;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RollUp_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlayOrPause_Click(object sender, RoutedEventArgs e)
        {
            if (playOrStop == true)
            {
                Pause();
            }
            else
            {
                Play();
            }
        }


        private void Play()
        {
            playOrStop = true;
            mediaPlayer.Position = position;
            mediaPlayer.Play();
            var icon = new PackIcon { Kind = PackIconKind.Pause };
            PlayOrPause.Content = icon;
            timer.Enabled = true;
        }

        private void Pause()
        {
            playOrStop = false;
            position = mediaPlayer.Position;
            mediaPlayer.Stop();
            var icon = new PackIcon { Kind = PackIconKind.Play };
            PlayOrPause.Content = icon;
            timer.Enabled = false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SoundKill_Click(object sender, RoutedEventArgs e)
        {
            NameSong.Text = "";
            mediaPlayer.Source = null;
            mediaPlayer.Stop();
            Liked.Foreground = new SolidColorBrush(Colors.Black);
            BitmapImage();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SoundRestart_Click(object sender, RoutedEventArgs e)
        {
            if (restart == false) { restart = true; SoundRestart.Foreground = new SolidColorBrush(Colors.White); }
            else { restart = false; SoundRestart.Foreground = new SolidColorBrush(Colors.Black); }
        }

        private void RestartSong()
        {
            MediaSlider.Value = 0; 
            mediaPlayer.Position = new TimeSpan(Convert.ToInt64(MediaSlider.Value));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Mix_Click(object sender, RoutedEventArgs e)
        {
            Mixer();
        }

        private void Mixer()
        {
            if (mix == false)
            {

                foreach (var track in Tracks)
                {
                    TracksCopy.Add(track);
                }
                Mix.Foreground = new SolidColorBrush(Colors.White);
                mix = true;
                int n = Tracks.Count;
                while (n > 1)
                {
                    n--;
                    int k = rng.Next(n + 1);
                    (Tracks[n], Tracks[k]) = (Tracks[k], Tracks[n]);
                }
            }
            else
            {
                Mix.Foreground = new SolidColorBrush(Colors.Black);
                mix = false;
                Tracks.Clear();
                foreach (var track in TracksCopy)
                {
                    Tracks.Add(track);
                }
                TracksCopy.Clear();
                FindIndex();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Liked_Click(object sender, RoutedEventArgs e)
        {
            LikeFolder(songindex);
        }

        private void LikeFolder(int index)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            if (!System.IO.File.Exists(path + "/" + Tracks[index].TitleWithExe))
            {
                System.IO.File.Copy(folderPath + "/" + Tracks[index].TitleWithExe, path + "/" + Tracks[index].TitleWithExe, true);
            }
            else
            {
                System.IO.File.Delete(path + "/" + Tracks[index].TitleWithExe);
                if (Tracks[index].Path == path + "\\" + Tracks[index].TitleWithExe)
                {
                    Tracks.RemoveAt(index);
                    Liked.Foreground = new SolidColorBrush(Colors.Black);
                }
            }
            CheckLiked(index);
        }

        private void CheckLiked(int index)
        {
            if (index != -1)
            {
                if (System.IO.File.Exists(path + "\\" + Tracks[index].TitleWithExe))
                {
                    Liked.Foreground = new SolidColorBrush(Colors.White);
                }
                else
                {
                    Liked.Foreground = new SolidColorBrush(Colors.Black);
                }
            }
        }


        private void checkIMG(int index)
        {
            if (index != -1)
            {
                var file = TagLib.File.Create(Tracks[index].Path);
                if (file.Tag.Pictures.Length > 0)
                {
                    var bin = file.Tag.Pictures[0].Data.Data;
                    var image = new BitmapImage();
                    using (var mem = new MemoryStream(bin))
                    {
                        mem.Position = 0;
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = mem;
                        image.EndInit();
                    }
                    ImageSong.Source = image;
                }
                else
                {
                    BitmapImage();
                }

            }
        }

        private void BitmapImage()
        {
            BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(@"1234.png", UriKind.Relative);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            ImageSong.Source = bitmap;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (mix == false) NextSong(); else NextSong(songindex);
        }
        private void NextSong()
        {
            if (songindex != Tracks.Count - 1)
            {
                songindex++;
                MediaSlider.Value = 0;
                SongsList.SelectedIndex = songindex;
                mediaPlayer.Source = new Uri(Tracks[songindex].Path);
                mediaPlayer.Play();
            }
            else
            {
                songindex = Tracks.Count - Tracks.Count;
                MediaSlider.Value = 0;
                SongsList.SelectedIndex = songindex;
                mediaPlayer.Source = new Uri(Tracks[songindex].Path);
                mediaPlayer.Play();
            }
        }

        private void NextSong(int rndindex)
        {
            MediaSlider.Value = 0;
            int n = Tracks.Count;
            int sub = rndindex;
            do
            {
            rndindex = rng.Next(n);
            } while (rndindex == sub);
            mediaPlayer.Source = new Uri(Tracks[rndindex].Path);
            SongsList.SelectedIndex = rndindex;
            SongName(rndindex);
            CheckLiked(rndindex);
            checkIMG(rndindex);
            mediaPlayer.Play();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VolumeChange_Click(object sender, RoutedEventArgs e)
        {
            switch (volumeLevel)
            {
                case 0:
                    volumeLevel = 1;
                    VolumeSlider.Value = 0;
                    mediaPlayer.Volume = VolumeSlider.Value / 100;
                    break; 
                case 1:
                    volumeLevel = 2;
                    VolumeSlider.Value = 25;
                    mediaPlayer.Volume = VolumeSlider.Value / 100;
                    break; 
                case 2:
                    volumeLevel = 3;
                    VolumeSlider.Value = 50;
                    mediaPlayer.Volume = VolumeSlider.Value / 100;
                    break; 
                case 3:
                    volumeLevel = 4;
                    VolumeSlider.Value = 75;
                    mediaPlayer.Volume = VolumeSlider.Value / 100;
                    break; 
                case 4:
                    volumeLevel = 0;
                    VolumeSlider.Value = 100;
                    mediaPlayer.Volume = VolumeSlider.Value / 100;
                    break;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChoiceSong_Click(object sender, RoutedEventArgs e)
        {
            ChoiceFolder();
        }

        private void ChoiceFolder()
        {
            string extensionFilter = "*.mp3|*.m4a|*.flac|*.wav|*.avi";
            clearInfo();
            CommonOpenFileDialog dialog = new CommonOpenFileDialog { IsFolderPicker = true };
            CommonFileDialogResult result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                folderPath = dialog.FileName;
                files = Directory.GetFiles(dialog.FileName).Where(f => extensionFilter.Contains(System.IO.Path.GetExtension(f).ToLower())).ToArray();
                foreach (var file in files)
                {
                    var File = TagLib.File.Create(file);
                    string title = File.Tag.Title;
                    string artist = File.Tag.FirstArtist;
                    string titleWithExe = System.IO.Path.GetFileName(file);
                    artist ??= "Uncnown";
                    string path = file;
                    IPicture? pic = File.Tag.Pictures.FirstOrDefault();
                    if (pic != null)
                    {
                        MemoryStream stream = new MemoryStream(pic.Data.Data);
                        cover = new BitmapImage();
                        cover.BeginInit();
                        cover.StreamSource = stream;
                        cover.CacheOption = BitmapCacheOption.OnLoad;
                        cover.EndInit();
                    }
                    else
                    {
                        cover = new();
                        cover.BeginInit();
                        cover.UriSource = new Uri(@"1234.png", UriKind.Relative);
                        cover.CacheOption = BitmapCacheOption.OnLoad;
                        cover.EndInit();
                    }
                    pic = null;
                    Track track = new(cover, title, artist, path, titleWithExe);
                    Tracks.Add(track);
                    SongsList.ItemsSource = Tracks;
                }
            }
        }

        private void clearInfo()
        {
            folderPath = "";
            Tracks.Clear();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (mix == false) BackSong(); else NextSong(0);
        }
        private void BackSong()
        {
            if (songindex != 0)
            {
                MediaSlider.Value = 0;
                songindex--;
                SongsList.SelectedIndex = songindex;
                mediaPlayer.Source = new Uri(Tracks[songindex].Path);
                mediaPlayer.Play();
            }
            else
            {
                MediaSlider.Value = 0;
                songindex = Tracks.Count - 1;
                SongsList.SelectedIndex = songindex;
                mediaPlayer.Source = new Uri(Tracks[songindex].Path);
                mediaPlayer.Play();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SongsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SongsList.Items.Count != 0)
            {
                if (finded == false)
                {
                    songindex = SongsList.SelectedIndex;
                    if (songindex != -1)
                    {
                        CheckLiked(songindex);
                        checkIMG(songindex);
                        Play(songindex);
                    }
                    else
                    {
                        FindIndex();
                        CheckLiked(songindex);
                        checkIMG(songindex);
                    }
                }
                else
                {
                    finded = false;
                }
            }
        }
        private void Play(int songindex)
        {
            try
            {
                var icon = new PackIcon { Kind = PackIconKind.Pause };
                PlayOrPause.Content = icon;
                playOrStop = true;
                mediaPlayer.Source = new Uri(Tracks[songindex].Path);
                SongName(songindex);
                mediaPlayer.Play();
            }
            catch (Exception)
            {
                throw;
            }
        }
        private void SongName(int index)
        {
            NameSong.Text = $"{Tracks[index].Title}";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MediaSlider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            position = new TimeSpan(Convert.ToInt64(MediaSlider.Value));
            mediaPlayer.Position = new TimeSpan(Convert.ToInt64(MediaSlider.Value));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            mediaPlayer.Volume = VolumeSlider.Value/100;
        }
        private void FindIndex()
        {
            int n = -1;
            foreach (var track in Tracks)
            {
                n++;
                if (track.Title == NameSong.Text)
                {
                    finded = true;
                    songindex = n;
                    SongsList.SelectedIndex = n;
                    break;
                }
            }
        }
    }
}

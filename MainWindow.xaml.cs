using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Windows.Controls;
using System.Globalization;
using System.Windows.Data;
using System.ComponentModel;
using System.Diagnostics;
using MoonPlayer.Models;
using MoonPlayer.Services;

namespace MoonPlayer
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private bool isSelectMode = false;
        private bool isPlaying = false;
        public bool IsSelectMode
        {
            get => isSelectMode;
            set
            {
                if (isSelectMode != value)
                {
                    isSelectMode = value;
                    OnPropertyChanged(nameof(IsSelectMode));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        private MediaPlayer mediaPlayer = new MediaPlayer();
        private List<AudioFile> audioFiles = new List<AudioFile>();
        private List<Playlist> playlists = new List<Playlist>();
        private int currentAudioIndex = 0;
        private TimeSpan currentMediaPosition = TimeSpan.Zero;

        private IMongoDatabase database;
        private IMongoCollection<AudioFile> collection;


        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timer_Tick;
            timer.Start();

            var client = new MongoClient("mongodb://localhost:27017");
            database = client.GetDatabase("MoonPlayer");
            collection = database.GetCollection<AudioFile>("audio");

            LoadAudioFile();
            LoadPlaylists();
            songsListBox.ItemsSource = audioFiles;

            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;

        }
        private Trie trie = new Trie();

        private void LoadAudioFile()
        {
            audioFiles = collection.Find(_ => true).ToList();
            foreach (var file in audioFiles)
            {
                trie.Insert(file.FileName.ToLower(), file);
            }
        }

        private void LoadPlaylists()
        {
            var playlistCollection = database.GetCollection<Playlist>("playlists");
            playlists = playlistCollection.Find(_ => true).ToList();

            foreach (var playlist in playlists)
            {
                playlist.CalculateTotalDuration();
            }

            playlistsListBox.ItemsSource = playlists;
        }

        private void ShowAllSongs_Click(object sender, RoutedEventArgs e)
        {
            LoadAudioFile();
            songsListBox.ItemsSource = audioFiles;

            playlistsListBox.SelectedIndex = -1;  // фікс перелючення після показати усі пісні
            currentAudioIndex = -1;
        }

        private void PlaylistsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (playlistsListBox.SelectedItem is Playlist selectedPlaylist)
            {
                audioFiles = selectedPlaylist.AudioFiles;
                songsListBox.ItemsSource = null;
                songsListBox.ItemsSource = audioFiles;
            }
        }
        private void TogglePlayStop_Click(object sender, RoutedEventArgs e)
        {
            if (!isPlaying)
            {
                PlayMedia();
                togglePlayStopButton.Content = "Stop";
            }
            else
            {
                StopMedia();
                togglePlayStopButton.Content = "Play";
            }
        }
        private void PlayMedia()
        {
            if (audioFiles != null && audioFiles.Any() && currentAudioIndex >= 0 && currentAudioIndex < audioFiles.Count)
            {
                var selectedFile = audioFiles[currentAudioIndex];
                mediaPlayer.Open(new Uri(selectedFile.FilePath, UriKind.Absolute));
                mediaPlayer.Position = currentMediaPosition;
                mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
                mediaPlayer.Play();
                isPlaying = true;
            }
        }

        private void StopMedia()
        {
            currentMediaPosition = mediaPlayer.Position; // Збереження поточної позиції
            mediaPlayer.Pause();
            isPlaying = false;
        }


        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            currentAudioIndex = (currentAudioIndex + 1) % audioFiles.Count;
            currentMediaPosition = TimeSpan.Zero; // Скидання позиції при переключенні треків
            PlaySelectedAudio();
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            currentAudioIndex--;
            if (currentAudioIndex < 0)
                currentAudioIndex = audioFiles.Count - 1;
            currentMediaPosition = TimeSpan.Zero; // Скидання позиції при переключенні треків
            PlaySelectedAudio();
        }


        private void PlaySelectedAudio()
        {
            if (audioFiles != null && audioFiles.Any() && currentAudioIndex >= 0 && currentAudioIndex < audioFiles.Count)
            {
                var selectedFile = audioFiles[currentAudioIndex];
                mediaPlayer.Open(new Uri(selectedFile.FilePath, UriKind.Absolute));
                mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
                mediaPlayer.Play();
                isPlaying = true;

                togglePlayStopButton.Content = "Stop";
                songsListBox.SelectedIndex = currentAudioIndex;
            }
        }
        private void MediaPlayer_MediaOpened(object sender, EventArgs e)
        {
            if (mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                progressSlider.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
            }
        }
        private void MediaPlayer_MediaEnded(object sender, EventArgs e)
        {
            NextSong();
        }
        private void NextSong()
        {
            if (audioFiles != null && audioFiles.Any())
            {
                currentAudioIndex = (currentAudioIndex + 1) % audioFiles.Count;
                currentMediaPosition = TimeSpan.Zero;
                PlaySelectedAudio();
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (mediaPlayer.Source != null)
                progressSlider.Value = mediaPlayer.Position.TotalSeconds;
        }
        private void progressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                TimeSpan newPosition = TimeSpan.FromSeconds(progressSlider.Value);
                mediaPlayer.Position = newPosition;
            }
        }


        private void SongsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (songsListBox.SelectedIndex != -1)
            {
                currentAudioIndex = songsListBox.SelectedIndex;
                currentMediaPosition = TimeSpan.Zero;
                PlaySelectedAudio();
            }
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = (sender as ComboBox).SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                string tag = selectedItem.Tag.ToString();
                Func<AudioFile, AudioFile, int> comparison;

                switch (tag)
                {
                    case "NameAscending":
                        comparison = (a, b) => string.Compare(a.FileName, b.FileName);
                        break;
                    case "NameDescending":
                        comparison = (a, b) => string.Compare(b.FileName, a.FileName);
                        break;
                    case "YearAscending":
                        comparison = (a, b) => a.FileYear.CompareTo(b.FileYear);
                        break;
                    case "YearDescending":
                        comparison = (a, b) => b.FileYear.CompareTo(a.FileYear);
                        break;
                    case "DurationAscending":
                        comparison = (a, b) => a.FileDuration.CompareTo(b.FileDuration);
                        break;
                    case "DurationDescending":
                        comparison = (a, b) => b.FileDuration.CompareTo(a.FileDuration);
                        break;
                    default:
                        return;
                }

                Sorter.QuickSort(audioFiles, comparison);
                songsListBox.ItemsSource = null;
                songsListBox.ItemsSource = audioFiles;
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = searchTextBox.Text.ToLower();
            var filteredSongs = trie.Search(searchText);

            songsListBox.ItemsSource = null;
            songsListBox.ItemsSource = filteredSongs;
            songsListBox.Items.Refresh();
        }

        public void CreatePlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            IsSelectMode = true;
            songsListBox.Items.Refresh();
            savePlaylistButton.Visibility = Visibility.Visible;
        }

        public void SavePlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedSongs = audioFiles.Where(f => f.IsSelected).ToList();
            if (selectedSongs.Any())
            {
                string playlistName = PromptPlaylistName();

                if (!string.IsNullOrWhiteSpace(playlistName))
                {
                    Playlist newPlaylist = new Playlist
                    {
                        Name = playlistName,
                        AudioFiles = selectedSongs
                    };

                    var playlistCollection = database.GetCollection<Playlist>("playlists");
                    playlistCollection.InsertOne(newPlaylist);

                    MessageBox.Show("Плейлист успішно збережено!");
                }
                else
                {
                    MessageBox.Show("Назва плейлиста не може бути порожньою!");
                }
            }
            else
            {
                MessageBox.Show("Жодної пісні не вибрано!");
            }

            IsSelectMode = false;
            foreach (var song in audioFiles)
                song.IsSelected = false;

            songsListBox.Items.Refresh();
            savePlaylistButton.Visibility = Visibility.Collapsed;
        }

        public void DeletePlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            if (playlistsListBox.SelectedItem is Playlist selectedPlaylist)
            {
                var result = MessageBox.Show($"Ви впевнені, що хочете видалити плейлист? '{selectedPlaylist.Name}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    var playlistCollection = database.GetCollection<Playlist>("playlists");
                    playlistCollection.DeleteOne(Builders<Playlist>.Filter.Eq(p => p.Id, selectedPlaylist.Id));
                    MessageBox.Show("Плейлист успішно видалено!");

                    LoadPlaylists();
                }
            }
            else
            {
                MessageBox.Show("Будь ласка, виберіть плейлист для видалення.");
            }
        }


        private string PromptPlaylistName()
        {
            return Microsoft.VisualBasic.Interaction.InputBox("Введіть назву списку відтворення:", "Назва списку відтворення", "Новий список відтворення");
        }

        public void CreatePlaylistByDurationButton_Click(object sender, RoutedEventArgs e)
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox("Введіть тривалість (у секундах):", "Тривалість плейлиста", "600");
            if (int.TryParse(input, out int durationInSeconds))
            {
                var selectedSongs = GetSongsForDuration(durationInSeconds);

                if (selectedSongs.Any())
                {
                    Playlist newPlaylist = new Playlist
                    {
                        Name = $"Плейлист з наближеною тривалістю: {durationInSeconds} секунд",
                        AudioFiles = selectedSongs
                    };

                    newPlaylist.CalculateTotalDuration();

                    var playlistCollection = database.GetCollection<Playlist>("playlists");
                    playlistCollection.InsertOne(newPlaylist);

                    MessageBox.Show("Плейлист успішно створено!");

                    LoadPlaylists();
                }
                else
                {
                    MessageBox.Show("Не вдалося знайти відповідну комбінацію пісень для вказаної тривалості.");
                }
            }
            else
            {
                MessageBox.Show("Неправильно введена тривалість.");
            }
        }

        private List<AudioFile> GetSongsForDuration(int targetDuration)
        {
            List<AudioFile> bestCombination = new List<AudioFile>();
            FindBestCombination(audioFiles, new List<AudioFile>(), targetDuration, ref bestCombination);
            return bestCombination;
        }

        private void FindBestCombination(List<AudioFile> availableSongs, List<AudioFile> currentCombination, int targetDuration, ref List<AudioFile> bestCombination)
        {
            int currentDuration = currentCombination.Sum(song => song.FileDuration);

            if (currentDuration <= targetDuration && currentDuration > bestCombination.Sum(song => song.FileDuration))
            {
                bestCombination = new List<AudioFile>(currentCombination);
            }

            if (currentDuration >= targetDuration)
            {
                return;
            }

            for (int i = 0; i < availableSongs.Count; i++)
            {
                var remainingSongs = availableSongs.Skip(i + 1).ToList();
                var newCombination = new List<AudioFile>(currentCombination) { availableSongs[i] };
                FindBestCombination(remainingSongs, newCombination, targetDuration, ref bestCombination);
            }
        }
        private void AddSongButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Audio files (*.mp3;*.wav)|*.mp3;*.wav";

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                var file = TagLib.File.Create(filePath);
                TimeSpan duration = file.Properties.Duration;

                // Відкриття діалогового вікна для введення даних
                InputAudioDetailsWindow inputWindow = new InputAudioDetailsWindow(filePath, duration);
                if (inputWindow.ShowDialog() == true)
                {
                    var newAudioFile = inputWindow.AudioFile;

                    // Збереження в базі даних
                    collection.InsertOne(newAudioFile);

                    // Додавання до списку
                    audioFiles.Add(newAudioFile);
                    trie.Insert(newAudioFile.FileName.ToLower(), newAudioFile);
                    songsListBox.ItemsSource = null;
                    songsListBox.ItemsSource = audioFiles;
                }
            }
        }
        private void DeleteSongButton_Click(object sender, RoutedEventArgs e)
        {
            if (songsListBox.SelectedItem is AudioFile selectedFile)
            {
                var result = MessageBox.Show($"Ви впевнені, що хочете видалити пісню '{selectedFile.FileName}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    // Видалення з бази даних
                    var deleteFilter = Builders<AudioFile>.Filter.Eq(f => f.Id, selectedFile.Id);
                    collection.DeleteOne(deleteFilter);

                    // Видалення зі списку
                    audioFiles.Remove(selectedFile);
                    trie = new Trie();
                    foreach (var file in audioFiles)
                    {
                        trie.Insert(file.FileName.ToLower(), file);
                    }
                    songsListBox.ItemsSource = null;
                    songsListBox.ItemsSource = audioFiles;
                    MessageBox.Show("Пісню успішно видалено!");
                }
            }
            else
            {
                MessageBox.Show("Будь ласка, виберіть пісню для видалення.");
            }
        }




    }
}
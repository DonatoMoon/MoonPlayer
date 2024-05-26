using System;
using System.Globalization;
using System.IO;
using System.Windows;
using MoonPlayer.Models;

namespace MoonPlayer
{
    public partial class InputAudioDetailsWindow : Window
    {
        public AudioFile AudioFile { get; private set; }

        public InputAudioDetailsWindow(string filePath, TimeSpan duration)
        {
            InitializeComponent();
            filePathTextBox.Text = filePath;
            durationTextBox.Text = duration.TotalSeconds.ToString();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Заміна коми на крапку для правильного парсингу
            string durationText = durationTextBox.Text.Replace(',', '.');

            if (int.TryParse(yearTextBox.Text, out int year) && double.TryParse(durationText, NumberStyles.Float, CultureInfo.InvariantCulture, out double duration))
            {
                AudioFile = new AudioFile
                {
                    FileName = Path.GetFileNameWithoutExtension(filePathTextBox.Text), // Назва файлу без розширення
                    FileAuthor = authorTextBox.Text,
                    FileGenre = genreTextBox.Text,
                    FileYear = year,
                    FileDuration = (int)duration,
                    FilePath = filePathTextBox.Text
                };
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Будь ласка, введіть коректні дані.");
            }
        }


    }
}

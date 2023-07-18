using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using NAudio.Wave;
using Avalonia.Controls;
using System.Threading.Tasks;

namespace MusicPlayer.ViewModels
{
    public sealed partial class MainViewModel : ObservableObject, IDisposable
    {
        private readonly IDisposable _musicTimerSubscription;
        private Window _mainWindow;

        public MainViewModel(Window mainWindow)
        {
            _mainWindow = mainWindow;
            PropertyChanged += OnPropertyChanged;
            _musicTimerSubscription = Observable
                .Interval(TimeSpan.FromSeconds(1))
                .Subscribe(_ => UpdateTimers());
        }
        
        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(NormalProgress))
            {
                if (_audioFileReader == null) return;

                var newPosition = TimeSpan.FromSeconds(NormalProgress * _audioFileReader.TotalTime.TotalSeconds);
                _audioFileReader.CurrentTime = newPosition;
            }

            if (e.PropertyName == nameof(SelectedSong))
            {
                if (!string.IsNullOrEmpty(SelectedSong))
                    Play(Path.Combine(_musicFolder, SelectedSong + ".mp3"));
            }
        }

        [ObservableProperty]
        private TimeSpan _normalTimer;

        [ObservableProperty]
        private double _normalProgress;

        [ObservableProperty]
        private TimeSpan _musicTimer;

        [ObservableProperty]
        private ObservableCollection<string> _songs = new();

        [ObservableProperty]
        private string _selectedSong = string.Empty;

        [ObservableProperty]
        private int _currentSongIndex;

        [ObservableProperty]
        private string _playButtonText = "Play";

        private string _musicFolder = string.Empty;
        private WaveOutEvent? _waveOut;
        private Mp3FileReader? _audioFileReader;
        private long _pausedPosition;

        private bool _isPlaying;
        private bool _isPaused;

        public async Task SelectMusicFolderAsync()
        {
            var folderDialog = new OpenFolderDialog
            {
                Title = "Select Music Folder"
            };

            var result = await folderDialog.ShowAsync(_mainWindow);

            if (result != null)
            {
                LoadSongs(result);
            }
        }

        public void PlayPause()
        {
            if (_isPlaying)
            {
                if (_isPaused)
                    Resume();
                else
                    Pause();

                return;
            }

            Play(Path.Combine(_musicFolder, $"{SelectedSong}.mp3"));
        }

        private void Play(string song)
        {
            _waveOut?.Stop();
            _waveOut?.Dispose();
            _waveOut = null;

            var audioStream = File.OpenRead(song);

            _waveOut = new WaveOutEvent();
            _audioFileReader = new Mp3FileReader(audioStream);
            _waveOut.Init(_audioFileReader);

            _waveOut.Play();
            _isPlaying = true;
            _isPaused = false;
            PlayButtonText = "Pause";
        }

        private void Pause()
        {
            _waveOut?.Pause();
            _pausedPosition = _audioFileReader!.Position;
            _isPaused = true;
            PlayButtonText = "Resume";
        }

        private void Resume()
        {
            _waveOut?.Play();
            _audioFileReader!.Position = _pausedPosition;
            _isPaused = false;
            PlayButtonText = "Pause";
        }

        public void Stop()
        {
            _waveOut?.Stop();
            _waveOut?.Dispose();
            _waveOut = null;

            _isPlaying = false;
            _isPaused = false;
            PlayButtonText = "Play";
        }

        public void Previous()
        {
            if (CurrentSongIndex == 0)
                CurrentSongIndex = Songs.Count - 1;
            else
                CurrentSongIndex--;

            SelectedSong = Songs[CurrentSongIndex];
        }

        public void Next()
        {
            if (CurrentSongIndex + 1 < Songs.Count)
                CurrentSongIndex++;
            else
                CurrentSongIndex = 0;

            SelectedSong = Songs[CurrentSongIndex];
        }

        public void LoadSongs(string musicFolder)
        {
            _musicFolder = musicFolder;
            var songFiles = Directory.GetFiles(musicFolder, "*.mp3");

            foreach (var songFile in songFiles)
            {
                var songTitle = Path.GetFileNameWithoutExtension(songFile);
                Songs.Add(songTitle);
            }
        }

        private void UpdateTimers()
        {
            if (!_isPlaying) return;

            var currentTime = _audioFileReader!.CurrentTime;
            var totalDuration = _audioFileReader.TotalTime;

            var remainingTime = totalDuration - currentTime;

            if (remainingTime >= TimeSpan.Zero)
            {
                MusicTimer = remainingTime;
                NormalTimer = currentTime;
                NormalProgress = currentTime.TotalSeconds / totalDuration.TotalSeconds;
            }
            else
            {
                MusicTimer = TimeSpan.Zero;
                NormalTimer = totalDuration;
                NormalProgress = 1;
            }
        }

        public void Dispose()
        {
            _waveOut?.Dispose();
            _musicTimerSubscription.Dispose();
        }
    }
}

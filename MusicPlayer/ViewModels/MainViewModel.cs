using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using NAudio.Wave;
using Avalonia.Controls;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;

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

            Dispatcher.UIThread.Invoke(() =>
            {
                PreviousCommand.NotifyCanExecuteChanged();
                NextCommand.NotifyCanExecuteChanged();
                PlayPauseCommand.NotifyCanExecuteChanged();
                StopCommand.NotifyCanExecuteChanged();
            });
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

        [ObservableProperty]
        private string _playButtonUri = "avares://MusicPlayer/Assets/play.png";

        private string _musicFolder = string.Empty;
        private WaveOutEvent? _waveOut;
        private Mp3FileReader? _audioFileReader;
        private long _pausedPosition;

        private bool _isPlaying;
        private bool _isPaused;

        public async Task SelectMusicFolderAsync()
        {
            var result = await _mainWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                AllowMultiple = false,
                Title = "Select Music Folder"
            });

            LoadSongs(result[0].Path.AbsolutePath);
        }

        private void LoadSongs(string musicFolder)
        {
            if (_isPaused || _isPlaying)
            {
                Stop();
            }

            Songs.Clear();

            _musicFolder = musicFolder;
            var songFiles = Directory.GetFiles(musicFolder, "*.mp3");

            foreach (var songFile in songFiles)
            {
                var songTitle = Path.GetFileNameWithoutExtension(songFile);
                Songs.Add(songTitle);
            }
        }

        [RelayCommand(CanExecute = nameof(CanPlayPause))]
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

        private bool CanPlayPause() => !string.IsNullOrEmpty(SelectedSong);

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

            PlayButtonUri = "avares://MusicPlayer/Assets/pause.png";
        }

        private void Pause()
        {
            _waveOut?.Pause();
            _pausedPosition = _audioFileReader!.Position;
            _isPaused = true;
            PlayButtonUri = "avares://MusicPlayer/Assets/play.png";
        }

        private void Resume()
        {
            _waveOut?.Play();
            _audioFileReader!.Position = _pausedPosition;
            _isPaused = false;
            PlayButtonUri = "avares://MusicPlayer/Assets/pause.png";
        }

        [RelayCommand(CanExecute = nameof(CanStop))]
        public void Stop()
        {
            _waveOut?.Stop();
            _waveOut?.Dispose();
            _waveOut = null;

            _isPlaying = false;
            _isPaused = false;
            NormalProgress = 0;
            MusicTimer = TimeSpan.Zero;
            NormalTimer = TimeSpan.Zero;
            PlayButtonUri = "avares://MusicPlayer/Assets/play.png";
        }

        private bool CanStop() => _isPlaying;

        [RelayCommand(CanExecute = nameof(CanPreviousOrNext))]
        public void Previous()
        {
            if (CurrentSongIndex == 0)
                CurrentSongIndex = Songs.Count - 1;
            else
                CurrentSongIndex--;

            SelectedSong = Songs[CurrentSongIndex];
        }

        [RelayCommand(CanExecute = nameof(CanPreviousOrNext))]
        public void Next()
        {
            if (CurrentSongIndex + 1 < Songs.Count)
                CurrentSongIndex++;
            else
                CurrentSongIndex = 0;

            SelectedSong = Songs[CurrentSongIndex];
        }

        private bool CanPreviousOrNext() => Songs.Count > 0;

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
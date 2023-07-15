using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using NAudio.Wave;
using ReactiveUI;

namespace MusicPlayer
{
    public class MainWindowViewModel : ReactiveObject, IDisposable
    {
        private bool isPlaying;
        private bool isPaused;
        private WaveOutEvent waveOut;
        private Mp3FileReader audioFileReader;
        private long pausedPosition;
        private string selectedSong;
        private int currentSongIndex;
        private string musicFolder = @"C:\Users\Mark Edrian\RiderProjects\MusicPlayer1\MusicPlayer\Assets\music";
        private string playButtonText;
        private TimeSpan musicTimer;
        private TimeSpan normalTimer;
        private double normalProgress;
        private IDisposable musicTimerSubscription;

        public MainWindowViewModel()
        {
            PlayButtonText = "Play";
            PlayPauseCommand = ReactiveCommand.Create(PlayPause);
            StopCommand = ReactiveCommand.Create(Stop);
            NextCommand = ReactiveCommand.Create(Next);
            PreviousCommand = ReactiveCommand.Create(Previous);

            Songs = new ObservableCollection<string>();
            LoadSongs();

            musicTimerSubscription = Observable.Interval(TimeSpan.FromSeconds(1))
                .Subscribe(_ => UpdateTimers());
        }

        public ReactiveCommand<Unit, Unit> PlayPauseCommand { get; }
        public ReactiveCommand<Unit, Unit> StopCommand { get; }
        public ReactiveCommand<Unit, Unit> NextCommand { get; }
        public ReactiveCommand<Unit, Unit> PreviousCommand { get; }

        public string PlayButtonText
        {
            get => playButtonText;
            set => this.RaiseAndSetIfChanged(ref playButtonText, value);
        }

        public bool IsPlaying
        {
            get => isPlaying;
            set => this.RaiseAndSetIfChanged(ref isPlaying, value);
        }

        public bool IsPaused
        {
            get => isPaused;
            set => this.RaiseAndSetIfChanged(ref isPaused, value);
        }

        public ObservableCollection<string> Songs { get; }

        public TimeSpan MusicTimer
        {
            get => musicTimer;
            set => this.RaiseAndSetIfChanged(ref musicTimer, value);
        }

        public TimeSpan NormalTimer
        {
            get => normalTimer;
            set => this.RaiseAndSetIfChanged(ref normalTimer, value);
        }

        public double NormalProgress
        {
            get => normalProgress;
            set
            {
                if (Math.Abs(normalProgress - value) > 0.0001)
                {
                    normalProgress = value;
                    if (audioFileReader != null)
                    {
                        var newPosition = TimeSpan.FromSeconds(normalProgress * audioFileReader.TotalTime.TotalSeconds);
                        audioFileReader.CurrentTime = newPosition;
                    }

                    this.RaisePropertyChanged();
                }
            }
        }

        public string SelectedSong
        {
            get => selectedSong;
            set
            {
                if (selectedSong != value)
                {
                    selectedSong = value;
                    Play(Path.Combine(musicFolder, selectedSong + ".mp3"));
                    this.RaisePropertyChanged();
                }
            }
        }

        private void Play(string song)
        {
            waveOut?.Stop();
            waveOut?.Dispose();
            waveOut = null;

            var audioStream = File.OpenRead(song);

            waveOut = new WaveOutEvent();
            audioFileReader = new Mp3FileReader(audioStream);
            waveOut.Init(audioFileReader);

            waveOut.Play();
            IsPlaying = true;
            IsPaused = false;
            PlayButtonText = "Pause";
        }

        private void Pause()
        {
            waveOut?.Pause();
            pausedPosition = audioFileReader.Position;
            IsPaused = true;
            PlayButtonText = "Resume";
        }

        private void Resume()
        {
            waveOut?.Play();
            audioFileReader.Position = pausedPosition;
            IsPaused = false;
            PlayButtonText = "Pause";
        }

        private void Stop()
        {
            waveOut?.Stop();
            waveOut?.Dispose();
            waveOut = null;

            IsPlaying = false;
            IsPaused = false;
            PlayButtonText = "Play";
        }

        private void PlayPause()
        {
            if (IsPlaying)
            {
                if (IsPaused)
                    Resume();
                else
                    Pause();
            }
            else if (SelectedSong != null)
            {
                Play(Path.Combine(musicFolder, selectedSong + ".mp3"));
            }
        }

        private void Next()
        {
            currentSongIndex++;
            if (currentSongIndex >= Songs.Count)
                currentSongIndex = 0;
            SelectedSong = Songs[currentSongIndex];
        }

        private void Previous()
        {
            currentSongIndex--;
            if (currentSongIndex < 0)
                currentSongIndex = Songs.Count - 1;
            SelectedSong = Songs[currentSongIndex];
        }

        private void LoadSongs()
        {
            var songFiles = Directory.GetFiles(musicFolder, "*.mp3");

            foreach (var songFile in songFiles)
            {
                var songTitle = Path.GetFileNameWithoutExtension(songFile);
                Songs.Add(songTitle);
            }
        }

        private void UpdateTimers()
        {
            if (IsPlaying && audioFileReader != null)
            {
                var currentTime = audioFileReader.CurrentTime;
                var totalDuration = audioFileReader.TotalTime;

                var remainingTime = totalDuration - currentTime;

                if (remainingTime >= TimeSpan.Zero)
                {
                    MusicTimer = remainingTime;
                    NormalTimer = currentTime;

                    // Update the slider position without triggering the setter
                    normalProgress = currentTime.TotalSeconds / totalDuration.TotalSeconds;
                    this.RaisePropertyChanged(nameof(NormalProgress)); // Manually raise the PropertyChanged event for NormalProgress
                }
                else
                {
                    MusicTimer = TimeSpan.Zero;
                    NormalTimer = totalDuration;
                    NormalProgress = 1;
                }
            }
        }

        public void Dispose()
        {
            musicTimerSubscription?.Dispose();
        }
    }
}
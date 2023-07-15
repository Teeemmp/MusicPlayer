using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MusicPlayer.ViewModels;

namespace MusicPlayer.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            Loaded+= OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            const string musicFolder = @"C:\Users\ogie\Downloads\Music";
           _viewModel.LoadSongs(musicFolder);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
using System.Windows;
using System.Windows.Input;
using WinampWPF.ViewModels;
using WinampWPF.Views;

namespace WinampWPF;

public partial class MainWindow : Window
{
    private MainViewModel ViewModel { get; }

    // FENÊTRE PLAYLIST
    // Null quand elle est fermée. On mémorise ses dernières
    // dimensions/position connues (fenêtre ouverte ou non) pour
    // les proposer à la réouverture et les sauvegarder à la
    // fermeture de l'application.
    private PlaylistWindow? _playlistWindow;
    private double _playlistWidth;
    private double _playlistHeight;
    private double _playlistLeft;
    private double _playlistTop;
    private bool _playlistWasOpen;

    public MainWindow()
    {
        InitializeComponent();
        ViewModel = new MainViewModel();
        DataContext = ViewModel;
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    // CHARGEMENT
    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Charger playlist + shuffle + fenêtre
        // (chargement asynchrone : la fenêtre s'affiche immédiatement,
        // la playlist se remplit sans geler l'UI)
        var settings = await ViewModel.LoadSettingsAsync();
        Width = settings.WindowWidth;
        Height = settings.WindowHeight;
        Left = settings.WindowLeft;
        Top = settings.WindowTop;

        // POSITION (le lecteur se dimensionne lui-même à son contenu)
        if (!double.IsNaN(settings.WindowLeft))
            Left = settings.WindowLeft;

        if (!double.IsNaN(settings.WindowTop))
            Top = settings.WindowTop;

        EnsureWindowIsVisible();

        // FENÊTRE PLAYLIST
        _playlistWidth = settings.PlaylistWindowWidth;
        _playlistHeight = settings.PlaylistWindowHeight;
        _playlistLeft = settings.PlaylistWindowLeft;
        _playlistTop = settings.PlaylistWindowTop;

        if (settings.PlaylistWindowOpen)
            OpenPlaylistWindow();
    }

    // BASCULE PLAYLIST
    private void PlaylistToggle_Click(object sender, RoutedEventArgs e)
    {
        if (_playlistWindow is null)
            OpenPlaylistWindow();
        else
            _playlistWindow.Close();
    }

    private void OpenPlaylistWindow()
    {
        if (_playlistWindow is not null)
        {
            _playlistWindow.Activate();
            return;
        }

        var window = new PlaylistWindow(ViewModel.Playlist)
        {
            Owner = this,
            Width = _playlistWidth,
            Height = _playlistHeight
        };

        if (!double.IsNaN(_playlistLeft))
            window.Left = _playlistLeft;

        if (!double.IsNaN(_playlistTop))
            window.Top = _playlistTop;

        window.EnsureWindowIsVisible();

        window.Closing += PlaylistWindow_Closing;
        window.Closed += PlaylistWindow_Closed;

        _playlistWindow = window;
        _playlistWindow.Show();
    }

    // On mémorise la taille/position juste avant la fermeture,
    // pendant que la fenêtre existe encore.
    private void PlaylistWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_playlistWindow is null)
            return;

        _playlistWidth = _playlistWindow.Width;
        _playlistHeight = _playlistWindow.Height;
        _playlistLeft = _playlistWindow.Left;
        _playlistTop = _playlistWindow.Top;
    }

    private void PlaylistWindow_Closed(object? sender, EventArgs e)
    {
        if (_playlistWindow is not null)
        {
            _playlistWindow.Closing -= PlaylistWindow_Closing;
            _playlistWindow.Closed -= PlaylistWindow_Closed;
        }

        _playlistWindow = null;
    }

    // VÉRIFICATION POSITION
    private void EnsureWindowIsVisible()
    {
        var virtualLeft = SystemParameters.VirtualScreenLeft;
        var virtualTop = SystemParameters.VirtualScreenTop;
        var virtualWidth = SystemParameters.VirtualScreenWidth;
        var virtualHeight = SystemParameters.VirtualScreenHeight;

        var width = ActualWidth > 0 ? ActualWidth : Width;
        var height = ActualHeight > 0 ? ActualHeight : Height;

        var visible = Left < virtualLeft + virtualWidth && Left + width > virtualLeft
            && Top < virtualTop + virtualHeight && Top + height > virtualTop;

        if (visible)
            return;

        // Si la position sauvegardée est invalide,
        // on centre sur l'écran principal.
        Left = SystemParameters.WorkArea.Left + (SystemParameters.WorkArea.Width - width) / 2;
        Top = SystemParameters.WorkArea.Top + (SystemParameters.WorkArea.Height - height) / 2;
    }

    //MOVE WINDOW

    // FERMETURE
    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Si la playlist est encore ouverte, on capture sa position
        // et sa taille actuelles avant de sauvegarder (elle sera
        // fermée automatiquement juste après, étant "possédée" par
        // cette fenêtre, mais son évènement Closing à elle ne
        // déclenche pas forcément avant la sauvegarde ci-dessous).
        _playlistWasOpen = _playlistWindow is not null;

        if (_playlistWindow is not null)
        {
            _playlistWidth = _playlistWindow.Width;
            _playlistHeight = _playlistWindow.Height;
            _playlistLeft = _playlistWindow.Left;
            _playlistTop = _playlistWindow.Top;
        }

        ViewModel.SaveSettings(
            ActualWidth, ActualHeight, Left, Top,
            _playlistWasOpen, _playlistWidth, _playlistHeight, _playlistLeft, _playlistTop);

        ViewModel.Dispose();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)        
            this.DragMove();        
    }
}

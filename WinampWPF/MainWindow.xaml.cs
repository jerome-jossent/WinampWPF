using System.Windows;
using WinampWPF.ViewModels;

namespace WinampWPF;

public partial class MainWindow : Window
{
    private MainViewModel ViewModel { get; }

    public MainWindow()
    {
        InitializeComponent();
        ViewModel = new MainViewModel();
        DataContext = ViewModel;
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    // CHARGEMENT
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Charger playlist + shuffle + fenêtre
        var settings = ViewModel.LoadSettings();

        // TAILLE
        if (settings.WindowWidth > 0)
            Width = settings.WindowWidth;

        if (settings.WindowHeight > 0)
            Height = settings.WindowHeight;

        // POSITION
        if (!double.IsNaN(settings.WindowLeft))
            Left = settings.WindowLeft;

        if (!double.IsNaN(settings.WindowTop))
            Top = settings.WindowTop;

        // VÉRIFICATION ÉCRAN
        EnsureWindowIsVisible();
    }

    // VÉRIFICATION POSITION
    private void EnsureWindowIsVisible()
    {
        var virtualLeft = SystemParameters.VirtualScreenLeft;
        var virtualTop = SystemParameters.VirtualScreenTop;
        var virtualWidth = SystemParameters.VirtualScreenWidth;
        var virtualHeight = SystemParameters.VirtualScreenHeight;

        var visible = Left < virtualLeft + virtualWidth && Left + Width > virtualLeft && Top < virtualTop + virtualHeight && Top + Height > virtualTop;

        if (visible)
            return;

        // Si la position sauvegardée est invalide,
        // on centre sur l'écran principal.
        Left = SystemParameters.WorkArea.Left + (SystemParameters.WorkArea.Width - Width) / 2;
        Top = SystemParameters.WorkArea.Top + (SystemParameters.WorkArea.Height - Height) / 2;
    }

    // FERMETURE
    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        ViewModel.SaveSettings(Width, Height, Left, Top);
        ViewModel.Dispose();
    }
}
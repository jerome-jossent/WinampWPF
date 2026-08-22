using System.Windows;
using WinampWPF.ViewModels;

namespace WinampWPF.Views;

// Fenêtre autonome pour la playlist : décrochée du lecteur, elle
// peut être ouverte/fermée depuis celui-ci, déplacée et
// redimensionnée indépendamment. Sa taille, sa position et son
// état ouvert/fermé sont mémorisés par la fenêtre principale.
public partial class PlaylistWindow : Window
{
    public PlaylistWindow(PlaylistViewModel playlist)
    {
        InitializeComponent();
        DataContext = playlist;
    }

    // VÉRIFICATION POSITION
    // Recentre la fenêtre si la position mémorisée n'est plus
    // visible sur aucun écran (résolution/configuration changée).
    public void EnsureWindowIsVisible()
    {
        var virtualLeft = SystemParameters.VirtualScreenLeft;
        var virtualTop = SystemParameters.VirtualScreenTop;
        var virtualWidth = SystemParameters.VirtualScreenWidth;
        var virtualHeight = SystemParameters.VirtualScreenHeight;

        var visible = Left < virtualLeft + virtualWidth && Left + Width > virtualLeft
            && Top < virtualTop + virtualHeight && Top + Height > virtualTop;

        if (visible)
            return;

        Left = SystemParameters.WorkArea.Left + (SystemParameters.WorkArea.Width - Width) / 2;
        Top = SystemParameters.WorkArea.Top + (SystemParameters.WorkArea.Height - Height) / 2;
    }
}

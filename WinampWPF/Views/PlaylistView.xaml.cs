using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WinampWPF.Models;
using WinampWPF.ViewModels;

namespace WinampWPF.Views;

public partial class PlaylistView : UserControl
{
    private PlaylistViewModel? ViewModel => DataContext as PlaylistViewModel;

    public PlaylistView()
    {
        InitializeComponent();

        DataContextChanged += PlaylistView_DataContextChanged;
        Unloaded += PlaylistView_Unloaded;
    }

    // DATACONTEXT
    private void PlaylistView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is PlaylistViewModel oldViewModel)
            oldViewModel.CurrentTrackChanged -= ViewModel_CurrentTrackChanged;

        if (e.NewValue is PlaylistViewModel newViewModel)
            newViewModel.CurrentTrackChanged += ViewModel_CurrentTrackChanged;
    }

    private void PlaylistView_Unloaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
            ViewModel.CurrentTrackChanged -= ViewModel_CurrentTrackChanged;
    }

    // MORCEAU COURANT
    private void ViewModel_CurrentTrackChanged(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() => { FocusCurrentTrack(); });
    }

    private void FocusCurrentTrack()
    {
        if (ViewModel?.CurrentTrack is not Track currentTrack)
            return;

        TrackList.SelectedItem = currentTrack;
        TrackList.ScrollIntoView(currentTrack);
        TrackList.UpdateLayout();
        var item = TrackList.ItemContainerGenerator.ContainerFromItem(currentTrack) as ListViewItem;
        item?.Focus();
    }

    // DOUBLE CLIC
    private void TrackList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (TrackList.SelectedItem is not Track track)
            return;
        ViewModel?.PlaySelectedTrack(track);
    }

    // SELECTION
    private void TrackList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel is null)
            return;

        ViewModel.SelectedTracks.Clear();

        foreach (var item in TrackList.SelectedItems)
            if (item is Track track)
                ViewModel.SelectedTracks.Add(track);
    }

    // TRI SECONDAIRE (CLIC DROIT SUR L'EN-TETE)
    private void ColumnHeader_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not GridViewColumnHeader header)
            return;

        if (header.CommandParameter is not string column)
            return;

        ViewModel?.SecondarySortCommand.Execute(column);

        // Empêche le menu contextuel par défaut et la
        // remontée de l'évènement au ListView.
        e.Handled = true;
    }

    // DRAG & DROP
    private void PlaylistView_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
            e.Effects = DragDropEffects.Copy;
        else
            e.Effects = DragDropEffects.None;

        e.Handled = true;
    }

    private void PlaylistView_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        if (e.Data.GetData(DataFormats.FileDrop) is not string[] paths)
            return;

        ViewModel?.AddDroppedFiles(paths);
        e.Handled = true;
    }
}
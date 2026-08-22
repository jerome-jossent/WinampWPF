using System.IO;
using WinampWPF.Models;

namespace WinampWPF.Services.Audio;

public sealed class PlaybackController : IDisposable
{
    private readonly IAudioPlayer _audioPlayer;
    // PLAYLIST
    private readonly List<Track> _tracks = [];

    // QUEUE
    private readonly Queue<Track> _queue = [];

    // SHUFFLE
    /*
     * Représente l'ordre aléatoire du cycle actuel.
     *
     * Exemple :
     *
     * Playlist :
     * A B C D E
     *
     * Shuffle :
     * C A E B D
     *
     * _shuffleOrder = [C, A, E, B, D]
     */
    private readonly List<Track> _shuffleOrder = [];

    private int _shuffleIndex = -1;
    private readonly Random _random = new();

    // ETAT
    private bool _disposed;

    public IReadOnlyList<Track> Tracks => _tracks;

    public IReadOnlyCollection<Track> Queue => _queue;

    public IReadOnlyList<Track> ShuffleOrder => _shuffleOrder;

    public Track? CurrentTrack { get; private set; }
    public bool ShuffleEnabled { get; private set; }

    public void SetShuffleEnabled(bool enabled)
    {
        if (_disposed)
            return;

        if (ShuffleEnabled == enabled)
            return;

        ToggleShuffle();
    }


    // EVENEMENTS
    public event EventHandler? CurrentTrackChanged;
    public event EventHandler? ShuffleChanged;

    // CONSTRUCTEUR
    public PlaybackController(IAudioPlayer audioPlayer)
    {
        _audioPlayer = audioPlayer;
        _audioPlayer.PlaybackEnded += AudioPlayer_PlaybackEnded;
    }

    // PLAYLIST
    public void SetTracks(IEnumerable<Track> tracks)
    {
        if (_disposed)
            return;

        _tracks.Clear();
        _tracks.AddRange(tracks);

        // Vérifie le morceau courant
        if (CurrentTrack is not null && !_tracks.Contains(CurrentTrack))
        {
            CurrentTrack.IsPlaying = false;
            CurrentTrack = null;
        }

        // Le changement de playlist invalide le cycle shuffle.
        if (ShuffleEnabled)
        {
            RebuildShuffleOrder();
            if (CurrentTrack is not null)
                EnsureTrackInShuffleOrder(CurrentTrack);
        }
        RaiseCurrentTrackChanged();
    }

    // PLAY
    public void Play(Track track)
    {
        if (_disposed)
            return;

        if (!_tracks.Contains(track))
            return;

        if (!IsPlayable(track))
        {
            track.IsMissing = true;
            return;
        }

        SetCurrentTrack(track);

        // Si Shuffle est actif, le morceau sélectionné
        // devient le point courant de la séquence shuffle.
        if (ShuffleEnabled)
            EnsureTrackInShuffleOrder(track);
    }


    // PLAY CURRENT
    public void PlayCurrent()
    {
        if (_disposed)
            return;

        // Aucun morceau sélectionné :
        // on démarre le premier morceau de la playlist.
        if (CurrentTrack is null)
        {
            var first = GetFirstPlayableTrack();

            if (first is null)
                return;

            Play(first);
            return;
        }

        // Si on était arrivé à la fin du morceau,
        // on recommence depuis le début.
        if (_audioPlayer.Duration > TimeSpan.Zero && _audioPlayer.Position >= _audioPlayer.Duration)
            _audioPlayer.Seek(TimeSpan.Zero);

        _audioPlayer.Play();
    }

    // PAUSE
    public void Pause()
    {
        if (_disposed)
            return;

        _audioPlayer.Pause();
    }

    // STOP
    public void Stop()
    {
        if (_disposed)
            return;

        _audioPlayer.Stop();
    }

    // NEXT
    public void Next()
    {
        if (_disposed)
            return;

        var next = GetNextTrack();

        if (next is null)
        {
            Stop();
            return;
        }

        SetCurrentTrack(next);

        if (ShuffleEnabled)
            EnsureTrackInShuffleOrder(next);
    }


    // PREVIOUS
    public void Previous()
    {
        if (_disposed)
            return;

        if (CurrentTrack is null)
            return;

        /*
         * IMPORTANT :
         *
         * Pour Previous, on ne tente PAS de reconstruire
         * le chemin à partir de la playlist.
         *
         * Le Previous doit correspondre à ce qui a réellement
         * été joué.
         *
         * Exemple :
         *
         * Shuffle :
         *
         * A → D → B → X → C
         *
         * Previous depuis C :
         *
         * X
         *
         * Previous depuis X :
         *
         * B
         *
         * C'est donc l'historique de lecture qui devra gérer
         * cette partie.
         *
         * Pour l'instant, le comportement minimal est :
         * si on appuie sur Previous au milieu d'une piste,
         * on recommence la piste.
         *
         * La navigation historique complète pourra être ajoutée
         * sans modifier la logique Queue/Shuffle.
         */

        if (_audioPlayer.Position > TimeSpan.FromSeconds(3))
        {
            _audioPlayer.Seek(TimeSpan.Zero);
            _audioPlayer.Play();
            return;
        }

        PreviousTrack();
    }

    // PREVIOUS TRACK
    private void PreviousTrack()
    {
        // SHUFFLE
        if (ShuffleEnabled)
        {
            if (_shuffleOrder.Count == 0)
                return;

            if (_shuffleIndex <= 0)
            {
                _audioPlayer.Seek(TimeSpan.Zero);
                _audioPlayer.Play();
                return;
            }

            var previousIndex = _shuffleIndex - 1;
            var previous = _shuffleOrder[previousIndex];

            if (!IsPlayable(previous))
            {
                _shuffleIndex = previousIndex;
                PreviousTrack();
                return;
            }

            _shuffleIndex = previousIndex;
            SetCurrentTrack(previous, updateShuffleIndex: false);
            return;
        }

        // LECTURE NORMALE
        if (CurrentTrack is null)
            return;

        var currentIndex = _tracks.IndexOf(CurrentTrack);

        if (currentIndex <= 0)
        {
            _audioPlayer.Seek(TimeSpan.Zero);
            _audioPlayer.Play();
            return;
        }

        for (var i = currentIndex - 1; i >= 0; i--)
        {
            if (IsPlayable(_tracks[i]))
            {
                SetCurrentTrack(_tracks[i], updateShuffleIndex: false);
                return;
            }
        }
        _audioPlayer.Seek(TimeSpan.Zero);
        _audioPlayer.Play();
    }

    // SHUFFLE
    public void ToggleShuffle()
    {
        if (_disposed)
            return;

        ShuffleEnabled = !ShuffleEnabled;

        // ACTIVATION
        if (ShuffleEnabled)
        {
            /*
             * On crée un nouvel ordre aléatoire.
             *
             * Le morceau actuellement joué est conservé
             * comme point de départ.
             */

            RebuildShuffleOrder();
            if (CurrentTrack is not null)
                EnsureTrackInShuffleOrder(CurrentTrack);
        }
        // DESACTIVATION
        else
        {
            /*
             * IMPORTANT :
             *
             * On détruit complètement le futur shuffle.
             *
             * Le prochain morceau sera calculé à partir de
             * CurrentTrack dans la playlist NORMALE.
             *
             * Exemple :
             *
             * Playlist :
             * A B C X Y Z
             *
             * Shuffle :
             * A X C B Y Z
             *
             * On est sur X.
             *
             * Désactivation Shuffle.
             *
             * Next => Y
             *
             * car X est recherché dans _tracks.
             */

            _shuffleOrder.Clear();
            _shuffleIndex = -1;
        }

        ShuffleChanged?.Invoke(this, EventArgs.Empty);
        RaiseCurrentTrackChanged();
    }


    // QUEUE
    public void Enqueue(Track track)
    {
        if (_disposed)
            return;

        if (!_tracks.Contains(track))
            return;

        if (_queue.Contains(track))
            return;

        _queue.Enqueue(track);
    }

    public void EnqueueNext(Track track)
    {
        if (_disposed)
            return;

        if (!_tracks.Contains(track))
            return;

        /*
         * La queue est une insertion.
         *
         * Elle ne retire JAMAIS le morceau de sa position
         * naturelle dans la playlist.
         */

        if (_queue.Contains(track))
            return;

        var items = _queue.ToList();

        _queue.Clear();

        _queue.Enqueue(track);

        foreach (var item in items)
            _queue.Enqueue(item);
    }

    public void RemoveFromQueue(Track track)
    {
        var items = _queue.Where(t => !ReferenceEquals(t, track)).ToList();

        _queue.Clear();

        foreach (var item in items)
            _queue.Enqueue(item);
    }

    public void ClearQueue()
    {
        _queue.Clear();
    }

    // CALCUL NEXT
    private Track? GetNextTrack()
    {
        // 1. QUEUE
        /*
         * La queue est TOUJOURS prioritaire.
         *
         * Exemple normal :
         *
         * A → B → C → D
         *
         * sur B :
         *
         * queue = D
         *
         * devient :
         *
         * A → B → D → C → D
         */

        if (_queue.Count > 0)
        {
            var queued = _queue.Dequeue();
            if (IsPlayable(queued))
                return queued;

            // Si le fichier est manquant,
            // on passe au suivant.
            return GetNextTrack();
        }

        // 2. SHUFFLE
        if (ShuffleEnabled)
            return GetNextShuffleTrack();

        // 3. LECTURE NORMALE
        return GetNextNormalTrack();
    }


    // NEXT NORMAL
    private Track? GetNextNormalTrack()
    {
        if (_tracks.Count == 0)
            return null;

        // Aucun morceau courant :
        // premier morceau.
        if (CurrentTrack is null)
            return GetFirstPlayableTrack();

        /*
         * C'est ici que se trouve la règle importante :
         *
         * on cherche TOUJOURS CurrentTrack dans la playlist
         * normale.
         *
         * On ne se soucie absolument pas de la séquence shuffle
         * précédente.
         */

        var currentIndex = _tracks.IndexOf(CurrentTrack);

        if (currentIndex < 0)
            return GetFirstPlayableTrack();

        for (var i = currentIndex + 1; i < _tracks.Count; i++)
            if (IsPlayable(_tracks[i]))
                return _tracks[i];


        // FIN DE PLAYLIST

        /*
         * Pas de Repeat :
         *
         * on s'arrête.
         */

        return null;
    }


    // NEXT SHUFFLE
    private Track? GetNextShuffleTrack()
    {
        if (_tracks.Count == 0)
            return null;

        // Aucun ordre shuffle
        if (_shuffleOrder.Count == 0)
        {
            RebuildShuffleOrder();
            if (_shuffleOrder.Count == 0)
                return null;
        }

        // Morceau suivant dans le cycle actuel
        var nextIndex = _shuffleIndex + 1;

        while (nextIndex < _shuffleOrder.Count)
        {
            var candidate = _shuffleOrder[nextIndex];
            _shuffleIndex = nextIndex;
            if (IsPlayable(candidate))
                return candidate;

            nextIndex++;
        }

        // FIN DU CYCLE SHUFFLE

        /*
         * Exemple :
         *
         * Cycle 1 :
         *
         * A → D → B → C → E
         *
         * E terminé.
         *
         * On construit :
         *
         * Cycle 2 :
         *
         * C → A → E → B → D
         */

        var previousTrack = CurrentTrack;

        RebuildShuffleOrder();

        if (_shuffleOrder.Count == 0)
            return null;

        // -----------------------------------------------------
        // Évite autant que possible :
        //
        // E → E
        // -----------------------------------------------------
        if (_shuffleOrder.Count > 1 && previousTrack is not null && ReferenceEquals(_shuffleOrder[0], previousTrack))
        {
            var swapIndex = _random.Next(1, _shuffleOrder.Count);
            (_shuffleOrder[0], _shuffleOrder[swapIndex]) = (_shuffleOrder[swapIndex], _shuffleOrder[0]);
        }

        _shuffleIndex = 0;
        return _shuffleOrder[0];
    }


    // CREATION D'UN NOUVEAU SHUFFLE
    private void RebuildShuffleOrder()
    {
        _shuffleOrder.Clear();

        foreach (var track in _tracks)
            if (IsPlayable(track))
                _shuffleOrder.Add(track);

        // Fisher-Yates
        for (var i = _shuffleOrder.Count - 1; i > 0; i--)
        {
            var j = _random.Next(i + 1);
            (_shuffleOrder[i], _shuffleOrder[j]) = (_shuffleOrder[j], _shuffleOrder[i]);
        }

        _shuffleIndex = -1;
    }


    // POSITIONNEMENT D'UN MORCEAU DANS LE SHUFFLE
    private void EnsureTrackInShuffleOrder(Track track)
    {
        var index = _shuffleOrder.IndexOf(track);

        if (index < 0)
        {
            _shuffleOrder.Add(track);
            index = _shuffleOrder.Count - 1;
        }

        _shuffleIndex = index;
    }

    // MORCEAU COURANT
    private void SetCurrentTrack(Track track, bool updateShuffleIndex = true)
    {
        if (_disposed)
            return;

        if (!IsPlayable(track))
        {
            track.IsMissing = true;
            return;
        }

        if (CurrentTrack is not null && !ReferenceEquals(CurrentTrack, track))
            CurrentTrack.IsPlaying = false;

        CurrentTrack = track;
        CurrentTrack.IsMissing = false;
        CurrentTrack.IsPlaying = true;

        // Position shuffle
        if (ShuffleEnabled && updateShuffleIndex)
            EnsureTrackInShuffleOrder(track);

        // AUDIO
        _audioPlayer.Load(track.FilePath);
        _audioPlayer.Play();
        RaiseCurrentTrackChanged();
    }


    // FIN DE MORCEAU
    private void AudioPlayer_PlaybackEnded(object? sender, EventArgs e)
    {
        Next();
    }

    // UTILITAIRES
    private static bool IsPlayable(Track track)
    {
        return !track.IsMissing && File.Exists(track.FilePath);
    }

    private Track? GetFirstPlayableTrack()
    {
        return _tracks.FirstOrDefault(IsPlayable);
    }

    // NOTIFICATION
    private void RaiseCurrentTrackChanged()
    {
        CurrentTrackChanged?.Invoke(this, EventArgs.Empty);
    }

    // DISPOSE
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _audioPlayer.PlaybackEnded -= AudioPlayer_PlaybackEnded;
        _audioPlayer.Dispose();
    }
}
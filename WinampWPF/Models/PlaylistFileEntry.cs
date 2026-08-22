namespace WinampWPF.Models;

// Représente un fichier à ajouter à la playlist, avec le dossier
// racine depuis lequel il a été chargé (dossier choisi via
// "+ Dossier" ou glissé-déposé). RootFolder est null quand le
// fichier a été ajouté individuellement.
// Utilisé à la fois pour le pipeline d'ajout (ViewModels) et pour
// la persistance de la playlist (Services.Persistence).
public sealed record PlaylistFileEntry(string FilePath, string? RootFolder = null);

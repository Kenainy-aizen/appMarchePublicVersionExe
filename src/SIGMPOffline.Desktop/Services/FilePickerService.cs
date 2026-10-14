using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace SIGMPOffline.Desktop.Services;

/// <inheritdoc />
public sealed class FilePickerService : IFilePickerService
{
    /// <inheritdoc />
    public async Task<string?> ChoisirDossierAsync(string titre)
    {
        var storage = FenetrePrincipale()?.StorageProvider;
        if (storage is null)
        {
            return null;
        }

        var dossiers = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = titre,
            AllowMultiple = false
        });

        return dossiers.Count > 0 ? dossiers[0].TryGetLocalPath() : null;
    }

    /// <inheritdoc />
    public async Task<string?> ChoisirFichierAsync(string titre, string? extension = null)
    {
        var storage = FenetrePrincipale()?.StorageProvider;
        if (storage is null)
        {
            return null;
        }

        var options = new FilePickerOpenOptions
        {
            Title = titre,
            AllowMultiple = false,
            FileTypeFilter = extension is null
                ? null
                : [new FilePickerFileType($"Fichier .{extension}") { Patterns = [$"*.{extension}"] }]
        };

        var fichiers = await storage.OpenFilePickerAsync(options);
        return fichiers.Count > 0 ? fichiers[0].TryGetLocalPath() : null;
    }

    private static Window? FenetrePrincipale()
        => (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
}

namespace SIGMPOffline.Desktop.Services;

/// <summary>Sélection de fichiers et de dossiers via les boîtes de dialogue natives.</summary>
public interface IFilePickerService
{
    /// <summary>Ouvre un sélecteur de dossier et retourne le chemin choisi (ou <c>null</c>).</summary>
    Task<string?> ChoisirDossierAsync(string titre);

    /// <summary>Ouvre un sélecteur de fichier et retourne le chemin choisi (ou <c>null</c>).</summary>
    /// <param name="titre">Titre de la boîte de dialogue.</param>
    /// <param name="extension">Extension filtrée sans le point (ex. <c>db</c>), facultative.</param>
    Task<string?> ChoisirFichierAsync(string titre, string? extension = null);
}

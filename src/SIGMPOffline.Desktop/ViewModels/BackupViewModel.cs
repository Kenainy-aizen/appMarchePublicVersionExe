using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIGMPOffline.Desktop.Services;
using SIGMPOffline.Services.Abstractions;

namespace SIGMPOffline.Desktop.ViewModels;

/// <summary>Module Utilitaire : sauvegarde / restauration du fichier SQLite.</summary>
public partial class BackupViewModel : ViewModelBase, IPageViewModel
{
    private readonly IBackupService _backup;
    private readonly IDialogService _dialogues;
    private readonly IFilePickerService _fichiers;

    public BackupViewModel(IBackupService backup, IDialogService dialogues, IFilePickerService fichiers)
    {
        _backup = backup ?? throw new ArgumentNullException(nameof(backup));
        _dialogues = dialogues ?? throw new ArgumentNullException(nameof(dialogues));
        _fichiers = fichiers ?? throw new ArgumentNullException(nameof(fichiers));
    }

    /// <inheritdoc />
    public string Titre => "Sauvegarde & Restauration";

    /// <summary>Chemin du fichier de base de données actif.</summary>
    public string BaseDeDonneesPath => _backup.BaseDeDonneesPath;

    /// <summary>Dossier de destination de la sauvegarde (clé USB). Vide = dossier par défaut.</summary>
    [ObservableProperty] private string? _dossierDestination;

    /// <summary>Chemin du fichier de sauvegarde à restaurer.</summary>
    [ObservableProperty] private string? _fichierRestauration;

    [ObservableProperty] private string? _message;
    [ObservableProperty] private string? _erreur;
    [ObservableProperty] private bool _estOccupe;

    /// <inheritdoc />
    [RelayCommand]
    public Task ChargerAsync()
    {
        Message = null;
        Erreur = null;
        OnPropertyChanged(nameof(BaseDeDonneesPath));
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ParcourirDossier()
    {
        var dossier = await _fichiers.ChoisirDossierAsync("Choisir le dossier de sauvegarde (clé USB)");
        if (!string.IsNullOrWhiteSpace(dossier))
        {
            DossierDestination = dossier;
        }
    }

    [RelayCommand]
    private async Task ParcourirFichierRestauration()
    {
        var fichier = await _fichiers.ChoisirFichierAsync("Choisir un fichier de sauvegarde à restaurer", "db");
        if (!string.IsNullOrWhiteSpace(fichier))
        {
            FichierRestauration = fichier;
        }
    }

    [RelayCommand]
    private async Task Sauvegarder()
    {
        try
        {
            EstOccupe = true;
            Erreur = null;
            Message = null;

            var chemin = await _backup.SauvegarderAsync(
                string.IsNullOrWhiteSpace(DossierDestination) ? null : DossierDestination);

            Message = $"Sauvegarde créée : {chemin}";
            await _dialogues.InformerAsync("Sauvegarde réussie", $"La sauvegarde a été créée :\n{chemin}");
        }
        catch (Exception ex)
        {
            Erreur = ex.Message;
            await _dialogues.InformerAsync("Erreur", ex.Message);
        }
        finally
        {
            EstOccupe = false;
        }
    }

    [RelayCommand]
    private async Task Restaurer()
    {
        if (string.IsNullOrWhiteSpace(FichierRestauration))
        {
            Erreur = "Indiquez le chemin du fichier de sauvegarde à restaurer.";
            return;
        }

        var confirme = await _dialogues.ConfirmerAsync(
            "Restaurer la base",
            "La restauration REMPLACE toutes les données actuelles par celles du fichier "
            + "de sauvegarde. Cette opération est irréversible.\n\n"
            + $"Fichier : {FichierRestauration}\n\nConfirmer la restauration ?");
        if (!confirme)
        {
            return;
        }

        try
        {
            EstOccupe = true;
            Erreur = null;
            Message = null;

            await _backup.RestaurerAsync(FichierRestauration);

            Message = "Restauration effectuée.";
            await _dialogues.InformerAsync(
                "Restauration réussie",
                "La restauration a été effectuée. Redémarrez l'application pour recharger les données.");
        }
        catch (Exception ex)
        {
            Erreur = ex.Message;
            await _dialogues.InformerAsync("Erreur", ex.Message);
        }
        finally
        {
            EstOccupe = false;
        }
    }
}

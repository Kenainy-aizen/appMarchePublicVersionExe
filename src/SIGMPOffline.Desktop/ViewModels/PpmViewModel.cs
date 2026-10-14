using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIGMPOffline.Desktop.Services;
using SIGMPOffline.Desktop.ViewModels.Dialogs;
using SIGMPOffline.Domain.Entities;
using SIGMPOffline.Services.Abstractions;

namespace SIGMPOffline.Desktop.ViewModels;

/// <summary>Module PPM : gestion (CRUD) des plans de passation et de leurs lignes.</summary>
public partial class PpmViewModel : ViewModelBase, IPageViewModel
{
    private readonly IPpmService _ppm;
    private readonly IDialogService _dialogues;

    public PpmViewModel(IPpmService ppm, IDialogService dialogues)
    {
        _ppm = ppm ?? throw new ArgumentNullException(nameof(ppm));
        _dialogues = dialogues ?? throw new ArgumentNullException(nameof(dialogues));
    }

    /// <inheritdoc />
    public string Titre => "Plan de Passation des Marchés";

    /// <summary>En-têtes de PPM.</summary>
    public ObservableCollection<PpmEntete> Entetes { get; } = new();

    /// <summary>Lignes du PPM sélectionné.</summary>
    public ObservableCollection<PpmDetail> Details { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AUnEntete))]
    private PpmEntete? _enteteSelectionne;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AUnDetail))]
    private PpmDetail? _detailSelectionne;

    [ObservableProperty] private bool _estOccupe;
    [ObservableProperty] private string? _erreur;
    [ObservableProperty] private string? _message;

    /// <summary>Vrai si un en-tête est sélectionné.</summary>
    public bool AUnEntete => EnteteSelectionne is not null;

    /// <summary>Vrai si une ligne de détail est sélectionnée.</summary>
    public bool AUnDetail => DetailSelectionne is not null;

    /// <inheritdoc />
    [RelayCommand]
    public async Task ChargerAsync()
    {
        try
        {
            EstOccupe = true;
            Erreur = null;

            var entetes = await _ppm.ListerAsync();            Entetes.Clear();
            foreach (var entete in entetes)
            {
                Entetes.Add(entete);
            }

            EnteteSelectionne = Entetes.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Erreur = ex.Message;
        }
        finally
        {
            EstOccupe = false;
        }
    }

    // ---------- En-tête ----------

    [RelayCommand]
    private async Task NouveauPpm()
    {
        var dialogue = new PpmEnteteDialogViewModel();
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        var entete = dialogue.VersEntite();
        await ExecuterAsync(
            () => _ppm.CreerAsync(entete),
            $"Le PPM « {entete.NumPPM} » a été créé.");

        await ChargerAsync();
        EnteteSelectionne = Entetes.FirstOrDefault(e => e.NumPPM == entete.NumPPM);
    }

    [RelayCommand]
    private async Task ModifierPpm()
    {
        if (EnteteSelectionne is null)
        {
            return;
        }

        var dialogue = new PpmEnteteDialogViewModel(EnteteSelectionne);
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        var entete = dialogue.VersEntite();
        await ExecuterAsync(() => _ppm.ModifierAsync(entete), "Le PPM a été modifié.");

        await ChargerAsync();
        EnteteSelectionne = Entetes.FirstOrDefault(e => e.NumPPM == entete.NumPPM);
    }

    [RelayCommand]
    private async Task SupprimerPpm()
    {
        if (EnteteSelectionne is null)
        {
            return;
        }

        var numPpm = EnteteSelectionne.NumPPM;
        var confirme = await _dialogues.ConfirmerAsync(
            "Supprimer le PPM",
            $"Confirmer la suppression du PPM « {numPpm} » et de toutes ses lignes ?");

        if (!confirme)
        {
            return;
        }

        await ExecuterAsync(() => _ppm.SupprimerAsync(numPpm), "Le PPM a été supprimé.");
        await ChargerAsync();
    }

    // ---------- Lignes de détail ----------

    [RelayCommand]
    private async Task AjouterDetail()
    {
        if (EnteteSelectionne is null)
        {
            return;
        }

        var dialogue = new PpmDetailDialogViewModel();
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        var detail = dialogue.VersEntite();
        await ExecuterAsync(
            () => _ppm.AjouterDetailAsync(EnteteSelectionne.NumPPM, detail),
            "La ligne a été ajoutée.");

        await ChargerDetailsAsync(EnteteSelectionne.NumPPM);
    }

    [RelayCommand]
    private async Task ModifierDetail()
    {
        if (EnteteSelectionne is null || DetailSelectionne is null)
        {
            return;
        }

        var dialogue = new PpmDetailDialogViewModel(DetailSelectionne);
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        var detail = dialogue.VersEntite();
        await ExecuterAsync(
            () => _ppm.ModifierDetailAsync(EnteteSelectionne.NumPPM, detail),
            "La ligne a été modifiée.");

        await ChargerDetailsAsync(EnteteSelectionne.NumPPM);
    }

    [RelayCommand]
    private async Task SupprimerDetail()
    {
        if (EnteteSelectionne is null || DetailSelectionne is null)
        {
            return;
        }

        var numDetail = DetailSelectionne.NumPPMDetail;
        var confirme = await _dialogues.ConfirmerAsync(
            "Supprimer la ligne",
            $"Confirmer la suppression de la ligne « {numDetail} » ?");

        if (!confirme)
        {
            return;
        }

        await ExecuterAsync(
            () => _ppm.SupprimerDetailAsync(EnteteSelectionne.NumPPM, numDetail),
            "La ligne a été supprimée.");

        await ChargerDetailsAsync(EnteteSelectionne.NumPPM);
    }

    // ---------- Interne ----------

    partial void OnEnteteSelectionneChanged(PpmEntete? value)
    {
        Details.Clear();
        DetailSelectionne = null;

        if (value is not null)
        {
            _ = ChargerDetailsAsync(value.NumPPM);
        }
    }

    private async Task ChargerDetailsAsync(string numPpm)
    {
        try
        {
            var entete = await _ppm.ObtenirAsync(numPpm);
            Details.Clear();
            foreach (var detail in entete.Details)
            {
                Details.Add(detail);
            }

            DetailSelectionne = Details.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Erreur = ex.Message;
        }
    }

    private async Task ExecuterAsync(Func<Task> action, string messageSucces)
    {
        try
        {
            EstOccupe = true;
            Erreur = null;
            Message = null;

            await action();
            Message = messageSucces;
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

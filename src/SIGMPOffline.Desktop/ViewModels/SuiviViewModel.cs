using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIGMPOffline.Desktop.Services;
using SIGMPOffline.Desktop.ViewModels.Dialogs;
using SIGMPOffline.Domain.Entities;
using SIGMPOffline.Services.Abstractions;
using SIGMPOffline.Services.Models;

namespace SIGMPOffline.Desktop.ViewModels;

/// <summary>
/// Module Suivi d'Exécution (cœur) : TEF, Ordre de Service, avenants, réceptions et clôture.
/// Toutes les saisies passent par des fenêtres modales.
/// </summary>
public partial class SuiviViewModel : ViewModelBase, IPageViewModel
{
    private readonly ISuiviExecutionService _suivi;
    private readonly IMarcheService _marches;
    private readonly IDialogService _dialogues;

    public SuiviViewModel(ISuiviExecutionService suivi, IMarcheService marches, IDialogService dialogues)
    {
        _suivi = suivi ?? throw new ArgumentNullException(nameof(suivi));
        _marches = marches ?? throw new ArgumentNullException(nameof(marches));
        _dialogues = dialogues ?? throw new ArgumentNullException(nameof(dialogues));
    }

    /// <inheritdoc />
    public string Titre => "Suivi d'exécution";

    /// <summary>Marchés disponibles.</summary>
    public ObservableCollection<Marche> Marches { get; } = new();

    /// <summary>Avenants du marché sélectionné.</summary>
    public ObservableCollection<Avenant> Avenants { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AUnMarche))]
    [NotifyPropertyChangedFor(nameof(PeutModifier))]
    private Marche? _marcheSelectionne;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AUnAvenant))]
    private Avenant? _avenantSelectionne;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PeutModifier))]
    private SituationMarche? _situation;

    [ObservableProperty] private bool _estOccupe;
    [ObservableProperty] private string? _message;
    [ObservableProperty] private string? _erreur;

    /// <summary>Vrai si un marché est sélectionné.</summary>
    public bool AUnMarche => MarcheSelectionne is not null;

    /// <summary>Vrai si un avenant est sélectionné.</summary>
    public bool AUnAvenant => AvenantSelectionne is not null;

    /// <summary>Vrai si le marché sélectionné peut encore être modifié (non clôturé).</summary>
    public bool PeutModifier => MarcheSelectionne is not null && Situation is { EstCloture: false };

    /// <inheritdoc />
    [RelayCommand]
    public async Task ChargerAsync()
    {
        try
        {
            EstOccupe = true;
            Erreur = null;

            var marches = await _marches.ListerAsync();
            Marches.Clear();
            foreach (var marche in marches)
            {
                Marches.Add(marche);
            }

            MarcheSelectionne = Marches.FirstOrDefault();
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

    // ---------- Actions d'exécution ----------

    [RelayCommand]
    private async Task EmettreOs()
    {
        if (MarcheSelectionne is null)
        {
            return;
        }

        var dialogue = new DateDialogViewModel(
            "Émettre l'Ordre de Service",
            "Date de notification de l'OS :",
            Situation?.DateOS);
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        await ExecuterAsync(
            () => _suivi.EmettreOrdreServiceAsync(MarcheSelectionne.RefMarche, dialogue.VersDate()),
            "Ordre de service émis.");
    }

    [RelayCommand]
    private async Task EnregistrerTef()
    {
        if (MarcheSelectionne is null)
        {
            return;
        }

        var dialogue = new TefDialogViewModel(Situation?.RefTef);
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        await ExecuterAsync(
            () => _suivi.EnregistrerTefAsync(MarcheSelectionne.RefMarche, dialogue.VersRefTef()),
            "TEF enregistré.");
    }

    [RelayCommand]
    private async Task ReceptionProvisoire()
    {
        await EnregistrerReceptionAsync(
            "Réception provisoire",
            "Date de réception provisoire :",
            definitive: false);
    }

    [RelayCommand]
    private async Task ReceptionDefinitive()
    {
        await EnregistrerReceptionAsync(
            "Réception définitive",
            "Date de réception définitive :",
            definitive: true);
    }

    [RelayCommand]
    private async Task Cloturer()
    {
        if (MarcheSelectionne is null)
        {
            return;
        }

        var dialogue = new DateDialogViewModel(
            "Clôturer le marché",
            "Date de clôture :",
            Situation?.DateLimiteExecution);
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        var confirme = await _dialogues.ConfirmerAsync(
            "Clôturer le marché",
            "Confirmer la clôture définitive de ce marché ? Cette opération empêche "
            + "toute nouvelle modification d'exécution (OS, avenants, réceptions).");
        if (!confirme)
        {
            return;
        }

        await ExecuterAsync(
            () => _suivi.CloturerAsync(MarcheSelectionne.RefMarche, dialogue.VersDate()),
            "Marché clôturé.");
    }

    // ---------- Avenants ----------

    [RelayCommand]
    private async Task AjouterAvenant()
    {
        if (MarcheSelectionne is null)
        {
            return;
        }

        var dialogue = new AvenantDialogViewModel();
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        var avenant = dialogue.VersEntite();
        await ExecuterAsync(
            () => _suivi.AjouterAvenantAsync(MarcheSelectionne.RefMarche, avenant),
            "Avenant ajouté.");
    }

    [RelayCommand]
    private async Task ModifierAvenant()
    {
        if (MarcheSelectionne is null || AvenantSelectionne is null)
        {
            return;
        }

        var dialogue = new AvenantDialogViewModel(AvenantSelectionne);
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        var avenant = dialogue.VersEntite();
        await ExecuterAsync(
            () => _suivi.ModifierAvenantAsync(MarcheSelectionne.RefMarche, avenant),
            "Avenant modifié.");
    }

    [RelayCommand]
    private async Task SupprimerAvenant()
    {
        if (MarcheSelectionne is null || AvenantSelectionne is null)
        {
            return;
        }

        var numero = AvenantSelectionne.NumAvenant;
        var id = AvenantSelectionne.IdAvenant;
        var confirme = await _dialogues.ConfirmerAsync(
            "Supprimer l'avenant",
            $"Confirmer la suppression de l'avenant « {numero} » ?");
        if (!confirme)
        {
            return;
        }

        await ExecuterAsync(
            () => _suivi.SupprimerAvenantAsync(MarcheSelectionne.RefMarche, id),
            "Avenant supprimé.");
    }

    // ---------- Interne ----------

    partial void OnMarcheSelectionneChanged(Marche? value)
    {
        Situation = null;
        Avenants.Clear();
        AvenantSelectionne = null;

        if (value is not null)
        {
            _ = ActualiserAsync(value.RefMarche);
        }
    }

    private async Task EnregistrerReceptionAsync(string titre, string libelle, bool definitive)
    {
        if (MarcheSelectionne is null)
        {
            return;
        }

        var initiale = definitive
            ? Situation?.DateLimiteExecution
            : Situation?.DateOS;

        var dialogue = new DateDialogViewModel(titre, libelle, initiale);
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        var date = dialogue.VersDate();
        var refMarche = MarcheSelectionne.RefMarche;

        await ExecuterAsync(
            () => definitive
                ? _suivi.EnregistrerReceptionDefinitiveAsync(refMarche, date)
                : _suivi.EnregistrerReceptionProvisoireAsync(refMarche, date),
            definitive
                ? "Réception définitive enregistrée."
                : "Réception provisoire enregistrée.");
    }

    private async Task ExecuterAsync(Func<Task> action, string messageSucces)
    {
        if (MarcheSelectionne is null)
        {
            return;
        }

        try
        {
            EstOccupe = true;
            Erreur = null;
            Message = null;

            await action();
            await ActualiserAsync(MarcheSelectionne.RefMarche);
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

    private async Task ActualiserAsync(string refMarche)
    {
        try
        {
            Situation = await _suivi.ObtenirSituationAsync(refMarche);

            var marche = await _marches.ObtenirAsync(refMarche);
            Avenants.Clear();
            foreach (var avenant in marche.Avenants.OrderByDescending(a => a.DateAvenant))
            {
                Avenants.Add(avenant);
            }

            AvenantSelectionne = Avenants.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Erreur = ex.Message;
        }
    }
}

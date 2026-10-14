using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIGMPOffline.Desktop.Services;
using SIGMPOffline.Desktop.ViewModels.Dialogs;
using SIGMPOffline.Domain.Entities;
using SIGMPOffline.Services.Abstractions;

namespace SIGMPOffline.Desktop.ViewModels;

/// <summary>Module Marché &amp; Bordereau : CRUD des marchés et de leurs éléments.</summary>
public partial class MarcheViewModel : ViewModelBase, IPageViewModel
{
    private readonly IMarcheService _marches;
    private readonly IDossierDaoService _dao;
    private readonly IDialogService _dialogues;

    public MarcheViewModel(IMarcheService marches, IDossierDaoService dao, IDialogService dialogues)
    {
        _marches = marches ?? throw new ArgumentNullException(nameof(marches));
        _dao = dao ?? throw new ArgumentNullException(nameof(dao));
        _dialogues = dialogues ?? throw new ArgumentNullException(nameof(dialogues));
    }

    /// <inheritdoc />
    public string Titre => "Marchés";

    /// <summary>Marchés formalisés.</summary>
    public ObservableCollection<Marche> Marches { get; } = new();

    /// <summary>Éléments (bordereau) du marché sélectionné.</summary>
    public ObservableCollection<ElementMarche> Elements { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AUnMarche))]
    private Marche? _marcheSelectionne;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AUnElement))]
    private ElementMarche? _elementSelectionne;

    [ObservableProperty] private bool _estOccupe;
    [ObservableProperty] private string? _erreur;
    [ObservableProperty] private string? _message;

    /// <summary>Vrai si un marché est sélectionné.</summary>
    public bool AUnMarche => MarcheSelectionne is not null;

    /// <summary>Vrai si un élément du bordereau est sélectionné.</summary>
    public bool AUnElement => ElementSelectionne is not null;

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

    // ---------- Marchés ----------

    [RelayCommand]
    private async Task NouveauMarche()
    {
        var dossiers = await ChargerDossiersDisponiblesAsync();
        if (dossiers.Count == 0)
        {
            await _dialogues.InformerAsync(
                "Aucun dossier disponible",
                "Tous les dossiers DAO sont déjà associés à un marché. "
                + "Créez d'abord un nouveau dossier dans le module « DAO & Attribution ».");
            return;
        }

        var candidats = await ChargerCandidatsAsync(dossiers);
        var dialogue = new MarcheDialogViewModel(dossiers, candidats);
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        var marche = dialogue.VersEntite();
        await ExecuterAsync(
            () => _marches.CreerDepuisDossierAsync(marche),
            $"Le marché « {marche.RefMarche} » a été créé.");

        await ChargerAsync();
        MarcheSelectionne = Marches.FirstOrDefault(m => m.RefMarche == marche.RefMarche);
    }

    [RelayCommand]
    private async Task ModifierMarche()
    {
        if (MarcheSelectionne is null)
        {
            return;
        }

        var dossiers = new List<OptionDossier>
        {
            new(MarcheSelectionne.RefDossier, LibelleDossier(MarcheSelectionne))
        };
        var candidats = await ChargerCandidatsAsync(dossiers);

        var dialogue = new MarcheDialogViewModel(dossiers, candidats, MarcheSelectionne);
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        var marche = dialogue.VersEntite();
        await ExecuterAsync(
            () => _marches.ModifierAsync(marche),
            "Le marché a été modifié.");

        await ChargerAsync();
        MarcheSelectionne = Marches.FirstOrDefault(m => m.RefMarche == marche.RefMarche);
    }

    [RelayCommand]
    private async Task SupprimerMarche()
    {
        if (MarcheSelectionne is null)
        {
            return;
        }

        var reference = MarcheSelectionne.RefMarche;
        var confirme = await _dialogues.ConfirmerAsync(
            "Supprimer le marché",
            $"Confirmer la suppression du marché « {reference} » "
            + "(bordereau, suivi d'exécution et avenants inclus) ?");

        if (!confirme)
        {
            return;
        }

        await ExecuterAsync(() => _marches.SupprimerAsync(reference), "Le marché a été supprimé.");
        await ChargerAsync();
    }

    // ---------- Bordereau ----------

    [RelayCommand]
    private async Task AjouterElement()
    {
        if (MarcheSelectionne is null)
        {
            return;
        }

        var dialogue = new ElementMarcheDialogViewModel();
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        var element = dialogue.VersEntite();
        var refMarche = MarcheSelectionne.RefMarche;
        await ExecuterAsync(
            () => _marches.AjouterElementAsync(refMarche, element),
            "L'élément a été ajouté au bordereau.");

        await ChargerElementsAsync(refMarche);
    }

    [RelayCommand]
    private async Task ModifierElement()
    {
        if (MarcheSelectionne is null || ElementSelectionne is null)
        {
            return;
        }

        var dialogue = new ElementMarcheDialogViewModel(ElementSelectionne);
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        var element = dialogue.VersEntite();
        var refMarche = MarcheSelectionne.RefMarche;
        await ExecuterAsync(
            () => _marches.ModifierElementAsync(refMarche, element),
            "L'élément a été modifié.");

        await ChargerElementsAsync(refMarche);
    }

    [RelayCommand]
    private async Task SupprimerElement()
    {
        if (MarcheSelectionne is null || ElementSelectionne is null)
        {
            return;
        }

        var code = ElementSelectionne.CodeElement;
        var confirme = await _dialogues.ConfirmerAsync(
            "Supprimer l'élément",
            $"Confirmer la suppression de l'élément « {code} » ?");

        if (!confirme)
        {
            return;
        }

        var refMarche = MarcheSelectionne.RefMarche;
        await ExecuterAsync(
            () => _marches.SupprimerElementAsync(refMarche, code),
            "L'élément a été supprimé.");

        await ChargerElementsAsync(refMarche);
    }

    [RelayCommand]
    private async Task RecalculerMontants()
    {
        if (MarcheSelectionne is null)
        {
            return;
        }

        var refMarche = MarcheSelectionne.RefMarche;
        await ExecuterAsync(
            () => _marches.RecalculerMontantsAsync(refMarche),
            "Les montants ont été recalculés à partir du bordereau (TVA 18 %).");

        await ChargerAsync();
        MarcheSelectionne = Marches.FirstOrDefault(m => m.RefMarche == refMarche);
    }

    // ---------- Interne ----------

    partial void OnMarcheSelectionneChanged(Marche? value)
    {
        Elements.Clear();
        ElementSelectionne = null;

        if (value is not null)
        {
            _ = ChargerElementsAsync(value.RefMarche);
        }
    }

    private async Task ChargerElementsAsync(string refMarche)
    {
        try
        {
            var marche = await _marches.ObtenirAsync(refMarche);
            Elements.Clear();
            foreach (var element in marche.Elements)
            {
                Elements.Add(element);
            }

            ElementSelectionne = Elements.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Erreur = ex.Message;
        }
    }

    /// <summary>Dossiers DAO qui ne sont pas encore rattachés à un marché.</summary>
    private async Task<IReadOnlyList<OptionDossier>> ChargerDossiersDisponiblesAsync()
    {
        var dejaAssocies = (await _marches.ListerAsync())
            .Select(m => m.RefDossier)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var dossiers = new List<OptionDossier>();
        foreach (var dossier in await _dao.ListerAsync())
        {
            if (dejaAssocies.Contains(dossier.RefDossier))
            {
                continue;
            }

            dossiers.Add(new OptionDossier(
                dossier.RefDossier,
                $"{dossier.RefDossier} — {dossier.ObjetDossier}"));
        }

        return dossiers;
    }

    private async Task<IReadOnlyDictionary<string, IReadOnlyList<OptionCandidat>>> ChargerCandidatsAsync(
        IReadOnlyList<OptionDossier> dossiers)
    {
        var resultat = new Dictionary<string, IReadOnlyList<OptionCandidat>>(StringComparer.OrdinalIgnoreCase);

        foreach (var dossier in dossiers)
        {
            var complet = await _dao.ObtenirAsync(dossier.RefDossier);
            resultat[dossier.RefDossier] = complet.Candidats
                .Select(c => new OptionCandidat(c.IdCandidat, c.NomEntreprise, c.EstRetenu))
                .ToList();
        }

        return resultat;
    }

    private static string LibelleDossier(Marche marche)
        => $"{marche.RefDossier} — {marche.ObjetMarche}";

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

    /// <summary>Formate un montant pour l'affichage.</summary>
    public static string FormaterMontant(decimal montant)
        => montant.ToString("N0", CultureInfo.GetCultureInfo("fr-FR")) + " FCFA";
}

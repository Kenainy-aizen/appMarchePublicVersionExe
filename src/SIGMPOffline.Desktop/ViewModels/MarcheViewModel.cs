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
public partial class MarcheViewModel : PageViewModelBase
{
    private readonly IMarcheService _marches;
    private readonly IDossierDaoService _dao;
    private readonly IDialogService _dialogues;

    public MarcheViewModel(
        IMarcheService marches,
        IDossierDaoService dao,
        IDialogService dialogues,
        ModeLectureService modeLecture)
        : base(modeLecture)
    {
        _marches = marches ?? throw new ArgumentNullException(nameof(marches));
        _dao = dao ?? throw new ArgumentNullException(nameof(dao));
        _dialogues = dialogues ?? throw new ArgumentNullException(nameof(dialogues));

        Marches.CollectionChanged += (_, _) => NotifierCompteursMarches();

        Elements.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(NombreElements));
            OnPropertyChanged(nameof(TotalBordereau));
            OnPropertyChanged(nameof(TotalBordereauAffiche));
        };
    }

    /// <inheritdoc />
    public override string Titre => "Marchés";

    /// <summary>Marchés formalisés.</summary>
    public ObservableCollection<Marche> Marches { get; } = new();

    /// <summary>Éléments (bordereau) du marché sélectionné.</summary>
    public ObservableCollection<ElementMarche> Elements { get; } = new();

    [ObservableProperty] private string _rechercheMarche = string.Empty;
    [ObservableProperty] private string _filtreStatutMarche = "Tous";

    /// <summary>Marchés correspondant à la recherche et au statut choisi.</summary>
    public IEnumerable<Marche> MarchesFiltres => Marches.Where(marche =>
        (string.IsNullOrWhiteSpace(RechercheMarche)
            || marche.RefMarche.Contains(RechercheMarche, StringComparison.OrdinalIgnoreCase)
            || marche.ObjetMarche.Contains(RechercheMarche, StringComparison.OrdinalIgnoreCase)
            || marche.RefDossier.Contains(RechercheMarche, StringComparison.OrdinalIgnoreCase))
        && (FiltreStatutMarche == "Tous" || Libelles.Pour(marche.StatutExecution) == FiltreStatutMarche));

    public IReadOnlyList<string> StatutsMarchesDisponibles { get; } = new[] { "Tous", "Non démarré", "En cours", "Réception provisoire", "Réception définitive", "Clôturé", "Résilié", "Annulé" };

    partial void OnRechercheMarcheChanged(string value) => OnPropertyChanged(nameof(MarchesFiltres));
    partial void OnFiltreStatutMarcheChanged(string value) => OnPropertyChanged(nameof(MarchesFiltres));

    [RelayCommand]
    private void ReinitialiserFiltres()
    {
        RechercheMarche = string.Empty;
        FiltreStatutMarche = "Tous";
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AUnMarche))]
    [NotifyPropertyChangedFor(nameof(TitreDetailMarche))]
    [NotifyPropertyChangedFor(nameof(DossierAffiche))]
    [NotifyPropertyChangedFor(nameof(TitulaireAffiche))]
    [NotifyPropertyChangedFor(nameof(DelaiAffiche))]
    [NotifyPropertyChangedFor(nameof(MontantHtAffiche))]
    [NotifyPropertyChangedFor(nameof(MontantTtcAffiche))]
    private Marche? _marcheSelectionne;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AUnElement))]
    private ElementMarche? _elementSelectionne;

    /// <summary>Vrai quand l'écran « détail d'un marché » est affiché à la place de la liste.</summary>
    [ObservableProperty] private bool _estEnVueDetail;

    [ObservableProperty] private string? _erreur;
    [ObservableProperty] private string? _message;

    /// <summary>Vrai si un marché est sélectionné.</summary>
    public bool AUnMarche => MarcheSelectionne is not null;

    /// <summary>Vrai si un élément du bordereau est sélectionné.</summary>
    public bool AUnElement => ElementSelectionne is not null;

    // ---------- Chiffres de la liste des marchés ----------

    /// <summary>Nombre de marchés de la province.</summary>
    public int NombreMarches => Marches.Count;

    /// <summary>Somme des montants TTC de tous les marchés.</summary>
    public decimal MontantTtcTotal => Marches.Sum(marche => marche.MontantTTC);

    /// <summary>Montant TTC total formaté en Ariary.</summary>
    public string MontantTtcTotalAffiche => $"{MontantTtcTotal:N0} Ar";

    /// <summary>Nombre total d'éléments de bordereau, tous marchés confondus.</summary>
    public int NombreElementsTousMarches => Marches.Sum(marche => marche.Elements.Count);

    // ---------- Chiffres du marché ouvert ----------

    /// <summary>Nombre d'éléments du bordereau du marché ouvert.</summary>
    public int NombreElements => Elements.Count;

    /// <summary>Somme des montants des lignes du bordereau (HT).</summary>
    public decimal TotalBordereau => Elements.Sum(element => element.MontantLigne);

    /// <summary>Total du bordereau formaté en Ariary.</summary>
    public string TotalBordereauAffiche => $"{TotalBordereau:N0} Ar";

    /// <summary>Titre du marché ouvert.</summary>
    public string TitreDetailMarche
        => MarcheSelectionne is { } marche
            ? $"Marché n° {marche.RefMarche}"
            : "Marché";

    /// <summary>Dossier DAO d'origine du marché.</summary>
    public string DossierAffiche
        => MarcheSelectionne?.Dossier is { } dossier
            ? $"{dossier.RefDossier} — {dossier.ObjetDossier}"
            : "—";

    /// <summary>Titulaire du marché (acheteur DAO retenu).</summary>
    public string TitulaireAffiche
        => MarcheSelectionne?.Titulaire is { } titulaire
            ? $"{titulaire.RaisonSociale} ({titulaire.NumStat})"
            : "—";

    /// <summary>Délai d'exécution affiché.</summary>
    public string DelaiAffiche
        => $"{(MarcheSelectionne?.DelaiExecutionJours ?? 0)} jours";

    /// <summary>Montant HT formaté en Ariary.</summary>
    public string MontantHtAffiche
        => $"{(MarcheSelectionne?.MontantHT ?? 0m):N0} Ar";

    /// <summary>Montant TTC formaté en Ariary.</summary>
    public string MontantTtcAffiche
        => $"{(MarcheSelectionne?.MontantTTC ?? 0m):N0} Ar";

    /// <inheritdoc />
    [RelayCommand]
    public override async Task ChargerAsync()
    {
        try
        {
            EstOccupe = true;
            Erreur = null;

            // On garde le marché sélectionné (s'il existe encore) après un rechargement.
            var refConservee = MarcheSelectionne?.RefMarche;

            var marches = await _marches.ListerAvecBordereauAsync();
            Marches.Clear();
            foreach (var marche in marches)
            {
                Marches.Add(marche);
            }

            MarcheSelectionne = refConservee is null
                ? Marches.FirstOrDefault()
                : Marches.FirstOrDefault(m => m.RefMarche == refConservee) ?? Marches.FirstOrDefault();

            if (MarcheSelectionne is null)
            {
                // Plus de marché à afficher : retour à la liste.
                EstEnVueDetail = false;
            }

            NotifierCompteursMarches();
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

    // ---------- Navigation liste <-> détail ----------

    /// <summary>« Ouvrir » : affiche le détail du marché choisi à la place de la liste.</summary>
    [RelayCommand]
    private void OuvrirMarche(Marche? marcheChoisi)
    {
        var marche = marcheChoisi ?? MarcheSelectionne;
        if (marche is null)
        {
            return;
        }

        Erreur = null;
        Message = null;

        var memeSelection = ReferenceEquals(MarcheSelectionne, marche);
        MarcheSelectionne = marche;
        EstEnVueDetail = true;

        if (memeSelection)
        {
            // Pas de changement de sélection : on relit le bordereau pour être à jour.
            _ = ChargerElementsAsync(marche.RefMarche);
        }
    }

    /// <summary>« Retour aux marchés » : revient à la liste (rechargée pour mettre à jour les compteurs).</summary>
    [RelayCommand]
    private async Task RetourListe()
    {
        EstEnVueDetail = false;
        Erreur = null;
        Message = null;
        await ChargerAsync();
    }

    // ---------- Marchés ----------

    [RelayCommand]
    private async Task NouveauMarche()
    {
        if (!PeutModifier)
        {
            return;
        }

        var dossiers = await ChargerDossiersDisponiblesAsync();
        if (dossiers.Count == 0)
        {
            await _dialogues.InformerAsync(
                "Aucun dossier disponible",
                "Tous les dossiers DAO sont déjà associés à un marché. "
                + "Créez d'abord un nouveau dossier dans le module « DAO & Attribution ».");
            return;
        }

        var titulaires = await ChargerTitulairesAsync(dossiers);
        var dialogue = new MarcheDialogViewModel(dossiers, titulaires);
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
    private async Task ModifierMarche(Marche? marcheSelectionnee)
    {
        if (!PeutModifier)
        {
            return;
        }

        var marcheActuel = marcheSelectionnee ?? MarcheSelectionne;
        if (marcheActuel is null)
        {
            return;
        }

        var dossiers = new List<OptionDossier>
        {
            new(marcheActuel.RefDossier, LibelleDossier(marcheActuel))
        };
        var titulaires = await ChargerTitulairesAsync(dossiers);

        var dialogue = new MarcheDialogViewModel(dossiers, titulaires, marcheActuel);
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
    private async Task SupprimerMarche(Marche? marcheSelectionnee)
    {
        if (!PeutModifier)
        {
            return;
        }

        var marcheActuel = marcheSelectionnee ?? MarcheSelectionne;
        if (marcheActuel is null)
        {
            return;
        }

        var reference = marcheActuel.RefMarche;
        var confirme = await _dialogues.ConfirmerAsync(
            "Supprimer le marché",
            $"Confirmer la suppression du marché « {reference} » "
            + "(bordereau, suivi d'exécution et avenants inclus) ?");

        if (!confirme)
        {
            return;
        }

        if (await ExecuterAsync(() => _marches.SupprimerAsync(reference), "Le marché a été supprimé."))
        {
            // Plus rien à afficher dans le détail : retour à la liste.
            EstEnVueDetail = false;
            MarcheSelectionne = null;
            await ChargerAsync();
        }
    }

    // ---------- Bordereau ----------

    [RelayCommand]
    private async Task AjouterElement()
    {
        if (!PeutModifier)
        {
            return;
        }

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
    private async Task ModifierElement(ElementMarche? elementSelectionne)
    {
        if (!PeutModifier)
        {
            return;
        }

        if (MarcheSelectionne is null)
        {
            return;
        }

        var elementActuel = elementSelectionne ?? ElementSelectionne;
        if (elementActuel is null)
        {
            return;
        }

        var dialogue = new ElementMarcheDialogViewModel(elementActuel);
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
    private async Task SupprimerElement(ElementMarche? elementSelectionne)
    {
        if (!PeutModifier)
        {
            return;
        }

        if (MarcheSelectionne is null)
        {
            return;
        }

        var elementActuel = elementSelectionne ?? ElementSelectionne;
        if (elementActuel is null)
        {
            return;
        }

        var code = elementActuel.CodeElement;
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
        if (!PeutModifier)
        {
            return;
        }

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

    private async Task<IReadOnlyDictionary<string, IReadOnlyList<OptionTitulaire>>> ChargerTitulairesAsync(
        IReadOnlyList<OptionDossier> dossiers)
    {
        var resultat = new Dictionary<string, IReadOnlyList<OptionTitulaire>>(StringComparer.OrdinalIgnoreCase);

        foreach (var dossier in dossiers)
        {
            var complet = await _dao.ObtenirAsync(dossier.RefDossier);
            resultat[dossier.RefDossier] = complet.Acheteurs
                .Select(a => new OptionTitulaire(a.NumStat, $"{a.RaisonSociale} ({a.NumStat})"))
                .ToList();
        }

        return resultat;
    }

    private static string LibelleDossier(Marche marche)
        => $"{marche.RefDossier} — {marche.ObjetMarche}";

    private void NotifierCompteursMarches()
    {
        OnPropertyChanged(nameof(NombreMarches));
        OnPropertyChanged(nameof(MontantTtcTotal));
        OnPropertyChanged(nameof(MontantTtcTotalAffiche));
        OnPropertyChanged(nameof(NombreElementsTousMarches));
    }

    /// <summary>Exécute une action, affiche le message de succès ou l'erreur. Retourne <c>true</c> si elle a réussi.</summary>
    private async Task<bool> ExecuterAsync(Func<Task> action, string messageSucces)
    {
        try
        {
            EstOccupe = true;
            Erreur = null;
            Message = null;

            await action();
            Message = messageSucces;
            return true;
        }
        catch (Exception ex)
        {
            Erreur = ex.Message;
            await _dialogues.InformerAsync("Erreur", ex.Message);
            return false;
        }
        finally
        {
            EstOccupe = false;
        }
    }

    /// <summary>Formate un montant pour l'affichage.</summary>
    public static string FormaterMontant(decimal montant)
        => montant.ToString("N0", CultureInfo.GetCultureInfo("fr-FR")) + " Ar";
}

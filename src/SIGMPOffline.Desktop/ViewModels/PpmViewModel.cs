using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIGMPOffline.Desktop.Services;
using SIGMPOffline.Desktop.ViewModels.Dialogs;
using SIGMPOffline.Domain.Entities;
using SIGMPOffline.Services.Abstractions;

namespace SIGMPOffline.Desktop.ViewModels;

/// <summary>
/// Module PPM : deux écrans dans la même page.
/// 1. La liste des plans (en-têtes) avec Ouvrir / Modifier / Supprimer ;
/// 2. le détail d'un plan (ses lignes), affiché seulement après « Ouvrir ».
/// </summary>
public partial class PpmViewModel : PageViewModelBase
{
    private readonly IPpmService _ppm;
    private readonly IDialogService _dialogues;

    public PpmViewModel(IPpmService ppm, IDialogService dialogues, ModeLectureService modeLecture)
        : base(modeLecture)
    {
        _ppm = ppm ?? throw new ArgumentNullException(nameof(ppm));
        _dialogues = dialogues ?? throw new ArgumentNullException(nameof(dialogues));

        Entetes.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(NombrePlans));
            OnPropertyChanged(nameof(NombreLignesTousPlans));
            OnPropertyChanged(nameof(MontantTousPlansAffiche));
        };

        Details.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(NombreLignesAffiche));
            OnPropertyChanged(nameof(MontantTotalPlan));
            OnPropertyChanged(nameof(MontantTotalAffiche));
        };
    }

    /// <inheritdoc />
    public override string Titre => "Plan de Passation des Marchés";

    /// <summary>En-têtes de PPM.</summary>
    public ObservableCollection<PpmEntete> Entetes { get; } = new();

    /// <summary>Lignes du PPM sélectionné.</summary>
    public ObservableCollection<PpmDetail> Details { get; } = new();

    [ObservableProperty] private string _recherchePpm = string.Empty;
    [ObservableProperty] private string _exerciceFiltre = "Tous";

    /// <summary>Plans correspondant à la recherche et à l'exercice sélectionné.</summary>
    public IEnumerable<PpmEntete> EntetesFiltres => Entetes.Where(entete =>
        (string.IsNullOrWhiteSpace(RecherchePpm)
            || entete.NumPPM.Contains(RecherchePpm, StringComparison.OrdinalIgnoreCase)
            || entete.AutoriteContractante.Contains(RecherchePpm, StringComparison.OrdinalIgnoreCase))
        && (ExerciceFiltre == "Tous" || entete.AnneeExercice.ToString() == ExerciceFiltre));

    public IReadOnlyList<string> ExercicesDisponibles
        => new[] { "Tous" }
            .Concat(Entetes.Select(entete => entete.AnneeExercice.ToString()).Distinct().OrderByDescending(annee => annee))
            .ToArray();

    partial void OnRecherchePpmChanged(string value) => OnPropertyChanged(nameof(EntetesFiltres));
    partial void OnExerciceFiltreChanged(string value) => OnPropertyChanged(nameof(EntetesFiltres));

    [RelayCommand]
    private void ReinitialiserFiltres()
    {
        RecherchePpm = string.Empty;
        ExerciceFiltre = "Tous";
    }

    /// <summary>
    /// Ligne sélectionnée dans la liste. Volontairement distincte de l'élément ouvert en détail :
    /// la liste remet sa sélection à null à chaque rafraîchissement, ce qui ne doit pas vider le détail.
    /// </summary>
    [ObservableProperty] private PpmEntete? _enteteListeSelectionne;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AUnEntete))]
    [NotifyPropertyChangedFor(nameof(TitreDetailPpm))]
    [NotifyPropertyChangedFor(nameof(DerniereMiseAJourAffichee))]
    private PpmEntete? _enteteSelectionne;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AUnDetail))]
    private PpmDetail? _detailSelectionne;

    /// <summary>Vrai quand l'écran « détail d'un plan » est affiché à la place de la liste des plans.</summary>
    [ObservableProperty] private bool _estEnVueDetail;

    [ObservableProperty] private string? _erreur;
    [ObservableProperty] private string? _message;

    // ---------- Chiffres de la liste des plans ----------

    /// <summary>Nombre de plans de la province.</summary>
    public int NombrePlans => Entetes.Count;

    /// <summary>Nombre total de lignes, tous plans confondus.</summary>
    public int NombreLignesTousPlans => Entetes.Sum(entete => entete.Details.Count);

    /// <summary>Montant estimatif de tous les plans, formaté en Ariary.</summary>
    public string MontantTousPlansAffiche
        => $"{Entetes.Sum(entete => entete.Details.Sum(detail => detail.MontantEstimatif)):N0} Ar";

    // ---------- Chiffres du plan ouvert ----------

    /// <summary>Nombre de lignes du plan sélectionné.</summary>
    public int NombreLignesAffiche => Details.Count;

    /// <summary>Somme des montants estimatifs du plan sélectionné.</summary>
    public decimal MontantTotalPlan => Details.Sum(detail => detail.MontantEstimatif);

    /// <summary>Montant total formaté en Ariary.</summary>
    public string MontantTotalAffiche => $"{MontantTotalPlan:N0} Ar";

    /// <summary>Titre complet du plan sélectionné.</summary>
    public string TitreDetailPpm
        => EnteteSelectionne is { } entete
            ? $"Plan de passation n° {entete.NumPPM} — exercice {entete.AnneeExercice}"
            : "Plan de passation";

    /// <summary>Dernière mise à jour avec numéro lisible.</summary>
    public string DerniereMiseAJourAffichee
        => EnteteSelectionne?.DateDerniereMAJ is DateTime date
            ? string.IsNullOrWhiteSpace(EnteteSelectionne.NumDerniereMAJ)
                ? date.ToString("dd/MM/yyyy")
                : $"{date:dd/MM/yyyy} — MAJ n° {EnteteSelectionne.NumDerniereMAJ}"
            : "—";

    /// <summary>Vrai si un en-tête est sélectionné.</summary>
    public bool AUnEntete => EnteteSelectionne is not null;

    /// <summary>Vrai si une ligne de détail est sélectionnée.</summary>
    public bool AUnDetail => DetailSelectionne is not null;

    /// <inheritdoc />
    [RelayCommand]
    public override async Task ChargerAsync()
    {
        try
        {
            EstOccupe = true;
            Erreur = null;

            // On garde le plan sélectionné (s'il existe encore) après un rechargement.
            var numConserve = EnteteSelectionne?.NumPPM;

            var entetes = await _ppm.ListerAsync();
            Entetes.Clear();
            foreach (var entete in entetes)
            {
                Entetes.Add(entete);
            }
            OnPropertyChanged(nameof(ExercicesDisponibles));

            EnteteSelectionne = numConserve is null
                ? null
                : Entetes.FirstOrDefault(e => e.NumPPM == numConserve);

            if (EnteteSelectionne is null)
            {
                // Le plan affiché n'existe plus : retour à la liste.
                EstEnVueDetail = false;
            }

            OnPropertyChanged(nameof(EntetesFiltres));
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

    /// <summary>« Ouvrir » : affiche le détail du plan choisi à la place de la liste.</summary>
    [RelayCommand]
    private void OuvrirPpm(PpmEntete? enteteChoisi)
    {
        var entete = enteteChoisi ?? EnteteListeSelectionne;
        if (entete is null)
        {
            return;
        }

        Erreur = null;
        Message = null;

        var memeSelection = ReferenceEquals(EnteteSelectionne, entete);
        EnteteSelectionne = entete;
        EstEnVueDetail = true;

        if (memeSelection)
        {
            // Pas de changement de sélection : on relit les lignes pour être à jour.
            _ = ChargerDetailsAsync(entete.NumPPM);
        }
    }

    /// <summary>« Retour aux plans » : revient à la liste (rechargée pour mettre à jour totaux et dates).</summary>
    [RelayCommand]
    private async Task RetourListe()
    {
        EstEnVueDetail = false;
        Erreur = null;
        Message = null;
        await ChargerAsync();
    }

    // ---------- En-tête ----------

    [RelayCommand]
    private async Task NouveauPpm()
    {
        if (!PeutModifier)
        {
            return;
        }

        var dialogue = new PpmEnteteDialogViewModel();
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        var entete = dialogue.VersEntite();
        var cree = await ExecuterAsync(
            () => _ppm.CreerAsync(entete),
            $"Le PPM « {entete.NumPPM} » a été créé.");

        if (cree)
        {
            await ChargerAsync();
            EnteteSelectionne = Entetes.FirstOrDefault(e => e.NumPPM == entete.NumPPM);
        }
    }

    [RelayCommand]
    private async Task ModifierPpm(PpmEntete? enteteChoisi)
    {
        if (!PeutModifier)
        {
            return;
        }

        var enteteActuelle = enteteChoisi ?? EnteteSelectionne;
        if (enteteActuelle is null)
        {
            return;
        }

        var dialogue = new PpmEnteteDialogViewModel(enteteActuelle);
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        var entete = dialogue.VersEntite();
        var modifie = await ExecuterAsync(() => _ppm.ModifierAsync(entete), "Le PPM a été modifié.");

        if (modifie)
        {
            // Dans la vue détail on reste sur le même plan ; depuis la liste, on le sélectionne.
            await ChargerAsync();
            EnteteSelectionne = Entetes.FirstOrDefault(e => e.NumPPM == entete.NumPPM);
        }
    }

    [RelayCommand]
    private async Task SupprimerPpm(PpmEntete? enteteChoisi)
    {
        if (!PeutModifier)
        {
            return;
        }

        var enteteActuelle = enteteChoisi ?? EnteteSelectionne;
        if (enteteActuelle is null)
        {
            return;
        }

        var numPpm = enteteActuelle.NumPPM;
        var confirme = await _dialogues.ConfirmerAsync(
            "Supprimer le PPM",
            $"Confirmer la suppression du PPM « {numPpm} » et de toutes ses lignes ?");

        if (!confirme)
        {
            return;
        }

        var supprime = await ExecuterAsync(() => _ppm.SupprimerAsync(numPpm), "Le PPM a été supprimé.");
        if (supprime)
        {
            // Plus rien à afficher dans le détail : retour à la liste.
            EstEnVueDetail = false;
            EnteteSelectionne = null;
            await ChargerAsync();
        }
    }

    // ---------- Mise à jour et lignes de détail ----------

    [RelayCommand]
    private async Task EnregistrerMiseAJour()
    {
        if (!PeutModifier)
        {
            return;
        }

        if (EnteteSelectionne is null)
        {
            return;
        }

        var confirme = await _dialogues.ConfirmerAsync(
            "Enregistrer une mise à jour",
            "Confirmer l'enregistrement d'une mise à jour à la date du jour ?");
        if (!confirme)
        {
            return;
        }

        var numPpm = EnteteSelectionne.NumPPM;
        if (await ExecuterAsync(() => _ppm.EnregistrerMiseAJourAsync(numPpm), "Mise à jour enregistrée."))
        {
            await ChargerAsync();
        }
    }

    [RelayCommand]
    private async Task AjouterDetail()
    {
        if (!PeutModifier)
        {
            return;
        }

        if (EnteteSelectionne is null)
        {
            return;
        }

        var dialogue = new PpmDetailDialogViewModel();
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        var numPpm = EnteteSelectionne.NumPPM;
        var detail = dialogue.VersEntite();
        if (await ExecuterAsync(() => _ppm.AjouterDetailAsync(numPpm, detail), "La ligne a été ajoutée."))
        {
            await ChargerAsync();
        }
    }

    [RelayCommand]
    private async Task ModifierDetail(PpmDetail? ligneChoisie)
    {
        if (!PeutModifier)
        {
            return;
        }

        if (EnteteSelectionne is null)
        {
            return;
        }

        var ligneActuelle = ligneChoisie ?? DetailSelectionne;
        if (ligneActuelle is null)
        {
            return;
        }

        var dialogue = new PpmDetailDialogViewModel(ligneActuelle);
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        var numPpm = EnteteSelectionne.NumPPM;
        var detail = dialogue.VersEntite();
        if (await ExecuterAsync(() => _ppm.ModifierDetailAsync(numPpm, detail), "La ligne a été modifiée."))
        {
            await ChargerAsync();
        }
    }

    [RelayCommand]
    private async Task SupprimerDetail(PpmDetail? ligneChoisie)
    {
        if (!PeutModifier)
        {
            return;
        }

        if (EnteteSelectionne is null)
        {
            return;
        }

        var ligneActuelle = ligneChoisie ?? DetailSelectionne;
        if (ligneActuelle is null)
        {
            return;
        }

        var numPpm = EnteteSelectionne.NumPPM;
        var numDetail = ligneActuelle.NumPPMDetail;
        var confirme = await _dialogues.ConfirmerAsync(
            "Supprimer la ligne",
            $"Confirmer la suppression de la ligne « {numDetail} » ?");

        if (!confirme)
        {
            return;
        }

        if (await ExecuterAsync(() => _ppm.SupprimerDetailAsync(numPpm, numDetail), "La ligne a été supprimée."))
        {
            await ChargerAsync();
        }
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
            foreach (var detail in entete.Details.OrderBy(d => d.NumPPMDetail))
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
}

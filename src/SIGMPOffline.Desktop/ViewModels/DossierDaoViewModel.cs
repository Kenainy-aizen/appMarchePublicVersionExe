using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIGMPOffline.Desktop.Services;
using SIGMPOffline.Desktop.ViewModels.Dialogs;
using SIGMPOffline.Domain.Entities;
using SIGMPOffline.Services.Abstractions;
using SIGMPOffline.Services.Scoping;

namespace SIGMPOffline.Desktop.ViewModels;

/// <summary>Module DAO &amp; Attribution : CRUD des dossiers, candidats et acheteurs.</summary>
public partial class DossierDaoViewModel : PageViewModelBase
{
    private readonly IScopeRunner _scopeRunner;
    private readonly IDialogService _dialogues;

    // Un seul chargement de détail à la fois : évite deux lectures concurrentes.
    private readonly SemaphoreSlim _chargementDetails = new(1, 1);
    private bool _chargementDetailsSuspendu;

    public DossierDaoViewModel(
        IScopeRunner scopeRunner,
        IDialogService dialogues,
        ModeLectureService modeLecture)
        : base(modeLecture)
    {
        _scopeRunner = scopeRunner ?? throw new ArgumentNullException(nameof(scopeRunner));
        _dialogues = dialogues ?? throw new ArgumentNullException(nameof(dialogues));

        Dossiers.CollectionChanged += (_, _) => NotifierCompteursDossiers();

        Candidats.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(NombreCandidats));
            OnPropertyChanged(nameof(MontantTotalOffres));
            OnPropertyChanged(nameof(MontantTotalOffresAffiche));
        };

        Acheteurs.CollectionChanged += (_, _) => OnPropertyChanged(nameof(NombreAcheteurs));
    }

    /// <inheritdoc />
    public override string Titre => "DAO & Attribution";

    /// <summary>Dossiers d'appel d'offres.</summary>
    public ObservableCollection<DossierDao> Dossiers { get; } = new();

    /// <summary>Candidats du dossier sélectionné, numérotés pour l'affichage.</summary>
    public ObservableCollection<LigneCandidat> Candidats { get; } = new();

    /// <summary>Acheteurs du dossier sélectionné, numérotés pour l'affichage.</summary>
    public ObservableCollection<LigneAcheteur> Acheteurs { get; } = new();

    [ObservableProperty] private string _rechercheDossier = string.Empty;
    [ObservableProperty] private string _filtreDossiers = "Tous";

    /// <summary>Dossiers correspondant à la recherche et au filtre de contenu.</summary>
    public IEnumerable<DossierDao> DossiersFiltres => Dossiers.Where(dossier =>
        (string.IsNullOrWhiteSpace(RechercheDossier)
            || dossier.RefDossier.Contains(RechercheDossier, StringComparison.OrdinalIgnoreCase)
            || dossier.ObjetDossier.Contains(RechercheDossier, StringComparison.OrdinalIgnoreCase)
            || (dossier.PpmDetail?.ObjetProjet?.Contains(RechercheDossier, StringComparison.OrdinalIgnoreCase) ?? false))
        && (FiltreDossiers == "Tous"
            || (FiltreDossiers == "Avec candidats" && dossier.Candidats.Count > 0)
            || (FiltreDossiers == "Sans candidats" && dossier.Candidats.Count == 0)));

    public IReadOnlyList<string> FiltresDossiersDisponibles { get; } = new[] { "Tous", "Avec candidats", "Sans candidats" };

    partial void OnRechercheDossierChanged(string value) => OnPropertyChanged(nameof(DossiersFiltres));
    partial void OnFiltreDossiersChanged(string value) => OnPropertyChanged(nameof(DossiersFiltres));

    [RelayCommand]
    private void ReinitialiserFiltres()
    {
        RechercheDossier = string.Empty;
        FiltreDossiers = "Tous";
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AUnDossier))]
    [NotifyPropertyChangedFor(nameof(TitreDetailDossier))]
    [NotifyPropertyChangedFor(nameof(LignePpmAffichee))]
    [NotifyPropertyChangedFor(nameof(CoutCahierAffiche))]
    [NotifyPropertyChangedFor(nameof(CautionAffichee))]
    private DossierDao? _dossierSelectionne;

    /// <summary>
    /// Ligne sélectionnée dans la liste. Volontairement distincte de l'élément ouvert en détail :
    /// la liste remet sa sélection à null à chaque rafraîchissement, ce qui ne doit pas vider le détail.
    /// </summary>
    [ObservableProperty] private DossierDao? _dossierListeSelectionne;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AUnCandidat))]
    private LigneCandidat? _candidatSelectionne;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AUnAcheteur))]
    private LigneAcheteur? _acheteurSelectionne;

    /// <summary>Vrai quand l'écran « détail d'un dossier » est affiché à la place de la liste.</summary>
    [ObservableProperty] private bool _estEnVueDetail;

    [ObservableProperty] private string? _erreur;
    [ObservableProperty] private string? _message;

    /// <summary>Vrai si un dossier est sélectionné.</summary>
    public bool AUnDossier => DossierSelectionne is not null;

    /// <summary>Vrai si un candidat est sélectionné.</summary>
    public bool AUnCandidat => CandidatSelectionne is not null;

    /// <summary>Vrai si un acheteur est sélectionné.</summary>
    public bool AUnAcheteur => AcheteurSelectionne is not null;

    // ---------- Chiffres de la liste des dossiers ----------

    /// <summary>Nombre de dossiers de la province.</summary>
    public int NombreDossiers => Dossiers.Count;

    /// <summary>Nombre total de candidats, tous dossiers confondus.</summary>
    public int NombreCandidatsTousDossiers => Dossiers.Sum(dossier => dossier.Candidats.Count);

    /// <summary>Nombre total d'acheteurs, tous dossiers confondus.</summary>
    public int NombreAcheteursTousDossiers => Dossiers.Sum(dossier => dossier.Acheteurs.Count);

    // ---------- Chiffres du dossier ouvert ----------

    /// <summary>Nombre de candidats du dossier ouvert.</summary>
    public int NombreCandidats => Candidats.Count;

    /// <summary>Nombre d'acheteurs du dossier ouvert.</summary>
    public int NombreAcheteurs => Acheteurs.Count;

    /// <summary>Somme des offres des candidats du dossier ouvert.</summary>
    public decimal MontantTotalOffres => Candidats.Sum(candidat => candidat.MontantOffre);

    /// <summary>Montant total des offres formaté en Ariary.</summary>
    public string MontantTotalOffresAffiche => $"{MontantTotalOffres:N0} Ar";

    /// <summary>Titre du dossier ouvert.</summary>
    public string TitreDetailDossier
        => DossierSelectionne is { } dossier
            ? $"Dossier n° {dossier.RefDossier}"
            : "Dossier d'appel d'offres";

    /// <summary>Ligne PPM rattachée au dossier.</summary>
    public string LignePpmAffichee
        => DossierSelectionne?.PpmDetail is { } ligne
            ? $"{ligne.NumPPMDetail} — {ligne.ObjetProjet}"
            : "—";

    /// <summary>Coût du cahier des charges formaté en Ariary.</summary>
    public string CoutCahierAffiche
        => $"{(DossierSelectionne?.CoutCahierCharges ?? 0m):N0} Ar";

    /// <summary>Montant de la caution de soumission formaté en Ariary.</summary>
    public string CautionAffichee
        => $"{(DossierSelectionne?.MontantCaution ?? 0m):N0} Ar";

    /// <inheritdoc />
    [RelayCommand]
    public override async Task ChargerAsync()
    {
        try
        {
            EstOccupe = true;
            Erreur = null;

            // On garde le dossier sélectionné (s'il existe encore) après un rechargement.
            var refConserve = DossierSelectionne?.RefDossier;

            IReadOnlyList<DossierDao> dossiers;
            await _chargementDetails.WaitAsync();
            try
            {
                dossiers = await _scopeRunner.ExecuterAsync<IDossierDaoService, IReadOnlyList<DossierDao>>(
                    service => service.ListerAvecDetailsAsync());
            }
            finally
            {
                _chargementDetails.Release();
            }

            Dossiers.Clear();
            foreach (var dossier in dossiers)
            {
                Dossiers.Add(dossier);
            }

            // Le setter déclenche normalement un chargement en tâche de fond : on le
            // suspend ici pour charger les détails une seule fois, de façon attendue.
            _chargementDetailsSuspendu = true;
            try
            {
                DossierSelectionne = refConserve is null
                    ? Dossiers.FirstOrDefault()
                    : Dossiers.FirstOrDefault(d => d.RefDossier == refConserve) ?? Dossiers.FirstOrDefault();
            }
            finally
            {
                _chargementDetailsSuspendu = false;
            }

            if (DossierSelectionne is null)
            {
                // Plus de dossier à afficher : retour à la liste.
                EstEnVueDetail = false;
            }
            else
            {
                await ChargerDetailsAsync(DossierSelectionne.RefDossier);
            }

            NotifierCompteursDossiers();
            OnPropertyChanged(nameof(DossiersFiltres));
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

    /// <summary>« Ouvrir » : affiche le détail du dossier choisi à la place de la liste.</summary>
    [RelayCommand]
    private async Task OuvrirDossier(DossierDao? dossierChoisi)
    {
        var dossier = dossierChoisi ?? DossierListeSelectionne;
        if (dossier is null)
        {
            return;
        }

        Erreur = null;
        Message = null;

        _chargementDetailsSuspendu = true;
        try
        {
            DossierSelectionne = dossier;
        }
        finally
        {
            _chargementDetailsSuspendu = false;
        }

        EstEnVueDetail = true;
        await ChargerDetailsAsync(dossier.RefDossier);
    }

    /// <summary>« Retour aux dossiers » : revient à la liste (rechargée pour mettre à jour les compteurs).</summary>
    [RelayCommand]
    private async Task RetourListe()
    {
        EstEnVueDetail = false;
        Erreur = null;
        Message = null;
        await ChargerAsync();
    }

    // ---------- Dossiers ----------

    [RelayCommand]
    private async Task NouveauDossier()
    {
        if (!PeutModifier)
        {
            return;
        }

        var lignes = await ChargerLignesPpmAsync();
        if (lignes.Count == 0)
        {
            await _dialogues.InformerAsync(
                "Aucune ligne de PPM",
                "Créez d'abord au moins une ligne dans un PPM pour pouvoir ouvrir un dossier.");
            return;
        }

        var dialogue = new DossierDaoDialogViewModel(lignes);
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        var dossier = dialogue.VersEntite();
        await ExecuterAsync(
            () => _scopeRunner.ExecuterAsync<IDossierDaoService>(service => service.CreerAsync(dossier)),
            $"Le dossier « {dossier.RefDossier} » a été créé.");

        await ChargerAsync();
        DossierSelectionne = Dossiers.FirstOrDefault(d => d.RefDossier == dossier.RefDossier);
    }

    [RelayCommand]
    private async Task ModifierDossier(DossierDao? dossierSelectionne)
    {
        if (!PeutModifier)
        {
            return;
        }

        var dossierActuel = dossierSelectionne ?? DossierSelectionne;
        if (dossierActuel is null)
        {
            return;
        }

        var lignes = await ChargerLignesPpmAsync();
        var dialogue = new DossierDaoDialogViewModel(lignes, dossierActuel);
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        var dossier = dialogue.VersEntite();
        await ExecuterAsync(
            () => _scopeRunner.ExecuterAsync<IDossierDaoService>(service => service.ModifierAsync(dossier)),
            "Le dossier a été modifié.");

        await ChargerAsync();
        DossierSelectionne = Dossiers.FirstOrDefault(d => d.RefDossier == dossier.RefDossier);
    }

    [RelayCommand]
    private async Task SupprimerDossier(DossierDao? dossierSelectionne)
    {
        if (!PeutModifier)
        {
            return;
        }

        var dossierActuel = dossierSelectionne ?? DossierSelectionne;
        if (dossierActuel is null)
        {
            return;
        }

        var refDossier = dossierActuel.RefDossier;
        var confirme = await _dialogues.ConfirmerAsync(
            "Supprimer le dossier",
            $"Confirmer la suppression du dossier « {refDossier} » (candidats et acheteurs inclus) ?");

        if (!confirme)
        {
            return;
        }

        if (await ExecuterAsync(
                () => _scopeRunner.ExecuterAsync<IDossierDaoService>(
                    service => service.SupprimerAsync(refDossier)),
                "Le dossier a été supprimé."))
        {
            // Plus rien à afficher dans le détail : retour à la liste.
            EstEnVueDetail = false;
            DossierSelectionne = null;
            await ChargerAsync();
        }
    }

    // ---------- Candidats ----------

    [RelayCommand]
    private async Task AjouterCandidat()
    {
        if (!PeutModifier)
        {
            return;
        }

        if (DossierSelectionne is null)
        {
            return;
        }

        var dialogue = new CandidatDialogViewModel();
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        var candidat = dialogue.VersEntite();
        await ExecuterAsync(
            () => _scopeRunner.ExecuterAsync<IDossierDaoService>(
                service => service.AjouterCandidatAsync(DossierSelectionne.RefDossier, candidat)),
            "Le candidat a été ajouté.");

        await ChargerDetailsAsync(DossierSelectionne.RefDossier);
    }

    [RelayCommand]
    private async Task ModifierCandidat(Candidat? candidatSelectionne)
    {
        if (!PeutModifier)
        {
            return;
        }

        if (DossierSelectionne is null)
        {
            return;
        }

        var candidatActuel = candidatSelectionne ?? CandidatSelectionne?.Candidat;
        if (candidatActuel is null)
        {
            return;
        }

        var dialogue = new CandidatDialogViewModel(candidatActuel);
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        var candidat = dialogue.VersEntite();
        await ExecuterAsync(
            () => _scopeRunner.ExecuterAsync<IDossierDaoService>(
                service => service.ModifierCandidatAsync(DossierSelectionne.RefDossier, candidat)),
            "Le candidat a été modifié.");

        await ChargerDetailsAsync(DossierSelectionne.RefDossier);
    }

    [RelayCommand]
    private async Task SupprimerCandidat(Candidat? candidatSelectionne)
    {
        if (!PeutModifier)
        {
            return;
        }

        if (DossierSelectionne is null)
        {
            return;
        }

        var candidatActuel = candidatSelectionne ?? CandidatSelectionne?.Candidat;
        if (candidatActuel is null)
        {
            return;
        }

        var nom = candidatActuel.NomEntreprise;
        var id = candidatActuel.IdCandidat;
        var confirme = await _dialogues.ConfirmerAsync(
            "Supprimer le candidat",
            $"Confirmer la suppression du candidat « {nom} » ?");

        if (!confirme)
        {
            return;
        }

        await ExecuterAsync(
            () => _scopeRunner.ExecuterAsync<IDossierDaoService>(
                service => service.SupprimerCandidatAsync(DossierSelectionne.RefDossier, id)),
            "Le candidat a été supprimé.");

        await ChargerDetailsAsync(DossierSelectionne.RefDossier);
    }

    [RelayCommand]
    private async Task DesignerRetenu()
    {
        if (!PeutModifier)
        {
            return;
        }

        if (DossierSelectionne is null || CandidatSelectionne is null)
        {
            return;
        }

        var refDossier = DossierSelectionne.RefDossier;
        var id = CandidatSelectionne.Candidat.IdCandidat;
        await ExecuterAsync(
            () => _scopeRunner.ExecuterAsync<IDossierDaoService>(
                service => service.DesignerRetenuAsync(refDossier, id)),
            "Le candidat retenu a été mis à jour.");

        await ChargerDetailsAsync(refDossier);
    }

    // ---------- Acheteurs ----------

    [RelayCommand]
    private async Task AjouterAcheteur()
    {
        if (!PeutModifier)
        {
            return;
        }

        if (DossierSelectionne is null)
        {
            return;
        }

        var dialogue = new AcheteurDaoDialogViewModel();
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        var acheteur = dialogue.VersEntite();
        await ExecuterAsync(
            () => _scopeRunner.ExecuterAsync<IDossierDaoService>(
                service => service.AjouterAcheteurAsync(DossierSelectionne.RefDossier, acheteur)),
            "L'acheteur a été ajouté.");

        await ChargerDetailsAsync(DossierSelectionne.RefDossier);
    }

    [RelayCommand]
    private async Task ModifierAcheteur(AcheteurDao? acheteurSelectionne)
    {
        if (!PeutModifier)
        {
            return;
        }

        if (DossierSelectionne is null)
        {
            return;
        }

        var acheteurActuel = acheteurSelectionne ?? AcheteurSelectionne?.Acheteur;
        if (acheteurActuel is null)
        {
            return;
        }

        var dialogue = new AcheteurDaoDialogViewModel(acheteurActuel);
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        var acheteur = dialogue.VersEntite();
        await ExecuterAsync(
            () => _scopeRunner.ExecuterAsync<IDossierDaoService>(
                service => service.ModifierAcheteurAsync(DossierSelectionne.RefDossier, acheteur)),
            "L'acheteur a été modifié.");

        await ChargerDetailsAsync(DossierSelectionne.RefDossier);
    }

    [RelayCommand]
    private async Task SupprimerAcheteur(AcheteurDao? acheteurSelectionne)
    {
        if (!PeutModifier)
        {
            return;
        }

        if (DossierSelectionne is null)
        {
            return;
        }

        var acheteurActuel = acheteurSelectionne ?? AcheteurSelectionne?.Acheteur;
        if (acheteurActuel is null)
        {
            return;
        }

        var nom = acheteurActuel.RaisonSociale;
        var id = acheteurActuel.IdAcheteur;
        var confirme = await _dialogues.ConfirmerAsync(
            "Supprimer l'acheteur",
            $"Confirmer la suppression de l'acheteur « {nom} » ?");

        if (!confirme)
        {
            return;
        }

        await ExecuterAsync(
            () => _scopeRunner.ExecuterAsync<IDossierDaoService>(
                service => service.SupprimerAcheteurAsync(DossierSelectionne.RefDossier, id)),
            "L'acheteur a été supprimé.");

        await ChargerDetailsAsync(DossierSelectionne.RefDossier);
    }

    // ---------- Interne ----------

    partial void OnDossierSelectionneChanged(DossierDao? value)
    {
        Candidats.Clear();
        Acheteurs.Clear();
        CandidatSelectionne = null;
        AcheteurSelectionne = null;

        if (!_chargementDetailsSuspendu)
        {
            _ = ChargerDetailsAsync(value?.RefDossier);
        }
    }

    private async Task ChargerDetailsAsync(string? refDossier)
    {
        await _chargementDetails.WaitAsync();
        try
        {
            Candidats.Clear();
            Acheteurs.Clear();
            CandidatSelectionne = null;
            AcheteurSelectionne = null;

            if (refDossier is null)
            {
                return;
            }

            var dossier = await _scopeRunner.ExecuterAsync<IDossierDaoService, DossierDao>(
                service => service.ObtenirAsync(refDossier));

            foreach (var ligne in LignesDao.Numeroter(dossier.Candidats))
            {
                Candidats.Add(ligne);
            }

            foreach (var ligne in LignesDao.Numeroter(dossier.Acheteurs))
            {
                Acheteurs.Add(ligne);
            }

            CandidatSelectionne = Candidats.FirstOrDefault();
            AcheteurSelectionne = Acheteurs.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Erreur = ex.Message;
        }
        finally
        {
            _chargementDetails.Release();
        }
    }

    private async Task<IReadOnlyList<OptionLignePpm>> ChargerLignesPpmAsync()
    {
        var lignes = new List<OptionLignePpm>();
        var entetes = await _scopeRunner.ExecuterAsync<IPpmService, IReadOnlyList<PpmEntete>>(
            service => service.ListerAsync());

        foreach (var entete in entetes)
        {
            var complet = await _scopeRunner.ExecuterAsync<IPpmService, PpmEntete>(
                service => service.ObtenirAsync(entete.NumPPM));

            foreach (var detail in complet.Details)
            {
                lignes.Add(new OptionLignePpm(
                    detail.NumPPMDetail,
                    $"{detail.NumPPMDetail} — {detail.ObjetProjet}"));
            }
        }

        return lignes;
    }

    private void NotifierCompteursDossiers()
    {
        OnPropertyChanged(nameof(NombreDossiers));
        OnPropertyChanged(nameof(NombreCandidatsTousDossiers));
        OnPropertyChanged(nameof(NombreAcheteursTousDossiers));
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

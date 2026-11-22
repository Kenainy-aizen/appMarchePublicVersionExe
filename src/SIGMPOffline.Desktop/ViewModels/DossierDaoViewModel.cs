using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIGMPOffline.Desktop.Services;
using SIGMPOffline.Desktop.ViewModels.Dialogs;
using SIGMPOffline.Domain.Entities;
using SIGMPOffline.Services.Abstractions;

namespace SIGMPOffline.Desktop.ViewModels;

/// <summary>Module DAO &amp; Attribution : CRUD des dossiers, candidats et acheteurs.</summary>
public partial class DossierDaoViewModel : ViewModelBase, IPageViewModel
{
    private readonly IDossierDaoService _dao;
    private readonly IPpmService _ppm;
    private readonly IDialogService _dialogues;

    public DossierDaoViewModel(IDossierDaoService dao, IPpmService ppm, IDialogService dialogues)
    {
        _dao = dao ?? throw new ArgumentNullException(nameof(dao));
        _ppm = ppm ?? throw new ArgumentNullException(nameof(ppm));
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
    public string Titre => "DAO & Attribution";

    /// <summary>Dossiers d'appel d'offres.</summary>
    public ObservableCollection<DossierDao> Dossiers { get; } = new();

    /// <summary>Candidats du dossier sélectionné.</summary>
    public ObservableCollection<Candidat> Candidats { get; } = new();

    /// <summary>Acheteurs du dossier sélectionné.</summary>
    public ObservableCollection<AcheteurDao> Acheteurs { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AUnDossier))]
    [NotifyPropertyChangedFor(nameof(TitreDetailDossier))]
    [NotifyPropertyChangedFor(nameof(LignePpmAffichee))]
    [NotifyPropertyChangedFor(nameof(CoutCahierAffiche))]
    [NotifyPropertyChangedFor(nameof(CautionAffichee))]
    private DossierDao? _dossierSelectionne;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AUnCandidat))]
    private Candidat? _candidatSelectionne;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AUnAcheteur))]
    private AcheteurDao? _acheteurSelectionne;

    /// <summary>Vrai quand l'écran « détail d'un dossier » est affiché à la place de la liste.</summary>
    [ObservableProperty] private bool _estEnVueDetail;

    [ObservableProperty] private bool _estOccupe;
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
    public async Task ChargerAsync()
    {
        try
        {
            EstOccupe = true;
            Erreur = null;

            // On garde le dossier sélectionné (s'il existe encore) après un rechargement.
            var refConserve = DossierSelectionne?.RefDossier;

            var dossiers = await _dao.ListerAvecDetailsAsync();
            Dossiers.Clear();
            foreach (var dossier in dossiers)
            {
                Dossiers.Add(dossier);
            }

            DossierSelectionne = refConserve is null
                ? Dossiers.FirstOrDefault()
                : Dossiers.FirstOrDefault(d => d.RefDossier == refConserve) ?? Dossiers.FirstOrDefault();

            if (DossierSelectionne is null)
            {
                // Plus de dossier à afficher : retour à la liste.
                EstEnVueDetail = false;
            }

            NotifierCompteursDossiers();
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
    private void OuvrirDossier(DossierDao? dossierChoisi)
    {
        var dossier = dossierChoisi ?? DossierSelectionne;
        if (dossier is null)
        {
            return;
        }

        Erreur = null;
        Message = null;

        var memeSelection = ReferenceEquals(DossierSelectionne, dossier);
        DossierSelectionne = dossier;
        EstEnVueDetail = true;

        if (memeSelection)
        {
            // Pas de changement de sélection : on relit les détails pour être à jour.
            _ = ChargerDetailsAsync(dossier.RefDossier);
        }
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
            () => _dao.CreerAsync(dossier),
            $"Le dossier « {dossier.RefDossier} » a été créé.");

        await ChargerAsync();
        DossierSelectionne = Dossiers.FirstOrDefault(d => d.RefDossier == dossier.RefDossier);
    }

    [RelayCommand]
    private async Task ModifierDossier(DossierDao? dossierSelectionne)
    {
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
            () => _dao.ModifierAsync(dossier),
            "Le dossier a été modifié.");

        await ChargerAsync();
        DossierSelectionne = Dossiers.FirstOrDefault(d => d.RefDossier == dossier.RefDossier);
    }

    [RelayCommand]
    private async Task SupprimerDossier(DossierDao? dossierSelectionne)
    {
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

        if (await ExecuterAsync(() => _dao.SupprimerAsync(refDossier), "Le dossier a été supprimé."))
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
            () => _dao.AjouterCandidatAsync(DossierSelectionne.RefDossier, candidat),
            "Le candidat a été ajouté.");

        await ChargerDetailsAsync(DossierSelectionne.RefDossier);
    }

    [RelayCommand]
    private async Task ModifierCandidat(Candidat? candidatSelectionne)
    {
        if (DossierSelectionne is null)
        {
            return;
        }

        var candidatActuel = candidatSelectionne ?? CandidatSelectionne;
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
            () => _dao.ModifierCandidatAsync(DossierSelectionne.RefDossier, candidat),
            "Le candidat a été modifié.");

        await ChargerDetailsAsync(DossierSelectionne.RefDossier);
    }

    [RelayCommand]
    private async Task SupprimerCandidat(Candidat? candidatSelectionne)
    {
        if (DossierSelectionne is null)
        {
            return;
        }

        var candidatActuel = candidatSelectionne ?? CandidatSelectionne;
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
            () => _dao.SupprimerCandidatAsync(DossierSelectionne.RefDossier, id),
            "Le candidat a été supprimé.");

        await ChargerDetailsAsync(DossierSelectionne.RefDossier);
    }

    [RelayCommand]
    private async Task DesignerRetenu()
    {
        if (DossierSelectionne is null || CandidatSelectionne is null)
        {
            return;
        }

        var refDossier = DossierSelectionne.RefDossier;
        var id = CandidatSelectionne.IdCandidat;
        await ExecuterAsync(
            () => _dao.DesignerRetenuAsync(refDossier, id),
            "Le candidat retenu a été mis à jour.");

        await ChargerDetailsAsync(refDossier);
    }

    // ---------- Acheteurs ----------

    [RelayCommand]
    private async Task AjouterAcheteur()
    {
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
            () => _dao.AjouterAcheteurAsync(DossierSelectionne.RefDossier, acheteur),
            "L'acheteur a été ajouté.");

        await ChargerDetailsAsync(DossierSelectionne.RefDossier);
    }

    [RelayCommand]
    private async Task ModifierAcheteur(AcheteurDao? acheteurSelectionne)
    {
        if (DossierSelectionne is null)
        {
            return;
        }

        var acheteurActuel = acheteurSelectionne ?? AcheteurSelectionne;
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
            () => _dao.ModifierAcheteurAsync(DossierSelectionne.RefDossier, acheteur),
            "L'acheteur a été modifié.");

        await ChargerDetailsAsync(DossierSelectionne.RefDossier);
    }

    [RelayCommand]
    private async Task SupprimerAcheteur(AcheteurDao? acheteurSelectionne)
    {
        if (DossierSelectionne is null)
        {
            return;
        }

        var acheteurActuel = acheteurSelectionne ?? AcheteurSelectionne;
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
            () => _dao.SupprimerAcheteurAsync(DossierSelectionne.RefDossier, id),
            "L'acheteur a été supprimé.");

        await ChargerDetailsAsync(DossierSelectionne.RefDossier);
    }

    // ---------- Interne ----------

    partial void OnDossierSelectionneChanged(DossierDao? value)
        => _ = ChargerDetailsAsync(value?.RefDossier);

    private async Task ChargerDetailsAsync(string? refDossier)
    {
        Candidats.Clear();
        Acheteurs.Clear();
        CandidatSelectionne = null;
        AcheteurSelectionne = null;

        if (refDossier is null)
        {
            return;
        }

        try
        {
            var dossier = await _dao.ObtenirAsync(refDossier);

            foreach (var candidat in dossier.Candidats)
            {
                Candidats.Add(candidat);
            }

            foreach (var acheteur in dossier.Acheteurs)
            {
                Acheteurs.Add(acheteur);
            }

            CandidatSelectionne = Candidats.FirstOrDefault();
            AcheteurSelectionne = Acheteurs.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Erreur = ex.Message;
        }
    }

    private async Task<IReadOnlyList<OptionLignePpm>> ChargerLignesPpmAsync()
    {
        var lignes = new List<OptionLignePpm>();
        foreach (var entete in await _ppm.ListerAsync())
        {
            var complet = await _ppm.ObtenirAsync(entete.NumPPM);
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

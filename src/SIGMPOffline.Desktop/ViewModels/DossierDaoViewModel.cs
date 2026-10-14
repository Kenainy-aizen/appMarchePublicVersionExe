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
    private DossierDao? _dossierSelectionne;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AUnCandidat))]
    private Candidat? _candidatSelectionne;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AUnAcheteur))]
    private AcheteurDao? _acheteurSelectionne;

    [ObservableProperty] private bool _estOccupe;
    [ObservableProperty] private string? _erreur;
    [ObservableProperty] private string? _message;

    /// <summary>Vrai si un dossier est sélectionné.</summary>
    public bool AUnDossier => DossierSelectionne is not null;

    /// <summary>Vrai si un candidat est sélectionné.</summary>
    public bool AUnCandidat => CandidatSelectionne is not null;

    /// <summary>Vrai si un acheteur est sélectionné.</summary>
    public bool AUnAcheteur => AcheteurSelectionne is not null;

    /// <inheritdoc />
    [RelayCommand]
    public async Task ChargerAsync()
    {
        try
        {
            EstOccupe = true;
            Erreur = null;

            var dossiers = await _dao.ListerAsync();
            Dossiers.Clear();
            foreach (var dossier in dossiers)
            {
                Dossiers.Add(dossier);
            }

            DossierSelectionne = Dossiers.FirstOrDefault();
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
    private async Task ModifierDossier()
    {
        if (DossierSelectionne is null)
        {
            return;
        }

        var lignes = await ChargerLignesPpmAsync();
        var dialogue = new DossierDaoDialogViewModel(lignes, DossierSelectionne);
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
    private async Task SupprimerDossier()
    {
        if (DossierSelectionne is null)
        {
            return;
        }

        var refDossier = DossierSelectionne.RefDossier;
        var confirme = await _dialogues.ConfirmerAsync(
            "Supprimer le dossier",
            $"Confirmer la suppression du dossier « {refDossier} » (candidats et acheteurs inclus) ?");

        if (!confirme)
        {
            return;
        }

        await ExecuterAsync(() => _dao.SupprimerAsync(refDossier), "Le dossier a été supprimé.");
        await ChargerAsync();
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
    private async Task ModifierCandidat()
    {
        if (DossierSelectionne is null || CandidatSelectionne is null)
        {
            return;
        }

        var dialogue = new CandidatDialogViewModel(CandidatSelectionne);
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
    private async Task SupprimerCandidat()
    {
        if (DossierSelectionne is null || CandidatSelectionne is null)
        {
            return;
        }

        var nom = CandidatSelectionne.NomEntreprise;
        var id = CandidatSelectionne.IdCandidat;
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
    private async Task ModifierAcheteur()
    {
        if (DossierSelectionne is null || AcheteurSelectionne is null)
        {
            return;
        }

        var dialogue = new AcheteurDaoDialogViewModel(AcheteurSelectionne);
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
    private async Task SupprimerAcheteur()
    {
        if (DossierSelectionne is null || AcheteurSelectionne is null)
        {
            return;
        }

        var nom = AcheteurSelectionne.RaisonSociale;
        var id = AcheteurSelectionne.IdAcheteur;
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

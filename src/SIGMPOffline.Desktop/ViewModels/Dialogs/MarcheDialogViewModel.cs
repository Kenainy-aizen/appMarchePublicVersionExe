using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Desktop.ViewModels.Dialogs;

/// <summary>Formulaire de création / modification d'un marché (en-tête du contrat).</summary>
public partial class MarcheDialogViewModel : DialogViewModelBase
{
    private static readonly OptionCandidat Aucun = new(0, "(aucun titulaire)");

    private readonly bool _creation;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<OptionCandidat>> _candidatsParDossier;

    /// <summary>
    /// Crée le formulaire. <paramref name="existant"/> vaut <c>null</c> pour une création.
    /// <paramref name="candidatsParDossier"/> fournit les titulaires possibles par dossier.
    /// </summary>
    public MarcheDialogViewModel(
        IReadOnlyList<OptionDossier> dossiers,
        IReadOnlyDictionary<string, IReadOnlyList<OptionCandidat>> candidatsParDossier,
        Marche? existant = null)
    {
        Dossiers = dossiers ?? throw new ArgumentNullException(nameof(dossiers));
        _candidatsParDossier = candidatsParDossier ?? throw new ArgumentNullException(nameof(candidatsParDossier));
        _creation = existant is null;

        _refMarche = existant?.RefMarche ?? string.Empty;
        _objetMarche = existant?.ObjetMarche ?? string.Empty;
        _montantHT = existant?.MontantHT ?? 0m;
        _montantTTC = existant?.MontantTTC ?? 0m;
        _delaiExecutionJours = existant?.DelaiExecutionJours ?? 0;

        _dossierSelectionne = Dossiers.FirstOrDefault(d => d.RefDossier == existant?.RefDossier)
            ?? Dossiers.FirstOrDefault();

        RafraichirCandidats(existant?.IdCandidatTitulaire, autoRetenu: existant is null);
    }

    /// <inheritdoc />
    public override string Titre => _creation ? "Nouveau marché" : "Modifier le marché";

    /// <summary>La référence n'est modifiable qu'à la création (elle sert de clé).</summary>
    public bool RefMarcheModifiable => _creation;

    /// <summary>Le dossier d'origine n'est plus modifiable après la création.</summary>
    public bool DossierModifiable => _creation;

    /// <summary>Dossiers disponibles proposés à la création.</summary>
    public IReadOnlyList<OptionDossier> Dossiers { get; }

    /// <summary>Titulaires possibles pour le dossier sélectionné.</summary>
    public ObservableCollection<OptionCandidat> Candidats { get; } = new();

    [ObservableProperty] private string _refMarche;
    [ObservableProperty] private string _objetMarche;
    [ObservableProperty] private decimal _montantHT;
    [ObservableProperty] private decimal _montantTTC;
    [ObservableProperty] private int _delaiExecutionJours;
    [ObservableProperty] private OptionDossier? _dossierSelectionne;
    [ObservableProperty] private OptionCandidat? _titulaireSelectionne;
    [ObservableProperty] private string? _erreur;

    partial void OnDossierSelectionneChanged(OptionDossier? value)
        => RafraichirCandidats(null, autoRetenu: true);

    /// <summary>Construit l'entité à partir de la saisie.</summary>
    public Marche VersEntite() => new()
    {
        RefMarche = RefMarche.Trim(),
        RefDossier = DossierSelectionne?.RefDossier ?? string.Empty,
        ObjetMarche = ObjetMarche.Trim(),
        MontantHT = MontantHT,
        MontantTTC = MontantTTC,
        DelaiExecutionJours = DelaiExecutionJours,
        IdCandidatTitulaire = TitulaireSelectionne?.IdCandidat is int id and > 0 ? id : null
    };

    private void RafraichirCandidats(int? idTitulaire, bool autoRetenu)
    {
        Candidats.Clear();
        Candidats.Add(Aucun);

        if (DossierSelectionne is not null
            && _candidatsParDossier.TryGetValue(DossierSelectionne.RefDossier, out var liste))
        {
            foreach (var candidat in liste)
            {
                Candidats.Add(candidat);
            }
        }

        var cible = idTitulaire is int id
            ? Candidats.FirstOrDefault(c => c.IdCandidat == id)
            : autoRetenu ? Candidats.FirstOrDefault(c => c.EstRetenu) : null;

        TitulaireSelectionne = cible ?? Candidats[0];
    }

    /// <inheritdoc />
    protected override bool ValiderSaisie()
    {
        if (string.IsNullOrWhiteSpace(RefMarche))
        {
            Erreur = "La référence du marché est obligatoire.";
            return false;
        }

        if (DossierSelectionne is null)
        {
            Erreur = "Le marché doit être rattaché à un dossier DAO.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(ObjetMarche))
        {
            Erreur = "L'objet du marché est obligatoire.";
            return false;
        }

        if (MontantHT < 0 || MontantTTC < 0)
        {
            Erreur = "Les montants ne peuvent pas être négatifs.";
            return false;
        }

        if (DelaiExecutionJours < 0)
        {
            Erreur = "Le délai d'exécution ne peut pas être négatif.";
            return false;
        }

        Erreur = null;
        return true;
    }
}

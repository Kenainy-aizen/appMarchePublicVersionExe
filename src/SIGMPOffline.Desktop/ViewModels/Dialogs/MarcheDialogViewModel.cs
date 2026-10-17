using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Desktop.ViewModels.Dialogs;

/// <summary>Formulaire de création / modification d'un marché (en-tête du contrat).</summary>
public partial class MarcheDialogViewModel : DialogViewModelBase
{
    private static readonly OptionTitulaire Aucun = new(string.Empty, "(aucun titulaire)");

    private readonly bool _creation;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<OptionTitulaire>> _titulairesParDossier;

    /// <summary>
    /// Crée le formulaire. <paramref name="existant"/> vaut <c>null</c> pour une création.
    /// <paramref name="titulairesParDossier"/> fournit les acheteurs titulaires possibles par dossier.
    /// </summary>
    public MarcheDialogViewModel(
        IReadOnlyList<OptionDossier> dossiers,
        IReadOnlyDictionary<string, IReadOnlyList<OptionTitulaire>> titulairesParDossier,
        Marche? existant = null)
    {
        Dossiers = dossiers ?? throw new ArgumentNullException(nameof(dossiers));
        _titulairesParDossier = titulairesParDossier ?? throw new ArgumentNullException(nameof(titulairesParDossier));
        _creation = existant is null;

        _refMarche = existant?.RefMarche ?? string.Empty;
        _objetMarche = existant?.ObjetMarche ?? string.Empty;
        _montantHT = existant?.MontantHT ?? 0m;
        _montantTTC = existant?.MontantTTC ?? 0m;
        _delaiExecutionJours = existant?.DelaiExecutionJours ?? 0;

        _dossierSelectionne = Dossiers.FirstOrDefault(d => d.RefDossier == existant?.RefDossier)
            ?? Dossiers.FirstOrDefault();

        RafraichirTitulaires(existant?.NumStatTitulaire);
    }

    /// <inheritdoc />
    public override string Titre => _creation ? "Nouveau marché" : "Modifier le marché";

    /// <summary>La référence n'est modifiable qu'à la création (elle sert de clé).</summary>
    public bool RefMarcheModifiable => _creation;

    /// <summary>Le dossier d'origine n'est plus modifiable après la création.</summary>
    public bool DossierModifiable => _creation;

    /// <summary>Dossiers disponibles proposés à la création.</summary>
    public IReadOnlyList<OptionDossier> Dossiers { get; }

    /// <summary>Acheteurs titulaires possibles pour le dossier sélectionné.</summary>
    public ObservableCollection<OptionTitulaire> Titulaires { get; } = new();

    [ObservableProperty] private string _refMarche;
    [ObservableProperty] private string _objetMarche;
    [ObservableProperty] private decimal _montantHT;
    [ObservableProperty] private decimal _montantTTC;
    [ObservableProperty] private int _delaiExecutionJours;
    [ObservableProperty] private OptionDossier? _dossierSelectionne;
    [ObservableProperty] private OptionTitulaire? _titulaireSelectionne;
    [ObservableProperty] private string? _erreur;

    partial void OnDossierSelectionneChanged(OptionDossier? value)
        => RafraichirTitulaires(null);

    /// <summary>Construit l'entité à partir de la saisie.</summary>
    public Marche VersEntite() => new()
    {
        RefMarche = RefMarche.Trim(),
        RefDossier = DossierSelectionne?.RefDossier ?? string.Empty,
        ObjetMarche = ObjetMarche.Trim(),
        MontantHT = MontantHT,
        MontantTTC = MontantTTC,
        DelaiExecutionJours = DelaiExecutionJours,
        NumStatTitulaire = string.IsNullOrWhiteSpace(TitulaireSelectionne?.NumStat)
            ? null
            : TitulaireSelectionne.NumStat
    };

    private void RafraichirTitulaires(string? numStatTitulaire)
    {
        Titulaires.Clear();
        Titulaires.Add(Aucun);

        if (DossierSelectionne is not null
            && _titulairesParDossier.TryGetValue(DossierSelectionne.RefDossier, out var liste))
        {
            foreach (var titulaire in liste)
            {
                Titulaires.Add(titulaire);
            }
        }

        var cible = !string.IsNullOrWhiteSpace(numStatTitulaire)
            ? Titulaires.FirstOrDefault(t =>
                string.Equals(t.NumStat, numStatTitulaire, StringComparison.OrdinalIgnoreCase))
            : null;

        TitulaireSelectionne = cible ?? Titulaires[0];
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

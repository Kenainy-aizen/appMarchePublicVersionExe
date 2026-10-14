using CommunityToolkit.Mvvm.ComponentModel;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Desktop.ViewModels.Dialogs;

/// <summary>Formulaire de création / modification d'un candidat (soumissionnaire).</summary>
public partial class CandidatDialogViewModel : DialogViewModelBase
{
    private readonly bool _creation;

    /// <summary>Crée le formulaire. <paramref name="existant"/> vaut <c>null</c> pour une création.</summary>
    public CandidatDialogViewModel(Candidat? existant = null)
    {
        _creation = existant is null;

        IdCandidat = existant?.IdCandidat ?? 0;
        _nomEntreprise = existant?.NomEntreprise ?? string.Empty;
        _numStat = existant?.NumStat ?? string.Empty;
        _numNIF = existant?.NumNIF ?? string.Empty;
        _montantOffre = existant?.MontantOffre ?? 0m;
    }

    /// <inheritdoc />
    public override string Titre => _creation ? "Nouveau candidat" : "Modifier le candidat";

    /// <summary>Identifiant (0 pour une création).</summary>
    public int IdCandidat { get; }

    [ObservableProperty] private string _nomEntreprise;
    [ObservableProperty] private string _numStat;
    [ObservableProperty] private string _numNIF;
    [ObservableProperty] private decimal _montantOffre;
    [ObservableProperty] private string? _erreur;

    /// <summary>Construit l'entité à partir de la saisie.</summary>
    public Candidat VersEntite() => new()
    {
        IdCandidat = IdCandidat,
        NomEntreprise = NomEntreprise.Trim(),
        NumStat = NumStat.Trim(),
        NumNIF = NumNIF.Trim(),
        MontantOffre = MontantOffre
    };

    /// <inheritdoc />
    protected override bool ValiderSaisie()
    {
        if (string.IsNullOrWhiteSpace(NomEntreprise))
        {
            Erreur = "Le nom de l'entreprise est obligatoire.";
            return false;
        }

        if (MontantOffre < 0)
        {
            Erreur = "Le montant de l'offre ne peut pas être négatif.";
            return false;
        }

        Erreur = null;
        return true;
    }
}

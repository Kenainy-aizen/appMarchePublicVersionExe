using CommunityToolkit.Mvvm.ComponentModel;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Desktop.ViewModels.Dialogs;

/// <summary>Formulaire de création / modification d'un acheteur de DAO.</summary>
public partial class AcheteurDaoDialogViewModel : DialogViewModelBase
{
    private readonly bool _creation;

    /// <summary>Crée le formulaire. <paramref name="existant"/> vaut <c>null</c> pour une création.</summary>
    public AcheteurDaoDialogViewModel(AcheteurDao? existant = null)
    {
        _creation = existant is null;

        IdAcheteur = existant?.IdAcheteur ?? 0;
        _raisonSociale = existant?.RaisonSociale ?? string.Empty;
        _numStat = existant?.NumStat ?? string.Empty;
        _numNIF = existant?.NumNIF ?? string.Empty;
        _adresse = existant?.Adresse;
        _dateAchat = new DateTimeOffset(
            existant is not null && existant.DateAchat != default ? existant.DateAchat : DateTime.Today);
    }

    /// <inheritdoc />
    public override string Titre => _creation ? "Nouvel acheteur" : "Modifier l'acheteur";

    /// <summary>Identifiant (0 pour une création).</summary>
    public int IdAcheteur { get; }

    /// <summary>
    /// Le numéro statistique identifie l'acheteur (cible de la clé étrangère
    /// <c>Marche.NumStatTitulaire</c>) : il n'est modifiable qu'à la création.
    /// </summary>
    public bool NumStatModifiable => _creation;

    [ObservableProperty] private string _raisonSociale;
    [ObservableProperty] private string _numStat;
    [ObservableProperty] private string _numNIF;
    [ObservableProperty] private string? _adresse;
    [ObservableProperty] private DateTimeOffset? _dateAchat;
    [ObservableProperty] private string? _erreur;

    /// <summary>Construit l'entité à partir de la saisie.</summary>
    public AcheteurDao VersEntite() => new()
    {
        IdAcheteur = IdAcheteur,
        RaisonSociale = RaisonSociale.Trim(),
        NumStat = NumStat.Trim(),
        NumNIF = NumNIF.Trim(),
        Adresse = string.IsNullOrWhiteSpace(Adresse) ? null : Adresse.Trim(),
        DateAchat = DateAchat?.DateTime ?? DateTime.Today
    };

    /// <inheritdoc />
    protected override bool ValiderSaisie()
    {
        if (string.IsNullOrWhiteSpace(RaisonSociale))
        {
            Erreur = "La raison sociale est obligatoire.";
            return false;
        }

        Erreur = null;
        return true;
    }
}

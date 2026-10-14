using CommunityToolkit.Mvvm.ComponentModel;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Desktop.ViewModels.Dialogs;

/// <summary>Formulaire de création / modification d'un en-tête de PPM.</summary>
public partial class PpmEnteteDialogViewModel : DialogViewModelBase
{
    private readonly bool _creation;

    /// <summary>Crée le formulaire. <paramref name="existant"/> vaut <c>null</c> pour une création.</summary>
    public PpmEnteteDialogViewModel(PpmEntete? existant = null)
    {
        _creation = existant is null;

        _numPPM = existant?.NumPPM ?? string.Empty;
        _anneeExercice = existant?.AnneeExercice ?? DateTime.Today.Year;
        _autoriteContractante = existant?.AutoriteContractante ?? string.Empty;

        // Une date d'élaboration non renseignée (valeur par défaut) est remplacée par la date du jour,
        // ce qui évite aussi un dépassement de plage de DateTimeOffset en fuseau local positif.
        var dateElaboration = existant is not null && existant.DateElaboration != default
            ? existant.DateElaboration
            : DateOnly.FromDateTime(DateTime.Today);

        _dateElaboration = new DateTimeOffset(dateElaboration.ToDateTime(TimeOnly.MinValue));
    }

    /// <inheritdoc />
    public override string Titre => _creation ? "Nouveau PPM" : "Modifier le PPM";

    /// <summary>Le numéro n'est modifiable qu'à la création (il sert de clé).</summary>
    public bool NumPpmModifiable => _creation;

    [ObservableProperty] private string _numPPM;
    [ObservableProperty] private decimal _anneeExercice;
    [ObservableProperty] private string _autoriteContractante;
    [ObservableProperty] private DateTimeOffset? _dateElaboration;
    [ObservableProperty] private string? _erreur;

    /// <summary>Construit l'entité à partir de la saisie.</summary>
    public PpmEntete VersEntite() => new()
    {
        NumPPM = NumPPM.Trim(),
        AnneeExercice = (int)AnneeExercice,
        AutoriteContractante = AutoriteContractante.Trim(),
        DateElaboration = DateOnly.FromDateTime(DateElaboration?.DateTime ?? DateTime.Today)
    };

    /// <inheritdoc />
    protected override bool ValiderSaisie()
    {
        if (string.IsNullOrWhiteSpace(NumPPM))
        {
            Erreur = "Le numéro du PPM est obligatoire.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(AutoriteContractante))
        {
            Erreur = "L'autorité contractante est obligatoire.";
            return false;
        }

        if (AnneeExercice < 2000 || AnneeExercice > 2100)
        {
            Erreur = "L'exercice budgétaire doit être compris entre 2000 et 2100.";
            return false;
        }

        Erreur = null;
        return true;
    }
}

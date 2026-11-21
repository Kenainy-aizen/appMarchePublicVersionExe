using CommunityToolkit.Mvvm.ComponentModel;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Desktop.ViewModels.Dialogs;

/// <summary>Formulaire de création / modification d'un en-tête de PPM.</summary>
public partial class PpmEnteteDialogViewModel : DialogViewModelBase
{
    private readonly bool _creation;
    private readonly string? _numDerniereMAJ;
    private readonly DateTime? _dateDerniereMAJInitiale;

    /// <summary>Crée le formulaire. <paramref name="existant"/> vaut <c>null</c> pour une création.</summary>
    public PpmEnteteDialogViewModel(PpmEntete? existant = null)
    {
        _creation = existant is null;

        _numPPM = existant?.NumPPM ?? string.Empty;
        _anneeExercice = existant?.AnneeExercice ?? DateTime.Today.Year;
        _autoriteContractante = existant?.AutoriteContractante ?? string.Empty;
        _numDerniereMAJ = existant?.NumDerniereMAJ;
        _dateDerniereMAJInitiale = existant?.DateDerniereMAJ?.Date;

        // La date de mise à jour est saisissable lors d'une modification ; à la création,
        // elle est initialisée à la date du jour (champ masqué).
        _dateDerniereMAJ = new DateTimeOffset(
            existant?.DateDerniereMAJ ?? DateTime.Today);

        // Une date d'élaboration non renseignée (valeur par défaut) est remplacée par la date du jour,
        // ce qui évite aussi un dépassement de plage de DateTimeOffset en fuseau local positif.
        var dateElaboration = existant is not null && existant.DateElaboration != default
            ? existant.DateElaboration
            : DateOnly.FromDateTime(DateTime.Today);

        _dateElaboration = new DateTimeOffset(dateElaboration.ToDateTime(TimeOnly.MinValue));
    }

    /// <inheritdoc />
    public override string Titre => _creation ? "Nouveau PPM" : "Modifier le PPM";

    /// <summary>Le numéro PPM reste saisi manuellement et est immuable après création.</summary>
    public bool NumPpmModifiable => _creation;

    /// <summary>L'exercice reste modifiable après création.</summary>
    public bool AnneeModifiable => true;

    /// <summary>La date d'élaboration reste modifiable après création.</summary>
    public bool DateElaborationModifiable => true;

    /// <summary>Le champ date de mise à jour n'apparaît qu'en modification.</summary>
    public bool AfficherDateMiseAJour => !_creation;

    /// <summary>
    /// Numéro de mise à jour affiché. Il s'incrémente automatiquement dès que
    /// la date de mise à jour saisie diffère de la date d'origine.
    /// </summary>
    public string NumeroMiseAJourAffiche
    {
        get
        {
            if (_creation)
            {
                return "—";
            }

            var dateActuelle = DateDerniereMAJ?.Date;
            var dateChangee = dateActuelle is not null
                && (_dateDerniereMAJInitiale is null
                    || dateActuelle.Value != _dateDerniereMAJInitiale.Value);

            if (!dateChangee)
            {
                return string.IsNullOrWhiteSpace(_numDerniereMAJ) ? "—" : _numDerniereMAJ!;
            }

            return IncrementerNumero(_numDerniereMAJ);
        }
    }

    [ObservableProperty] private string _numPPM;
    [ObservableProperty] private decimal _anneeExercice;
    [ObservableProperty] private string _autoriteContractante;
    [ObservableProperty] private DateTimeOffset? _dateElaboration;
    [ObservableProperty] private DateTimeOffset? _dateDerniereMAJ;
    [ObservableProperty] private string? _erreur;

    /// <summary>Construit l'entité à partir de la saisie.</summary>
    public PpmEntete VersEntite() => new()
    {
        NumPPM = NumPPM.Trim(),
        AnneeExercice = (int)AnneeExercice,
        AutoriteContractante = AutoriteContractante.Trim(),
        DateElaboration = DateOnly.FromDateTime(DateElaboration?.DateTime ?? DateTime.Today),
        NumDerniereMAJ = NumeroMiseAJourAffiche == "—" ? null : NumeroMiseAJourAffiche,
        DateDerniereMAJ = DateDerniereMAJ?.Date ?? DateTime.Today
    };

    partial void OnDateDerniereMAJChanged(DateTimeOffset? value)
        => OnPropertyChanged(nameof(NumeroMiseAJourAffiche));

    /// <summary>Incrémente un numéro de mise à jour (« MAJ-02 » → « 3 », « 1 » → « 2 »).</summary>
    private static string IncrementerNumero(string? numero)
    {
        var chiffres = new string((numero ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
        var valeur = int.TryParse(chiffres, out var nombre) ? nombre : 0;
        return (valeur + 1).ToString();
    }

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

        if (DateElaboration is null)
        {
            Erreur = "La date d'élaboration est obligatoire.";
            return false;
        }

        Erreur = null;
        return true;
    }
}

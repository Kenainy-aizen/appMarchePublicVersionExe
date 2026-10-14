using CommunityToolkit.Mvvm.ComponentModel;

namespace SIGMPOffline.Desktop.ViewModels.Dialogs;

/// <summary>
/// Formulaire modal générique de saisie d'une date (OS, réceptions, clôture).
/// Le titre et le libellé du champ sont paramétrés par l'appelant.
/// </summary>
public partial class DateDialogViewModel : DialogViewModelBase
{
    /// <summary>Crée le formulaire.</summary>
    /// <param name="titre">Titre de la fenêtre.</param>
    /// <param name="libelle">Libellé affiché devant le sélecteur de date.</param>
    /// <param name="dateInitiale">Date pré-remplie (aujourd'hui par défaut).</param>
    public DateDialogViewModel(string titre, string libelle, DateOnly? dateInitiale = null)
    {
        _titre = titre ?? throw new ArgumentNullException(nameof(titre));
        Libelle = libelle ?? throw new ArgumentNullException(nameof(libelle));
        _date = dateInitiale is DateOnly date
            ? new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue))
            : new DateTimeOffset(DateTime.Today);
    }

    private readonly string _titre;

    /// <inheritdoc />
    public override string Titre => _titre;

    /// <summary>Libellé affiché devant le champ.</summary>
    public string Libelle { get; }

    [ObservableProperty] private DateTimeOffset? _date;
    [ObservableProperty] private string? _erreur;

    /// <summary>Retourne la date saisie.</summary>
    public DateOnly VersDate() => DateOnly.FromDateTime((Date ?? DateTimeOffset.Now).DateTime);

    /// <inheritdoc />
    protected override bool ValiderSaisie()
    {
        if (Date is null)
        {
            Erreur = "La date est obligatoire.";
            return false;
        }

        Erreur = null;
        return true;
    }
}

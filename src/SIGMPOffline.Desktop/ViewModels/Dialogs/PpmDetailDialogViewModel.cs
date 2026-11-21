using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIGMPOffline.Domain.Entities;
using SIGMPOffline.Domain.Enums;

namespace SIGMPOffline.Desktop.ViewModels.Dialogs;

/// <summary>Formulaire de création / modification d'une ligne de détail d'un PPM.</summary>
public partial class PpmDetailDialogViewModel : DialogViewModelBase
{
    private readonly bool _creation;

    /// <summary>Crée le formulaire. <paramref name="existant"/> vaut <c>null</c> pour une création.</summary>
    public PpmDetailDialogViewModel(PpmDetail? existant = null)
    {
        _creation = existant is null;

        _numPPMDetail = existant?.NumPPMDetail ?? string.Empty;
        _objetProjet = existant?.ObjetProjet ?? string.Empty;
        _montantEstimatif = existant?.MontantEstimatif ?? 0m;
        _sourceFinancement = existant?.SourceFinancement;
        _comptes = existant?.Comptes;
        _responsableProcedure = existant?.ResponsableProcedure;
        _dateLancement = VersDateTimeOffset(existant?.DateLancementPrevue);
        _dateOuverturePlis = VersDateTimeOffset(existant?.DateOuverturePlisPrevue);
        _dateAttribution = VersDateTimeOffset(existant?.DateAttributionPrevue);
        _natureSelectionnee = OptionsEnums.Nature(existant?.NatureMarche ?? NatureMarche.Travaux);
        _modeSelectionne = OptionsEnums.Mode(existant?.ModePassation ?? ModePassation.AppelOffresOuvert);

        Beneficiaires.CollectionChanged += (_, _) => ActualiserMontant();

        if (existant is not null)
        {
            foreach (var beneficiaire in existant.Beneficiaires)
            {
                Beneficiaires.Add(NouvelleLigneBeneficiaire(beneficiaire.Service, beneficiaire.Montant));
            }
        }
    }

    /// <inheritdoc />
    public override string Titre => _creation ? "Nouvelle ligne de PPM" : "Modifier la ligne de PPM";

    /// <summary>Le numéro de ligne reste saisi manuellement et immuable après création.</summary>
    public bool NumDetailModifiable => _creation;

    /// <summary>Convertit une saisie avec espaces en montant entier Ariary.</summary>
    public static decimal? ParserMontantEntier(string? saisie)
    {
        if (string.IsNullOrWhiteSpace(saisie))
        {
            return 0m;
        }

        var normalise = saisie.Replace(" ", string.Empty).Replace(" ", string.Empty);
        return decimal.TryParse(normalise, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.GetCultureInfo("fr-FR"), out var montant)
            ? montant
            : null;
    }

    /// <summary>Natures de marché proposées.</summary>
    public IReadOnlyList<OptionEnum<NatureMarche>> Natures { get; } = OptionsEnums.Natures;

    /// <summary>Modes de passation proposés.</summary>
    public IReadOnlyList<OptionEnum<ModePassation>> Modes { get; } = OptionsEnums.ModesPassation;

    /// <summary>Services bénéficiaires et leurs montants.</summary>
    public ObservableCollection<BeneficiaireEditionViewModel> Beneficiaires { get; } = new();

    /// <summary>Le montant se saisit à la main seulement s'il n'y a aucun service bénéficiaire ; sinon il en est la somme.</summary>
    public bool MontantSaisissable => Beneficiaires.Count == 0;

    /// <summary>Vrai si au moins un service bénéficiaire est saisi.</summary>
    public bool ADesBeneficiaires => Beneficiaires.Count > 0;

    [ObservableProperty] private string _numPPMDetail;
    [ObservableProperty] private string _objetProjet;
    [ObservableProperty] private decimal _montantEstimatif;
    [ObservableProperty] private string? _sourceFinancement;
    [ObservableProperty] private string? _comptes;
    [ObservableProperty] private string? _responsableProcedure;
    [ObservableProperty] private DateTimeOffset? _dateLancement;
    [ObservableProperty] private DateTimeOffset? _dateOuverturePlis;
    [ObservableProperty] private DateTimeOffset? _dateAttribution;
    [ObservableProperty] private OptionEnum<NatureMarche> _natureSelectionnee;
    [ObservableProperty] private OptionEnum<ModePassation> _modeSelectionne;
    [ObservableProperty] private string? _erreur;

    [RelayCommand]
    private void AjouterBeneficiaire()
        => Beneficiaires.Add(NouvelleLigneBeneficiaire(string.Empty, 0m));

    [RelayCommand]
    private void EffacerDates()
    {
        DateLancement = null;
        DateOuverturePlis = null;
        DateAttribution = null;
    }

    /// <summary>Construit l'entité à partir de la saisie.</summary>
    public PpmDetail VersEntite()
    {
        var beneficiaires = Beneficiaires
            .Select(b => new PpmBeneficiaire { Service = b.Service.Trim(), Montant = b.Montant })
            .ToList();

        return new PpmDetail
        {
            NumPPMDetail = NumPPMDetail.Trim(),
            ObjetProjet = ObjetProjet.Trim(),
            MontantEstimatif = beneficiaires.Count > 0 ? beneficiaires.Sum(b => b.Montant) : MontantEstimatif,
            SourceFinancement = Nettoyer(SourceFinancement),
            Comptes = Nettoyer(Comptes),
            ResponsableProcedure = Nettoyer(ResponsableProcedure),
            DateLancementPrevue = VersDateOnly(DateLancement),
            DateOuverturePlisPrevue = VersDateOnly(DateOuverturePlis),
            DateAttributionPrevue = VersDateOnly(DateAttribution),
            NatureMarche = NatureSelectionnee.Valeur,
            ModePassation = ModeSelectionne.Valeur,
            Beneficiaires = beneficiaires
        };
    }

    /// <inheritdoc />
    protected override bool ValiderSaisie()
    {
        if (string.IsNullOrWhiteSpace(NumPPMDetail))
        {
            Erreur = "Le numéro de la ligne est obligatoire.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(ObjetProjet))
        {
            Erreur = "L'objet du projet est obligatoire.";
            return false;
        }

        if (NatureSelectionnee is null)
        {
            Erreur = "La nature du marché est obligatoire.";
            return false;
        }

        if (MontantEstimatif < 0)
        {
            Erreur = "Le montant estimatif ne peut pas être négatif.";
            return false;
        }

        if (MontantEstimatif != decimal.Truncate(MontantEstimatif))
        {
            Erreur = "Le montant estimatif doit être un nombre entier en Ariary.";
            return false;
        }

        DateOnly? precedente = null;
        foreach (var date in new[] { VersDateOnly(DateLancement), VersDateOnly(DateOuverturePlis), VersDateOnly(DateAttribution) })
        {
            if (date is null)
            {
                continue;
            }

            if (precedente is not null && date < precedente)
            {
                Erreur = "Les dates prévisionnelles doivent suivre l'ordre : lancement, ouverture des plis, attribution.";
                return false;
            }

            precedente = date;
        }

        foreach (var beneficiaire in Beneficiaires)
        {
            if (string.IsNullOrWhiteSpace(beneficiaire.Service))
            {
                Erreur = "Chaque service bénéficiaire doit avoir un nom (ou retirez la ligne vide).";
                return false;
            }

            if (beneficiaire.Montant < 0 || beneficiaire.Montant != decimal.Truncate(beneficiaire.Montant))
            {
                Erreur = "Le montant d'un service bénéficiaire doit être un entier positif en Ariary.";
                return false;
            }
        }

        Erreur = null;
        return true;
    }

    private BeneficiaireEditionViewModel NouvelleLigneBeneficiaire(string service, decimal montant)
    {
        var ligne = new BeneficiaireEditionViewModel(service, montant, retiree => Beneficiaires.Remove(retiree));
        ligne.PropertyChanged += (_, _) => ActualiserMontant();
        return ligne;
    }

    /// <summary>Avec des services bénéficiaires, le montant de la ligne est leur somme.</summary>
    private void ActualiserMontant()
    {
        if (Beneficiaires.Count > 0)
        {
            MontantEstimatif = Beneficiaires.Sum(b => b.Montant);
        }

        OnPropertyChanged(nameof(MontantSaisissable));
        OnPropertyChanged(nameof(ADesBeneficiaires));
    }

    private static string? Nettoyer(string? texte)
        => string.IsNullOrWhiteSpace(texte) ? null : texte.Trim();

    private static DateTimeOffset? VersDateTimeOffset(DateOnly? date)
        => date is DateOnly valeur ? new DateTimeOffset(valeur.ToDateTime(TimeOnly.MinValue)) : null;

    private static DateOnly? VersDateOnly(DateTimeOffset? date)
        => date is DateTimeOffset valeur ? DateOnly.FromDateTime(valeur.DateTime) : null;
}

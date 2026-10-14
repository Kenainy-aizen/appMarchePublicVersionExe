using CommunityToolkit.Mvvm.ComponentModel;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Desktop.ViewModels.Dialogs;

/// <summary>Formulaire de création / modification d'un avenant à un marché.</summary>
public partial class AvenantDialogViewModel : DialogViewModelBase
{
    private readonly bool _creation;

    /// <summary>Crée le formulaire. <paramref name="existant"/> vaut <c>null</c> pour une création.</summary>
    public AvenantDialogViewModel(Avenant? existant = null)
    {
        _creation = existant is null;

        IdAvenant = existant?.IdAvenant ?? 0;
        _numAvenant = existant?.NumAvenant ?? string.Empty;
        _motif = existant?.Motif ?? string.Empty;
        _variationMontant = existant?.VariationMontant ?? 0m;
        _prolongationJours = existant?.ProlongationDelaiJours ?? 0;
        _dateAvenant = existant is not null && existant.DateAvenant != default
            ? new DateTimeOffset(existant.DateAvenant.ToDateTime(TimeOnly.MinValue))
            : new DateTimeOffset(DateTime.Today);
    }

    /// <inheritdoc />
    public override string Titre => _creation ? "Nouvel avenant" : "Modifier l'avenant";

    /// <summary>Identifiant (0 pour une création).</summary>
    public int IdAvenant { get; }

    [ObservableProperty] private string _numAvenant;
    [ObservableProperty] private string? _motif;
    [ObservableProperty] private decimal _variationMontant;
    [ObservableProperty] private int _prolongationJours;
    [ObservableProperty] private DateTimeOffset? _dateAvenant;
    [ObservableProperty] private string? _erreur;

    /// <summary>Construit l'entité à partir de la saisie.</summary>
    public Avenant VersEntite() => new()
    {
        IdAvenant = IdAvenant,
        NumAvenant = NumAvenant.Trim(),
        Motif = string.IsNullOrWhiteSpace(Motif) ? null : Motif.Trim(),
        VariationMontant = VariationMontant,
        ProlongationDelaiJours = ProlongationJours,
        DateAvenant = DateAvenant is DateTimeOffset date
            ? DateOnly.FromDateTime(date.DateTime)
            : DateOnly.FromDateTime(DateTime.Today)
    };

    /// <inheritdoc />
    protected override bool ValiderSaisie()
    {
        if (string.IsNullOrWhiteSpace(NumAvenant))
        {
            Erreur = "Le numéro de l'avenant est obligatoire.";
            return false;
        }

        if (DateAvenant is null)
        {
            Erreur = "La date de l'avenant est obligatoire.";
            return false;
        }

        Erreur = null;
        return true;
    }
}

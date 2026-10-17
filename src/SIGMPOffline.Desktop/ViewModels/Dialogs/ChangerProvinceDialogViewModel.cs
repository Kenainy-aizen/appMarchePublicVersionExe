using CommunityToolkit.Mvvm.ComponentModel;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Desktop.ViewModels.Dialogs;

/// <summary>
/// Formulaire de bascule vers une autre province : sélection de la province
/// cible et saisie de son mot de passe.
/// </summary>
public partial class ChangerProvinceDialogViewModel : DialogViewModelBase
{
    /// <summary>Crée le formulaire avec la liste des provinces disponibles.</summary>
    /// <param name="provinces">Provinces proposées.</param>
    /// <param name="idProvinceCourante">Province actuellement ouverte (présélectionnée).</param>
    public ChangerProvinceDialogViewModel(IReadOnlyList<Province> provinces, int? idProvinceCourante)
    {
        Provinces = provinces ?? Array.Empty<Province>();
        ProvinceSelectionnee = Provinces.FirstOrDefault(p => p.IdProvince == idProvinceCourante)
            ?? (Provinces.Count > 0 ? Provinces[0] : null);
    }

    /// <summary>Provinces proposées.</summary>
    public IReadOnlyList<Province> Provinces { get; }

    [ObservableProperty] private Province? _provinceSelectionnee;
    [ObservableProperty] private string _motDePasse = string.Empty;
    [ObservableProperty] private string? _erreur;

    /// <summary>Province choisie (0 si aucune).</summary>
    public int IdProvinceChoisi => ProvinceSelectionnee?.IdProvince ?? 0;

    /// <inheritdoc />
    public override string Titre => "Changer de province";

    /// <inheritdoc />
    protected override bool ValiderSaisie()
    {
        if (ProvinceSelectionnee is null)
        {
            Erreur = "Sélectionnez une province.";
            return false;
        }

        if (string.IsNullOrEmpty(MotDePasse))
        {
            Erreur = "Saisissez le mot de passe de la province.";
            return false;
        }

        Erreur = null;
        return true;
    }
}

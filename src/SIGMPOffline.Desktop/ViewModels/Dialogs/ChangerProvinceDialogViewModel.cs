using CommunityToolkit.Mvvm.ComponentModel;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Desktop.ViewModels.Dialogs;

/// <summary>Formulaire de sélection d'une province autorisée au compte connecté.</summary>
public partial class ChangerProvinceDialogViewModel : DialogViewModelBase
{
    public ChangerProvinceDialogViewModel(IReadOnlyList<Province> provinces, int? idProvinceCourante)
    {
        Provinces = provinces ?? Array.Empty<Province>();
        ProvinceSelectionnee = Provinces.FirstOrDefault(p => p.IdProvince == idProvinceCourante)
            ?? (Provinces.Count > 0 ? Provinces[0] : null);
    }

    public IReadOnlyList<Province> Provinces { get; }
    [ObservableProperty] private Province? _provinceSelectionnee;
    [ObservableProperty] private string? _erreur;

    public bool ProvinceInvalide => Erreur is not null && ProvinceSelectionnee is null;
    public int IdProvinceChoisi => ProvinceSelectionnee?.IdProvince ?? 0;
    public override string Titre => "Changer de province";

    partial void OnErreurChanged(string? value) => OnPropertyChanged(nameof(ProvinceInvalide));

    partial void OnProvinceSelectionneeChanged(Province? value)
    {
        Erreur = null;
        OnPropertyChanged(nameof(ProvinceInvalide));
    }

    protected override bool ValiderSaisie()
    {
        if (ProvinceSelectionnee is null)
        {
            Erreur = "Sélectionnez une province autorisée.";
            return false;
        }

        Erreur = null;
        return true;
    }
}

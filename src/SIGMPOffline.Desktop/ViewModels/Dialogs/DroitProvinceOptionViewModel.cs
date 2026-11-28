using CommunityToolkit.Mvvm.ComponentModel;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Desktop.ViewModels.Dialogs;

/// <summary>Option de province et niveau de droit dans le formulaire d'un compte.</summary>
public partial class DroitProvinceOptionViewModel : ObservableObject
{
    public DroitProvinceOptionViewModel(Province province, bool estAutorise, bool peutModifier)
    {
        IdProvince = province.IdProvince;
        NomProvince = province.NomProvince;
        EstAutorise = estAutorise;
        PeutModifier = peutModifier;
    }

    public int IdProvince { get; }
    public string NomProvince { get; }

    [ObservableProperty] private bool _estAutorise;
    [ObservableProperty] private bool _peutModifier;
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SIGMPOffline.Desktop.ViewModels.Dialogs;

/// <summary>Une ligne « service bénéficiaire / montant » dans le formulaire d'une ligne de PPM.</summary>
public partial class BeneficiaireEditionViewModel : ObservableObject
{
    public BeneficiaireEditionViewModel(string service, decimal montant, Action<BeneficiaireEditionViewModel> retirer)
    {
        _service = service;
        _montant = montant;
        RetirerCommand = new RelayCommand(() => retirer(this));
    }

    [ObservableProperty] private string _service;
    [ObservableProperty] private decimal _montant;

    /// <summary>Retire cette ligne du formulaire.</summary>
    public IRelayCommand RetirerCommand { get; }
}

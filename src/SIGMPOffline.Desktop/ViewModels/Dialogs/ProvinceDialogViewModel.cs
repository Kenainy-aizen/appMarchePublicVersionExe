using CommunityToolkit.Mvvm.ComponentModel;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Desktop.ViewModels.Dialogs;

/// <summary>Formulaire de création d'une province, réservée à l'administrateur PRMP.</summary>
public partial class ProvinceDialogViewModel : DialogViewModelBase
{
    [ObservableProperty] private string _nomProvince = string.Empty;
    [ObservableProperty] private string? _erreur;

    public bool NomProvinceInvalide => Erreur is not null && string.IsNullOrWhiteSpace(NomProvince);
    public override string Titre => "Nouvelle province";

    partial void OnNomProvinceChanged(string value) => Erreur = null;
    partial void OnErreurChanged(string? value) => OnPropertyChanged(nameof(NomProvinceInvalide));

    protected override bool ValiderSaisie()
    {
        NomProvince = (NomProvince ?? string.Empty).Trim();
        if (NomProvince.Length == 0)
        {
            Erreur = "Le nom de la province est obligatoire.";
            return false;
        }

        Erreur = null;
        return true;
    }
}

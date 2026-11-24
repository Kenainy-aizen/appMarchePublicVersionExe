using CommunityToolkit.Mvvm.ComponentModel;

namespace SIGMPOffline.Desktop.Services;

/// <summary>
/// Mode consultation globale : toutes les données restent visibles, mais aucune
/// création, modification ou suppression n'est autorisée.
/// </summary>
public sealed partial class ModeLectureService : ObservableObject
{
    [ObservableProperty] private bool _estActif;

    /// <summary>Indique si les actions d'écriture sont autorisées.</summary>
    public bool PeutModifier => !EstActif;

    partial void OnEstActifChanged(bool value) => OnPropertyChanged(nameof(PeutModifier));
}

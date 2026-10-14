namespace SIGMPOffline.Desktop.ViewModels;

/// <summary>Contrat d'une page navigable (chargement paresseux à l'ouverture).</summary>
public interface IPageViewModel
{
    /// <summary>Titre de la page.</summary>
    string Titre { get; }

    /// <summary>Charge (ou recharge) les données de la page.</summary>
    Task ChargerAsync();
}

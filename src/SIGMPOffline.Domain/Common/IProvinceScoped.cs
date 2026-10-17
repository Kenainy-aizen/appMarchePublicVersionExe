namespace SIGMPOffline.Domain.Common;

/// <summary>
/// Marque une entité métier rattachée à une province. Les entités implémentant
/// cette interface voient leur <see cref="IdProvince"/> renseignée automatiquement
/// à la sauvegarde, et sont filtrées par province en lecture.
/// </summary>
public interface IProvinceScoped
{
    /// <summary>Clé étrangère vers la province propriétaire des données.</summary>
    int? IdProvince { get; set; }
}

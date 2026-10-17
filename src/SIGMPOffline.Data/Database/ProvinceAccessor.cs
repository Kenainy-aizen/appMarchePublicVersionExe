using SIGMPOffline.Domain.Common;

namespace SIGMPOffline.Data.Database;

/// <summary>
/// Implémentation en mémoire de <see cref="IProvinceAccessor"/>, enregistrée comme
/// <b>singleton</b> : la province active est partagée par tout le conteneur et
/// évaluée par les filtres de requête globaux d'EF Core à chaque requête.
/// </summary>
public sealed class ProvinceAccessor : IProvinceAccessor
{
    private int? _idProvince;

    /// <inheritdoc />
    public int? IdProvince => _idProvince;

    /// <inheritdoc />
    public void DefinirProvince(int? idProvince) => _idProvince = idProvince;
}

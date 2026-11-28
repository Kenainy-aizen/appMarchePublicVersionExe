namespace SIGMPOffline.Domain.Entities;

/// <summary>Province dont l'accès est attribué aux comptes utilisateurs.</summary>
public class Province
{
    /// <summary>Identifiant (clé primaire, auto-incrémenté).</summary>
    public int IdProvince { get; set; }

    /// <summary>Nom de la province (unique).</summary>
    public string NomProvince { get; set; } = string.Empty;

    /// <summary>Date de création de la province.</summary>
    public DateTime DateCreation { get; set; }

    /// <summary>Comptes auxquels cette province est attribuée.</summary>
    public ICollection<PermissionProvince> PermissionsUtilisateurs { get; set; } = new List<PermissionProvince>();
}

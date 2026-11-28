namespace SIGMPOffline.Domain.Entities;

/// <summary>Compte de connexion de l'application (PRMP administrateur ou utilisateur).</summary>
public class Utilisateur
{
    public int IdUtilisateur { get; set; }
    public string NomUtilisateur { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public bool EstAdministrateur { get; set; }
    public bool EstActif { get; set; } = true;
    public DateTime DateCreation { get; set; }

    /// <summary>Provinces autorisées et niveau d'accès de ce compte.</summary>
    public ICollection<PermissionProvince> PermissionsProvinces { get; set; } = new List<PermissionProvince>();
}

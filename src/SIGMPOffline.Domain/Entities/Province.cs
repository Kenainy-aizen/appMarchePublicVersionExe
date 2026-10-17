namespace SIGMPOffline.Domain.Entities;

/// <summary>
/// Province gérée par l'application. Chaque province possède son propre mot de passe
/// et voit ses données isolées des autres provinces.
/// Table : <c>Province</c>.
/// </summary>
public class Province
{
    /// <summary>Identifiant (clé primaire, auto-incrémenté).</summary>
    public int IdProvince { get; set; }

    /// <summary>Nom de la province (unique).</summary>
    public string NomProvince { get; set; } = string.Empty;

    /// <summary>Condensat du mot de passe (PBKDF2, Base64).</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Sel du mot de passe (Base64).</summary>
    public string Salt { get; set; } = string.Empty;

    /// <summary>Date de création de la province.</summary>
    public DateTime DateCreation { get; set; }
}

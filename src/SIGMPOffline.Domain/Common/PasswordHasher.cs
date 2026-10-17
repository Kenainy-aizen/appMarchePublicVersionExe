using System.Security.Cryptography;

namespace SIGMPOffline.Domain.Common;

/// <summary>
/// Hachage et vérification de mots de passe via <b>PBKDF2</b> (HMAC-SHA-256).
/// Le sel est aléatoire et stocké séparément du condensat (colonnes <c>Salt</c>
/// et <c>PasswordHash</c>), conformément au modèle de données.
/// </summary>
public static class PasswordHasher
{
    /// <summary>Nombre d'itérations PBKDF2 (coût de dérivation).</summary>
    private const int Iterations = 120_000;

    /// <summary>Taille du sel, en octets.</summary>
    private const int SaltSize = 16;

    /// <summary>Taille du condensat, en octets.</summary>
    private const int HashSize = 32;

    /// <summary>
    /// Calcule le condensat d'un mot de passe avec un nouveau sel aléatoire.
    /// </summary>
    /// <param name="motDePasse">Mot de passe en clair (jamais stocké).</param>
    /// <returns>Le couple (condensat Base64, sel Base64).</returns>
    /// <exception cref="DomainException">Si le mot de passe est vide.</exception>
    public static (string Hash, string Salt) Hacher(string motDePasse)
    {
        if (string.IsNullOrWhiteSpace(motDePasse))
        {
            throw new DomainException("Le mot de passe ne peut pas être vide.");
        }

        var sel = RandomNumberGenerator.GetBytes(SaltSize);
        var condensat = Rfc2898DeriveBytes.Pbkdf2(
            motDePasse, sel, Iterations, HashAlgorithmName.SHA256, HashSize);

        return (Convert.ToBase64String(condensat), Convert.ToBase64String(sel));
    }

    /// <summary>
    /// Vérifie un mot de passe contre un condensat et un sel stockés.
    /// La comparaison est à temps constant.
    /// </summary>
    /// <param name="motDePasse">Mot de passe candidat en clair.</param>
    /// <param name="hashBase64">Condensat attendu (Base64).</param>
    /// <param name="saltBase64">Sel (Base64).</param>
    /// <returns><c>true</c> si le mot de passe correspond.</returns>
    public static bool Verifier(string motDePasse, string hashBase64, string saltBase64)
    {
        if (string.IsNullOrEmpty(motDePasse)
            || string.IsNullOrEmpty(hashBase64)
            || string.IsNullOrEmpty(saltBase64))
        {
            return false;
        }

        byte[] sel;
        byte[] attendu;
        try
        {
            sel = Convert.FromBase64String(saltBase64);
            attendu = Convert.FromBase64String(hashBase64);
        }
        catch (FormatException)
        {
            return false;
        }

        if (attendu.Length == 0)
        {
            return false;
        }

        var essai = Rfc2898DeriveBytes.Pbkdf2(
            motDePasse, sel, Iterations, HashAlgorithmName.SHA256, attendu.Length);

        return CryptographicOperations.FixedTimeEquals(essai, attendu);
    }
}

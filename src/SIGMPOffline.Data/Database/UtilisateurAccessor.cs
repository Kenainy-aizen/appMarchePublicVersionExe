namespace SIGMPOffline.Data.Database;

/// <summary>État singleton du compte connecté, utilisé par le journal d'audit.</summary>
public sealed class UtilisateurAccessor : IUtilisateurAccessor
{
    public int? IdUtilisateur { get; private set; }
    public string? NomUtilisateur { get; private set; }
    public string? NomProvince { get; private set; }

    public void DefinirUtilisateur(int? idUtilisateur, string? nomUtilisateur, string? nomProvince = null)
    {
        IdUtilisateur = idUtilisateur;
        NomUtilisateur = idUtilisateur is null ? null : nomUtilisateur;
        NomProvince = idUtilisateur is null ? null : nomProvince;
    }
}

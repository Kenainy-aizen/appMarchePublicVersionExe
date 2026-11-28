namespace SIGMPOffline.Data.Database;

/// <summary>Identité active pour attribuer les écritures métier à leur auteur.</summary>
public interface IUtilisateurAccessor
{
    int? IdUtilisateur { get; }
    string? NomUtilisateur { get; }
    string? NomProvince { get; }

    void DefinirUtilisateur(int? idUtilisateur, string? nomUtilisateur, string? nomProvince = null);
}

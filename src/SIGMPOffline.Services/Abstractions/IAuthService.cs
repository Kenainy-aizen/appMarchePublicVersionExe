using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Services.Abstractions;

/// <summary>Authentification des utilisateurs et gestion des droits par province.</summary>
public interface IAuthService
{
    bool EstAuthentifie { get; }
    bool EstAdministrateur { get; }
    bool PeutModifier { get; }
    Province? ProvinceActive { get; }
    Utilisateur? UtilisateurActif { get; }
    ConfigurationApp? Configuration { get; }

    Task<bool> EstPremierLancementAsync(CancellationToken cancellationToken = default);

    /// <summary>Crée la configuration initiale et le compte admin PRMP.</summary>
    Task InitialiserAsync(
        string nomProprietaire,
        string nomProvince,
        string motDePasse,
        CancellationToken cancellationToken = default);

    /// <summary>Liste les provinces proposées à l'écran de connexion.</summary>
    Task<IReadOnlyList<Province>> ListerProvincesAsync(CancellationToken cancellationToken = default);

    /// <summary>Liste les provinces auxquelles le compte connecté a accès.</summary>
    Task<IReadOnlyList<Province>> ListerProvincesAutoriseesAsync(CancellationToken cancellationToken = default);

    /// <summary>Liste les comptes et leurs droits. Réservé à l'administrateur PRMP.</summary>
    Task<IReadOnlyList<Utilisateur>> ListerUtilisateursAsync(CancellationToken cancellationToken = default);

    /// <summary>Liste les événements récents. Réservé à l'administrateur PRMP.</summary>
    Task<IReadOnlyList<HistoriqueEvenement>> ListerHistoriqueAsync(
        int limite = 500,
        CancellationToken cancellationToken = default);

    Task<string> ModifierNomProprietaireAsync(
        string nomProprietaire,
        CancellationToken cancellationToken = default);

    /// <summary>Crée une province sans mot de passe dédié.</summary>
    Task<Province> CreerProvinceAsync(string nomProvince, CancellationToken cancellationToken = default);

    /// <summary>Authentifie un compte et vérifie son droit sur la province demandée.</summary>
    Task<Province> ConnecterAsync(
        string nomUtilisateur,
        string motDePasse,
        int idProvince,
        CancellationToken cancellationToken = default);

    /// <summary>Change la province active sans redemander les identifiants.</summary>
    Task<Province> ChangerProvinceAsync(int idProvince, CancellationToken cancellationToken = default);

    /// <summary>Crée un compte non administrateur avec ses droits par province.</summary>
    Task<Utilisateur> CreerUtilisateurAsync(
        string nomUtilisateur,
        string motDePasse,
        IReadOnlyDictionary<int, bool> droitsParProvince,
        CancellationToken cancellationToken = default);

    /// <summary>Modifie le mot de passe (si fourni) et les droits d'un compte non administrateur.</summary>
    Task ModifierUtilisateurAsync(
        int idUtilisateur,
        string nomUtilisateur,
        string? nouveauMotDePasse,
        IReadOnlyDictionary<int, bool> droitsParProvince,
        CancellationToken cancellationToken = default);

    /// <summary>Supprime un compte non administrateur.</summary>
    Task SupprimerUtilisateurAsync(int idUtilisateur, CancellationToken cancellationToken = default);

    /// <summary>Change le mot de passe du compte actuellement connecté.</summary>
    Task ChangerMotDePasseAsync(
        string ancienMotDePasse,
        string nouveauMotDePasse,
        CancellationToken cancellationToken = default);

    Task DeconnecterAsync(CancellationToken cancellationToken = default);
}

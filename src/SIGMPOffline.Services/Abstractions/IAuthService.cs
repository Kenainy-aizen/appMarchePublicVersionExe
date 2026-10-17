using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Services.Abstractions;

/// <summary>
/// Authentification et gestion <b>multi-provinces</b> : détection du premier
/// lancement, configuration initiale, connexion, création de provinces et
/// changement de mot de passe.
/// </summary>
public interface IAuthService
{
    /// <summary>Indique si une province est actuellement ouverte (session active).</summary>
    bool EstAuthentifie { get; }

    /// <summary>Province actuellement ouverte, ou <c>null</c> si aucune session.</summary>
    Province? ProvinceActive { get; }

    /// <summary>Configuration globale (propriétaire), ou <c>null</c> avant la première configuration.</summary>
    ConfigurationApp? Configuration { get; }

    /// <summary>Indique si l'application n'a pas encore été configurée (premier lancement).</summary>
    Task<bool> EstPremierLancementAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Effectue la configuration initiale : enregistre le propriétaire PRMP et crée
    /// la première province (qui devient la province active).
    /// </summary>
    Task InitialiserAsync(
        string nomProprietaire,
        string nomProvince,
        string motDePasse,
        CancellationToken cancellationToken = default);

    /// <summary>Liste les provinces disponibles pour la connexion, triées par nom.</summary>
    Task<IReadOnlyList<Province>> ListerProvincesAsync(CancellationToken cancellationToken = default);

    /// <summary>Crée une nouvelle province avec son propre mot de passe.</summary>
    Task<Province> CreerProvinceAsync(
        string nomProvince,
        string motDePasse,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ouvre la session sur la province indiquée après vérification du mot de passe.
    /// Lève une exception de domaine si le mot de passe est incorrect.
    /// </summary>
    Task<Province> ConnecterAsync(
        int idProvince,
        string motDePasse,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Change le mot de passe d'une province. L'ancien mot de passe est exigé.
    /// </summary>
    Task ChangerMotDePasseAsync(
        int idProvince,
        string ancienMotDePasse,
        string nouveauMotDePasse,
        CancellationToken cancellationToken = default);

    /// <summary>Ferme la session en cours (plus aucune province active).</summary>
    Task DeconnecterAsync(CancellationToken cancellationToken = default);
}

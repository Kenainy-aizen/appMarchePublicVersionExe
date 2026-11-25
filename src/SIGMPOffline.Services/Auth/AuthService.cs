using Microsoft.Extensions.DependencyInjection;
using SIGMPOffline.Data;
using SIGMPOffline.Data.Database;
using SIGMPOffline.Data.Repositories.Interfaces;
using SIGMPOffline.Domain.Common;
using SIGMPOffline.Domain.Entities;
using SIGMPOffline.Services.Abstractions;
using SIGMPOffline.Services.Scoping;

namespace SIGMPOffline.Services.Auth;

/// <inheritdoc />
/// <remarks>
/// L'état de session (province active) est conservé en mémoire et propagé à
/// <see cref="IProvinceAccessor"/>, qui alimente les filtres de requête globaux
/// d'EF Core chargés d'isoler les données de chaque province.
/// Chaque accès à la base ouvre un scope court via <see cref="IScopeRunner"/> :
/// aucun contexte EF Core n'est conservé entre deux opérations.
/// </remarks>
public class AuthService : IAuthService
{
    /// <summary>Longueur minimale admise pour un mot de passe de province.</summary>
    public const int LongueurMinMotDePasse = 4;

    /// <summary>Longueur maximale du nom du responsable PRMP (alignée sur la base).</summary>
    public const int LongueurMaxNomProprietaire = 200;

    private readonly IScopeRunner _scopeRunner;
    private readonly IProvinceAccessor _provinceAccessor;

    private ConfigurationApp? _configuration;
    private Province? _provinceActive;
    private bool _estVisiteur;

    public AuthService(IScopeRunner scopeRunner, IProvinceAccessor provinceAccessor)
    {
        _scopeRunner = scopeRunner ?? throw new ArgumentNullException(nameof(scopeRunner));
        _provinceAccessor = provinceAccessor ?? throw new ArgumentNullException(nameof(provinceAccessor));
    }

    /// <inheritdoc />
    public bool EstAuthentifie => _provinceActive is not null;

    /// <inheritdoc />
    public Province? ProvinceActive => _provinceActive;

    /// <inheritdoc />
    public bool EstVisiteur => _estVisiteur;

    /// <inheritdoc />
    public ConfigurationApp? Configuration => _configuration;

    /// <inheritdoc />
    public async Task<bool> EstPremierLancementAsync(CancellationToken cancellationToken = default)
        => await _scopeRunner.ExecuterAsync<IUnitOfWork, bool>(async unitOfWork =>
        {
            _configuration = await unitOfWork.Configuration.GetConfigurationAsync(cancellationToken);

            if (_configuration is null)
            {
                return true;
            }

            var provinces = await unitOfWork.Provinces.CountAsync(cancellationToken: cancellationToken);
            return provinces == 0;
        });

    /// <inheritdoc />
    public async Task InitialiserAsync(
        string nomProprietaire,
        string nomProvince,
        string motDePasse,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nomProprietaire))
        {
            throw new DomainException("Le nom du responsable PRMP est obligatoire.");
        }

        nomProvince = (nomProvince ?? string.Empty).Trim();
        if (nomProvince.Length == 0)
        {
            throw new DomainException("Le nom de la province est obligatoire.");
        }

        ValiderMotDePasse(motDePasse);

        await _scopeRunner.ExecuterAsync(async services =>
        {
            var unitOfWork = services.GetRequiredService<IUnitOfWork>();
            var context = services.GetRequiredService<AppDbContext>();

            if (await unitOfWork.Provinces.NomExisteAsync(nomProvince, null, cancellationToken))
            {
                throw new DomainException($"Une province nommée « {nomProvince} » existe déjà.");
            }

            // 1) Configuration globale (propriétaire) — créée une seule fois.
            var configuration = await unitOfWork.Configuration.GetConfigurationAsync(cancellationToken);
            if (configuration is null)
            {
                configuration = new ConfigurationApp
                {
                    NomProprietairePRMP = nomProprietaire.Trim(),
                    DateInstallation = DateTime.Now
                };
                await unitOfWork.Configuration.AddAsync(configuration, cancellationToken);
            }
            else
            {
                configuration.NomProprietairePRMP = nomProprietaire.Trim();
                unitOfWork.Configuration.Update(configuration);
            }

            // 2) Première province.
            var (hash, sel) = PasswordHasher.Hacher(motDePasse);
            var province = new Province
            {
                NomProvince = nomProvince,
                PasswordHash = hash,
                Salt = sel,
                DateCreation = DateTime.Now
            };

            await unitOfWork.Provinces.AddAsync(province, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // 3) Rattache les données éventuellement créées avant le multi-provinces.
            await DbSeeder.AdopterDonneesSansProvinceAsync(context, province.IdProvince, cancellationToken);

            // 4) Ouvre directement la session sur la province créée et sème la démo.
            OuvrirSession(configuration, province);
            unitOfWork.DetacherTout();
            await DbSeeder.SeedAsync(context, cancellationToken);
        });
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Province>> ListerProvincesAsync(CancellationToken cancellationToken = default)
        => await _scopeRunner.ExecuterAsync<IUnitOfWork, IReadOnlyList<Province>>(
            unitOfWork => unitOfWork.Provinces.ListerAsync(cancellationToken));

    /// <inheritdoc />
    public async Task<string> ModifierNomProprietaireAsync(
        string nomProprietaire,
        CancellationToken cancellationToken = default)
    {
        var nom = (nomProprietaire ?? string.Empty).Trim();

        if (nom.Length == 0)
        {
            throw new DomainException("Le nom du responsable PRMP est obligatoire.");
        }

        if (nom.Length > LongueurMaxNomProprietaire)
        {
            throw new DomainException(
                $"Le nom du responsable PRMP ne peut pas dépasser {LongueurMaxNomProprietaire} caractères.");
        }

        _configuration = await _scopeRunner.ExecuterAsync<IUnitOfWork, ConfigurationApp>(
            async unitOfWork =>
            {
                var configuration = _configuration
                    ?? await unitOfWork.Configuration.GetConfigurationAsync(cancellationToken)
                    ?? throw new DomainException("La configuration de l'application est introuvable.");

                if (!string.Equals(configuration.NomProprietairePRMP, nom, StringComparison.Ordinal))
                {
                    configuration.NomProprietairePRMP = nom;
                    unitOfWork.Configuration.Update(configuration);
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                }

                return configuration;
            });

        // Conserve la configuration en mémoire pour l'en-tête du shell.
        return nom;
    }

    /// <inheritdoc />
    public async Task<Province> CreerProvinceAsync(
        string nomProvince,
        string motDePasse,
        CancellationToken cancellationToken = default)
    {
        nomProvince = (nomProvince ?? string.Empty).Trim();
        if (nomProvince.Length == 0)
        {
            throw new DomainException("Le nom de la province est obligatoire.");
        }

        ValiderMotDePasse(motDePasse);

        return await _scopeRunner.ExecuterAsync<IUnitOfWork, Province>(async unitOfWork =>
        {
            if (await unitOfWork.Provinces.NomExisteAsync(nomProvince, null, cancellationToken))
            {
                throw new DomainException($"Une province nommée « {nomProvince} » existe déjà.");
            }

            var (hash, sel) = PasswordHasher.Hacher(motDePasse);
            var province = new Province
            {
                NomProvince = nomProvince,
                PasswordHash = hash,
                Salt = sel,
                DateCreation = DateTime.Now
            };

            await unitOfWork.Provinces.AddAsync(province, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return province;
        });
    }

    /// <inheritdoc />
    public async Task<Province> ConnecterAsync(
        int idProvince,
        string motDePasse,
        CancellationToken cancellationToken = default)
        => await _scopeRunner.ExecuterAsync<IUnitOfWork, Province>(async unitOfWork =>
        {
            var province = await unitOfWork.Provinces.GetByIdAsync(idProvince, cancellationToken)
                ?? throw new DomainException("La province sélectionnée est introuvable.");

            if (!PasswordHasher.Verifier(motDePasse ?? string.Empty, province.PasswordHash, province.Salt))
            {
                throw new DomainException("Mot de passe incorrect.");
            }

            _configuration ??= await unitOfWork.Configuration.GetConfigurationAsync(cancellationToken);
            OuvrirSession(_configuration, province);

            return province;
        });

    /// <inheritdoc />
    public async Task<Province> ConnecterVisiteurAsync(
        int idProvince,
        string motDePasse,
        CancellationToken cancellationToken = default)
        => await _scopeRunner.ExecuterAsync<IUnitOfWork, Province>(async unitOfWork =>
        {
            var province = await unitOfWork.Provinces.GetByIdAsync(idProvince, cancellationToken)
                ?? throw new DomainException("La province sélectionnée est introuvable.");

            if (string.IsNullOrEmpty(province.VisitorPasswordHash)
                || string.IsNullOrEmpty(province.VisitorSalt)
                || !PasswordHasher.Verifier(motDePasse ?? string.Empty, province.VisitorPasswordHash, province.VisitorSalt))
            {
                throw new DomainException("Accès visiteur indisponible ou mot de passe incorrect.");
            }

            _configuration ??= await unitOfWork.Configuration.GetConfigurationAsync(cancellationToken);
            OuvrirSession(_configuration, province, estVisiteur: true);

            return province;
        });

    /// <inheritdoc />
    public async Task ConfigurerAccesVisiteurAsync(
        string? nouveauMotDePasse,
        CancellationToken cancellationToken = default)
    {
        var provinceActive = _provinceActive;
        if (provinceActive is null || _estVisiteur)
        {
            throw new DomainException("Seul le responsable connecté peut gérer l'accès visiteur.");
        }

        var desactiver = string.IsNullOrWhiteSpace(nouveauMotDePasse);
        if (!desactiver)
        {
            ValiderMotDePasse(nouveauMotDePasse!);
        }

        var informations = desactiver
            ? ((string Hash, string Salt)?)null
            : PasswordHasher.Hacher(nouveauMotDePasse!);

        await _scopeRunner.ExecuterAsync<IUnitOfWork>(async unitOfWork =>
        {
            var province = await unitOfWork.Provinces.GetByIdAsync(provinceActive.IdProvince, cancellationToken)
                ?? throw new DomainException("La province active est introuvable.");

            province.VisitorPasswordHash = informations?.Hash;
            province.VisitorSalt = informations?.Salt;
            unitOfWork.Provinces.Update(province);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        });

        provinceActive.VisitorPasswordHash = informations?.Hash;
        provinceActive.VisitorSalt = informations?.Salt;
    }

    /// <inheritdoc />
    public async Task ChangerMotDePasseAsync(
        int idProvince,
        string ancienMotDePasse,
        string nouveauMotDePasse,
        CancellationToken cancellationToken = default)
        => await _scopeRunner.ExecuterAsync<IUnitOfWork>(async unitOfWork =>
        {
            var province = await unitOfWork.Provinces.GetByIdAsync(idProvince, cancellationToken)
                ?? throw new DomainException("La province est introuvable.");

            if (!PasswordHasher.Verifier(ancienMotDePasse ?? string.Empty, province.PasswordHash, province.Salt))
            {
                throw new DomainException("L'ancien mot de passe est incorrect.");
            }

            ValiderMotDePasse(nouveauMotDePasse);

            var (hash, sel) = PasswordHasher.Hacher(nouveauMotDePasse);
            province.PasswordHash = hash;
            province.Salt = sel;

            unitOfWork.Provinces.Update(province);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        });

    /// <inheritdoc />
    public Task DeconnecterAsync(CancellationToken cancellationToken = default)
    {
        _provinceActive = null;
        _estVisiteur = false;
        _provinceAccessor.DefinirProvince(null);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Active la province indiquée : met à jour l'état de session et le contexte
    /// d'isolation. Les entités suivies sont libérées avec le scope de l'opération.
    /// </summary>
    private void OuvrirSession(ConfigurationApp? configuration, Province province, bool estVisiteur = false)
    {
        _configuration = configuration;
        _provinceActive = province;
        _estVisiteur = estVisiteur;

        _provinceAccessor.DefinirProvince(province.IdProvince);
    }

    /// <summary>Valide la robustesse minimale d'un mot de passe.</summary>
    private static void ValiderMotDePasse(string motDePasse)
    {
        if (string.IsNullOrWhiteSpace(motDePasse))
        {
            throw new DomainException("Le mot de passe est obligatoire.");
        }

        if (motDePasse.Length < LongueurMinMotDePasse)
        {
            throw new DomainException(
                $"Le mot de passe doit contenir au moins {LongueurMinMotDePasse} caractères.");
        }
    }
}

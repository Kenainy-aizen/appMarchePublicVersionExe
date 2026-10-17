using SIGMPOffline.Data;
using SIGMPOffline.Data.Database;
using SIGMPOffline.Data.Repositories.Interfaces;
using SIGMPOffline.Domain.Common;
using SIGMPOffline.Domain.Entities;
using SIGMPOffline.Services.Abstractions;

namespace SIGMPOffline.Services.Auth;

/// <inheritdoc />
/// <remarks>
/// L'état de session (province active) est conservé en mémoire et propagé à
/// <see cref="IProvinceAccessor"/>, qui alimente les filtres de requête globaux
/// d'EF Core chargés d'isoler les données de chaque province.
/// </remarks>
public class AuthService : IAuthService
{
    /// <summary>Longueur minimale admise pour un mot de passe de province.</summary>
    public const int LongueurMinMotDePasse = 4;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IProvinceAccessor _provinceAccessor;
    private readonly AppDbContext _context;

    private ConfigurationApp? _configuration;
    private Province? _provinceActive;

    public AuthService(
        IUnitOfWork unitOfWork,
        IProvinceAccessor provinceAccessor,
        AppDbContext context)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _provinceAccessor = provinceAccessor ?? throw new ArgumentNullException(nameof(provinceAccessor));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public bool EstAuthentifie => _provinceActive is not null;

    /// <inheritdoc />
    public Province? ProvinceActive => _provinceActive;

    /// <inheritdoc />
    public ConfigurationApp? Configuration => _configuration;

    /// <inheritdoc />
    public async Task<bool> EstPremierLancementAsync(CancellationToken cancellationToken = default)
    {
        _configuration = await _unitOfWork.Configuration.GetConfigurationAsync(cancellationToken);

        if (_configuration is null)
        {
            return true;
        }

        var provinces = await _unitOfWork.Provinces.CountAsync(cancellationToken: cancellationToken);
        return provinces == 0;
    }

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

        if (await _unitOfWork.Provinces.NomExisteAsync(nomProvince, null, cancellationToken))
        {
            throw new DomainException($"Une province nommée « {nomProvince} » existe déjà.");
        }

        // 1) Configuration globale (propriétaire) — créée une seule fois.
        var configuration = await _unitOfWork.Configuration.GetConfigurationAsync(cancellationToken);
        if (configuration is null)
        {
            configuration = new ConfigurationApp
            {
                NomProprietairePRMP = nomProprietaire.Trim(),
                DateInstallation = DateTime.Now
            };
            await _unitOfWork.Configuration.AddAsync(configuration, cancellationToken);
        }
        else
        {
            configuration.NomProprietairePRMP = nomProprietaire.Trim();
            _unitOfWork.Configuration.Update(configuration);
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

        await _unitOfWork.Provinces.AddAsync(province, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 3) Rattache les données éventuellement créées avant le multi-provinces.
        await DbSeeder.AdopterDonneesSansProvinceAsync(_context, province.IdProvince, cancellationToken);

        // 4) Ouvre directement la session sur la province créée et sème la démo.
        OuvrirSession(configuration, province);
        await DbSeeder.SeedAsync(_context, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Province>> ListerProvincesAsync(CancellationToken cancellationToken = default)
        => await _unitOfWork.Provinces.ListerAsync(cancellationToken);

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

        if (await _unitOfWork.Provinces.NomExisteAsync(nomProvince, null, cancellationToken))
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

        await _unitOfWork.Provinces.AddAsync(province, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return province;
    }

    /// <inheritdoc />
    public async Task<Province> ConnecterAsync(
        int idProvince,
        string motDePasse,
        CancellationToken cancellationToken = default)
    {
        var province = await _unitOfWork.Provinces.GetByIdAsync(idProvince, cancellationToken)
            ?? throw new DomainException("La province sélectionnée est introuvable.");

        if (!PasswordHasher.Verifier(motDePasse ?? string.Empty, province.PasswordHash, province.Salt))
        {
            throw new DomainException("Mot de passe incorrect.");
        }

        _configuration ??= await _unitOfWork.Configuration.GetConfigurationAsync(cancellationToken);
        OuvrirSession(_configuration, province);

        return province;
    }

    /// <inheritdoc />
    public async Task ChangerMotDePasseAsync(
        int idProvince,
        string ancienMotDePasse,
        string nouveauMotDePasse,
        CancellationToken cancellationToken = default)
    {
        var province = await _unitOfWork.Provinces.GetByIdAsync(idProvince, cancellationToken)
            ?? throw new DomainException("La province est introuvable.");

        if (!PasswordHasher.Verifier(ancienMotDePasse ?? string.Empty, province.PasswordHash, province.Salt))
        {
            throw new DomainException("L'ancien mot de passe est incorrect.");
        }

        ValiderMotDePasse(nouveauMotDePasse);

        var (hash, sel) = PasswordHasher.Hacher(nouveauMotDePasse);
        province.PasswordHash = hash;
        province.Salt = sel;

        _unitOfWork.Provinces.Update(province);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task DeconnecterAsync(CancellationToken cancellationToken = default)
    {
        _provinceActive = null;
        _provinceAccessor.DefinirProvince(null);
        _unitOfWork.DetacherTout();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Active la province indiquée : met à jour l'état de session, le contexte
    /// d'isolation et purge le suivi d'entités (changement de province).
    /// </summary>
    private void OuvrirSession(ConfigurationApp? configuration, Province province)
    {
        _configuration = configuration;
        _provinceActive = province;

        // Purge les entités suivies avant de basculer : évite toute fuite entre provinces.
        _unitOfWork.DetacherTout();
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

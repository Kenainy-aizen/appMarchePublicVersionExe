using Microsoft.EntityFrameworkCore;
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
/// Les rôles et les autorisations sont associés aux comptes utilisateurs. La province
/// active reste propagée à <see cref="IProvinceAccessor"/> pour conserver l'isolation EF.
/// </remarks>
public class AuthService : IAuthService
{
    /// <summary>Longueur minimale admise pour un mot de passe.</summary>
    public const int LongueurMinMotDePasse = 4;

    /// <summary>Longueur maximale d'un identifiant de connexion.</summary>
    public const int LongueurMaxNomUtilisateur = 200;

    /// <summary>Longueur maximale du nom du responsable PRMP.</summary>
    public const int LongueurMaxNomProprietaire = 200;

    private readonly IScopeRunner _scopeRunner;
    private readonly IProvinceAccessor _provinceAccessor;
    private readonly IUtilisateurAccessor _utilisateurAccessor;

    private ConfigurationApp? _configuration;
    private Province? _provinceActive;
    private Utilisateur? _utilisateurActif;
    private bool _peutModifier;

    public AuthService(
        IScopeRunner scopeRunner,
        IProvinceAccessor provinceAccessor,
        IUtilisateurAccessor utilisateurAccessor)
    {
        _scopeRunner = scopeRunner ?? throw new ArgumentNullException(nameof(scopeRunner));
        _provinceAccessor = provinceAccessor ?? throw new ArgumentNullException(nameof(provinceAccessor));
        _utilisateurAccessor = utilisateurAccessor ?? throw new ArgumentNullException(nameof(utilisateurAccessor));
    }

    public bool EstAuthentifie => _utilisateurActif is not null && _provinceActive is not null;
    public bool EstAdministrateur => _utilisateurActif?.EstAdministrateur == true;
    public bool PeutModifier => EstAdministrateur || _peutModifier;
    public Province? ProvinceActive => _provinceActive;
    public Utilisateur? UtilisateurActif => _utilisateurActif;
    public ConfigurationApp? Configuration => _configuration;

    /// <inheritdoc />
    public async Task<bool> EstPremierLancementAsync(CancellationToken cancellationToken = default)
        => await _scopeRunner.ExecuterAsync<AppDbContext, bool>(async context =>
        {
            _configuration = await context.Configurations.FirstOrDefaultAsync(cancellationToken);
            return _configuration is null
                || !await context.Utilisateurs.AnyAsync(x => x.EstAdministrateur, cancellationToken);
        });

    /// <inheritdoc />
    public async Task InitialiserAsync(
        string nomProprietaire,
        string nomProvince,
        string motDePasse,
        CancellationToken cancellationToken = default)
    {
        var identifiantAdmin = ValiderNomUtilisateur(nomProprietaire);
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

            if (await context.Utilisateurs.AnyAsync(x => x.EstAdministrateur, cancellationToken))
            {
                throw new DomainException("Le compte administrateur PRMP est déjà initialisé.");
            }

            if (await unitOfWork.Provinces.NomExisteAsync(nomProvince, null, cancellationToken))
            {
                throw new DomainException($"Une province nommée « {nomProvince} » existe déjà.");
            }

            if (await context.Utilisateurs.AnyAsync(
                    x => x.NomUtilisateur == identifiantAdmin, cancellationToken))
            {
                throw new DomainException("Cet identifiant utilisateur est déjà utilisé.");
            }

            var configuration = await unitOfWork.Configuration.GetConfigurationAsync(cancellationToken);
            if (configuration is null)
            {
                configuration = new ConfigurationApp
                {
                    NomProprietairePRMP = identifiantAdmin,
                    DateInstallation = DateTime.Now
                };
                await unitOfWork.Configuration.AddAsync(configuration, cancellationToken);
            }
            else
            {
                configuration.NomProprietairePRMP = identifiantAdmin;
                unitOfWork.Configuration.Update(configuration);
            }

            var (hash, sel) = PasswordHasher.Hacher(motDePasse);
            var province = new Province
            {
                NomProvince = nomProvince,
                DateCreation = DateTime.Now
            };
            var administrateur = new Utilisateur
            {
                NomUtilisateur = identifiantAdmin,
                PasswordHash = hash,
                Salt = sel,
                EstAdministrateur = true,
                EstActif = true,
                DateCreation = DateTime.Now
            };

            await unitOfWork.Provinces.AddAsync(province, cancellationToken);
            await context.Utilisateurs.AddAsync(administrateur, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await DbSeeder.AdopterDonneesSansProvinceAsync(context, province.IdProvince, cancellationToken);
            OuvrirSession(configuration, province, administrateur, peutModifier: true, definirAuteur: false);
            unitOfWork.DetacherTout();
            await DbSeeder.SeedAsync(context, cancellationToken);
            DefinirAuteurActif();
        });
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Province>> ListerProvincesAsync(
        CancellationToken cancellationToken = default)
        => await _scopeRunner.ExecuterAsync<AppDbContext, IReadOnlyList<Province>>(
            async context => await context.Provinces.AsNoTracking()
                .OrderBy(x => x.NomProvince).ToListAsync(cancellationToken));

    /// <inheritdoc />
    public async Task<IReadOnlyList<Province>> ListerProvincesAutoriseesAsync(
        CancellationToken cancellationToken = default)
    {
        ExigerSession();

        return await _scopeRunner.ExecuterAsync<AppDbContext, IReadOnlyList<Province>>(async context =>
        {
            if (EstAdministrateur)
            {
                return await context.Provinces.AsNoTracking()
                    .OrderBy(x => x.NomProvince).ToListAsync(cancellationToken);
            }

            var ids = await context.PermissionsProvinces.AsNoTracking()
                .Where(x => x.IdUtilisateur == _utilisateurActif!.IdUtilisateur)
                .Select(x => x.IdProvince)
                .ToListAsync(cancellationToken);

            return await context.Provinces.AsNoTracking()
                .Where(x => ids.Contains(x.IdProvince))
                .OrderBy(x => x.NomProvince)
                .ToListAsync(cancellationToken);
        });
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Utilisateur>> ListerUtilisateursAsync(
        CancellationToken cancellationToken = default)
    {
        ExigerAdministrateur();

        return await _scopeRunner.ExecuterAsync<AppDbContext, IReadOnlyList<Utilisateur>>(async context =>
            await context.Utilisateurs.AsNoTracking()
                .Include(x => x.PermissionsProvinces)
                .ThenInclude(x => x.Province)
                .OrderBy(x => x.EstAdministrateur ? 0 : 1)
                .ThenBy(x => x.NomUtilisateur)
                .ToListAsync(cancellationToken));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HistoriqueEvenement>> ListerHistoriqueAsync(
        int limite = 500,
        CancellationToken cancellationToken = default)
    {
        ExigerAdministrateur();
        limite = Math.Clamp(limite, 1, 2000);

        return await _scopeRunner.ExecuterAsync<AppDbContext, IReadOnlyList<HistoriqueEvenement>>(
            async context => await context.HistoriqueEvenements.AsNoTracking()
                .OrderByDescending(x => x.DateEvenement)
                .ThenByDescending(x => x.IdHistorique)
                .Take(limite)
                .ToListAsync(cancellationToken));
    }

    /// <inheritdoc />
    public async Task<string> ModifierNomProprietaireAsync(
        string nomProprietaire,
        CancellationToken cancellationToken = default)
    {
        ExigerAdministrateur();
        var nom = ValiderNomUtilisateur(nomProprietaire);
        if (nom.Length > LongueurMaxNomProprietaire)
        {
            throw new DomainException(
                $"Le nom du responsable PRMP ne peut pas dépasser {LongueurMaxNomProprietaire} caractères.");
        }

        await _scopeRunner.ExecuterAsync<AppDbContext>(async context =>
        {
            var collision = await context.Utilisateurs.AnyAsync(
                x => x.IdUtilisateur != _utilisateurActif!.IdUtilisateur && x.NomUtilisateur == nom,
                cancellationToken);
            if (collision)
            {
                throw new DomainException("Ce nom est déjà utilisé par un autre compte.");
            }

            var utilisateur = await context.Utilisateurs.SingleAsync(
                x => x.IdUtilisateur == _utilisateurActif!.IdUtilisateur, cancellationToken);
            var configuration = await context.Configurations.FirstOrDefaultAsync(cancellationToken)
                ?? throw new DomainException("La configuration de l'application est introuvable.");

            utilisateur.NomUtilisateur = nom;
            configuration.NomProprietairePRMP = nom;
            await context.SaveChangesAsync(cancellationToken);
        });

        _utilisateurActif!.NomUtilisateur = nom;
        DefinirAuteurActif();
        if (_configuration is not null)
        {
            _configuration.NomProprietairePRMP = nom;
        }

        return nom;
    }

    /// <inheritdoc />
    public async Task<Province> CreerProvinceAsync(
        string nomProvince,
        CancellationToken cancellationToken = default)
    {
        ExigerAdministrateur();
        nomProvince = (nomProvince ?? string.Empty).Trim();
        if (nomProvince.Length == 0)
        {
            throw new DomainException("Le nom de la province est obligatoire.");
        }

        return await _scopeRunner.ExecuterAsync<IUnitOfWork, Province>(async unitOfWork =>
        {
            if (await unitOfWork.Provinces.NomExisteAsync(nomProvince, null, cancellationToken))
            {
                throw new DomainException($"Une province nommée « {nomProvince} » existe déjà.");
            }

            var province = new Province { NomProvince = nomProvince, DateCreation = DateTime.Now };
            await unitOfWork.Provinces.AddAsync(province, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return province;
        });
    }

    /// <inheritdoc />
    public async Task<Province> ConnecterAsync(
        string nomUtilisateur,
        string motDePasse,
        int idProvince,
        CancellationToken cancellationToken = default)
    {
        var identifiant = (nomUtilisateur ?? string.Empty).Trim();
        if (identifiant.Length == 0 || string.IsNullOrEmpty(motDePasse))
        {
            throw new DomainException("Nom d'utilisateur, mot de passe ou province incorrect.");
        }

        return await _scopeRunner.ExecuterAsync<AppDbContext, Province>(async context =>
        {
            var utilisateur = await context.Utilisateurs.FirstOrDefaultAsync(
                x => x.NomUtilisateur == identifiant && x.EstActif, cancellationToken);
            if (utilisateur is null || !PasswordHasher.Verifier(motDePasse, utilisateur.PasswordHash, utilisateur.Salt))
            {
                await JournaliserEchecConnexionAsync(identifiant, idProvince, cancellationToken);
                throw new DomainException("Nom d'utilisateur, mot de passe ou province incorrect.");
            }

            var province = await context.Provinces.FirstOrDefaultAsync(
                x => x.IdProvince == idProvince, cancellationToken);
            if (province is null)
            {
                await JournaliserEchecConnexionAsync(identifiant, idProvince, cancellationToken);
                throw new DomainException("Nom d'utilisateur, mot de passe ou province incorrect.");
            }

            var droit = utilisateur.EstAdministrateur
                ? null
                : await context.PermissionsProvinces.AsNoTracking().FirstOrDefaultAsync(
                    x => x.IdUtilisateur == utilisateur.IdUtilisateur && x.IdProvince == idProvince,
                    cancellationToken);
            if (!utilisateur.EstAdministrateur && droit is null)
            {
                await JournaliserEchecConnexionAsync(identifiant, idProvince, cancellationToken);
                throw new DomainException("Nom d'utilisateur, mot de passe ou province incorrect.");
            }

            _configuration ??= await context.Configurations.FirstOrDefaultAsync(cancellationToken);
            OuvrirSession(_configuration, province, utilisateur, utilisateur.EstAdministrateur || droit!.PeutModifier);
            await JournaliserEvenementSessionAsync(
                "Connexion réussie", "Authentification", idProvince.ToString(), province.IdProvince, province.NomProvince,
                cancellationToken);
            return province;
        });
    }

    /// <inheritdoc />
    public async Task<Province> ChangerProvinceAsync(
        int idProvince,
        CancellationToken cancellationToken = default)
    {
        ExigerSession();

        return await _scopeRunner.ExecuterAsync<AppDbContext, Province>(async context =>
        {
            var province = await context.Provinces.FirstOrDefaultAsync(
                x => x.IdProvince == idProvince, cancellationToken)
                ?? throw new DomainException("La province sélectionnée est introuvable.");

            var droit = EstAdministrateur
                ? null
                : await context.PermissionsProvinces.AsNoTracking().FirstOrDefaultAsync(
                    x => x.IdUtilisateur == _utilisateurActif!.IdUtilisateur && x.IdProvince == idProvince,
                    cancellationToken);
            if (!EstAdministrateur && droit is null)
            {
                throw new DomainException("Votre compte n'est pas autorisé à accéder à cette province.");
            }

            OuvrirSession(_configuration, province, _utilisateurActif!, EstAdministrateur || droit!.PeutModifier);
            await JournaliserEvenementSessionAsync(
                "Changement de province", "Authentification", idProvince.ToString(), province.IdProvince,
                province.NomProvince, cancellationToken);
            return province;
        });
    }

    /// <inheritdoc />
    public async Task<Utilisateur> CreerUtilisateurAsync(
        string nomUtilisateur,
        string motDePasse,
        IReadOnlyDictionary<int, bool> droitsParProvince,
        CancellationToken cancellationToken = default)
    {
        ExigerAdministrateur();
        var nom = ValiderNomUtilisateur(nomUtilisateur);
        ValiderMotDePasse(motDePasse);
        ValiderDroits(droitsParProvince);

        return await _scopeRunner.ExecuterAsync<AppDbContext, Utilisateur>(async context =>
        {
            var collision = await context.Utilisateurs.AnyAsync(x => x.NomUtilisateur == nom, cancellationToken);
            if (collision)
            {
                throw new DomainException("Cet identifiant utilisateur est déjà utilisé.");
            }

            await VerifierProvincesAsync(context, droitsParProvince.Keys, cancellationToken);
            var (hash, sel) = PasswordHasher.Hacher(motDePasse);
            var utilisateur = new Utilisateur
            {
                NomUtilisateur = nom,
                PasswordHash = hash,
                Salt = sel,
                EstAdministrateur = false,
                EstActif = true,
                DateCreation = DateTime.Now,
                PermissionsProvinces = droitsParProvince.Select(droit => new PermissionProvince
                {
                    IdProvince = droit.Key,
                    PeutModifier = droit.Value
                }).ToList()
            };

            await context.Utilisateurs.AddAsync(utilisateur, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return utilisateur;
        });
    }

    /// <inheritdoc />
    public async Task ModifierUtilisateurAsync(
        int idUtilisateur,
        string nomUtilisateur,
        string? nouveauMotDePasse,
        IReadOnlyDictionary<int, bool> droitsParProvince,
        CancellationToken cancellationToken = default)
    {
        ExigerAdministrateur();
        var nom = ValiderNomUtilisateur(nomUtilisateur);
        ValiderDroits(droitsParProvince);
        if (!string.IsNullOrWhiteSpace(nouveauMotDePasse))
        {
            ValiderMotDePasse(nouveauMotDePasse);
        }

        await _scopeRunner.ExecuterAsync<AppDbContext>(async context =>
        {
            var utilisateur = await context.Utilisateurs
                .Include(x => x.PermissionsProvinces)
                .FirstOrDefaultAsync(x => x.IdUtilisateur == idUtilisateur, cancellationToken)
                ?? throw new DomainException("Le compte utilisateur est introuvable.");
            if (utilisateur.EstAdministrateur || utilisateur.IdUtilisateur == _utilisateurActif!.IdUtilisateur)
            {
                throw new DomainException("Le compte administrateur PRMP ne peut pas être modifié ici.");
            }

            var collision = await context.Utilisateurs.AnyAsync(
                x => x.IdUtilisateur != idUtilisateur && x.NomUtilisateur == nom, cancellationToken);
            if (collision)
            {
                throw new DomainException("Cet identifiant utilisateur est déjà utilisé.");
            }

            await VerifierProvincesAsync(context, droitsParProvince.Keys, cancellationToken);
            utilisateur.NomUtilisateur = nom;
            var droitsExistants = utilisateur.PermissionsProvinces.ToDictionary(x => x.IdProvince);
            foreach (var droitExistant in utilisateur.PermissionsProvinces.ToArray())
            {
                if (!droitsParProvince.TryGetValue(droitExistant.IdProvince, out var peutModifier))
                {
                    utilisateur.PermissionsProvinces.Remove(droitExistant);
                }
                else
                {
                    droitExistant.PeutModifier = peutModifier;
                }
            }

            foreach (var droit in droitsParProvince)
            {
                if (!droitsExistants.ContainsKey(droit.Key))
                {
                    utilisateur.PermissionsProvinces.Add(new PermissionProvince
                    {
                        IdUtilisateur = utilisateur.IdUtilisateur,
                        IdProvince = droit.Key,
                        PeutModifier = droit.Value
                    });
                }
            }

            if (!string.IsNullOrWhiteSpace(nouveauMotDePasse))
            {
                var (hash, sel) = PasswordHasher.Hacher(nouveauMotDePasse);
                utilisateur.PasswordHash = hash;
                utilisateur.Salt = sel;
            }

            await context.SaveChangesAsync(cancellationToken);
        });
    }

    /// <inheritdoc />
    public async Task SupprimerUtilisateurAsync(int idUtilisateur, CancellationToken cancellationToken = default)
    {
        ExigerAdministrateur();
        await _scopeRunner.ExecuterAsync<AppDbContext>(async context =>
        {
            var utilisateur = await context.Utilisateurs.FirstOrDefaultAsync(
                x => x.IdUtilisateur == idUtilisateur, cancellationToken)
                ?? throw new DomainException("Le compte utilisateur est introuvable.");
            if (utilisateur.EstAdministrateur || utilisateur.IdUtilisateur == _utilisateurActif!.IdUtilisateur)
            {
                throw new DomainException("Le compte administrateur PRMP ne peut pas être supprimé.");
            }

            context.Utilisateurs.Remove(utilisateur);
            await context.SaveChangesAsync(cancellationToken);
        });
    }

    /// <inheritdoc />
    public async Task ChangerMotDePasseAsync(
        string ancienMotDePasse,
        string nouveauMotDePasse,
        CancellationToken cancellationToken = default)
    {
        ExigerSession();
        ValiderMotDePasse(nouveauMotDePasse);
        var informations = PasswordHasher.Hacher(nouveauMotDePasse);

        await _scopeRunner.ExecuterAsync<AppDbContext>(async context =>
        {
            var utilisateur = await context.Utilisateurs.SingleAsync(
                x => x.IdUtilisateur == _utilisateurActif!.IdUtilisateur, cancellationToken);
            if (!PasswordHasher.Verifier(ancienMotDePasse ?? string.Empty, utilisateur.PasswordHash, utilisateur.Salt))
            {
                throw new DomainException("L'ancien mot de passe est incorrect.");
            }

            utilisateur.PasswordHash = informations.Hash;
            utilisateur.Salt = informations.Salt;
            await context.SaveChangesAsync(cancellationToken);
        });

        _utilisateurActif!.PasswordHash = informations.Hash;
        _utilisateurActif.Salt = informations.Salt;
    }

    /// <inheritdoc />
    public async Task DeconnecterAsync(CancellationToken cancellationToken = default)
    {
        if (EstAuthentifie)
        {
            try
            {
                await JournaliserEvenementSessionAsync(
                    "Déconnexion", "Authentification", null, _provinceActive!.IdProvince,
                    _provinceActive.NomProvince, cancellationToken);
            }
            catch
            {
                // La déconnexion ne doit pas être bloquée si le journal est indisponible.
            }
        }

        _provinceActive = null;
        _utilisateurActif = null;
        _peutModifier = false;
        _provinceAccessor.DefinirProvince(null);
        _utilisateurAccessor.DefinirUtilisateur(null, null);
    }

    private void OuvrirSession(
        ConfigurationApp? configuration,
        Province province,
        Utilisateur utilisateur,
        bool peutModifier,
        bool definirAuteur = true)
    {
        _configuration = configuration;
        _provinceActive = province;
        _utilisateurActif = utilisateur;
        _peutModifier = peutModifier;
        _provinceAccessor.DefinirProvince(province.IdProvince);
        if (definirAuteur)
        {
            DefinirAuteurActif();
        }
    }

    private void DefinirAuteurActif()
        => _utilisateurAccessor.DefinirUtilisateur(
            _utilisateurActif?.IdUtilisateur,
            _utilisateurActif?.NomUtilisateur,
            _provinceActive?.NomProvince);

    private Task JournaliserEvenementSessionAsync(
        string action,
        string typeEntite,
        string? referenceEntite,
        int? idProvince,
        string? nomProvince,
        CancellationToken cancellationToken)
    {
        var utilisateur = _utilisateurActif;
        if (utilisateur is null)
        {
            return Task.CompletedTask;
        }

        return _scopeRunner.ExecuterAsync<AppDbContext>(async context =>
        {
            context.HistoriqueEvenements.Add(new HistoriqueEvenement
            {
                DateEvenement = DateTime.Now,
                IdUtilisateur = utilisateur.IdUtilisateur,
                NomUtilisateur = utilisateur.NomUtilisateur,
                IdProvince = idProvince,
                NomProvince = nomProvince,
                Action = action,
                TypeEntite = typeEntite,
                ReferenceEntite = referenceEntite
            });
            await context.SaveChangesAsync(cancellationToken);
        });
    }

    private Task JournaliserEchecConnexionAsync(
        string nomUtilisateur,
        int idProvince,
        CancellationToken cancellationToken)
        => _scopeRunner.ExecuterAsync<AppDbContext>(async context =>
        {
            var province = await context.Provinces.AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdProvince == idProvince, cancellationToken);
            context.HistoriqueEvenements.Add(new HistoriqueEvenement
            {
                DateEvenement = DateTime.Now,
                NomUtilisateur = nomUtilisateur.Length <= LongueurMaxNomUtilisateur
                    ? nomUtilisateur
                    : nomUtilisateur[..LongueurMaxNomUtilisateur],
                IdProvince = idProvince,
                NomProvince = province?.NomProvince,
                Action = "Échec de connexion",
                TypeEntite = "Authentification",
                ReferenceEntite = idProvince.ToString()
            });
            await context.SaveChangesAsync(cancellationToken);
        });

    private void ExigerSession()
    {
        if (!EstAuthentifie)
        {
            throw new DomainException("Aucun utilisateur n'est connecté.");
        }
    }

    private void ExigerAdministrateur()
    {
        if (!EstAuthentifie || !EstAdministrateur)
        {
            throw new DomainException("Cette opération est réservée à l'administrateur PRMP.");
        }
    }

    private static string ValiderNomUtilisateur(string? valeur)
    {
        var nom = (valeur ?? string.Empty).Trim();
        if (nom.Length == 0)
        {
            throw new DomainException("Le nom d'utilisateur est obligatoire.");
        }

        if (nom.Length > LongueurMaxNomUtilisateur)
        {
            throw new DomainException($"Le nom d'utilisateur ne peut pas dépasser {LongueurMaxNomUtilisateur} caractères.");
        }

        return nom;
    }

    private static void ValiderDroits(IReadOnlyDictionary<int, bool> droitsParProvince)
    {
        ArgumentNullException.ThrowIfNull(droitsParProvince);
        if (droitsParProvince.Count == 0 || droitsParProvince.Keys.Any(id => id <= 0))
        {
            throw new DomainException("Attribuez au moins une province à ce compte.");
        }
    }

    private static async Task VerifierProvincesAsync(
        AppDbContext context,
        IEnumerable<int> idsProvince,
        CancellationToken cancellationToken)
    {
        var ids = idsProvince.Distinct().ToList();
        var nombre = await context.Provinces.CountAsync(x => ids.Contains(x.IdProvince), cancellationToken);
        if (nombre != ids.Count)
        {
            throw new DomainException("Une ou plusieurs provinces sélectionnées sont introuvables.");
        }
    }

    /// <summary>Valide la robustesse minimale d'un mot de passe.</summary>
    private static void ValiderMotDePasse(string? motDePasse)
    {
        if (string.IsNullOrWhiteSpace(motDePasse))
        {
            throw new DomainException("Le mot de passe est obligatoire.");
        }

        if (motDePasse.Length < LongueurMinMotDePasse)
        {
            throw new DomainException($"Le mot de passe doit contenir au moins {LongueurMinMotDePasse} caractères.");
        }
    }
}

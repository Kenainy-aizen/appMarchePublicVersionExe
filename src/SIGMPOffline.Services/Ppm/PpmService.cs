using SIGMPOffline.Data.Repositories.Interfaces;
using SIGMPOffline.Domain.Common;
using SIGMPOffline.Domain.Entities;
using SIGMPOffline.Services.Abstractions;

namespace SIGMPOffline.Services.Ppm;

/// <inheritdoc />
public class PpmService : IPpmService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPpmRepository _ppm;

    public PpmService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _ppm = unitOfWork.Ppm;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PpmEntete>> ListerAsync(
        int? anneeExercice = null,
        CancellationToken cancellationToken = default)
        => anneeExercice is int annee
            ? await _ppm.ListByAnneeAsync(annee, cancellationToken)
            : await _ppm.ListerAvecDetailsAsync(cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<PpmEntete>> ListerAvecDetailsAsync(CancellationToken cancellationToken = default)
        => _ppm.ListerAvecDetailsAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<PpmEntete> ObtenirAsync(string numPpm, CancellationToken cancellationToken = default)
    {
        var entete = await _ppm.GetWithDetailsAsync(numPpm, cancellationToken);
        return entete ?? throw new EntityNotFoundException(nameof(PpmEntete), numPpm);
    }

    /// <inheritdoc />
    public async Task<PpmEntete> CreerAsync(PpmEntete entete, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entete);

        if (string.IsNullOrWhiteSpace(entete.NumPPM))
        {
            throw new DomainException("Le numéro du PPM (NumPPM) est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(entete.AutoriteContractante))
        {
            throw new DomainException("L'autorité contractante est obligatoire.");
        }

        if (await _ppm.GetByIdAsync(entete.NumPPM, cancellationToken) is not null)
        {
            throw new DomainException($"Un PPM avec le numéro « {entete.NumPPM} » existe déjà.");
        }

        entete.DateDerniereMAJ ??= DateTime.Now;
        await _ppm.AddAsync(entete, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entete;
    }

    /// <inheritdoc />
    public async Task ModifierAsync(PpmEntete entete, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entete);

        if (string.IsNullOrWhiteSpace(entete.AutoriteContractante))
        {
            throw new DomainException("L'autorité contractante est obligatoire.");
        }

        var existant = await _ppm.GetByIdAsync(entete.NumPPM, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(PpmEntete), entete.NumPPM);

        // Copie sur l'instance suivie par EF pour éviter les conflits de tracking
        // (l'entité transmise par l'IHM provient généralement d'une liste non suivie).
        existant.AnneeExercice = entete.AnneeExercice;
        existant.AutoriteContractante = entete.AutoriteContractante;
        existant.DateElaboration = entete.DateElaboration;
        existant.NumDerniereMAJ = entete.NumDerniereMAJ;
        existant.DateDerniereMAJ = DateTime.Now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SupprimerAsync(string numPpm, CancellationToken cancellationToken = default)
    {
        var entete = await _ppm.GetWithDetailsAsync(numPpm, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(PpmEntete), numPpm);

        if (entete.Details.Any(detail => detail.Dossiers.Any()))
        {
            throw new DomainException("Ce plan ne peut pas être supprimé : au moins une ligne est utilisée par un dossier DAO.");
        }

        _ppm.Remove(entete);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PpmDetail> AjouterDetailAsync(
        string numPpm,
        PpmDetail detail,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(detail);

        if (string.IsNullOrWhiteSpace(detail.NumPPMDetail))
        {
            throw new DomainException("Le numéro de la ligne de détail (NumPPMDetail) est obligatoire.");
        }

        ValiderLigne(detail);

        var entete = await _ppm.GetWithDetailsAsync(numPpm, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(PpmEntete), numPpm);

        detail.NumPPMEntete = numPpm;
        PreparerBeneficiaires(detail);
        entete.Details.Add(detail);
        entete.DateDerniereMAJ = DateTime.Now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return detail;
    }

    /// <inheritdoc />
    public async Task ModifierDetailAsync(
        string numPpm,
        PpmDetail detail,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(detail);
        ValiderLigne(detail);

        var entete = await _ppm.GetWithDetailsAsync(numPpm, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(PpmEntete), numPpm);

        var existant = entete.Details.FirstOrDefault(d => d.NumPPMDetail == detail.NumPPMDetail)
            ?? throw new EntityNotFoundException(nameof(PpmDetail), detail.NumPPMDetail);

        PreparerBeneficiaires(detail);

        existant.NatureMarche = detail.NatureMarche;
        existant.ObjetProjet = detail.ObjetProjet;
        existant.MontantEstimatif = detail.MontantEstimatif;
        existant.ModePassation = detail.ModePassation;
        existant.SourceFinancement = detail.SourceFinancement;
        existant.Comptes = detail.Comptes;
        existant.DateLancementPrevue = detail.DateLancementPrevue;
        existant.DateOuverturePlisPrevue = detail.DateOuverturePlisPrevue;
        existant.DateAttributionPrevue = detail.DateAttributionPrevue;
        existant.ResponsableProcedure = detail.ResponsableProcedure;

        // Les services bénéficiaires sont remplacés en bloc (les anciens sont supprimés par EF).
        existant.Beneficiaires.Clear();
        foreach (var beneficiaire in detail.Beneficiaires)
        {
            existant.Beneficiaires.Add(new PpmBeneficiaire
            {
                NumPPMDetail = existant.NumPPMDetail,
                Service = beneficiaire.Service,
                Montant = beneficiaire.Montant
            });
        }

        entete.DateDerniereMAJ = DateTime.Now;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SupprimerDetailAsync(
        string numPpm,
        string numDetail,
        CancellationToken cancellationToken = default)
    {
        var entete = await _ppm.GetWithDetailsAsync(numPpm, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(PpmEntete), numPpm);

        var existant = entete.Details.FirstOrDefault(d => d.NumPPMDetail == numDetail)
            ?? throw new EntityNotFoundException(nameof(PpmDetail), numDetail);

        if (existant.Dossiers.Any())
        {
            throw new DomainException("Cette ligne ne peut pas être supprimée : elle est utilisée par un dossier DAO.");
        }

        entete.Details.Remove(existant);
        entete.DateDerniereMAJ = DateTime.Now;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task EnregistrerMiseAJourAsync(
        string numPpm,
        CancellationToken cancellationToken = default)
    {
        var entete = await _ppm.GetByIdAsync(numPpm, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(PpmEntete), numPpm);

        var numeroActuel = 0;
        if (!string.IsNullOrWhiteSpace(entete.NumDerniereMAJ))
        {
            var chiffres = new string(entete.NumDerniereMAJ.Where(char.IsAsciiDigit).ToArray());
            int.TryParse(chiffres, out numeroActuel);
        }

        entete.NumDerniereMAJ = (numeroActuel + 1).ToString();
        entete.DateDerniereMAJ = DateTime.Now;
        _ppm.Update(entete);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Règles communes aux lignes du plan : dates dans l'ordre et services bénéficiaires cohérents.</summary>
    private static void ValiderLigne(PpmDetail detail)
    {
        DateOnly? precedente = null;
        foreach (var date in new[] { detail.DateLancementPrevue, detail.DateOuverturePlisPrevue, detail.DateAttributionPrevue })
        {
            if (date is null)
            {
                continue;
            }

            if (precedente is not null && date < precedente)
            {
                throw new DomainException(
                    "Les dates prévisionnelles doivent suivre l'ordre : lancement, ouverture des plis, attribution.");
            }

            precedente = date;
        }

        foreach (var beneficiaire in detail.Beneficiaires)
        {
            if (string.IsNullOrWhiteSpace(beneficiaire.Service))
            {
                throw new DomainException("Chaque service bénéficiaire doit avoir un nom.");
            }

            if (beneficiaire.Montant < 0)
            {
                throw new DomainException("Le montant d'un service bénéficiaire ne peut pas être négatif.");
            }
        }
    }

    /// <summary>S'il y a des services bénéficiaires, le montant estimatif de la ligne en est la somme.</summary>
    private static void PreparerBeneficiaires(PpmDetail detail)
    {
        foreach (var beneficiaire in detail.Beneficiaires)
        {
            beneficiaire.NumPPMDetail = detail.NumPPMDetail;
            beneficiaire.Service = beneficiaire.Service.Trim();
        }

        if (detail.Beneficiaires.Count > 0)
        {
            detail.MontantEstimatif = detail.Beneficiaires.Sum(b => b.Montant);
        }
    }
}

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
            : await _ppm.GetAllAsync(cancellationToken);

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
        var entete = await _ppm.GetByIdAsync(numPpm, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(PpmEntete), numPpm);

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

        var entete = await _ppm.GetWithDetailsAsync(numPpm, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(PpmEntete), numPpm);

        detail.NumPPMEntete = numPpm;
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

        var entete = await _ppm.GetWithDetailsAsync(numPpm, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(PpmEntete), numPpm);

        var existant = entete.Details.FirstOrDefault(d => d.NumPPMDetail == detail.NumPPMDetail)
            ?? throw new EntityNotFoundException(nameof(PpmDetail), detail.NumPPMDetail);

        existant.NatureMarche = detail.NatureMarche;
        existant.ObjetProjet = detail.ObjetProjet;
        existant.MontantEstimatif = detail.MontantEstimatif;
        existant.ModePassation = detail.ModePassation;
        existant.SourceFinancement = detail.SourceFinancement;

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

        entete.Details.Remove(existant);
        entete.DateDerniereMAJ = DateTime.Now;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ReviserAsync(
        string numPpm,
        string? numMiseAJour,
        CancellationToken cancellationToken = default)
    {
        var entete = await _ppm.GetByIdAsync(numPpm, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(PpmEntete), numPpm);

        entete.NumDerniereMAJ = numMiseAJour;
        entete.DateDerniereMAJ = DateTime.Now;
        _ppm.Update(entete);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

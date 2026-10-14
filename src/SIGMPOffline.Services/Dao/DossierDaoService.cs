using Microsoft.EntityFrameworkCore;
using SIGMPOffline.Data.Repositories.Interfaces;
using SIGMPOffline.Domain.Common;
using SIGMPOffline.Domain.Entities;
using SIGMPOffline.Services.Abstractions;

namespace SIGMPOffline.Services.Dao;

/// <inheritdoc />
public class DossierDaoService : IDossierDaoService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDossierDaoRepository _dossiers;

    public DossierDaoService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _dossiers = unitOfWork.Dossiers;
    }

    /// <inheritdoc />
    public async Task<DossierDao> CreerAsync(DossierDao dossier, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dossier);

        if (string.IsNullOrWhiteSpace(dossier.RefDossier))
        {
            throw new DomainException("La référence du dossier (RefDossier) est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(dossier.NumPPMDetail))
        {
            throw new DomainException("Le dossier doit être rattaché à une ligne de PPM (NumPPMDetail).");
        }

        if (await _dossiers.GetByIdAsync(dossier.RefDossier, cancellationToken) is not null)
        {
            throw new DomainException($"Un dossier avec la référence « {dossier.RefDossier} » existe déjà.");
        }

        // Vérifie l'existence de la ligne PPM parente.
        var ppmDetailExists = await _unitOfWork.Ppm.Query()
            .AnyAsync(x => x.Details.Any(d => d.NumPPMDetail == dossier.NumPPMDetail), cancellationToken);

        if (!ppmDetailExists)
        {
            throw new EntityNotFoundException(nameof(PpmDetail), dossier.NumPPMDetail);
        }

        dossier.DateCreation = dossier.DateCreation == default ? DateTime.Now : dossier.DateCreation;

        await _dossiers.AddAsync(dossier, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return dossier;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DossierDao>> ListerAsync(CancellationToken cancellationToken = default)
        => await _dossiers.GetAllAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<DossierDao> ObtenirAsync(string refDossier, CancellationToken cancellationToken = default)
    {
        var dossier = await _dossiers.GetFullAsync(refDossier, cancellationToken);
        return dossier ?? throw new EntityNotFoundException(nameof(DossierDao), refDossier);
    }

    /// <inheritdoc />
    public async Task<AcheteurDao> AjouterAcheteurAsync(
        string refDossier,
        AcheteurDao acheteur,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(acheteur);

        var dossier = await _dossiers.GetByIdAsync(refDossier, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(DossierDao), refDossier);

        if (string.IsNullOrWhiteSpace(acheteur.RaisonSociale))
        {
            throw new DomainException("La raison sociale de l'acheteur est obligatoire.");
        }

        acheteur.RefDossier = refDossier;
        acheteur.DateAchat = acheteur.DateAchat == default ? DateTime.Now : acheteur.DateAchat;
        dossier.Acheteurs.Add(acheteur);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return acheteur;
    }

    /// <inheritdoc />
    public async Task<Candidat> AjouterCandidatAsync(
        string refDossier,
        Candidat candidat,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidat);

        var dossier = await _dossiers.GetWithCandidatsAsync(refDossier, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(DossierDao), refDossier);

        if (string.IsNullOrWhiteSpace(candidat.NomEntreprise))
        {
            throw new DomainException("Le nom de l'entreprise candidate est obligatoire.");
        }

        candidat.RefDossier = refDossier;
        dossier.Candidats.Add(candidat);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return candidat;
    }

    /// <inheritdoc />
    public async Task<Candidat> DesignerRetenuAsync(
        string refDossier,
        int idCandidat,
        CancellationToken cancellationToken = default)
    {
        var dossier = await _dossiers.GetWithCandidatsAsync(refDossier, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(DossierDao), refDossier);

        var retenu = dossier.Candidats.FirstOrDefault(c => c.IdCandidat == idCandidat)
            ?? throw new EntityNotFoundException(nameof(Candidat), idCandidat);

        // Un seul candidat retenu par dossier.
        foreach (var candidat in dossier.Candidats)
        {
            candidat.EstRetenu = candidat.IdCandidat == idCandidat;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return retenu;
    }

    /// <inheritdoc />
    public async Task ModifierAsync(DossierDao dossier, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dossier);

        if (string.IsNullOrWhiteSpace(dossier.NumPPMDetail))
        {
            throw new DomainException("Le dossier doit être rattaché à une ligne de PPM (NumPPMDetail).");
        }

        if (string.IsNullOrWhiteSpace(dossier.ObjetDossier))
        {
            throw new DomainException("L'objet du dossier est obligatoire.");
        }

        var existant = await _dossiers.GetByIdAsync(dossier.RefDossier, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(DossierDao), dossier.RefDossier);

        // Copie sur l'instance suivie par EF (l'entité de l'IHM provient d'une liste non suivie).
        existant.NumPPMDetail = dossier.NumPPMDetail;
        existant.ObjetDossier = dossier.ObjetDossier;
        existant.CoutCahierCharges = dossier.CoutCahierCharges;
        existant.MontantCaution = dossier.MontantCaution;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SupprimerAsync(string refDossier, CancellationToken cancellationToken = default)
    {
        var existant = await _dossiers.GetByIdAsync(refDossier, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(DossierDao), refDossier);

        _dossiers.Remove(existant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ModifierCandidatAsync(
        string refDossier,
        Candidat candidat,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidat);

        var dossier = await _dossiers.GetWithCandidatsAsync(refDossier, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(DossierDao), refDossier);

        var existant = dossier.Candidats.FirstOrDefault(c => c.IdCandidat == candidat.IdCandidat)
            ?? throw new EntityNotFoundException(nameof(Candidat), candidat.IdCandidat);

        if (string.IsNullOrWhiteSpace(candidat.NomEntreprise))
        {
            throw new DomainException("Le nom de l'entreprise candidate est obligatoire.");
        }

        existant.NomEntreprise = candidat.NomEntreprise;
        existant.NumStat = candidat.NumStat;
        existant.NumNIF = candidat.NumNIF;
        existant.MontantOffre = candidat.MontantOffre;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SupprimerCandidatAsync(
        string refDossier,
        int idCandidat,
        CancellationToken cancellationToken = default)
    {
        var dossier = await _dossiers.GetWithCandidatsAsync(refDossier, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(DossierDao), refDossier);

        var existant = dossier.Candidats.FirstOrDefault(c => c.IdCandidat == idCandidat)
            ?? throw new EntityNotFoundException(nameof(Candidat), idCandidat);

        dossier.Candidats.Remove(existant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ModifierAcheteurAsync(
        string refDossier,
        AcheteurDao acheteur,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(acheteur);

        var dossier = await _dossiers.GetFullAsync(refDossier, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(DossierDao), refDossier);

        var existant = dossier.Acheteurs.FirstOrDefault(a => a.IdAcheteur == acheteur.IdAcheteur)
            ?? throw new EntityNotFoundException(nameof(AcheteurDao), acheteur.IdAcheteur);

        if (string.IsNullOrWhiteSpace(acheteur.RaisonSociale))
        {
            throw new DomainException("La raison sociale de l'acheteur est obligatoire.");
        }

        existant.RaisonSociale = acheteur.RaisonSociale;
        existant.NumStat = acheteur.NumStat;
        existant.NumNIF = acheteur.NumNIF;
        existant.Adresse = acheteur.Adresse;
        existant.DateAchat = acheteur.DateAchat;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SupprimerAcheteurAsync(
        string refDossier,
        int idAcheteur,
        CancellationToken cancellationToken = default)
    {
        var dossier = await _dossiers.GetFullAsync(refDossier, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(DossierDao), refDossier);

        var existant = dossier.Acheteurs.FirstOrDefault(a => a.IdAcheteur == idAcheteur)
            ?? throw new EntityNotFoundException(nameof(AcheteurDao), idAcheteur);

        dossier.Acheteurs.Remove(existant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

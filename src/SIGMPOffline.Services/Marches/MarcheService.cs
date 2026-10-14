using SIGMPOffline.Data.Repositories.Interfaces;
using SIGMPOffline.Domain.Common;
using SIGMPOffline.Domain.Entities;
using SIGMPOffline.Domain.Enums;
using SIGMPOffline.Services.Abstractions;

namespace SIGMPOffline.Services.Marches;

/// <inheritdoc />
public class MarcheService : IMarcheService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMarcheRepository _marches;
    private readonly IDossierDaoRepository _dossiers;

    public MarcheService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _marches = unitOfWork.Marches;
        _dossiers = unitOfWork.Dossiers;
    }

    /// <inheritdoc />
    public async Task<Marche> CreerDepuisDossierAsync(Marche marche, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(marche);

        if (string.IsNullOrWhiteSpace(marche.RefMarche))
        {
            throw new DomainException("La référence du marché (RefMarche) est obligatoire.");
        }

        if (await _marches.GetByIdAsync(marche.RefMarche, cancellationToken) is not null)
        {
            throw new DomainException($"Un marché avec la référence « {marche.RefMarche} » existe déjà.");
        }

        var dossier = await _dossiers.GetFullAsync(marche.RefDossier, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(DossierDao), marche.RefDossier);

        if (dossier.Marche is not null)
        {
            throw new DomainException($"Le dossier « {dossier.RefDossier} » est déjà associé à un marché.");
        }

        // Titulaire : par défaut, le candidat retenu du dossier.
        if (marche.IdCandidatTitulaire is null)
        {
            var retenu = dossier.Candidats.FirstOrDefault(c => c.EstRetenu);
            if (retenu is not null)
            {
                marche.IdCandidatTitulaire = retenu.IdCandidat;
            }
        }

        if (marche.StatutExecution == default)
        {
            marche.StatutExecution = StatutExecution.NonDemarre;
        }

        await _marches.AddAsync(marche, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return marche;
    }

    /// <inheritdoc />
    public async Task<Marche> ObtenirAsync(string refMarche, CancellationToken cancellationToken = default)
    {
        var marche = await _marches.GetWithExecutionAsync(refMarche, cancellationToken);
        return marche ?? throw new EntityNotFoundException(nameof(Marche), refMarche);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Marche>> ListerAsync(CancellationToken cancellationToken = default)
        => _marches.ListWithExecutionAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<ElementMarche> AjouterElementAsync(
        string refMarche,
        ElementMarche element,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(element);

        if (string.IsNullOrWhiteSpace(element.CodeElement))
        {
            throw new DomainException("Le code de l'élément (CodeElement) est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(element.Libelle))
        {
            throw new DomainException("Le libellé de l'élément est obligatoire.");
        }

        if (element.Quantite < 0 || element.PrixUnitaire < 0)
        {
            throw new DomainException("Le prix unitaire et la quantité ne peuvent pas être négatifs.");
        }

        var marche = await _marches.GetWithExecutionAsync(refMarche, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Marche), refMarche);

        if (marche.Elements.Any(e => e.CodeElement == element.CodeElement))
        {
            throw new DomainException($"L'élément « {element.CodeElement} » existe déjà pour ce marché.");
        }

        element.RefMarche = refMarche;
        marche.Elements.Add(element);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return element;
    }

    /// <inheritdoc />
    public async Task RecalculerMontantsAsync(
        string refMarche,
        decimal tauxTVA = 0.18m,
        CancellationToken cancellationToken = default)
    {
        var marche = await _marches.GetWithExecutionAsync(refMarche, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Marche), refMarche);

        var totalHt = marche.Elements.Sum(e => e.PrixUnitaire * e.Quantite);
        marche.MontantHT = decimal.Round(totalHt, 2, MidpointRounding.AwayFromZero);
        marche.MontantTTC = decimal.Round(totalHt * (1m + tauxTVA), 2, MidpointRounding.AwayFromZero);

        _marches.Update(marche);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ModifierAsync(Marche marche, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(marche);

        if (string.IsNullOrWhiteSpace(marche.ObjetMarche))
        {
            throw new DomainException("L'objet du marché est obligatoire.");
        }

        if (marche.DelaiExecutionJours < 0)
        {
            throw new DomainException("Le délai d'exécution ne peut pas être négatif.");
        }

        if (marche.MontantHT < 0 || marche.MontantTTC < 0)
        {
            throw new DomainException("Les montants ne peuvent pas être négatifs.");
        }

        var existant = await _marches.GetWithExecutionAsync(marche.RefMarche, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Marche), marche.RefMarche);

        // Copie sur l'instance suivie par EF pour éviter les conflits de tracking
        // (l'entité transmise par l'IHM provient d'une liste non suivie).
        existant.ObjetMarche = marche.ObjetMarche;
        existant.MontantHT = marche.MontantHT;
        existant.MontantTTC = marche.MontantTTC;
        existant.DelaiExecutionJours = marche.DelaiExecutionJours;
        existant.IdCandidatTitulaire = marche.IdCandidatTitulaire;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SupprimerAsync(string refMarche, CancellationToken cancellationToken = default)
    {
        var existant = await _marches.GetWithExecutionAsync(refMarche, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Marche), refMarche);

        _marches.Remove(existant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ModifierElementAsync(
        string refMarche,
        ElementMarche element,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(element);

        if (string.IsNullOrWhiteSpace(element.Libelle))
        {
            throw new DomainException("Le libellé de l'élément est obligatoire.");
        }

        if (element.Quantite < 0 || element.PrixUnitaire < 0)
        {
            throw new DomainException("Le prix unitaire et la quantité ne peuvent pas être négatifs.");
        }

        var marche = await _marches.GetWithExecutionAsync(refMarche, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Marche), refMarche);

        var existant = marche.Elements.FirstOrDefault(e => e.CodeElement == element.CodeElement)
            ?? throw new EntityNotFoundException(nameof(ElementMarche), element.CodeElement);

        existant.Libelle = element.Libelle;
        existant.PrixUnitaire = element.PrixUnitaire;
        existant.Quantite = element.Quantite;
        existant.Unite = element.Unite;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SupprimerElementAsync(
        string refMarche,
        string codeElement,
        CancellationToken cancellationToken = default)
    {
        var marche = await _marches.GetWithExecutionAsync(refMarche, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Marche), refMarche);

        var existant = marche.Elements.FirstOrDefault(e => e.CodeElement == codeElement)
            ?? throw new EntityNotFoundException(nameof(ElementMarche), codeElement);

        marche.Elements.Remove(existant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

using SIGMPOffline.Data.Repositories.Interfaces;
using SIGMPOffline.Domain.Common;
using SIGMPOffline.Domain.Entities;
using SIGMPOffline.Domain.Enums;
using SIGMPOffline.Services.Abstractions;
using SIGMPOffline.Services.Models;

namespace SIGMPOffline.Services.Suivi;

/// <inheritdoc />
public class SuiviExecutionService : ISuiviExecutionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMarcheRepository _marches;

    public SuiviExecutionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _marches = unitOfWork.Marches;
    }

    /// <inheritdoc />
    public async Task<SituationMarche> ObtenirSituationAsync(
        string refMarche,
        CancellationToken cancellationToken = default)
    {
        var marche = await ChargerMarcheAsync(refMarche, cancellationToken);

        var aujourdhui = DateOnly.FromDateTime(DateTime.Today);
        var dateLimite = MarcheDelais.CalculerDateLimiteExecution(
            marche.Suivi?.DateOS,
            marche.DelaiExecutionJours,
            marche.Avenants);

        var estCloture = marche.Suivi?.EstCloture ?? false;

        return new SituationMarche
        {
            RefMarche = marche.RefMarche,
            ObjetMarche = marche.ObjetMarche,
            DateOS = marche.Suivi?.DateOS,
            RefTef = marche.Suivi?.RefTEF,
            DateLimiteExecution = dateLimite,
            JoursRestants = MarcheDelais.JoursRestants(dateLimite, aujourdhui),
            EstEnRetard = MarcheDelais.EstEnRetard(dateLimite, estCloture, aujourdhui),
            Statut = marche.StatutExecution,
            EstCloture = estCloture
        };
    }

    /// <inheritdoc />
    public async Task<SuiviExecution> ObtenirOuCreerSuiviAsync(
        string refMarche,
        CancellationToken cancellationToken = default)
    {
        var marche = await ChargerMarcheAsync(refMarche, cancellationToken);

        if (marche.Suivi is null)
        {
            marche.Suivi = new SuiviExecution { RefMarche = refMarche };
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return marche.Suivi;
    }

    /// <inheritdoc />
    public async Task EnregistrerTefAsync(
        string refMarche,
        string refTef,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refTef))
        {
            throw new DomainException("La référence du TEF est obligatoire.");
        }

        var suivi = await ObtenirOuCreerSuiviAsync(refMarche, cancellationToken);
        suivi.RefTEF = refTef;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task EmettreOrdreServiceAsync(
        string refMarche,
        DateOnly dateOs,
        CancellationToken cancellationToken = default)
    {
        var marche = await ChargerMarcheAsync(refMarche, cancellationToken);

        if (marche.Suivi?.EstCloture == true)
        {
            throw new DomainException("Impossible d'émettre un OS sur un marché clôturé.");
        }

        marche.Suivi ??= new SuiviExecution { RefMarche = refMarche };
        marche.Suivi.DateOS = dateOs;

        if (marche.StatutExecution is StatutExecution.NonDemarre)
        {
            marche.StatutExecution = StatutExecution.EnCours;
        }

        _marches.Update(marche);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Avenant> AjouterAvenantAsync(
        string refMarche,
        Avenant avenant,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(avenant);

        if (string.IsNullOrWhiteSpace(avenant.NumAvenant))
        {
            throw new DomainException("Le numéro de l'avenant (NumAvenant) est obligatoire.");
        }

        var marche = await ChargerMarcheAsync(refMarche, cancellationToken);

        if (marche.Suivi?.EstCloture == true)
        {
            throw new DomainException("Impossible d'ajouter un avenant à un marché clôturé.");
        }

        avenant.RefMarche = refMarche;
        avenant.DateAvenant = avenant.DateAvenant == default
            ? DateOnly.FromDateTime(DateTime.Today)
            : avenant.DateAvenant;

        marche.Avenants.Add(avenant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return avenant;
    }

    /// <inheritdoc />
    public async Task ModifierAvenantAsync(
        string refMarche,
        Avenant avenant,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(avenant);

        if (string.IsNullOrWhiteSpace(avenant.NumAvenant))
        {
            throw new DomainException("Le numéro de l'avenant (NumAvenant) est obligatoire.");
        }

        var marche = await ChargerMarcheAsync(refMarche, cancellationToken);

        if (marche.Suivi?.EstCloture == true)
        {
            throw new DomainException("Impossible de modifier un avenant d'un marché clôturé.");
        }

        var existant = marche.Avenants.FirstOrDefault(a => a.IdAvenant == avenant.IdAvenant)
            ?? throw new EntityNotFoundException(nameof(Avenant), avenant.IdAvenant);

        existant.NumAvenant = avenant.NumAvenant;
        existant.Motif = avenant.Motif;
        existant.VariationMontant = avenant.VariationMontant;
        existant.ProlongationDelaiJours = avenant.ProlongationDelaiJours;
        existant.DateAvenant = avenant.DateAvenant;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SupprimerAvenantAsync(
        string refMarche,
        int idAvenant,
        CancellationToken cancellationToken = default)
    {
        var marche = await ChargerMarcheAsync(refMarche, cancellationToken);

        if (marche.Suivi?.EstCloture == true)
        {
            throw new DomainException("Impossible de supprimer un avenant d'un marché clôturé.");
        }

        var existant = marche.Avenants.FirstOrDefault(a => a.IdAvenant == idAvenant)
            ?? throw new EntityNotFoundException(nameof(Avenant), idAvenant);

        marche.Avenants.Remove(existant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task EnregistrerReceptionProvisoireAsync(
        string refMarche,
        DateOnly date,
        CancellationToken cancellationToken = default)
        => MettreAJourReceptionAsync(refMarche, date, definitive: false, cancellationToken);

    /// <inheritdoc />
    public Task EnregistrerReceptionDefinitiveAsync(
        string refMarche,
        DateOnly date,
        CancellationToken cancellationToken = default)
        => MettreAJourReceptionAsync(refMarche, date, definitive: true, cancellationToken);

    /// <inheritdoc />
    public async Task CloturerAsync(
        string refMarche,
        DateOnly dateCloture,
        CancellationToken cancellationToken = default)
    {
        var marche = await ChargerMarcheAsync(refMarche, cancellationToken);

        if (marche.Suivi?.EstCloture == true)
        {
            throw new DomainException("Ce marché est déjà clôturé.");
        }

        marche.Suivi ??= new SuiviExecution { RefMarche = refMarche };
        marche.Suivi.EstCloture = true;
        marche.Suivi.DateCloture = dateCloture;
        marche.StatutExecution = StatutExecution.Cloture;

        _marches.Update(marche);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task MettreAJourReceptionAsync(
        string refMarche,
        DateOnly date,
        bool definitive,
        CancellationToken cancellationToken)
    {
        var marche = await ChargerMarcheAsync(refMarche, cancellationToken);

        if (marche.Suivi?.EstCloture == true)
        {
            throw new DomainException("Impossible d'enregistrer une réception sur un marché clôturé.");
        }

        marche.Suivi ??= new SuiviExecution { RefMarche = refMarche };

        if (definitive)
        {
            marche.Suivi.DateReceptionDefinitive = date;
            marche.StatutExecution = StatutExecution.ReceptionDefinitive;
        }
        else
        {
            marche.Suivi.DateReceptionProvisoire = date;
            marche.StatutExecution = StatutExecution.ReceptionProvisoire;
        }

        _marches.Update(marche);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Marche> ChargerMarcheAsync(string refMarche, CancellationToken cancellationToken)
        => await _marches.GetWithExecutionAsync(refMarche, cancellationToken)
           ?? throw new EntityNotFoundException(nameof(Marche), refMarche);
}

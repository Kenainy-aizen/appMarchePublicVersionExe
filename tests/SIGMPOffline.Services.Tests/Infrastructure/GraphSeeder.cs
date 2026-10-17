using SIGMPOffline.Domain.Entities;
using SIGMPOffline.Domain.Enums;
using SIGMPOffline.Services.Dao;
using SIGMPOffline.Services.Marches;
using SIGMPOffline.Services.Ppm;
using SIGMPOffline.Services.Tests.Infrastructure;

namespace SIGMPOffline.Services.Tests.Infrastructure;

/// <summary>Construit un graphe complet PPM → DAO → Marché pour les tests.</summary>
public static class GraphSeeder
{
    /// <summary>
    /// Crée la chaîne complète (PPM → détail → dossier → candidat retenu → marché).
    /// Les identifiants dérivent de <paramref name="refMarche"/> pour rester uniques
    /// lorsqu'on sème plusieurs marchés dans la même base.
    /// </summary>
    public static async Task<string> SeedMarcheAsync(
        TestDatabase db,
        string refMarche = "M-001",
        int delaiJours = 30)
    {
        var ppm = new PpmService(db.UnitOfWork);
        var dao = new DossierDaoService(db.UnitOfWork);
        var marches = new MarcheService(db.UnitOfWork);

        var numPpm = $"PPM-{refMarche}";
        var numDetail = $"D-{refMarche}";
        var refDossier = $"DOS-{refMarche}";

        await ppm.CreerAsync(new PpmEntete
        {
            NumPPM = numPpm,
            AnneeExercice = 2026,
            AutoriteContractante = "PRMP Test",
            DateElaboration = new DateOnly(2026, 1, 1)
        });

        await ppm.AjouterDetailAsync(numPpm, new PpmDetail
        {
            NumPPMDetail = numDetail,
            NatureMarche = NatureMarche.Travaux,
            ObjetProjet = "Réhabilitation route",
            MontantEstimatif = 1_000_000m,
            ModePassation = ModePassation.AppelOffresOuvert
        });

        await dao.CreerAsync(new DossierDao
        {
            RefDossier = refDossier,
            NumPPMDetail = numDetail,
            ObjetDossier = "DAO réhabilitation route",
            CoutCahierCharges = 50_000m,
            MontantCaution = 100_000m
        });

        var numStatTitulaire = $"STAT-{refMarche}";

        var candidat = await dao.AjouterCandidatAsync(refDossier, new Candidat
        {
            NomEntreprise = "ACME BTP",
            NumStat = numStatTitulaire,
            NumNIF = "NIF-1",
            MontantOffre = 900_000m
        });

        await dao.DesignerRetenuAsync(refDossier, candidat.IdCandidat);

        // L'acheteur titulaire : la clé étrangère du marché pointe sur son numéro de statistique.
        await dao.AjouterAcheteurAsync(refDossier, new AcheteurDao
        {
            RaisonSociale = "ACME BTP",
            NumStat = numStatTitulaire,
            NumNIF = "NIF-1",
            DateAchat = new DateTime(2026, 1, 5)
        });

        await marches.CreerDepuisDossierAsync(new Marche
        {
            RefMarche = refMarche,
            RefDossier = refDossier,
            ObjetMarche = "Réhabilitation route bitumée",
            MontantHT = 900_000m,
            MontantTTC = 1_062_000m,
            DelaiExecutionJours = delaiJours,
            NumStatTitulaire = numStatTitulaire
        });

        return refMarche;
    }
}

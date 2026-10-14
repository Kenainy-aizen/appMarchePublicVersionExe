using Microsoft.EntityFrameworkCore;
using SIGMPOffline.Domain.Entities;
using SIGMPOffline.Domain.Enums;

namespace SIGMPOffline.Data.Database;

/// <summary>
/// Insère un jeu de données de démonstration au premier lancement (base vide).
/// Idempotent : ne fait rien si des données existent déjà.
/// </summary>
public static class DbSeeder
{
    /// <summary>Sème les données de démo. Retourne <c>true</c> si des données ont été créées.</summary>
    public static async Task<bool> SeedAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (await context.PpmEntetes.AnyAsync(cancellationToken))
        {
            return false; // Déjà initialisé.
        }

        var entete = new PpmEntete
        {
            NumPPM = "PPM-2026",
            AnneeExercice = 2026,
            AutoriteContractante = "PRMP - Ministère de l'Équipement",
            DateElaboration = new DateOnly(2025, 11, 15),
            DateDerniereMAJ = DateTime.Now,
            NumDerniereMAJ = "MAJ-01"
        };

        entete.Details.Add(new PpmDetail
        {
            NumPPMDetail = "2026-001",
            NatureMarche = NatureMarche.Travaux,
            ObjetProjet = "Réhabilitation de la route RN-15 (30 km)",
            MontantEstimatif = 2_500_000_000m,
            ModePassation = ModePassation.AppelOffresOuvert,
            SourceFinancement = "Budget National"
        });

        entete.Details.Add(new PpmDetail
        {
            NumPPMDetail = "2026-002",
            NatureMarche = NatureMarche.Fournitures,
            ObjetProjet = "Acquisition de 20 véhicules 4x4",
            MontantEstimatif = 850_000_000m,
            ModePassation = ModePassation.AppelOffresOuvert,
            SourceFinancement = "Budget National"
        });

        entete.Details.Add(new PpmDetail
        {
            NumPPMDetail = "2026-003",
            NatureMarche = NatureMarche.PrestationsIntellectuelles,
            ObjetProjet = "Audit technique du parc informatique",
            MontantEstimatif = 45_000_000m,
            ModePassation = ModePassation.DemandeCotation,
            SourceFinancement = "Bailleur"
        });

        context.PpmEntetes.Add(entete);

        // ---- Marché 1 : en retard (OS ancien, non clôturé) ----
        var dos1 = NouveauDossier("DAO-2026-001", "2026-001", new DateTime(2026, 1, 10),
            "DAO réhabilitation RN-15", 500_000m, 25_000_000m);

        var c1 = nouveauCandidat("ENTREPRISE ROUTES SA", "STAT-1001", "NIF-1001", 2_350_000_000m, retenu: true);
        var c2 = nouveauCandidat("BTP CONSTRUCTIONS SARL", "STAT-1002", "NIF-1002", 2_480_000_000m, retenu: false);
        var c3 = nouveauCandidat("TRAVAUX & GENIE CIVIL", "STAT-1003", "NIF-1003", 2_600_000_000m, retenu: false);
        dos1.Candidats.Add(c1);
        dos1.Candidats.Add(c2);
        dos1.Candidats.Add(c3);

        var m1 = new Marche
        {
            RefMarche = "M-2026-001",
            RefDossier = "DAO-2026-001",
            ObjetMarche = "Réhabilitation de la route RN-15 (30 km)",
            MontantHT = 2_350_000_000m,
            MontantTTC = 2_773_000_000m,
            DelaiExecutionJours = 120,
            StatutExecution = StatutExecution.EnCours,
            Titulaire = c1,
            Suivi = new SuiviExecution
            {
                RefMarche = "M-2026-001",
                RefTEF = "TEF-2026-0001",
                DateOS = new DateOnly(2026, 1, 20)
            }
        };
        m1.Elements.Add(nouvelElement("EL-001", "M-2026-001", "Terrassement général", 45_000m, 30_000m, "m3"));
        m1.Elements.Add(nouvelElement("EL-002", "M-2026-001", "Couche de base grave concassée", 32_000m, 25_000m, "m3"));
        m1.Elements.Add(nouvelElement("EL-003", "M-2026-001", "Revêtement bitumineux", 58_000m, 18_000m, "m2"));
        m1.Avenants.Add(new Avenant
        {
            RefMarche = "M-2026-001",
            NumAvenant = "AV-01",
            Motif = "Travaux supplémentaires (assainissement)",
            VariationMontant = 120_000_000m,
            ProlongationDelaiJours = 30,
            DateAvenant = new DateOnly(2026, 4, 15)
        });
        dos1.Marche = m1;
        context.DossiersDao.Add(dos1);

        // ---- Marché 2 : en cours, dans les délais ----
        var dos2 = NouveauDossier("DAO-2026-002", "2026-002", new DateTime(2026, 6, 5),
            "DAO acquisition véhicules", 200_000m, 10_000_000m);

        var c4 = nouveauCandidat("AUTO DISTRIBUTION SA", "STAT-2001", "NIF-2001", 812_000_000m, retenu: true);
        dos2.Candidats.Add(c4);

        var m2 = new Marche
        {
            RefMarche = "M-2026-002",
            RefDossier = "DAO-2026-002",
            ObjetMarche = "Acquisition de 20 véhicules 4x4",
            MontantHT = 812_000_000m,
            MontantTTC = 958_160_000m,
            DelaiExecutionJours = 180,
            StatutExecution = StatutExecution.EnCours,
            Titulaire = c4,
            Suivi = new SuiviExecution
            {
                RefMarche = "M-2026-002",
                RefTEF = "TEF-2026-0002",
                DateOS = new DateOnly(2026, 9, 1)
            }
        };
        m2.Elements.Add(nouvelElement("EL-101", "M-2026-002", "Véhicule 4x4 pick-up", 40_600_000m, 20m, "unité"));
        dos2.Marche = m2;
        context.DossiersDao.Add(dos2);

        // ---- Marché 3 : clôturé ----
        var dos3 = NouveauDossier("DAO-2026-003", "2026-003", new DateTime(2026, 1, 25),
            "DAO audit informatique", 100_000m, 2_000_000m);

        var c5 = nouveauCandidat("CONSEIL & SYSTEMES", "STAT-3001", "NIF-3001", 43_000_000m, retenu: true);
        dos3.Candidats.Add(c5);

        var m3 = new Marche
        {
            RefMarche = "M-2026-003",
            RefDossier = "DAO-2026-003",
            ObjetMarche = "Audit technique du parc informatique",
            MontantHT = 43_000_000m,
            MontantTTC = 50_740_000m,
            DelaiExecutionJours = 60,
            StatutExecution = StatutExecution.Cloture,
            Titulaire = c5,
            Suivi = new SuiviExecution
            {
                RefMarche = "M-2026-003",
                RefTEF = "TEF-2026-0003",
                DateOS = new DateOnly(2026, 2, 1),
                DateReceptionProvisoire = new DateOnly(2026, 3, 25),
                DateReceptionDefinitive = new DateOnly(2026, 4, 20),
                EstCloture = true,
                DateCloture = new DateOnly(2026, 5, 10)
            }
        };
        m3.Elements.Add(nouvelElement("EL-201", "M-2026-003", "Audit et rapport", 43_000_000m, 1m, "forfait"));
        dos3.Marche = m3;
        context.DossiersDao.Add(dos3);

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static DossierDao NouveauDossier(
        string reference,
        string numPpmDetail,
        DateTime dateCreation,
        string objet,
        decimal coutCahierCharges,
        decimal montantCaution)
        => new()
        {
            RefDossier = reference,
            NumPPMDetail = numPpmDetail,
            DateCreation = dateCreation,
            ObjetDossier = objet,
            CoutCahierCharges = coutCahierCharges,
            MontantCaution = montantCaution
        };

    private static Candidat nouveauCandidat(
        string nom,
        string numStat,
        string numNif,
        decimal montantOffre,
        bool retenu)
        => new()
        {
            NomEntreprise = nom,
            NumStat = numStat,
            NumNIF = numNif,
            MontantOffre = montantOffre,
            EstRetenu = retenu
        };

    private static ElementMarche nouvelElement(
        string code,
        string refMarche,
        string libelle,
        decimal prixUnitaire,
        decimal quantite,
        string unite)
        => new()
        {
            CodeElement = code,
            RefMarche = refMarche,
            Libelle = libelle,
            PrixUnitaire = prixUnitaire,
            Quantite = quantite,
            Unite = unite
        };
}

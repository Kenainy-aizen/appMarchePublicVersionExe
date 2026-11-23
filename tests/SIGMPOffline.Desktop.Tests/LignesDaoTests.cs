using SIGMPOffline.Desktop.ViewModels;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Desktop.Tests;

/// <summary>Tests de la numérotation des candidats et acheteurs du module DAO.</summary>
public class LignesDaoTests
{
    [Fact]
    public void Numeroter_candidats_attribue_1_2_3_dans_l_ordre_de_creation()
    {
        var candidats = new[]
        {
            new Candidat { IdCandidat = 10, NomEntreprise = "Troisième", MontantOffre = 3000m },
            new Candidat { IdCandidat = 2, NomEntreprise = "Premier", MontantOffre = 1000m },
            new Candidat { IdCandidat = 7, NomEntreprise = "Deuxième", MontantOffre = 2000m, EstRetenu = true }
        };

        var lignes = LignesDao.Numeroter(candidats);

        Assert.Equal([1, 2, 3], lignes.Select(ligne => ligne.Numero));
        Assert.Equal(["Premier", "Deuxième", "Troisième"], lignes.Select(ligne => ligne.NomEntreprise));
        Assert.Equal([1000m, 2000m, 3000m], lignes.Select(ligne => ligne.MontantOffre));
        Assert.True(lignes[1].EstRetenu);
    }

    [Fact]
    public void Numeroter_acheteurs_attribue_1_2_3_dans_l_ordre_de_creation()
    {
        var acheteurs = new[]
        {
            new AcheteurDao { IdAcheteur = 9, RaisonSociale = "Troisième", NumStat = "STAT-3" },
            new AcheteurDao { IdAcheteur = 1, RaisonSociale = "Premier", NumStat = "STAT-1" },
            new AcheteurDao { IdAcheteur = 4, RaisonSociale = "Deuxième", NumStat = "STAT-2" }
        };

        var lignes = LignesDao.Numeroter(acheteurs);

        Assert.Equal([1, 2, 3], lignes.Select(ligne => ligne.Numero));
        Assert.Equal(["Premier", "Deuxième", "Troisième"], lignes.Select(ligne => ligne.RaisonSociale));
        Assert.Equal(["STAT-1", "STAT-2", "STAT-3"], lignes.Select(ligne => ligne.NumStat));
    }

    [Fact]
    public void Numeroter_une_liste_vide_ne_produit_aucune_ligne()
    {
        Assert.Empty(LignesDao.Numeroter(Array.Empty<Candidat>()));
        Assert.Empty(LignesDao.Numeroter(Array.Empty<AcheteurDao>()));
    }
}

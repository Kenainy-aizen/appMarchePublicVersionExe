using SIGMPOffline.Domain.Common;
using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Domain.Tests;

/// <summary>Tests des calculs de délais métier (<see cref="MarcheDelais"/>).</summary>
public class MarcheDelaisTests
{
    [Fact]
    public void CalculerDateLimiteExecution_sans_OS_retourne_null()
    {
        var resultat = MarcheDelais.CalculerDateLimiteExecution(null, 30);
        Assert.Null(resultat);
    }

    [Fact]
    public void CalculerDateLimiteExecution_ajoute_le_delai_contractuel()
    {
        var os = new DateOnly(2026, 1, 1);

        var resultat = MarcheDelais.CalculerDateLimiteExecution(os, 30);

        Assert.Equal(new DateOnly(2026, 1, 31), resultat);
    }

    [Fact]
    public void CalculerDateLimiteExecution_ajoute_les_prolongations_des_avenants()
    {
        var os = new DateOnly(2026, 1, 1);
        var avenants = new List<Avenant>
        {
            new() { ProlongationDelaiJours = 15 },
            new() { ProlongationDelaiJours = 10 }
        };

        var resultat = MarcheDelais.CalculerDateLimiteExecution(os, 30, avenants);

        Assert.Equal(new DateOnly(2026, 2, 25), resultat);
    }

    [Fact]
    public void EstEnRetard_vrai_quand_la_date_limite_est_depassee_et_non_cloture()
    {
        var aujourdhui = new DateOnly(2026, 3, 1);

        Assert.True(MarcheDelais.EstEnRetard(new DateOnly(2026, 2, 1), estCloture: false, aujourdhui));
    }

    [Fact]
    public void EstEnRetard_faux_quand_le_marche_est_cloture()
    {
        var aujourdhui = new DateOnly(2026, 3, 1);

        Assert.False(MarcheDelais.EstEnRetard(new DateOnly(2026, 2, 1), estCloture: true, aujourdhui));
    }

    [Fact]
    public void JoursRestants_retourne_un_nombre_negatif_si_depasse()
    {
        var aujourdhui = new DateOnly(2026, 3, 10);

        var resultat = MarcheDelais.JoursRestants(new DateOnly(2026, 3, 1), aujourdhui);

        Assert.Equal(-9, resultat);
    }
}

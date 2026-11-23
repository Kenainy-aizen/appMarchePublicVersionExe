using SIGMPOffline.Domain.Entities;

namespace SIGMPOffline.Desktop.ViewModels;

/// <summary>Ligne d'affichage d'un candidat, avec son numéro d'ordre dans la liste.</summary>
public sealed class LigneCandidat(int numero, Candidat candidat)
{
    /// <summary>Numéro d'ordre affiché (1, 2, 3…).</summary>
    public int Numero { get; } = numero;

    /// <summary>Candidat sous-jacent.</summary>
    public Candidat Candidat { get; } = candidat;

    /// <summary>Nom de l'entreprise candidate.</summary>
    public string NomEntreprise => Candidat.NomEntreprise;

    /// <summary>Montant de l'offre soumise.</summary>
    public decimal MontantOffre => Candidat.MontantOffre;

    /// <summary>Indique si le candidat a été retenu.</summary>
    public bool EstRetenu => Candidat.EstRetenu;
}

/// <summary>Ligne d'affichage d'un acheteur, avec son numéro d'ordre dans la liste.</summary>
public sealed class LigneAcheteur(int numero, AcheteurDao acheteur)
{
    /// <summary>Numéro d'ordre affiché (1, 2, 3…).</summary>
    public int Numero { get; } = numero;

    /// <summary>Acheteur sous-jacent.</summary>
    public AcheteurDao Acheteur { get; } = acheteur;

    /// <summary>Raison sociale de l'entreprise.</summary>
    public string RaisonSociale => Acheteur.RaisonSociale;

    /// <summary>Numéro de statistique.</summary>
    public string NumStat => Acheteur.NumStat;

    /// <summary>Date d'achat du cahier des charges.</summary>
    public DateTime DateAchat => Acheteur.DateAchat;
}

/// <summary>Fabrique les lignes numérotées des listes du module DAO.</summary>
public static class LignesDao
{
    /// <summary>Numérote les candidats de 1 à N, dans l'ordre de création.</summary>
    public static IReadOnlyList<LigneCandidat> Numeroter(IEnumerable<Candidat> candidats)
        => candidats
            .OrderBy(candidat => candidat.IdCandidat)
            .Select((candidat, index) => new LigneCandidat(index + 1, candidat))
            .ToList();

    /// <summary>Numérote les acheteurs de 1 à N, dans l'ordre de création.</summary>
    public static IReadOnlyList<LigneAcheteur> Numeroter(IEnumerable<AcheteurDao> acheteurs)
        => acheteurs
            .OrderBy(acheteur => acheteur.IdAcheteur)
            .Select((acheteur, index) => new LigneAcheteur(index + 1, acheteur))
            .ToList();
}

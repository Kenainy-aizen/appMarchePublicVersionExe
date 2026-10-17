namespace SIGMPOffline.Desktop.ViewModels;

/// <summary>Option de liste déroulante représentant un dossier DAO (référence + objet).</summary>
public sealed record OptionDossier(string RefDossier, string Libelle)
{
    /// <inheritdoc />
    public override string ToString() => Libelle;
}

/// <summary>Option de liste déroulante représentant un acheteur titulaire possible.</summary>
public sealed record OptionTitulaire(string NumStat, string Libelle)
{
    /// <inheritdoc />
    public override string ToString() => Libelle;
}

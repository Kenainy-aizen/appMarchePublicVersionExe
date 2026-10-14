namespace SIGMPOffline.Desktop.ViewModels;

/// <summary>Option de liste déroulante représentant un dossier DAO (référence + objet).</summary>
public sealed record OptionDossier(string RefDossier, string Libelle)
{
    /// <inheritdoc />
    public override string ToString() => Libelle;
}

/// <summary>Option de liste déroulante représentant un candidat titulaire possible.</summary>
public sealed record OptionCandidat(int IdCandidat, string Libelle, bool EstRetenu = false)
{
    /// <inheritdoc />
    public override string ToString() => EstRetenu ? $"{Libelle} (retenu)" : Libelle;
}

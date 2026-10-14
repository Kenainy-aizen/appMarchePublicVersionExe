namespace SIGMPOffline.Desktop.ViewModels;

/// <summary>Option de liste déroulante représentant une ligne de PPM (n° + objet).</summary>
public sealed record OptionLignePpm(string NumPPMDetail, string Libelle)
{
    /// <inheritdoc />
    public override string ToString() => Libelle;
}

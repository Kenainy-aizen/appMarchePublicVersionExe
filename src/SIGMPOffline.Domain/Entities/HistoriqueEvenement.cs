namespace SIGMPOffline.Domain.Entities;

/// <summary>Trace immuable d'une opération utilisateur ou d'une modification métier.</summary>
public class HistoriqueEvenement
{
    public long IdHistorique { get; set; }
    public DateTime DateEvenement { get; set; }
    public int? IdUtilisateur { get; set; }
    public string NomUtilisateur { get; set; } = string.Empty;
    public int? IdProvince { get; set; }
    public string? NomProvince { get; set; }
    public string Action { get; set; } = string.Empty;
    public string TypeEntite { get; set; } = string.Empty;
    public string? ReferenceEntite { get; set; }
    public string? Detail { get; set; }
}

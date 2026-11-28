namespace SIGMPOffline.Domain.Entities;

/// <summary>Droit d'un compte utilisateur sur une province donnée.</summary>
public class PermissionProvince
{
    public int IdUtilisateur { get; set; }
    public int IdProvince { get; set; }
    public bool PeutModifier { get; set; }
    public string LibelleDroit => PeutModifier ? "Modification" : "Consultation seule";

    public Utilisateur? Utilisateur { get; set; }
    public Province? Province { get; set; }
}

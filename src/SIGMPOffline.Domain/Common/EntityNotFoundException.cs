namespace SIGMPOffline.Domain.Common;

/// <summary>Levée lorsqu'une entité attendue est introuvable en base.</summary>
public class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityName, object key)
        : base($"Aucun(e) {entityName} trouvé(e) pour la clé « {key} ».")
    {
        EntityName = entityName;
        Key = key;
    }

    /// <summary>Nom de l'entité recherchée.</summary>
    public string EntityName { get; }

    /// <summary>Clé utilisée pour la recherche.</summary>
    public object Key { get; }
}

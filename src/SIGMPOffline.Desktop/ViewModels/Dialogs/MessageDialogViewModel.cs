namespace SIGMPOffline.Desktop.ViewModels.Dialogs;

/// <summary>Boîte de dialogue simple : message d'information ou demande de confirmation.</summary>
public sealed class MessageDialogViewModel : DialogViewModelBase
{
    /// <summary>Crée un message.</summary>
    /// <param name="titre">Titre de la fenêtre.</param>
    /// <param name="message">Texte du message.</param>
    /// <param name="estQuestion">Vrai pour une confirmation (boutons Confirmer/Annuler).</param>
    public MessageDialogViewModel(string titre, string message, bool estQuestion)
    {
        Titre = titre;
        Message = message;
        EstQuestion = estQuestion;
    }

    /// <inheritdoc />
    public override string Titre { get; }

    /// <summary>Texte du message.</summary>
    public string Message { get; }

    /// <summary>Vrai s'il s'agit d'une question (confirmation).</summary>
    public bool EstQuestion { get; }

    /// <summary>Libellé du bouton principal.</summary>
    public string LibelleValider => EstQuestion ? "Confirmer" : "OK";

    /// <summary>Le bouton d'annulation n'apparaît que pour une confirmation.</summary>
    public bool AfficherAnnuler => EstQuestion;
}

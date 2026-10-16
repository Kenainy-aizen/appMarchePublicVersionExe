using Avalonia.Media;

namespace SIGMPOffline.Desktop.ViewModels;

/// <summary>Entrée du menu latéral : un titre, une icône vectorielle et le ViewModel de la page.</summary>
public sealed class NavigationItem
{
    public NavigationItem(string titre, Geometry icone, ViewModelBase viewModel)
    {
        Titre = titre;
        Icone = icone;
        ViewModel = viewModel;
    }

    /// <summary>Libellé affiché dans le menu.</summary>
    public string Titre { get; }

    /// <summary>Icône vectorielle affichée dans le menu.</summary>
    public Geometry Icone { get; }

    /// <summary>ViewModel de la page associée.</summary>
    public ViewModelBase ViewModel { get; }
}

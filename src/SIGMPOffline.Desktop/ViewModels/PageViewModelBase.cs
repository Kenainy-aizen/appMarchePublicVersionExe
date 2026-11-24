using CommunityToolkit.Mvvm.ComponentModel;
using SIGMPOffline.Desktop.Services;

namespace SIGMPOffline.Desktop.ViewModels;

/// <summary>
/// Base des pages métier : expose l'état du mode lecture seule et notifie les
/// vues lorsque les droits d'écriture changent.
/// </summary>
public abstract partial class PageViewModelBase : ViewModelBase, IPageViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PeutAgir))]
    private bool _estOccupe;

    protected PageViewModelBase(ModeLectureService modeLecture)
    {
        ModeLecture = modeLecture ?? throw new ArgumentNullException(nameof(modeLecture));
        ModeLecture.PropertyChanged += (_, evenement) =>
        {
            if (evenement.PropertyName == nameof(ModeLectureService.EstActif))
            {
                OnPropertyChanged(nameof(PeutModifier));
                OnPropertyChanged(nameof(PeutAgir));
            }
        };
    }

    /// <summary>Mode consultation partagé par toute l'application.</summary>
    protected ModeLectureService ModeLecture { get; }

    /// <summary>Indique si les actions de création, modification et suppression sont autorisées.</summary>
    public virtual bool PeutModifier => ModeLecture.PeutModifier;

    /// <summary>Indique si une action d'écriture peut être lancée maintenant.</summary>
    public bool PeutAgir => PeutModifier && !EstOccupe;

    /// <inheritdoc />
    public abstract string Titre { get; }

    /// <inheritdoc />
    public abstract Task ChargerAsync();
}

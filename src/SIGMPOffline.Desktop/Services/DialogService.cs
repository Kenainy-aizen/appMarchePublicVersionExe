using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using SIGMPOffline.Desktop.ViewModels.Dialogs;
using SIGMPOffline.Desktop.Views.Dialogs;

namespace SIGMPOffline.Desktop.Services;

/// <inheritdoc />
public sealed class DialogService : IDialogService
{
    private readonly Dictionary<Type, Func<DialogViewModelBase, Window>> _fenetres = new();

    public DialogService()
    {
        Enregistrer<PpmEnteteDialogViewModel>(_ => new PpmEnteteDialogWindow());
        Enregistrer<PpmDetailDialogViewModel>(_ => new PpmDetailDialogWindow());
        Enregistrer<DossierDaoDialogViewModel>(_ => new DossierDaoDialogWindow());
        Enregistrer<CandidatDialogViewModel>(_ => new CandidatDialogWindow());
        Enregistrer<AcheteurDaoDialogViewModel>(_ => new AcheteurDaoDialogWindow());
        Enregistrer<MarcheDialogViewModel>(_ => new MarcheDialogWindow());
        Enregistrer<ElementMarcheDialogViewModel>(_ => new ElementMarcheDialogWindow());
        Enregistrer<DateDialogViewModel>(_ => new DateDialogWindow());
        Enregistrer<TefDialogViewModel>(_ => new TefDialogWindow());
        Enregistrer<AvenantDialogViewModel>(_ => new AvenantDialogWindow());
        Enregistrer<MessageDialogViewModel>(_ => new MessageDialogWindow());
        Enregistrer<ProvinceDialogViewModel>(_ => new ProvinceDialogWindow());
        Enregistrer<MotDePasseDialogViewModel>(_ => new MotDePasseDialogWindow());
        Enregistrer<ChangerProvinceDialogViewModel>(_ => new ChangerProvinceDialogWindow());
    }

    /// <inheritdoc />
    public async Task<bool> AfficherAsync(DialogViewModelBase viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        if (!_fenetres.TryGetValue(viewModel.GetType(), out var fabrique))
        {
            throw new InvalidOperationException(
                $"Aucune fenêtre n'est enregistrée pour le dialogue « {viewModel.GetType().Name} ».");
        }

        var fenetre = fabrique(viewModel);
        fenetre.DataContext = viewModel;

        var resultat = false;

        void SurFermeture(object? _, bool value)
        {
            resultat = value;
            fenetre.Close();
        }

        viewModel.CloseRequested += SurFermeture;
        try
        {
            var proprietaire = FenetrePrincipale();
            if (proprietaire is not null)
            {
                await fenetre.ShowDialog(proprietaire);
            }
            else
            {
                fenetre.Show();
            }
        }
        finally
        {
            viewModel.CloseRequested -= SurFermeture;
        }

        return resultat;
    }

    /// <inheritdoc />
    public Task<bool> ConfirmerAsync(string titre, string message)
        => AfficherAsync(new MessageDialogViewModel(titre, message, estQuestion: true));

    /// <inheritdoc />
    public Task InformerAsync(string titre, string message)
        => AfficherAsync(new MessageDialogViewModel(titre, message, estQuestion: false));

    private void Enregistrer<TViewModel>(Func<TViewModel, Window> fabrique)
        where TViewModel : DialogViewModelBase
        => _fenetres[typeof(TViewModel)] = viewModel => fabrique((TViewModel)viewModel);

    private static Window? FenetrePrincipale()
        => (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
}

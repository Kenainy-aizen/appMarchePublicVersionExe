using SIGMPOffline.Desktop.Services;
using SIGMPOffline.Desktop.ViewModels;

namespace SIGMPOffline.Desktop.Tests;

public class ModeLectureTests
{
    private sealed class PageTestViewModel : PageViewModelBase
    {
        public PageTestViewModel(ModeLectureService mode) : base(mode)
        {
        }

        public override string Titre => "Page de test";

        public override Task ChargerAsync() => Task.CompletedTask;
    }

    [Fact]
    public void Par_defaut_les_ecritures_sont_autorisees()
    {
        var mode = new ModeLectureService();
        var page = new PageTestViewModel(mode);

        Assert.False(mode.EstActif);
        Assert.True(mode.PeutModifier);
        Assert.True(page.PeutModifier);
        Assert.True(page.PeutAgir);
    }

    [Fact]
    public void Activer_le_mode_desactive_les_ecritures_sur_les_pages()
    {
        var mode = new ModeLectureService();
        var page = new PageTestViewModel(mode);

        mode.EstActif = true;

        Assert.False(mode.PeutModifier);
        Assert.False(page.PeutModifier);
        Assert.False(page.PeutAgir);
    }

    [Fact]
    public void Le_mode_notifie_les_pages_et_revient_a_l_etat_initial()
    {
        var mode = new ModeLectureService();
        var page = new PageTestViewModel(mode);
        var notifications = new List<string?>();
        page.PropertyChanged += (_, evenement) => notifications.Add(evenement.PropertyName);

        mode.EstActif = true;
        mode.EstActif = false;

        Assert.True(page.PeutModifier);
        Assert.True(page.PeutAgir);
        Assert.Contains(nameof(PageViewModelBase.PeutModifier), notifications);
        Assert.Contains(nameof(PageViewModelBase.PeutAgir), notifications);
    }

    [Fact]
    public void Une_page_occupee_ne_peut_pas_agir()
    {
        var mode = new ModeLectureService();
        var page = new PageTestViewModel(mode) { EstOccupe = true };

        Assert.True(page.PeutModifier);
        Assert.False(page.PeutAgir);
    }
}

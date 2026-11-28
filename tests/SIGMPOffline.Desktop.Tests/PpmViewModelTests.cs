using SIGMPOffline.Desktop.Services;
using SIGMPOffline.Desktop.Tests.Infrastructure;
using SIGMPOffline.Desktop.ViewModels;
using SIGMPOffline.Desktop.ViewModels.Dialogs;
using SIGMPOffline.Domain.Entities;
using SIGMPOffline.Domain.Enums;

namespace SIGMPOffline.Desktop.Tests;

/// <summary>Tests de la recherche dans le détail d'un plan de passation.</summary>
public class PpmViewModelTests
{
    private sealed class FakeDialogues : IDialogService
    {
        public Task<bool> AfficherAsync(DialogViewModelBase viewModel) => Task.FromResult(true);

        public Task<bool> ConfirmerAsync(string titre, string message) => Task.FromResult(true);

        public Task InformerAsync(string titre, string message) => Task.CompletedTask;
    }

    private sealed class ServiceInutilise;

    private static PpmViewModel Creer()
        => new(RunnerDeTest.Creer(new ServiceInutilise()), new FakeDialogues(), new ModeLectureService());

    [Fact]
    public void Recherche_detail_filtre_les_lignes_du_plan()
    {
        var vm = Creer();
        vm.Details.Add(new PpmDetail
        {
            NumPPMDetail = "2026-001",
            ObjetProjet = "Construction d'une école",
            NatureMarche = NatureMarche.Travaux,
            ModePassation = ModePassation.AppelOffresOuvert,
            SourceFinancement = "Budget national",
            MontantEstimatif = 1_500_000m
        });
        vm.Details.Add(new PpmDetail
        {
            NumPPMDetail = "2026-002",
            ObjetProjet = "Achat de fournitures",
            NatureMarche = NatureMarche.Fournitures,
            ModePassation = ModePassation.ConsultationPrix,
            SourceFinancement = "Bailleur",
            MontantEstimatif = 800_000m
        });

        vm.RechercheDetail = "école";
        Assert.Single(vm.DetailsFiltres);
        Assert.Equal("2026-001", vm.DetailsFiltres[0].NumPPMDetail);
        Assert.Equal(1, vm.NombreLignesFiltrees);
        Assert.False(vm.AucunResultatDetail);

        vm.RechercheDetail = "fournitures";
        Assert.Single(vm.DetailsFiltres);
        Assert.Equal("2026-002", vm.DetailsFiltres[0].NumPPMDetail);

        vm.RechercheDetail = "1500000";
        Assert.Single(vm.DetailsFiltres);

        vm.RechercheDetail = "introuvable";
        Assert.Empty(vm.DetailsFiltres);
        Assert.True(vm.AucunResultatDetail);

        vm.ReinitialiserRechercheDetailCommand.Execute(null);
        Assert.Equal(2, vm.DetailsFiltres.Count);
        Assert.False(vm.AucunResultatDetail);
    }
}

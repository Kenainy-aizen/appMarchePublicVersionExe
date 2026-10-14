using SIGMPOffline.Desktop.Services;
using SIGMPOffline.Desktop.ViewModels;
using SIGMPOffline.Desktop.ViewModels.Dialogs;
using SIGMPOffline.Services.Abstractions;

namespace SIGMPOffline.Desktop.Tests;

/// <summary>Tests du module Utilitaire (confirmation de restauration, sélecteurs de fichiers).</summary>
public class BackupViewModelTests
{
    private sealed class FakeBackup : IBackupService
    {
        public string BaseDeDonneesPath => "/home/test/sigmp.db";

        public string? DernierDossierSauvegarde { get; private set; }
        public string? DernierFichierRestaure { get; private set; }
        public string CheminSauvegarde { get; set; } = "/home/test/SIGMPOffline-Backups/sigmp-backup.db";

        public Task<string> SauvegarderAsync(
            string? destinationDirectory = null,
            CancellationToken cancellationToken = default)
        {
            DernierDossierSauvegarde = destinationDirectory;
            return Task.FromResult(CheminSauvegarde);
        }

        public Task RestaurerAsync(string sourceFilePath, CancellationToken cancellationToken = default)
        {
            DernierFichierRestaure = sourceFilePath;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDialogues : IDialogService
    {
        public bool ReponseConfirmation { get; set; } = true;
        public int Confirmations { get; private set; }
        public List<string> Informations { get; } = new();

        public Task<bool> AfficherAsync(DialogViewModelBase viewModel) => Task.FromResult(true);

        public Task<bool> ConfirmerAsync(string titre, string message)
        {
            Confirmations++;
            return Task.FromResult(ReponseConfirmation);
        }

        public Task InformerAsync(string titre, string message)
        {
            Informations.Add($"{titre} : {message}");
            return Task.CompletedTask;
        }
    }

    private sealed class FakePicker : IFilePickerService
    {
        public string? Dossier { get; set; }
        public string? Fichier { get; set; }

        public Task<string?> ChoisirDossierAsync(string titre) => Task.FromResult(Dossier);

        public Task<string?> ChoisirFichierAsync(string titre, string? extension = null)
            => Task.FromResult(Fichier);
    }

    [Fact]
    public void Restaurer_sans_chemin_affiche_une_erreur()
    {
        var backup = new FakeBackup();
        var vm = new BackupViewModel(backup, new FakeDialogues(), new FakePicker())
        {
            FichierRestauration = "   "
        };

        vm.RestaurerCommand.Execute(null);

        Assert.NotNull(vm.Erreur);
        Assert.Null(backup.DernierFichierRestaure);
    }

    [Fact]
    public async Task Restaurer_annule_si_l_utilisateur_refuse()
    {
        var backup = new FakeBackup();
        var dialogues = new FakeDialogues { ReponseConfirmation = false };
        var vm = new BackupViewModel(backup, dialogues, new FakePicker())
        {
            FichierRestauration = "/media/usb/sigmp-backup.db"
        };

        await vm.RestaurerCommand.ExecuteAsync(null);

        Assert.Equal(1, dialogues.Confirmations);
        Assert.Null(backup.DernierFichierRestaure);
    }

    [Fact]
    public async Task Restaurer_confirme_appelle_le_service()
    {
        var backup = new FakeBackup();
        var dialogues = new FakeDialogues { ReponseConfirmation = true };
        var vm = new BackupViewModel(backup, dialogues, new FakePicker())
        {
            FichierRestauration = "/media/usb/sigmp-backup.db"
        };

        await vm.RestaurerCommand.ExecuteAsync(null);

        Assert.Equal("/media/usb/sigmp-backup.db", backup.DernierFichierRestaure);
        Assert.Contains(dialogues.Informations, i => i.StartsWith("Restauration réussie"));
    }

    [Fact]
    public async Task Sauvegarder_transmet_le_dossier_choisi()
    {
        var backup = new FakeBackup();
        var vm = new BackupViewModel(backup, new FakeDialogues(), new FakePicker())
        {
            DossierDestination = "/media/usb/SIGMP"
        };

        await vm.SauvegarderCommand.ExecuteAsync(null);

        Assert.Equal("/media/usb/SIGMP", backup.DernierDossierSauvegarde);
    }

    [Fact]
    public async Task Sauvegarder_sans_dossier_transmet_null()
    {
        var backup = new FakeBackup();
        var vm = new BackupViewModel(backup, new FakeDialogues(), new FakePicker())
        {
            DossierDestination = "   "
        };

        await vm.SauvegarderCommand.ExecuteAsync(null);

        Assert.Null(backup.DernierDossierSauvegarde);
    }

    [Fact]
    public async Task ParcourirDossier_renseigne_le_chemin_choisi()
    {
        var vm = new BackupViewModel(new FakeBackup(), new FakeDialogues(), new FakePicker
        {
            Dossier = "/media/usb/CLES"
        });

        await vm.ParcourirDossierCommand.ExecuteAsync(null);

        Assert.Equal("/media/usb/CLES", vm.DossierDestination);
    }

    [Fact]
    public async Task ParcourirFichierRestauration_renseigne_le_chemin_choisi()
    {
        var vm = new BackupViewModel(new FakeBackup(), new FakeDialogues(), new FakePicker
        {
            Fichier = "/media/usb/sigmp-backup.db"
        });

        await vm.ParcourirFichierRestaurationCommand.ExecuteAsync(null);

        Assert.Equal("/media/usb/sigmp-backup.db", vm.FichierRestauration);
    }
}

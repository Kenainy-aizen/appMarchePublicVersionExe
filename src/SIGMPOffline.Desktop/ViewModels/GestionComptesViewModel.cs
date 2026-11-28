using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIGMPOffline.Desktop.Services;
using SIGMPOffline.Desktop.ViewModels.Dialogs;
using SIGMPOffline.Domain.Entities;
using SIGMPOffline.Services.Abstractions;

namespace SIGMPOffline.Desktop.ViewModels;

/// <summary>Gestion PRMP des comptes et des droits d'accès par province.</summary>
public partial class GestionComptesViewModel : PageViewModelBase
{
    private readonly IAuthService _auth;
    private readonly IDialogService _dialogues;

    public GestionComptesViewModel(
        IAuthService auth,
        IDialogService dialogues,
        ModeLectureService modeLecture)
        : base(modeLecture)
    {
        _auth = auth ?? throw new ArgumentNullException(nameof(auth));
        _dialogues = dialogues ?? throw new ArgumentNullException(nameof(dialogues));
        ModeLecture.PropertyChanged += (_, evenement) =>
        {
            if (evenement.PropertyName == nameof(ModeLectureService.EstActif))
            {
                OnPropertyChanged(nameof(CompteSelectionneEstModifiable));
            }
        };
    }

    public override string Titre => "Comptes & permissions";
    public ObservableCollection<Utilisateur> Utilisateurs { get; } = new();
    public ObservableCollection<HistoriqueEvenement> Historique { get; } = new();

    [ObservableProperty] private bool _estHistoriqueAffiche;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CompteSelectionneEstModifiable))]
    [NotifyPropertyChangedFor(nameof(CompteSelectionneEstAdministrateur))]
    [NotifyPropertyChangedFor(nameof(DroitsCompteSelectionne))]
    private Utilisateur? _compteSelectionne;

    [ObservableProperty] private string? _erreur;

    public bool CompteSelectionneEstModifiable
        => PeutModifier && CompteSelectionne is { EstAdministrateur: false };

    public bool CompteSelectionneEstAdministrateur => CompteSelectionne?.EstAdministrateur == true;

    public IReadOnlyList<PermissionProvince> DroitsCompteSelectionne
        => CompteSelectionne?.PermissionsProvinces.OrderBy(x => x.Province?.NomProvince).ToArray()
            ?? Array.Empty<PermissionProvince>();

    [RelayCommand]
    public override async Task ChargerAsync()
    {
        try
        {
            EstOccupe = true;
            Erreur = null;
            if (EstHistoriqueAffiche)
            {
                Historique.Clear();
                foreach (var evenement in await _auth.ListerHistoriqueAsync())
                {
                    Historique.Add(evenement);
                }
            }
            else
            {
                var selection = CompteSelectionne?.IdUtilisateur;
                var comptes = await _auth.ListerUtilisateursAsync();
                Utilisateurs.Clear();
                foreach (var compte in comptes)
                {
                    Utilisateurs.Add(compte);
                }

                CompteSelectionne = Utilisateurs.FirstOrDefault(x => x.IdUtilisateur == selection)
                    ?? Utilisateurs.FirstOrDefault();
            }
        }
        catch (Exception ex)
        {
            Erreur = ex.Message;
        }
        finally
        {
            EstOccupe = false;
        }
    }

    [RelayCommand]
    private async Task AfficherComptes()
    {
        EstHistoriqueAffiche = false;
        await ChargerAsync();
    }

    [RelayCommand]
    private async Task AfficherHistorique()
    {
        EstHistoriqueAffiche = true;
        await ChargerAsync();
    }

    [RelayCommand]
    private async Task CreerCompte()
    {
        if (!PeutModifier)
        {
            return;
        }

        await OuvrirEditionAsync(null);
    }

    [RelayCommand]
    private async Task ModifierCompte()
    {
        if (!CompteSelectionneEstModifiable || CompteSelectionne is null)
        {
            return;
        }

        await OuvrirEditionAsync(CompteSelectionne);
    }

    [RelayCommand]
    private async Task SupprimerCompte()
    {
        if (!CompteSelectionneEstModifiable || CompteSelectionne is not { } compte)
        {
            return;
        }

        if (!await _dialogues.ConfirmerAsync(
                "Supprimer le compte",
                $"Supprimer le compte « {compte.NomUtilisateur} » et tous ses droits d'accès ?"))
        {
            return;
        }

        try
        {
            await _auth.SupprimerUtilisateurAsync(compte.IdUtilisateur);
            await ChargerAsync();
        }
        catch (Exception ex)
        {
            Erreur = ex.Message;
            await _dialogues.InformerAsync("Suppression impossible", ex.Message);
        }
    }

    private async Task OuvrirEditionAsync(Utilisateur? utilisateur)
    {
        try
        {
            Erreur = null;
            var provinces = await _auth.ListerProvincesAsync();
            var dialogue = new UtilisateurDialogViewModel(provinces, utilisateur);
            if (!await _dialogues.AfficherAsync(dialogue))
            {
                return;
            }

            if (utilisateur is null)
            {
                await _auth.CreerUtilisateurAsync(
                    dialogue.NomUtilisateur,
                    dialogue.MotDePasse,
                    dialogue.DroitsParProvince);
            }
            else
            {
                await _auth.ModifierUtilisateurAsync(
                    utilisateur.IdUtilisateur,
                    dialogue.NomUtilisateur,
                    dialogue.MotDePasse,
                    dialogue.DroitsParProvince);
            }

            await ChargerAsync();
            CompteSelectionne = Utilisateurs.FirstOrDefault(x =>
                string.Equals(x.NomUtilisateur, dialogue.NomUtilisateur, StringComparison.OrdinalIgnoreCase));
            await _dialogues.InformerAsync(
                "Compte enregistré",
                $"Les informations et droits du compte « {dialogue.NomUtilisateur} » ont été enregistrés.");
        }
        catch (Exception ex)
        {
            Erreur = ex.Message;
            await _dialogues.InformerAsync("Enregistrement impossible", ex.Message);
        }
    }
}

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIGMPOffline.Desktop.Services;
using SIGMPOffline.Desktop.ViewModels.Dialogs;
using SIGMPOffline.Domain.Entities;
using SIGMPOffline.Services.Abstractions;
using SIGMPOffline.Services.Models;
using SIGMPOffline.Services.Scoping;

namespace SIGMPOffline.Desktop.ViewModels;

/// <summary>
/// Module Suivi d'Exécution (cœur) : TEF, Ordre de Service, avenants, réceptions et clôture.
/// Toutes les saisies passent par des fenêtres modales.
/// </summary>
public partial class SuiviViewModel : PageViewModelBase
{
    private readonly IScopeRunner _scopeRunner;
    private readonly IDialogService _dialogues;

    // Un seul chargement de situation à la fois : évite deux lectures concurrentes.
    private readonly SemaphoreSlim _chargementSituation = new(1, 1);

    public SuiviViewModel(
        IScopeRunner scopeRunner,
        IDialogService dialogues,
        ModeLectureService modeLecture)
        : base(modeLecture)
    {
        _scopeRunner = scopeRunner ?? throw new ArgumentNullException(nameof(scopeRunner));
        _dialogues = dialogues ?? throw new ArgumentNullException(nameof(dialogues));
    }

    /// <inheritdoc />
    public override string Titre => "Suivi d'exécution";

    /// <summary>Marchés disponibles.</summary>
    public ObservableCollection<Marche> Marches { get; } = new();

    /// <summary>Avenants du marché sélectionné.</summary>
    public ObservableCollection<Avenant> Avenants { get; } = new();

    [ObservableProperty] private string _rechercheSuivi = string.Empty;
    [ObservableProperty] private string _filtreStatutSuivi = "Tous";

    /// <summary>Marchés du suivi correspondant à la recherche et au statut choisi.</summary>
    public IEnumerable<Marche> MarchesFiltres => Marches.Where(marche =>
        (string.IsNullOrWhiteSpace(RechercheSuivi)
            || marche.RefMarche.Contains(RechercheSuivi, StringComparison.OrdinalIgnoreCase)
            || marche.ObjetMarche.Contains(RechercheSuivi, StringComparison.OrdinalIgnoreCase)
            || marche.RefDossier.Contains(RechercheSuivi, StringComparison.OrdinalIgnoreCase))
        && (FiltreStatutSuivi == "Tous"
            || (_situations.TryGetValue(marche.RefMarche, out var situation)
                && Libelles.Pour(situation.Statut) == FiltreStatutSuivi)));

    public IReadOnlyList<string> StatutsSuiviDisponibles { get; } = new[] { "Tous", "Non démarré", "En cours", "Réception provisoire", "Réception définitive", "Clôturé", "Résilié", "Annulé" };

    partial void OnRechercheSuiviChanged(string value) => OnPropertyChanged(nameof(MarchesFiltres));
    partial void OnFiltreStatutSuiviChanged(string value) => OnPropertyChanged(nameof(MarchesFiltres));

    [RelayCommand]
    private void ReinitialiserFiltres()
    {
        RechercheSuivi = string.Empty;
        FiltreStatutSuivi = "Tous";
    }

    private readonly Dictionary<string, SituationMarche> _situations = new(StringComparer.OrdinalIgnoreCase);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AUnMarche))]
    [NotifyPropertyChangedFor(nameof(PeutModifier))]
    [NotifyPropertyChangedFor(nameof(PeutAgir))]
    [NotifyPropertyChangedFor(nameof(TitreDetailMarche))]
    private Marche? _marcheSelectionne;

    /// <summary>
    /// Ligne sélectionnée dans la liste. Volontairement distincte de <see cref="MarcheSelectionne"/> :
    /// la liste remet sa sélection à null à chaque rafraîchissement, ce qui ne doit pas vider le détail ouvert.
    /// </summary>
    [ObservableProperty] private Marche? _marcheListeSelectionne;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AUnAvenant))]
    private Avenant? _avenantSelectionne;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PeutModifier))]
    [NotifyPropertyChangedFor(nameof(PeutAgir))]
    private SituationMarche? _situation;

    [ObservableProperty] private string? _message;
    [ObservableProperty] private string? _erreur;

    /// <summary>Vrai lorsque l'écran de détail est affiché.</summary>
    [ObservableProperty] private bool _estEnVueDetail;

    /// <summary>Vrai si un marché est sélectionné.</summary>
    public bool AUnMarche => MarcheSelectionne is not null;

    /// <summary>Vrai si un avenant est sélectionné.</summary>
    public bool AUnAvenant => AvenantSelectionne is not null;

    /// <summary>Vrai si le marché sélectionné peut encore être modifié (non clôturé et hors mode lecture seule).</summary>
    public override bool PeutModifier => base.PeutModifier
        && MarcheSelectionne is not null
        && Situation is { EstCloture: false };

    /// <summary>Nombre de marchés suivis.</summary>
    public int NombreMarches => Marches.Count;

    /// <summary>Nombre de marchés actuellement en retard.</summary>
    public int NombreMarchesEnRetard => _situations.Values.Count(x => x.EstEnRetard);

    /// <summary>Nombre de marchés clôturés.</summary>
    public int NombreMarchesClotures => _situations.Values.Count(x => x.EstCloture);

    /// <summary>Titre du détail du marché.</summary>
    public string TitreDetailMarche => MarcheSelectionne is { } marche
        ? $"Marché n° {marche.RefMarche}"
        : "Suivi du marché";

    /// <inheritdoc />
    [RelayCommand]
    public override async Task ChargerAsync()
    {
        try
        {
            EstOccupe = true;
            Erreur = null;

            // Mémorise le marché ouvert en détail pour le restaurer après rechargement
            // (retour depuis une autre page du menu, bouton « Actualiser »…).
            var refConservee = EstEnVueDetail ? MarcheSelectionne?.RefMarche : null;

            IReadOnlyList<Marche> marches;
            IReadOnlyList<SituationMarche> situations;
            await _chargementSituation.WaitAsync();
            try
            {
                marches = await _scopeRunner.ExecuterAsync<IMarcheService, IReadOnlyList<Marche>>(
                    service => service.ListerAsync());
                situations = await _scopeRunner.ExecuterAsync<ISuiviExecutionService, IReadOnlyList<SituationMarche>>(
                    service => service.ListerSituationsAsync());
            }
            finally
            {
                _chargementSituation.Release();
            }

            _situations.Clear();
            foreach (var situation in situations)
            {
                _situations[situation.RefMarche] = situation;
            }
            Marches.Clear();
            foreach (var marche in marches)
            {
                Marches.Add(marche);
            }

            if (refConservee is not null)
            {
                var restaure = Marches.FirstOrDefault(m =>
                    string.Equals(m.RefMarche, refConservee, StringComparison.OrdinalIgnoreCase));

                if (restaure is null)
                {
                    // Le marché n'existe plus : retour à la liste.
                    EstEnVueDetail = false;
                    MarcheSelectionne = null;
                }
                else
                {
                    // Un seul chargement à la fois : évite deux actualisations concurrentes de l'écran.
                    _actualisationParSelectionSuspendue = true;
                    try { MarcheSelectionne = restaure; }
                    finally { _actualisationParSelectionSuspendue = false; }

                    await ActualiserAsync(restaure.RefMarche);
                }
            }

            NotifierCompteurs();
            OnPropertyChanged(nameof(MarchesFiltres));
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

    // ---------- Navigation liste <-> détail ----------

    [RelayCommand]
    private async Task OuvrirMarche(Marche? marcheChoisi)
    {
        var marche = marcheChoisi ?? MarcheListeSelectionne;
        if (marche is null) return;

        Erreur = null;
        Message = null;

        // Le setter déclenche normalement un chargement en tâche de fond : on le
        // suspend ici pour charger la situation une seule fois, de façon attendue.
        _actualisationParSelectionSuspendue = true;
        try
        {
            MarcheSelectionne = marche;
        }
        finally
        {
            _actualisationParSelectionSuspendue = false;
        }

        EstEnVueDetail = true;
        await ActualiserAsync(marche.RefMarche);
    }

    /// <summary>
    /// Ouvre directement le détail d'un marché depuis une autre page du shell.
    /// </summary>
    public async Task OuvrirMarcheParReferenceAsync(string refMarche)
    {
        var marche = Marches.FirstOrDefault(m =>
            string.Equals(m.RefMarche, refMarche, StringComparison.OrdinalIgnoreCase));

        if (marche is null)
        {
            await ChargerAsync();
            marche = Marches.FirstOrDefault(m =>
                string.Equals(m.RefMarche, refMarche, StringComparison.OrdinalIgnoreCase));
        }

        if (marche is null)
        {
            Erreur = $"Le marché « {refMarche} » est introuvable.";
            return;
        }

        RechercheSuivi = string.Empty;
        FiltreStatutSuivi = "Tous";
        await OuvrirMarche(marche);
    }

    [RelayCommand]
    private async Task RetourListe()
    {
        EstEnVueDetail = false;
        Erreur = null;
        Message = null;
        await ChargerAsync();
    }

    // ---------- Actions d'exécution ----------

    [RelayCommand]
    private async Task EmettreOs()
    {
        if (!PeutModifier)
        {
            return;
        }

        if (MarcheSelectionne is null)
        {
            return;
        }

        var dialogue = new DateDialogViewModel(
            "Émettre l'Ordre de Service",
            "Date de notification de l'OS :",
            Situation?.DateOS);
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        await ExecuterAsync(
            () => _scopeRunner.ExecuterAsync<ISuiviExecutionService>(
                service => service.EmettreOrdreServiceAsync(MarcheSelectionne.RefMarche, dialogue.VersDate())),
            "Ordre de service émis.");
    }

    [RelayCommand]
    private async Task EnregistrerTef()
    {
        if (!PeutModifier)
        {
            return;
        }

        if (MarcheSelectionne is null)
        {
            return;
        }

        var dialogue = new TefDialogViewModel(Situation?.RefTef);
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        await ExecuterAsync(
            () => _scopeRunner.ExecuterAsync<ISuiviExecutionService>(
                service => service.EnregistrerTefAsync(MarcheSelectionne.RefMarche, dialogue.VersRefTef())),
            "TEF enregistré.");
    }

    [RelayCommand]
    private async Task ReceptionProvisoire()
    {
        await EnregistrerReceptionAsync(
            "Réception provisoire",
            "Date de réception provisoire :",
            definitive: false);
    }

    [RelayCommand]
    private async Task ReceptionDefinitive()
    {
        await EnregistrerReceptionAsync(
            "Réception définitive",
            "Date de réception définitive :",
            definitive: true);
    }

    [RelayCommand]
    private async Task Cloturer()
    {
        if (!PeutModifier)
        {
            return;
        }

        if (MarcheSelectionne is null)
        {
            return;
        }

        var dialogue = new DateDialogViewModel(
            "Clôturer le marché",
            "Date de clôture :",
            Situation?.DateLimiteExecution);
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        var confirme = await _dialogues.ConfirmerAsync(
            "Clôturer le marché",
            "Confirmer la clôture définitive de ce marché ? Cette opération empêche "
            + "toute nouvelle modification d'exécution (OS, avenants, réceptions).");
        if (!confirme)
        {
            return;
        }

        await ExecuterAsync(
            () => _scopeRunner.ExecuterAsync<ISuiviExecutionService>(
                service => service.CloturerAsync(MarcheSelectionne.RefMarche, dialogue.VersDate())),
            "Marché clôturé.");
    }

    // ---------- Avenants ----------

    [RelayCommand]
    private async Task AjouterAvenant()
    {
        if (!PeutModifier)
        {
            return;
        }

        if (MarcheSelectionne is null)
        {
            return;
        }

        var dialogue = new AvenantDialogViewModel();
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        var avenant = dialogue.VersEntite();
        await ExecuterAsync(
            () => _scopeRunner.ExecuterAsync<ISuiviExecutionService>(
                service => service.AjouterAvenantAsync(MarcheSelectionne.RefMarche, avenant)),
            "Avenant ajouté.");
    }

    [RelayCommand]
    private async Task ModifierAvenant(Avenant? avenantSelectionne)
    {
        if (!PeutModifier)
        {
            return;
        }

        if (MarcheSelectionne is null)
        {
            return;
        }

        var avenantActuel = avenantSelectionne ?? AvenantSelectionne;
        if (avenantActuel is null)
        {
            return;
        }

        var dialogue = new AvenantDialogViewModel(avenantActuel);
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        var avenant = dialogue.VersEntite();
        await ExecuterAsync(
            () => _scopeRunner.ExecuterAsync<ISuiviExecutionService>(
                service => service.ModifierAvenantAsync(MarcheSelectionne.RefMarche, avenant)),
            "Avenant modifié.");
    }

    [RelayCommand]
    private async Task SupprimerAvenant(Avenant? avenantSelectionne)
    {
        if (!PeutModifier)
        {
            return;
        }

        if (MarcheSelectionne is null)
        {
            return;
        }

        var avenantActuel = avenantSelectionne ?? AvenantSelectionne;
        if (avenantActuel is null)
        {
            return;
        }

        var numero = avenantActuel.NumAvenant;
        var id = avenantActuel.IdAvenant;
        var confirme = await _dialogues.ConfirmerAsync(
            "Supprimer l'avenant",
            $"Confirmer la suppression de l'avenant « {numero} » ?");
        if (!confirme)
        {
            return;
        }

        await ExecuterAsync(
            () => _scopeRunner.ExecuterAsync<ISuiviExecutionService>(
                service => service.SupprimerAvenantAsync(MarcheSelectionne.RefMarche, id)),
            "Avenant supprimé.");
    }

    // ---------- Interne ----------

    partial void OnMarcheSelectionneChanged(Marche? value)
    {
        Situation = null;
        Avenants.Clear();
        AvenantSelectionne = null;

        if (value is not null && !_actualisationParSelectionSuspendue)
        {
            _ = ActualiserAsync(value.RefMarche);
        }
    }

    private bool _actualisationParSelectionSuspendue;

    private void NotifierCompteurs()
    {
        OnPropertyChanged(nameof(NombreMarches));
        OnPropertyChanged(nameof(NombreMarchesEnRetard));
        OnPropertyChanged(nameof(NombreMarchesClotures));
    }

    private async Task EnregistrerReceptionAsync(string titre, string libelle, bool definitive)
    {
        if (!PeutModifier)
        {
            return;
        }

        if (MarcheSelectionne is null)
        {
            return;
        }

        var initiale = definitive
            ? Situation?.DateLimiteExecution
            : Situation?.DateOS;

        var dialogue = new DateDialogViewModel(titre, libelle, initiale);
        if (!await _dialogues.AfficherAsync(dialogue))
        {
            return;
        }

        var date = dialogue.VersDate();
        var refMarche = MarcheSelectionne.RefMarche;

        await ExecuterAsync(
            () => definitive
                ? _scopeRunner.ExecuterAsync<ISuiviExecutionService>(
                    service => service.EnregistrerReceptionDefinitiveAsync(refMarche, date))
                : _scopeRunner.ExecuterAsync<ISuiviExecutionService>(
                    service => service.EnregistrerReceptionProvisoireAsync(refMarche, date)),
            definitive
                ? "Réception définitive enregistrée."
                : "Réception provisoire enregistrée.");
    }

    private async Task ExecuterAsync(Func<Task> action, string messageSucces)
    {
        if (MarcheSelectionne is null)
        {
            return;
        }

        try
        {
            EstOccupe = true;
            Erreur = null;
            Message = null;

            await action();
            await ActualiserAsync(MarcheSelectionne.RefMarche);
            Message = messageSucces;
            await _dialogues.InformerAsync("Opération réussie", messageSucces);
        }
        catch (Exception ex)
        {
            Erreur = ex.Message;
            await _dialogues.InformerAsync("Erreur", ex.Message);
        }
        finally
        {
            EstOccupe = false;
        }
    }

    private async Task ActualiserAsync(string refMarche)
    {
        await _chargementSituation.WaitAsync();
        try
        {
            var nouvelleSituation = await _scopeRunner.ExecuterAsync<ISuiviExecutionService, SituationMarche>(
                service => service.ObtenirSituationAsync(refMarche));
            var marche = await _scopeRunner.ExecuterAsync<IMarcheService, Marche>(
                service => service.ObtenirAsync(refMarche));

            // Ignore le résultat si un autre marché a été sélectionné entre-temps.
            if (!string.Equals(MarcheSelectionne?.RefMarche, refMarche, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Situation = nouvelleSituation;
            Avenants.Clear();
            foreach (var avenant in marche.Avenants.OrderByDescending(a => a.DateAvenant))
            {
                Avenants.Add(avenant);
            }

            AvenantSelectionne = Avenants.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Erreur = ex.Message;
        }
        finally
        {
            _chargementSituation.Release();
        }
    }
}

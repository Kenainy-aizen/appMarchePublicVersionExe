# Tableau de bord métier

## Fonctionnalités actuelles

Le tableau de bord est filtré par province grâce au contexte EF Core actif. Il
propose maintenant :

- KPI des marchés, marchés en cours, clôturés et en retard ;
- montant total HT ;
- filtre dynamique par exercice PPM ;
- filtre par statut d'exécution ;
- alertes de retard ;
- échéances dans les 30 prochains jours ;
- marchés avec données de suivi incomplètes (OS, TEF, réception provisoire ou
  réception définitive).

Les filtres sont appliqués au niveau du service et non uniquement à l'écran.
Les chiffres et toutes les listes restent donc cohérents.

## Contrats de service

`IDashboardService` expose :

- `ObtenirKpiAsync(DashboardFiltre?)` ;
- `ListerAlertesAsync(DashboardFiltre?)` ;
- `ListerEcheancesAsync(DashboardFiltre?)` ;
- `ListerMarchesIncompletsAsync(DashboardFiltre?)` ;
- `ListerExercicesAsync()`.

`DashboardFiltre` contient :

- `AnneeExercice` nullable ;
- `Statut` nullable (`StatutExecution`).

Une valeur nulle signifie « tous ».

## Règles des alertes

### Retard

Un marché est en retard si sa date limite calculée par `MarcheDelais` est
dépassée et que le marché n'est pas clôturé.

### Échéance proche

Une échéance est affichée si la date limite est comprise entre aujourd'hui et
les 30 prochains jours, pour un marché non clôturé.

### Donnée incomplète

Le service signale séparément chaque anomalie :

- OS absent ;
- TEF absent ;
- réception provisoire absente pour un marché démarré ;
- réception définitive absente après la réception provisoire.

## Fichiers à connaître

- `src/SIGMPOffline.Services/Dashboard/DashboardService.cs` : calcul métier ;
- `src/SIGMPOffline.Services/Models/DashboardFiltre.cs` : filtre ;
- `src/SIGMPOffline.Services/Models/AlerteEcheance.cs` : échéance proche ;
- `src/SIGMPOffline.Services/Models/MarcheIncomplet.cs` : anomalie ;
- `src/SIGMPOffline.Desktop/ViewModels/DashboardViewModel.cs` : chargement et
  sélection des filtres ;
- `src/SIGMPOffline.Desktop/Views/DashboardView.axaml` : présentation.

## Consignes pour la suite

- Conserver `DashboardFiltre` pour toute nouvelle statistique filtrable.
- Ne pas calculer les KPI uniquement dans XAML ou dans le ViewModel.
- Toute nouvelle alerte doit avoir un modèle dans `Services.Models`, un test de
  service et une documentation de sa règle métier.
- Les liens « ouvrir le suivi » pourront être ajoutés ensuite via un événement
  de navigation partagé avec `ShellViewModel`.
- Les graphiques et exports doivent réutiliser les mêmes résultats filtrés.

## Vérification

```bash
dotnet build -c Debug --nologo
dotnet test --no-build -c Debug --nologo
```

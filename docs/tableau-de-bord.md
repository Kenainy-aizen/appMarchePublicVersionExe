# Tableau de bord métier

## Fonctionnalités

Le tableau de bord est filtré par province grâce au contexte EF Core actif. Il
propose :

- KPI des marchés : total, en cours, en réception, clôturés, en retard et non démarrés ;
- taux de clôture calculé sur l'ensemble filtré ;
- filtre par exercice PPM ;
- filtre par statut d'exécution ;
- fenêtre d'échéances configurable : 7, 30, 60 ou 90 jours ;
- répartition des marchés par statut, sous forme de barres ;
- échéances des trois prochains mois, mois par mois ;
- alertes de retard, avec navigation directe vers le suivi du marché ;
- marchés avec données de suivi incomplètes, regroupées par marché ;
- boutons « Voir tout / Voir moins » lorsque les listes dépassent cinq lignes ;
- date et heure de la dernière actualisation.

Les filtres sont appliqués au niveau du service et non uniquement à l'écran.
Les chiffres, les listes et la synthèse restent donc cohérents.

## Ordre des sections

Les sections sont présentées par ordre d'urgence :

1. alertes de retard ;
2. échéances proches ;
3. données de suivi à compléter.

Chaque alerte est cliquable : elle ouvre le module **Suivi d'exécution** sur le
marché concerné. La navigation est portée par l'événement
`DashboardViewModel.MarcheDemandee`, écouté par `ShellViewModel`.

## Contrats de service

`IDashboardService` expose :

- `ObtenirTableauDeBordAsync(DashboardFiltre?)` : photographie complète en une
  seule lecture de la base (KPI, alertes, échéances, incomplets, répartition,
  échéances par mois et exercices) ;
- `ObtenirKpiAsync(DashboardFiltre?)` ;
- `ListerAlertesAsync(DashboardFiltre?)` ;
- `ListerEcheancesAsync(DashboardFiltre?)` ;
- `ListerMarchesIncompletsAsync(DashboardFiltre?)` ;
- `ListerRepartitionAsync(DashboardFiltre?)` ;
- `ListerEcheancesParMoisAsync(DashboardFiltre?)` ;
- `ListerExercicesAsync()`.

Les méthodes unitaires restent disponibles (outils, tests) et partagent les
mêmes calculs privés que `ObtenirTableauDeBordAsync`. Le ViewModel du tableau de
bord n'appelle plus qu'`ObtenirTableauDeBordAsync` par actualisation, ce qui
évite de recharger sept fois la liste des marchés. `DashboardInstantane` porte
le résultat complet.

`DashboardFiltre` contient :

- `AnneeExercice` nullable ;
- `Statut` nullable (`StatutExecution`) ;
- `JoursEcheances` (30 par défaut).

Une valeur nulle pour l'exercice ou le statut signifie « tous ».

`DashboardKpi` contient :

- `TotalMarches`, `MarchesEnCours`, `MarchesClotures`, `MarchesEnRetard` ;
- `MarchesNonDemarres` ;
- `MarchesEnReception` (réception provisoire ou définitive) ;
- `TauxCloture` (propriété calculée, en pourcentage).

## Règles des alertes

### Marché terminé

Un marché **clôturé, résilié ou annulé** n'est jamais considéré comme en retard.
Il est exclu des alertes, des échéances proches et de la synthèse mensuelle des
échéances. La règle est centralisée dans `MarcheDelais.EstTermine(StatutExecution)`
et réutilisée par tous les calculs du tableau de bord.

### Retard

Un marché est en retard si sa date limite calculée par `MarcheDelais` est
dépassée et que le marché n'est pas terminé (voir ci-dessus).

### Échéance proche

Une échéance est affichée si la date limite est comprise entre aujourd'hui et la
fin de la fenêtre choisie, pour un marché non terminé. La synthèse mensuelle
regroupe les échéances des trois prochains mois, y compris les mois sans
échéance, et applique la même exclusion.

### Donnée incomplète

Les marchés **non démarrés**, **résiliés** et **annulés** sont totalement ignorés.
Pour les autres, le service signale chaque anomalie, puis les regroupe par
marché :

- OS absent si la date d'ordre de service est nulle ;
- TEF absent si la référence TEF est vide ;
- réception provisoire absente uniquement pour un marché en réception
  définitive ou clôturé ;
- réception définitive absente uniquement pour un marché clôturé.

`MarcheIncomplet.Anomalies` contient les libellés complets,
`AnomaliesCourtes` fournit les badges compacts (`RP absente`, `RD absente`),
`NombreAnomalies` le nombre d'anomalies et `Anomalie` un résumé sous forme de
texte.

## Répartition par statut

`RepartitionStatut` contient le statut, le nombre de marchés et le pourcentage
(arrondi à une décimale). Les statuts absents de l'ensemble filtré ne sont pas
affichés. Si aucun marché ne correspond aux filtres, la liste est vide.

## Fichiers à connaître

- `src/SIGMPOffline.Services/Dashboard/DashboardService.cs` : calcul métier ;
- `src/SIGMPOffline.Services/Models/DashboardFiltre.cs` : filtres ;
- `src/SIGMPOffline.Services/Models/DashboardInstantane.cs` : résultat complet
  d'une actualisation ;
- `src/SIGMPOffline.Services/Models/DashboardKpi.cs` : indicateurs ;
- `src/SIGMPOffline.Services/Models/AlerteEcheance.cs` : échéance proche ;
- `src/SIGMPOffline.Services/Models/AlerteRetard.cs` : alerte de retard ;
- `src/SIGMPOffline.Services/Models/MarcheIncomplet.cs` : anomalies groupées ;
- `src/SIGMPOffline.Services/Models/RepartitionStatut.cs` : répartition ;
- `src/SIGMPOffline.Services/Models/EcheanceMois.cs` : synthèse mensuelle ;
- `src/SIGMPOffline.Desktop/ViewModels/DashboardViewModel.cs` : chargement,
  filtres, aperçus et navigation ;
- `src/SIGMPOffline.Desktop/ViewModels/ShellViewModel.cs` : bascule vers le
  suivi d'un marché ;
- `src/SIGMPOffline.Desktop/ViewModels/SuiviViewModel.cs` :
  `OuvrirMarcheParReferenceAsync` ;
- `src/SIGMPOffline.Desktop/Views/DashboardView.axaml` : présentation.

## Consignes pour la suite

- Conserver `DashboardFiltre` pour toute nouvelle statistique filtrable.
- Ne pas calculer les KPI uniquement dans XAML ou dans le ViewModel.
- Toute nouvelle alerte doit avoir un modèle dans `Services.Models`, un test de
  service et une documentation de sa règle métier.
- Les nouvelles listes doivent rester cliquables et ouvrir le suivi du marché.
- Les graphiques et exports doivent réutiliser les mêmes résultats filtrés.
- Les montants affichés utilisent `fr-FR` et le suffixe `Ar`.

## Vérification

```bash
dotnet build -c Debug --nologo
dotnet test --no-build -c Debug --nologo
```

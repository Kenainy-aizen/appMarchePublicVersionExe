# Suivi métier d'un marché

## Vue d'ensemble

Le détail du module **Suivi d'exécution** présente maintenant une synthèse
métier complète d'un marché, sans ajouter de colonne à la base de données.
Les données sont calculées à partir de `Marche`, `SuiviExecution` et `Avenant`.

## Chronologie affichée

La chronologie suit les étapes suivantes :

1. Ordre de service (OS) — `SuiviExecution.DateOS` ;
2. Titre d'engagement financier (TEF) — `SuiviExecution.RefTEF` ;
3. Réception provisoire — `SuiviExecution.DateReceptionProvisoire` ;
4. Réception définitive — `SuiviExecution.DateReceptionDefinitive` ;
5. Clôture — `SuiviExecution.EstCloture` et `DateCloture`.

Chaque étape affiche sa date ou sa référence, ainsi que `Fait` / `À faire`.
Les boutons d'opération restent désactivés lorsque le suivi est clôturé.

## Synthèse calculée

`SituationMarche` expose les informations nécessaires à l'écran :

- `DelaiContractuelJours` : délai initial de `Marche.DelaiExecutionJours` ;
- `ProlongationAvenantsJours` : somme de
  `Avenant.ProlongationDelaiJours` ;
- `DelaiTotalJours` : délai initial + prolongations ;
- `MontantInitialHt` : `Marche.MontantHT` ;
- `VariationAvenants` : somme des `Avenant.VariationMontant` ;
- `MontantFinalHt` : montant initial + variations ;
- les dates des réceptions et de clôture ;
- les libellés `EtatOs`, `EtatTef`, `EtatReceptionProvisoire`,
  `EtatReceptionDefinitive` et `EtatCloture`.

La date limite d'exécution continue d'être calculée par `MarcheDelais` à partir
de la date OS, du délai contractuel et des prolongations.

## Fichiers importants

- `src/SIGMPOffline.Services/Models/SituationMarche.cs` : projection calculée ;
- `src/SIGMPOffline.Services/Suivi/SuiviExecutionService.cs` : construction de
  la projection et règles métier ;
- `src/SIGMPOffline.Desktop/ViewModels/SuiviViewModel.cs` : commandes et
  rafraîchissement du détail ;
- `src/SIGMPOffline.Desktop/Views/SuiviView.axaml` : chronologie, synthèse et
  liste des avenants ;
- `tests/SIGMPOffline.Services.Tests/SuiviExecutionServiceTests.cs` : tests de
  calcul de la synthèse.

## Consignes pour une prochaine IA

- Ne pas créer de migration pour modifier uniquement cette présentation : les
  champs affichés existent déjà dans les entités persistées.
- Toute nouvelle étape doit être ajoutée dans `SituationMarche`, calculée dans
  `ConstruireSituation`, affichée dans la chronologie et couverte par un test.
- Les montants sont exprimés en Ariary et doivent rester formatés avec `N0` et
  le suffixe `Ar` dans l'interface.
- Les dates affichées utilisent `JJ/MM/AAAA`.
- Une modification d'exécution doit passer par `ISuiviExecutionService` et
  respecter l'interdiction de modifier un marché clôturé.

## Vérification

```bash
dotnet build -c Debug --nologo
dotnet test --no-build -c Debug --nologo
```

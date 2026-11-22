# Recherche et filtres de l'interface

## Objectif

Les quatre modules principaux proposent maintenant une recherche immédiate et un
filtre métier local dans la page :

| Module | Recherche | Filtre |
|---|---|---|
| PPM | Numéro du plan ou autorité contractante | Exercice présent dans les données chargées |
| DAO | Référence, objet ou ligne PPM | Dossiers avec/sans candidats |
| Marchés | Référence, objet ou dossier DAO | Statut du marché |
| Suivi d'exécution | Référence, objet ou dossier DAO | Statut d'exécution |

Le bouton **Réinitialiser** vide la recherche et remet le filtre à `Tous`.

## Architecture technique

Les filtres sont volontairement implémentés dans les ViewModels, sans modifier
la base de données ni les services métier :

- la collection complète reste la collection source (`Entetes`, `Dossiers`,
  `Marches`) ; elle est nécessaire aux CRUD, aux compteurs et à la sélection ;
- la propriété calculée `EntetesFiltres`, `DossiersFiltres` ou `MarchesFiltres`
  est utilisée par le `ListBox` de la vue ;
- les champs de recherche sont des propriétés CommunityToolkit MVVM ;
- les méthodes partielles `On...Changed` notifient la vue afin que la liste se
  recalcule à chaque caractère ou changement de ComboBox ;
- aucun filtre n'est envoyé à SQLite : le filtrage est local sur les données
  déjà chargées pour la province active.

Propriétés principales :

- `PpmViewModel.RecherchePpm`, `ExerciceFiltre`, `EntetesFiltres` ;
- `DossierDaoViewModel.RechercheDossier`, `FiltreDossiers`, `DossiersFiltres` ;
- `MarcheViewModel.RechercheMarche`, `FiltreStatutMarche`, `MarchesFiltres` ;
- `SuiviViewModel.RechercheSuivi`, `FiltreStatutSuivi`, `MarchesFiltres`.

Chaque ViewModel expose aussi `ReinitialiserFiltresCommand`.

## Règles à respecter pour une prochaine évolution

1. Ne pas remplacer la collection source par la collection filtrée : les
   opérations de création, modification, suppression et les KPI s'appuient sur
   les données complètes.
2. Ajouter toute nouvelle propriété de filtre avec `[ObservableProperty]` et
   notifier la collection filtrée dans `On...Changed`.
3. Utiliser `StringComparison.OrdinalIgnoreCase` pour les recherches textuelles.
4. Ajouter les valeurs d'un filtre dans une propriété `...Disponibles`, avec
   `Tous` comme première valeur.
5. Garder les libellés de statut cohérents avec `Libelles.Pour(...)` et
   `OptionsEnums.cs`.
6. Utiliser `PlaceholderText` dans Avalonia, et non l'ancien attribut
   `Watermark`.
7. Ne pas contourner les filtres multi-provinces : les services continuent de
   charger uniquement la province active grâce aux filtres EF Core.

## Limites actuelles et suite possible

Le filtrage est local et adapté à une base SQLite hors-ligne de taille normale.
Si le volume devient important, il faudra déplacer la recherche dans les
repositories avec des paramètres, ajouter une pagination et conserver les
mêmes propriétés publiques côté ViewModel.

Les tests à compléter lors d'une prochaine évolution de l'interface sont :

- recherche insensible à la casse ;
- filtre sans résultat ;
- combinaison recherche + filtre ;
- réinitialisation ;
- maintien de la sélection après actualisation ;
- absence de débordement à 1366 px.

## Vérification

Après toute modification des filtres, exécuter depuis la racine :

```bash
dotnet build -c Debug --nologo
dotnet test --no-build -c Debug --nologo
```

Le build doit rester sans avertissement et tous les tests doivent passer.

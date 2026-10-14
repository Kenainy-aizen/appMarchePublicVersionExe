# SIGMP Offline

Application **Desktop 100 % locale et hors-ligne** destinée à la **PRMP**
(Personne Responsable des Marchés Publics) pour le suivi des marchés publics.

- **Langage** : C# (.NET 9)
- **GUI** : [Avalonia UI](https://avaloniaui.net/) 12 (MVVM)
- **Base de données** : SQLite local (EF Core + `Microsoft.Data.Sqlite`)
- **Architecture** : N-Tiers / MVVM

## Prérequis

- .NET SDK **9.0** (ou 10.0, compatible pour cibler `net9.0`)
- Linux, macOS ou Windows

```bash
dotnet --version   # ≥ 9.0
```

## Solution

```
SIGMPOffline.slnx
├── src/
│   ├── SIGMPOffline.Domain/       # Entités, enums (aucune dépendance)
│   ├── SIGMPOffline.Data/         # EF Core : AppDbContext, configs, repositories, migrations
│   ├── SIGMPOffline.Services/     # Logique métier (modules PPM, DAO, Marché, Suivi, Dashboard, Backup)
│   └── SIGMPOffline.Desktop/      # Avalonia : Views + ViewModels + DI
└── tests/
    ├── SIGMPOffline.Domain.Tests/
    ├── SIGMPOffline.Data.Tests/
    ├── SIGMPOffline.Services.Tests/
    └── SIGMPOffline.Desktop.Tests/
```

Sens des dépendances : `Desktop → Services → Data → Domain`.

## Démarrage

```bash
dotnet restore
dotnet build
dotnet run --project src/SIGMPOffline.Desktop
```

Au premier lancement, l'application applique automatiquement les migrations puis
insère un **jeu de données de démonstration** (idempotent : rien n'est inséré si
la base contient déjà des données).

### Interface

Fenêtre unique avec **menu latéral**, une page par module :

1. **Tableau de bord** — KPI et alertes de dépassement de délai.
2. **Plan de Passation (PPM)** — en-têtes et lignes de détail.
3. **DAO & Attribution** — dossiers, candidats, acheteurs DAO.
4. **Marchés** — marchés et éléments du bordereau.
5. **Suivi d'exécution** — OS, TEF, avenants, réceptions, clôture.
6. **Sauvegarde** — export/restauration du fichier SQLite (clé USB).

### Emplacement de la base

Le fichier SQLite est stocké côté utilisateur :

- Linux : `~/.local/share/SIGMPOffline/sigmp.db`
- Windows : `%APPDATA%\SIGMPOffline\sigmp.db`

La variable d'environnement `SIGMP_CONNECTION` permet de forcer une autre chaîne
de connexion (utile pour les tests).

## Conventions d'IHM

- **Formulaires modaux** : les créations / modifications s'effectuent dans une
  fenêtre modale. Un `DialogViewModelBase` (dans
  `ViewModels/Dialogs`) expose `ValiderCommand` / `AnnulerCommand` et un
  évènement de fermeture que le `DialogService` (dans `Services/`) traduit en
  `Window.ShowDialog(owner)`.
- **Ajouter un dialogue** : créer la `Window` (dans `Views/Dialogs`), son
  ViewModel, puis l'enregistrer dans `DialogService`.
- **Dialogues réutilisables** : `DateDialogViewModel` (saisie d'une date : OS,
  réceptions, clôture), `TefDialogViewModel` et `AvenantDialogViewModel` couvrent
  le module Suivi d'Exécution.
- **Sélecteurs de fichiers** : `IFilePickerService` (implémentation basée sur le
  `StorageProvider` d'Avalonia) fournit les boîtes de dialogue natives de choix de
  dossier (clé USB) et de fichier (restauration).
- **Bindings compilés** : le projet Desktop active
  `AvaloniaUseCompiledBindingsByDefault`. Chaque vue porte un `x:DataType`, ce
  qui fait échouer la **compilation** en cas de chemin de liaison invalide.
- **Libellés d'énumérations** : centralisés dans `OptionsEnums` / `Libelles`
  (utilisés par les listes déroulantes et le convertisseur `EnumLibelle`).

## Tests

```bash
dotnet test
```

## Migrations EF Core

L'outil `dotnet-ef` est déclaré comme outil **local** (voir `dotnet-tools.json` à
la racine). Le projet *startup* est `SIGMPOffline.Data`, qui expose une
`IDesignTimeDbContextFactory`.

```bash
dotnet tool restore
dotnet ef migrations add <Nom> --project src/SIGMPOffline.Data --startup-project src/SIGMPOffline.Data
dotnet ef database update --project src/SIGMPOffline.Data --startup-project src/SIGMPOffline.Data
```

## Modules fonctionnels

1. **PPM** — saisie / révision de l'en-tête et des lignes de détail.
   *CRUD complet (fenêtres modales).*
2. **DAO & Attribution** — création du dossier, acheteurs DAO, candidats.
   *CRUD complet (dossiers, candidats, acheteurs) + désignation du retenu.*
3. **Marché & Bordereau** — formalisation du contrat et éléments du marché.
   *CRUD complet (marché + éléments du bordereau) + recalcul des montants.*
4. **Suivi d'Exécution** *(cœur)* — TEF, Ordre de Service, avenants, réceptions, clôture.
   *Saisies en fenêtres modales ; CRUD complet des avenants ; statut calculé.*
5. **Tableau de bord** — alertes de dépassement de délai et statuts.
6. **Utilitaire** — sauvegarde / restauration en un clic du fichier SQLite (clé USB).
   *Sélecteurs natifs (dossier/fichier) et confirmation avant restauration.*

## Licence

Interne — PRMP. Voir le fichier `LICENSE` le cas échéant.

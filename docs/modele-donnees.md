# SIGMP Offline — Modèle de données (SQLite)

> Application Desktop 100 % locale et hors-ligne destinée à la **PRMP**
> (Personne Responsable des Marchés Publics) pour le suivi des marchés publics.

Base de données : **SQLite** local (via EF Core + `Microsoft.Data.Sqlite`).
Fichier par défaut :

- Linux : `~/.local/share/SIGMPOffline/sigmp.db`
- Windows : `%APPDATA%\SIGMPOffline\sigmp.db`

La variable d'environnement `SIGMP_CONNECTION` permet de forcer une autre chaîne
de connexion. Les migrations EF Core en attente sont appliquées automatiquement
au démarrage.

## Entités et relations

| # | Entité | Table | Clé primaire | Clés étrangères | Rôle |
|---|--------|-------|--------------|-----------------|------|
| 1 | `PpmEntete` | `PPMEntete` | `NumPPM` | `IdProvince → Province` | En-tête du Plan de Passation des Marchés |
| 2 | `PpmDetail` | `PPMDetail` | `NumPPMDetail` | `NumPPMEntete → PPMEntete` | Lignes du PPM |
| 3 | `PpmBeneficiaire` | `PPMBeneficiaire` | `Id` | `NumPPMDetail → PPMDetail` | Services bénéficiaires d'une ligne et montants affectés |
| 4 | `DossierDao` | `DossierDAO` | `RefDossier` | `NumPPMDetail → PPMDetail`, `IdProvince → Province` | Dossier d'Appel d'Offres |
| 5 | `AcheteurDao` | `AcheteurDAO` | `IdAcheteur` | `RefDossier → DossierDao` | Acheteur du dossier DAO (et titulaire possible) |
| 6 | `Candidat` | `Candidat` | `IdCandidat` | `RefDossier → DossierDao` | Soumissionnaire / candidat |
| 7 | `Marche` | `Marche` | `RefMarche` | `RefDossier → DossierDao`, `NumStatTitulaire → AcheteurDao`, `IdProvince → Province` | Contrat formalisé |
| 8 | `ElementMarche` | `ElementMarche` | `CodeElement` | `RefMarche → Marche` | Articles / bordereau |
| 9 | `SuiviExecution` | `SuiviExecution` | `IdSuivi` | `RefMarche → Marche` | TEF, OS, réceptions, clôture |
| 10 | `Avenant` | `Avenant` | `IdAvenant` | `RefMarche → Marche` | Avenants au marché |
| 11 | `Province` | `Province` | `IdProvince` | — | Province gérée (mot de passe, isolation des données) |
| 12 | `ConfigurationApp` | `ConfigurationApp` | `Id` | — | Configuration globale (propriétaire PRMP) |
| 13 | `Utilisateur` | `Utilisateur` | `IdUtilisateur` | — | Compte utilisateur (identifiant, mot de passe haché, rôle admin) |
| 14 | `PermissionProvince` | `PermissionProvince` | `IdPermission` | `IdUtilisateur → Utilisateur`, `IdProvince → Province` | Droit de consultation ou modification par province |
| 15 | `HistoriqueEvenement` | `HistoriqueEvenement` | `IdHistorique` | — | Journal d'audit : auteur, province, action, entité et référence |

## Schéma relationnel (texte)

```
ConfigurationApp (1 ligne)

Province (1) ──< (N) PPMEntete │ DossierDAO │ Marche

PPMEntete (1) ──< (N) PPMDetail
                              ├──< (N) PPMBeneficiaire     [CASCADE]
                              └──< (N) DossierDAO          [RESTRICT]
                                         │
                    ┌────────────────────┼────────────────────┐
                    │                    │                    │
               (N) Candidat        (1) Marche          (N) AcheteurDAO
                                         │
                    ┌────────────────────┼────────────────────┐
                    │                    │                    │
              (N) ElementMarche   (1) SuiviExecution   (N) Avenant
```

> **Isolation multi-provinces** : `PPMEntete`, `DossierDAO` et `Marche` portent la clé
> étrangère `IdProvince`. Toutes les entités filles héritent de la province de leur
> parent. L'application n'expose que les données de la province ouverte ; les
> nouvelles entités y sont automatiquement rattachées.

## Détail des champs

### 1. PPMEntete

- `NumPPM` (PK, texte 50) — numéro du PPM, saisi manuellement et **immuable après création**
- `IdProvince` (FK → `Province.IdProvince`, nullable) — province propriétaire
- `AnneeExercice` (entier) — exercice budgétaire, modifiable après création
- `AutoriteContractante` (texte 200, obligatoire)
- `DateElaboration` (date) — date d'élaboration, modifiable après création
- `DateDerniereMAJ` (date/heure, nullable) — date de dernière mise à jour
- `NumDerniereMAJ` (texte 50, nullable) — numéro de la dernière mise à jour

### 2. PPMDetail

- `NumPPMDetail` (PK, texte 50) — numéro de la ligne, saisi manuellement et immuable après création
- `NumPPMEntete` (FK → `PPMEntete.NumPPM`, suppression en cascade)
- `NatureMarche` — énumération persistée en texte : `Travaux`, `Fournitures`, `Services`,
  `PrestationsIntellectuelles`
- `ObjetProjet` (texte 500, obligatoire)
- `MontantEstimatif` (entier, centimes) — montant estimatif en Ariary
- `ModePassation` — énumération persistée en texte : `AppelOffresOuvert`,
  `AppelOffresRestreint`, `ConsultationPrix`, `AppelManifestationInteret`, `AppelPrequalification`
- `SourceFinancement` (texte 200, nullable)
- `Comptes` (texte 500, nullable) — comptes budgétaires imputés
- `DateLancementPrevue` (date, nullable) — date prévisionnelle de lancement
- `DateOuverturePlisPrevue` (date, nullable) — date prévisionnelle d'ouverture des plis
- `DateAttributionPrevue` (date, nullable) — date prévisionnelle d'attribution
- `ResponsableProcedure` (texte 200, nullable) — responsable de la conduite de la procédure
- `Beneficiaires` — collection de `PpmBeneficiaire` (relation 1-N, cascade)

### 3. PPMBeneficiaire

- `Id` (PK, auto-incrément)
- `NumPPMDetail` (FK → `PPMDetail.NumPPMDetail`, suppression en cascade, indexé)
- `Service` (texte 200, obligatoire) — nom du service bénéficiaire
- `Montant` (entier, centimes) — montant affecté à ce service

> S'il existe au moins un service bénéficiaire, le `MontantEstimatif` de la ligne
> est automatiquement la **somme** des montants des services.

### 4. DossierDAO

- `RefDossier` (PK)
- `NumPPMDetail` (FK → `PPMDetail.NumPPMDetail`, **suppression RESTRICT**)
- `IdProvince` (FK → `Province.IdProvince`, nullable) — province propriétaire
- `DateCreation` (date saisie manuellement dans l'en-tête du DAO), `ObjetDossier`,
  `CoutCahierCharges`, `MontantCaution`

### 5. AcheteurDAO

- `IdAcheteur` (PK, auto-incrément)
- `RefDossier` (FK → `DossierDAO.RefDossier`)
- `NumStat` (clé alternative unique), `NumNIF`, `RaisonSociale`, `Adresse`, `DateAchat`

### 6. Candidat

- `IdCandidat` (PK, auto-incrément)
- `RefDossier` (FK → `DossierDAO.RefDossier`)
- `NumStat`, `NumNIF`, `NomEntreprise`, `MontantOffre`, `EstRetenu`

### 7. Marche

- `RefMarche` (PK)
- `RefDossier` (FK → `DossierDAO.RefDossier`, unique : 1 marché par dossier)
- `NumStatTitulaire` (FK → `AcheteurDAO.NumStat`, nullable)
- `IdProvince` (FK → `Province.IdProvince`, nullable) — province propriétaire
- `ObjetMarche`, `MontantHT`, `MontantTTC`, `DelaiExecutionJours`, `StatutExecution`

### 8. ElementMarche

- `CodeElement` (PK)
- `RefMarche` (FK → `Marche.RefMarche`)
- `Libelle`, `PrixUnitaire`, `Quantite`, `Unite`

### 9. SuiviExecution

- `IdSuivi` (PK, auto-incrément)
- `RefMarche` (FK → `Marche.RefMarche`, unique : relation 1-1)
- `RefTEF`, `DateOS`, `DateReceptionProvisoire`, `DateReceptionDefinitive`, `EstCloture`, `DateCloture`

### 10. Avenant

- `IdAvenant` (PK, auto-incrément)
- `RefMarche` (FK → `Marche.RefMarche`)
- `NumAvenant`, `Motif`, `VariationMontant`, `ProlongationDelaiJours`, `DateAvenant`

### 11. Province

- `IdProvince` (PK, auto-incrément)
- `NomProvince` (unique)
- `PasswordHash` — condensat PBKDF2 du mot de passe
- `Salt` — sel aléatoire (Base64)
- `DateCreation`

### 12. ConfigurationApp

- `Id` (PK)
- `NomProprietairePRMP` — responsable PRMP / propriétaire de l'application
- `DateInstallation` — date de la première configuration

## Énumérations persistées

Les énumérations sont enregistrées **en texte** (nom de la valeur), pour rester
lisibles directement en base.

| Énumération | Valeurs |
|-------------|---------|
| `NatureMarche` | `Travaux`, `Fournitures`, `Services`, `PrestationsIntellectuelles` |
| `ModePassation` | `AppelOffresOuvert` (DAO), `AppelOffresRestreint` (AOR), `ConsultationPrix` (DCP), `AppelManifestationInteret` (AMI), `AppelPrequalification` (APQ) |
| `StatutExecution` | `NonDemarre`, `EnCours`, `ReceptionProvisoire`, `ReceptionDefinitive`, `Cloture`, `Resilie`, `Annule` |

## Conventions de stockage

- **Montants monétaires** : `decimal` converti en `INTEGER` ×100 (centimes) via
  `DecimalConverters.Money`, afin de conserver des agrégats et tris numériques en SQLite.
- **Quantités** : converties en `INTEGER` ×1000 (3 décimales) via `DecimalConverters.Quantity`.
- **Dates** : `DateTime` / `DateOnly` persistés en `TEXT` (format ISO) ; affichage `JJ/MM/AAAA`.
- **Booléens** : `INTEGER` 0/1.

## Règles métier PPM

### En-tête du plan

- `NumPPM` est saisi manuellement à la création, puis **non modifiable**.
- L'exercice budgétaire et la date d'élaboration restent modifiables après création.
- La **date de mise à jour** est saisissable dans « Modifier l'en-tête ».
- Le **numéro de mise à jour** s'incrémente automatiquement de 1 lorsque la date
  de mise à jour change (`2` → `3`). Il reste inchangé si la date ne change pas.
- Le bouton **« Enregistrer une mise à jour »** force la date du jour et incrémente
  le numéro de mise à jour.
- L'ajout, la modification ou la suppression d'une ligne met à jour la date de
  dernière mise à jour (sans incrémenter le numéro).

### Lignes du plan

- Le numéro de ligne (`NumPPMDetail`) est saisi manuellement et immuable après création.
- Champs obligatoires : nature du marché, objet du projet.
- Le montant estimatif doit être un **entier positif ou nul en Ariary**
  (espaces de saisie tolérés).
- Les dates prévisionnelles, si renseignées, doivent respecter l'ordre :
  lancement ≤ ouverture des plis ≤ attribution.
- Chaque service bénéficiaire doit avoir un nom ; son montant doit être un entier positif ou nul.
- Si des services bénéficiaires existent, le montant de la ligne est leur somme.

### Suppressions protégées

- Une **ligne** utilisée par au moins un dossier DAO ne peut pas être supprimée.
- Un **plan** dont au moins une ligne est utilisée par un dossier DAO ne peut pas être supprimé.
- La contrainte SQLite `DossierDAO.NumPPMDetail → PPMDetail` est en `ON DELETE RESTRICT`
  (double protection base + message métier).

### Affichage

- Liste des plans : N°, exercice, autorité contractante, date d'élaboration,
  dernière mise à jour, nombre de lignes, montant total, bouton « Ouvrir ».
- Détail d'un plan : lignes avec nature, objet/détails, mode, dates prévisionnelles,
  montant et actions ; **total du plan** en pied de tableau.
- Montants affichés au format `1 500 000 Ar`, dates au format `JJ/MM/AAAA`.

## Historique des migrations

| Migration | Contenu |
|-----------|---------|
| `20261014073641_InitialCreate` | Schéma initial (PPM, DAO, marché, suivi) |
| `20261016193904_TitulaireMarcheVersAcheteur` | Titulaire du marché via `AcheteurDAO` |
| `20261017051217_AuthEtProvinces` | Provinces, authentification et `IdProvince` |
| `20261121120000_PpmDetailComplet` | Comptes, dates prévisionnelles, responsable de procédure et table `PPMBeneficiaire` |
| `20261125185738_AccesVisiteur` | Anciens accès visiteurs par province |
| `20261127182401_UtilisateursEtPermissions` | Comptes utilisateurs et droits par province (conversion des anciens accès) |
| `20261128112505_HistoriqueEvenements` | Table `HistoriqueEvenement` (audit des opérations) |

## Authentification et isolation (multi-provinces)

- **Premier lancement** (aucune configuration) : un assistant crée `ConfigurationApp`
  puis la première `Province` et le compte administrateur PRMP, et ouvre sa session.
- **Lancements suivants** : écran de connexion — nom d'utilisateur + mot de passe + province.
- **Mots de passe** : hachés avec **PBKDF2** (HMAC-SHA-256, 120 000 itérations, sel de
  16 octets, condensat de 32 octets) ; comparaison à temps constant. Aucun mot de passe
  en clair n'est stocké.
- **Isolation** : filtres de requête globaux EF Core basés sur la province active ;
  les nouvelles entités sont automatiquement rattachées à la province ouverte.
- **Gestion** (administrateur PRMP) : créer des provinces, créer/modifier/supprimer
  des comptes, attribuer la consultation ou la modification province par province,
  et changer son mot de passe.
- **Audit** : `HistoriqueEvenement` enregistre les événements de session et chaque
  création/modification/suppression métier avec l'auteur connecté. Aucun mot de
  passe ni hachage n'y est consigné. Consultable dans **Comptes & permissions → Historique**.

## Règles métier clés (Suivi d'exécution)

- **Date limite d'exécution** = `DateOS` + `DelaiExecutionJours` (+ `ProlongationDelaiJours` des avenants validés).
- **Alerte de dépassement** lorsque la date du jour dépasse la date limite et que le marché n'est pas clôturé.
- **Clôture** : `EstCloture = true` ⇒ `DateCloture` renseignée.

# SIGMP Offline — Modèle de données (SQLite)

> Application Desktop 100 % locale et hors-ligne destinée à la **PRMP**
> (Personne Responsable des Marchés Publics) pour le suivi des marchés publics.

Base de données : **SQLite** local (via EF Core + `Microsoft.Data.Sqlite`).

## Entités et relations

| # | Entité | Clé primaire | Clés étrangères | Rôle |
|---|--------|--------------|-----------------|------|
| 1 | `PPMEntete` | `NumPPM` | `IdProvince → Province` | En-tête du Plan de Passation des Marchés |
| 2 | `PPMDetail` | `NumPPMDetail` | `NumPPMEntete → PPMEntete` | Lignes du PPM |
| 3 | `DossierDAO` | `RefDossier` | `NumPPMDetail → PPMDetail`, `IdProvince → Province` | Dossier d'Appel d'Offres |
| 4 | `AcheteurDAO` | `IdAcheteur` | `RefDossier → DossierDAO` | Acheteur du dossier DAO (et titulaire possible) |
| 5 | `Candidat` | `IdCandidat` | `RefDossier → DossierDAO` | Soumissionnaire / candidat |
| 6 | `Marche` | `RefMarche` | `RefDossier → DossierDAO`, `NumStatTitulaire → AcheteurDAO`, `IdProvince → Province` | Contrat formalisé |
| 7 | `ElementMarche` | `CodeElement` | `RefMarche → Marche` | Articles / bordereau |
| 8 | `SuiviExecution` | `IdSuivi` | `RefMarche → Marche` | TEF, OS, réceptions, clôture |
| 9 | `Avenant` | `IdAvenant` | `RefMarche → Marche` | Avenants au marché |
| 10 | `Province` | `IdProvince` | — | Province gérée (mot de passe, isolation des données) |
| 11 | `ConfigurationApp` | `Id` | — | Configuration globale (propriétaire PRMP) |

## Schéma relationnel (texte)

```
ConfigurationApp (1 ligne)

Province (1) ──< (N) PPMEntete │ DossierDAO │ Marche

PPMEntete (1) ──< (N) PPMDetail (1) ──< (N) DossierDAO
                                               │
                        ┌──────────────────────┼──────────────────────┐
                        │                      │                      │
                   (N) Candidat           (1) Marche            (N) AcheteurDAO
                                               │
                        ┌──────────────────────┼──────────────────────┐
                        │                      │                      │
                  (N) ElementMarche       (1) SuiviExecution      (N) Avenant
```

> **Isolation multi-provinces** : `PPMEntete`, `DossierDAO` et `Marche` portent la clé
> étrangère `IdProvince`. Toutes les entités filles héritent de la province de leur
> parent. L'application n'expose que les données de la province ouverte ; les
> nouvelles entités y sont automatiquement rattachées.

## Détail des champs

### 1. PPMEntete
- `NumPPM` (PK) — numéro du PPM
- `IdProvince` (FK → Province.IdProvince, nullable) — province propriétaire
- `AnneeExercice` — exercice budgétaire
- `AutoriteContractante` — autorité contractante
- `DateElaboration` — date d'élaboration
- `DateDerniereMAJ` — date de dernière mise à jour
- `NumDerniereMAJ` — numéro de la dernière MAJ

### 2. PPMDetail
- `NumPPMDetail` (PK)
- `NumPPMEntete` (FK → PPMEntete.NumPPM)
- `NatureMarche`, `ObjetProjet`, `MontantEstimatif`, `ModePassation`, `SourceFinancement`

### 3. DossierDAO
- `RefDossier` (PK)
- `NumPPMDetail` (FK → PPMDetail.NumPPMDetail)
- `IdProvince` (FK → Province.IdProvince, nullable) — province propriétaire
- `DateCreation`, `ObjetDossier`, `CoutCahierCharges`, `MontantCaution`

### 4. AcheteurDAO
- `IdAcheteur` (PK)
- `RefDossier` (FK → DossierDAO.RefDossier)
- `NumStat`, `NumNIF`, `RaisonSociale`, `Adresse`, `DateAchat`

### 5. Candidat
- `IdCandidat` (PK)
- `RefDossier` (FK → DossierDAO.RefDossier)
- `NumStat`, `NumNIF`, `NomEntreprise`, `MontantOffre`, `EstRetenu`

### 6. Marche
- `RefMarche` (PK)
- `RefDossier` (FK → DossierDAO.RefDossier)
- `NumStatTitulaire` (FK → AcheteurDAO.NumStat)
- `IdProvince` (FK → Province.IdProvince, nullable) — province propriétaire
- `ObjetMarche`, `MontantHT`, `MontantTTC`, `DelaiExecutionJours`, `StatutExecution`

### 7. ElementMarche
- `CodeElement` (PK)
- `RefMarche` (FK → Marche.RefMarche)
- `Libelle`, `PrixUnitaire`, `Quantite`, `Unite`

### 8. SuiviExecution
- `IdSuivi` (PK)
- `RefMarche` (FK → Marche.RefMarche)
- `RefTEF`, `DateOS`, `DateReceptionProvisoire`, `DateReceptionDefinitive`, `EstCloture`, `DateCloture`

### 9. Avenant
- `IdAvenant` (PK)
- `RefMarche` (FK → Marche.RefMarche)
- `NumAvenant`, `Motif`, `VariationMontant`, `ProlongationDelaiJours`, `DateAvenant`

### 10. Province
- `IdProvince` (PK, auto-incrément)
- `NomProvince` (unique)
- `PasswordHash` — condensat PBKDF2 du mot de passe
- `Salt` — sel aléatoire (Base64)
- `DateCreation`

### 11. ConfigurationApp
- `Id` (PK)
- `NomProprietairePRMP` — responsable PRMP / propriétaire de l'application
- `DateInstallation` — date de la première configuration

## Authentification et isolation (multi-provinces)

- **Premier lancement** (aucune configuration) : un assistant crée `ConfigurationApp`
  puis la première `Province`, et ouvre immédiatement sa session.
- **Lancements suivants** : écran de connexion — choix de la province + mot de passe.
- **Mots de passe** : hachés avec **PBKDF2** (HMAC-SHA-256, 120 000 itérations, sel de
  16 octets, condensat de 32 octets) ; comparaison à temps constant. Aucun mot de passe
  en clair n'est stocké.
- **Isolation** : filtres de requête globaux EF Core basés sur la province active ;
  les nouvelles entités sont automatiquement rattachées à la province ouverte.
- **Gestion** : créer une province, changer de province (mot de passe requis) et
  changer le mot de passe d'une province.

## Règles métier clés (Suivi d'exécution)

- **Date limite d'exécution** = `DateOS` + `DelaiExecutionJours` (+ `ProlongationDelaiJours` des avenants validés).
- **Alerte de dépassement** lorsque la date du jour dépasse la date limite et que le marché n'est pas clôturé.
- **Clôture** : `EstCloture = true` ⇒ `DateCloture` renseignée.

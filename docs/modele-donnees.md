# SIGMP Offline — Modèle de données (SQLite)

> Application Desktop 100 % locale et hors-ligne destinée à la **PRMP**
> (Personne Responsable des Marchés Publics) pour le suivi des marchés publics.

Base de données : **SQLite** local (via EF Core + `Microsoft.Data.Sqlite`).

## Entités et relations

| # | Entité | Clé primaire | Clés étrangères | Rôle |
|---|--------|--------------|-----------------|------|
| 1 | `PPMEntete` | `NumPPM` | — | En-tête du Plan de Passation des Marchés |
| 2 | `PPMDetail` | `NumPPMDetail` | `NumPPMEntete → PPMEntete` | Lignes du PPM |
| 3 | `DossierDAO` | `RefDossier` | `NumPPMDetail → PPMDetail` | Dossier d'Appel d'Offres |
| 4 | `AcheteurDAO` | `IdAcheteur` | `RefDossier → DossierDAO` | Acheteur du dossier DAO |
| 5 | `Candidat` | `IdCandidat` | `RefDossier → DossierDAO` | Soumissionnaire / candidat |
| 6 | `Marche` | `RefMarche` | `RefDossier → DossierDAO`, `NumStatTitulaire → AcheteurDAO` | Contrat formalisé |
| 7 | `ElementMarche` | `CodeElement` | `RefMarche → Marche` | Articles / bordereau |
| 8 | `SuiviExecution` | `IdSuivi` | `RefMarche → Marche` | TEF, OS, réceptions, clôture |
| 9 | `Avenant` | `IdAvenant` | `RefMarche → Marche` | Avenants au marché |

## Schéma relationnel (texte)

```
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

## Détail des champs

### 1. PPMEntete
- `NumPPM` (PK) — numéro du PPM
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

## Règles métier clés (Suivi d'exécution)

- **Date limite d'exécution** = `DateOS` + `DelaiExecutionJours` (+ `ProlongationDelaiJours` des avenants validés).
- **Alerte de dépassement** lorsque la date du jour dépasse la date limite et que le marché n'est pas clôturé.
- **Clôture** : `EstCloture = true` ⇒ `DateCloture` renseignée.

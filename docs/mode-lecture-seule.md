# Mode lecture seule

## Objectif

Le mode **lecture seule** laisse l'application entièrement consultable — navigation,
recherche, filtres, thème, tableau de bord et sauvegarde — mais **désactive toute
écriture** : création, modification, suppression et restauration.

Il s'active depuis le menu latéral, sous le bloc **« Mode lecture seule »**
(`ToggleSwitch`), et s'applique immédiatement à toutes les pages.

## Comportement

- Un **bandeau d'information** apparaît en haut de la zone de contenu pour
  rappeler que les écritures sont bloquées.
- Les boutons d'action (Nouveau, Modifier, Supprimer, Ajouter, Recalculer,
  Désigner, Émettre, Clôturer, Restaurer…) sont **désactivés**.
- Les actions encore visibles sur les lignes (icônes Modifier / Supprimer) sont
  également désactivées.
- Les commandes de configuration (`Nouvelle province`, `Changer le mot de passe`)
  sont bloquées.
- La **sauvegarde** reste possible (elle ne modifie pas les données), mais la
  **restauration** est bloquée (elle remplacerait la base).
- Aucune donnée n'est écrite, mise à jour ou supprimée tant que le mode est actif.

## Architecture

| Élément | Rôle |
|---|---|
| `Services/ModeLectureService` | État partagé `EstActif` + `PeutModifier`, injecté en singleton. |
| `ViewModels/PageViewModelBase` | Base commune des pages métier : expose `PeutModifier` et `PeutAgir` (`PeutModifier && !EstOccupe`), notifie les vues au changement de mode. |
| `ShellViewModel.EstLectureSeule` | Expose le mode au shell ; bascule `ModeLectureService.EstActif`. |
| Vues (`*.axaml`) | `IsEnabled="{Binding PeutAgir}"` (ou `PeutModifier`) sur les actions d'écriture. |

Chaque commande de mutation vérifie aussi `PeutModifier` en début de méthode :
même un raccourci clavier, un `Command` direct ou un double-clic ne peut pas
contourner le mode.

## Tests

- `BackupViewModelTests.Restaurer_est_bloque_en_mode_lecture_seule` : la commande
  ne fait rien quand le mode est actif.
- `BackupViewModelTests.Le_mode_lecture_seule_notifie_les_pages` : les pages sont
  notifiées et `PeutModifier` / `PeutAgir` passent à `false`.

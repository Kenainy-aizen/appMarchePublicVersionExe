# Mode lecture seule

## Objectif

Le mode **lecture seule** laisse les pages autorisées consultables — navigation,
recherche, filtres et tableau de bord — mais désactive les modifications des données.
Les comptes sans droit de modification sont verrouillés dans ce mode. Les comptes
autorisés à modifier peuvent activer le mode manuellement.

Il s'active depuis le menu latéral, sous le bloc **« Mode lecture seule »**
(`ToggleSwitch`), et s'applique immédiatement à toutes les pages.

## Comportement

- Un **bandeau d'information** apparaît en haut de la zone de contenu pour
  rappeler que les écritures sont bloquées.
- Les boutons d'action (Nouveau, Modifier, Supprimer, Ajouter, Recalculer,
  Désigner, Émettre, Clôturer, Restaurer…) sont **désactivés**.
- Les actions encore visibles sur les lignes (icônes Modifier / Supprimer) sont
  également désactivées.
- La création de province et la gestion des comptes sont réservées à
  l'administrateur PRMP ; le changement du mot de passe du compte reste possible.
- La sauvegarde et la restauration sont réservées à l'administrateur PRMP. La
  restauration est bloquée en mode lecture seule.
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

#!/usr/bin/env bash
#
# Remet SIGMP Offline dans l'état du tout premier lancement :
# sauvegarde la base SQLite, la met de côté, puis relance l'application
# sur l'assistant de première configuration.
#
set -euo pipefail

DATA_DIR="${SIGMP_DATA_DIR:-${XDG_DATA_HOME:-$HOME/.local/share}/SIGMPOffline}"
DB="$DATA_DIR/sigmp.db"
OLD="$DATA_DIR/sigmp.db.old"
TS="$(date +%Y%m%d-%H%M%S)"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="$SCRIPT_DIR/src/SIGMPOffline.Desktop"

usage() {
  cat <<'EOF'
Usage : ./reset-premier-lancement.sh [options]

  (sans option)  Sauvegarde la base, la met de côté, puis relance
                 l'application sur l'assistant de première configuration.
  --no-run       Remet à zéro sans relancer l'application.
  --restore      Restaure la base mise de côté par un reset précédent,
                 puis relance l'application (arrête-la d'abord).
  -h, --help     Affiche cette aide.

Variables :
  SIGMP_DATA_DIR  Dossier de données à utiliser (défaut : ~/.local/share/SIGMPOffline)
EOF
}

die() {
  echo "Erreur : $*" >&2
  exit 1
}

app_running() {
  command -v pgrep >/dev/null 2>&1 && pgrep -f "SIGMPOffline.Desktop" >/dev/null 2>&1
}

# Déplace/restaure les fichiers annexes SQLite (-wal, -shm) avec la base.
move_sidecars() {
  local from="$1" to="$2" ext
  for ext in "-wal" "-shm"; do
    if [[ -f "$from$ext" ]]; then
      mv "$from$ext" "$to$ext"
    fi
  done
}

remove_sidecars() {
  local base="$1" ext
  for ext in "-wal" "-shm"; do
    rm -f "$base$ext"
  done
}

reset_db() {
  if app_running; then
    die "l'application semble en cours d'exécution ; ferme-la d'abord."
  fi

  mkdir -p "$DATA_DIR"

  if [[ ! -f "$DB" ]]; then
    echo "Aucune base existante : l'application démarrera sur l'assistant."
    return
  fi

  cp -p "$DB" "$DB.bak.$TS"
  echo "Sauvegarde    : $DB.bak.$TS"

  if [[ -f "$OLD" ]]; then
    mv "$OLD" "$OLD.$TS"
    echo "Ancien .old   : renommé en $OLD.$TS"
  fi

  mv "$DB" "$OLD"
  move_sidecars "$DB" "$OLD"

  echo "Base de côté  : $OLD"
  echo "L'assistant de première configuration s'affichera au prochain lancement."
}

restore_db() {
  if [[ ! -f "$OLD" ]]; then
    die "aucune base de secours trouvée ($OLD)."
  fi
  if app_running; then
    die "l'application semble en cours d'exécution ; ferme-la d'abord."
  fi

  rm -f "$DB"
  remove_sidecars "$DB"

  mv "$OLD" "$DB"
  move_sidecars "$OLD" "$DB"

  echo "Base restaurée : $DB"
}

MODE="reset"
RUN=1

while [[ $# -gt 0 ]]; do
  case "$1" in
    --no-run)  RUN=0 ;;
    --restore) MODE="restore" ;;
    -h|--help) usage; exit 0 ;;
    *) die "option inconnue : $1 (voir --help)" ;;
  esac
  shift
done

if [[ "$MODE" == "restore" ]]; then
  restore_db
else
  reset_db
fi

if [[ "$RUN" == 1 ]]; then
  command -v dotnet >/dev/null 2>&1 || die "dotnet introuvable dans le PATH."
  [[ -d "$PROJECT" ]] || die "projet introuvable : $PROJECT"
  echo "Lancement de l'application…"
  exec dotnet run --project "$PROJECT"
fi

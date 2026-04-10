#!/usr/bin/env bash
# =============================================================================
# 3D Builder for Printer – Linux Build & Install Script
# =============================================================================
# Usage:
#   chmod +x build.sh
#   ./build.sh            # install deps + verify setup
#   ./build.sh --bundle   # additionally create a standalone binary with PyInstaller
# =============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
VENV_DIR="$SCRIPT_DIR/.venv"
BUNDLE=false

# Parse args
for arg in "$@"; do
    [[ "$arg" == "--bundle" ]] && BUNDLE=true
done

# ── Colours ───────────────────────────────────────────────────────────────────
GREEN='\033[0;32m'; YELLOW='\033[1;33m'; RED='\033[0;31m'; NC='\033[0m'
info()  { echo -e "${GREEN}[INFO]${NC}  $*"; }
warn()  { echo -e "${YELLOW}[WARN]${NC}  $*"; }
error() { echo -e "${RED}[ERROR]${NC} $*" >&2; exit 1; }

# ── Check Python ──────────────────────────────────────────────────────────────
if ! command -v python3 &>/dev/null; then
    error "Python 3 nicht gefunden. Bitte installieren:\n  sudo apt install python3 python3-pip python3-venv"
fi

PY_VER=$(python3 -c "import sys; print(f'{sys.version_info.major}.{sys.version_info.minor}')")
info "Python $PY_VER gefunden."

python3 -c "import sys; sys.exit(0 if sys.version_info >= (3,10) else 1)" || \
    error "Python 3.10+ wird benötigt (gefunden: $PY_VER)."

# ── Virtual environment ───────────────────────────────────────────────────────
if [[ ! -d "$VENV_DIR" ]]; then
    info "Erstelle virtuelles Environment in $VENV_DIR ..."
    python3 -m venv "$VENV_DIR"
fi

source "$VENV_DIR/bin/activate"
info "Virtuelle Umgebung aktiv: $VIRTUAL_ENV"

pip install --upgrade pip --quiet

# ── System dependencies hint ─────────────────────────────────────────────────
if command -v apt-get &>/dev/null; then
    info "System-Pakete (Qt5, OpenGL) – falls noch nicht installiert:"
    echo "      sudo apt install -y python3-pyqt5 python3-opengl libgl1-mesa-glx \\"
    echo "           libglib2.0-0 libxrender1 libxkbcommon-x11-0 libxcb-icccm4 \\"
    echo "           libxcb-image0 libxcb-keysyms1 libxcb-randr0 libxcb-render-util0 \\"
    echo "           libxcb-xinerama0 libxcb-xfixes0"
elif command -v dnf &>/dev/null; then
    info "System-Pakete (Qt5, OpenGL) – falls noch nicht installiert:"
    echo "      sudo dnf install -y python3-qt5 mesa-libGL mesa-libEGL"
fi

# ── Install Python deps ───────────────────────────────────────────────────────
info "Installiere Python-Abhaengigkeiten ..."
pip install -r "$SCRIPT_DIR/requirements.txt" --quiet

# Check CadQuery
if python3 -c "import cadquery" 2>/dev/null; then
    info "CadQuery: OK"
else
    warn "CadQuery nicht importierbar."
    warn "Alternativ: conda install -c conda-forge cadquery"
fi

# Check PyQt5
if python3 -c "import PyQt5" 2>/dev/null; then
    info "PyQt5: OK"
else
    error "PyQt5 fehlt. Bitte manuell installieren: pip install PyQt5"
fi

# Check viewport
if python3 -c "import pyvistaqt" 2>/dev/null; then
    info "pyvistaqt: OK (3D-Viewport aktiv)"
elif python3 -c "import matplotlib" 2>/dev/null; then
    warn "pyvistaqt nicht verfuegbar - nutze matplotlib-Fallback."
else
    warn "Kein 3D-Renderer gefunden."
fi

# ── Optional: PyInstaller bundle ─────────────────────────────────────────────
if $BUNDLE; then
    info "Erstelle standalone Binary mit PyInstaller ..."
    pip install pyinstaller --quiet
    pyinstaller \
        --name "3dbuilder" \
        --onefile \
        --windowed \
        --add-data "PythonBackend:PythonBackend" \
        --add-data "app:app" \
        "$SCRIPT_DIR/main.py"
    info "Binary erstellt: dist/3dbuilder"
fi

# ── Done ──────────────────────────────────────────────────────────────────────
echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN} Build erfolgreich!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo "Anwendung starten:"
echo "  source .venv/bin/activate && python main.py"
echo ""
echo "Oder direkt:"
echo "  .venv/bin/python main.py"
echo ""

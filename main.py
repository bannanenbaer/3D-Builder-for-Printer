#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
3D Builder for Printer – Linux entry point
==========================================
Run with:
    python main.py
Or after installation:
    3dbuilder
"""

import sys
import os

# ── Ensure project root is on the path ────────────────────────────────────────
_ROOT = os.path.dirname(os.path.abspath(__file__))
if _ROOT not in sys.path:
    sys.path.insert(0, _ROOT)

# ── Qt5 high-DPI support ───────────────────────────────────────────────────────
os.environ.setdefault('QT_AUTO_SCREEN_SCALE_FACTOR', '1')

# Fix for some Wayland compositors (use XCB backend for reliability)
if 'WAYLAND_DISPLAY' in os.environ and 'QT_QPA_PLATFORM' not in os.environ:
    os.environ['QT_QPA_PLATFORM'] = 'xcb'

from PyQt5.QtWidgets import QApplication, QSplashScreen, QLabel
from PyQt5.QtCore    import Qt, QTimer
from PyQt5.QtGui     import QFont


def _check_deps() -> list[str]:
    """Return list of missing critical packages."""
    missing = []
    try:
        import PyQt5  # noqa: F401
    except ImportError:
        missing.append('PyQt5')
    return missing


def main():
    missing = _check_deps()
    if missing:
        print(f'FEHLER: Folgende Pakete fehlen: {", ".join(missing)}')
        print('Bitte ausführen:  pip install ' + ' '.join(missing))
        sys.exit(1)

    app = QApplication(sys.argv)
    app.setApplicationName('3D Builder for Printer')
    app.setApplicationVersion('1.0.0')
    app.setOrganizationName('3DBuilderProject')

    # ── Apply theme ───────────────────────────────────────────────────────────
    from app.services import settings_service as cfg
    from app import theme
    if cfg.get('theme', 'dark') == 'dark':
        theme.apply_dark(app)
    else:
        theme.apply_light(app)

    # ── Splash screen ─────────────────────────────────────────────────────────
    splash = QSplashScreen()
    splash.setFixedSize(420, 180)
    splash.setStyleSheet(
        'background: #1a1a2e; border: 2px solid #2d5fa6; border-radius: 8px;'
    )
    lbl = QLabel(
        '<div style="color:#ddd; text-align:center;">'
        '<h2 style="color:#4488ff;">3D Builder for Printer</h2>'
        '<p>Linux Edition · Lade Module…</p>'
        '</div>',
        splash
    )
    lbl.setAlignment(Qt.AlignCenter)
    lbl.setGeometry(0, 0, 420, 180)
    splash.show()
    app.processEvents()

    # ── Main window ───────────────────────────────────────────────────────────
    from app.main_window import MainWindow
    window = MainWindow()

    def _show():
        splash.finish(window)
        window.show()
        window.raise_()

    QTimer.singleShot(800, _show)
    sys.exit(app.exec_())


if __name__ == '__main__':
    main()

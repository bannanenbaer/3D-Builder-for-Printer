# -*- coding: utf-8 -*-
"""AutoFix panel – print quality analysis and one-click repair."""

from PyQt5.QtWidgets import (
    QWidget, QVBoxLayout, QLabel, QPushButton,
    QGroupBox, QHBoxLayout, QProgressBar,
)
from PyQt5.QtCore import pyqtSignal, Qt


_CHECK_LABELS = {
    'sharp_edges':  ('Scharfe Kanten',    'Fillet empfohlen'),
    'small_holes':  ('Kleine Löcher',     'Löcher füllen'),
    'thin_walls':   ('Dünne Wände',       'Wände verstärken'),
    'non_manifold': ('Non-Manifold-Geo.', 'Geometrie reparieren'),
}


class AutoFixPanel(QWidget):
    analyze_requested     = pyqtSignal()
    fix_sharp_edges       = pyqtSignal()
    fix_small_holes       = pyqtSignal()
    fix_thin_walls        = pyqtSignal()
    fix_non_manifold      = pyqtSignal()

    def __init__(self, parent=None):
        super().__init__(parent)
        self._indicators: dict = {}
        self._fix_btns: dict = {}
        self._build_ui()

    def _build_ui(self):
        root = QVBoxLayout(self)
        root.setContentsMargins(8, 8, 8, 8)
        root.setSpacing(8)

        title = QLabel('Druckqualitäts-Analyse')
        title.setStyleSheet('font-weight: bold; font-size: 13px;')
        root.addWidget(title)

        analyze_btn = QPushButton('Jetzt analysieren')
        analyze_btn.clicked.connect(self.analyze_requested)
        root.addWidget(analyze_btn)

        # ── Score bar ────────────────────────────────────────────────────────
        score_grp = QGroupBox('Qualitätsbewertung')
        score_lay = QVBoxLayout()
        self.score_bar = QProgressBar()
        self.score_bar.setRange(0, 100)
        self.score_bar.setValue(0)
        self.score_bar.setTextVisible(True)
        self.score_bar.setFormat('%v / 100')
        score_lay.addWidget(self.score_bar)
        self.score_lbl = QLabel('Noch nicht analysiert')
        self.score_lbl.setAlignment(Qt.AlignCenter)
        score_lay.addWidget(self.score_lbl)
        score_grp.setLayout(score_lay)
        root.addWidget(score_grp)

        # ── Individual checks ────────────────────────────────────────────────
        checks_grp = QGroupBox('Erkannte Probleme')
        checks_lay = QVBoxLayout()
        checks_lay.setSpacing(4)

        signals = {
            'sharp_edges':  self.fix_sharp_edges,
            'small_holes':  self.fix_small_holes,
            'thin_walls':   self.fix_thin_walls,
            'non_manifold': self.fix_non_manifold,
        }

        for key, (label, fix_label) in _CHECK_LABELS.items():
            row = QHBoxLayout()
            indicator = QLabel('●')
            indicator.setStyleSheet('color: #444; font-size: 16px;')
            indicator.setFixedWidth(20)
            row.addWidget(indicator)
            row.addWidget(QLabel(label), 1)
            fix_btn = QPushButton(fix_label)
            fix_btn.setEnabled(False)
            fix_btn.clicked.connect(signals[key].emit)
            row.addWidget(fix_btn)
            checks_lay.addLayout(row)
            self._indicators[key] = indicator
            self._fix_btns[key]   = fix_btn

        checks_grp.setLayout(checks_lay)
        root.addWidget(checks_grp)
        root.addStretch(1)

    def show_results(self, results: dict):
        """Update indicators from analysis result dict."""
        issues = sum(1 for k in _CHECK_LABELS if results.get(k, False))
        score = max(0, 100 - issues * 25)
        self.score_bar.setValue(score)

        if score == 100:
            self.score_lbl.setText('Ausgezeichnet – keine Probleme gefunden!')
            self.score_lbl.setStyleSheet('color: #4c4;')
        elif score >= 75:
            self.score_lbl.setText('Gut – kleinere Probleme erkannt')
            self.score_lbl.setStyleSheet('color: #ca4;')
        else:
            self.score_lbl.setText('Bitte Probleme beheben vor dem Drucken')
            self.score_lbl.setStyleSheet('color: #f44;')

        for key in _CHECK_LABELS:
            has_issue = results.get(key, False)
            color = '#f44' if has_issue else '#4c4'
            self._indicators[key].setStyleSheet(f'color: {color}; font-size: 16px;')
            self._fix_btns[key].setEnabled(has_issue)

    def reset(self):
        self.score_bar.setValue(0)
        self.score_lbl.setText('Noch nicht analysiert')
        self.score_lbl.setStyleSheet('color: #aaa;')
        for key in _CHECK_LABELS:
            self._indicators[key].setStyleSheet('color: #444; font-size: 16px;')
            self._fix_btns[key].setEnabled(False)

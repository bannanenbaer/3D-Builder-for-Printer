# -*- coding: utf-8 -*-
"""Settings panel."""

from PyQt5.QtWidgets import (
    QWidget, QVBoxLayout, QFormLayout, QComboBox, QLineEdit,
    QPushButton, QGroupBox, QCheckBox, QSpinBox, QLabel,
    QHBoxLayout, QFileDialog,
)
from PyQt5.QtCore import pyqtSignal

from app.services import settings_service as cfg


class SettingsPanel(QWidget):
    settings_changed = pyqtSignal()

    def __init__(self, parent=None):
        super().__init__(parent)
        self._build_ui()
        self._load()

    def _build_ui(self):
        root = QVBoxLayout(self)
        root.setContentsMargins(8, 8, 8, 8)
        root.setSpacing(10)

        # ── General ─────────────────────────────────────────────────────────
        gen_grp = QGroupBox('Allgemein')
        gen_form = QFormLayout()
        gen_form.setSpacing(6)

        self.lang_combo = QComboBox()
        self.lang_combo.addItem('Deutsch', 'de')
        self.lang_combo.addItem('English', 'en')
        gen_form.addRow('Sprache:', self.lang_combo)

        self.theme_combo = QComboBox()
        self.theme_combo.addItem('Dunkel (Dark)', 'dark')
        self.theme_combo.addItem('Hell (Light)',  'light')
        gen_form.addRow('Thema:', self.theme_combo)

        self.autosave_cb = QCheckBox('Auto-Speichern aktivieren')
        gen_form.addRow('', self.autosave_cb)

        self.autosave_interval = QSpinBox()
        self.autosave_interval.setRange(30, 3600)
        self.autosave_interval.setSuffix(' s')
        gen_form.addRow('Intervall:', self.autosave_interval)

        gen_grp.setLayout(gen_form)
        root.addWidget(gen_grp)

        # ── AI ───────────────────────────────────────────────────────────────
        ai_grp = QGroupBox('KI-Assistent')
        ai_form = QFormLayout()
        ai_form.setSpacing(6)

        self.ai_combo = QComboBox()
        self.ai_combo.addItem('Lokal (keine API)', 'local')
        self.ai_combo.addItem('Claude API',         'claude')
        self.ai_combo.currentIndexChanged.connect(self._on_ai_changed)
        ai_form.addRow('Anbieter:', self.ai_combo)

        self.api_key = QLineEdit()
        self.api_key.setPlaceholderText('sk-ant-…')
        self.api_key.setEchoMode(QLineEdit.Password)
        ai_form.addRow('Claude API-Key:', self.api_key)

        ai_grp.setLayout(ai_form)
        root.addWidget(ai_grp)

        # ── OpenSCAD ─────────────────────────────────────────────────────────
        scad_grp = QGroupBox('OpenSCAD')
        scad_lay = QHBoxLayout()
        self.openscad_path = QLineEdit()
        self.openscad_path.setPlaceholderText('/usr/bin/openscad')
        scad_lay.addWidget(self.openscad_path, 1)
        browse_btn = QPushButton('...')
        browse_btn.setMaximumWidth(35)
        browse_btn.clicked.connect(self._browse_openscad)
        scad_lay.addWidget(browse_btn)
        scad_grp.setLayout(scad_lay)
        root.addWidget(scad_grp)

        # ── Save button ──────────────────────────────────────────────────────
        save_btn = QPushButton('Einstellungen speichern')
        save_btn.clicked.connect(self._save)
        root.addWidget(save_btn)

        self.status_lbl = QLabel('')
        root.addWidget(self.status_lbl)
        root.addStretch(1)

    def _on_ai_changed(self, idx):
        is_claude = self.ai_combo.currentData() == 'claude'
        self.api_key.setEnabled(is_claude)

    def _browse_openscad(self):
        path, _ = QFileDialog.getOpenFileName(
            self, 'OpenSCAD auswählen', '/usr/bin', 'Alle Dateien (*)')
        if path:
            self.openscad_path.setText(path)

    def _load(self):
        self.lang_combo.setCurrentIndex(
            self.lang_combo.findData(cfg.get('language', 'de')))
        self.theme_combo.setCurrentIndex(
            self.theme_combo.findData(cfg.get('theme', 'dark')))
        self.autosave_cb.setChecked(cfg.get('autosave', True))
        self.autosave_interval.setValue(cfg.get('autosave_interval', 300))
        self.ai_combo.setCurrentIndex(
            self.ai_combo.findData(cfg.get('ai_provider', 'local')))
        self.api_key.setText(cfg.get('claude_api_key', ''))
        self.openscad_path.setText(cfg.get('openscad_path', ''))
        self._on_ai_changed(0)

    def _save(self):
        cfg.set('language',          self.lang_combo.currentData())
        cfg.set('theme',             self.theme_combo.currentData())
        cfg.set('autosave',          self.autosave_cb.isChecked())
        cfg.set('autosave_interval', self.autosave_interval.value())
        cfg.set('ai_provider',       self.ai_combo.currentData())
        cfg.set('claude_api_key',    self.api_key.text().strip())
        cfg.set('openscad_path',     self.openscad_path.text().strip())
        self.status_lbl.setText('Gespeichert.')
        self.status_lbl.setStyleSheet('color: #4c4;')
        self.settings_changed.emit()

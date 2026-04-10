# -*- coding: utf-8 -*-
"""Shape creation panel – lets the user pick a shape and set its parameters."""

from PyQt5.QtWidgets import (
    QWidget, QVBoxLayout, QHBoxLayout, QLabel, QComboBox,
    QPushButton, QDoubleSpinBox, QSpinBox, QGroupBox, QFormLayout,
    QScrollArea, QSizePolicy,
)
from PyQt5.QtCore import pyqtSignal, Qt

from app.services import geometry_service as geo

_SHAPE_LABELS = {
    'box':        'Quader (Box)',
    'sphere':     'Kugel (Sphere)',
    'cylinder':   'Zylinder',
    'cone':       'Kegel (Cone)',
    'torus':      'Torus (Ring)',
    'prism':      'Prisma',
    'pyramid':    'Pyramide',
    'tube':       'Rohr (Tube)',
    'ellipsoid':  'Ellipsoid',
    'hemisphere': 'Halbkugel',
    'l_profile':  'L-Profil',
    't_profile':  'T-Profil',
    'star':       'Stern',
    'polygon':    'Polygon',
    'thread_cyl': 'Gewindezylinder',
}

_PARAM_LABELS = {
    'width': 'Breite (mm)', 'height': 'Höhe (mm)', 'depth': 'Tiefe (mm)',
    'radius': 'Radius (mm)', 'radius_bottom': 'Radius unten (mm)',
    'radius_top': 'Radius oben (mm)', 'radius_major': 'Radius außen (mm)',
    'radius_minor': 'Radius innen (mm)', 'sides': 'Seiten',
    'base_size': 'Basisgröße (mm)', 'radius_outer': 'Außenradius (mm)',
    'radius_inner': 'Innenradius (mm)', 'rx': 'Radius X (mm)',
    'ry': 'Radius Y (mm)', 'rz': 'Radius Z (mm)',
    'thickness': 'Wandstärke (mm)', 'length': 'Länge (mm)',
    'outer_r': 'Außenradius (mm)', 'inner_r': 'Innenradius (mm)',
    'points': 'Zacken', 'pitch': 'Steigung (mm)',
}


class ShapePanel(QWidget):
    shape_requested = pyqtSignal(str, dict)   # (shape_type, params)

    def __init__(self, parent=None):
        super().__init__(parent)
        self._param_widgets: dict = {}
        self._build_ui()

    def _build_ui(self):
        root = QVBoxLayout(self)
        root.setContentsMargins(8, 8, 8, 8)
        root.setSpacing(6)

        # ── Shape selector ──────────────────────────────────────────────────
        lbl = QLabel('Form auswählen:')
        lbl.setStyleSheet('font-weight: bold;')
        root.addWidget(lbl)

        self.shape_combo = QComboBox()
        defs = geo.get_shape_defs()
        for key in defs:
            self.shape_combo.addItem(_SHAPE_LABELS.get(key, key), userData=key)
        self.shape_combo.currentIndexChanged.connect(self._on_shape_changed)
        root.addWidget(self.shape_combo)

        # ── Parameter area ──────────────────────────────────────────────────
        self.param_group = QGroupBox('Parameter')
        self.param_form  = QFormLayout()
        self.param_form.setContentsMargins(8, 8, 8, 8)
        self.param_form.setSpacing(4)
        self.param_group.setLayout(self.param_form)

        scroll = QScrollArea()
        scroll.setWidget(self.param_group)
        scroll.setWidgetResizable(True)
        scroll.setFrameShape(QScrollArea.NoFrame)
        scroll.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Expanding)
        root.addWidget(scroll, 1)

        # ── Add button ──────────────────────────────────────────────────────
        self.add_btn = QPushButton('Form hinzufügen')
        self.add_btn.clicked.connect(self._emit_add)
        root.addWidget(self.add_btn)

        # Init first shape
        self._on_shape_changed(0)

    def _on_shape_changed(self, _index):
        shape_type = self.shape_combo.currentData()
        if not shape_type:
            return
        defs = geo.get_shape_defs()
        params_def = defs.get(shape_type, {}).get('params', {})

        # Clear old widgets
        while self.param_form.rowCount() > 0:
            self.param_form.removeRow(0)
        self._param_widgets.clear()

        for key, meta in params_def.items():
            label = _PARAM_LABELS.get(key, key)
            if meta['type'] == 'int':
                spin = QSpinBox()
                spin.setMinimum(int(meta.get('min', 1)))
                spin.setMaximum(int(meta.get('max', 9999)))
                spin.setValue(int(meta['default']))
            else:
                spin = QDoubleSpinBox()
                spin.setMinimum(float(meta.get('min', 0.0)))
                spin.setMaximum(float(meta.get('max', 9999.0)))
                spin.setValue(float(meta['default']))
                spin.setDecimals(2)
                spin.setSingleStep(0.5)
            self.param_form.addRow(label, spin)
            self._param_widgets[key] = (spin, meta['type'])

    def _collect_params(self) -> dict:
        result = {}
        for key, (widget, typ) in self._param_widgets.items():
            result[key] = int(widget.value()) if typ == 'int' else float(widget.value())
        return result

    def _emit_add(self):
        shape_type = self.shape_combo.currentData()
        if shape_type:
            self.shape_requested.emit(shape_type, self._collect_params())

    def current_shape_type(self) -> str:
        return self.shape_combo.currentData() or ''

    def current_params(self) -> dict:
        return self._collect_params()

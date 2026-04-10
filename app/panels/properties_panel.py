# -*- coding: utf-8 -*-
"""Properties panel – shows and edits properties of the selected scene object."""

from PyQt5.QtWidgets import (
    QWidget, QVBoxLayout, QFormLayout, QLabel, QDoubleSpinBox,
    QPushButton, QGroupBox, QHBoxLayout, QLineEdit, QSizePolicy,
)
from PyQt5.QtCore import pyqtSignal, Qt


class PropertiesPanel(QWidget):
    # Emitted when user wants to apply fillet/chamfer/position changes
    fillet_requested  = pyqtSignal(float)       # radius
    chamfer_requested = pyqtSignal(float)       # size
    position_changed  = pyqtSignal(float, float, float)   # x y z
    rotation_changed  = pyqtSignal(float, float, float)
    delete_requested  = pyqtSignal()
    boolean_requested = pyqtSignal(str)         # 'union'|'cut'|'intersect'

    def __init__(self, parent=None):
        super().__init__(parent)
        self._obj_id: str = ''
        self._build_ui()
        self.clear()

    def _build_ui(self):
        root = QVBoxLayout(self)
        root.setContentsMargins(8, 8, 8, 8)
        root.setSpacing(8)

        # ── Object name ─────────────────────────────────────────────────────
        row = QHBoxLayout()
        row.addWidget(QLabel('Objekt:'))
        self.name_lbl = QLabel('—')
        self.name_lbl.setStyleSheet('font-weight: bold;')
        row.addWidget(self.name_lbl, 1)
        root.addLayout(row)

        # ── Position ────────────────────────────────────────────────────────
        pos_grp = QGroupBox('Position (mm)')
        pos_form = QFormLayout()
        pos_form.setSpacing(4)
        self.pos_x = self._dspin(-9999, 9999)
        self.pos_y = self._dspin(-9999, 9999)
        self.pos_z = self._dspin(-9999, 9999)
        pos_form.addRow('X:', self.pos_x)
        pos_form.addRow('Y:', self.pos_y)
        pos_form.addRow('Z:', self.pos_z)
        pos_apply = QPushButton('Anwenden')
        pos_apply.clicked.connect(self._apply_pos)
        pos_form.addRow('', pos_apply)
        pos_grp.setLayout(pos_form)
        root.addWidget(pos_grp)

        # ── Rotation ────────────────────────────────────────────────────────
        rot_grp = QGroupBox('Rotation (°)')
        rot_form = QFormLayout()
        rot_form.setSpacing(4)
        self.rot_x = self._dspin(-360, 360)
        self.rot_y = self._dspin(-360, 360)
        self.rot_z = self._dspin(-360, 360)
        rot_form.addRow('X:', self.rot_x)
        rot_form.addRow('Y:', self.rot_y)
        rot_form.addRow('Z:', self.rot_z)
        rot_apply = QPushButton('Anwenden')
        rot_apply.clicked.connect(self._apply_rot)
        rot_form.addRow('', rot_apply)
        rot_grp.setLayout(rot_form)
        root.addWidget(rot_grp)

        # ── Fillet / Chamfer ────────────────────────────────────────────────
        edge_grp = QGroupBox('Kantenbearbeitung')
        edge_form = QFormLayout()
        edge_form.setSpacing(4)
        self.fillet_r = self._dspin(0.01, 100, val=1.0)
        self.chamfer_s = self._dspin(0.01, 100, val=1.0)
        edge_form.addRow('Fillet-Radius (mm):', self.fillet_r)
        f_btn = QPushButton('Fillet anwenden')
        f_btn.clicked.connect(lambda: self.fillet_requested.emit(self.fillet_r.value()))
        edge_form.addRow('', f_btn)
        edge_form.addRow('Chamfer-Größe (mm):', self.chamfer_s)
        c_btn = QPushButton('Chamfer anwenden')
        c_btn.clicked.connect(lambda: self.chamfer_requested.emit(self.chamfer_s.value()))
        edge_form.addRow('', c_btn)
        edge_grp.setLayout(edge_form)
        root.addWidget(edge_grp)

        # ── Boolean operations ───────────────────────────────────────────────
        bool_grp = QGroupBox('Boolean (mit nächstem Objekt)')
        bool_lay = QHBoxLayout()
        for lbl, op in [('Union', 'union'), ('Cut', 'cut'), ('Intersect', 'intersect')]:
            btn = QPushButton(lbl)
            btn.clicked.connect(lambda checked, o=op: self.boolean_requested.emit(o))
            bool_lay.addWidget(btn)
        bool_grp.setLayout(bool_lay)
        root.addWidget(bool_grp)

        # ── Delete ───────────────────────────────────────────────────────────
        del_btn = QPushButton('Objekt löschen')
        del_btn.setProperty('danger', True)
        del_btn.clicked.connect(self.delete_requested)
        root.addWidget(del_btn)

        root.addStretch(1)

    @staticmethod
    def _dspin(lo, hi, val=0.0):
        sp = QDoubleSpinBox()
        sp.setRange(lo, hi)
        sp.setValue(val)
        sp.setDecimals(2)
        sp.setSingleStep(1.0)
        return sp

    def _apply_pos(self):
        self.position_changed.emit(
            self.pos_x.value(), self.pos_y.value(), self.pos_z.value())

    def _apply_rot(self):
        self.rotation_changed.emit(
            self.rot_x.value(), self.rot_y.value(), self.rot_z.value())

    def load(self, obj):
        """Populate panel from a SceneObject."""
        self._obj_id = obj.obj_id
        self.name_lbl.setText(obj.name)
        self.pos_x.setValue(obj.pos_x)
        self.pos_y.setValue(obj.pos_y)
        self.pos_z.setValue(obj.pos_z)
        self.rot_x.setValue(obj.rot_x)
        self.rot_y.setValue(obj.rot_y)
        self.rot_z.setValue(obj.rot_z)
        self.setEnabled(True)

    def clear(self):
        self._obj_id = ''
        self.name_lbl.setText('— kein Objekt ausgewählt —')
        self.setEnabled(False)

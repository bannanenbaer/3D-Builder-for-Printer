# -*- coding: utf-8 -*-
"""
3D Viewport widget.
Uses pyvistaqt (PyVista + Qt) when available, falls back to a plain
OpenGL / matplotlib canvas.
"""

import os
import sys
import numpy as np

from PyQt5.QtWidgets import QWidget, QVBoxLayout, QLabel
from PyQt5.QtCore    import Qt, pyqtSignal

# ── Try pyvistaqt first ────────────────────────────────────────────────────────
_PV_OK = False
try:
    import pyvista as pv
    import pyvistaqt as pvqt
    _PV_OK = True
except ImportError:
    pass

# ── Fallback: matplotlib ───────────────────────────────────────────────────────
_MPL_OK = False
if not _PV_OK:
    try:
        from matplotlib.backends.backend_qt5agg import FigureCanvasQTAgg as FigureCanvas
        from matplotlib.figure import Figure
        import mpl_toolkits.mplot3d  # noqa: F401
        _MPL_OK = True
    except ImportError:
        pass


def _load_stl_mesh(stl_path: str):
    """Load an STL file; returns a list of (verts, faces) tuples."""
    verts_all, faces_all = [], []
    with open(stl_path, 'r', errors='ignore') as f:
        content = f.read()
    # Try ASCII STL
    import re
    facets = re.findall(
        r'facet normal[^\n]*\n\s*outer loop(.*?)endloop',
        content, re.DOTALL)
    if facets:
        for facet in facets:
            vlines = re.findall(r'vertex\s+([-\d.e+]+)\s+([-\d.e+]+)\s+([-\d.e+]+)', facet)
            if len(vlines) == 3:
                for v in vlines:
                    verts_all.append([float(v[0]), float(v[1]), float(v[2])])
    # Binary fallback
    if not verts_all:
        with open(stl_path, 'rb') as f:
            f.read(80)  # header
            n = int.from_bytes(f.read(4), 'little')
            for _ in range(n):
                f.read(12)  # normal
                for __ in range(3):
                    v = np.frombuffer(f.read(12), dtype='<f4')
                    verts_all.append(v.tolist())
                f.read(2)  # attr
    return np.array(verts_all, dtype=float) if verts_all else np.zeros((0, 3))


# ══════════════════════════════════════════════════════════════════════════════
#  PyVista viewport
# ══════════════════════════════════════════════════════════════════════════════

class PyVistaViewport(QWidget):
    object_clicked = pyqtSignal(str)

    def __init__(self, parent=None):
        super().__init__(parent)
        self._actors = {}  # obj_id -> pyvista actor
        layout = QVBoxLayout(self)
        layout.setContentsMargins(0, 0, 0, 0)

        self.plotter = pvqt.BackgroundPlotter(
            show=False,
            window_size=(800, 600),
            off_screen=False,
        )
        self.plotter.set_background('#1a1a2e')
        self.plotter.add_axes(line_width=2)
        self.plotter.enable_trackball_style()

        # Embed the plotter's Qt widget
        layout.addWidget(self.plotter.interactor)

    def add_or_update(self, obj_id: str, stl_path: str,
                      pos=(0,0,0), rot=(0,0,0), color='#4488ff', selected=False):
        if not os.path.isfile(stl_path):
            return
        # Remove previous actor for this object
        if obj_id in self._actors:
            self.plotter.remove_actor(self._actors[obj_id])

        mesh = pv.read(stl_path)
        if any(pos):
            mesh = mesh.translate(pos)

        show_edges = selected
        actor = self.plotter.add_mesh(
            mesh,
            color=color if not selected else '#ffaa00',
            show_edges=show_edges,
            edge_color='#ffff00' if selected else '#888888',
            opacity=1.0,
            pickable=True,
        )
        self._actors[obj_id] = actor
        self.plotter.render()

    def remove(self, obj_id: str):
        if obj_id in self._actors:
            self.plotter.remove_actor(self._actors.pop(obj_id))
            self.plotter.render()

    def clear(self):
        for actor in self._actors.values():
            self.plotter.remove_actor(actor)
        self._actors.clear()
        self.plotter.render()

    def reset_camera(self):
        self.plotter.reset_camera()
        self.plotter.render()

    def set_background(self, color: str):
        self.plotter.set_background(color)
        self.plotter.render()


# ══════════════════════════════════════════════════════════════════════════════
#  Matplotlib fallback viewport
# ══════════════════════════════════════════════════════════════════════════════

class MatplotlibViewport(QWidget):
    object_clicked = pyqtSignal(str)

    def __init__(self, parent=None):
        super().__init__(parent)
        self._meshes = {}   # obj_id -> vertices array
        layout = QVBoxLayout(self)
        layout.setContentsMargins(0, 0, 0, 0)

        self.fig = Figure(facecolor='#1a1a2e')
        self.canvas = FigureCanvas(self.fig)
        layout.addWidget(self.canvas)

        self.ax = self.fig.add_subplot(111, projection='3d')
        self._style_ax()

    def _style_ax(self):
        self.ax.set_facecolor('#1a1a2e')
        self.ax.tick_params(colors='#aaaaaa', labelsize=7)
        for pane in [self.ax.xaxis.pane, self.ax.yaxis.pane, self.ax.zaxis.pane]:
            pane.fill = False
            pane.set_edgecolor('#333333')
        self.ax.set_xlabel('X', color='#aaa', fontsize=8)
        self.ax.set_ylabel('Y', color='#aaa', fontsize=8)
        self.ax.set_zlabel('Z', color='#aaa', fontsize=8)

    def _redraw(self):
        self.ax.cla()
        self._style_ax()
        for obj_id, verts in self._meshes.items():
            if len(verts) >= 3:
                xs, ys, zs = verts[:,0], verts[:,1], verts[:,2]
                self.ax.scatter(xs[::max(1, len(xs)//500)],
                                ys[::max(1, len(ys)//500)],
                                zs[::max(1, len(zs)//500)],
                                s=0.5, color='#4488ff', alpha=0.8)
        self.canvas.draw()

    def add_or_update(self, obj_id: str, stl_path: str, **kwargs):
        if not os.path.isfile(stl_path):
            return
        verts = _load_stl_mesh(stl_path)
        self._meshes[obj_id] = verts
        self._redraw()

    def remove(self, obj_id: str):
        self._meshes.pop(obj_id, None)
        self._redraw()

    def clear(self):
        self._meshes.clear()
        self._redraw()

    def reset_camera(self):
        self.ax.autoscale()
        self.canvas.draw()

    def set_background(self, color: str):
        self.fig.set_facecolor(color)
        self.ax.set_facecolor(color)
        self.canvas.draw()


# ══════════════════════════════════════════════════════════════════════════════
#  No-renderer placeholder
# ══════════════════════════════════════════════════════════════════════════════

class PlaceholderViewport(QWidget):
    object_clicked = pyqtSignal(str)

    def __init__(self, parent=None):
        super().__init__(parent)
        lbl = QLabel(
            'Kein 3D-Renderer verfügbar.\n'
            'Bitte installiere pyvistaqt:\n\n'
            '  pip install pyvistaqt pyvista\n\n'
            'oder matplotlib:\n\n'
            '  pip install matplotlib',
            self)
        lbl.setAlignment(Qt.AlignCenter)
        lbl.setStyleSheet('color: #aaa; font-size: 14px;')
        lay = QVBoxLayout(self)
        lay.addWidget(lbl)

    def add_or_update(self, *a, **kw): pass
    def remove(self, *a, **kw): pass
    def clear(self): pass
    def reset_camera(self): pass
    def set_background(self, *a): pass


# ══════════════════════════════════════════════════════════════════════════════
#  Factory
# ══════════════════════════════════════════════════════════════════════════════

def create_viewport(parent=None) -> QWidget:
    if _PV_OK:
        return PyVistaViewport(parent)
    if _MPL_OK:
        return MatplotlibViewport(parent)
    return PlaceholderViewport(parent)

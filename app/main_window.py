# -*- coding: utf-8 -*-
"""
Main application window for 3D Builder (Linux/PyQt5 edition).
Replaces the Windows WPF CSharpUI entirely.
"""

import os
import threading
import uuid

from PyQt5.QtWidgets import (
    QMainWindow, QDockWidget, QListWidget, QListWidgetItem,
    QAction, QToolBar, QStatusBar, QLabel, QFileDialog,
    QMessageBox, QWidget, QVBoxLayout, QInputDialog,
    QSplitter, QApplication,
)
from PyQt5.QtCore    import Qt, QTimer, pyqtSignal, QObject, QThread
from PyQt5.QtGui     import QIcon, QKeySequence

from app.scene   import Scene, SceneObject
from app.viewport import create_viewport
from app.panels.shape_panel      import ShapePanel
from app.panels.properties_panel import PropertiesPanel
from app.panels.scad_panel       import ScadPanel
from app.panels.autofix_panel    import AutoFixPanel
from app.panels.assistant_panel  import AssistantPanel
from app.panels.settings_panel   import SettingsPanel
from app.services import geometry_service as geo
from app.services import settings_service as cfg


# ── Background worker ──────────────────────────────────────────────────────────

class _GeometryWorker(QObject):
    finished = pyqtSignal(dict)
    error    = pyqtSignal(str)

    def __init__(self, fn, *args, **kwargs):
        super().__init__()
        self._fn   = fn
        self._args = args
        self._kw   = kwargs

    def run(self):
        try:
            result = self._fn(*self._args, **self._kw)
            self.finished.emit(result)
        except Exception as e:
            self.error.emit(str(e))


# ══════════════════════════════════════════════════════════════════════════════

class MainWindow(QMainWindow):
    def __init__(self):
        super().__init__()
        self._scene   = Scene()
        self._busy    = False
        self._autosave_timer = QTimer(self)
        self._current_file: str = ''

        self.setWindowTitle('3D Builder for Printer')
        self.resize(1280, 800)
        self.setMinimumSize(900, 600)

        self._build_ui()
        self._setup_menus()
        self._setup_toolbar()
        self._setup_docks()
        self._setup_autosave()
        self._update_status()

    # ── UI construction ───────────────────────────────────────────────────────

    def _build_ui(self):
        # Central widget: scene tree + viewport
        splitter = QSplitter(Qt.Horizontal)

        # Left: scene object list
        self._scene_list = QListWidget()
        self._scene_list.setMaximumWidth(200)
        self._scene_list.setMinimumWidth(140)
        self._scene_list.currentRowChanged.connect(self._on_scene_selection)
        self._scene_list.setToolTip('Szenenobjekte')
        splitter.addWidget(self._scene_list)

        # Centre: 3D viewport
        self.viewport = create_viewport()
        splitter.addWidget(self.viewport)
        splitter.setStretchFactor(1, 1)

        self.setCentralWidget(splitter)

        # Status bar
        self._status_lbl = QLabel('Bereit')
        self.statusBar().addPermanentWidget(self._status_lbl)
        self._engine_lbl = QLabel('')
        self.statusBar().addPermanentWidget(self._engine_lbl)

    def _setup_menus(self):
        mb = self.menuBar()

        # File
        file_menu = mb.addMenu('&Datei')
        a_new = QAction('&Neu', self, shortcut='Ctrl+N')
        a_new.triggered.connect(self._action_new)
        file_menu.addAction(a_new)

        a_open = QAction('&Öffnen…', self, shortcut='Ctrl+O')
        a_open.triggered.connect(self._action_open)
        file_menu.addAction(a_open)

        a_save = QAction('&Speichern', self, shortcut='Ctrl+S')
        a_save.triggered.connect(self._action_save)
        file_menu.addAction(a_save)

        a_save_as = QAction('Speichern &unter…', self, shortcut='Ctrl+Shift+S')
        a_save_as.triggered.connect(self._action_save_as)
        file_menu.addAction(a_save_as)

        file_menu.addSeparator()

        a_imp_stl = QAction('STL &importieren…', self)
        a_imp_stl.triggered.connect(self._action_import_stl)
        file_menu.addAction(a_imp_stl)

        a_imp_3mf = QAction('3MF importieren…', self)
        a_imp_3mf.triggered.connect(self._action_import_3mf)
        file_menu.addAction(a_imp_3mf)

        file_menu.addSeparator()

        a_exp_stl = QAction('Als STL &exportieren…', self)
        a_exp_stl.triggered.connect(self._action_export_stl)
        file_menu.addAction(a_exp_stl)

        a_exp_scad = QAction('Als OpenSCAD exportieren…', self)
        a_exp_scad.triggered.connect(self._action_export_scad)
        file_menu.addAction(a_exp_scad)

        file_menu.addSeparator()
        a_quit = QAction('&Beenden', self, shortcut='Ctrl+Q')
        a_quit.triggered.connect(self.close)
        file_menu.addAction(a_quit)

        # Edit
        edit_menu = mb.addMenu('&Bearbeiten')
        a_undo = QAction('&Rückgängig', self, shortcut='Ctrl+Z')
        a_undo.triggered.connect(self._action_undo)
        edit_menu.addAction(a_undo)

        a_redo = QAction('&Wiederholen', self, shortcut='Ctrl+Y')
        a_redo.triggered.connect(self._action_redo)
        edit_menu.addAction(a_redo)

        edit_menu.addSeparator()
        a_del = QAction('Objekt &löschen', self, shortcut='Delete')
        a_del.triggered.connect(self._action_delete_selected)
        edit_menu.addAction(a_del)

        a_rename = QAction('Objekt &umbenennen', self, shortcut='F2')
        a_rename.triggered.connect(self._action_rename_selected)
        edit_menu.addAction(a_rename)

        # View
        view_menu = mb.addMenu('&Ansicht')
        a_reset_cam = QAction('Kamera zurücksetzen', self, shortcut='R')
        a_reset_cam.triggered.connect(lambda: self.viewport.reset_camera())
        view_menu.addAction(a_reset_cam)

        # Help
        help_menu = mb.addMenu('&Hilfe')
        a_about = QAction('Über 3D Builder…', self)
        a_about.triggered.connect(self._action_about)
        help_menu.addAction(a_about)

    def _setup_toolbar(self):
        tb = QToolBar('Werkzeuge')
        tb.setMovable(False)
        tb.setToolButtonStyle(Qt.ToolButtonTextUnderIcon)
        self.addToolBar(tb)

        for label, slot, tip in [
            ('Neu',        self._action_new,              'Neue Szene (Ctrl+N)'),
            ('Importieren', self._action_import_stl,      'STL-Datei importieren'),
            ('Exportieren', self._action_export_stl,      'Als STL exportieren'),
            ('Rückgängig', self._action_undo,             'Rückgängig (Ctrl+Z)'),
            ('Wiederholen', self._action_redo,            'Wiederholen (Ctrl+Y)'),
            ('Löschen',    self._action_delete_selected,  'Objekt löschen (Del)'),
            ('Reset Cam',  lambda: self.viewport.reset_camera(), 'Kamera zurücksetzen (R)'),
        ]:
            act = QAction(label, self)
            act.setStatusTip(tip)
            act.triggered.connect(slot)
            tb.addAction(act)

    def _setup_docks(self):
        def _dock(title, widget, area, min_w=220, max_w=320):
            d = QDockWidget(title, self)
            d.setWidget(widget)
            d.setMinimumWidth(min_w)
            d.setMaximumWidth(max_w)
            d.setFeatures(
                QDockWidget.DockWidgetMovable |
                QDockWidget.DockWidgetFloatable |
                QDockWidget.DockWidgetClosable
            )
            self.addDockWidget(area, d)
            return d

        # Left docks
        self._shape_panel = ShapePanel()
        self._shape_panel.shape_requested.connect(self._on_shape_requested)
        _dock('Formen', self._shape_panel, Qt.LeftDockWidgetArea)

        # Right docks
        self._props_panel = PropertiesPanel()
        self._props_panel.fillet_requested.connect(self._on_fillet)
        self._props_panel.chamfer_requested.connect(self._on_chamfer)
        self._props_panel.position_changed.connect(self._on_position_changed)
        self._props_panel.rotation_changed.connect(self._on_rotation_changed)
        self._props_panel.delete_requested.connect(self._action_delete_selected)
        self._props_panel.boolean_requested.connect(self._on_boolean)
        _dock('Eigenschaften', self._props_panel, Qt.RightDockWidgetArea)

        self._autofix_panel = AutoFixPanel()
        self._autofix_panel.analyze_requested.connect(self._on_analyze)
        self._autofix_panel.fix_sharp_edges.connect(
            lambda: self._on_autofix('sharp_edges'))
        self._autofix_panel.fix_small_holes.connect(
            lambda: self._on_autofix('small_holes'))
        self._autofix_panel.fix_thin_walls.connect(
            lambda: self._on_autofix('thin_walls'))
        self._autofix_panel.fix_non_manifold.connect(
            lambda: self._on_autofix('non_manifold'))
        _dock('AutoFix', self._autofix_panel, Qt.RightDockWidgetArea)

        # Bottom docks
        self._scad_panel = ScadPanel()
        self._scad_panel.compile_requested.connect(self._on_scad_compile)
        self._scad_panel.export_requested.connect(self._action_export_scad)
        scad_dock = _dock('SCAD-Editor', self._scad_panel,
                          Qt.BottomDockWidgetArea, min_w=400, max_w=9999)
        scad_dock.setMinimumHeight(180)

        self._assistant_panel = AssistantPanel()
        _dock('Brixl Assistent', self._assistant_panel,
              Qt.BottomDockWidgetArea, min_w=300, max_w=500)

        self._settings_panel = SettingsPanel()
        self._settings_panel.settings_changed.connect(self._on_settings_changed)
        _dock('Einstellungen', self._settings_panel,
              Qt.RightDockWidgetArea, min_w=220, max_w=350)

    def _setup_autosave(self):
        if cfg.get('autosave', True):
            interval = int(cfg.get('autosave_interval', 300)) * 1000
            self._autosave_timer.timeout.connect(self._autosave)
            self._autosave_timer.start(interval)

    # ── Scene list helpers ────────────────────────────────────────────────────

    def _refresh_scene_list(self):
        self._scene_list.blockSignals(True)
        self._scene_list.clear()
        for obj in self._scene.all():
            item = QListWidgetItem(obj.name)
            item.setData(Qt.UserRole, obj.obj_id)
            if obj.selected:
                item.setSelected(True)
            self._scene_list.addItem(item)
        self._scene_list.blockSignals(False)

    def _on_scene_selection(self, row: int):
        if row < 0:
            self._props_panel.clear()
            self._scene.select(None)
            return
        item = self._scene_list.item(row)
        if not item:
            return
        obj_id = item.data(Qt.UserRole)
        self._scene.select(obj_id)
        obj = self._scene.get(obj_id)
        if obj:
            self._props_panel.load(obj)
        # Highlight in viewport
        for o in self._scene.all():
            self.viewport.add_or_update(
                o.obj_id, o.stl_path,
                pos=(o.pos_x, o.pos_y, o.pos_z),
                selected=o.selected
            )

    # ── Status helpers ────────────────────────────────────────────────────────

    def _update_status(self, msg: str = ''):
        obj_count = len(self._scene.all())
        self._status_lbl.setText(
            msg or f'{obj_count} Objekt{"e" if obj_count != 1 else ""} in der Szene'
        )
        cq = 'CadQuery' if geo.cq_available() else \
             ('OpenSCAD' if geo.openscad_available() else 'Fallback')
        self._engine_lbl.setText(f'Engine: {cq}')

    def _set_busy(self, busy: bool, msg: str = ''):
        self._busy = busy
        if busy:
            self._update_status(msg or 'Berechne…')
            QApplication.setOverrideCursor(Qt.WaitCursor)
        else:
            QApplication.restoreOverrideCursor()
            self._update_status()

    # ── Worker helper ─────────────────────────────────────────────────────────

    def _run_in_thread(self, fn, *args, on_done=None, on_error=None, **kwargs):
        thread = QThread(self)
        worker = _GeometryWorker(fn, *args, **kwargs)
        worker.moveToThread(thread)
        thread.started.connect(worker.run)

        def _done(result):
            thread.quit()
            self._set_busy(False)
            if on_done:
                on_done(result)

        def _err(msg):
            thread.quit()
            self._set_busy(False)
            if on_error:
                on_error(msg)
            else:
                QMessageBox.critical(self, 'Fehler', msg)

        worker.finished.connect(_done)
        worker.error.connect(_err)
        thread.finished.connect(thread.deleteLater)
        self._set_busy(True, 'Berechne Geometrie…')
        thread.start()

    # ── Shape creation ────────────────────────────────────────────────────────

    def _on_shape_requested(self, shape_type: str, params: dict):
        if self._busy:
            return
        obj_id = self._scene.new_id()
        name   = f'{shape_type.capitalize()} {len(self._scene.all()) + 1}'

        def _done(result):
            obj = SceneObject(
                obj_id=obj_id, name=name, shape_type=shape_type,
                params=params, stl_path=result['stl_path'],
            )
            self._scene.add(obj)
            self.viewport.add_or_update(obj_id, obj.stl_path)
            self._refresh_scene_list()
            if result.get('warning'):
                self.statusBar().showMessage(result['warning'], 5000)

        self._run_in_thread(geo.create_shape, shape_type, params, obj_id,
                            on_done=_done)

    # ── Fillet / Chamfer ──────────────────────────────────────────────────────

    def _on_fillet(self, radius: float):
        obj = self._scene.selected()
        if not obj:
            return
        def _done(result):
            self._scene.update(obj.obj_id, stl_path=result['stl_path'])
            self.viewport.add_or_update(obj.obj_id, result['stl_path'], selected=True)
        self._run_in_thread(
            geo.apply_fillet,
            obj.obj_id, obj.shape_type, obj.params,
            (obj.pos_x, obj.pos_y, obj.pos_z),
            (obj.rot_x, obj.rot_y, obj.rot_z),
            radius,
            on_done=_done
        )

    def _on_chamfer(self, size: float):
        obj = self._scene.selected()
        if not obj:
            return
        def _done(result):
            self._scene.update(obj.obj_id, stl_path=result['stl_path'])
            self.viewport.add_or_update(obj.obj_id, result['stl_path'], selected=True)
        self._run_in_thread(
            geo.apply_chamfer,
            obj.obj_id, obj.shape_type, obj.params,
            (obj.pos_x, obj.pos_y, obj.pos_z),
            (obj.rot_x, obj.rot_y, obj.rot_z),
            size,
            on_done=_done
        )

    # ── Position / Rotation ───────────────────────────────────────────────────

    def _on_position_changed(self, x, y, z):
        obj = self._scene.selected()
        if not obj:
            return
        self._scene.update(obj.obj_id, pos_x=x, pos_y=y, pos_z=z)
        self.viewport.add_or_update(
            obj.obj_id, obj.stl_path, pos=(x, y, z), selected=True)

    def _on_rotation_changed(self, rx, ry, rz):
        obj = self._scene.selected()
        if not obj:
            return
        self._scene.update(obj.obj_id, rot_x=rx, rot_y=ry, rot_z=rz)
        # Rebuild mesh with new rotation
        if self._busy:
            return
        def _done(result):
            self._scene.update(obj.obj_id, stl_path=result['stl_path'])
            self.viewport.add_or_update(obj.obj_id, result['stl_path'], selected=True)
        self._run_in_thread(
            geo.create_shape, obj.shape_type, obj.params, obj.obj_id,
            pos=(obj.pos_x, obj.pos_y, obj.pos_z),
            rot=(rx, ry, rz),
            on_done=_done
        )

    # ── Boolean ───────────────────────────────────────────────────────────────

    def _on_boolean(self, op: str):
        objs = self._scene.all()
        sel  = self._scene.selected()
        if not sel or len(objs) < 2:
            QMessageBox.information(self, 'Boolean',
                'Bitte mindestens 2 Objekte hinzufügen und ein Objekt auswählen.')
            return
        # Find next object
        idx = next((i for i, o in enumerate(objs) if o.obj_id == sel.obj_id), 0)
        other = objs[(idx + 1) % len(objs)]

        def _done(result):
            new_id = self._scene.new_id()
            combined = SceneObject(
                obj_id=new_id,
                name=f'Bool_{op}_{new_id}',
                shape_type='box',  # placeholder type
                params={},
                stl_path=result['stl_path'],
            )
            self._scene.remove(sel.obj_id)
            self._scene.remove(other.obj_id)
            self._scene.add(combined)
            self.viewport.remove(sel.obj_id)
            self.viewport.remove(other.obj_id)
            self.viewport.add_or_update(new_id, combined.stl_path)
            self._refresh_scene_list()

        self._run_in_thread(geo.boolean_op, op, sel.to_dict(), other.to_dict(),
                            on_done=_done)

    # ── SCAD ──────────────────────────────────────────────────────────────────

    def _on_scad_compile(self, code: str):
        if self._busy:
            return
        def _done(result):
            obj_id = self._scene.new_id()
            obj = SceneObject(
                obj_id=obj_id, name=f'SCAD_{obj_id}',
                shape_type='scad', params={}, stl_path=result['stl_path'],
            )
            self._scene.add(obj)
            self.viewport.add_or_update(obj_id, obj.stl_path)
            self._refresh_scene_list()
            self._scad_panel.set_status(True)

        def _err(msg):
            self._scad_panel.set_status(False, msg)

        self._run_in_thread(geo.compile_scad, code, on_done=_done, on_error=_err)

    # ── AutoFix ───────────────────────────────────────────────────────────────

    def _on_analyze(self):
        obj = self._scene.selected()
        if not obj:
            QMessageBox.information(self, 'AutoFix', 'Bitte zuerst ein Objekt auswählen.')
            return

        def _done(result):
            self._autofix_panel.show_results(result)

        self._run_in_thread(geo.analyze_model, obj.obj_id, on_done=_done)

    def _on_autofix(self, issue_type: str):
        obj = self._scene.selected()
        if not obj:
            return
        # Map issue type to fix function
        fix_map = {
            'sharp_edges':  (geo.apply_fillet,     [obj.obj_id, obj.shape_type,
                                                    obj.params,
                                                    (obj.pos_x, obj.pos_y, obj.pos_z),
                                                    (obj.rot_x, obj.rot_y, obj.rot_z),
                                                    1.0]),
        }
        if issue_type not in fix_map:
            QMessageBox.information(self, 'AutoFix',
                f'Automatische Reparatur für "{issue_type}" noch nicht implementiert.')
            return
        fn, args = fix_map[issue_type]

        def _done(result):
            self._scene.update(obj.obj_id, stl_path=result['stl_path'])
            self.viewport.add_or_update(obj.obj_id, result['stl_path'], selected=True)
            self._on_analyze()

        self._run_in_thread(fn, *args, on_done=_done)

    # ── File actions ──────────────────────────────────────────────────────────

    def _action_new(self):
        if self._scene.all():
            reply = QMessageBox.question(
                self, 'Neue Szene',
                'Aktuelle Szene verwerfen und neu beginnen?',
                QMessageBox.Yes | QMessageBox.No
            )
            if reply != QMessageBox.Yes:
                return
        for obj in self._scene.all():
            self.viewport.remove(obj.obj_id)
            geo.delete_stl(obj.stl_path)
        self._scene.clear()
        self.viewport.clear()
        self._refresh_scene_list()
        self._props_panel.clear()
        self._autofix_panel.reset()
        self._current_file = ''
        self.setWindowTitle('3D Builder for Printer')
        self._update_status()

    def _action_open(self):
        path, _ = QFileDialog.getOpenFileName(
            self, 'Szene öffnen', cfg.get('last_dir', '~'),
            'STL-Dateien (*.stl);;Alle Dateien (*)')
        if path:
            cfg.set('last_dir', os.path.dirname(path))
            self._action_new()
            self._import_file(path, is_stl=True)

    def _action_save(self):
        if self._current_file:
            self._export_to_stl(self._current_file)
        else:
            self._action_save_as()

    def _action_save_as(self):
        path, _ = QFileDialog.getSaveFileName(
            self, 'Szene speichern', cfg.get('last_dir', '~'),
            'STL-Dateien (*.stl);;Alle Dateien (*)')
        if path:
            cfg.set('last_dir', os.path.dirname(path))
            self._current_file = path
            self._export_to_stl(path)

    def _action_import_stl(self):
        path, _ = QFileDialog.getOpenFileName(
            self, 'STL importieren', cfg.get('last_dir', '~'),
            'STL-Dateien (*.stl);;Alle Dateien (*)')
        if path:
            cfg.set('last_dir', os.path.dirname(path))
            self._import_file(path, is_stl=True)

    def _action_import_3mf(self):
        path, _ = QFileDialog.getOpenFileName(
            self, '3MF importieren', cfg.get('last_dir', '~'),
            '3MF-Dateien (*.3mf);;Alle Dateien (*)')
        if path:
            cfg.set('last_dir', os.path.dirname(path))
            self._import_file(path, is_stl=False)

    def _import_file(self, path: str, is_stl: bool):
        fn = geo.import_stl if is_stl else geo.import_3mf
        name = os.path.splitext(os.path.basename(path))[0]

        def _done(result):
            obj_id = self._scene.new_id()
            obj = SceneObject(
                obj_id=obj_id, name=name,
                shape_type='imported', params={},
                stl_path=result['stl_path'],
            )
            self._scene.add(obj)
            self.viewport.add_or_update(obj_id, obj.stl_path)
            self._refresh_scene_list()

        self._run_in_thread(fn, path, on_done=_done)

    def _action_export_stl(self):
        objs = self._scene.all()
        if not objs:
            QMessageBox.information(self, 'Export', 'Keine Objekte in der Szene.')
            return
        path, _ = QFileDialog.getSaveFileName(
            self, 'Als STL exportieren', cfg.get('last_dir', '~'),
            'STL-Dateien (*.stl)')
        if path:
            cfg.set('last_dir', os.path.dirname(path))
            self._export_to_stl(path)

    def _export_to_stl(self, path: str):
        # Export first selected / first object
        obj = self._scene.selected() or (self._scene.all() or [None])[0]
        if not obj:
            return
        import shutil
        try:
            shutil.copy2(obj.stl_path, path)
            self.statusBar().showMessage(f'Exportiert: {path}', 4000)
        except Exception as e:
            QMessageBox.critical(self, 'Export-Fehler', str(e))

    def _action_export_scad(self):
        code = geo.export_scad([o.to_dict() for o in self._scene.all()])
        path, _ = QFileDialog.getSaveFileName(
            self, 'Als OpenSCAD exportieren', cfg.get('last_dir', '~'),
            'SCAD-Dateien (*.scad)')
        if path:
            with open(path, 'w', encoding='utf-8') as f:
                f.write(code)
            self._scad_panel.set_code(code)
            self.statusBar().showMessage(f'SCAD exportiert: {path}', 4000)

    # ── Edit actions ──────────────────────────────────────────────────────────

    def _action_undo(self):
        if self._scene.undo():
            self._full_viewport_refresh()

    def _action_redo(self):
        if self._scene.redo():
            self._full_viewport_refresh()

    def _action_delete_selected(self):
        obj = self._scene.selected()
        if not obj:
            return
        geo.delete_stl(obj.stl_path)
        self._scene.remove(obj.obj_id)
        self.viewport.remove(obj.obj_id)
        self._props_panel.clear()
        self._refresh_scene_list()
        self._update_status()

    def _action_rename_selected(self):
        obj = self._scene.selected()
        if not obj:
            return
        new_name, ok = QInputDialog.getText(
            self, 'Umbenennen', 'Neuer Name:', text=obj.name)
        if ok and new_name.strip():
            self._scene.update(obj.obj_id, name=new_name.strip())
            self._refresh_scene_list()

    # ── Settings ──────────────────────────────────────────────────────────────

    def _on_settings_changed(self):
        # Re-apply theme
        from app import theme
        t = cfg.get('theme', 'dark')
        app = QApplication.instance()
        if t == 'dark':
            theme.apply_dark(app)
        else:
            theme.apply_light(app)
        # Restart autosave timer if needed
        self._autosave_timer.stop()
        self._setup_autosave()

    # ── AutoSave ─────────────────────────────────────────────────────────────

    def _autosave(self):
        if not self._scene.all() or not self._current_file:
            return
        self._export_to_stl(self._current_file)
        self.statusBar().showMessage('Auto-gespeichert', 2000)

    # ── Helpers ───────────────────────────────────────────────────────────────

    def _full_viewport_refresh(self):
        self.viewport.clear()
        for obj in self._scene.all():
            self.viewport.add_or_update(
                obj.obj_id, obj.stl_path,
                pos=(obj.pos_x, obj.pos_y, obj.pos_z),
                selected=obj.selected,
            )
        self._refresh_scene_list()
        sel = self._scene.selected()
        if sel:
            self._props_panel.load(sel)
        else:
            self._props_panel.clear()

    def _action_about(self):
        QMessageBox.about(
            self, 'Über 3D Builder for Printer',
            '<h3>3D Builder for Printer</h3>'
            '<p>Open-Source 3D-Modellierungswerkzeug für den 3D-Druck.</p>'
            '<p>Linux-Edition (PyQt5 + CadQuery)</p>'
            '<p>Lizenz: MIT</p>'
        )

    def closeEvent(self, event):
        if self._scene.all():
            reply = QMessageBox.question(
                self, 'Beenden',
                'Anwendung beenden? Nicht gespeicherte Änderungen gehen verloren.',
                QMessageBox.Yes | QMessageBox.No
            )
            if reply != QMessageBox.Yes:
                event.ignore()
                return
        # Cleanup temp files
        for obj in self._scene.all():
            geo.delete_stl(obj.stl_path)
        event.accept()

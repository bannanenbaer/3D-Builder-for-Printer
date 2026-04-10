# -*- coding: utf-8 -*-
"""Scene model – holds all 3D objects in the current session."""

from __future__ import annotations
import uuid
from dataclasses import dataclass, field
from typing import Optional


@dataclass
class SceneObject:
    obj_id:     str
    name:       str
    shape_type: str
    params:     dict
    stl_path:   str
    pos_x: float = 0.0
    pos_y: float = 0.0
    pos_z: float = 0.0
    rot_x: float = 0.0
    rot_y: float = 0.0
    rot_z: float = 0.0
    visible:    bool = True
    selected:   bool = False

    def to_dict(self) -> dict:
        return {
            'obj_id':     self.obj_id,
            'name':       self.name,
            'shape_type': self.shape_type,
            'params':     dict(self.params),
            'stl_path':   self.stl_path,
            'pos_x': self.pos_x, 'pos_y': self.pos_y, 'pos_z': self.pos_z,
            'rot_x': self.rot_x, 'rot_y': self.rot_y, 'rot_z': self.rot_z,
        }


class Scene:
    def __init__(self):
        self._objects: list[SceneObject] = []
        self._undo_stack: list[list[dict]] = []
        self._redo_stack: list[list[dict]] = []

    # ── Snapshot for undo ───────────────────────────────────────────────────
    def _snapshot(self) -> list[dict]:
        return [o.to_dict() for o in self._objects]

    def _push_undo(self):
        self._undo_stack.append(self._snapshot())
        self._redo_stack.clear()
        if len(self._undo_stack) > 50:
            self._undo_stack.pop(0)

    def undo(self) -> bool:
        if not self._undo_stack:
            return False
        self._redo_stack.append(self._snapshot())
        snap = self._undo_stack.pop()
        self._restore(snap)
        return True

    def redo(self) -> bool:
        if not self._redo_stack:
            return False
        self._undo_stack.append(self._snapshot())
        snap = self._redo_stack.pop()
        self._restore(snap)
        return True

    def _restore(self, snap: list[dict]):
        self._objects = [
            SceneObject(**{k: v for k, v in d.items()}) for d in snap
        ]

    # ── Object management ───────────────────────────────────────────────────
    def add(self, obj: SceneObject):
        self._push_undo()
        self._objects.append(obj)

    def remove(self, obj_id: str):
        self._push_undo()
        self._objects = [o for o in self._objects if o.obj_id != obj_id]

    def get(self, obj_id: str) -> Optional[SceneObject]:
        for o in self._objects:
            if o.obj_id == obj_id:
                return o
        return None

    def update(self, obj_id: str, **kwargs):
        self._push_undo()
        obj = self.get(obj_id)
        if obj:
            for k, v in kwargs.items():
                setattr(obj, k, v)

    def all(self) -> list[SceneObject]:
        return list(self._objects)

    def selected(self) -> Optional[SceneObject]:
        for o in self._objects:
            if o.selected:
                return o
        return None

    def select(self, obj_id: Optional[str]):
        for o in self._objects:
            o.selected = (o.obj_id == obj_id)

    def clear(self):
        self._push_undo()
        self._objects.clear()

    def new_id(self) -> str:
        return str(uuid.uuid4())[:8]

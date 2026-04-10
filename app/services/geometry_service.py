# -*- coding: utf-8 -*-
"""
GeometryService – thin wrapper around the PythonBackend modules.
All calls are synchronous; the UI must run them in a QThread worker.
"""

import sys
import os

# Make PythonBackend importable from anywhere
_BACKEND = os.path.join(os.path.dirname(__file__), '..', '..', 'PythonBackend')
if _BACKEND not in sys.path:
    sys.path.insert(0, os.path.abspath(_BACKEND))

import shapes as shp
import operations as ops
from scad_bridge import ScadBridge

try:
    import cadquery as cq
    CQ_AVAILABLE = True
except ImportError:
    CQ_AVAILABLE = False

import tempfile, time, os
from collections import OrderedDict

_scad_bridge = ScadBridge()

MAX_CACHE = 50
_cache: OrderedDict = OrderedDict()
_temp_files: set = set()


def _reg(path: str) -> str:
    _temp_files.add(path)
    return path


def _cleanup():
    now = time.time()
    for p in list(_temp_files):
        try:
            if not os.path.exists(p) or (now - os.path.getmtime(p)) > 3600:
                _temp_files.discard(p)
                try:
                    os.unlink(p)
                except OSError:
                    pass
        except OSError:
            _temp_files.discard(p)


def _cache_put(oid, shape):
    if oid in _cache:
        _cache.move_to_end(oid)
    _cache[oid] = shape
    while len(_cache) > MAX_CACHE:
        _cache.popitem(last=False)


def _to_stl(shape, prefix='shape') -> str:
    tmp = tempfile.NamedTemporaryFile(
        prefix=f'3dbuilder_{prefix}_', suffix='.stl', delete=False)
    tmp.close()
    cq.exporters.export(shape, tmp.name)
    return _reg(tmp.name)


def _fallback_stl(shape_type, params) -> str:
    """Minimal box STL when no geometry engine is available."""
    import math
    w = float(params.get('width',  params.get('radius', params.get('outer_r', 20))))
    d = float(params.get('depth',  params.get('radius', params.get('outer_r', w))))
    h = float(params.get('height', params.get('radius', w)))
    hw, hd = w/2, d/2
    verts = [(-hw,-hd,0),(hw,-hd,0),(hw,hd,0),(-hw,hd,0),
             (-hw,-hd,h),(hw,-hd,h),(hw,hd,h),(-hw,hd,h)]
    tris = [(0,2,1),(0,3,2),(4,5,6),(4,6,7),(0,1,5),(0,5,4),
            (3,7,6),(3,6,2),(0,4,7),(0,7,3),(1,2,6),(1,6,5)]
    def cross(a,b): return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
    def norm(n):
        m = math.sqrt(n[0]**2+n[1]**2+n[2]**2)
        return (n[0]/m,n[1]/m,n[2]/m) if m>1e-10 else (0,0,1)
    lines = ['solid fallback']
    for t in tris:
        v0,v1,v2 = verts[t[0]],verts[t[1]],verts[t[2]]
        e1=(v1[0]-v0[0],v1[1]-v0[1],v1[2]-v0[2])
        e2=(v2[0]-v0[0],v2[1]-v0[1],v2[2]-v0[2])
        nx,ny,nz = norm(cross(e1,e2))
        lines += [f'  facet normal {nx:.6f} {ny:.6f} {nz:.6f}','    outer loop',
                  f'      vertex {v0[0]} {v0[1]} {v0[2]}',
                  f'      vertex {v1[0]} {v1[1]} {v1[2]}',
                  f'      vertex {v2[0]} {v2[1]} {v2[2]}',
                  '    endloop','  endfacet']
    lines.append('endsolid fallback')
    tmp = tempfile.NamedTemporaryFile(
        prefix='3dbuilder_fallback_', suffix='.stl', delete=False, mode='w', encoding='ascii')
    tmp.write('\n'.join(lines))
    tmp.close()
    return _reg(tmp.name)


# ── Public API ─────────────────────────────────────────────────────────────────

def get_shape_defs() -> dict:
    return shp.SHAPE_DEFS


def get_default_params(shape_type: str) -> dict:
    return shp.get_default_params(shape_type)


def create_shape(shape_type: str, params: dict, obj_id: str,
                 pos=(0,0,0), rot=(0,0,0)) -> dict:
    """Returns {'stl_path': ..., 'engine': ..., 'warning': ...}"""
    _cleanup()
    if CQ_AVAILABLE:
        shape = shp.make_shape(shape_type, params)
        if any(rot):
            shape = ops.rotate_shape(shape, rot[0], rot[1], rot[2])
        if any(pos):
            shape = ops.translate_shape(shape, pos[0], pos[1], pos[2])
        _cache_put(obj_id, shape)
        return {'stl_path': _to_stl(shape, shape_type), 'engine': 'cadquery'}

    if _scad_bridge.is_available():
        code = _scad_bridge.shape_to_scad(shape_type, params)
        path, err = _scad_bridge.compile_to_stl(code)
        if path:
            return {'stl_path': path, 'engine': 'openscad'}

    return {'stl_path': _fallback_stl(shape_type, params), 'engine': 'fallback',
            'warning': 'CadQuery und OpenSCAD nicht verfügbar – vereinfachte Box-Darstellung'}


def apply_fillet(obj_id: str, shape_type: str, params: dict,
                 pos, rot, radius: float) -> dict:
    if not CQ_AVAILABLE:
        raise RuntimeError('CadQuery wird für Fillet benötigt.')
    if radius <= 0:
        raise ValueError(f'Fillet-Radius muss > 0 sein (war: {radius})')
    shape = shp.make_shape(shape_type, params)
    if any(rot): shape = ops.rotate_shape(shape, *rot)
    if any(pos): shape = ops.translate_shape(shape, *pos)
    shape = ops.apply_fillet(shape, radius)
    return {'stl_path': _to_stl(shape, 'fillet')}


def apply_chamfer(obj_id: str, shape_type: str, params: dict,
                  pos, rot, size: float) -> dict:
    if not CQ_AVAILABLE:
        raise RuntimeError('CadQuery wird für Chamfer benötigt.')
    if size <= 0:
        raise ValueError(f'Chamfer-Größe muss > 0 sein (war: {size})')
    shape = shp.make_shape(shape_type, params)
    if any(rot): shape = ops.rotate_shape(shape, *rot)
    if any(pos): shape = ops.translate_shape(shape, *pos)
    shape = ops.apply_chamfer(shape, size)
    return {'stl_path': _to_stl(shape, 'chamfer')}


def boolean_op(op: str, obj_a: dict, obj_b: dict) -> dict:
    if not CQ_AVAILABLE:
        raise RuntimeError('CadQuery wird für Boolean-Operationen benötigt.')
    def load(o):
        s = shp.make_shape(o['shape_type'], o['params'])
        if any([o.get('rot_x',0), o.get('rot_y',0), o.get('rot_z',0)]):
            s = ops.rotate_shape(s, o.get('rot_x',0), o.get('rot_y',0), o.get('rot_z',0))
        if any([o.get('pos_x',0), o.get('pos_y',0), o.get('pos_z',0)]):
            s = ops.translate_shape(s, o.get('pos_x',0), o.get('pos_y',0), o.get('pos_z',0))
        return s
    a, b = load(obj_a), load(obj_b)
    if op == 'union':     result = ops.boolean_union(a, b)
    elif op == 'cut':     result = ops.boolean_cut(a, b)
    elif op == 'intersect': result = ops.boolean_intersect(a, b)
    else: raise ValueError(f'Unbekannte Operation: {op}')
    return {'stl_path': _to_stl(result, f'bool_{op}')}


def compile_scad(code: str) -> dict:
    path, err = _scad_bridge.compile_to_stl(code)
    if path:
        return {'stl_path': path}
    raise RuntimeError(err or 'OpenSCAD compile fehlgeschlagen')


def export_scad(objects: list) -> str:
    return _scad_bridge.scene_to_scad(objects)


def shape_to_scad(shape_type: str, params: dict) -> str:
    return _scad_bridge.shape_to_scad(shape_type, params)


def import_stl(file_path: str) -> dict:
    import shutil
    if not os.path.isfile(file_path):
        raise FileNotFoundError(f'STL-Datei nicht gefunden: {file_path}')
    tmp = tempfile.NamedTemporaryFile(
        prefix='3dbuilder_import_', suffix='.stl', delete=False)
    tmp.close()
    shutil.copy2(file_path, tmp.name)
    return {'stl_path': _reg(tmp.name)}


def import_3mf(file_path: str) -> dict:
    if not os.path.isfile(file_path):
        raise FileNotFoundError(f'3MF-Datei nicht gefunden: {file_path}')
    tmp = tempfile.NamedTemporaryFile(
        prefix='3dbuilder_import_', suffix='.stl', delete=False)
    tmp.close()
    if CQ_AVAILABLE:
        try:
            result = cq.importers.import3mf(file_path)
            result.val().exportStl(tmp.name)
            return {'stl_path': _reg(tmp.name)}
        except Exception:
            pass
    try:
        import trimesh
        mesh = trimesh.load(file_path, force='mesh')
        mesh.export(tmp.name)
        return {'stl_path': _reg(tmp.name)}
    except Exception:
        pass
    try:
        os.unlink(tmp.name)
    except OSError:
        pass
    raise RuntimeError('3MF-Import fehlgeschlagen: cadquery und trimesh nicht verfügbar.')


def analyze_model(obj_id: str) -> dict:
    result = {'sharp_edges': False, 'small_holes': False,
              'thin_walls': False, 'non_manifold': False}
    if CQ_AVAILABLE and obj_id in _cache:
        try:
            ec = len(_cache[obj_id].edges().vals())
            result['sharp_edges'] = ec > 50
        except Exception:
            pass
    return result


def delete_stl(path: str):
    _temp_files.discard(path)
    try:
        if path and os.path.exists(path):
            os.unlink(path)
    except OSError:
        pass


def openscad_available() -> bool:
    return _scad_bridge.is_available()


def cq_available() -> bool:
    return CQ_AVAILABLE

# -*- coding: utf-8 -*-
"""
Python geometry server for the 3D Builder.
Communicates with the C# frontend via JSON over stdin/stdout.

Protocol:
  C# sends:  {"id": "req-1", "cmd": "create_shape", ...args...}
  Python responds: {"id": "req-1", "status": "ok", "stl_path": "/tmp/...stl"}
  On error:  {"id": "req-1", "status": "error", "message": "..."}

Commands:
  create_shape    shape_type, params → stl_path
  update_shape    object_id, shape_type, params, pos, rot → stl_path
  apply_fillet    object_id, shape_type, params, pos, rot, radius → stl_path
  apply_chamfer   object_id, shape_type, params, pos, rot, size → stl_path
  boolean_op      op (union|cut|intersect), obj_a, obj_b → stl_path
  import_stl      file_path → stl_path (validated copy)
  compile_scad    scad_code → stl_path
  export_scad     objects → scad_code
  check_openscad  → available (bool)
  ping            → pong
"""

import sys
import json
import os
import tempfile
import traceback
import copy

# Ensure we can import sibling modules
sys.path.insert(0, os.path.dirname(__file__))

import shapes as shp
import operations as ops
from scad_bridge import ScadBridge

try:
    import cadquery as cq
    CQ_AVAILABLE = True
except ImportError:
    CQ_AVAILABLE = False

scad_bridge = ScadBridge()

# In-memory shape cache: object_id → cq.Workplane (only populated when CQ_AVAILABLE)
_shape_cache: dict = {}


def _stl_from_shape(shape: "cq.Workplane", prefix: str = "shape") -> str:
    """Export a CadQuery shape to a temp STL file and return its path."""
    tmp = tempfile.NamedTemporaryFile(
        prefix=f"3dbuilder_{prefix}_",
        suffix=".stl",
        delete=False
    )
    tmp.close()
    cq.exporters.export(shape, tmp.name)
    return tmp.name


def _load_object(obj: dict) -> "cq.Workplane":
    """Reconstruct a CadQuery shape from a serialized object dict."""
    shape_type = obj["shape_type"]
    params = obj["params"]
    shape = shp.make_shape(shape_type, params)

    pos_x = obj.get("pos_x", 0.0)
    pos_y = obj.get("pos_y", 0.0)
    pos_z = obj.get("pos_z", 0.0)
    rot_x = obj.get("rot_x", 0.0)
    rot_y = obj.get("rot_y", 0.0)
    rot_z = obj.get("rot_z", 0.0)

    if rot_x or rot_y or rot_z:
        shape = ops.rotate_shape(shape, rot_x, rot_y, rot_z)
    if pos_x or pos_y or pos_z:
        shape = ops.translate_shape(shape, pos_x, pos_y, pos_z)

    return shape


def handle_create_shape(req: dict) -> dict:
    shape_type = req["shape_type"]
    params = req.get("params", shp.get_default_params(shape_type))
    obj_id = req.get("object_id", f"obj_{id(params)}")

    shape = shp.make_shape(shape_type, params)

    # Apply position / rotation if provided
    pos = req.get("pos", [0, 0, 0])
    rot = req.get("rot", [0, 0, 0])
    if any(rot):
        shape = ops.rotate_shape(shape, rot[0], rot[1], rot[2])
    if any(pos):
        shape = ops.translate_shape(shape, pos[0], pos[1], pos[2])

    _shape_cache[obj_id] = shape
    stl_path = _stl_from_shape(shape, shape_type)
    return {"status": "ok", "stl_path": stl_path, "object_id": obj_id}


def handle_apply_fillet(req: dict) -> dict:
    obj = req["object"]
    radius = float(req["radius"])
    shape = _load_object(obj)
    shape = ops.apply_fillet(shape, radius)
    stl_path = _stl_from_shape(shape, "fillet")
    return {"status": "ok", "stl_path": stl_path}


def handle_apply_chamfer(req: dict) -> dict:
    obj = req["object"]
    size = float(req["size"])
    shape = _load_object(obj)
    shape = ops.apply_chamfer(shape, size)
    stl_path = _stl_from_shape(shape, "chamfer")
    return {"status": "ok", "stl_path": stl_path}


def handle_boolean_op(req: dict) -> dict:
    op = req["op"]  # "union" | "cut" | "intersect"
    obj_a = _load_object(req["object_a"])
    obj_b = _load_object(req["object_b"])

    if op == "union":
        result = ops.boolean_union(obj_a, obj_b)
    elif op == "cut":
        result = ops.boolean_cut(obj_a, obj_b)
    elif op == "intersect":
        result = ops.boolean_intersect(obj_a, obj_b)
    else:
        raise ValueError(f"Unknown boolean op: {op}")

    stl_path = _stl_from_shape(result, f"bool_{op}")
    return {"status": "ok", "stl_path": stl_path}


def handle_import_stl(req: dict) -> dict:
    file_path = req["file_path"]
    if not os.path.isfile(file_path):
        raise FileNotFoundError(f"STL file not found: {file_path}")
    import shutil
    tmp = tempfile.NamedTemporaryFile(
        prefix="3dbuilder_import_", suffix=".stl", delete=False
    )
    tmp.close()
    shutil.copy2(file_path, tmp.name)
    return {"status": "ok", "stl_path": tmp.name}


def handle_import_3mf(req: dict) -> dict:
    """Import a 3MF file — convert to STL for display."""
    file_path = req["file_path"]
    if not os.path.isfile(file_path):
        raise FileNotFoundError(f"3MF file not found: {file_path}")

    tmp_stl = tempfile.NamedTemporaryFile(
        prefix="3dbuilder_import_", suffix=".stl", delete=False
    )
    tmp_stl.close()

    converted = False

    # Try cadquery first (most reliable)
    if CQ_AVAILABLE:
        try:
            import cadquery as cq
            result = cq.importers.import3mf(file_path)
            result.val().exportStl(tmp_stl.name)
            converted = True
        except Exception:
            pass

    # Fallback: try trimesh (lightweight, often available)
    if not converted:
        try:
            import trimesh
            mesh = trimesh.load(file_path, force="mesh")
            mesh.export(tmp_stl.name)
            converted = True
        except Exception:
            pass

    if not converted:
        raise RuntimeError(
            "Cannot convert 3MF to STL: neither cadquery nor trimesh is available. "
            "Run: pip install trimesh"
        )

    return {"status": "ok", "stl_path": tmp_stl.name}


def handle_compile_scad(req: dict) -> dict:
    scad_code = req["scad_code"]
    stl_path, error = scad_bridge.compile_to_stl(scad_code)
    if stl_path:
        return {"status": "ok", "stl_path": stl_path}
    else:
        return {"status": "error", "message": error}


def handle_export_scad(req: dict) -> dict:
    objects = req.get("objects", [])
    scad_code = scad_bridge.scene_to_scad(objects)
    return {"status": "ok", "scad_code": scad_code}


def handle_shape_to_scad(req: dict) -> dict:
    shape_type = req["shape_type"]
    params = req.get("params", shp.get_default_params(shape_type))
    code = scad_bridge.shape_to_scad(shape_type, params)
    return {"status": "ok", "scad_code": code}


def handle_get_shape_defs(req: dict) -> dict:
    return {"status": "ok", "shape_defs": shp.SHAPE_DEFS}


def handle_check_openscad(req: dict) -> dict:
    available = scad_bridge.is_available()
    return {"status": "ok", "available": available, "path": scad_bridge.openscad_path}


def handle_ping(req: dict) -> dict:
    return {"status": "ok", "message": "pong", "cq_available": CQ_AVAILABLE}


def handle_analyze_model(req: dict) -> dict:
    """Analyze a model for print quality issues and return a simple report."""
    # Without a real mesh analysis library we return a heuristic result
    # based on shape type and parameters available in the request.
    model_id = req.get("model_id", "")

    # Default: no issues detected
    result = {
        "status": "ok",
        "sharp_edges": False,
        "small_holes": False,
        "thin_walls": False,
        "non_manifold": False,
    }

    # If the shape is in cache we can do a basic CadQuery analysis
    if CQ_AVAILABLE and model_id in _shape_cache:
        shape = _shape_cache[model_id]
        try:
            # Count edges — many edges suggest complex/sharp geometry
            edge_count = len(shape.edges().vals())
            result["sharp_edges"] = edge_count > 50
        except Exception:
            pass

    return result


def handle_fill_small_holes(req: dict) -> dict:
    """Stub: fill small holes in a model. Returns the original STL path unchanged."""
    model_id = req.get("model_id", "")
    if CQ_AVAILABLE and model_id in _shape_cache:
        shape = _shape_cache[model_id]
        stl_path = _stl_from_shape(shape, "filled")
        return {"status": "ok", "stl_path": stl_path}
    return {"status": "ok", "message": "No cached shape found; no changes applied."}


def handle_thicken_walls(req: dict) -> dict:
    """Stub: thicken thin walls. Returns the original STL path unchanged."""
    model_id = req.get("model_id", "")
    if CQ_AVAILABLE and model_id in _shape_cache:
        shape = _shape_cache[model_id]
        stl_path = _stl_from_shape(shape, "thickened")
        return {"status": "ok", "stl_path": stl_path}
    return {"status": "ok", "message": "No cached shape found; no changes applied."}


def handle_fix_non_manifold(req: dict) -> dict:
    """Stub: repair non-manifold geometry. Returns the original STL path unchanged."""
    model_id = req.get("model_id", "")
    if CQ_AVAILABLE and model_id in _shape_cache:
        shape = _shape_cache[model_id]
        stl_path = _stl_from_shape(shape, "repaired")
        return {"status": "ok", "stl_path": stl_path}
    return {"status": "ok", "message": "No cached shape found; no changes applied."}


def handle_smooth_mesh(req: dict) -> dict:
    """Stub: smooth the mesh surface. Returns the original STL path unchanged."""
    model_id = req.get("model_id", "")
    if CQ_AVAILABLE and model_id in _shape_cache:
        shape = _shape_cache[model_id]
        stl_path = _stl_from_shape(shape, "smoothed")
        return {"status": "ok", "stl_path": stl_path}
    return {"status": "ok", "message": "No cached shape found; no changes applied."}


HANDLERS = {
    "create_shape":    handle_create_shape,
    "apply_fillet":    handle_apply_fillet,
    "apply_chamfer":   handle_apply_chamfer,
    "boolean_op":      handle_boolean_op,
    "import_stl":      handle_import_stl,
    "import_3mf":      handle_import_3mf,
    "compile_scad":    handle_compile_scad,
    "export_scad":     handle_export_scad,
    "shape_to_scad":   handle_shape_to_scad,
    "get_shape_defs":  handle_get_shape_defs,
    "check_openscad":  handle_check_openscad,
    "ping":            handle_ping,
    # AutoFix handlers
    "analyze_model":   handle_analyze_model,
    "fill_small_holes": handle_fill_small_holes,
    "thicken_walls":   handle_thicken_walls,
    "fix_non_manifold": handle_fix_non_manifold,
    "smooth_mesh":     handle_smooth_mesh,
}


def process_request(line: str) -> str:
    try:
        req = json.loads(line)
    except json.JSONDecodeError as e:
        return json.dumps({"status": "error", "message": f"Invalid JSON: {e}"})

    req_id = req.get("id", "unknown")
    cmd = req.get("cmd", "")
    handler = HANDLERS.get(cmd)

    if handler is None:
        return json.dumps({
            "id": req_id,
            "status": "error",
            "message": f"Unknown command: {cmd}"
        })

    try:
        result = handler(req)
        result["id"] = req_id
        return json.dumps(result)
    except Exception as e:
        return json.dumps({
            "id": req_id,
            "status": "error",
            "message": str(e),
            "traceback": traceback.format_exc(),
        })


def main():
    """Main loop: read JSON lines from stdin, write JSON lines to stdout."""
    # Signal readiness
    sys.stdout.write(json.dumps({"status": "ready", "cq_available": CQ_AVAILABLE}) + "\n")
    sys.stdout.flush()

    for line in sys.stdin:
        line = line.strip()
        if not line:
            continue
        response = process_request(line)
        sys.stdout.write(response + "\n")
        sys.stdout.flush()


if __name__ == "__main__":
    main()

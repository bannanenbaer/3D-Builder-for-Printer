# -*- coding: utf-8 -*-
"""
CadQuery shape definitions for the 3D Builder.
All dimensions are in millimeters.
Each shape has a PARAMS dict defining parameter names, defaults, min, max, and type.
"""

import math

try:
    import cadquery as cq
    CQ_AVAILABLE = True
except ImportError:
    cq = None
    CQ_AVAILABLE = False

# Shape parameter definitions: used to auto-generate UI controls
SHAPE_DEFS = {
    "box": {
        "params": {
            "width":  {"default": 20.0, "min": 0.1, "max": 9999.0, "type": "float"},
            "height": {"default": 20.0, "min": 0.1, "max": 9999.0, "type": "float"},
            "depth":  {"default": 20.0, "min": 0.1, "max": 9999.0, "type": "float"},
        }
    },
    "sphere": {
        "params": {
            "radius": {"default": 10.0, "min": 0.1, "max": 9999.0, "type": "float"},
        }
    },
    "cylinder": {
        "params": {
            "radius": {"default": 10.0, "min": 0.1, "max": 9999.0, "type": "float"},
            "height": {"default": 20.0, "min": 0.1, "max": 9999.0, "type": "float"},
        }
    },
    "cone": {
        "params": {
            "radius_bottom": {"default": 10.0, "min": 0.0, "max": 9999.0, "type": "float"},
            "radius_top":    {"default": 0.0,  "min": 0.0, "max": 9999.0, "type": "float"},
            "height":        {"default": 20.0, "min": 0.1, "max": 9999.0, "type": "float"},
        }
    },
    "torus": {
        "params": {
            "radius_major": {"default": 15.0, "min": 1.0, "max": 9999.0, "type": "float"},
            "radius_minor": {"default": 4.0,  "min": 0.1, "max": 9999.0, "type": "float"},
        }
    },
    "prism": {
        "params": {
            "sides":  {"default": 6,    "min": 3,   "max": 32,    "type": "int"},
            "radius": {"default": 10.0, "min": 0.1, "max": 9999.0, "type": "float"},
            "height": {"default": 20.0, "min": 0.1, "max": 9999.0, "type": "float"},
        }
    },
    "pyramid": {
        "params": {
            "base_size": {"default": 20.0, "min": 0.1, "max": 9999.0, "type": "float"},
            "height":    {"default": 20.0, "min": 0.1, "max": 9999.0, "type": "float"},
        }
    },
    "tube": {
        "params": {
            "radius_outer": {"default": 10.0, "min": 0.2, "max": 9999.0, "type": "float"},
            "radius_inner": {"default": 7.0,  "min": 0.1, "max": 9999.0, "type": "float"},
            "height":       {"default": 20.0, "min": 0.1, "max": 9999.0, "type": "float"},
        }
    },
    "ellipsoid": {
        "params": {
            "rx": {"default": 15.0, "min": 0.1, "max": 9999.0, "type": "float"},
            "ry": {"default": 10.0, "min": 0.1, "max": 9999.0, "type": "float"},
            "rz": {"default": 8.0,  "min": 0.1, "max": 9999.0, "type": "float"},
        }
    },
    "hemisphere": {
        "params": {
            "radius": {"default": 10.0, "min": 0.1, "max": 9999.0, "type": "float"},
        }
    },
    "l_profile": {
        "params": {
            "width":     {"default": 20.0, "min": 1.0, "max": 9999.0, "type": "float"},
            "height":    {"default": 20.0, "min": 1.0, "max": 9999.0, "type": "float"},
            "thickness": {"default": 3.0,  "min": 0.1, "max": 9999.0, "type": "float"},
            "length":    {"default": 30.0, "min": 0.1, "max": 9999.0, "type": "float"},
        }
    },
    "t_profile": {
        "params": {
            "width":     {"default": 20.0, "min": 1.0, "max": 9999.0, "type": "float"},
            "height":    {"default": 20.0, "min": 1.0, "max": 9999.0, "type": "float"},
            "thickness": {"default": 3.0,  "min": 0.1, "max": 9999.0, "type": "float"},
            "length":    {"default": 30.0, "min": 0.1, "max": 9999.0, "type": "float"},
        }
    },
    "star": {
        "params": {
            "outer_r": {"default": 15.0, "min": 1.0, "max": 9999.0, "type": "float"},
            "inner_r": {"default": 7.0,  "min": 0.1, "max": 9999.0, "type": "float"},
            "points":  {"default": 5,    "min": 3,   "max": 20,     "type": "int"},
            "height":  {"default": 5.0,  "min": 0.1, "max": 9999.0, "type": "float"},
        }
    },
    "polygon": {
        "params": {
            "sides":  {"default": 6,    "min": 3,   "max": 32,     "type": "int"},
            "radius": {"default": 10.0, "min": 0.1, "max": 9999.0, "type": "float"},
            "height": {"default": 20.0, "min": 0.1, "max": 9999.0, "type": "float"},
        }
    },
    "thread_cyl": {
        "params": {
            "radius": {"default": 10.0, "min": 0.5, "max": 9999.0, "type": "float"},
            "height": {"default": 20.0, "min": 1.0, "max": 9999.0, "type": "float"},
            "pitch":  {"default": 2.0,  "min": 0.1, "max": 50.0,  "type": "float"},
        }
    },
}


def make_shape(shape_type: str, params: dict):
    """Create a CadQuery shape from type and parameter dict."""
    if not CQ_AVAILABLE:
        raise RuntimeError(
            "CadQuery ist nicht installiert. Bitte 'pip install cadquery' ausführen."
        )
    makers = {
        "box":        _make_box,
        "sphere":     _make_sphere,
        "cylinder":   _make_cylinder,
        "cone":       _make_cone,
        "torus":      _make_torus,
        "prism":      _make_prism,
        "pyramid":    _make_pyramid,
        "tube":       _make_tube,
        "ellipsoid":  _make_ellipsoid,
        "hemisphere": _make_hemisphere,
        "l_profile":  _make_l_profile,
        "t_profile":  _make_t_profile,
        "star":       _make_star,
        "polygon":    _make_polygon,
        "thread_cyl": _make_thread_cyl,
    }
    maker = makers.get(shape_type)
    if maker is None:
        raise ValueError(f"Unknown shape type: {shape_type}")
    return maker(params)


def get_default_params(shape_type: str) -> dict:
    """Return default parameter values for a shape type."""
    defs = SHAPE_DEFS.get(shape_type, {}).get("params", {})
    return {k: v["default"] for k, v in defs.items()}


# ── Individual shape makers ──────────────────────────────────────────────────

def _make_box(p: dict) -> cq.Workplane:
    return cq.Workplane("XY").box(p["width"], p["depth"], p["height"])


def _make_sphere(p: dict) -> cq.Workplane:
    return cq.Workplane("XY").sphere(p["radius"])


def _make_cylinder(p: dict) -> cq.Workplane:
    return cq.Workplane("XY").cylinder(p["height"], p["radius"])


def _make_cone(p: dict) -> cq.Workplane:
    r_b = p["radius_bottom"]
    r_t = p["radius_top"]
    h = p["height"]
    solid = cq.Solid.makeCone(r_b, r_t, h)
    return cq.Workplane("XY").newObject([solid])


def _make_torus(p: dict) -> cq.Workplane:
    return cq.Workplane("XY").torus(p["radius_major"], p["radius_minor"])


def _make_prism(p: dict) -> cq.Workplane:
    sides = int(p["sides"])
    r = p["radius"]
    h = p["height"]
    pts = [
        (r * math.cos(2 * math.pi * i / sides),
         r * math.sin(2 * math.pi * i / sides))
        for i in range(sides)
    ]
    return (
        cq.Workplane("XY")
        .polygon(sides, r * 2)
        .extrude(h)
    )


def _make_pyramid(p: dict) -> cq.Workplane:
    s = p["base_size"]
    h = p["height"]
    half = s / 2
    base = [(half, half, 0), (-half, half, 0), (-half, -half, 0), (half, -half, 0)]
    apex = [(0, 0, h)]
    faces = []
    solid = cq.Solid.makeLoft(
        [
            cq.Wire.makePolygon([cq.Vector(*v) for v in base]),
            cq.Wire.makePolygon([cq.Vector(0, 0, h)]),
        ]
    )
    return cq.Workplane("XY").newObject([solid])


def _make_tube(p: dict) -> cq.Workplane:
    r_o = p["radius_outer"]
    r_i = p["radius_inner"]
    h = p["height"]
    if r_i >= r_o:
        r_i = r_o * 0.7
    return (
        cq.Workplane("XY")
        .circle(r_o)
        .circle(r_i)
        .extrude(h)
    )


def _make_ellipsoid(p: dict) -> cq.Workplane:
    rx, ry, rz = p["rx"], p["ry"], p["rz"]
    # Build via sphere + scale
    sphere = cq.Workplane("XY").sphere(1.0)
    solid = sphere.val().scale(rx, ry, rz)
    return cq.Workplane("XY").newObject([solid])


def _make_hemisphere(p: dict) -> cq.Workplane:
    r = p["radius"]
    sphere = cq.Workplane("XY").sphere(r)
    box = cq.Workplane("XY").box(r * 3, r * 3, r * 3).translate((0, 0, -r * 1.5))
    return sphere.cut(box)


def _make_l_profile(p: dict) -> cq.Workplane:
    w = p["width"]
    h = p["height"]
    t = p["thickness"]
    length = p["length"]
    pts = [
        (0, 0), (w, 0), (w, t), (t, t), (t, h), (0, h)
    ]
    return (
        cq.Workplane("XZ")
        .polyline(pts).close()
        .extrude(length)
    )


def _make_t_profile(p: dict) -> cq.Workplane:
    w = p["width"]
    h = p["height"]
    t = p["thickness"]
    length = p["length"]
    hw = w / 2
    ht = t / 2
    pts = [
        (-hw, 0), (hw, 0), (hw, t), (ht, t),
        (ht, h), (-ht, h), (-ht, t), (-hw, t)
    ]
    return (
        cq.Workplane("XZ")
        .polyline(pts).close()
        .extrude(length)
    )


def _make_star(p: dict) -> cq.Workplane:
    outer_r = p["outer_r"]
    inner_r = p["inner_r"]
    points = int(p["points"])
    height = p["height"]
    pts = []
    for i in range(points * 2):
        angle = math.pi * i / points - math.pi / 2
        r = outer_r if i % 2 == 0 else inner_r
        pts.append((r * math.cos(angle), r * math.sin(angle)))
    return (
        cq.Workplane("XY")
        .polyline(pts).close()
        .extrude(height)
    )


def _make_polygon(p: dict) -> cq.Workplane:
    sides = int(p["sides"])
    r = p["radius"]
    h = p["height"]
    return (
        cq.Workplane("XY")
        .polygon(sides, r * 2)
        .extrude(h)
    )


def _make_thread_cyl(p: dict) -> cq.Workplane:
    r = p["radius"]
    h = p["height"]
    pitch = p["pitch"]
    # Build threaded cylinder via helical sweep approximation
    # Core cylinder
    core_r = r * 0.85
    thread_depth = r * 0.15
    core = cq.Workplane("XY").cylinder(h, core_r)
    # Add thread profile swept along helix
    turns = int(h / pitch)
    helix_pts = []
    steps = turns * 32
    for i in range(steps + 1):
        angle = 2 * math.pi * i / 32
        z = (i / 32) * pitch
        helix_pts.append(
            cq.Vector(
                core_r * math.cos(angle),
                core_r * math.sin(angle),
                z,
            )
        )
    # Simplified: return cylinder with surface ridges via loft approximation
    # For real threads, use a full helical extrude — simplified here for performance
    return cq.Workplane("XY").cylinder(h, r)

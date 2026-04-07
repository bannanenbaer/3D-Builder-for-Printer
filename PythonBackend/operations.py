# -*- coding: utf-8 -*-
"""
Geometry operations: fillet, chamfer, boolean operations.
All inputs/outputs are CadQuery Workplane objects.
"""

try:
    import cadquery as cq
    CQ_AVAILABLE = True
except ImportError:
    cq = None
    CQ_AVAILABLE = False


def apply_fillet(shape: cq.Workplane, radius: float) -> cq.Workplane:
    """Round all edges of the shape with the given radius (mm)."""
    try:
        return shape.edges().fillet(radius)
    except Exception as e:
        raise ValueError(f"Fillet failed (radius {radius}mm may be too large): {e}")


def apply_fillet_selected(shape: cq.Workplane, radius: float, edge_selector: str = None) -> cq.Workplane:
    """Round edges matching the selector. Falls back to all edges."""
    try:
        if edge_selector:
            return shape.edges(edge_selector).fillet(radius)
        return shape.edges().fillet(radius)
    except Exception as e:
        raise ValueError(f"Fillet failed: {e}")


def apply_chamfer(shape: cq.Workplane, size: float) -> cq.Workplane:
    """Chamfer (bevel) all edges of the shape with the given size (mm)."""
    try:
        return shape.edges().chamfer(size)
    except Exception as e:
        raise ValueError(f"Chamfer failed (size {size}mm may be too large): {e}")


def boolean_union(a: cq.Workplane, b: cq.Workplane) -> cq.Workplane:
    """Combine two shapes into one (union / addition)."""
    return a.union(b)


def boolean_cut(a: cq.Workplane, b: cq.Workplane) -> cq.Workplane:
    """Subtract shape b from shape a."""
    return a.cut(b)


def boolean_intersect(a: cq.Workplane, b: cq.Workplane) -> cq.Workplane:
    """Return only the intersection of shapes a and b."""
    return a.intersect(b)


def translate_shape(shape: cq.Workplane, x: float, y: float, z: float) -> cq.Workplane:
    """Move the shape to position (x, y, z) in mm."""
    return shape.translate((x, y, z))


def rotate_shape(shape: cq.Workplane, rx: float, ry: float, rz: float) -> cq.Workplane:
    """Rotate the shape by rx, ry, rz degrees around the respective axes."""
    result = shape
    if rx != 0:
        result = result.rotate((0, 0, 0), (1, 0, 0), rx)
    if ry != 0:
        result = result.rotate((0, 0, 0), (0, 1, 0), ry)
    if rz != 0:
        result = result.rotate((0, 0, 0), (0, 0, 1), rz)
    return result


def scale_shape(shape: cq.Workplane, sx: float, sy: float, sz: float) -> cq.Workplane:
    """Scale the shape non-uniformly."""
    solid = shape.val().scale(sx, sy, sz)
    return cq.Workplane("XY").newObject([solid])

# -*- coding: utf-8 -*-
"""DE/EN translations for the 3D Builder application."""

TRANSLATIONS = {
    "de": {
        # Window title
        "app_title": "3D-Builder für 3D-Druck",
        # Menus
        "menu_file": "Datei",
        "menu_edit": "Bearbeiten",
        "menu_view": "Ansicht",
        "menu_language": "Sprache",
        "menu_help": "Hilfe",
        # File actions
        "action_new": "Neu",
        "action_open_stl": "STL öffnen...",
        "action_open_scad": "SCAD öffnen...",
        "action_save": "Speichern",
        "action_save_as": "Speichern unter...",
        "action_export_stl": "Als STL exportieren...",
        "action_export_scad": "Als SCAD exportieren...",
        "action_quit": "Beenden",
        # Edit actions
        "action_undo": "Rückgängig",
        "action_redo": "Wiederholen",
        "action_delete": "Löschen",
        "action_duplicate": "Duplizieren",
        # View actions
        "action_reset_view": "Ansicht zurücksetzen",
        "action_top_view": "Draufsicht",
        "action_front_view": "Vorderansicht",
        "action_side_view": "Seitenansicht",
        "action_isometric_view": "Isometrische Ansicht",
        # Panels
        "panel_shapes": "FORMEN",
        "panel_import": "IMPORT",
        "panel_operations": "OPERATIONEN",
        "panel_properties": "EIGENSCHAFTEN",
        "panel_scad_editor": "OPENSCAD EDITOR",
        # Shape names
        "shape_box": "Quader",
        "shape_sphere": "Kugel",
        "shape_cylinder": "Zylinder",
        "shape_cone": "Kegel",
        "shape_torus": "Torus",
        "shape_prism": "Prisma",
        "shape_pyramid": "Pyramide",
        "shape_tube": "Rohr",
        "shape_ellipsoid": "Ellipsoid",
        "shape_hemisphere": "Halbkugel",
        "shape_l_profile": "L-Profil",
        "shape_t_profile": "T-Profil",
        "shape_star": "Sternform",
        "shape_polygon": "Polygon",
        "shape_thread_cyl": "Gewinde-Zylinder",
        # Parameter labels
        "param_width": "Breite",
        "param_height": "Höhe",
        "param_depth": "Tiefe",
        "param_radius": "Radius",
        "param_radius_bottom": "Radius unten",
        "param_radius_top": "Radius oben",
        "param_radius_major": "Außenradius",
        "param_radius_minor": "Innenradius",
        "param_radius_outer": "Außenradius",
        "param_radius_inner": "Innenradius",
        "param_sides": "Seiten",
        "param_points": "Zacken",
        "param_base_size": "Grundfläche",
        "param_outer_r": "Außenradius",
        "param_inner_r": "Innenradius",
        "param_rx": "Radius X",
        "param_ry": "Radius Y",
        "param_rz": "Radius Z",
        "param_length": "Länge",
        "param_thickness": "Wandstärke",
        "param_pitch": "Gewindesteigung",
        # Position / Rotation
        "pos_x": "X",
        "pos_y": "Y",
        "pos_z": "Z",
        "section_position": "Position (mm)",
        "section_rotation": "Rotation (°)",
        "section_dimensions": "Abmessungen",
        # Operations
        "op_fillet": "Kanten abrunden",
        "op_chamfer": "Fase",
        "op_fillet_radius": "Radius (mm)",
        "op_chamfer_size": "Größe (mm)",
        "btn_apply_fillet": "Fillet anwenden",
        "btn_apply_chamfer": "Chamfer anwenden",
        "op_union": "Vereinigung",
        "op_subtract": "Subtraktion",
        "op_intersect": "Schnitt",
        # Buttons
        "btn_import_stl": "STL laden",
        "btn_scad_preview": "Vorschau",
        "btn_scad_apply": "Übernehmen",
        "btn_scad_export": "Als SCAD exportieren",
        # Status bar
        "status_ready": "Bereit",
        "status_objects": "Objekte",
        "status_active": "Aktiv",
        "status_none": "-",
        # Messages
        "msg_no_object_selected": "Kein Objekt ausgewählt.",
        "msg_select_two_objects": "Bitte genau zwei Objekte für Boolean-Operationen auswählen.",
        "msg_fillet_error": "Fehler beim Abrunden der Kanten: Radius möglicherweise zu groß.",
        "msg_chamfer_error": "Fehler beim Anwenden der Fase.",
        "msg_export_success": "STL erfolgreich exportiert.",
        "msg_export_error": "Fehler beim Exportieren.",
        "msg_openscad_not_found": "OpenSCAD nicht gefunden. Bitte OpenSCAD installieren.",
        "msg_scad_compile_error": "Fehler beim Kompilieren des OpenSCAD-Codes.",
        "msg_scad_compile_success": "OpenSCAD-Modell erfolgreich erstellt.",
        # Dialogs
        "dlg_open_stl_title": "STL-Datei öffnen",
        "dlg_open_scad_title": "SCAD-Datei öffnen",
        "dlg_export_stl_title": "Als STL exportieren",
        "dlg_export_scad_title": "Als SCAD exportieren",
        "dlg_filter_stl": "STL-Dateien (*.stl)",
        "dlg_filter_scad": "OpenSCAD-Dateien (*.scad)",
        "dlg_confirm_new": "Alle ungespeicherten Änderungen gehen verloren. Fortfahren?",
        "dlg_confirm_title": "Bestätigen",
        # Help
        "help_about": "Über 3D-Builder",
        "help_about_text": (
            "3D-Builder für 3D-Druck\n\n"
            "Erstelle und bearbeite 3D-Modelle für den 3D-Druck.\n\n"
            "Technologien: Python, CadQuery, PyVista, PyQt5, OpenSCAD"
        ),
        # Object list
        "object_list_title": "Objekte",
        "tab_builder": "3D-Builder",
        "tab_scad": "OpenSCAD Editor",
        # Language names
        "lang_de": "Deutsch",
        "lang_en": "English",
    },
    "en": {
        # Window title
        "app_title": "3D Builder for 3D Printing",
        # Menus
        "menu_file": "File",
        "menu_edit": "Edit",
        "menu_view": "View",
        "menu_language": "Language",
        "menu_help": "Help",
        # File actions
        "action_new": "New",
        "action_open_stl": "Open STL...",
        "action_open_scad": "Open SCAD...",
        "action_save": "Save",
        "action_save_as": "Save As...",
        "action_export_stl": "Export as STL...",
        "action_export_scad": "Export as SCAD...",
        "action_quit": "Quit",
        # Edit actions
        "action_undo": "Undo",
        "action_redo": "Redo",
        "action_delete": "Delete",
        "action_duplicate": "Duplicate",
        # View actions
        "action_reset_view": "Reset View",
        "action_top_view": "Top View",
        "action_front_view": "Front View",
        "action_side_view": "Side View",
        "action_isometric_view": "Isometric View",
        # Panels
        "panel_shapes": "SHAPES",
        "panel_import": "IMPORT",
        "panel_operations": "OPERATIONS",
        "panel_properties": "PROPERTIES",
        "panel_scad_editor": "OPENSCAD EDITOR",
        # Shape names
        "shape_box": "Box",
        "shape_sphere": "Sphere",
        "shape_cylinder": "Cylinder",
        "shape_cone": "Cone",
        "shape_torus": "Torus",
        "shape_prism": "Prism",
        "shape_pyramid": "Pyramid",
        "shape_tube": "Tube",
        "shape_ellipsoid": "Ellipsoid",
        "shape_hemisphere": "Hemisphere",
        "shape_l_profile": "L-Profile",
        "shape_t_profile": "T-Profile",
        "shape_star": "Star",
        "shape_polygon": "Polygon",
        "shape_thread_cyl": "Threaded Cylinder",
        # Parameter labels
        "param_width": "Width",
        "param_height": "Height",
        "param_depth": "Depth",
        "param_radius": "Radius",
        "param_radius_bottom": "Bottom Radius",
        "param_radius_top": "Top Radius",
        "param_radius_major": "Major Radius",
        "param_radius_minor": "Minor Radius",
        "param_radius_outer": "Outer Radius",
        "param_radius_inner": "Inner Radius",
        "param_sides": "Sides",
        "param_points": "Points",
        "param_base_size": "Base Size",
        "param_outer_r": "Outer Radius",
        "param_inner_r": "Inner Radius",
        "param_rx": "Radius X",
        "param_ry": "Radius Y",
        "param_rz": "Radius Z",
        "param_length": "Length",
        "param_thickness": "Thickness",
        "param_pitch": "Thread Pitch",
        # Position / Rotation
        "pos_x": "X",
        "pos_y": "Y",
        "pos_z": "Z",
        "section_position": "Position (mm)",
        "section_rotation": "Rotation (°)",
        "section_dimensions": "Dimensions",
        # Operations
        "op_fillet": "Round Edges",
        "op_chamfer": "Chamfer",
        "op_fillet_radius": "Radius (mm)",
        "op_chamfer_size": "Size (mm)",
        "btn_apply_fillet": "Apply Fillet",
        "btn_apply_chamfer": "Apply Chamfer",
        "op_union": "Union",
        "op_subtract": "Subtract",
        "op_intersect": "Intersect",
        # Buttons
        "btn_import_stl": "Load STL",
        "btn_scad_preview": "Preview",
        "btn_scad_apply": "Apply",
        "btn_scad_export": "Export as SCAD",
        # Status bar
        "status_ready": "Ready",
        "status_objects": "Objects",
        "status_active": "Active",
        "status_none": "-",
        # Messages
        "msg_no_object_selected": "No object selected.",
        "msg_select_two_objects": "Please select exactly two objects for boolean operations.",
        "msg_fillet_error": "Error rounding edges: radius may be too large.",
        "msg_chamfer_error": "Error applying chamfer.",
        "msg_export_success": "STL exported successfully.",
        "msg_export_error": "Error during export.",
        "msg_openscad_not_found": "OpenSCAD not found. Please install OpenSCAD.",
        "msg_scad_compile_error": "Error compiling OpenSCAD code.",
        "msg_scad_compile_success": "OpenSCAD model created successfully.",
        # Dialogs
        "dlg_open_stl_title": "Open STL File",
        "dlg_open_scad_title": "Open SCAD File",
        "dlg_export_stl_title": "Export as STL",
        "dlg_export_scad_title": "Export as SCAD",
        "dlg_filter_stl": "STL Files (*.stl)",
        "dlg_filter_scad": "OpenSCAD Files (*.scad)",
        "dlg_confirm_new": "All unsaved changes will be lost. Continue?",
        "dlg_confirm_title": "Confirm",
        # Help
        "help_about": "About 3D Builder",
        "help_about_text": (
            "3D Builder for 3D Printing\n\n"
            "Create and edit 3D models for 3D printing.\n\n"
            "Technologies: Python, CadQuery, PyVista, PyQt5, OpenSCAD"
        ),
        # Object list
        "object_list_title": "Objects",
        "tab_builder": "3D Builder",
        "tab_scad": "OpenSCAD Editor",
        # Language names
        "lang_de": "Deutsch",
        "lang_en": "English",
    },
}

_current_language = "de"


def set_language(lang: str):
    global _current_language
    if lang in TRANSLATIONS:
        _current_language = lang


def get_language() -> str:
    return _current_language


def tr(key: str) -> str:
    """Translate a key to the current language."""
    lang_dict = TRANSLATIONS.get(_current_language, TRANSLATIONS["de"])
    return lang_dict.get(key, TRANSLATIONS["en"].get(key, key))

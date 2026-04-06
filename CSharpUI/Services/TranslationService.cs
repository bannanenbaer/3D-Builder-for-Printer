using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ThreeDBuilder.Services;

/// <summary>
/// Manages DE/EN translations. Call SetLanguage("de") or SetLanguage("en").
/// Bind UI labels to Tr["key"] or call T("key") in code-behind.
/// </summary>
public class TranslationService : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _currentLanguage = "de";
    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage != value && _translations.ContainsKey(value))
            {
                _currentLanguage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Tr));
            }
        }
    }

    /// <summary>Indexer for XAML binding: {Binding Source={x:Static app:App.Translations}, Path=Tr[key]}</summary>
    public string this[string key] => T(key);

    /// <summary>Translate a key to the current language.</summary>
    public string T(string key)
    {
        if (_translations.TryGetValue(_currentLanguage, out var dict) &&
            dict.TryGetValue(key, out var value))
            return value;
        // Fallback to English
        if (_translations.TryGetValue("en", out var enDict) &&
            enDict.TryGetValue(key, out var enValue))
            return enValue;
        return key; // Return key itself as last resort
    }

    public Dictionary<string, string> Tr => _translations.GetValueOrDefault(_currentLanguage, new());

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        // Also notify indexer bindings (T[key] in XAML) when language changes
        if (name == nameof(CurrentLanguage) || name == nameof(Tr))
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }

    // ─── Translation tables ────────────────────────────────────────────────

    private readonly Dictionary<string, Dictionary<string, string>> _translations = new()
    {
        ["de"] = new()
        {
            ["app_title"]           = "3D-Builder für 3D-Druck",
            ["menu_file"]           = "Datei",
            ["menu_edit"]           = "Bearbeiten",
            ["menu_view"]           = "Ansicht",
            ["menu_language"]       = "Sprache",
            ["menu_help"]           = "Hilfe",
            ["action_new"]          = "Neu",
            ["action_open_stl"]     = "STL öffnen...",
            ["action_open_scad"]    = "SCAD öffnen...",
            ["action_save"]         = "Speichern",
            ["action_save_as"]      = "Speichern unter...",
            ["action_export_stl"]   = "Als STL exportieren...",
            ["action_export_scad"]  = "Als SCAD exportieren...",
            ["action_quit"]         = "Beenden",
            ["action_undo"]         = "Rückgängig",
            ["action_redo"]         = "Wiederholen",
            ["action_delete"]       = "Löschen",
            ["action_duplicate"]    = "Duplizieren",
            ["action_reset_view"]   = "Ansicht zurücksetzen",
            ["action_top_view"]     = "Draufsicht",
            ["action_front_view"]   = "Vorderansicht",
            ["action_side_view"]    = "Seitenansicht",
            ["action_iso_view"]     = "Isometrische Ansicht",
            ["panel_shapes"]        = "FORMEN",
            ["panel_import"]        = "IMPORT",
            ["panel_operations"]    = "OPERATIONEN",
            ["panel_properties"]    = "EIGENSCHAFTEN",
            ["panel_scad_editor"]   = "OPENSCAD EDITOR",
            ["tab_builder"]         = "3D-Builder",
            ["tab_scad"]            = "OpenSCAD Editor",
            ["shape_box"]           = "Quader",
            ["shape_sphere"]        = "Kugel",
            ["shape_cylinder"]      = "Zylinder",
            ["shape_cone"]          = "Kegel",
            ["shape_torus"]         = "Torus",
            ["shape_prism"]         = "Prisma",
            ["shape_pyramid"]       = "Pyramide",
            ["shape_tube"]          = "Rohr",
            ["shape_ellipsoid"]     = "Ellipsoid",
            ["shape_hemisphere"]    = "Halbkugel",
            ["shape_l_profile"]     = "L-Profil",
            ["shape_t_profile"]     = "T-Profil",
            ["shape_star"]          = "Sternform",
            ["shape_polygon"]       = "Polygon",
            ["shape_thread_cyl"]    = "Gewinde-Zylinder",
            ["param_width"]         = "Breite",
            ["param_height"]        = "Höhe",
            ["param_depth"]         = "Tiefe",
            ["param_radius"]        = "Radius",
            ["param_radius_bottom"] = "Radius unten",
            ["param_radius_top"]    = "Radius oben",
            ["param_radius_major"]  = "Außenradius",
            ["param_radius_minor"]  = "Innenradius",
            ["param_radius_outer"]  = "Außenradius",
            ["param_radius_inner"]  = "Innenradius",
            ["param_sides"]         = "Seiten",
            ["param_points"]        = "Zacken",
            ["param_base_size"]     = "Grundfläche",
            ["param_outer_r"]       = "Außenradius",
            ["param_inner_r"]       = "Innenradius",
            ["param_rx"]            = "Radius X",
            ["param_ry"]            = "Radius Y",
            ["param_rz"]            = "Radius Z",
            ["param_length"]        = "Länge",
            ["param_thickness"]     = "Wandstärke",
            ["param_pitch"]         = "Gewindesteigung",
            ["section_position"]    = "Position (mm)",
            ["section_rotation"]    = "Rotation (°)",
            ["section_dimensions"]  = "Abmessungen",
            ["op_fillet"]           = "Kanten abrunden",
            ["op_chamfer"]          = "Fase",
            ["op_fillet_radius"]    = "Radius (mm)",
            ["op_chamfer_size"]     = "Größe (mm)",
            ["btn_apply_fillet"]    = "Fillet anwenden",
            ["btn_apply_chamfer"]   = "Chamfer anwenden",
            ["op_union"]            = "Vereinigung",
            ["op_subtract"]         = "Subtraktion",
            ["op_intersect"]        = "Schnitt",
            ["btn_import_stl"]      = "3D-Datei laden",
            ["btn_scad_preview"]    = "Vorschau",
            ["btn_scad_apply"]      = "Übernehmen",
            ["btn_scad_export"]     = "Als SCAD exportieren",
            ["btn_delete"]          = "Objekt löschen",
            ["btn_duplicate"]       = "Duplizieren",
            ["status_ready"]        = "Bereit",
            ["status_objects"]      = "Objekte",
            ["status_active"]       = "Aktiv",
            ["status_loading"]      = "Lade...",
            ["status_none"]         = "-",
            ["object_list"]         = "Objektliste",
            ["msg_no_selection"]    = "Kein Objekt ausgewählt.",
            ["msg_select_two"]      = "Bitte zwei Objekte für Boolean-Operationen auswählen.",
            ["msg_fillet_error"]    = "Fehler: Radius möglicherweise zu groß.",
            ["msg_chamfer_error"]   = "Fehler beim Anwenden der Fase.",
            ["msg_export_success"]  = "STL erfolgreich exportiert.",
            ["msg_export_error"]    = "Fehler beim Exportieren.",
            ["msg_scad_not_found"]  = "OpenSCAD nicht gefunden. Bitte installieren.",
            ["msg_scad_error"]      = "Fehler beim Kompilieren des OpenSCAD-Codes.",
            ["msg_scad_success"]    = "OpenSCAD-Modell erstellt.",
            ["dlg_open_stl"]        = "3D-Datei öffnen",
            ["dlg_open_scad"]       = "SCAD-Datei öffnen",
            ["dlg_export_stl"]      = "Als STL exportieren",
            ["dlg_filter_stl"]      = "3D-Dateien (*.stl;*.3mf)|*.stl;*.3mf|STL-Dateien (*.stl)|*.stl|3MF-Dateien (*.3mf)|*.3mf|Alle Dateien (*.*)|*.*",
            ["dlg_filter_stl_save"] = "STL-Dateien (*.stl)|*.stl|Alle Dateien (*.*)|*.*",
            ["dlg_filter_scad"]     = "OpenSCAD-Dateien (*.scad)|*.scad|Alle Dateien (*.*)|*.*",
            ["dlg_confirm_new"]     = "Alle ungespeicherten Änderungen gehen verloren. Fortfahren?",
            ["dlg_confirm"]         = "Bestätigen",
            ["help_about"]          = "Über 3D-Builder",
            ["lang_de"]             = "Deutsch",
            ["lang_en"]             = "English",
            ["loading_backend"]     = "Starte Geometrie-Engine...",
            ["backend_ready"]       = "Bereit",
            ["backend_error"]       = "Backend nicht verfügbar",
            ["scad_placeholder"]    = "// OpenSCAD-Code hier eingeben\n// Beispiel:\ncube([20, 20, 20], center=true);",
        },
        ["en"] = new()
        {
            ["app_title"]           = "3D Builder for 3D Printing",
            ["menu_file"]           = "File",
            ["menu_edit"]           = "Edit",
            ["menu_view"]           = "View",
            ["menu_language"]       = "Language",
            ["menu_help"]           = "Help",
            ["action_new"]          = "New",
            ["action_open_stl"]     = "Open STL...",
            ["action_open_scad"]    = "Open SCAD...",
            ["action_save"]         = "Save",
            ["action_save_as"]      = "Save As...",
            ["action_export_stl"]   = "Export as STL...",
            ["action_export_scad"]  = "Export as SCAD...",
            ["action_quit"]         = "Quit",
            ["action_undo"]         = "Undo",
            ["action_redo"]         = "Redo",
            ["action_delete"]       = "Delete",
            ["action_duplicate"]    = "Duplicate",
            ["action_reset_view"]   = "Reset View",
            ["action_top_view"]     = "Top View",
            ["action_front_view"]   = "Front View",
            ["action_side_view"]    = "Side View",
            ["action_iso_view"]     = "Isometric View",
            ["panel_shapes"]        = "SHAPES",
            ["panel_import"]        = "IMPORT",
            ["panel_operations"]    = "OPERATIONS",
            ["panel_properties"]    = "PROPERTIES",
            ["panel_scad_editor"]   = "OPENSCAD EDITOR",
            ["tab_builder"]         = "3D Builder",
            ["tab_scad"]            = "OpenSCAD Editor",
            ["shape_box"]           = "Box",
            ["shape_sphere"]        = "Sphere",
            ["shape_cylinder"]      = "Cylinder",
            ["shape_cone"]          = "Cone",
            ["shape_torus"]         = "Torus",
            ["shape_prism"]         = "Prism",
            ["shape_pyramid"]       = "Pyramid",
            ["shape_tube"]          = "Tube",
            ["shape_ellipsoid"]     = "Ellipsoid",
            ["shape_hemisphere"]    = "Hemisphere",
            ["shape_l_profile"]     = "L-Profile",
            ["shape_t_profile"]     = "T-Profile",
            ["shape_star"]          = "Star",
            ["shape_polygon"]       = "Polygon",
            ["shape_thread_cyl"]    = "Threaded Cylinder",
            ["param_width"]         = "Width",
            ["param_height"]        = "Height",
            ["param_depth"]         = "Depth",
            ["param_radius"]        = "Radius",
            ["param_radius_bottom"] = "Bottom Radius",
            ["param_radius_top"]    = "Top Radius",
            ["param_radius_major"]  = "Major Radius",
            ["param_radius_minor"]  = "Minor Radius",
            ["param_radius_outer"]  = "Outer Radius",
            ["param_radius_inner"]  = "Inner Radius",
            ["param_sides"]         = "Sides",
            ["param_points"]        = "Points",
            ["param_base_size"]     = "Base Size",
            ["param_outer_r"]       = "Outer Radius",
            ["param_inner_r"]       = "Inner Radius",
            ["param_rx"]            = "Radius X",
            ["param_ry"]            = "Radius Y",
            ["param_rz"]            = "Radius Z",
            ["param_length"]        = "Length",
            ["param_thickness"]     = "Thickness",
            ["param_pitch"]         = "Thread Pitch",
            ["section_position"]    = "Position (mm)",
            ["section_rotation"]    = "Rotation (°)",
            ["section_dimensions"]  = "Dimensions",
            ["op_fillet"]           = "Round Edges",
            ["op_chamfer"]          = "Chamfer",
            ["op_fillet_radius"]    = "Radius (mm)",
            ["op_chamfer_size"]     = "Size (mm)",
            ["btn_apply_fillet"]    = "Apply Fillet",
            ["btn_apply_chamfer"]   = "Apply Chamfer",
            ["op_union"]            = "Union",
            ["op_subtract"]         = "Subtract",
            ["op_intersect"]        = "Intersect",
            ["btn_import_stl"]      = "Load 3D File",
            ["btn_scad_preview"]    = "Preview",
            ["btn_scad_apply"]      = "Apply",
            ["btn_scad_export"]     = "Export as SCAD",
            ["btn_delete"]          = "Delete Object",
            ["btn_duplicate"]       = "Duplicate",
            ["status_ready"]        = "Ready",
            ["status_objects"]      = "Objects",
            ["status_active"]       = "Active",
            ["status_loading"]      = "Loading...",
            ["status_none"]         = "-",
            ["object_list"]         = "Object List",
            ["msg_no_selection"]    = "No object selected.",
            ["msg_select_two"]      = "Please select two objects for boolean operations.",
            ["msg_fillet_error"]    = "Error: radius may be too large.",
            ["msg_chamfer_error"]   = "Error applying chamfer.",
            ["msg_export_success"]  = "STL exported successfully.",
            ["msg_export_error"]    = "Export failed.",
            ["msg_scad_not_found"]  = "OpenSCAD not found. Please install it.",
            ["msg_scad_error"]      = "Error compiling OpenSCAD code.",
            ["msg_scad_success"]    = "OpenSCAD model created.",
            ["dlg_open_stl"]        = "Open 3D File",
            ["dlg_open_scad"]       = "Open SCAD File",
            ["dlg_export_stl"]      = "Export as STL",
            ["dlg_filter_stl"]      = "3D Files (*.stl;*.3mf)|*.stl;*.3mf|STL Files (*.stl)|*.stl|3MF Files (*.3mf)|*.3mf|All Files (*.*)|*.*",
            ["dlg_filter_stl_save"] = "STL Files (*.stl)|*.stl|All Files (*.*)|*.*",
            ["dlg_filter_scad"]     = "OpenSCAD Files (*.scad)|*.scad|All Files (*.*)|*.*",
            ["dlg_confirm_new"]     = "All unsaved changes will be lost. Continue?",
            ["dlg_confirm"]         = "Confirm",
            ["help_about"]          = "About 3D Builder",
            ["lang_de"]             = "Deutsch",
            ["lang_en"]             = "English",
            ["loading_backend"]     = "Starting geometry engine...",
            ["backend_ready"]       = "Ready",
            ["backend_error"]       = "Backend unavailable",
            ["scad_placeholder"]    = "// Enter OpenSCAD code here\n// Example:\ncube([20, 20, 20], center=true);",
        },
    };
}

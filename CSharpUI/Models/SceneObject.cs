using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ThreeDBuilder.Models;

/// <summary>
/// Represents a single 3D object in the scene.
/// Holds shape parameters, position, rotation, and the path to its current STL preview file.
/// </summary>
public class SceneObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _id = Guid.NewGuid().ToString();
    private string _name = "Object";
    private string _shapeType = "box";
    private Dictionary<string, object> _params = new();
    private double _posX, _posY, _posZ;
    private double _rotX, _rotY, _rotZ;
    private string? _stlPath;
    private bool _isSelected;
    private bool _isSubtractor;

    public string Id { get => _id; init => _id = value; }

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public string ShapeType
    {
        get => _shapeType;
        set { _shapeType = value; OnPropertyChanged(); }
    }

    public Dictionary<string, object> Params
    {
        get => _params;
        set { _params = value; OnPropertyChanged(); }
    }

    public double PosX { get => _posX; set { _posX = value; OnPropertyChanged(); } }
    public double PosY { get => _posY; set { _posY = value; OnPropertyChanged(); } }
    public double PosZ { get => _posZ; set { _posZ = value; OnPropertyChanged(); } }

    public double RotX { get => _rotX; set { _rotX = value; OnPropertyChanged(); } }
    public double RotY { get => _rotY; set { _rotY = value; OnPropertyChanged(); } }
    public double RotZ { get => _rotZ; set { _rotZ = value; OnPropertyChanged(); } }

    public string? StlPath
    {
        get => _stlPath;
        set { _stlPath = value; OnPropertyChanged(); }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// When true, this object acts as a "cutter" (subtractor).
    /// It is shown semi-transparent red in the viewport and can be used
    /// to hollow out other objects via boolean subtraction.
    /// </summary>
    public bool IsSubtractor
    {
        get => _isSubtractor;
        set { _isSubtractor = value; OnPropertyChanged(); }
    }

    /// <summary>Serialize to a dict for sending to the Python backend.</summary>
    public Dictionary<string, object?> ToBackendDict() => new()
    {
        ["shape_type"] = ShapeType,
        ["params"]     = Params,
        ["pos_x"]      = PosX,
        ["pos_y"]      = PosY,
        ["pos_z"]      = PosZ,
        ["rot_x"]      = RotX,
        ["rot_y"]      = RotY,
        ["rot_z"]      = RotZ,
        ["name"]       = Name,
    };

    /// <summary>
    /// Create a deep copy of this object.
    /// When <paramref name="forUndo"/> is true the original name is preserved;
    /// for a user-visible duplicate the suffix " (Kopie)" is appended.
    /// </summary>
    public SceneObject Clone(bool forUndo = false) => new()
    {
        Id           = Guid.NewGuid().ToString(),
        Name         = forUndo ? Name : Name + " (Kopie)",
        ShapeType    = ShapeType,
        Params       = new Dictionary<string, object>(Params),
        PosX = PosX, PosY = PosY, PosZ = PosZ,
        RotX = RotX, RotY = RotY, RotZ = RotZ,
        StlPath      = StlPath,
        IsSubtractor = IsSubtractor,
    };

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

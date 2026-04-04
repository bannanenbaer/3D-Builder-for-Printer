using System;
using System.Collections.Generic;
using System.Linq;

namespace ThreeDBuilder.Services
{
    /// <summary>
    /// Undo/Redo Service - Verwaltet die Bearbeitungshistorie
    /// </summary>
    public class UndoRedoService
    {
        public interface IUndoRedoAction
        {
            void Execute();
            void Undo();
            string Description { get; }
        }

        private readonly Stack<IUndoRedoAction> _undoStack = new();
        private readonly Stack<IUndoRedoAction> _redoStack = new();
        private readonly int _maxHistorySize;

        public event EventHandler HistoryChanged;

        public UndoRedoService(int maxHistorySize = 100)
        {
            _maxHistorySize = maxHistorySize;
        }

        /// <summary>
        /// Führt eine Aktion aus und speichert sie in der Historie
        /// </summary>
        public void Execute(IUndoRedoAction action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            action.Execute();
            _undoStack.Push(action);

            // Limit history size
            if (_undoStack.Count > _maxHistorySize)
            {
                var temp = _undoStack.ToList();
                _undoStack.Clear();
                foreach (var item in temp.Take(_maxHistorySize))
                {
                    _undoStack.Push(item);
                }
            }

            // Clear redo stack when new action is executed
            _redoStack.Clear();

            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Macht die letzte Aktion rückgängig
        /// </summary>
        public bool Undo()
        {
            if (!CanUndo)
                return false;

            var action = _undoStack.Pop();
            action.Undo();
            _redoStack.Push(action);

            HistoryChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Wiederholt die letzte rückgängig gemachte Aktion
        /// </summary>
        public bool Redo()
        {
            if (!CanRedo)
                return false;

            var action = _redoStack.Pop();
            action.Execute();
            _undoStack.Push(action);

            HistoryChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Löscht die komplette Historie
        /// </summary>
        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public string GetUndoDescription()
        {
            return CanUndo ? _undoStack.Peek().Description : "Nichts zum Rückgängigmachen";
        }

        public string GetRedoDescription()
        {
            return CanRedo ? _redoStack.Peek().Description : "Nichts zum Wiederherstellen";
        }

        public int UndoCount => _undoStack.Count;
        public int RedoCount => _redoStack.Count;
    }

    /// <summary>
    /// Konkrete Implementierungen für verschiedene Aktionen
    /// </summary>
    public class ShapeAddedAction : UndoRedoService.IUndoRedoAction
    {
        private readonly Action _executeAction;
        private readonly Action _undoAction;
        private readonly string _shapeName;

        public string Description => $"Form '{_shapeName}' hinzugefügt";

        public ShapeAddedAction(string shapeName, Action executeAction, Action undoAction)
        {
            _shapeName = shapeName;
            _executeAction = executeAction;
            _undoAction = undoAction;
        }

        public void Execute() => _executeAction?.Invoke();
        public void Undo() => _undoAction?.Invoke();
    }

    public class ShapeModifiedAction : UndoRedoService.IUndoRedoAction
    {
        private readonly Action _executeAction;
        private readonly Action _undoAction;
        private readonly string _shapeName;
        private readonly string _modification;

        public string Description => $"'{_shapeName}' - {_modification}";

        public ShapeModifiedAction(string shapeName, string modification, Action executeAction, Action undoAction)
        {
            _shapeName = shapeName;
            _modification = modification;
            _executeAction = executeAction;
            _undoAction = undoAction;
        }

        public void Execute() => _executeAction?.Invoke();
        public void Undo() => _undoAction?.Invoke();
    }

    public class BooleanOperationAction : UndoRedoService.IUndoRedoAction
    {
        private readonly Action _executeAction;
        private readonly Action _undoAction;
        private readonly string _operation;
        private readonly string _shape1;
        private readonly string _shape2;

        public string Description => $"{_operation}: '{_shape1}' + '{_shape2}'";

        public BooleanOperationAction(string operation, string shape1, string shape2, Action executeAction, Action undoAction)
        {
            _operation = operation;
            _shape1 = shape1;
            _shape2 = shape2;
            _executeAction = executeAction;
            _undoAction = undoAction;
        }

        public void Execute() => _executeAction?.Invoke();
        public void Undo() => _undoAction?.Invoke();
    }

    public class FilletAction : UndoRedoService.IUndoRedoAction
    {
        private readonly Action _executeAction;
        private readonly Action _undoAction;
        private readonly string _shapeName;
        private readonly float _radius;

        public string Description => $"Fillet '{_shapeName}' ({_radius}mm)";

        public FilletAction(string shapeName, float radius, Action executeAction, Action undoAction)
        {
            _shapeName = shapeName;
            _radius = radius;
            _executeAction = executeAction;
            _undoAction = undoAction;
        }

        public void Execute() => _executeAction?.Invoke();
        public void Undo() => _undoAction?.Invoke();
    }

    public class ChamferAction : UndoRedoService.IUndoRedoAction
    {
        private readonly Action _executeAction;
        private readonly Action _undoAction;
        private readonly string _shapeName;
        private readonly float _size;

        public string Description => $"Chamfer '{_shapeName}' ({_size}mm)";

        public ChamferAction(string shapeName, float size, Action executeAction, Action undoAction)
        {
            _shapeName = shapeName;
            _size = size;
            _executeAction = executeAction;
            _undoAction = undoAction;
        }

        public void Execute() => _executeAction?.Invoke();
        public void Undo() => _undoAction?.Invoke();
    }

    public class AutoFixAction : UndoRedoService.IUndoRedoAction
    {
        private readonly Action _executeAction;
        private readonly Action _undoAction;
        private readonly string _shapeName;

        public string Description => $"AutoFix '{_shapeName}' - Optimiert für Druck";

        public AutoFixAction(string shapeName, Action executeAction, Action undoAction)
        {
            _shapeName = shapeName;
            _executeAction = executeAction;
            _undoAction = undoAction;
        }

        public void Execute() => _executeAction?.Invoke();
        public void Undo() => _undoAction?.Invoke();
    }
}

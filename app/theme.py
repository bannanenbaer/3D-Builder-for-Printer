# -*- coding: utf-8 -*-
"""Dark / Light theme palette for PyQt5."""

from PyQt5.QtGui import QPalette, QColor
from PyQt5.QtCore import Qt


def apply_dark(app):
    palette = QPalette()
    palette.setColor(QPalette.Window,          QColor(30, 30, 30))
    palette.setColor(QPalette.WindowText,      QColor(220, 220, 220))
    palette.setColor(QPalette.Base,            QColor(22, 22, 22))
    palette.setColor(QPalette.AlternateBase,   QColor(40, 40, 40))
    palette.setColor(QPalette.ToolTipBase,     QColor(50, 50, 50))
    palette.setColor(QPalette.ToolTipText,     QColor(220, 220, 220))
    palette.setColor(QPalette.Text,            QColor(220, 220, 220))
    palette.setColor(QPalette.Button,          QColor(50, 50, 50))
    palette.setColor(QPalette.ButtonText,      QColor(220, 220, 220))
    palette.setColor(QPalette.BrightText,      Qt.red)
    palette.setColor(QPalette.Link,            QColor(80, 160, 255))
    palette.setColor(QPalette.Highlight,       QColor(30, 100, 200))
    palette.setColor(QPalette.HighlightedText, QColor(255, 255, 255))
    palette.setColor(QPalette.Disabled, QPalette.Text,       QColor(100, 100, 100))
    palette.setColor(QPalette.Disabled, QPalette.ButtonText, QColor(100, 100, 100))
    app.setPalette(palette)
    app.setStyleSheet("""
        QMainWindow, QDialog { background: #1e1e1e; }
        QDockWidget::title {
            background: #2a2a2a; padding: 4px 8px;
            font-weight: bold; color: #ddd;
        }
        QToolBar { background: #252525; border: none; spacing: 4px; }
        QToolBar QToolButton {
            background: transparent; border: none; padding: 4px 8px;
            color: #ddd; border-radius: 4px;
        }
        QToolBar QToolButton:hover { background: #3a3a3a; }
        QToolBar QToolButton:pressed { background: #1e6ec8; }
        QPushButton {
            background: #2d5fa6; color: #fff;
            border: none; border-radius: 4px;
            padding: 5px 12px; font-size: 12px;
        }
        QPushButton:hover   { background: #3570c0; }
        QPushButton:pressed { background: #1a4a8a; }
        QPushButton:disabled { background: #444; color: #777; }
        QPushButton[danger="true"] { background: #a03030; }
        QPushButton[danger="true"]:hover { background: #c03030; }
        QLineEdit, QDoubleSpinBox, QSpinBox, QComboBox, QTextEdit, QPlainTextEdit {
            background: #2a2a2a; color: #ddd;
            border: 1px solid #444; border-radius: 3px; padding: 3px 6px;
        }
        QLineEdit:focus, QDoubleSpinBox:focus, QSpinBox:focus,
        QComboBox:focus, QTextEdit:focus, QPlainTextEdit:focus {
            border: 1px solid #3570c0;
        }
        QComboBox::drop-down { border: none; }
        QComboBox QAbstractItemView { background: #2a2a2a; color: #ddd; border: 1px solid #444; }
        QListWidget { background: #1e1e1e; color: #ddd; border: 1px solid #333; }
        QListWidget::item:selected { background: #2d5fa6; }
        QListWidget::item:hover    { background: #2a2a2a; }
        QGroupBox {
            color: #aaa; border: 1px solid #383838;
            border-radius: 4px; margin-top: 8px; padding-top: 4px;
        }
        QGroupBox::title { subcontrol-origin: margin; left: 8px; padding: 0 4px; }
        QLabel { color: #ddd; }
        QStatusBar { background: #252525; color: #aaa; }
        QScrollBar:vertical {
            background: #1e1e1e; width: 10px; border: none;
        }
        QScrollBar::handle:vertical { background: #444; border-radius: 5px; min-height: 20px; }
        QScrollBar::handle:vertical:hover { background: #555; }
        QScrollBar::add-line:vertical, QScrollBar::sub-line:vertical { height: 0; }
        QSplitter::handle { background: #333; }
        QTabWidget::pane { border: 1px solid #333; }
        QTabBar::tab {
            background: #2a2a2a; color: #aaa;
            padding: 5px 12px; border: 1px solid #333;
            border-bottom: none; border-radius: 3px 3px 0 0;
        }
        QTabBar::tab:selected { background: #1e1e1e; color: #ddd; }
        QTabBar::tab:hover    { background: #353535; }
        QSlider::groove:horizontal {
            height: 4px; background: #444; border-radius: 2px;
        }
        QSlider::handle:horizontal {
            background: #3570c0; width: 14px; height: 14px;
            margin: -5px 0; border-radius: 7px;
        }
        QCheckBox { color: #ddd; }
        QCheckBox::indicator { width: 15px; height: 15px; border: 1px solid #555; border-radius: 2px; background: #2a2a2a; }
        QCheckBox::indicator:checked { background: #2d5fa6; }
        QMenuBar { background: #252525; color: #ddd; }
        QMenuBar::item:selected { background: #333; }
        QMenu { background: #2a2a2a; color: #ddd; border: 1px solid #444; }
        QMenu::item:selected { background: #2d5fa6; }
        QMenu::separator { height: 1px; background: #444; margin: 2px 0; }
    """)


def apply_light(app):
    app.setPalette(app.style().standardPalette())
    app.setStyleSheet("")

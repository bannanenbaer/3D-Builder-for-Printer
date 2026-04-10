# -*- coding: utf-8 -*-
"""OpenSCAD editor panel."""

from PyQt5.QtWidgets import (
    QWidget, QVBoxLayout, QHBoxLayout, QPushButton,
    QPlainTextEdit, QLabel, QSplitter,
)
from PyQt5.QtGui  import QFont, QColor, QSyntaxHighlighter, QTextCharFormat
from PyQt5.QtCore import pyqtSignal, Qt, QRegExp


class _ScadHighlighter(QSyntaxHighlighter):
    def __init__(self, document):
        super().__init__(document)
        kw_fmt = QTextCharFormat()
        kw_fmt.setForeground(QColor('#569cd6'))
        kw_fmt.setFontWeight(QFont.Bold)

        num_fmt = QTextCharFormat()
        num_fmt.setForeground(QColor('#b5cea8'))

        comment_fmt = QTextCharFormat()
        comment_fmt.setForeground(QColor('#6a9955'))
        comment_fmt.setFontItalic(True)

        fn_fmt = QTextCharFormat()
        fn_fmt.setForeground(QColor('#dcdcaa'))

        keywords = [
            'module', 'function', 'for', 'if', 'else', 'let', 'each',
            'include', 'use', 'true', 'false', 'undef',
        ]
        builtins = [
            'cube', 'sphere', 'cylinder', 'polyhedron', 'union', 'difference',
            'intersection', 'translate', 'rotate', 'scale', 'mirror',
            'linear_extrude', 'rotate_extrude', 'import', 'color',
            'offset', 'hull', 'minkowski', 'polygon', 'circle', 'square',
            'text', 'projection', 'surface',
        ]
        self._rules = []
        for kw in keywords:
            self._rules.append((QRegExp(r'\b' + kw + r'\b'), kw_fmt))
        for bi in builtins:
            self._rules.append((QRegExp(r'\b' + bi + r'\b'), fn_fmt))
        self._rules.append((QRegExp(r'\b\d+\.?\d*\b'), num_fmt))
        self._rules.append((QRegExp(r'//[^\n]*'), comment_fmt))

    def highlightBlock(self, text):
        for pattern, fmt in self._rules:
            idx = pattern.indexIn(text)
            while idx >= 0:
                length = pattern.matchedLength()
                self.setFormat(idx, length, fmt)
                idx = pattern.indexIn(text, idx + length)


class ScadPanel(QWidget):
    compile_requested = pyqtSignal(str)   # scad code
    export_requested  = pyqtSignal()

    def __init__(self, parent=None):
        super().__init__(parent)
        self._build_ui()

    def _build_ui(self):
        root = QVBoxLayout(self)
        root.setContentsMargins(4, 4, 4, 4)
        root.setSpacing(4)

        # Toolbar
        tb = QHBoxLayout()
        tb.setSpacing(4)
        compile_btn = QPushButton('Kompilieren (F5)')
        compile_btn.setShortcut('F5')
        compile_btn.clicked.connect(self._on_compile)

        export_btn = QPushButton('SCAD exportieren')
        export_btn.clicked.connect(self.export_requested)

        self.status_lbl = QLabel('Bereit')
        self.status_lbl.setStyleSheet('color: #aaa; font-size: 11px;')

        tb.addWidget(compile_btn)
        tb.addWidget(export_btn)
        tb.addStretch(1)
        tb.addWidget(self.status_lbl)
        root.addLayout(tb)

        # Splitter: editor + error output
        splitter = QSplitter(Qt.Vertical)

        self.editor = QPlainTextEdit()
        self.editor.setFont(QFont('Monospace', 11))
        self.editor.setLineWrapMode(QPlainTextEdit.NoWrap)
        self.editor.setPlaceholderText(
            '// OpenSCAD-Code hier eingeben\n'
            '// Beispiel:\n'
            'cube([20, 20, 20], center=true);'
        )
        _ScadHighlighter(self.editor.document())
        splitter.addWidget(self.editor)

        self.output = QPlainTextEdit()
        self.output.setReadOnly(True)
        self.output.setFont(QFont('Monospace', 10))
        self.output.setMaximumHeight(120)
        self.output.setStyleSheet('background: #111; color: #f88;')
        splitter.addWidget(self.output)
        splitter.setSizes([400, 100])

        root.addWidget(splitter, 1)

    def _on_compile(self):
        code = self.editor.toPlainText().strip()
        if code:
            self.status_lbl.setText('Kompiliere...')
            self.compile_requested.emit(code)
        else:
            self.output.setPlainText('Kein Code vorhanden.')

    def set_code(self, code: str):
        self.editor.setPlainText(code)

    def set_status(self, ok: bool, message: str = ''):
        if ok:
            self.status_lbl.setText('OK')
            self.status_lbl.setStyleSheet('color: #4c4; font-size: 11px;')
            self.output.setPlainText('')
        else:
            self.status_lbl.setText('Fehler')
            self.status_lbl.setStyleSheet('color: #f44; font-size: 11px;')
            self.output.setPlainText(message)

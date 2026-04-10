# -*- coding: utf-8 -*-
"""Brixl AI assistant panel."""

import threading

from PyQt5.QtWidgets import (
    QWidget, QVBoxLayout, QHBoxLayout, QPushButton,
    QLineEdit, QTextEdit, QLabel, QScrollBar,
)
from PyQt5.QtCore    import pyqtSignal, Qt, QMetaObject, Q_ARG
from PyQt5.QtGui     import QTextCursor, QFont

from app.services import ai_service, settings_service as cfg


class AssistantPanel(QWidget):
    def __init__(self, parent=None):
        super().__init__(parent)
        self._build_ui()
        self._greet()

    def _build_ui(self):
        root = QVBoxLayout(self)
        root.setContentsMargins(8, 8, 8, 8)
        root.setSpacing(6)

        # Header
        header = QHBoxLayout()
        icon = QLabel('🤖')
        icon.setFont(QFont('', 20))
        header.addWidget(icon)
        title = QLabel('Brixl – 3D-Druck Assistent')
        title.setStyleSheet('font-weight: bold; font-size: 13px;')
        header.addWidget(title, 1)
        clear_btn = QPushButton('Löschen')
        clear_btn.setMaximumWidth(70)
        clear_btn.clicked.connect(self._clear_chat)
        header.addWidget(clear_btn)
        root.addLayout(header)

        # Chat display
        self.chat = QTextEdit()
        self.chat.setReadOnly(True)
        self.chat.setFont(QFont('Sans', 11))
        self.chat.setStyleSheet(
            'background: #1a1a2e; color: #ddd; border: 1px solid #333; border-radius: 4px;'
        )
        root.addWidget(self.chat, 1)

        # Input row
        row = QHBoxLayout()
        row.setSpacing(4)
        self.input = QLineEdit()
        self.input.setPlaceholderText('Frage eingeben… (Enter zum Senden)')
        self.input.returnPressed.connect(self._send)
        row.addWidget(self.input, 1)
        send_btn = QPushButton('Senden')
        send_btn.clicked.connect(self._send)
        row.addWidget(send_btn)
        root.addLayout(row)

        self._busy = False

    def _greet(self):
        lang = cfg.get('language', 'de')
        if lang == 'de':
            msg = ('Hallo! Ich bin <b>Brixl</b>, dein 3D-Druck-Assistent.<br>'
                   'Frag mich zu Formen, Export, Fillet, Boolean-Operationen u.v.m.')
        else:
            msg = ('Hi! I\'m <b>Brixl</b>, your 3D print assistant.<br>'
                   'Ask me about shapes, export, fillet, boolean ops and more.')
        self._append_bot(msg)

    def _send(self):
        text = self.input.text().strip()
        if not text or self._busy:
            return
        self.input.clear()
        self._append_user(text)
        self._busy = True
        threading.Thread(target=self._ask_thread, args=(text,), daemon=True).start()

    def _ask_thread(self, text: str):
        lang = cfg.get('language', 'de')
        try:
            answer = ai_service.ask(text, lang)
        except Exception as e:
            answer = f'Fehler: {e}'
        # Marshal back to Qt main thread
        QMetaObject.invokeMethod(
            self, '_append_bot_slot',
            Qt.QueuedConnection,
            Q_ARG(str, answer)
        )

    def _append_user(self, text: str):
        self.chat.append(
            f'<p style="text-align:right; color:#82aaff;">'
            f'<b>Du:</b> {self._escape(text)}</p>'
        )

    def _append_bot(self, html: str):
        self.chat.append(
            f'<p style="color:#c3e88d;"><b>Brixl:</b> {html}</p>'
        )
        self.chat.moveCursor(QTextCursor.End)

    @staticmethod
    def _escape(text: str) -> str:
        return text.replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;')

    # Qt slot called from background thread via invokeMethod
    def _append_bot_slot(self, text: str):
        self._append_bot(self._escape(text))
        self._busy = False

    def _clear_chat(self):
        self.chat.clear()
        self._greet()

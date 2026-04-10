# -*- coding: utf-8 -*-
"""
AI assistant backend.
Supports local rule-based responses and Claude API (optional).
"""

import re
from . import settings_service as cfg

_LOCAL_KB = [
    (r'fillet|rundung|kante\s*rund', 'Für Kantenrundungen wähle "Fillet" im Eigenschaften-Panel und stelle den Radius ein (z.B. 1–3 mm).'),
    (r'chamfer|fase|abschräg', 'Mit "Chamfer" kannst du Kanten abschrägen. Kleine Werte (0.5–2 mm) eignen sich gut für den 3D-Druck.'),
    (r'boolean|vereinig|schneid|verbind', 'Boolean-Operationen findest du unter Bearbeiten: Union verbindet, Cut subtrahiert, Intersect gibt nur den Überschnitt zurück.'),
    (r'stl.*export|exportier|speicher', 'Nutze Datei → Exportieren → STL. Die Datei kannst du direkt in PrusaSlicer, Cura oder Bambu Studio laden.'),
    (r'3mf|import', '3MF-Dateien importierst du über Datei → Importieren → 3MF. Das Format speichert auch Farben und Materialien.'),
    (r'wand.*dünn|thin.*wall|wandstärke', 'Für FDM-Druck empfehle ich mindestens 1.2 mm Wandstärke (= 3× 0.4 mm Nozzle). Dünne Wände können im AutoFix-Panel markiert werden.'),
    (r'support|stütz', 'Überhänge > 45° benötigen Stützmaterial. Drehe dein Modell im 3D-Viewport so, dass möglichst wenige Überhänge entstehen.'),
    (r'openscad|scad', 'Im SCAD-Editor kannst du OpenSCAD-Code schreiben und direkt in der Vorschau rendern. OpenSCAD muss installiert sein.'),
    (r'fehler|error|problem|kaputt', 'Nutze das AutoFix-Panel (Zahnrad-Symbol) für automatische Fehlererkennung und Reparatur.'),
    (r'hallo|hi|guten\s*(tag|morgen|abend)', 'Hallo! Ich bin Brixl, dein 3D-Druck-Assistent. Wie kann ich dir helfen?'),
]

_EN_KB = [
    (r'fillet|round.*edge|edge.*round', 'Use "Fillet" in the Properties panel to round edges. A radius of 1–3 mm works well for most prints.'),
    (r'chamfer|bevel', '"Chamfer" bevels edges. Small values (0.5–2 mm) are good for 3D printing.'),
    (r'boolean|union|cut|intersect|combine|subtract', 'Boolean operations are under Edit: Union combines, Cut subtracts, Intersect keeps only the overlap.'),
    (r'export.*stl|save.*stl', 'Use File → Export → STL. The file loads directly in PrusaSlicer, Cura, or Bambu Studio.'),
    (r'import|3mf', 'Import STL or 3MF files via File → Import.'),
    (r'thin.*wall|wall.*thick', 'For FDM printing, use at least 1.2 mm wall thickness (3× 0.4 mm nozzle). The AutoFix panel can detect thin walls.'),
    (r'support|overhang', 'Overhangs > 45° need support material. Rotate your model to minimise overhangs.'),
    (r'openscad|scad', 'Use the SCAD Editor to write OpenSCAD code and preview it. OpenSCAD must be installed.'),
    (r'error|broken|fix|problem', 'Use the AutoFix panel (gear icon) for automatic error detection and repair.'),
    (r'hello|hi|hey', "Hi! I'm Brixl, your 3D print assistant. How can I help you?"),
]


def local_answer(text: str, lang: str = 'de') -> str:
    kb = _LOCAL_KB if lang == 'de' else _EN_KB
    t = text.lower()
    for pattern, answer in kb:
        if re.search(pattern, t):
            return answer
    if lang == 'de':
        return ('Ich bin mir nicht sicher. Versuche es mit Stichwörtern wie '
                '"Fillet", "Boolean", "Export" oder "AutoFix".')
    return ('I\'m not sure. Try keywords like "fillet", "boolean", "export" or "autofix".')


def ask(question: str, lang: str = 'de') -> str:
    provider = cfg.get('ai_provider', 'local')
    if provider == 'claude':
        api_key = cfg.get('claude_api_key', '')
        if api_key:
            return _ask_claude(question, api_key, lang)
    return local_answer(question, lang)


def _ask_claude(question: str, api_key: str, lang: str) -> str:
    try:
        import anthropic
        client = anthropic.Anthropic(api_key=api_key)
        system = (
            'Du bist Brixl, ein freundlicher Assistent für 3D-Druck und CAD-Modellierung. '
            'Antworte kurz und präzise auf Deutsch.'
            if lang == 'de' else
            'You are Brixl, a helpful assistant for 3D printing and CAD modelling. '
            'Answer briefly and accurately in English.'
        )
        msg = client.messages.create(
            model='claude-opus-4-6',
            max_tokens=512,
            system=system,
            messages=[{'role': 'user', 'content': question}]
        )
        return msg.content[0].text
    except Exception as e:
        return local_answer(question, lang) + f'\n\n[Claude API Fehler: {e}]'

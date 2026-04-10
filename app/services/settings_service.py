# -*- coding: utf-8 -*-
"""Persistent settings stored as JSON in ~/.config/3dbuilder/settings.json"""

import json, os

_CFG_DIR  = os.path.expanduser('~/.config/3dbuilder')
_CFG_FILE = os.path.join(_CFG_DIR, 'settings.json')

_DEFAULTS = {
    'language':        'de',        # 'de' | 'en'
    'theme':           'dark',      # 'dark' | 'light'
    'ai_provider':     'local',     # 'local' | 'claude'
    'claude_api_key':  '',
    'autosave':        True,
    'autosave_interval': 300,       # seconds
    'openscad_path':   '',
    'last_dir':        os.path.expanduser('~'),
}

_data: dict = {}


def _load():
    global _data
    _data = dict(_DEFAULTS)
    if os.path.isfile(_CFG_FILE):
        try:
            with open(_CFG_FILE, 'r', encoding='utf-8') as f:
                _data.update(json.load(f))
        except Exception:
            pass


def _save():
    os.makedirs(_CFG_DIR, exist_ok=True)
    with open(_CFG_FILE, 'w', encoding='utf-8') as f:
        json.dump(_data, f, indent=2, ensure_ascii=False)


def get(key: str, default=None):
    if not _data:
        _load()
    return _data.get(key, _DEFAULTS.get(key, default))


def set(key: str, value):
    if not _data:
        _load()
    _data[key] = value
    _save()


def all_settings() -> dict:
    if not _data:
        _load()
    return dict(_data)


_load()

#!/usr/bin/env python3
import sys
import re
from pathlib import Path

try:
    import yaml
except Exception as e:
    print("MISSING: PyYAML not installed. Install with: python -m pip install pyyaml")
    sys.exit(2)

p = Path('azure-pipelines.yml')
if not p.exists():
    print('ERROR: azure-pipelines.yml not found in repository root')
    sys.exit(1)

text = p.read_text(encoding='utf-8')
lines = text.splitlines()

errors = []
warnings = []

# Check for tabs
for i, l in enumerate(lines, 1):
    if '\t' in l:
        warnings.append(f'Line {i}: TAB character found (YAML prefers spaces).')

# Check condition lines for unbalanced parentheses
for i, l in enumerate(lines, 1):
    if 'condition:' in l:
        # collect the rest of the same indented block (simple heuristic)
        cond = l.split('condition:', 1)[1]
        # naive check: parentheses balance in the single line
        if cond.count('(') != cond.count(')'):
            errors.append(f'Line {i}: Unbalanced parentheses in condition: {cond.strip()}')

# Detect duplicate mapping keys using a custom loader
from yaml import SafeLoader

class UniqueKeyLoader(SafeLoader):
    pass


def construct_mapping(loader, node, deep=False):
    mapping = {}
    for key_node, value_node in node.value:
        key = loader.construct_object(key_node, deep=deep)
        if key in mapping:
            # key_node.start_mark gives (line, column) 0-based
            ln = key_node.start_mark.line + 1
            raise yaml.YAMLError(f"Duplicate key '{key}' found at line {ln}")
        mapping[key] = loader.construct_object(value_node, deep=deep)
    return mapping

UniqueKeyLoader.add_constructor(yaml.resolver.BaseResolver.DEFAULT_MAPPING_TAG, construct_mapping)

try:
    yaml.load(text, Loader=UniqueKeyLoader)
except Exception as e:
    errors.append(f'YAML parse error: {e}')

# Report
if warnings:
    print('WARNINGS:')
    for w in warnings:
        print('  -', w)

if errors:
    print('\nERRORS:')
    for e in errors:
        print('  -', e)
    sys.exit(3)

print('OK: azure-pipelines.yml parsed successfully with no issues found.')
sys.exit(0)

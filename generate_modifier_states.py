import json
import os
import re

with open(os.path.join('demofile-net', 'src', 'DemoFile.Game.Deadlock', 'Schema', '!GlobalTypes.json'), 'r') as f:
  global_types = json.load(f)

assert type(global_types) == type({})
assert set(global_types.keys()) == set(['enums', 'classes'])
assert type(global_types['enums']) == type({})
assert 'EModifierState' in global_types['enums']
assert type(global_types['enums']['EModifierState']) == type({})
assert set(global_types['enums']['EModifierState'].keys()) == set(['align', 'items'])
assert global_types['enums']['EModifierState']['align'] == 4  # probably don't need 32 bits since these are shift values, but whatever.
assert type(global_types['enums']['EModifierState']['items']) == type([])
assert len(global_types['enums']['EModifierState']['items']) > 2  # count and invalid
assert not [i for i in global_types['enums']['EModifierState']['items'] if type(i) != type({})]
assert not [i for i in global_types['enums']['EModifierState']['items'] if set(i.keys()) != set(['name', 'value'])]
assert not [i for i in range(len(global_types['enums']['EModifierState']['items']) - 1) if global_types['enums']['EModifierState']['items'][i]['value'] != i]
assert global_types['enums']['EModifierState']['items'][-1]['value'] == 255
assert not [i for i in global_types['enums']['EModifierState']['items'] if not re.match(r'^MODIFIER_STATE_[A-Z0-9_]+$', i['name'])], f"{len([i for i in global_types['enums']['EModifierState']['items'] if not re.match(r'^MODIFIER_STATE_[A-Z_]+$', i['name'])])}: {[i for i in global_types['enums']['EModifierState']['items'] if not re.match(r'^MODIFIER_STATE_[A-Z_]+$', i['name'])][:10]}"
assert global_types['enums']['EModifierState']['items'][-2]['name'] == 'MODIFIER_STATE_COUNT'
assert global_types['enums']['EModifierState']['items'][-1]['name'] == 'MODIFIER_STATE_INVALID'
items = [i['name'] for i in global_types['enums']['EModifierState']['items']]
assert len(items) == len(set(items))
items = [''.join([f"{part[0].upper()}{part[1:].lower()}" for part in i.split('_')[2:]]) for i in items]
assert len(items) == len(set(items))
print(f"public enum ModifierStateShift")
print(f"{{")
for i in range(len(items)):
  print(f"  {items[i]} = {i},")
print(f"}}")
print()
print(f"public enum ModifierStateIndex")
print(f"{{")
for i in items:
  print(f"  {i} = ModifierStateShift.{i} / 32,")
print(f"}}")
print()
print(f"public enum ModifierStateMask : uint")
print(f"{{")
for i in items:
  print(f"  {i} = 1u << (ModifierStateShift.{i} % 32),")
print(f"}}")

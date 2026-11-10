"""Primary Editor resistance categories; unknown raw bytes remain lossless.

These names apply to the four resistance type bytes. Do not reuse the historical
Creature.AttackType spell list for those fields. A 0..100 editor range is not a
proved runtime percentage formula, because the sampled damage paths never read it.
"""
import native_creature_resistance as physical

CATEGORIES = {
    0: 'NONE', 1: 'MELEE_BLADE', 2: 'MELEE_BLUNT', 3: 'MELEE_BODY',
    4: 'FIRE', 5: 'MOVEMENT', 6: 'LIGHTNING', 7: 'GAS', 8: 'PROJECTILE',
    9: 'ENHANCE_OTHER', 10: 'ENHANCE_SELF', 11: 'CLOSE_COMBAT',
    12: 'GENERATION', 13: 'PHYSICAL_TRAP', 14: 'NON_LETHAL_TRAP', 15: 'MELEE_SCYTHE'}


def view(raw):
    return [dict(item, category_name=CATEGORIES.get(item['attack_type']),
        editor_value_in_range=0 <= item['value_bits'] <= 100) for item in physical.view(raw)]


def patch(raw, slot, *, category=None, value=None):
    """Physical u8 edit; preserve opaque IDs/values rather than silently clamp."""
    return physical.patch(raw, slot, attack_type=category, value=value)


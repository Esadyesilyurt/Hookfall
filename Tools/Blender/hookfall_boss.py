# Hookfall - the stage boss and the portal it leaves behind (2026-11-08). The Abyss Colossus: a stone titan rising out
# of a floating rock, with a magenta crystal core in its chest, a horned head with a jaw that opens for its roar, two
# huge arms hinged at the shoulders and three rocks orbiting its base. The origin is the core, so the boss's position in
# the game is where the items aim. Faces -Y like the other models. The Portal: a low stone dais with a ring of blocks
# standing on it and a glowing Heartstone disc inside the ring; the origin is the dais bottom.
# Loaded by hookfall_models.py like hookfall_characters.py.
# AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
import math
import random

PALETTE_BOSS = {
    "M_BossStone": (0.30, 0.27, 0.34),
}


def install(ns):
    """Adds the boss palette and shares the main script's helpers."""
    ns["PALETTE"].update(PALETTE_BOSS)
    g = globals()
    for name in ("Builder", "M", "reset", "export", "preview", "empty"):
        g[name] = ns[name]


def _boulder(b, rnd, mat, t, s, seed, moss=False, amp=0.32):
    verts = b.ico(mat, M(t, s, (rnd.uniform(0, 40), rnd.uniform(0, 40), rnd.uniform(0, 90))))
    b.displace(verts, amp=min(s) * amp, freq=1.5 / min(s), seed=seed)
    if moss:
        b.bm.normal_update()
        moss_index = b.mat_index("M_Moss")
        for f in {f for v in verts for f in v.link_faces}:
            if f.normal.z > 0.75:
                f.material_index = moss_index
    return verts


def _crystal(b, t, length, width, tilt, spin):
    """A glowing crystal spike: a pointed cone and a short base, tilted away from the body."""
    b.cone("M_EnemyEye", M(t, (1, 1, 1), (tilt, 0, spin)) @ M((0, 0, length * 0.5)), width, 0.0, length, segments=5)
    b.cone("M_EnemyEye", M(t, (1, 1, 1), (tilt, 0, spin)) @ M((0, 0, -length * 0.1)), width * 0.75, width, length * 0.2, segments=5)


def boss(out_dir, prev_dir):
    """The Abyss Colossus, about 19 m tall from the bottom of its rock to the tips of its horns."""
    reset()
    rnd = random.Random(77)

    body = Builder("Body")
    # The floating rock it grows out of, with crystals hanging under it.
    _boulder(body, rnd, "M_BossStone", (0, 0, -7.2), (5.6, 5.0, 3.2), 1, moss=True)
    _boulder(body, rnd, "M_BossStone", (2.6, 1.6, -9.0), (2.4, 2.2, 1.8), 2)
    _boulder(body, rnd, "M_BossStone", (-2.8, -1.2, -9.4), (2.0, 2.0, 1.6), 3)
    for i in range(7):
        a = i / 7 * math.tau + 0.3
        _crystal(body, (math.cos(a) * 3.6, math.sin(a) * 3.2, -9.2 + rnd.uniform(-0.6, 0.6)), rnd.uniform(1.8, 3.2), 0.55,
                 180 - rnd.uniform(10, 35), math.degrees(a) + 90)
    # Waist, chest and shoulders: chunky boulders.
    _boulder(body, rnd, "M_BossStone", (0, 0.2, -3.4), (3.5, 2.9, 2.4), 4)
    _boulder(body, rnd, "M_BossStone", (0, 0.3, 0.6), (4.7, 3.4, 3.7), 5)
    for side in (-1, 1):
        _boulder(body, rnd, "M_BossStone", (side * 4.7, 0.3, 2.7), (2.3, 2.2, 2.0), 6 + side, moss=True)
        for k in range(3):
            _crystal(body, (side * (4.9 + k * 0.5), 0.6 - k * 0.6, 4.2 + k * 0.2), 1.6 + k * 0.4, 0.42, side * (25 + k * 10), 90)
    # A crest of crystals down the back.
    for k in range(5):
        _crystal(body, (0, 2.7, 3.4 - k * 1.3), 1.5 + (2 - abs(k - 2)) * 0.5, 0.45, -50, 0)
    # The setting of the core: a ring of dark stone around the hole in the chest.
    for i in range(10):
        a = i / 10 * math.tau
        body.cube("M_BossStone", M((math.cos(a) * 2.2, -3.15, 0.9 + math.sin(a) * 2.2), (0.7, 0.6, 0.7), (0, math.degrees(-a), 0)))
    body.build()

    # The core: an octahedral magenta crystal, its own object so it can pulse and turn.
    core = Builder("Core")
    core.cone("M_EnemyEye", M((0, 0, 0.95)), 1.6, 0.0, 1.9, segments=4)
    core.cone("M_EnemyEye", M((0, 0, -0.95), (1, 1, 1), (180, 0, 0)), 1.6, 0.0, 1.9, segments=4)
    core.build(location=(0, -3.2, 0.9))

    # The head on its own pivot at the neck, with a jaw hinged under it.
    head = Builder("Head")
    _boulder(head, rnd, "M_BossStone", (0, -0.6, 1.4), (1.9, 1.8, 1.6), 9, amp=0.2)
    head.cube("M_BossStone", M((0, -2.1, 1.9), (2.8, 0.7, 0.6)))                       # brow
    for side in (-1, 1):
        head.cube("M_EnemyEye", M((side * 0.65, -2.35, 1.55), (0.62, 0.12, 0.2)))        # eye slits
        # Horns in two segments that bend upwards.
        head.cone("M_BossStone", M((side * 1.6, -0.6, 2.2), (1, 1, 1), (0, side * 60, 0)) @ M((0, 0, 0.9)), 0.6, 0.33, 1.8, segments=6)
        head.cone("M_BossStone", M((side * 3.16, -0.6, 3.1), (1, 1, 1), (0, side * 20, 0)) @ M((0, 0, 0.95)), 0.33, 0.0, 1.9, segments=6)
    for k in range(5):
        head.cone("M_EnemyEye", M(((k - 2) * 0.55, -0.2 + abs(k - 2) * 0.2, 2.9), (1, 1, 1), ((k - 2) * 10, 0, 0)) @ M((0, 0, 0.6)),
                  0.22, 0.0, 1.2 + (2 - abs(k - 2)) * 0.4, segments=5)                    # crown
    head_ob = head.build(location=(0, -0.4, 4.3))

    jaw = Builder("Jaw")
    jaw.cube("M_BossStone", M((0, -1.4, -0.35), (2.2, 2.0, 0.7)))
    for k in range(4):
        jaw.cone("M_Fang", M(((k - 1.5) * 0.45, -2.15, 0.15)) @ M((0, 0, 0.25)), 0.14, 0.0, 0.5, segments=4)
    jaw_ob = jaw.build(location=(0, -0.6, 0.7))
    jaw_ob.parent = head_ob

    # Arms on pivots at the shoulders: upper arm, forearm and a huge fist with crystal knuckles.
    for side, name in ((-1, "Arm_L"), (1, "Arm_R")):
        arm = Builder(name)
        _boulder(arm, rnd, "M_BossStone", (side * 0.5, -0.2, -1.6), (1.4, 1.3, 1.9), 20 + side)
        _boulder(arm, rnd, "M_BossStone", (side * 0.8, -0.8, -4.2), (1.3, 1.2, 1.8), 22 + side)
        _boulder(arm, rnd, "M_BossStone", (side * 0.9, -1.2, -6.6), (1.9, 1.8, 1.7), 24 + side, moss=True)
        for k in range(3):
            _crystal(arm, (side * 0.9 + (k - 1) * 0.8, -2.7, -6.6), 0.9, 0.3, 90, 0)
        _crystal(arm, (side * 1.6, 0.3, -1.0), 1.4, 0.35, side * 60, 90)
        arm.build(location=(side * 5.4, 0.2, 2.8))

    # Rocks orbiting the base (the game turns them around the body).
    for i in range(3):
        a = i / 3 * math.tau
        rock = Builder(f"Orbit_{i + 1}")
        _boulder(rock, rnd, "M_BossStone", (0, 0, 0), (1.1, 1.0, 0.9), 30 + i, moss=True)
        _crystal(rock, (0, 0, -0.6), 1.0, 0.3, 180, 0)
        rock.build(location=(math.cos(a) * 8.0, math.sin(a) * 8.0, -6.5 + i * 0.8))

    export("Boss", out_dir)
    preview("Boss", prev_dir)


def portal(out_dir, prev_dir):
    """The way to the next stage: a dais, a ring of stone blocks and a Heartstone disc, about 7 m tall."""
    reset()
    dais = Builder("Dais")
    dais.cone("M_Stone", M((0, 0, 0.15)), 4.2, 4.0, 0.3, segments=28)
    dais.cone("M_Tower", M((0, 0, 0.42)), 3.4, 3.3, 0.25, segments=28)
    for i in range(12):
        a = i / 12 * math.tau
        dais.cube("M_Heartstone", M((math.cos(a) * 3.7, math.sin(a) * 3.7, 0.31), (0.5, 0.12, 0.04), (0, 0, math.degrees(a) + 90)))
    dais.build()

    ring = Builder("Ring")
    centre_z, radius = 3.6, 2.95
    blocks = 16
    for i in range(blocks):
        a = i / blocks * math.tau
        x, z = math.cos(a) * radius, centre_z + math.sin(a) * radius
        mat = "M_Heartstone" if i == 4 else ("M_Tower" if i % 2 == 0 else "M_TowerDark")
        ring.cube(mat, M((x, 0, z), (0.62 if i != 4 else 0.7, 0.75, 1.12), (0, -math.degrees(a), 0)))
    for side in (-1, 1):
        ring.cube("M_Tower", M((side * 3.45, 0, 1.35), (0.7, 0.9, 1.9)))
        ring.cube("M_TowerDark", M((side * 3.45, 0, 2.45), (0.85, 1.0, 0.3)))
    ring.build()

    disc = Builder("Disc")
    disc.cone("M_Heartstone", M((0, 0, 0), (1, 1, 1), (90, 0, 0)), 2.6, 2.6, 0.08, segments=32)
    disc.build(location=(0, 0, centre_z))

    export("Portal", out_dir)
    preview("Portal", prev_dir)

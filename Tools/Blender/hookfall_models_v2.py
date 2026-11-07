# Hookfall - second pass on the models (2026-11-07, after the first playtest found them too plain): a detailed tower,
# richer creatures and Warden, more props, and the UI icons rendered as sprites. Loaded by hookfall_models.py, whose
# helpers (Builder, M, reset, export, preview, empty, material, PALETTE) it uses; definitions here replace the
# first-pass ones with the same name.
# AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
import bpy, bmesh, math, random, os
from mathutils import Vector, Matrix

PALETTE_V2 = {
    "M_Banner": (0.15, 0.62, 0.70), "M_BatBelly": (0.78, 0.36, 0.70), "M_Fang": (0.95, 0.93, 0.85),
    "M_Bush": (0.36, 0.55, 0.32), "M_Chain": (0.30, 0.31, 0.34),
    "M_IconRed": (0.92, 0.28, 0.32), "M_IconSteel": (0.74, 0.78, 0.84), "M_IconGreen": (0.45, 0.85, 0.42),
    "M_IconWhite": (0.96, 0.96, 0.98), "M_IconDark": (0.18, 0.20, 0.26),
}


def install(ns):
    """Adds the v2 palette and replaces the first-pass model functions in the main script's namespace."""
    ns["PALETTE"].update(PALETTE_V2)
    g = globals()
    for name in ("Builder", "M", "reset", "export", "preview", "empty", "material", "PALETTE"):
        g[name] = ns[name]
    for name in ("tower", "bat", "wyvern", "golem", "warden", "bush", "lantern", "statue", "icons"):
        ns[name] = g[name]


def torus(b, mat, matrix, R, r, segments=24, sides=6, arc=math.tau):
    """A torus (or an arc of one) in the XZ plane, facing -Y."""
    bm = b.bm
    rings = []
    closed = arc >= math.tau - 0.001
    count = segments if closed else segments + 1
    for i in range(count):
        a = arc * i / segments
        centre = Vector((math.cos(a) * R, 0, math.sin(a) * R))
        out = centre.normalized()
        ring = []
        for j in range(sides):
            t = math.tau * j / sides
            p = centre + out * (math.cos(t) * r) + Vector((0, math.sin(t) * r, 0))
            ring.append(bm.verts.new(matrix @ p))
        rings.append(ring)
    idx = b.mat_index(mat)
    for i in range(len(rings) - (0 if closed else 1)):
        a, c = rings[i], rings[(i + 1) % len(rings)]
        for j in range(sides):
            k = (j + 1) % sides
            bm.faces.new((a[j], c[j], c[k], a[k])).material_index = idx


def chain(b, start, end, link=0.45, thickness=0.09):
    """Heavy chain from start to end: alternating flat links."""
    start, end = Vector(start), Vector(end)
    d = end - start
    n = max(2, int(d.length / link))
    rot = d.to_track_quat('Z', 'Y').to_matrix().to_4x4()
    for i in range(n):
        p = start + d * ((i + 0.5) / n)
        twist = Matrix.Rotation(math.radians(90 * (i % 2)), 4, 'Z')
        b.cube("M_Chain", Matrix.Translation(p) @ rot @ twist @ Matrix.Diagonal((thickness * 2.2, thickness, link * 1.1, 1)))


# ---------------------------------------------------------------- tower

def tower(out_dir, prev_dir):
    """Round stone archer tower (mood board 4): stepped plinth, buttresses, block courses, framed windows,
    corbels, a wooden deck with braziers and a flag, cyan banners of the Order, chains that tether it to the
    island, and the Heartstone glowing in a framed arch at its foot (mood board 5)."""
    reset()
    b = Builder("Tower")
    h = 18.0
    b.cone("M_Tower", M((0, 0, 0.35)), 4.3, 4.1, 0.7, segments=32)
    b.cone("M_Tower", M((0, 0, 1.0)), 3.8, 3.6, 0.7, segments=32)
    b.cone("M_Tower", M((0, 0, 1.2 + (h - 2.2) / 2)), 3.2, 2.95, h - 2.2, segments=32)
    for k in range(10):   # block courses
        z = 2.2 + k * 1.55
        r = 3.2 - (z - 1.2) / (h - 2.2) * 0.25
        b.cone("M_Tower", M((0, 0, z)), r + 0.06, r + 0.06, 0.18, segments=32)
    for z in (6.0, 11.5):
        r = 3.25 - (z - 1.2) / (h - 2.2) * 0.25
        b.cone("M_Tower", M((0, 0, z)), r + 0.12, r + 0.1, 0.4, segments=32)
    for i in range(4):   # buttresses
        a = math.radians(45 + i * 90)
        b.cube("M_Tower", M((math.cos(a) * 3.4, math.sin(a) * 3.4, 2.2), (0.9, 1.0, 4.4), (0, 0, math.degrees(a))))
        b.cube("M_Tower", M((math.cos(a) * 3.65, math.sin(a) * 3.65, 0.9), (0.9, 1.4, 1.8), (0, 0, math.degrees(a))))
    for i in range(20):   # corbels under the top
        a = i / 20 * math.tau
        b.cube("M_Tower", M((math.cos(a) * 3.15, math.sin(a) * 3.15, h - 1.25), (0.35, 0.5, 0.5), (0, 0, math.degrees(a))))
    b.cone("M_Tower", M((0, 0, h - 0.5)), 3.05, 3.5, 1.0, segments=32)
    for i in range(16):   # parapet and merlons
        a = i / 16 * math.tau
        b.cube("M_Tower", M((math.cos(a) * 3.3, math.sin(a) * 3.3, h + 0.35), (0.62, 0.38, 0.7), (0, 0, math.degrees(a) + 90)))
        if i % 2 == 0:
            b.cube("M_Tower", M((math.cos(a) * 3.3, math.sin(a) * 3.3, h + 1.05), (0.66, 0.44, 0.7), (0, 0, math.degrees(a) + 90)))
    for i in range(12):   # deck planks
        x = -2.75 + i * 0.5
        length = 2 * math.sqrt(max(0.0, 3.05 ** 2 - x * x))
        b.cube("M_Wood", M((x, 0, h - 0.02), (0.46, length, 0.1)))
    for z, offset in ((8.5, 0), (13.5, 30), (16.0, 60)):   # framed slit windows
        for k in range(3):
            a = math.radians(offset + k * 120 + 60)
            r = 3.18 - (z - 1.2) / (h - 2.2) * 0.25
            rot = math.degrees(a) + 90
            b.cube("M_Tower", M((math.cos(a) * r, math.sin(a) * r, z), (0.55, 0.3, 1.7), (0, 0, rot)))
            b.cube("M_TowerDark", M((math.cos(a) * (r + 0.08), math.sin(a) * (r + 0.08), z), (0.25, 0.3, 1.3), (0, 0, rot)))
    # Arch at the front (-Y) with a stone frame.
    b.cube("M_TowerDark", M((0, -3.12, 2.6), (2.2, 0.3, 2.8)))
    b.cone("M_TowerDark", M((0, -3.12, 4.0), r=(90, 0, 0)), 1.1, 1.1, 0.3, segments=16)
    torus(b, "M_Tower", M((0, -3.3, 4.0)), 1.25, 0.18, segments=12, sides=5, arc=math.pi)
    for x in (-1.25, 1.25):
        b.cube("M_Tower", M((x, -3.3, 2.5), (0.36, 0.36, 3.0)))
    # Braziers on the deck.
    for x in (-2.2, 2.2):
        b.cone("M_Chain", M((x, 1.4, h + 0.35)), 0.12, 0.12, 0.7, segments=8)
        b.cone("M_Chain", M((x, 1.4, h + 0.8)), 0.15, 0.4, 0.3, segments=10)
        b.cone("M_Heartstone", M((x, 1.4, h + 1.15)), 0.3, 0.0, 0.6, segments=6)
    b.cone("M_Wood", M((2.4, -1.6, h + 2.0)), 0.07, 0.06, 4.0, segments=6)   # flag pole
    b.build()

    cloth = Builder("Banners")
    for a in (0, 180):
        rad = math.radians(a)
        r = 3.15
        base = Vector((math.cos(rad) * r, math.sin(rad) * r, h - 2.0))
        out = Vector((math.cos(rad), math.sin(rad), 0))
        tangent = Vector((-math.sin(rad), math.cos(rad), 0))
        pts = [base + tangent * 0.7, base - tangent * 0.7, base - tangent * 0.7 + Vector((0, 0, -4.2)),
               base + Vector((0, 0, -4.8)), base + tangent * 0.7 + Vector((0, 0, -4.2))]
        cloth.membrane("M_Banner", [p + out * 0.05 for p in pts], thickness=0.04)
        cloth.cone("M_Heartstone", M(tuple(base + out * 0.12 + Vector((0, 0, -1.8))), (1, 0.3, 1.4), (0, 0, a + 90)) @ M(r=(0, 0, 45)), 0.35, 0.35, 0.1, segments=4)
    flag = [(2.45, -1.6, h + 3.9), (2.45, -1.6, h + 3.0), (3.9, -1.6, h + 3.3), (3.6, -1.6, h + 3.6), (3.95, -1.6, h + 3.9)]
    cloth.membrane("M_Banner", flag, thickness=0.03)
    cloth.build()

    links = Builder("Chains")
    for a in (35, 155, 275):
        rad = math.radians(a)
        chain(links, (math.cos(rad) * 3.1, math.sin(rad) * 3.1, 12.5), (math.cos(rad) * 9.0, math.sin(rad) * 9.0, 0.3))
        links.cube("M_Tower", M((math.cos(rad) * 9.0, math.sin(rad) * 9.0, 0.3), (0.9, 0.9, 0.6), (0, 0, a)))
    links.build()

    heart = Builder("Heartstone")
    heart.cone("M_Heartstone", M((0, 0, 0.55)), 0.45, 0.0, 1.1, segments=6)
    heart.cone("M_Heartstone", M((0, 0, -0.35), r=(180, 0, 0)), 0.45, 0.0, 0.7, segments=6)
    for i in range(5):
        a = i / 5 * math.tau
        heart.cone("M_Crystal", M((math.cos(a) * 0.75, math.sin(a) * 0.35, -0.6 + (i % 2) * 0.3), r=(0, 25, math.degrees(a))), 0.12, 0.0, 0.5, segments=6)
    torus(heart, "M_Chain", M((0, 0, 0.1), r=(0, 0, 0)), 0.75, 0.04, segments=20, sides=4)
    heart.build(location=(0, -3.6, 2.4))
    empty("TowerTop", (0, 0, h))
    export("Tower", out_dir)
    preview("Tower", prev_dir)


# ---------------------------------------------------------------- creatures

def bat(out_dir, prev_dir):
    """Horde bat (mood board 7): round furry body with a pink belly, big ears, fangs, glowing eyes,
    clawed wings with finger bones. Faces -Y; wings are separate objects for flapping."""
    reset()
    b = Builder("Body")
    b.sphere("M_Bat", M((0, 0, 0), (0.3, 0.36, 0.28)), u=12, v=8)
    b.sphere("M_BatBelly", M((0, -0.08, -0.08), (0.22, 0.26, 0.18)), u=10, v=6)
    b.sphere("M_Bat", M((0, -0.32, 0.08), (0.21, 0.2, 0.19)), u=12, v=8)
    b.sphere("M_Bat", M((0, 0.05, 0.22), (0.16, 0.2, 0.08)), u=8, v=5)   # fur tuft
    for side in (-1, 1):
        b.cone("M_Bat", M((side * 0.12, -0.28, 0.34), r=(-12, side * 24, 0)), 0.09, 0.0, 0.36, segments=6)
        b.cone("M_BatBelly", M((side * 0.12, -0.31, 0.32), r=(-12, side * 24, 0)), 0.05, 0.0, 0.26, segments=5)
        b.sphere("M_EnemyEye", M((side * 0.085, -0.49, 0.13), (0.05, 0.03, 0.05)), u=6, v=4)
        b.cone("M_Fang", M((side * 0.05, -0.5, -0.02), r=(180, 0, 0)), 0.025, 0.0, 0.08, segments=4)
        b.cone("M_Bat", M((side * 0.1, 0.05, -0.3), r=(180, 0, 0)), 0.04, 0.0, 0.14, segments=4)   # feet
    b.cone("M_Bat", M((0, 0.32, -0.05), r=(-80, 0, 0)), 0.1, 0.0, 0.25, segments=5)
    b.build()
    for side, name in ((-1, "Wing_L"), (1, "Wing_R")):
        w = Builder(name)
        pts = [(0, 0, 0.05), (side * 0.45, 0, 0.2), (side * 1.0, 0, 0.14), (side * 0.8, 0, -0.08),
               (side * 0.6, 0, -0.02), (side * 0.42, 0, -0.16), (side * 0.22, 0, -0.07), (0, 0, -0.1)]
        w.membrane("M_Bat", [(x, y - 0.02, z) for x, y, z in pts], thickness=0.025)
        for tip in ((side * 1.0, 0.14), (side * 0.8, -0.08), (side * 0.42, -0.16)):   # finger bones
            d = Vector((tip[0] - side * 0.45, 0, tip[1] - 0.18))
            mid = Vector((side * 0.45, -0.04, 0.18)) + d * 0.5
            w.cone("M_BatBelly", Matrix.Translation(mid) @ d.to_track_quat('Z', 'Y').to_matrix().to_4x4(), 0.018, 0.012, d.length, segments=4)
        w.cone("M_Fang", M((side * 0.45, -0.03, 0.26)), 0.03, 0.0, 0.12, segments=4)   # wrist claw
        w.build(location=(side * 0.22, 0.02, 0.06))
    export("Bat", out_dir)
    preview("Bat", prev_dir)


def wyvern(out_dir, prev_dir):
    """Heavy flyer (mood board 8): lean body, long neck, horned head with a jaw, back spines, spiked tail,
    big wings with finger bones and wrist claws. Purple with magenta belly. Faces -Y."""
    reset()
    b = Builder("Body")
    b.sphere("M_Wyvern", M((0, 0, 0), (0.55, 1.1, 0.5)), u=16, v=10)
    b.sphere("M_WyvernBelly", M((0, -0.1, -0.18), (0.42, 0.9, 0.32)), u=12, v=7)
    b.cone("M_Wyvern", M((0, -1.15, 0.25), r=(-70, 0, 0)), 0.32, 0.2, 1.0, segments=10)
    b.sphere("M_Wyvern", M((0, -1.75, 0.45), (0.3, 0.42, 0.26)), u=12, v=8)
    b.cone("M_Wyvern", M((0, -2.2, 0.42), r=(-90, 0, 0)), 0.18, 0.05, 0.5, segments=8)          # snout
    b.cone("M_WyvernBelly", M((0, -2.08, 0.28), (1, 1, 0.5), (-95, 0, 0)), 0.15, 0.04, 0.55, segments=8)  # jaw
    for side in (-1, 1):
        b.cone("M_WyvernBelly", M((side * 0.16, -1.6, 0.72), r=(30, side * 25, 0)), 0.08, 0.0, 0.55, segments=6)
        b.cone("M_WyvernBelly", M((side * 0.24, -1.7, 0.6), r=(60, side * 50, 0)), 0.05, 0.0, 0.3, segments=5)
        b.sphere("M_EnemyEye", M((side * 0.17, -1.98, 0.54), (0.07, 0.05, 0.05)), u=6, v=4)
        b.cone("M_Fang", M((side * 0.08, -2.3, 0.28), r=(180, 0, 0)), 0.03, 0.0, 0.1, segments=4)
        b.cone("M_Wyvern", M((side * 0.3, -0.3, -0.55), r=(10, 0, 0)), 0.1, 0.06, 0.5, segments=6)
        for k in (-1, 0, 1):
            b.cone("M_Fang", M((side * 0.3 + k * 0.06, -0.42, -0.85), r=(-60, 0, 0)), 0.03, 0.0, 0.2, segments=4)
    for i in range(9):   # back spines along neck, body and tail
        y = -1.5 + i * 0.55
        z = 0.5 - max(0, y) * 0.12 if y < 1.0 else 0.25 - (y - 1.0) * 0.25
        b.cone("M_WyvernBelly", M((0, y, z + (0.45 if -1 < y < 1 else 0.25)), r=(20, 0, 0)), 0.09, 0.0, 0.32, segments=4)
    for i in range(5):
        t = i / 5
        b.cone("M_Wyvern", M((0, 1.0 + i * 0.55, -0.05 - t * t * 0.6), r=(-90 + t * 20, 0, 0)), 0.3 * (1 - t) + 0.05, 0.3 * (1 - t - 0.2) + 0.04, 0.6, segments=8)
    b.cone("M_WyvernBelly", M((0, 3.85, -0.62), (1, 1, 0.3), (-90, 0, 0)), 0.32, 0.0, 0.5, segments=4)
    b.build()
    for side, name in ((-1, "Wing_L"), (1, "Wing_R")):
        w = Builder(name)
        w.cone("M_Wyvern", M((side * 1.0, 0, 0.1), r=(0, 90, side * 6)), 0.09, 0.05, 2.0, segments=6)
        pts = [(0, 0, 0.12), (side * 1.9, 0, 0.32), (side * 3.4, 0, 0.1), (side * 2.9, 0, -0.45),
               (side * 2.2, 0, -0.3), (side * 1.7, 0, -0.85), (side * 1.0, 0, -0.55), (side * 0.4, 0, -0.9), (0, 0, -0.6)]
        w.membrane("M_WyvernBelly", pts, thickness=0.04)
        for tip in ((side * 3.4, 0.1), (side * 2.9, -0.45), (side * 1.7, -0.85)):
            d = Vector((tip[0] - side * 1.9, 0, tip[1] - 0.32))
            mid = Vector((side * 1.9, -0.05, 0.32)) + d * 0.5
            w.cone("M_Wyvern", Matrix.Translation(mid) @ d.to_track_quat('Z', 'Y').to_matrix().to_4x4(), 0.05, 0.03, d.length, segments=5)
        w.cone("M_Fang", M((side * 1.9, -0.05, 0.45)), 0.06, 0.0, 0.25, segments=4)
        w.build(location=(side * 0.42, -0.35, 0.3))
    export("Wyvern", out_dir)
    preview("Wyvern", prev_dir)


def golem(out_dir, prev_dir):
    """Fallen guardian (mood board 6): stacked mossy boulders, magenta crystals growing from its back and shoulders,
    glowing rune lines on the chest, heavy fists. About 3.4 m tall, faces -Y."""
    reset()
    rnd = random.Random(6)
    b = Builder("Body")

    def boulder(t, s, seed, mossy=True):
        verts = b.ico("M_Golem", M(t, s, (rnd.uniform(0, 40), rnd.uniform(0, 40), 0)))
        b.displace(verts, amp=min(s) * 0.35, freq=1.6 / min(s), seed=seed)
        if mossy:
            b.bm.normal_update()
            moss = b.mat_index("M_Moss")
            for f in {f for v in verts for f in v.link_faces}:
                if f.normal.z > 0.6:
                    f.material_index = moss

    for side in (-1, 1):
        boulder((side * 0.5, 0, 0.45), (0.5, 0.5, 0.55), 10 + side, mossy=False)
    boulder((0, 0, 1.2), (0.85, 0.65, 0.5), 3)
    boulder((0, 0, 2.15), (1.15, 0.85, 0.85), 4)
    boulder((0, -0.15, 3.15), (0.5, 0.45, 0.42), 5)
    for side in (-1, 1):
        b.cube("M_EnemyEye", M((side * 0.18, -0.55, 3.2), (0.16, 0.05, 0.09)))
    for i in range(6):   # crystals on the back and shoulders
        a = rnd.uniform(-60, 60)
        x = rnd.uniform(-0.8, 0.8)
        z = rnd.uniform(2.2, 2.9)
        b.cone("M_EnemyEye", M((x, 0.55, z), r=(-50 + rnd.uniform(-15, 15), a * 0.3, 0)) @ M((0, 0, 0.3)), 0.12, 0.0, rnd.uniform(0.5, 0.9), segments=5)
    for z, w in ((2.35, 0.6), (2.05, 0.4)):   # rune lines
        b.cube("M_EnemyEye", M((0, -0.82, z), (w, 0.05, 0.05)))
    b.cube("M_EnemyEye", M((0, -0.82, 2.2), (0.05, 0.05, 0.35)))
    b.build()
    for side, name in ((-1, "Arm_L"), (1, "Arm_R")):
        a = Builder(name)
        for i, (z, s) in enumerate(((-0.25, 0.42), (-0.85, 0.36), (-1.45, 0.55))):
            verts = a.ico("M_Golem", M((side * 0.1, 0, z), (s, s, s * 1.1), (rnd.uniform(0, 40), 0, 0)))
            a.displace(verts, amp=s * 0.3, freq=1.6 / s, seed=20 + i + side)
        a.cone("M_EnemyEye", M((side * 0.35, 0.15, -0.1), r=(0, side * 50, 0)), 0.08, 0.0, 0.45, segments=5)
        a.build(location=(side * 1.3, 0, 2.55))
    export("Golem", out_dir)
    preview("Golem", prev_dir)


# ---------------------------------------------------------------- the Warden

def warden(out_dir, prev_dir):
    """The Warden: hooded warrior of the Order, cyan scarf and sigil, belt with pouches, layered armour, and the
    hook gauntlet wound with a glowing tether coil. 1.8 m, feet at 0, faces -Y.
    Parts: Body, Head (neck pivot), Arm_L/Arm_R (shoulders), Leg_L/Leg_R (hips), Cape and Scarf (neck)."""
    reset()
    body = Builder("Body")
    body.cone("M_WardenCloth", M((0, 0, 1.22)), 0.2, 0.25, 0.5, segments=14)
    body.cone("M_WardenArmor", M((0, -0.02, 1.27)), 0.215, 0.23, 0.28, segments=14)        # breastplate
    body.cone("M_WardenArmor", M((0, 0, 0.98)), 0.22, 0.22, 0.09, segments=14)               # belt
    body.cube("M_Gold", M((0, -0.22, 0.98), (0.08, 0.03, 0.07)))                             # buckle
    for x in (-0.15, 0.13):
        body.cube("M_Wood", M((x, -0.17, 0.92), (0.09, 0.07, 0.1), (0, 0, x * 60)))         # pouches
    body.cone("M_WardenCloth", M((0, 0, 0.72)), 0.3, 0.19, 0.48, segments=14)
    body.cone("M_WardenCape", M((0, 0, 0.5)), 0.31, 0.3, 0.04, segments=14)                  # hem
    for side in (-1, 1):
        body.sphere("M_WardenArmor", M((side * 0.27, 0, 1.43), (0.15, 0.15, 0.11)), u=10, v=6)
        body.sphere("M_WardenArmor", M((side * 0.27, 0, 1.36), (0.16, 0.16, 0.06)), u=10, v=4)
    body.cube("M_Wood", M((0.02, -0.05, 1.2), (0.05, 0.42, 0.04), (0, 30, 0)))               # strap
    body.cone("M_PlayerGlow", M((0, -0.25, 1.25), (1, 0.3, 1), (90, 0, 45)), 0.07, 0.07, 0.04, segments=4)
    body.build()

    head = Builder("Head")
    head.sphere("M_WardenSkin", M((0, -0.02, 0.13), (0.11, 0.11, 0.13)), u=10, v=7)
    head.sphere("M_WardenCloth", M((0, 0.02, 0.16), (0.15, 0.15, 0.16)), u=12, v=8)
    head.cone("M_WardenCloth", M((0, 0.14, 0.22), r=(-60, 0, 0)), 0.1, 0.0, 0.22, segments=8)
    for v in head.bm.verts:
        if v.co.y < -0.06 and v.co.z > 0.05 and abs(v.co.x) < 0.1 and v.co.z < 0.24:
            v.co.y = -0.06
    torus(head, "M_PlayerGlow", M((0, -0.07, 0.15), (1.0, 1.0, 1.15)), 0.1, 0.012, segments=16, sides=4, arc=math.pi)  # hood trim
    head.build(location=(0, 0, 1.52))

    scarf = Builder("Scarf")
    scarf.cone("M_Banner", M((0, 0, 0)), 0.14, 0.15, 0.08, segments=12)
    scarf.membrane("M_Banner", [(-0.06, 0.13, 0.0), (0.06, 0.13, 0.0), (0.09, 0.15, -0.55), (-0.02, 0.15, -0.62)], thickness=0.02)
    scarf.build(location=(0, 0, 1.5))

    for side, name in ((-1, "Arm_L"), (1, "Arm_R")):
        arm = Builder(name)
        arm.cone("M_WardenCloth", M((0, 0, -0.17)), 0.06, 0.055, 0.34, segments=8)
        arm.cone("M_WardenArmor", M((0, 0, -0.45)), 0.058, 0.052, 0.28, segments=8)
        arm.sphere("M_WardenSkin", M((0, 0, -0.63), (0.055, 0.055, 0.06)), u=8, v=5)
        if side == 1:   # the hook gauntlet with its tether coil
            arm.cube("M_WardenArmor", M((0, 0, -0.45), (0.14, 0.14, 0.24)))
            for k in range(3):
                arm.cone("M_PlayerGlow", M((0, 0, -0.38 - k * 0.07)), 0.085, 0.085, 0.025, segments=10)
            arm.cone("M_PlayerGlow", M((0, -0.08, -0.52), r=(90, 0, 0)), 0.04, 0.02, 0.12, segments=6)
        else:
            arm.cone("M_WardenArmor", M((0, 0, -0.5)), 0.065, 0.06, 0.12, segments=8)        # bracer
        arm.build(location=(side * 0.27, 0, 1.4))

    for side, name in ((-1, "Leg_L"), (1, "Leg_R")):
        leg = Builder(name)
        leg.cone("M_WardenCloth", M((0, 0, -0.22)), 0.085, 0.07, 0.44, segments=8)
        leg.cone("M_WardenArmor", M((0, 0, -0.62)), 0.07, 0.066, 0.36, segments=8)
        leg.cone("M_WardenArmor", M((0, 0, -0.45)), 0.08, 0.08, 0.06, segments=8)             # boot cuff
        leg.cube("M_WardenArmor", M((0, -0.04, -0.85), (0.12, 0.23, 0.1)))
        leg.sphere("M_WardenArmor", M((0, -0.02, -0.4), (0.06, 0.05, 0.05)), u=6, v=4)       # knee
        leg.build(location=(side * 0.11, 0, 0.9))

    cape = Builder("Cape")
    cape.membrane("M_WardenCape", [(-0.2, 0, 0), (0.2, 0, 0), (0.29, 0, -0.95), (0.12, 0, -1.02), (0.0, 0, -0.94), (-0.12, 0, -1.02), (-0.29, 0, -0.95)], thickness=0.02)
    cape.cone("M_PlayerGlow", M((0, 0.02, -0.35), (1, 0.3, 1.3), (90, 0, 45)), 0.07, 0.07, 0.02, segments=4)
    cape.build(location=(0, 0.2, 1.45))
    export("Warden", out_dir)
    preview("Warden", prev_dir)


# ---------------------------------------------------------------- props

def bush(out_dir, prev_dir):
    reset()
    rnd = random.Random(51)
    b = Builder("Bush")
    for i in range(6):
        r = rnd.uniform(0.35, 0.6)
        verts = b.ico("M_Bush", M((rnd.uniform(-0.5, 0.5), rnd.uniform(-0.5, 0.5), r * 0.8), (r, r, r * 0.85)))
        b.displace(verts, amp=0.08, freq=3, seed=i, chunky=False)
    for i in range(3):
        b.sphere("M_Flower", M((rnd.uniform(-0.5, 0.5), rnd.uniform(-0.5, 0.5), rnd.uniform(0.8, 1.1)), (0.08, 0.08, 0.06)), u=6, v=4)
    b.build()
    export("Bush", out_dir)
    preview("Bush", prev_dir)


def lantern(out_dir, prev_dir):
    """Stone lantern of the Order, still burning with Heartstone light."""
    reset()
    b = Builder("Lantern")
    b.cube("M_Tower", M((0, 0, 0.15), (0.7, 0.7, 0.3)))
    b.cone("M_Tower", M((0, 0, 0.8)), 0.16, 0.14, 1.0, segments=8)
    b.cube("M_Tower", M((0, 0, 1.35), (0.55, 0.55, 0.12)))
    for x, y in ((-0.2, -0.2), (0.2, -0.2), (-0.2, 0.2), (0.2, 0.2)):
        b.cube("M_Tower", M((x, y, 1.6), (0.08, 0.08, 0.4)))
    b.cone("M_Heartstone", M((0, 0, 1.6)), 0.13, 0.0, 0.35, segments=6)
    b.cone("M_Tower", M((0, 0, 1.95)), 0.42, 0.05, 0.35, segments=4)
    b.build()
    export("Lantern", out_dir)
    preview("Lantern", prev_dir)


def statue(out_dir, prev_dir):
    """Broken statue of an old Warden: plinth, legs, a torso with a hooded head fallen beside it."""
    reset()
    b = Builder("Statue")
    b.cube("M_Tower", M((0, 0, 0.3), (1.2, 1.2, 0.6)))
    for x in (-0.18, 0.18):
        b.cone("M_Tower", M((x, 0, 1.15)), 0.17, 0.14, 1.1, segments=8)
    verts = b.cone("M_Tower", M((0, 0, 2.1)), 0.42, 0.32, 0.9, segments=10)
    for v in verts:
        if v.co.z > 2.4:
            v.co.z -= abs(v.co.x) * 0.8   # broken shoulders
    b.sphere("M_Tower", M((0.9, 0.5, 0.25), (0.3, 0.3, 0.33)), u=10, v=6)
    b.cone("M_Moss", M((0, 0, 0.62)), 0.62, 0.55, 0.06, segments=12)
    b.build()
    export("Statue", out_dir)
    preview("Statue", prev_dir)


# ---------------------------------------------------------------- UI icons

def _render_icon(name, out_dir, size=256):
    scene = bpy.context.scene
    objs = [o for o in scene.objects if o.type == 'MESH']
    lo = Vector((min(min((o.matrix_world @ Vector(c))[i] for c in o.bound_box) for o in objs) for i in range(3)))
    hi = Vector((max(max((o.matrix_world @ Vector(c))[i] for c in o.bound_box) for o in objs) for i in range(3)))
    centre = (lo + hi) / 2
    extent = max(hi.x - lo.x, hi.z - lo.z)
    cam_data = bpy.data.cameras.new("IconCam")
    cam_data.type = 'ORTHO'
    cam_data.ortho_scale = extent * 1.25
    cam = bpy.data.objects.new("IconCam", cam_data)
    scene.collection.objects.link(cam)
    cam.location = centre + Vector((0, -10, 0))
    cam.rotation_euler = (math.radians(90), 0, 0)
    scene.camera = cam
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.display.shading.light = 'FLAT'   # full-strength palette colours, like the toon shader's lit band
    scene.display.shading.color_type = 'MATERIAL'
    scene.display.shading.show_cavity = True
    scene.display.shading.cavity_type = 'BOTH'
    scene.display.shading.cavity_ridge_factor = 1.0
    scene.display.shading.cavity_valley_factor = 1.5
    scene.display.shading.show_object_outline = True
    scene.display.shading.object_outline_color = (0.08, 0.07, 0.11)
    scene.view_settings.view_transform = 'Standard'   # palette colours as they are, not tone-mapped
    scene.render.film_transparent = True
    scene.render.image_settings.color_mode = 'RGBA'
    scene.render.resolution_x = scene.render.resolution_y = size
    scene.render.filepath = os.path.join(out_dir, "ICON_" + name + ".png")
    bpy.ops.render.render(write_still=True)
    print(f"[Hookfall] icon {name}")


def _heart(b, mat, t=(0, 0, 0), s=1.0):
    for x in (-0.28, 0.28):
        b.sphere(mat, M((t[0] + x * s, t[1], t[2] + 0.18 * s), (0.36 * s, 0.25 * s, 0.36 * s)), u=14, v=8)
    b.cube(mat, M((t[0], t[1], t[2] - 0.12 * s), (0.62 * s, 0.4 * s, 0.62 * s), (0, 45, 0)))


def _arrow(b, mat, start, end, width=0.08):
    start, end = Vector(start), Vector(end)
    d = end - start
    rot = d.to_track_quat('Z', 'Y').to_matrix().to_4x4()
    shaft = d * 0.75
    b.cone(mat, Matrix.Translation(start + shaft * 0.5) @ rot, width, width, shaft.length, segments=6)
    b.cone(mat, Matrix.Translation(start + d * 0.87) @ rot, width * 2.6, 0.0, d.length * 0.26, segments=6)


def _hook(b, mat, t=(0, 0, 0), s=1.0):
    b.cone(mat, M((t[0], t[1], t[2] + 0.35 * s)), 0.07 * s, 0.07 * s, 0.8 * s, segments=8)
    # The arc starts at the foot of the shaft and curls under and up the far side.
    torus(b, mat, M((t[0] + 0.25 * s, t[1], t[2] - 0.05 * s), r=(0, 180, 0)), 0.25 * s, 0.07 * s, segments=12, sides=6, arc=math.pi * 1.25)


def icons(out_dir, prev_dir):
    """Renders every UI icon (items, stats, HUD) as a 256 px sprite with a transparent background, in the game's
    palette: cyan for the player's items, warm for rewards, red for health (GDD 8.1, mood board 9)."""
    os.makedirs(out_dir, exist_ok=True)

    def make(name, build):
        reset()
        b = Builder("Icon")
        build(b)
        b.build()
        _render_icon(name, out_dir)

    def orbit(b):
        torus(b, "M_IconSteel", M(), 0.7, 0.04, segments=32, sides=5)
        for i in range(3):
            a = i / 3 * 360
            b.cube("M_PlayerGlow", M(r=(0, a, 0)) @ M((0.7, 0, 0), (0.12, 0.08, 0.6), (0, 30, 0)))
        b.sphere("M_IconWhite", M((0, 0, 0), (0.18, 0.18, 0.18)), u=10, v=6)

    def shards(b):
        for i in range(8):
            a = i / 8 * 360
            b.cone("M_PlayerGlow", M(r=(0, a, 0)) @ M((0, 0, 0.55), r=(0, 0, 0)), 0.1, 0.0, 0.6, segments=6)
        b.ico("M_Heartstone", M((0, 0, 0), (0.25, 0.25, 0.25)), subdiv=1)

    def laser(b):
        b.sphere("M_IconSteel", M((-0.55, 0, 0), (0.28, 0.12, 0.28)), u=12, v=8)
        b.cube("M_PlayerGlow", M((0.25, 0, 0), (1.3, 0.06, 0.12)))
        torus(b, "M_PlayerGlow", M((-0.55, 0, 0)), 1.0, 0.04, segments=20, sides=4, arc=math.pi * 0.6)

    def ram(b):
        b.cone("M_PlayerGlow", M((0.25, 0, 0.25), r=(0, 45, 0)), 0.32, 0.0, 0.7, segments=6)
        for k in range(3):
            b.cube("M_IconWhite", M((-0.35 - k * 0.12, 0, -0.1 + k * 0.17), (0.7, 0.05, 0.06), (0, -45, 0)))

    def sparks(b):
        for x, z in ((-0.5, -0.3), (0.0, 0.25), (0.5, -0.1)):
            b.sphere("M_PlayerGlow", M((x, 0, z), (0.16, 0.16, 0.16)), u=10, v=6)
            b.cone("M_IconWhite", M((x - 0.25, 0, z - 0.18), r=(0, -55, 0)), 0.09, 0.0, 0.45, segments=6)

    def storm(b):
        torus(b, "M_PlayerGlow", M(), 0.75, 0.05, segments=32, sides=5)
        for (x1, z1), (x2, z2) in (((0.15, 0.55), (-0.1, 0.05)), ((-0.1, 0.05), (0.15, 0.0)), ((0.15, 0.0), (-0.15, -0.55))):
            d = Vector((x2 - x1, 0, z2 - z1))
            b.cone("M_Gold", Matrix.Translation(Vector((x1, 0, z1)) + d * 0.5) @ d.to_track_quat('Z', 'Y').to_matrix().to_4x4(), 0.08, 0.06, d.length + 0.1, segments=4)

    def coil(b):
        for k in range(4):   # coil rings seen at an angle
            torus(b, "M_PlayerGlow", M((0, 0, -0.55 + k * 0.28), (1, 1, 0.35)), 0.38, 0.06, segments=20, sides=5)
        _hook(b, "M_IconSteel", (0, 0, 0.75), 0.6)

    def heart(b):
        _heart(b, "M_IconRed")

    def armor(b):
        b.cube("M_IconSteel", M((0, 0, 0.15), (0.9, 0.2, 0.8)))
        b.cone("M_IconSteel", M((0, 0, -0.45), (1, 0.22, 1), r=(180, 0, 45)), 0.64, 0.0, 0.5, segments=4)
        b.cube("M_PlayerGlow", M((0, -0.12, 0.1), (0.12, 0.05, 0.6)))

    def sword(b):
        b.cube("M_IconSteel", M((0, 0, 0.25), (0.14, 0.06, 1.0)))
        b.cone("M_IconSteel", M((0, 0, 0.85), (1, 0.4, 1), (0, 0, 45)), 0.1, 0.0, 0.25, segments=4)
        b.cube("M_Gold", M((0, 0, -0.3), (0.55, 0.1, 0.1)))
        b.cube("M_Wood", M((0, 0, -0.5), (0.1, 0.1, 0.35)))

    def chevrons(b):
        for k in range(2):
            for side in (-1, 1):
                b.cube("M_Gold", M((side * 0.2, 0, -0.2 + k * 0.45), (0.55, 0.1, 0.14), (0, side * -40, 0)))

    def boot(b):
        b.cube("M_Wood", M((0, 0, 0.15), (0.35, 0.3, 0.8)))
        b.cube("M_Wood", M((0.2, 0, -0.35), (0.8, 0.3, 0.25)))
        for k in range(3):
            b.cube("M_IconWhite", M((-0.55, 0, 0.3 - k * 0.25), (0.35, 0.05, 0.06)))

    def wing(b):
        for k in range(4):
            b.cone("M_IconWhite", M((0, 0, -0.3), r=(0, -20 + k * 22, 0)) @ M((0, 0, 0.45 - k * 0.05)), 0.12, 0.03, 0.9 - k * 0.12, segments=6)

    def hook_range(b):
        _hook(b, "M_IconSteel", (-0.25, 0, 0.1), 0.9)
        _arrow(b, "M_PlayerGlow", (0.1, 0, -0.55), (0.75, 0, 0.45))

    def magnet(b):
        torus(b, "M_IconRed", M((0, 0, 0)), 0.4, 0.13, segments=16, sides=6, arc=math.pi)
        for x in (-0.4, 0.4):
            b.cone("M_IconRed", M((x, 0, -0.2)), 0.13, 0.13, 0.4, segments=8)
            b.cone("M_IconSteel", M((x, 0, -0.48)), 0.13, 0.13, 0.16, segments=8)
        b.ico("M_XPGem", M((0, 0, -0.75), (0.12, 0.12, 0.16)), subdiv=1)

    def regen(b):
        _heart(b, "M_IconRed", (-0.12, 0, 0), 0.9)
        b.cube("M_IconGreen", M((0.5, -0.2, 0.4), (0.4, 0.1, 0.12)))
        b.cube("M_IconGreen", M((0.5, -0.2, 0.4), (0.12, 0.1, 0.4)))

    def xp(b):
        b.cone("M_XPGem", M((0, 0, 0.2)), 0.35, 0.0, 0.6, segments=6)
        b.cone("M_XPGem", M((0, 0, -0.2), r=(180, 0, 0)), 0.35, 0.0, 0.45, segments=6)
        _arrow(b, "M_IconGreen", (0.55, 0, -0.4), (0.55, 0, 0.5), 0.06)

    def luck(b):
        for a in (45, 135, 225, 315):
            r = math.radians(a)
            b.sphere("M_IconGreen", M((math.cos(r) * 0.28, 0, math.sin(r) * 0.28 + 0.1), (0.26, 0.12, 0.26)), u=12, v=6)
        b.cone("M_IconGreen", M((0.1, 0, -0.45), r=(0, 20, 0)), 0.05, 0.03, 0.6, segments=6)

    def hourglass(b, mat="M_PlayerGlow"):
        b.cone(mat, M((0, 0, 0.3), r=(180, 0, 0)), 0.38, 0.03, 0.55, segments=10)
        b.cone(mat, M((0, 0, -0.3)), 0.38, 0.03, 0.55, segments=10)
        for z in (0.6, -0.6):
            b.cube("M_Wood", M((0, 0, z), (0.9, 0.3, 0.1)))

    def zip_range(b):
        _hook(b, "M_PlayerGlow", (-0.3, 0, 0.1), 0.8)
        _arrow(b, "M_IconWhite", (0.0, 0, -0.1), (0.8, 0, -0.1), 0.06)
        _arrow(b, "M_IconWhite", (0.0, 0, 0.25), (0.8, 0, 0.25), 0.06)

    def gold(b):
        b.cone("M_Gold", M(r=(90, 0, 0)), 0.5, 0.5, 0.1, segments=20)
        b.cone("M_ChestTrim", M((0, -0.04, 0), r=(90, 0, 0)), 0.32, 0.32, 0.1, segments=20)

    def kills(b):
        b.sphere("M_Bat", M((0, 0, 0), (0.35, 0.25, 0.3)), u=12, v=8)
        for side in (-1, 1):
            b.cone("M_Bat", M((side * 0.17, 0, 0.35), r=(0, side * 25, 0)), 0.1, 0.0, 0.35, segments=5)
            b.membrane("M_Bat", [(side * 0.25, 0, 0.1), (side * 0.95, 0, 0.3), (side * 0.75, 0, -0.1), (side * 0.5, 0, -0.05), (side * 0.3, 0, -0.15)], thickness=0.04)
            b.sphere("M_EnemyEye", M((side * 0.12, -0.24, 0.05), (0.06, 0.03, 0.06)), u=6, v=4)

    def zip_icon(b):
        _hook(b, "M_PlayerGlow", (0, 0, 0), 1.0)

    jobs = {
        "OrbitBlades": orbit, "ShardBurst": shards, "ArcLaser": laser, "RamStrike": ram, "HomingSparks": sparks,
        "StormAura": storm, "TetherCoil": coil,
        "MaxHealth": heart, "Armor": armor, "Damage": sword, "AttackSpeed": chevrons, "WalkSpeed": boot,
        "FlightSpeed": wing, "HookRange": hook_range, "PickupRadius": magnet, "Regen": regen, "XPGain": xp,
        "Luck": luck, "ZipCooldown": hourglass, "ZipRange": zip_range,
        "Gold": gold, "Kills": kills, "Timer": lambda b: hourglass(b, "M_IconWhite"), "Health": heart, "Zip": zip_icon,
    }
    for name, build in jobs.items():
        make(name, build)

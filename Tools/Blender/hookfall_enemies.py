# Hookfall - the enemies added on 2026-11-10, so the sky holds more than bats and wyverns, and the second stage boss:
#   Razorwing    a sleek dart of a bird with swept wings; flies in formation and slashes past the Warden
#   Blightpuff   a spiked ball with a glowing belly; it swells up and bursts
#   Venomwing    a wasp that keeps its distance and spits; its abdomen ("Tail") is a part of its own, to aim with
#   GloomJelly   a sky jellyfish: bell, a glowing core and six tentacles that are parts of their own, to sway
#   Wyrm         the Tempest Wyrm: a head with a hinged jaw, one body segment (the game strings many together) and a tail
# All face -Y like the other creatures; flyers have their origin at the middle of the body. Moving parts are separate
# objects whose origin is the joint. Loaded by hookfall_models.py like hookfall_boss.py.
# AI disclosure: written with Claude (Anthropic) on 2026-11-10. Prompts and changes are recorded in Docs/PromptLog.md.
import math
import random

import bmesh
from mathutils import Matrix, Vector

PALETTE_ENEMIES = {
    "M_Razor": (0.20, 0.17, 0.44), "M_RazorEdge": (0.86, 0.36, 0.80),
    "M_Puff": (0.34, 0.13, 0.32), "M_PuffCore": (1.00, 0.42, 0.16),
    "M_Wasp": (0.26, 0.13, 0.36), "M_WaspWing": (0.80, 0.76, 0.96), "M_Venom": (0.72, 1.00, 0.30),
    "M_Jelly": (0.62, 0.44, 0.86), "M_JellyDark": (0.36, 0.22, 0.58),
    "M_Wyrm": (0.20, 0.24, 0.50), "M_WyrmBelly": (0.56, 0.52, 0.86), "M_WyrmFin": (0.86, 0.82, 1.00),
    "M_Lightning": (1.00, 0.92, 0.40),
}


def install(ns):
    """Adds the palette and shares the main script's helpers (and the v2 torus)."""
    ns["PALETTE"].update(PALETTE_ENEMIES)
    g = globals()
    for name in ("Builder", "M", "reset", "export", "preview", "empty"):
        g[name] = ns[name]
    import hookfall_models_v2
    g["torus"] = hookfall_models_v2.torus


def jobs(out_dir, prev_dir):
    return {
        "Razorwing": lambda: razorwing(out_dir, prev_dir),
        "Blightpuff": lambda: blightpuff(out_dir, prev_dir),
        "Venomwing": lambda: venomwing(out_dir, prev_dir),
        "GloomJelly": lambda: gloom_jelly(out_dir, prev_dir),
        "Wyrm": lambda: wyrm(out_dir, prev_dir),
    }


# ---------------------------------------------------------------- helpers

def aim(pos, direction):
    """A matrix that puts a primitive at pos with its +Z along direction."""
    return Matrix.Translation(Vector(pos)) @ Vector(direction).to_track_quat('Z', 'Y').to_matrix().to_4x4()


def limb(b, mat, a, c, ra, rc, segments=6):
    """A tapered cone from point a (radius ra) to point c (radius rc)."""
    a, c = Vector(a), Vector(c)
    d = c - a
    return b.cone(mat, aim((a + c) / 2, d), ra, rc, d.length, segments=segments)


def plate(b, mat, pts, thickness=0.04, axis='Z'):
    """A flat, closed shape from outline points, `thickness` thick along axis ('Z': a level wing, 'X': an upright
    fin, 'Y': a sheet facing the front). The outline may run either way round."""
    normal = {'X': Vector((1, 0, 0)), 'Y': Vector((0, 1, 0)), 'Z': Vector((0, 0, 1))}[axis]
    pts = [Vector(p) for p in pts]
    area = Vector((0, 0, 0))
    for i in range(len(pts)):                       # Newell's method: which way the outline runs
        area += pts[i].cross(pts[(i + 1) % len(pts)])
    if area.dot(normal) < 0:
        pts.reverse()
    off = normal * (thickness / 2)
    top = [b.bm.verts.new(p + off) for p in pts]
    bot = [b.bm.verts.new(p - off) for p in pts]
    idx = b.mat_index(mat)
    b.bm.faces.new(top).material_index = idx
    b.bm.faces.new(list(reversed(bot))).material_index = idx
    for i in range(len(pts)):
        j = (i + 1) % len(pts)
        b.bm.faces.new((top[i], bot[i], bot[j], top[j])).material_index = idx
    return top + bot


def sphere_points(count, seed=0.0):
    """Evenly spread unit directions (a Fibonacci sphere)."""
    out = []
    golden = math.pi * (3 - math.sqrt(5))
    for i in range(count):
        z = 1 - 2 * (i + 0.5) / count
        r = math.sqrt(max(0.0, 1 - z * z))
        a = golden * i + seed
        out.append(Vector((math.cos(a) * r, math.sin(a) * r, z)))
    return out


# ---------------------------------------------------------------- Razorwing

def razorwing(out_dir, prev_dir):
    """A dart of a bird about 1.7 m long with a 2.8 m span: long swept wings with a bright cutting edge, a blade crest,
    a forked tail. The wings are level plates hinged at the shoulders."""
    reset()
    b = Builder("Body")
    b.sphere("M_Razor", M((0, 0.05, 0), (0.21, 0.72, 0.19)), u=12, v=8)
    b.sphere("M_RazorEdge", M((0, 0.0, -0.07), (0.15, 0.56, 0.12)), u=10, v=6)             # pale belly
    b.sphere("M_Razor", M((0, -0.68, 0.05), (0.17, 0.24, 0.16)), u=10, v=7)                # head
    limb(b, "M_Fang", (0, -0.86, 0.02), (0, -1.28, -0.02), 0.07, 0.0, 5)                    # beak
    for side in (-1, 1):
        b.sphere("M_EnemyEye", M((side * 0.115, -0.78, 0.09), (0.045, 0.06, 0.04)), u=6, v=4)
        # Forked tail: two long blades.
        plate(b, "M_Razor", [(side * 0.05, 0.62, 0.0), (side * 0.34, 1.32, 0.0), (side * 0.12, 1.02, 0.0), (0, 0.9, 0.0)], 0.03)
        plate(b, "M_RazorEdge", [(side * 0.34, 1.32, 0.0), (side * 0.29, 1.12, 0.0), (side * 0.2, 1.1, 0.0)], 0.035)
    # A blade crest from the brow to the back.
    plate(b, "M_RazorEdge", [(0, -0.72, 0.16), (0, -0.2, 0.44), (0, 0.3, 0.2), (0, -0.3, 0.16)], 0.03, axis='X')
    b.build()
    for side, name in ((-1, "Wing_L"), (1, "Wing_R")):
        w = Builder(name)
        # Level wing: a swept leading edge to a sharp tip, three steps back along the trailing edge.
        outline = [(0, -0.28, 0), (side * 0.7, -0.12, 0), (side * 1.38, 0.62, 0), (side * 1.0, 0.36, 0), (side * 0.92, 0.6, 0),
                   (side * 0.62, 0.32, 0), (side * 0.5, 0.56, 0), (side * 0.24, 0.3, 0), (0, 0.36, 0)]
        plate(w, "M_Razor", outline, 0.04)
        plate(w, "M_RazorEdge", [(side * 0.04, -0.3, 0), (side * 0.72, -0.15, 0), (side * 1.4, 0.64, 0),
                                 (side * 0.72, -0.04, 0), (side * 0.04, -0.2, 0)], 0.055)   # the cutting edge
        limb(w, "M_Razor", (0, -0.18, 0.01), (side * 0.66, -0.06, 0.01), 0.055, 0.03, 5)    # arm bone
        w.build(location=(side * 0.17, -0.12, 0.05))
    export("Razorwing", out_dir)
    preview("Razorwing", prev_dir)


# ---------------------------------------------------------------- Blightpuff

def blightpuff(out_dir, prev_dir):
    """A spiked ball about 1.1 m across: dark hide, glowing blisters and a glowing belly, small eyes, bone spikes all
    round, two stubby fins it flutters, and a curled tail with a glowing tip."""
    reset()
    rnd = random.Random(11)
    b = Builder("Body")
    verts = b.ico("M_Puff", M((0, 0, 0), (0.5, 0.5, 0.48)), subdiv=2)
    b.displace(verts, amp=0.05, freq=3.0, seed=4)
    b.sphere("M_PuffCore", M((0, -0.04, -0.2), (0.4, 0.4, 0.3)), u=12, v=7)                 # the belly that glows
    for d in sphere_points(18, 0.7):
        if d.y < -0.72 or d.z < -0.55:
            continue                                                                     # keep the face and the belly clear
        length = rnd.uniform(0.22, 0.36)
        limb(b, "M_Fang", d * 0.44, d * (0.5 + length), 0.075, 0.0, 5)
    for d in sphere_points(9, 2.1):
        if d.z < -0.3 or d.y < -0.8:
            continue
        b.sphere("M_PuffCore", Matrix.Translation(d * 0.47) @ Matrix.Diagonal((0.085, 0.085, 0.085, 1)), u=6, v=4)   # blisters
    for side in (-1, 1):
        b.sphere("M_EnemyEye", M((side * 0.19, -0.44, 0.12), (0.075, 0.05, 0.085)), u=7, v=5)
        b.cube("M_Puff", M((side * 0.19, -0.47, 0.2), (0.17, 0.04, 0.035), (0, side * -24, 0)))       # angry brows
    for k in range(-2, 3):                                                                # a row of teeth
        limb(b, "M_Fang", (k * 0.07, -0.47, -0.06), (k * 0.07, -0.5, -0.15), 0.03, 0.0, 4)
    # Tail: a curl ending in a glowing bulb.
    pts = [(0, 0.44, 0.02), (0, 0.66, 0.16), (0, 0.78, 0.4), (0, 0.66, 0.58)]
    for i in range(len(pts) - 1):
        limb(b, "M_Puff", pts[i], pts[i + 1], 0.09 - i * 0.02, 0.07 - i * 0.02, 6)
    b.sphere("M_PuffCore", M((0, 0.64, 0.62), (0.1, 0.1, 0.1)), u=8, v=5)
    b.build()
    for side, name in ((-1, "Wing_L"), (1, "Wing_R")):
        w = Builder(name)
        plate(w, "M_Puff", [(0, -0.1, 0), (side * 0.36, -0.16, 0), (side * 0.46, 0.06, 0), (side * 0.3, 0.02, 0),
                            (side * 0.3, 0.2, 0), (side * 0.14, 0.1, 0), (0, 0.16, 0)], 0.04)
        w.build(location=(side * 0.44, 0.02, 0.04))
    export("Blightpuff", out_dir)
    preview("Blightpuff", prev_dir)


# ---------------------------------------------------------------- Venomwing

def venomwing(out_dir, prev_dir):
    """A wasp about 1.9 m long: head with mandibles, a thorax, long narrow wings in two pairs, dangling legs, and an
    abdomen (its own part, "Tail", hinged at the waist) in banded segments that ends in a glowing sting."""
    reset()
    b = Builder("Body")
    b.sphere("M_Wasp", M((0, 0, 0), (0.3, 0.42, 0.28)), u=12, v=8)                          # thorax
    b.sphere("M_Wasp", M((0, -0.52, 0.06), (0.24, 0.24, 0.22)), u=12, v=8)                  # head
    for side in (-1, 1):
        b.sphere("M_EnemyEye", M((side * 0.17, -0.62, 0.12), (0.09, 0.11, 0.1)), u=8, v=5)
        limb(b, "M_Fang", (side * 0.08, -0.7, -0.05), (side * 0.03, -0.9, -0.16), 0.045, 0.0, 5)      # mandibles
        limb(b, "M_Wasp", (side * 0.1, -0.64, 0.24), (side * 0.3, -0.92, 0.5), 0.022, 0.012, 4)      # feelers
        for k, y in enumerate((-0.18, 0.02, 0.2)):                                        # legs, folded under
            knee = (side * (0.42 + k * 0.03), y, -0.3)
            limb(b, "M_Wasp", (side * 0.2, y, -0.16), knee, 0.035, 0.028, 4)
            limb(b, "M_Wasp", knee, (side * 0.3, y + 0.16, -0.62), 0.028, 0.012, 4)
    torus(b, "M_BatBelly", M((0, 0.3, 0), r=(90, 0, 0)), 0.2, 0.05, segments=12, sides=5)      # the waist
    b.build()

    tail = Builder("Tail")
    # Abdomen: fat banded segments that curl down and forward, the sting at the end. The origin is the waist.
    spine = [(0, 0.0, 0.0), (0, 0.34, -0.04), (0, 0.66, -0.2), (0, 0.84, -0.48), (0, 0.8, -0.76)]
    radii = [0.17, 0.3, 0.27, 0.18, 0.09]
    for i in range(len(spine) - 1):
        limb(tail, "M_Wasp" if i % 2 == 0 else "M_BatBelly", spine[i], spine[i + 1], radii[i], radii[i + 1], 10)
    limb(tail, "M_Venom", spine[-1], (0, 0.62, -1.02), 0.075, 0.0, 6)                     # the sting
    tail.sphere("M_Venom", M((0, 0.74, -0.84), (0.1, 0.1, 0.1)), u=7, v=5)                 # venom sac
    tail.build(location=(0, 0.34, 0.0))

    for side, name in ((-1, "Wing_L"), (1, "Wing_R")):
        w = Builder(name)
        plate(w, "M_WaspWing", [(0, -0.06, 0), (side * 0.5, -0.2, 0), (side * 1.25, -0.02, 0), (side * 1.3, 0.14, 0),
                                (side * 0.6, 0.2, 0), (0, 0.08, 0)], 0.02)                 # fore wing
        plate(w, "M_WaspWing", [(0, 0.12, -0.02), (side * 0.5, 0.26, -0.02), (side * 0.92, 0.5, -0.02), (side * 0.8, 0.62, -0.02),
                                (side * 0.3, 0.46, -0.02), (0, 0.28, -0.02)], 0.02)        # hind wing
        limb(w, "M_Wasp", (0, -0.04, 0.01), (side * 1.2, 0.02, 0.01), 0.025, 0.012, 4)     # vein
        w.build(location=(side * 0.2, -0.08, 0.24))
    export("Venomwing", out_dir)
    preview("Venomwing", prev_dir)


# ---------------------------------------------------------------- Gloom Jelly

def gloom_jelly(out_dir, prev_dir):
    """A sky jellyfish about 3.2 m across: a bell with glowing spots and a frilled rim, a darker underside, a glowing
    core hanging inside it and six tentacles (parts of their own, hinged at the rim) about 3 m long."""
    reset()
    bell = Builder("Body")
    outer = bell.sphere("M_Jelly", M((0, 0, 0), (1.6, 1.6, 1.25)), u=18, v=10)
    bmesh.ops.delete(bell.bm, geom=[v for v in outer if v.co.z < -0.02], context='VERTS')
    # The underside: a smaller, darker dome seen from below (its faces turned inwards).
    inner = bell.sphere("M_JellyDark", M((0, 0, -0.02), (1.5, 1.5, 1.05)), u=18, v=8)
    keep = [v for v in inner if v.co.z >= -0.04]
    bmesh.ops.delete(bell.bm, geom=[v for v in inner if v.co.z < -0.04], context='VERTS')
    bmesh.ops.reverse_faces(bell.bm, faces=list({f for v in keep for f in v.link_faces}))
    torus(bell, "M_JellyDark", M((0, 0, -0.02), r=(90, 0, 0)), 1.56, 0.11, segments=24, sides=6)   # the rim
    for i in range(12):                                                                  # frill under the rim
        a = i / 12 * math.tau
        limb(bell, "M_Jelly", (math.cos(a) * 1.5, math.sin(a) * 1.5, -0.06), (math.cos(a) * 1.62, math.sin(a) * 1.62, -0.4), 0.13, 0.03, 5)
    for d in sphere_points(16, 1.3):                                                     # glowing spots on the bell
        if d.z < 0.25:
            continue
        p = Vector((d.x * 1.6, d.y * 1.6, d.z * 1.25))
        bell.sphere("M_EnemyEye", Matrix.Translation(p * 0.985) @ Matrix.Diagonal((0.13, 0.13, 0.06, 1)), u=7, v=4)
    # The core and the short arms round it.
    bell.sphere("M_EnemyEye", M((0, 0, 0.18), (0.46, 0.46, 0.52)), u=12, v=8)
    for i in range(4):
        a = i / 4 * math.tau + 0.4
        limb(bell, "M_Jelly", (math.cos(a) * 0.22, math.sin(a) * 0.22, -0.1), (math.cos(a) * 0.34, math.sin(a) * 0.34, -1.25), 0.15, 0.04, 6)
    bell.build()

    rnd = random.Random(5)
    for i in range(6):
        a = i / 6 * math.tau + 0.26
        t = Builder(f"Tentacle_{i + 1}")
        # Hangs straight down from its origin in four tapering lengths, with a little bend so it is not a rod.
        pts = [Vector((0, 0, 0))]
        for k in range(4):
            pts.append(pts[-1] + Vector((rnd.uniform(-0.12, 0.12), rnd.uniform(-0.12, 0.12), -0.72 - k * 0.04)))
        for k in range(4):
            limb(t, "M_JellyDark" if k % 2 == 0 else "M_Jelly", pts[k], pts[k + 1], 0.12 - k * 0.022, 0.1 - k * 0.022, 6)
        t.sphere("M_EnemyEye", Matrix.Translation(pts[-1]) @ Matrix.Diagonal((0.09, 0.09, 0.12, 1)), u=6, v=4)
        t.build(location=(math.cos(a) * 1.18, math.sin(a) * 1.18, -0.08))
    export("GloomJelly", out_dir)
    preview("GloomJelly", prev_dir)


# ---------------------------------------------------------------- Tempest Wyrm

def wyrm(out_dir, prev_dir):
    """The Tempest Wyrm, in the pieces the game strings together: a horned head about 6 m long with a hinged jaw
    ("Jaw"), one body segment 3.4 m across ("Segment": the game places many in a row along its path) and the tail
    with its blade ("Tail"). Each piece has its origin on the spine and faces -Y. They are laid out in a row for the
    preview; the game moves each on its own."""
    reset()
    rnd = random.Random(31)

    head = Builder("Head")
    v = head.ico("M_Wyrm", M((0, -0.4, 0.2), (1.5, 2.3, 1.15)), subdiv=2)
    head.displace(v, amp=0.16, freq=0.9, seed=3)
    v = head.ico("M_Wyrm", M((0, -2.7, 0.0), (0.95, 1.5, 0.62)), subdiv=2)                 # snout
    head.displace(v, amp=0.1, freq=1.1, seed=5)
    head.sphere("M_WyrmBelly", M((0, -1.0, -0.42), (1.2, 2.4, 0.5)), u=12, v=6)            # pale throat and upper lip
    for side in (-1, 1):
        head.sphere("M_Lightning", M((side * 1.02, -1.5, 0.62), (0.2, 0.36, 0.22)), u=8, v=5)          # eyes
        head.cube("M_Wyrm", M((side * 1.0, -1.5, 0.9), (0.62, 1.0, 0.16), (0, side * -16, side * 10)))   # brow ridge
        limb(head, "M_Fang", (side * 0.52, -3.9, -0.02), (side * 0.52, -4.0, -0.5), 0.1, 0.0, 5)       # front fangs
        # Two great horns sweeping back, and a smaller pair.
        horn = [(side * 0.9, 0.5, 0.9), (side * 1.5, 1.6, 1.7), (side * 1.75, 2.9, 2.0), (side * 1.6, 4.0, 1.75)]
        for i in range(len(horn) - 1):
            limb(head, "M_WyrmFin", horn[i], horn[i + 1], 0.36 - i * 0.1, 0.26 - i * 0.1, 7)
        limb(head, "M_WyrmFin", (side * 1.3, -0.2, 0.7), (side * 2.2, 0.9, 0.9), 0.2, 0.0, 6)
        # Whiskers: long thin fins from the snout.
        plate(head, "M_WyrmFin", [(side * 0.8, -3.2, 0.1), (side * 2.3, -2.4, 0.5), (side * 3.0, -1.0, 0.2), (side * 2.1, -2.0, 0.2), (side * 0.9, -2.9, 0.0)], 0.06)
        for k in range(4):                                                                 # upper teeth
            limb(head, "M_Fang", (side * 0.78, -3.3 + k * 0.6, -0.3), (side * 0.8, -3.2 + k * 0.6, -0.72), 0.085, 0.0, 4)
        head.sphere("M_TowerDark", M((side * 0.3, -4.0, 0.22), (0.1, 0.14, 0.08)), u=6, v=4)             # nostrils
    for i in range(5):                                                                     # crest down the skull
        y = -1.6 + i * 0.75
        plate(head, "M_WyrmFin", [(0, y, 1.1), (0, y + 0.5, 1.95 - i * 0.12), (0, y + 0.7, 1.1)], 0.07, axis='X')
    head.sphere("M_Lightning", M((0, -0.6, 1.26), (0.26, 0.4, 0.2)), u=8, v=5)             # the storm gem on its brow
    head_ob = head.build(location=(0, 0, 0))

    jaw = Builder("Jaw")
    # Hinged at the back of the head: the origin is the hinge, the jaw reaches forward (-Y) from it.
    jv = jaw.ico("M_Wyrm", M((0, -1.9, -0.25), (0.95, 2.1, 0.42)), subdiv=2)
    jaw.displace(jv, amp=0.08, freq=1.1, seed=8)
    jaw.sphere("M_WyrmBelly", M((0, -1.7, -0.5), (0.8, 1.8, 0.3)), u=10, v=5)
    for side in (-1, 1):
        for k in range(5):
            limb(jaw, "M_Fang", (side * 0.66, -3.3 + k * 0.6, -0.05), (side * 0.68, -3.25 + k * 0.6, 0.36), 0.08, 0.0, 4)
        plate(jaw, "M_WyrmFin", [(side * 0.7, -0.6, -0.4), (side * 1.5, 0.3, -1.1), (side * 0.8, 0.1, -0.5)], 0.06)   # chin fins
    jaw.sphere("M_Lightning", M((0, -1.4, 0.06), (0.5, 1.3, 0.14)), u=8, v=4)              # the glow in its throat
    jaw.build(location=(0, 0.2, -0.75), parent=head_ob)

    seg = Builder("Segment")
    sv = seg.ico("M_Wyrm", M((0, 0, 0), (1.7, 1.75, 1.5)), subdiv=2)
    seg.displace(sv, amp=0.14, freq=0.9, seed=13)
    seg.sphere("M_WyrmBelly", M((0, 0, -0.62), (1.35, 1.6, 0.98)), u=12, v=7)              # belly plates
    for k in (-1, 0, 1):
        seg.cube("M_WyrmBelly", M((0, k * 0.95, -1.5), (1.5, 0.72, 0.14)))
    plate(seg, "M_WyrmFin", [(0, -1.1, 1.3), (0, -0.2, 2.75), (0, 0.5, 2.3), (0, 1.25, 1.3)], 0.09, axis='X')        # back sail
    for side in (-1, 1):
        plate(seg, "M_WyrmFin", [(side * 1.5, -0.7, 0.1), (side * 2.9, 0.5, 0.5), (side * 2.4, 0.9, 0.2), (side * 1.5, 0.8, 0.0)], 0.07)   # side fin
        for k in range(3):                                                                 # a row of storm scales
            seg.sphere("M_Lightning", M((side * (1.5 - k * 0.14), -0.9 + k * 0.9, 0.72 + k * 0.05), (0.13, 0.2, 0.13)), u=6, v=4)
    seg.build(location=(0, 6.5, 0))

    tail = Builder("Tail")
    pts = [(0, 0, 0), (0, 1.6, 0.05), (0, 3.1, 0.2), (0, 4.4, 0.5)]
    rad = [1.25, 0.85, 0.5, 0.2]
    for i in range(len(pts) - 1):
        limb(tail, "M_Wyrm", pts[i], pts[i + 1], rad[i], rad[i + 1], 10)
    plate(tail, "M_WyrmFin", [(0, 2.6, 0.2), (0, 4.0, 2.4), (0, 5.9, 2.9), (0, 5.0, 0.9), (0, 6.3, -1.3), (0, 4.4, -0.3)], 0.1, axis='X')   # tail blade
    plate(tail, "M_WyrmFin", [(0, 0.2, 1.1), (0, 1.0, 1.9), (0, 1.9, 0.8)], 0.08, axis='X')
    tail.build(location=(0, 11.0, 0))
    export("Wyrm", out_dir)
    preview("Wyrm", prev_dir)

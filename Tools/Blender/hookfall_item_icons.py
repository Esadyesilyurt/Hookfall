# Hookfall - icons for the long-range items, the passives and the Lifesteal stat (2026-11-08), in the style of the
# first icon set (hookfall_models_v2.icons): small 3D emblems seen from the front, flat palette colours, ink outline.
# Loaded by hookfall_models.py; run the "ItemIcons" job.
# AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
import math, os
from mathutils import Vector, Matrix

PALETTE_ICONS = {
    "M_IconPurple": (0.60, 0.36, 0.95), "M_IconOrange": (1.00, 0.52, 0.12), "M_IconBrown": (0.55, 0.35, 0.20),
    "M_IconPink": (1.00, 0.58, 0.78), "M_IconBlood": (0.70, 0.04, 0.12), "M_IconAmber": (1.00, 0.74, 0.18),
    "M_IconGlass": (0.70, 0.90, 1.00), "M_IconBone": (0.95, 0.90, 0.78), "M_IconLeaf": (0.30, 0.72, 0.30),
}


def install(ns):
    ns["PALETTE"].update(PALETTE_ICONS)
    g = globals()
    for name in ("Builder", "M", "reset"):
        g[name] = ns[name]
    import hookfall_models_v2 as v2
    for name in ("torus", "_render_icon", "_heart", "_arrow"):
        g[name] = getattr(v2, name)


def _bolt(b, mat, points, width=0.07, y=-0.55):
    """A zigzag lightning bolt through the given XZ points."""
    for (x1, z1), (x2, z2) in zip(points, points[1:]):
        d = Vector((x2 - x1, 0, z2 - z1))
        b.cone(mat, Matrix.Translation(Vector((x1, y, z1)) + d * 0.5) @ d.to_track_quat('Z', 'Y').to_matrix().to_4x4(),
               width, width * 0.8, d.length + width, segments=4)


def _jar(b, glass="M_IconGlass", lid="M_Wood"):
    b.cone(glass, M((0, 0.1, -0.15)), 0.45, 0.42, 0.85, segments=16)
    b.cone(glass, M((0, 0.1, 0.33)), 0.3, 0.3, 0.12, segments=16)
    b.cube(lid, M((0, 0.1, 0.45), (0.5, 0.5, 0.14)))


def _star(b, mat, t, s):
    for a in range(5):
        b.cone(mat, M(t, r=(0, a * 72, 0)) @ M((0, 0, 0.25 * s)), 0.11 * s, 0.0, 0.5 * s, segments=4)
    b.sphere(mat, M(t, (0.16 * s, 0.1 * s, 0.16 * s)), u=8, v=5)


def _coin(b, mat, t=(0, 0, 0), s=1.0):
    b.cone(mat, M(t, (s, s, s), (90, 0, 0)), 0.5, 0.5, 0.12, segments=24)
    b.cone("M_ChestTrim", M((t[0], t[1] - 0.05 * s, t[2]), (s, s, s), (90, 0, 0)), 0.36, 0.36, 0.1, segments=24)


def _fang(b, t, s=1.0):
    b.cone("M_IconBone", M(t, (s, s, s), (180, 0, 0)), 0.13, 0.0, 0.5, segments=8)


def item_icons(out_dir, prev_dir):
    os.makedirs(out_dir, exist_ok=True)

    def make(name, build):
        reset()
        b = Builder("Icon")
        build(b)
        b.build()
        _render_icon(name, out_dir)

    def skybow(b):
        torus(b, "M_Wood", M(r=(0, 90, 0)), 0.75, 0.07, segments=24, sides=6, arc=math.pi)
        b.cube("M_IconWhite", M((0, 0, 0), (0.025, 0.05, 1.5)))
        _arrow(b, "M_PlayerGlow", (-0.2, -0.1, 0), (0.95, -0.1, 0), 0.05)

    def storm_jar(b):
        _jar(b)
        b.cube("M_IconDark", M((0, -0.45, -0.15), (0.55, 0.02, 0.6)))   # the jar's dark inside, in front of the opaque glass
        _bolt(b, "M_Gold", [(0.12, 0.25), (-0.12, -0.02), (0.1, -0.05), (-0.1, -0.45)], 0.07)

    def grimoire(b):
        b.cube("M_IconPurple", M((0, 0, -0.4), (1.1, 0.25, 0.5), (0, -8, 0)))
        b.cube("M_IconWhite", M((0, -0.05, -0.35), (1.0, 0.22, 0.42), (0, -8, 0)))
        for x, z, s in ((-0.45, 0.45, 0.8), (0.1, 0.65, 1.0), (0.55, 0.3, 0.7)):
            _star(b, "M_Gold", (x, -0.1, z), s * 0.55)

    def dragon_ember(b):
        _heart(b, "M_IconOrange", (0, 0, -0.1), 0.95)
        for x, h in ((-0.25, 0.45), (0.0, 0.6), (0.25, 0.4)):
            b.cone("M_Gold", M((x, -0.15, 0.35)), 0.12, 0.0, h, segments=6)

    def giants_ring(b):
        torus(b, "M_Gold", M(), 0.55, 0.15, segments=28, sides=8)
        b.ico("M_IconRed", M((0, -0.05, 0.72), (0.2, 0.15, 0.2)), subdiv=1)

    def troll_cloak(b):
        b.membrane("M_IconLeaf", [(-0.35, 0, 0.6), (0.35, 0, 0.6), (0.65, 0, -0.7), (0.2, 0, -0.55), (0.0, 0, -0.75), (-0.2, 0, -0.55), (-0.65, 0, -0.7)], thickness=0.06)
        b.cube("M_IconBrown", M((0, -0.04, 0.55), (0.8, 0.08, 0.16)))
        b.sphere("M_Gold", M((0, -0.1, 0.55), (0.13, 0.08, 0.13)), u=10, v=6)

    def griffin_plume(b):
        b.cone("M_IconWhite", M((0.03, -0.06, 0.12), r=(0, 32, 0)), 0.03, 0.03, 1.4, segments=6)   # the quill, along the vane
        b.membrane("M_Gold", [(0.42, 0, 0.75), (0.15, 0, 0.55), (-0.25, 0, -0.1), (-0.35, 0, -0.5), (-0.05, 0, -0.2), (0.3, 0, 0.3)], thickness=0.04)
        b.membrane("M_IconOrange", [(0.42, 0, 0.75), (0.5, 0, 0.35), (0.25, 0, -0.1), (-0.35, 0, -0.5), (0.05, 0, 0.15)], thickness=0.04)

    def imp_jar(b):
        _jar(b)
        b.sphere("M_IconRed", M((0, -0.45, -0.15), (0.25, 0.2, 0.25)), u=12, v=8)
        for side in (-1, 1):
            b.cone("M_IconBone", M((side * 0.15, -0.45, 0.08), r=(0, side * 25, 0)), 0.06, 0.0, 0.2, segments=5)
            b.sphere("M_Gold", M((side * 0.09, -0.65, -0.1), (0.05, 0.03, 0.06)), u=6, v=4)
        _coin(b, "M_Gold", (0.5, -0.7, -0.55), 0.45)

    def pixie_pouch(b):
        b.sphere("M_IconBrown", M((0, 0, -0.25), (0.55, 0.45, 0.5)), u=14, v=8)
        b.cone("M_IconBrown", M((0, 0, 0.3)), 0.2, 0.32, 0.25, segments=12)
        b.cube("M_Gold", M((0, -0.05, 0.18), (0.48, 0.1, 0.08)))
        for x, z, m in ((-0.4, 0.65, "M_IconPink"), (0.1, 0.8, "M_PlayerGlow"), (0.45, 0.55, "M_IconPink"), (-0.05, 0.55, "M_Gold")):
            _star(b, m, (x, -0.2, z), 0.25)

    def plush_bear(b):
        b.sphere("M_IconBrown", M((0, 0, -0.35), (0.48, 0.35, 0.45)), u=14, v=8)
        b.sphere("M_IconBrown", M((0, -0.05, 0.25), (0.38, 0.3, 0.34)), u=14, v=8)
        for side in (-1, 1):
            b.sphere("M_IconBrown", M((side * 0.3, -0.02, 0.55), (0.13, 0.08, 0.13)), u=10, v=6)
            b.sphere("M_IconDark", M((side * 0.13, -0.32, 0.3), (0.05, 0.03, 0.05)), u=6, v=4)
            b.sphere("M_IconBrown", M((side * 0.45, -0.05, -0.25), (0.15, 0.12, 0.2)), u=10, v=6)
        b.sphere("M_IconBone", M((0, -0.32, 0.17), (0.14, 0.06, 0.1)), u=10, v=6)
        b.membrane("M_IconLeaf", [(0.0, -0.1, 0.62), (0.25, -0.1, 0.85), (0.12, -0.1, 0.62), (0.3, -0.1, 0.62)], thickness=0.04)
        _heart(b, "M_IconRed", (0, -0.4, -0.35), 0.22)

    def owl_monocle(b):
        torus(b, "M_Gold", M(), 0.5, 0.07, segments=28, sides=6)
        b.cone("M_IconGlass", M(r=(90, 0, 0)), 0.45, 0.45, 0.04, segments=24)
        for side in (-1, 1):
            b.cone("M_IconBrown", M((side * 0.32, 0, 0.55), r=(0, side * 25, 0)), 0.1, 0.0, 0.35, segments=5)
        for k in range(4):
            b.sphere("M_Gold", M((0.45 + k * 0.08, 0, -0.45 - k * 0.12), (0.05, 0.05, 0.05)), u=6, v=4)

    def kraken(b):
        pts = [(-0.55, -0.6), (-0.2, -0.45), (0.1, -0.15), (0.2, 0.2), (0.05, 0.5), (-0.2, 0.62)]
        for i, (x, z) in enumerate(pts):
            r = 0.26 - i * 0.035
            b.sphere("M_IconPurple", M((x, 0, z), (r, r, r)), u=12, v=8)
            b.sphere("M_IconPink", M((x + r * 0.5, -r * 0.8, z - r * 0.2), (r * 0.3, r * 0.15, r * 0.3)), u=8, v=4)

    def leech_jar(b):
        _jar(b)
        b.sphere("M_IconBlood", M((0, -0.45, -0.15), (0.16, 0.14, 0.38), (0, 25, 0)), u=12, v=8)
        b.sphere("M_IconRed", M((0.55, -0.6, -0.55), (0.12, 0.1, 0.14)), u=10, v=6)
        b.cone("M_IconRed", M((0.55, -0.6, -0.42)), 0.1, 0.0, 0.18, segments=8)

    def whetstone(b):
        b.cube("M_Stone", M((0, 0, -0.45), (1.1, 0.35, 0.3), (0, 8, 0)))
        b.cube("M_IconSteel", M((0.05, -0.1, 0.25), (0.12, 0.05, 1.0), (0, 35, 0)))
        b.cube("M_Gold", M((-0.27, -0.1, 0.65), (0.4, 0.08, 0.08), (0, 35, 0)))
        for x in (0.3, 0.45, 0.6):
            _star(b, "M_IconWhite", (x, -0.25, -0.15 + x * 0.3), 0.15)

    def pixie_wings(b):
        for side in (-1, 1):
            b.membrane("M_IconPink", [(0, -0.05, 0.05), (side * 0.85, -0.05, 0.75), (side * 0.75, -0.05, 0.15)], thickness=0.03)
            b.membrane("M_PlayerGlow", [(0, -0.06, -0.05), (side * 0.7, -0.06, -0.6), (side * 0.3, -0.06, -0.55)], thickness=0.03)
        b.cone("M_IconWhite", M((0, -0.1, 0)), 0.07, 0.05, 0.6, segments=8)

    def clover_coin(b):
        _coin(b, "M_Gold")
        for a in (45, 135, 225, 315):
            r = math.radians(a)
            b.sphere("M_IconLeaf", M((math.cos(r) * 0.13, -0.12, math.sin(r) * 0.13 + 0.03), (0.12, 0.04, 0.12)), u=10, v=5)

    def mage_hourglass(b):
        b.cone("M_Gold", M((0, 0, 0.3), r=(180, 0, 0)), 0.38, 0.03, 0.55, segments=10)
        b.cone("M_Gold", M((0, 0, -0.3)), 0.38, 0.03, 0.55, segments=10)
        for z in (0.6, -0.6):
            b.cube("M_IconPurple", M((0, 0, z), (0.95, 0.32, 0.12)))
        for x, z in ((-0.62, 0.3), (0.62, -0.25)):
            _star(b, "M_PlayerGlow", (x, -0.2, z), 0.25)

    def vampire_fang(b):
        torus(b, "M_IconSteel", M((0, 0, 0.35)), 0.45, 0.03, segments=24, sides=4, arc=math.pi)
        _fang(b, (-0.17, -0.05, 0.05))
        _fang(b, (0.17, -0.05, 0.05))
        b.sphere("M_IconBlood", M((0.17, -0.1, -0.45), (0.1, 0.08, 0.12)), u=10, v=6)

    def soul_lantern(b):
        for x in (-0.32, 0.32):
            b.cube("M_IconDark", M((x, 0, -0.05), (0.08, 0.08, 0.9)))
        for z in (0.42, -0.52):
            b.cube("M_IconDark", M((0, 0, z), (0.8, 0.2, 0.1)))
        b.sphere("M_PlayerGlow", M((0, 0, -0.05), (0.25, 0.2, 0.3)), u=12, v=8)
        torus(b, "M_IconDark", M((0, 0, 0.55)), 0.18, 0.04, segments=16, sides=4, arc=math.pi)

    def basilisk_eye(b):
        b.sphere("M_IconAmber", M((0, 0, 0), (0.62, 0.3, 0.42)), u=18, v=10)
        b.cube("M_IconDark", M((0, -0.25, 0), (0.1, 0.1, 0.62)))
        for k in range(5):
            a = math.radians(-60 + k * 30)
            b.cone("M_IconLeaf", M((math.sin(a) * 0.72, 0.05, math.cos(a) * 0.55), r=(0, math.degrees(a), 0)), 0.1, 0.0, 0.25, segments=4)

    def bramble_mail(b):
        b.cube("M_IconSteel", M((0, 0, 0.15), (0.9, 0.2, 0.8)))
        b.cone("M_IconSteel", M((0, 0, -0.45), (1, 0.22, 1), r=(180, 0, 45)), 0.64, 0.0, 0.5, segments=4)
        for (x, z, a) in ((-0.55, 0.4, -60), (0.55, 0.4, 60), (-0.5, -0.1, -90), (0.5, -0.1, 90), (0, 0.65, 0)):
            b.cone("M_IconLeaf", M((x, -0.05, z), r=(0, a, 0)), 0.08, 0.0, 0.3, segments=5)
        torus(b, "M_IconLeaf", M((0, -0.12, 0.1)), 0.3, 0.04, segments=16, sides=4)

    def turtle_shell(b):
        b.sphere("M_IconLeaf", M((0, 0, -0.1), (0.75, 0.4, 0.55)), u=18, v=10)
        torus(b, "M_IconAmber", M((0, 0, -0.35), (1, 1, 0.35)), 0.72, 0.06, segments=28, sides=5)
        for x, z in ((0, 0.1), (-0.32, -0.1), (0.32, -0.1)):
            b.cone("M_IconAmber", M((x, -0.33, z), r=(90, 0, 0)), 0.16, 0.16, 0.05, segments=6)
        torus(b, "M_PlayerGlow", M((0, -0.2, 0)), 0.95, 0.025, segments=32, sides=4)

    def sylph_sigil(b):
        torus(b, "M_IconWhite", M(), 0.65, 0.05, segments=32, sides=5)
        for k in range(3):
            torus(b, "M_PlayerGlow", M(r=(0, k * 120, 0)), 0.35, 0.05, segments=16, sides=4, arc=math.pi * 0.9)
        b.sphere("M_IconWhite", M((0, -0.05, 0), (0.1, 0.1, 0.1)), u=8, v=5)

    def minotaur_blood(b):
        b.cone("M_IconGlass", M((0, 0.1, -0.2)), 0.3, 0.18, 0.75, segments=14)
        b.cone("M_IconBlood", M((0, 0.05, -0.3)), 0.28, 0.22, 0.5, segments=14)
        b.cube("M_Wood", M((0, 0.1, 0.25), (0.22, 0.22, 0.14)))
        for side in (-1, 1):
            b.cone("M_IconBone", M((side * 0.45, 0, 0.45), r=(0, side * -55, 0)), 0.11, 0.0, 0.55, segments=6)

    def blood_moon_chalice(b):
        b.sphere("M_IconBlood", M((0.25, 0.3, 0.45), (0.45, 0.1, 0.45)), u=16, v=8)
        b.cone("M_Gold", M((-0.05, 0, 0.05)), 0.4, 0.18, 0.45, segments=16)
        b.cone("M_IconBlood", M((-0.05, -0.01, 0.26)), 0.38, 0.38, 0.04, segments=16)
        b.cube("M_Gold", M((-0.05, 0, -0.3), (0.1, 0.1, 0.3)))
        b.cone("M_Gold", M((-0.05, 0, -0.5)), 0.3, 0.15, 0.1, segments=16)

    def phoenix_feather(b):
        b.cone("M_Gold", M((0.03, -0.06, 0.12), r=(0, 32, 0)), 0.03, 0.03, 1.4, segments=6)
        b.membrane("M_IconOrange", [(0.32, 0, 0.7), (-0.1, 0, 0.45), (-0.4, 0, -0.1), (-0.3, 0, -0.55), (0.0, 0, -0.1), (0.35, 0, 0.3)], thickness=0.04)
        b.membrane("M_IconRed", [(0.32, 0, 0.7), (0.5, 0, 0.3), (0.2, 0, -0.2), (-0.3, 0, -0.55), (0.1, 0, 0.1)], thickness=0.04)
        for x, z, h in ((-0.45, -0.6, 0.35), (-0.2, -0.75, 0.3)):
            b.cone("M_Gold", M((x, -0.1, z)), 0.1, 0.0, h, segments=6)

    def heartstone_core(b):
        b.ico("M_Heartstone", M((0, 0, 0), (0.45, 0.35, 0.6)), subdiv=1)
        torus(b, "M_PlayerGlow", M(), 0.75, 0.04, segments=32, sides=4)
        for k in range(6):
            b.cone("M_PlayerGlow", M(r=(0, k * 60, 0)) @ M((0, 0, 0.92)), 0.05, 0.0, 0.2, segments=4)

    def lifesteal(b):
        _heart(b, "M_IconRed", (0, 0, -0.1), 0.85)
        _fang(b, (-0.2, -0.35, 0.45), 0.8)
        _fang(b, (0.2, -0.35, 0.45), 0.8)

    jobs = {
        "SkyboltCrossbow": skybow, "ChainLightning": storm_jar, "Starfall": grimoire,
        "HeartLocket": dragon_ember, "GiantsRing": giants_ring, "IronHide": troll_cloak, "GaleFeather": griffin_plume,
        "Lodestone": imp_jar, "FairyDust": pixie_pouch, "MendingMoss": plush_bear, "SagesLens": owl_monocle,
        "KrakenTentacle": kraken, "LeechJar": leech_jar, "Whetstone": whetstone, "QuickDraw": pixie_wings,
        "LuckyCoin": clover_coin, "WizardHourglass": mage_hourglass, "VampireFang": vampire_fang,
        "SoulHarvest": soul_lantern, "HuntersEye": basilisk_eye, "ThornMail": bramble_mail, "WardShell": turtle_shell,
        "WindSigil": sylph_sigil, "BerserkerBlood": minotaur_blood, "BloodMoonChalice": blood_moon_chalice,
        "PhoenixFeather": phoenix_feather, "HeartstoneCore": heartstone_core, "Lifesteal": lifesteal,
    }
    for name, build in jobs.items():
        make(name, build)

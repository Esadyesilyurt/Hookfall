# Hookfall - the playable characters besides the Warden (2026-11-08): Skyrunner, Bulwark and Lumen, each with a
# portrait icon. They keep the Warden's part names and joints (Body, Head, Scarf, Arm_L, Arm_R, Leg_L, Leg_R, Cape), so
# WardenAnimator moves all of them. Loaded by hookfall_models.py like hookfall_models_v2.py.
# AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
import math, os

PALETTE_CHARACTERS = {
    "M_ScoutCloth": (0.80, 0.64, 0.40), "M_ScoutLeather": (0.42, 0.27, 0.18), "M_ScoutScarf": (0.96, 0.46, 0.20),
    "M_BulwarkPlate": (0.64, 0.66, 0.72), "M_BulwarkCloth": (0.58, 0.18, 0.20),
    "M_LumenRobe": (0.38, 0.28, 0.64), "M_LumenTrim": (0.96, 0.86, 0.56),
}


def install(ns):
    """Adds the character palette and shares the main script's helpers (and the v2 torus and icon renderer)."""
    ns["PALETTE"].update(PALETTE_CHARACTERS)
    g = globals()
    for name in ("Builder", "M", "reset", "export", "preview", "warden"):
        g[name] = ns[name]
    import hookfall_models_v2
    g["torus"] = hookfall_models_v2.torus
    g["_render_icon"] = hookfall_models_v2._render_icon


def _icons_dir(out_dir):
    path = os.path.join(out_dir, "..", "UI", "Resources", "Icons")
    os.makedirs(path, exist_ok=True)
    return path


def _finish(name, out_dir, prev_dir):
    export(name, out_dir)
    _render_icon(name, _icons_dir(out_dir))   # the portrait on the character select card
    preview(name, prev_dir)


def warden_portrait(out_dir, prev_dir):
    warden(out_dir, prev_dir)
    _render_icon("Warden", _icons_dir(out_dir))


def skyrunner(out_dir, prev_dir):
    """Skyrunner: a slim scout in an aviator cap with goggles, a long orange scarf and a short split cape."""
    reset()
    body = Builder("Body")
    body.cone("M_ScoutCloth", M((0, 0, 1.22)), 0.17, 0.21, 0.5, segments=14)
    body.cone("M_ScoutLeather", M((0, -0.01, 1.3)), 0.18, 0.2, 0.2, segments=14)             # leather vest
    body.cone("M_ScoutLeather", M((0, 0, 0.98)), 0.19, 0.19, 0.08, segments=14)              # belt
    body.cube("M_Gold", M((0, -0.19, 0.98), (0.07, 0.03, 0.06)))
    body.cone("M_ScoutCloth", M((0, 0, 0.78)), 0.24, 0.17, 0.36, segments=14)
    for side in (-1, 1):
        body.sphere("M_ScoutLeather", M((side * 0.23, 0, 1.43), (0.11, 0.11, 0.08)), u=10, v=6)
    body.cube("M_ScoutLeather", M((0.02, -0.04, 1.2), (0.04, 0.38, 0.04), (0, -30, 0)))     # strap
    body.cone("M_PlayerGlow", M((0, -0.22, 1.25), (1, 0.3, 1), (90, 0, 45)), 0.05, 0.05, 0.04, segments=4)
    body.build()

    head = Builder("Head")
    head.sphere("M_WardenSkin", M((0, -0.02, 0.13), (0.11, 0.11, 0.13)), u=10, v=7)
    head.sphere("M_ScoutLeather", M((0, 0.01, 0.18), (0.125, 0.125, 0.11)), u=12, v=7)        # aviator cap
    for side in (-1, 1):
        head.cube("M_ScoutLeather", M((side * 0.11, -0.01, 0.08), (0.03, 0.08, 0.12)))        # ear flaps
        head.cone("M_PlayerGlow", M((side * 0.05, -0.11, 0.2), r=(90, 0, 0)), 0.04, 0.04, 0.03, segments=10)  # goggles
    head.cube("M_ScoutLeather", M((0, -0.1, 0.2), (0.2, 0.02, 0.025)))                         # goggle strap
    head.build(location=(0, 0, 1.52))

    scarf = Builder("Scarf")
    scarf.cone("M_ScoutScarf", M((0, 0, 0)), 0.13, 0.14, 0.09, segments=12)
    scarf.membrane("M_ScoutScarf", [(-0.05, 0.12, 0.0), (0.07, 0.12, 0.0), (0.12, 0.16, -0.85), (-0.01, 0.16, -0.95)], thickness=0.02)
    scarf.build(location=(0, 0, 1.5))

    for side, name in ((-1, "Arm_L"), (1, "Arm_R")):
        arm = Builder(name)
        arm.cone("M_ScoutCloth", M((0, 0, -0.17)), 0.05, 0.047, 0.34, segments=8)
        arm.cone("M_ScoutLeather", M((0, 0, -0.45)), 0.05, 0.045, 0.28, segments=8)
        arm.sphere("M_WardenSkin", M((0, 0, -0.63), (0.05, 0.05, 0.055)), u=8, v=5)
        if side == 1:   # a light hook bracer
            for k in range(2):
                arm.cone("M_PlayerGlow", M((0, 0, -0.4 - k * 0.08)), 0.065, 0.065, 0.022, segments=10)
            arm.cone("M_PlayerGlow", M((0, -0.07, -0.52), r=(90, 0, 0)), 0.035, 0.018, 0.1, segments=6)
        arm.build(location=(side * 0.23, 0, 1.4))

    for side, name in ((-1, "Leg_L"), (1, "Leg_R")):
        leg = Builder(name)
        leg.cone("M_ScoutCloth", M((0, 0, -0.22)), 0.075, 0.062, 0.44, segments=8)
        leg.cone("M_ScoutLeather", M((0, 0, -0.62)), 0.065, 0.06, 0.36, segments=8)
        leg.cube("M_ScoutLeather", M((0, -0.04, -0.85), (0.11, 0.22, 0.09)))
        leg.build(location=(side * 0.1, 0, 0.9))

    cape = Builder("Cape")
    cape.membrane("M_ScoutScarf", [(-0.16, 0, 0), (0.16, 0, 0), (0.2, 0, -0.55), (0.05, 0, -0.62), (0.0, 0, -0.45), (-0.05, 0, -0.62), (-0.2, 0, -0.55)], thickness=0.02)
    cape.build(location=(0, 0.17, 1.45))
    _finish("Skyrunner", out_dir, prev_dir)


def bulwark(out_dir, prev_dir):
    """Bulwark: a broad knight in heavy plate with a crested helmet, huge pauldrons, a red tabard and a tower shield."""
    reset()
    body = Builder("Body")
    body.cone("M_BulwarkPlate", M((0, 0, 1.22)), 0.25, 0.31, 0.52, segments=14)              # cuirass
    body.cone("M_BulwarkCloth", M((0, -0.02, 1.0)), 0.27, 0.27, 0.12, segments=14)           # belt
    body.cube("M_Gold", M((0, -0.28, 1.0), (0.1, 0.03, 0.09)))
    body.cone("M_BulwarkCloth", M((0, 0, 0.74)), 0.33, 0.24, 0.44, segments=14)              # tabard skirt
    body.cube("M_BulwarkCloth", M((0, -0.3, 1.18), (0.16, 0.02, 0.42)))                        # tabard front
    body.cone("M_Gold", M((0, -0.32, 1.25), (1, 0.3, 1), (90, 0, 45)), 0.07, 0.07, 0.03, segments=4)
    for side in (-1, 1):
        body.sphere("M_BulwarkPlate", M((side * 0.33, 0, 1.45), (0.21, 0.21, 0.15)), u=10, v=6)   # pauldrons
        body.sphere("M_BulwarkPlate", M((side * 0.33, 0, 1.36), (0.22, 0.22, 0.07)), u=10, v=4)
        body.cone("M_Gold", M((side * 0.33, 0, 1.53)), 0.05, 0.0, 0.1, segments=6)              # spikes
    body.build()

    head = Builder("Head")
    head.sphere("M_BulwarkPlate", M((0, 0, 0.14), (0.15, 0.15, 0.16)), u=12, v=8)             # great helm
    head.cube("M_TowerDark", M((0, -0.14, 0.15), (0.16, 0.03, 0.025)))                         # visor slit
    head.cube("M_BulwarkCloth", M((0, 0.02, 0.33), (0.04, 0.24, 0.1)))                         # crest
    head.cone("M_PlayerGlow", M((0, -0.155, 0.15), r=(90, 0, 0)), 0.02, 0.02, 0.01, segments=6)
    head.build(location=(0, 0, 1.56))

    scarf = Builder("Scarf")
    scarf.cone("M_BulwarkPlate", M((0, 0, 0)), 0.17, 0.18, 0.09, segments=12)                  # gorget
    scarf.build(location=(0, 0, 1.52))

    for side, name in ((-1, "Arm_L"), (1, "Arm_R")):
        arm = Builder(name)
        arm.cone("M_BulwarkPlate", M((0, 0, -0.17)), 0.08, 0.075, 0.34, segments=8)
        arm.cone("M_BulwarkPlate", M((0, 0, -0.45)), 0.08, 0.072, 0.28, segments=8)
        arm.sphere("M_BulwarkPlate", M((0, 0, -0.64), (0.075, 0.075, 0.07)), u=8, v=5)
        if side == -1:   # tower shield on the free arm
            arm.cube("M_BulwarkCloth", M((-0.08, -0.04, -0.45), (0.06, 0.5, 0.75)))
            arm.cube("M_Gold", M((-0.115, -0.04, -0.45), (0.02, 0.1, 0.55)))
            arm.cube("M_Gold", M((-0.115, -0.04, -0.4), (0.02, 0.4, 0.08)))
        else:            # the hook gauntlet
            arm.cube("M_BulwarkPlate", M((0, 0, -0.45), (0.17, 0.17, 0.26)))
            for k in range(3):
                arm.cone("M_PlayerGlow", M((0, 0, -0.38 - k * 0.07)), 0.1, 0.1, 0.025, segments=10)
        arm.build(location=(side * 0.34, 0, 1.4))

    for side, name in ((-1, "Leg_L"), (1, "Leg_R")):
        leg = Builder(name)
        leg.cone("M_BulwarkPlate", M((0, 0, -0.22)), 0.1, 0.085, 0.44, segments=8)
        leg.cone("M_BulwarkPlate", M((0, 0, -0.62)), 0.085, 0.08, 0.36, segments=8)
        leg.sphere("M_BulwarkPlate", M((0, -0.03, -0.4), (0.08, 0.06, 0.07)), u=6, v=4)
        leg.cube("M_BulwarkPlate", M((0, -0.05, -0.85), (0.15, 0.26, 0.11)))
        leg.build(location=(side * 0.13, 0, 0.9))

    cape = Builder("Cape")
    cape.membrane("M_BulwarkCloth", [(-0.26, 0, 0), (0.26, 0, 0), (0.33, 0, -1.0), (-0.33, 0, -1.0)], thickness=0.025)
    cape.build(location=(0, 0.24, 1.47))
    _finish("Bulwark", out_dir, prev_dir)


def lumen(out_dir, prev_dir):
    """Lumen: a robed mystic of the Heartstone with a tall pointed hood, gold trim and a glowing orb on the hook hand."""
    reset()
    body = Builder("Body")
    body.cone("M_LumenRobe", M((0, 0, 1.22)), 0.18, 0.23, 0.5, segments=14)
    body.cone("M_LumenTrim", M((0, 0, 0.98)), 0.2, 0.2, 0.06, segments=14)                      # sash
    body.cone("M_LumenRobe", M((0, 0, 0.6)), 0.34, 0.2, 0.72, segments=16)                      # long robe
    body.cone("M_LumenTrim", M((0, 0, 0.25)), 0.345, 0.34, 0.04, segments=16)                   # hem
    body.cube("M_LumenTrim", M((0, -0.2, 1.1), (0.05, 0.02, 0.5)))                               # front stripe
    body.cone("M_PlayerGlow", M((0, -0.22, 1.28), (1, 0.3, 1), (90, 0, 45)), 0.07, 0.07, 0.04, segments=4)
    for side in (-1, 1):
        body.sphere("M_LumenRobe", M((side * 0.24, 0, 1.43), (0.12, 0.12, 0.09)), u=10, v=6)
    body.build()

    head = Builder("Head")
    head.sphere("M_WardenSkin", M((0, -0.02, 0.13), (0.1, 0.1, 0.12)), u=10, v=7)
    head.sphere("M_LumenRobe", M((0, 0.02, 0.16), (0.15, 0.15, 0.16)), u=12, v=8)
    head.cone("M_LumenRobe", M((0, 0.06, 0.28), r=(-20, 0, 0)), 0.12, 0.0, 0.42, segments=10)   # tall pointed hood
    for v in head.bm.verts:
        if v.co.y < -0.06 and v.co.z > 0.05 and abs(v.co.x) < 0.1 and v.co.z < 0.24:
            v.co.y = -0.06
    torus(head, "M_LumenTrim", M((0, -0.07, 0.15), (1.0, 1.0, 1.15)), 0.1, 0.012, segments=16, sides=4, arc=math.pi)
    head.build(location=(0, 0, 1.52))

    scarf = Builder("Scarf")
    scarf.cone("M_LumenTrim", M((0, 0, 0)), 0.13, 0.14, 0.06, segments=12)                      # collar
    scarf.membrane("M_LumenTrim", [(-0.05, -0.13, 0.0), (0.05, -0.13, 0.0), (0.04, -0.17, -0.4), (-0.04, -0.17, -0.4)], thickness=0.015)  # stole
    scarf.build(location=(0, 0, 1.5))

    for side, name in ((-1, "Arm_L"), (1, "Arm_R")):
        arm = Builder(name)
        arm.cone("M_LumenRobe", M((0, 0, -0.2)), 0.06, 0.1, 0.42, segments=8)                    # wide sleeve
        arm.cone("M_LumenTrim", M((0, 0, -0.42)), 0.1, 0.1, 0.03, segments=8)
        arm.sphere("M_WardenSkin", M((0, 0, -0.55), (0.05, 0.05, 0.06)), u=8, v=5)
        if side == 1:   # the Heartstone orb that throws the hook
            arm.sphere("M_PlayerGlow", M((0, -0.06, -0.68), (0.09, 0.09, 0.09)), u=10, v=7)
            torus(arm, "M_LumenTrim", M((0, -0.06, -0.68), r=(0, 0, 90)), 0.12, 0.01, segments=16, sides=4)
        arm.build(location=(side * 0.24, 0, 1.4))

    for side, name in ((-1, "Leg_L"), (1, "Leg_R")):
        leg = Builder(name)
        leg.cone("M_LumenRobe", M((0, 0, -0.5)), 0.07, 0.065, 0.6, segments=8)
        leg.cube("M_LumenTrim", M((0, -0.04, -0.85), (0.1, 0.2, 0.08)))
        leg.build(location=(side * 0.1, 0, 0.9))

    cape = Builder("Cape")
    cape.membrane("M_LumenRobe", [(-0.18, 0, 0), (0.18, 0, 0), (0.26, 0, -1.05), (0.0, 0, -1.15), (-0.26, 0, -1.05)], thickness=0.02)
    cape.build(location=(0, 0.2, 1.45))
    _finish("Lumen", out_dir, prev_dir)

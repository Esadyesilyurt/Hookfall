# Hookfall - builds every game model in Blender from code and exports FBX files for Unity.
# Run headless from the project root:
#   blender -b --factory-startup -P Tools/Blender/hookfall_models.py -- <fbx out dir> <preview out dir> [model names...]
# AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
#
# Style (GDD 8.1): chunky, slightly exaggerated, smooth forms for a toon shader; muted stone and moss, enemy purples,
# warm rewards, cyan for everything that belongs to the player. Moving parts (wings, arms, legs, cape, chest lid) are
# separate objects whose origin is the joint, so Unity can animate them procedurally without rigs.
# Budgets (GDD AI Tool Plan): enemies under 5,000 triangles, large islands under 10,000.
# Units: metres, Z up, creatures face -Y (Blender front). Origins: island and tower at the top centre of the walkable
# surface, creatures and props at their feet.
import bpy, bmesh, math, random, sys, os
from mathutils import Vector, Matrix, noise

PALETTE = {
    "M_Grass": (0.42, 0.58, 0.36), "M_Stone": (0.55, 0.52, 0.50), "M_Moss": (0.36, 0.50, 0.30),
    "M_Tower": (0.72, 0.70, 0.66), "M_TowerDark": (0.16, 0.14, 0.20), "M_Wood": (0.48, 0.33, 0.20),
    "M_Heartstone": (0.40, 0.95, 1.00), "M_Crystal": (0.55, 0.70, 1.00), "M_Cloud": (0.96, 0.97, 1.00),
    "M_Bat": (0.42, 0.16, 0.48), "M_Wyvern": (0.30, 0.14, 0.38), "M_WyvernBelly": (0.70, 0.28, 0.62),
    "M_Golem": (0.33, 0.31, 0.34), "M_EnemyEye": (1.00, 0.25, 0.80), "M_ChestWood": (0.45, 0.28, 0.15),
    "M_ChestTrim": (1.00, 0.72, 0.20), "M_CacheGlow": (1.00, 0.85, 0.40), "M_XPGem": (1.00, 0.62, 0.15),
    "M_Gold": (1.00, 0.85, 0.30), "M_WardenCloth": (0.12, 0.42, 0.46), "M_WardenCape": (0.08, 0.28, 0.32),
    "M_WardenArmor": (0.42, 0.45, 0.50), "M_WardenSkin": (0.85, 0.66, 0.52), "M_PlayerGlow": (0.35, 0.95, 1.00),
}


# ---------------------------------------------------------------- scene and mesh helpers

def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def material(name):
    m = bpy.data.materials.get(name)
    if m is None:
        m = bpy.data.materials.new(name)
        r, g, b = PALETTE[name]
        m.diffuse_color = (r, g, b, 1.0)
    return m


class Builder:
    """Accumulates geometry for one object in a bmesh, with named materials."""

    def __init__(self, name):
        self.name = name
        self.bm = bmesh.new()
        self.materials = []

    def mat_index(self, mat):
        if mat not in self.materials:
            self.materials.append(mat)
        return self.materials.index(mat)

    def _finish(self, verts, mat, matrix):
        bmesh.ops.transform(self.bm, matrix=matrix, verts=verts)
        idx = self.mat_index(mat)
        faces = {f for v in verts for f in v.link_faces}
        for f in faces:
            f.material_index = idx
        return verts

    def ico(self, mat, matrix, subdiv=3, radius=1.0):
        res = bmesh.ops.create_icosphere(self.bm, subdivisions=subdiv, radius=radius)
        return self._finish(res["verts"], mat, matrix)

    def sphere(self, mat, matrix, u=12, v=8, radius=1.0):
        res = bmesh.ops.create_uvsphere(self.bm, u_segments=u, v_segments=v, radius=radius)
        return self._finish(res["verts"], mat, matrix)

    def cone(self, mat, matrix, r1, r2, depth, segments=12, caps=True):
        res = bmesh.ops.create_cone(self.bm, cap_ends=caps, cap_tris=False, segments=segments,
                                    radius1=r1, radius2=r2, depth=depth)
        return self._finish(res["verts"], mat, matrix)

    def cube(self, mat, matrix, size=1.0):
        res = bmesh.ops.create_cube(self.bm, size=size)
        return self._finish(res["verts"], mat, matrix)

    def membrane(self, mat, points, thickness=0.03):
        """A double-sided flat shape (wings, cape) from 2D outline points in the XZ plane, given as (x, y, z)."""
        top = [self.bm.verts.new(Vector(p) + Vector((0, -thickness / 2, 0))) for p in points]
        bot = [self.bm.verts.new(Vector(p) + Vector((0, thickness / 2, 0))) for p in points]
        f1 = self.bm.faces.new(top)
        f2 = self.bm.faces.new(list(reversed(bot)))
        idx = self.mat_index(mat)
        for f in (f1, f2):
            f.material_index = idx
        n = len(points)
        for i in range(n):
            j = (i + 1) % n
            side = self.bm.faces.new((top[i], top[j], bot[j], bot[i]))
            side.material_index = idx
        return top + bot

    def displace(self, verts, amp, freq, seed=0, chunky=True, where=None):
        self.bm.normal_update()
        offset = Vector((seed * 13.1, seed * 7.7, seed * 3.3))
        moves = []
        for v in verts:
            if where is not None and not where(v):
                continue
            p = v.co * freq + offset
            if chunky:
                d = noise.voronoi(p, distance_metric='DISTANCE')[0]
                val = min(d[1] - d[0], 0.6) - 0.3       # cells bulge out, cracks sink in
            else:
                val = noise.noise(p)
            moves.append((v, v.normal * (val * amp)))
        for v, m in moves:
            v.co += m

    def build(self, location=(0, 0, 0), parent=None):
        bmesh.ops.triangulate(self.bm, faces=[f for f in self.bm.faces if len(f.verts) > 4])
        self.bm.normal_update()
        mesh = bpy.data.meshes.new(self.name)
        self.bm.to_mesh(mesh)
        self.bm.free()
        for m in self.materials:
            mesh.materials.append(material(m))
        for poly in mesh.polygons:
            poly.use_smooth = True
        ob = bpy.data.objects.new(self.name, mesh)
        bpy.context.scene.collection.objects.link(ob)
        ob.location = location
        if parent is not None:
            ob.parent = parent
        return ob


def M(t=(0, 0, 0), s=(1, 1, 1), r=(0, 0, 0)):
    """Matrix from translation, scale and XYZ rotation in degrees."""
    rot = (Matrix.Rotation(math.radians(r[2]), 4, 'Z') @ Matrix.Rotation(math.radians(r[1]), 4, 'Y')
           @ Matrix.Rotation(math.radians(r[0]), 4, 'X'))
    scale = Matrix.Diagonal((s[0], s[1], s[2], 1.0))
    return Matrix.Translation(Vector(t)) @ rot @ scale


def empty(name, location):
    ob = bpy.data.objects.new(name, None)
    bpy.context.scene.collection.objects.link(ob)
    ob.location = location
    return ob


def triangles():
    return sum(len(p.vertices) - 2 for ob in bpy.context.scene.objects if ob.type == 'MESH' for p in ob.data.polygons)


def export(name, out_dir):
    path = os.path.join(out_dir, name + ".fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=False, object_types={'MESH', 'EMPTY'},
                             apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
                             axis_forward='-Z', axis_up='Y', bake_space_transform=True,
                             mesh_smooth_type='OFF', add_leaf_bones=False, bake_anim=False)
    print(f"[Hookfall] {name}: {triangles()} triangles -> {path}")


def preview(name, out_dir):
    """A quick Workbench render with material colours, for review."""
    scene = bpy.context.scene
    objs = [o for o in scene.objects if o.type == 'MESH']
    lo = Vector((min(min((o.matrix_world @ Vector(c))[i] for c in o.bound_box) for o in objs) for i in range(3)))
    hi = Vector((max(max((o.matrix_world @ Vector(c))[i] for c in o.bound_box) for o in objs) for i in range(3)))
    centre, size = (lo + hi) / 2, max((hi - lo).length, 0.5)
    cam_data = bpy.data.cameras.new("Preview")
    cam_data.lens = 50
    cam = bpy.data.objects.new("Preview", cam_data)
    scene.collection.objects.link(cam)
    direction = Vector((0.9, -1.4, 0.75)).normalized()
    cam.location = centre + direction * size * 1.5
    cam.rotation_euler = (centre - cam.location).to_track_quat('-Z', 'Y').to_euler()
    scene.camera = cam
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.display.shading.light = 'STUDIO'
    scene.display.shading.color_type = 'MATERIAL'
    scene.display.shading.show_cavity = True
    scene.display.shading.show_object_outline = True
    scene.render.resolution_x, scene.render.resolution_y = 640, 480
    scene.render.filepath = os.path.join(out_dir, name + ".png")
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam)


# ---------------------------------------------------------------- islands

def island(name, radius, depth, seed, segments, boulders, out_dir, prev_dir):
    """Flat grassy top with a rounded lip, chunky rock underside tapering to a point (GDD 8.1, mood board 1-2)."""
    reset()
    rnd = random.Random(seed)
    b = Builder(name)
    bm = b.bm
    phase = [rnd.uniform(0, 6.28) for _ in range(4)]

    def outline(a):
        return 1 + 0.09 * math.sin(3 * a + phase[0]) + 0.05 * math.sin(5 * a + phase[1]) + 0.03 * math.sin(9 * a + phase[2])

    # (z, radius factor, material, jitter): top surface, grass lip, grass edge, then the rock rings.
    rings = [(0.0, 0.55, "M_Grass", 0), (0.0, 1.0, "M_Grass", 0), (-0.22, 1.035, "M_Grass", 0), (-0.55, 1.0, "M_Grass", 0)]
    rock_rings = 9
    for i in range(1, rock_rings + 1):
        t = i / rock_rings
        rings.append((-0.55 - t * depth, max(0.05, 0.97 * (1 - t ** 1.4) ** 0.9), "M_Stone", 0.08))

    loops = []
    for z, f, _, jitter in rings:
        loop = []
        for s in range(segments):
            a = s / segments * math.tau
            r = radius * f * outline(a) * (1 + rnd.uniform(-jitter, jitter))
            loop.append(bm.verts.new((math.cos(a) * r, math.sin(a) * r, z)))
        loops.append(loop)
    centre = bm.verts.new((0, 0, 0))
    bottom = bm.verts.new((rnd.uniform(-0.3, 0.3) * radius * 0.2, 0, -0.55 - depth - 0.4))

    grass, stone = b.mat_index("M_Grass"), b.mat_index("M_Stone")
    for s in range(segments):
        n = (s + 1) % segments
        bm.faces.new((centre, loops[0][s], loops[0][n])).material_index = grass
    for k in range(len(loops) - 1):
        mat = grass if rings[k + 1][2] == "M_Grass" else stone
        for s in range(segments):
            n = (s + 1) % segments
            bm.faces.new((loops[k][s], loops[k + 1][s], loops[k + 1][n], loops[k][n])).material_index = mat
    for s in range(segments):
        n = (s + 1) % segments
        bm.faces.new((loops[-1][s], bottom, loops[-1][n])).material_index = stone

    # Chunky rock: displace only below the grass edge so the top stays flat and walkable.
    b.displace(list(bm.verts), amp=radius * 0.16, freq=1.6 / radius, seed=seed, where=lambda v: v.co.z < -0.7)

    # Boulders hugging the underside break up the silhouette.
    for i in range(boulders):
        a = rnd.uniform(0, math.tau)
        size = radius * rnd.uniform(0.22, 0.38)
        # Deep enough that the boulder (plus its displacement) stays below the grass, so the top stays clean.
        t = max(rnd.uniform(0.15, 0.6), (size * 1.15 + 0.4) / depth)
        r = radius * (0.97 * (1 - t ** 1.4) ** 0.9) * rnd.uniform(0.7, 0.9)
        verts = b.ico("M_Stone", M((math.cos(a) * r, math.sin(a) * r, -0.55 - t * depth), (size, size, size * 0.85),
                                   (rnd.uniform(0, 90), rnd.uniform(0, 90), 0)))
        b.displace(verts, amp=size * 0.35, freq=2.2 / size, seed=seed + i + 1)

    b.build()
    export(name, out_dir)
    preview(name, prev_dir)


# ---------------------------------------------------------------- tower and Heartstone

def tower(out_dir, prev_dir):
    """Round stone archer tower with bands, slit windows, battlements and a wooden top deck at 18 m (mood board 4),
    and the Heartstone glowing in an arch at its foot (mood board 5)."""
    reset()
    b = Builder("Tower")
    h = 18.0
    b.cone("M_Tower", M((0, 0, 0.6)), 3.7, 3.5, 1.2, segments=28)                      # plinth
    b.cone("M_Tower", M((0, 0, 1.2 + (h - 2.2) / 2)), 3.2, 2.95, h - 2.2, segments=28)  # body
    for z in (6.0, 11.5):
        b.cone("M_Tower", M((0, 0, z)), 3.25 - z * 0.012, 3.22 - z * 0.012, 0.35, segments=28)  # stone bands
    b.cone("M_Tower", M((0, 0, h - 0.5)), 3.0, 3.45, 1.0, segments=28)                 # corbelled top
    b.cone("M_Wood", M((0, 0, h - 0.04)), 3.05, 3.05, 0.08, segments=28)               # wooden deck
    # Parapet ring and merlons.
    for i in range(14):
        a = i / 14 * math.tau
        b.cube("M_Tower", M((math.cos(a) * 3.25, math.sin(a) * 3.25, h + 0.35), (0.55, 0.35, 0.7), (0, 0, math.degrees(a) + 90)))
        if i % 2 == 0:
            b.cube("M_Tower", M((math.cos(a) * 3.25, math.sin(a) * 3.25, h + 1.0), (0.6, 0.4, 0.65), (0, 0, math.degrees(a) + 90)))
    # Slit windows.
    for z, offset in ((8.5, 0), (13.5, 30), (16.0, 60)):
        for k in range(3):
            a = math.radians(offset + k * 120 + 60)
            r = 3.0 - z * 0.012
            b.cube("M_TowerDark", M((math.cos(a) * r, math.sin(a) * r, z), (0.25, 0.4, 1.3), (0, 0, math.degrees(a) + 90)))
    # Arch at the front (-Y) where the Heartstone sits.
    b.cube("M_TowerDark", M((0, -3.12, 2.6), (2.2, 0.3, 2.8)))
    b.cone("M_TowerDark", M((0, -3.12, 4.0), (1.0, 1.0, 1.0), (90, 0, 0)), 1.1, 1.1, 0.3, segments=16)
    b.build()

    heart = Builder("Heartstone")
    heart.cone("M_Heartstone", M((0, 0, 0.55)), 0.45, 0.0, 1.1, segments=6)
    heart.cone("M_Heartstone", M((0, 0, -0.35), r=(180, 0, 0)), 0.45, 0.0, 0.7, segments=6)
    for i in range(5):
        a = i / 5 * math.tau
        heart.cone("M_Crystal", M((math.cos(a) * 0.75, math.sin(a) * 0.35, -0.6 + (i % 2) * 0.3), r=(0, 25, math.degrees(a))),
                   0.12, 0.0, 0.5, segments=6)
    heart.build(location=(0, -3.6, 2.4))
    empty("TowerTop", (0, 0, h))
    export("Tower", out_dir)
    preview("Tower", prev_dir)


# ---------------------------------------------------------------- props and clouds

def rock(name, seed, out_dir, prev_dir):
    reset()
    rnd = random.Random(seed)
    b = Builder(name)
    verts = b.ico("M_Stone", M((0, 0, 0.45), (1.0, 0.8, 0.65), (0, 0, rnd.uniform(0, 90))), subdiv=2, radius=0.9)
    b.displace(verts, amp=0.35, freq=1.8, seed=seed)
    b.bm.normal_update()
    moss = b.mat_index("M_Moss")
    for f in b.bm.faces:
        if f.normal.z > 0.75:
            f.material_index = moss
    b.build()
    export(name, out_dir)
    preview(name, prev_dir)


def ruin(out_dir, prev_dir):
    """Broken pillars from the fallen fortress."""
    reset()
    b = Builder("Ruin")
    b.cube("M_Tower", M((0, 0, 0.15), (2.6, 1.4, 0.3)))
    for x, height in ((-0.8, 2.6), (0.8, 1.5)):
        b.cone("M_Tower", M((x, 0, 0.3 + height / 2)), 0.32, 0.3, height, segments=10)
        b.cube("M_Tower", M((x, 0, 0.3 + height + 0.1), (0.8, 0.8, 0.2), (0, 0, 15)))
    verts = b.cone("M_Tower", M((0.4, 0.9, 0.35), r=(0, 90, 20)), 0.3, 0.3, 1.2, segments=10)
    b.displace(verts, amp=0.06, freq=3.0, seed=4)
    b.build()
    export("Ruin", out_dir)
    preview("Ruin", prev_dir)


def crystals(out_dir, prev_dir):
    reset()
    rnd = random.Random(11)
    b = Builder("Crystals")
    for i in range(6):
        length = rnd.uniform(0.6, 1.5)
        tilt = (rnd.uniform(-25, 25), rnd.uniform(-25, 25), rnd.uniform(0, 60))
        base = (rnd.uniform(-0.35, 0.35), rnd.uniform(-0.35, 0.35), 0)
        mat = "M_Crystal" if i % 2 else "M_Heartstone"
        b.cone(mat, M(base, r=tilt) @ M((0, 0, length / 2)), 0.16, 0.16, length, segments=6)
        b.cone(mat, M(base, r=tilt) @ M((0, 0, length + 0.15)), 0.16, 0.0, 0.3, segments=6)
    b.cone("M_Stone", M((0, 0, 0.1)), 0.7, 0.5, 0.25, segments=10)
    b.build()
    export("Crystals", out_dir)
    preview("Crystals", prev_dir)


def cloud(name, seed, out_dir, prev_dir):
    """Puffy, flat-bottomed cloud for the hard-edged toon sky (mood board 3). About 10 m across at scale 1."""
    reset()
    rnd = random.Random(seed)
    b = Builder(name)
    for i in range(rnd.randint(6, 9)):
        r = rnd.uniform(1.2, 2.4)
        b.sphere("M_Cloud", M((rnd.uniform(-4, 4), rnd.uniform(-1.5, 1.5), rnd.uniform(0, 1.2)), (r, r, r * 0.8)), u=14, v=8)
    for v in b.bm.verts:
        if v.co.z < 0:
            v.co.z *= 0.15
    b.build()
    export(name, out_dir)
    preview(name, prev_dir)


# ---------------------------------------------------------------- enemies

def bat(out_dir, prev_dir):
    """Small horde flyer: long ears, wide wings, purple body (mood board 7). Faces -Y."""
    reset()
    b = Builder("Body")
    b.sphere("M_Bat", M((0, 0, 0), (0.3, 0.36, 0.28)), u=10, v=7)
    b.sphere("M_Bat", M((0, -0.32, 0.08), (0.2, 0.2, 0.19)), u=10, v=7)
    for side in (-1, 1):
        b.cone("M_Bat", M((side * 0.1, -0.3, 0.3), r=(-10, side * 20, 0)), 0.07, 0.0, 0.28, segments=5)
        b.sphere("M_EnemyEye", M((side * 0.08, -0.49, 0.12), (0.045, 0.03, 0.045)), u=6, v=4)
    b.cone("M_Bat", M((0, 0.3, -0.05), r=(-80, 0, 0)), 0.1, 0.0, 0.25, segments=5)
    b.build()
    for side, name in ((-1, "Wing_L"), (1, "Wing_R")):
        w = Builder(name)
        pts = [(0, 0, 0.05), (side * 0.45, 0, 0.18), (side * 0.95, 0, 0.12), (side * 0.75, 0, -0.08),
               (side * 0.55, 0, -0.02), (side * 0.38, 0, -0.14), (side * 0.2, 0, -0.06), (0, 0, -0.1)]
        w.membrane("M_Bat", [(x, y - 0.02, z) for x, y, z in pts], thickness=0.025)
        w.build(location=(side * 0.22, 0.02, 0.06))
    export("Bat", out_dir)
    preview("Bat", prev_dir)


def wyvern(out_dir, prev_dir):
    """Heavy flyer: lean body, long neck and tail, big wings, purple with magenta belly (mood board 8). Faces -Y."""
    reset()
    b = Builder("Body")
    b.sphere("M_Wyvern", M((0, 0, 0), (0.55, 1.1, 0.5)), u=14, v=9)
    b.sphere("M_WyvernBelly", M((0, -0.1, -0.18), (0.42, 0.9, 0.32)), u=12, v=7)
    b.cone("M_Wyvern", M((0, -1.15, 0.25), r=(-70, 0, 0)), 0.32, 0.2, 1.0, segments=10)        # neck
    b.sphere("M_Wyvern", M((0, -1.75, 0.45), (0.3, 0.42, 0.26)), u=12, v=8)                     # head
    b.cone("M_Wyvern", M((0, -2.2, 0.38), r=(-90, 0, 0)), 0.18, 0.05, 0.5, segments=8)          # snout
    for side in (-1, 1):
        b.cone("M_WyvernBelly", M((side * 0.16, -1.6, 0.72), r=(30, side * 25, 0)), 0.07, 0.0, 0.45, segments=6)  # horns
        b.sphere("M_EnemyEye", M((side * 0.17, -1.98, 0.52), (0.06, 0.05, 0.05)), u=6, v=4)
        b.cone("M_Wyvern", M((side * 0.3, -0.3, -0.55), r=(10, 0, 0)), 0.1, 0.06, 0.5, segments=6)  # legs
        b.cone("M_WyvernBelly", M((side * 0.3, -0.36, -0.85), r=(-60, 0, 0)), 0.08, 0.0, 0.25, segments=5)
    # Tail: tapering segments curving down, with a spade tip.
    for i in range(5):
        t = i / 5
        b.cone("M_Wyvern", M((0, 1.0 + i * 0.55, -0.05 - t * t * 0.6), r=(-90 + t * 20, 0, 0)), 0.3 * (1 - t) + 0.05, 0.3 * (1 - t - 0.2) + 0.04, 0.6, segments=8)
    b.cone("M_WyvernBelly", M((0, 3.85, -0.62), (1, 1, 0.3), (-90, 0, 0)), 0.32, 0.0, 0.5, segments=4)
    b.displace(list(b.bm.verts), amp=0.04, freq=3.0, seed=2, chunky=False)
    b.build()
    for side, name in ((-1, "Wing_L"), (1, "Wing_R")):
        w = Builder(name)
        w.cone("M_Wyvern", M((side * 1.0, 0, 0.1), r=(0, 90, side * 6)), 0.08, 0.05, 2.0, segments=6)   # arm bone
        pts = [(0, 0, 0.12), (side * 1.9, 0, 0.32), (side * 3.4, 0, 0.1), (side * 2.9, 0, -0.45),
               (side * 2.2, 0, -0.3), (side * 1.7, 0, -0.85), (side * 1.0, 0, -0.55), (side * 0.4, 0, -0.9), (0, 0, -0.6)]
        w.membrane("M_WyvernBelly", pts, thickness=0.04)
        w.build(location=(side * 0.42, -0.35, 0.3))
    export("Wyvern", out_dir)
    preview("Wyvern", prev_dir)


def golem(out_dir, prev_dir):
    """Fallen guardian: stacked mossy boulders with glowing magenta eyes (mood board 6). About 3.4 m tall, faces -Y."""
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
                if f.normal.z > 0.7:
                    f.material_index = moss

    for side in (-1, 1):
        boulder((side * 0.5, 0, 0.45), (0.5, 0.5, 0.55), 10 + side, mossy=False)               # legs
    boulder((0, 0, 1.2), (0.85, 0.65, 0.5), 3)                                                   # pelvis
    boulder((0, 0, 2.15), (1.15, 0.85, 0.85), 4)                                                 # torso
    boulder((0, -0.15, 3.15), (0.5, 0.45, 0.42), 5)                                              # head
    for side in (-1, 1):
        b.cube("M_EnemyEye", M((side * 0.18, -0.55, 3.2), (0.16, 0.05, 0.09)))
    b.build()
    for side, name in ((-1, "Arm_L"), (1, "Arm_R")):
        a = Builder(name)
        for i, (z, s) in enumerate(((-0.25, 0.42), (-0.85, 0.36), (-1.4, 0.45))):
            verts = a.ico("M_Golem", M((side * 0.1, 0, z), (s, s, s * 1.1), (rnd.uniform(0, 40), 0, 0)))
            a.displace(verts, amp=s * 0.3, freq=1.6 / s, seed=20 + i + side)
        a.build(location=(side * 1.3, 0, 2.55))
    export("Golem", out_dir)
    preview("Golem", prev_dir)


def boulder_model(out_dir, prev_dir):
    reset()
    b = Builder("Boulder")
    verts = b.ico("M_Golem", M((0, 0, 0)), subdiv=2, radius=0.6)
    b.displace(verts, amp=0.22, freq=2.6, seed=9)
    b.build()
    export("Boulder", out_dir)
    preview("Boulder", prev_dir)


# ---------------------------------------------------------------- chests and pickups

def chest(out_dir, prev_dir):
    """Wooden chest with gold bands; the lid is its own object hinged at the back. Faces -Y."""
    reset()
    b = Builder("Base")
    b.cube("M_ChestWood", M((0, 0, 0.32), (1.2, 0.8, 0.64)))
    for x in (-0.42, 0.42):
        b.cube("M_ChestTrim", M((x, 0, 0.32), (0.12, 0.84, 0.66)))
    b.cube("M_ChestTrim", M((0, -0.41, 0.52), (0.2, 0.06, 0.22)))                              # lock plate
    b.build()
    lid = Builder("Lid")
    lid.cone("M_ChestWood", M((0, -0.4, 0.0), (1, 1, 1), (0, 90, 0)), 0.4, 0.4, 1.2, segments=12)
    for x in (-0.42, 0.42):
        lid.cone("M_ChestTrim", M((x, -0.4, 0.0), (1, 1, 1), (0, 90, 0)), 0.42, 0.42, 0.12, segments=12)
    for v in lid.bm.verts:   # keep only the top half of the round lid
        if v.co.z < 0:
            v.co.z = 0
    lid.build(location=(0, 0.4, 0.64))
    export("Chest", out_dir)
    preview("Chest", prev_dir)


def tower_cache(out_dir, prev_dir):
    """The rare tower cache: gold box with a glowing gem."""
    reset()
    b = Builder("Cache")
    b.cube("M_ChestTrim", M((0, 0, 0.45), (1.4, 1.0, 0.9)))
    b.cube("M_ChestWood", M((0, 0, 0.45), (1.3, 1.04, 0.5)))
    b.cone("M_ChestTrim", M((0, 0, 0.95), (1, 1, 1), (0, 0, 45)), 0.75, 0.45, 0.2, segments=4)
    b.cone("M_CacheGlow", M((0, 0, 1.4)), 0.25, 0.0, 0.5, segments=6)
    b.cone("M_CacheGlow", M((0, 0, 1.05), r=(180, 0, 0)), 0.25, 0.0, 0.25, segments=6)
    b.build()
    export("TowerCache", out_dir)
    preview("TowerCache", prev_dir)


def gem(out_dir, prev_dir):
    reset()
    b = Builder("Gem")
    b.cone("M_XPGem", M((0, 0, 0.18)), 0.22, 0.0, 0.36, segments=6)
    b.cone("M_XPGem", M((0, 0, -0.14), r=(180, 0, 0)), 0.22, 0.0, 0.28, segments=6)
    b.build()
    export("XPGem", out_dir)
    preview("XPGem", prev_dir)


def coin(out_dir, prev_dir):
    """Stands upright (face along Y) so it can spin around the vertical axis."""
    reset()
    b = Builder("Coin")
    b.cone("M_Gold", M((0, 0, 0), r=(90, 0, 0)), 0.25, 0.25, 0.06, segments=16)
    b.cone("M_ChestTrim", M((0, 0, 0), r=(90, 0, 0)), 0.15, 0.15, 0.08, segments=16)
    b.build()
    export("Coin", out_dir)
    preview("Coin", prev_dir)


# ---------------------------------------------------------------- the Warden

def warden(out_dir, prev_dir):
    """The Warden: hooded warrior of the Order with a cyan hook gauntlet. 1.8 m, feet at 0, faces -Y.
    Parts: Body, Head (neck pivot), Arm_L/Arm_R (shoulder pivots), Leg_L/Leg_R (hip pivots), Cape (neck pivot)."""
    reset()
    body = Builder("Body")
    body.cone("M_WardenCloth", M((0, 0, 1.22)), 0.2, 0.25, 0.5, segments=12)                  # chest
    body.cone("M_WardenArmor", M((0, 0, 0.98)), 0.21, 0.21, 0.08, segments=12)                 # belt
    body.cone("M_WardenCloth", M((0, 0, 0.72)), 0.3, 0.19, 0.48, segments=12)                  # tunic skirt
    for side in (-1, 1):
        body.sphere("M_WardenArmor", M((side * 0.27, 0, 1.43), (0.14, 0.14, 0.1)), u=10, v=6)  # pauldrons
    body.cone("M_PlayerGlow", M((0, -0.24, 1.25), (1, 0.3, 1), (90, 0, 45)), 0.07, 0.07, 0.04, segments=4)  # chest rune
    body.build()

    head = Builder("Head")
    head.sphere("M_WardenSkin", M((0, -0.02, 0.13), (0.11, 0.11, 0.13)), u=10, v=7)
    head.sphere("M_WardenCloth", M((0, 0.02, 0.16), (0.15, 0.15, 0.16)), u=12, v=8)            # hood
    head.cone("M_WardenCloth", M((0, 0.14, 0.22), r=(-60, 0, 0)), 0.1, 0.0, 0.22, segments=8)  # hood tip
    for v in head.bm.verts:   # open the hood at the face
        if v.co.y < -0.06 and v.co.z > 0.05 and abs(v.co.x) < 0.1 and v.co.z < 0.24:
            v.co.y = -0.06
    head.build(location=(0, 0, 1.52))

    for side, name in ((-1, "Arm_L"), (1, "Arm_R")):
        arm = Builder(name)
        arm.cone("M_WardenCloth", M((0, 0, -0.17)), 0.06, 0.055, 0.34, segments=8)
        arm.cone("M_WardenArmor", M((0, 0, -0.45)), 0.055, 0.05, 0.28, segments=8)
        arm.sphere("M_WardenSkin", M((0, 0, -0.63), (0.055, 0.055, 0.06)), u=8, v=5)
        if side == 1:   # the hook gauntlet
            arm.cube("M_WardenArmor", M((0, 0, -0.45), (0.13, 0.13, 0.22)))
            arm.cone("M_PlayerGlow", M((0, -0.07, -0.5), r=(90, 0, 0)), 0.04, 0.02, 0.1, segments=6)
        arm.build(location=(side * 0.27, 0, 1.4))

    for side, name in ((-1, "Leg_L"), (1, "Leg_R")):
        leg = Builder(name)
        leg.cone("M_WardenCloth", M((0, 0, -0.22)), 0.085, 0.07, 0.44, segments=8)
        leg.cone("M_WardenArmor", M((0, 0, -0.62)), 0.07, 0.065, 0.36, segments=8)
        leg.cube("M_WardenArmor", M((0, -0.04, -0.85), (0.12, 0.22, 0.1)))
        leg.build(location=(side * 0.11, 0, 0.9))

    cape = Builder("Cape")
    cape.membrane("M_WardenCape", [(-0.2, 0, 0), (0.2, 0, 0), (0.28, 0, -0.95), (0.0, 0, -1.05), (-0.28, 0, -0.95)], thickness=0.02)
    cape.build(location=(0, 0.2, 1.45))
    export("Warden", out_dir)
    preview("Warden", prev_dir)


def hook_head(out_dir, prev_dir):
    """The thrown hook: a small three-pronged grapple pointing along -Y (its direction of travel)."""
    reset()
    b = Builder("Hook")
    b.cone("M_WardenArmor", M((0, 0, 0), r=(90, 0, 0)), 0.06, 0.06, 0.4, segments=8)
    for i in range(3):
        a = i / 3 * 360
        b.cone("M_PlayerGlow", M((0, -0.2, 0), r=(0, 0, a)) @ M((0, 0, 0.12), r=(-40, 0, 0)), 0.035, 0.0, 0.25, segments=5)
    b.build()
    export("Hook", out_dir)
    preview("Hook", prev_dir)


# ---------------------------------------------------------------- main

def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    out_dir = os.path.abspath(argv[0]) if argv else os.path.abspath("Assets/_Hookfall/Art/Models")
    prev_dir = os.path.abspath(argv[1]) if len(argv) > 1 else os.path.join(out_dir, "..", "..", "..", "..", "Docs", "ModelPreviews")
    wanted = set(argv[2:])
    os.makedirs(out_dir, exist_ok=True)
    os.makedirs(prev_dir, exist_ok=True)

    jobs = {
        "Island_Large_1": lambda: island("Island_Large_1", 7.0, 8.5, 101, 48, 4, out_dir, prev_dir),
        "Island_Large_2": lambda: island("Island_Large_2", 7.0, 10.5, 202, 48, 3, out_dir, prev_dir),
        "Island_Large_3": lambda: island("Island_Large_3", 7.0, 7.0, 303, 48, 5, out_dir, prev_dir),
        "Island_Small_1": lambda: island("Island_Small_1", 3.0, 5.0, 404, 28, 2, out_dir, prev_dir),
        "Island_Small_2": lambda: island("Island_Small_2", 3.0, 6.5, 505, 28, 1, out_dir, prev_dir),
        "Island_Small_3": lambda: island("Island_Small_3", 3.0, 4.2, 606, 28, 3, out_dir, prev_dir),
        "Tower": lambda: tower(out_dir, prev_dir),
        "Rock_1": lambda: rock("Rock_1", 21, out_dir, prev_dir),
        "Rock_2": lambda: rock("Rock_2", 22, out_dir, prev_dir),
        "Ruin": lambda: ruin(out_dir, prev_dir),
        "Crystals": lambda: crystals(out_dir, prev_dir),
        "Cloud_1": lambda: cloud("Cloud_1", 31, out_dir, prev_dir),
        "Cloud_2": lambda: cloud("Cloud_2", 32, out_dir, prev_dir),
        "Cloud_3": lambda: cloud("Cloud_3", 33, out_dir, prev_dir),
        "Bat": lambda: bat(out_dir, prev_dir),
        "Wyvern": lambda: wyvern(out_dir, prev_dir),
        "Golem": lambda: golem(out_dir, prev_dir),
        "Boulder": lambda: boulder_model(out_dir, prev_dir),
        "Chest": lambda: chest(out_dir, prev_dir),
        "TowerCache": lambda: tower_cache(out_dir, prev_dir),
        "XPGem": lambda: gem(out_dir, prev_dir),
        "Coin": lambda: coin(out_dir, prev_dir),
        "Warden": lambda: warden(out_dir, prev_dir),
        "Hook": lambda: hook_head(out_dir, prev_dir),
    }
    for name, job in jobs.items():
        if not wanted or name in wanted:
            job()


main()

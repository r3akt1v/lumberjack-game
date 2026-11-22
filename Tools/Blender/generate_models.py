import bpy
import math
import os
from mathutils import Vector

OUT_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "Models", "Generated")
os.makedirs(OUT_DIR, exist_ok=True)

def clean_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)

def mat(name, color, rough=0.8):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = next((n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
    if bsdf is None:
        bsdf = m.node_tree.nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Roughness"].default_value = rough
    return m

def assign(obj, material):
    obj.data.materials.append(material)

def finish(name):
    """Apply transforms and export selected objects to FBX."""
    bpy.ops.object.select_all(action='DESELECT')
    objs = [o for o in bpy.context.scene.objects if o.type == 'MESH' or o.type == 'EMPTY']
    # apply modifiers/transforms on meshes
    for o in objs:
        if o.type == 'MESH':
            o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0] if objs else None
    if objs:
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    path = os.path.join(OUT_DIR, name + ".fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=False,
                             apply_scale_options='FBX_SCALE_NONE',
                             global_scale=1.0, bake_anim=False)
    clean_scene()

def box(name, loc, scale, material, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(location=loc)
    o = bpy.context.active_object
    o.name = name
    o.scale = (scale[0]/2, scale[1]/2, scale[2]/2)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel > 0:
        mod = o.modifiers.new("bev", 'BEVEL')
        mod.width = bevel
        mod.segments = 1
    assign(o, material)
    return o

def cyl(name, loc, radius, depth, material, vertices=8, rot=(0,0,0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rot)
    o = bpy.context.active_object
    o.name = name
    assign(o, material)
    return o

def cone(name, loc, r1, r2, depth, material, vertices=7, rot=(0,0,0)):
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=r1, radius2=r2, depth=depth, location=loc, rotation=rot)
    o = bpy.context.active_object
    o.name = name
    assign(o, material)
    return o

def ico(name, loc, radius, material, scale=(1,1,1), subdiv=1):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdiv, radius=radius, location=loc)
    o = bpy.context.active_object
    o.name = name
    o.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    # random-ish vertex jitter for rock look
    for v in o.data.vertices:
        v.co.x *= 1.0 + (hash(v.co.x*13.7) % 100 - 50) * 0.004
        v.co.y *= 1.0 + (hash(v.co.y*7.1) % 100 - 50) * 0.004
        v.co.z *= 1.0 + (hash(v.co.z*3.3) % 100 - 50) * 0.004
    assign(o, material)
    return o

def uv(name, loc, scale, material):
    bpy.ops.mesh.primitive_plane_add(size=1, location=loc, rotation=(math.radians(90), 0, 0))
    o = bpy.context.active_object
    o.name = name
    o.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    assign(o, material)
    return o

# ---------------- TREE ----------------
def make_tree():
    bark = mat("Bark", (0.35, 0.22, 0.12), 0.9)
    leaves = mat("Leaves", (0.18, 0.42, 0.16), 0.85)
    leaves2 = mat("Leaves2", (0.24, 0.5, 0.18), 0.85)
    trunk_h = 2.2
    cyl("Trunk", (0, trunk_h/2, 0), 0.28, trunk_h, bark, vertices=7)
    # root flare
    cone("Roots", (0, 0.15, 0), 0.5, 0.28, 0.5, bark, vertices=7)
    layers = [(1.9, 1.6, 2.6), (1.5, 1.3, 2.2), (1.0, 1.0, 1.9), (0.55, 0.55, 1.5)]
    z = trunk_h - 0.4
    for i, (r, r2, h) in enumerate(layers):
        m = leaves if i % 2 == 0 else leaves2
        cone(f"Layer{i}", (0, z + h/2, 0), r, r2, h, m, vertices=8)
        z += h * 0.62
    cone("Top", (0, z + 0.5, 0), 0.35, 0.0, 1.2, leaves2, vertices=7)
    finish("tree_pine")

# ---------------- STUMP ----------------
def make_stump():
    bark = mat("Bark", (0.4, 0.28, 0.16), 0.9)
    top = mat("Cut", (0.75, 0.6, 0.4), 0.85)
    cyl("Stump", (0, 0.25, 0), 0.32, 0.5, bark, vertices=8)
    cyl("CutTop", (0, 0.51, 0), 0.3, 0.03, top, vertices=8)
    finish("stump")

# ---------------- LOG ----------------
def make_log():
    bark = mat("Bark", (0.38, 0.25, 0.14), 0.9)
    cut = mat("Cut", (0.78, 0.62, 0.42), 0.85)
    cyl("Log", (0, 0.35, 0), 0.32, 1.4, bark, vertices=9, rot=(0, math.radians(90), 0))
    # cut ends
    bpy.ops.mesh.primitive_cylinder_add(vertices=9, radius=0.3, depth=0.04, location=(-0.71, 0.35, 0), rotation=(0, math.radians(90), 0))
    o = bpy.context.active_object; o.name = "CutL"; assign(o, cut)
    bpy.ops.mesh.primitive_cylinder_add(vertices=9, radius=0.3, depth=0.04, location=(0.71, 0.35, 0), rotation=(0, math.radians(90), 0))
    o = bpy.context.active_object; o.name = "CutR"; assign(o, cut)
    finish("log")

# ---------------- PLANK ----------------
def make_plank():
    wood = mat("Wood", (0.72, 0.52, 0.3), 0.85)
    box("Plank", (0, 0.06, 0), (1.6, 0.12, 0.35), wood, bevel=0.02)
    finish("plank")

# ---------------- ROCK ----------------
def make_rock():
    rock = mat("Rock", (0.5, 0.5, 0.52), 0.95)
    ico("Rock", (0, 0.4, 0), 0.8, rock, scale=(1.2, 0.8, 1.0), subdiv=1)
    finish("rock")

# ---------------- GRASS TUFT ----------------
def make_grass():
    g1 = mat("GrassA", (0.35, 0.6, 0.22), 0.9)
    g2 = mat("GrassB", (0.45, 0.68, 0.25), 0.9)
    for i, ang in enumerate([0, math.radians(60), math.radians(120)]):
        uv(f"Blade{i}", (0, 0.35, 0), (0.12, 0.7, 1), g1 if i % 2 == 0 else g2)
        bpy.context.active_object.rotation_euler[2] = ang
        # rotate around Y instead: fix by rotating plane around its local Z after tilt
    # simpler: rebuild blades as tilted planes
    for o in list(bpy.context.scene.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for i, ang in enumerate([0, math.radians(60), math.radians(120)]):
        bpy.ops.mesh.primitive_plane_add(size=1, location=(0, 0.35, 0))
        o = bpy.context.active_object
        o.name = f"Blade{i}"
        o.scale = (0.09, 0.7, 1)
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        # tilt 15 deg and spin around Y
        o.rotation_euler = (math.radians(12), ang, 0)
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
        assign(o, g1 if i % 2 == 0 else g2)
    finish("grass_tuft")

# ---------------- FLOWER ----------------
def make_flower():
    stem = mat("Stem", (0.3, 0.55, 0.2), 0.9)
    petal = mat("Petal", (0.9, 0.5, 0.6), 0.7)
    cyl("Stem", (0, 0.25, 0), 0.02, 0.5, stem, vertices=5)
    ico("Head", (0, 0.55, 0), 0.09, petal, subdiv=1)
    finish("flower")

# ---------------- AXE ----------------
def make_axe():
    wood = mat("Wood", (0.55, 0.35, 0.18), 0.8)
    metal = mat("Metal", (0.6, 0.62, 0.65), 0.4)
    # handle along Z? make along Y for grip, head at top
    cyl("Handle", (0, 0.35, 0), 0.035, 0.9, wood, vertices=6)
    box("Head", (0.09, 0.72, 0), (0.3, 0.16, 0.06), metal, bevel=0.01)
    cone("Edge", (-0.06, 0.72, 0), 0.05, 0.0, 0.18, metal, vertices=4, rot=(0, 0, math.radians(90)))
    finish("axe")

# ---------------- HOUSE ----------------
def make_house():
    wall = mat("Wall", (0.85, 0.78, 0.65), 0.9)
    wall2 = mat("Wall2", (0.75, 0.6, 0.45), 0.9)
    roof = mat("Roof", (0.55, 0.25, 0.18), 0.85)
    roof2 = mat("Roof2", (0.4, 0.28, 0.22), 0.85)
    wood = mat("Wood", (0.5, 0.32, 0.16), 0.85)
    glass = mat("Glass", (0.4, 0.65, 0.8), 0.2)
    stone = mat("Stone", (0.55, 0.55, 0.55), 0.95)

    # main block 6x4x5
    box("Walls", (0, 2.0, 0), (6, 4, 5), wall)
    box("Walls2", (0, 4.6, 0), (5, 1.2, 4), wall2)  # second floor smaller
    # roof: use two rotated boxes as prism approx -> use cone 4-sided
    cone("Roof", (0, 5.6, 0), 4.4, 0.0, 2.4, roof, vertices=4, rot=(0, math.radians(45), 0))
    # fix: rotate so ridge along X
    bpy.context.active_object.rotation_euler = (0, 0, math.radians(45))
    # chimney
    box("Chimney", (1.8, 6.0, 0.8), (0.6, 2.2, 0.6), stone)
    # door
    box("Door", (-1.2, 1.0, 2.51), (0.9, 2.0, 0.08), wood)
    # windows
    for x in (-2.2, 2.2):
        box(f"Win{x}", (x, 2.4, 2.51), (0.8, 0.8, 0.08), glass)
        box(f"WinSide{x}", (x, 2.4, -2.51), (0.8, 0.8, 0.08), glass)
    # porch (upgrade part)
    porch = box("Part_Porch", (0, 0.15, 3.6), (6.5, 0.3, 2.2), wood)
    for px in (-2.8, 2.8):
        box("PorchPost", (px, 1.4, 4.4), (0.18, 2.8, 0.18), wood)
    # second floor already there; add tower part
    box("Part_Tower", (2.6, 4.5, -1.2), (1.6, 4.5, 1.6), wall2)
    cone("Part_TowerRoof", (2.6, 7.4, -1.2), 1.5, 0.0, 1.6, roof2, vertices=4)
    # golden trim part
    box("Part_Trim", (0, 5.25, 0), (5.1, 0.15, 4.1), mat("Gold", (0.9, 0.7, 0.2), 0.4))
    finish("house")

# ---------------- SAWMILL ----------------
def make_sawmill():
    wood = mat("Wood", (0.55, 0.35, 0.18), 0.85)
    metal = mat("Metal", (0.5, 0.52, 0.55), 0.5)
    roofm = mat("RoofM", (0.45, 0.3, 0.2), 0.85)
    box("FrameL", (-1.4, 1.1, 0), (0.25, 2.2, 0.25), wood)
    box("FrameR", (1.4, 1.1, 0), (0.25, 2.2, 0.25), wood)
    box("Beam", (0, 2.25, 0), (3.1, 0.25, 0.3), wood)
    cone("Roof", (0, 2.6, 0), 2.2, 0.0, 1.2, roofm, vertices=4, rot=(0, math.radians(45), 0))
    bpy.context.active_object.rotation_euler = (0, 0, math.radians(45))
    cyl("Blade", (0, 1.2, 0.15), 0.55, 0.06, metal, vertices=16, rot=(math.radians(90), 0, 0))
    box("Table", (0, 0.75, 0), (2.6, 0.12, 1.0), wood)
    box("Bin", (1.9, 0.4, 0.8), (0.9, 0.8, 0.9), wood)
    finish("sawmill")

# ---------------- CRATE / SELL POINT ----------------
def make_crate():
    wood = mat("Wood", (0.6, 0.42, 0.24), 0.85)
    box("Crate", (0, 0.4, 0), (1.0, 0.8, 1.0), wood)
    box("Lid", (0, 0.85, 0), (1.1, 0.1, 1.1), wood)
    finish("crate")

# ---------------- FENCE ----------------
def make_fence():
    wood = mat("Wood", (0.55, 0.38, 0.2), 0.9)
    box("Rail1", (0, 0.5, 0), (2.0, 0.08, 0.08), wood)
    box("Rail2", (0, 0.9, 0), (2.0, 0.08, 0.08), wood)
    box("PostL", (-0.95, 0.6, 0), (0.12, 1.2, 0.12), wood)
    box("PostR", (0.95, 0.6, 0), (0.12, 1.2, 0.12), wood)
    finish("fence")

# ---------------- BUSH ----------------
def make_bush():
    l1 = mat("LeafB", (0.2, 0.45, 0.18), 0.9)
    l2 = mat("LeafB2", (0.28, 0.52, 0.2), 0.9)
    ico("B1", (-0.3, 0.35, 0), 0.45, l1, scale=(1,0.8,1))
    ico("B2", (0.3, 0.3, 0.1), 0.4, l2, scale=(1,0.75,1))
    ico("B3", (0, 0.55, -0.2), 0.4, l1, scale=(1,0.8,1))
    finish("bush")

# ---------------- PATH SIGN ----------------
def make_sign():
    wood = mat("Wood", (0.5, 0.33, 0.17), 0.85)
    cyl("Post", (0, 0.8, 0), 0.06, 1.6, wood, vertices=6)
    box("Board", (0, 1.45, 0), (0.9, 0.35, 0.06), wood)
    finish("sign")

MODELS = [make_tree, make_stump, make_log, make_plank, make_rock, make_grass,
          make_flower, make_axe, make_house, make_sawmill, make_crate, make_fence, make_bush, make_sign]

for fn in MODELS:
    try:
        fn()
        print("OK", fn.__name__)
    except Exception as e:
        print("FAIL", fn.__name__, e)

print("DONE")

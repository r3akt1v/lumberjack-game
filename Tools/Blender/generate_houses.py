import bpy
import math
import os

OUT_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "Models", "Generated")
os.makedirs(OUT_DIR, exist_ok=True)

def clean_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for o in list(bpy.context.scene.objects):
        bpy.data.objects.remove(o, do_unlink=True)

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

def box(name, loc, dims, material, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(location=loc)
    o = bpy.context.active_object
    o.name = name
    o.scale = (dims[0]/2, dims[1]/2, dims[2]/2)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel > 0:
        mod = o.modifiers.new("bev", 'BEVEL')
        mod.width = bevel
        mod.segments = 1
    assign(o, material)
    return o

def cyl(name, loc, radius, height, material, vertices=8, rot=(0,0,0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=height, location=loc, rotation=rot)
    o = bpy.context.active_object
    o.name = name
    assign(o, material)
    return o

def cone(name, loc, r1, r2, height, material, vertices=7, rot=(0,0,0)):
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=r1, radius2=r2, depth=height, location=loc, rotation=rot)
    o = bpy.context.active_object
    o.name = name
    assign(o, material)
    return o

def finish(name):
    bpy.ops.object.select_all(action='DESELECT')
    objs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    for o in objs:
        o.select_set(True)
    if objs:
        bpy.context.view_layer.objects.active = objs[0]
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    path = os.path.join(OUT_DIR, name + ".fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=False,
                             global_scale=1.0, bake_anim=False,
                             apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL')
    clean_scene()

# shared materials (recreated per model because clean_scene wipes blend data)
WALL=WALL2=WALL3=ROOF=ROOF2=ROOF3=WOOD=WOODL=GLASS=STONE=GOLD=FLOORW=BEDM=SHEET=DOORM=None

def init_mats():
    global WALL,WALL2,WALL3,ROOF,ROOF2,ROOF3,WOOD,WOODL,GLASS,STONE,GOLD,FLOORW,BEDM,SHEET,DOORM
    WALL   = mat("Wall",   (0.85, 0.78, 0.65), 0.9)
    WALL2  = mat("Wall2",  (0.72, 0.55, 0.4), 0.9)
    WALL3  = mat("Wall3",  (0.8, 0.7, 0.58), 0.9)
    ROOF   = mat("Roof",   (0.55, 0.25, 0.18), 0.85)
    ROOF2  = mat("Roof2",  (0.38, 0.26, 0.2), 0.85)
    ROOF3  = mat("Roof3",  (0.45, 0.32, 0.24), 0.85)
    WOOD   = mat("Wood",   (0.5, 0.32, 0.16), 0.85)
    WOODL  = mat("WoodL",  (0.62, 0.45, 0.26), 0.85)
    GLASS  = mat("Glass",  (0.45, 0.7, 0.85), 0.15)
    STONE  = mat("Stone",  (0.55, 0.55, 0.55), 0.95)
    GOLD   = mat("Gold",   (0.9, 0.7, 0.2), 0.35)
    FLOORW = mat("FloorW", (0.55, 0.4, 0.24), 0.8)
    BEDM   = mat("BedM",   (0.7, 0.3, 0.3), 0.85)
    SHEET  = mat("Sheet",  (0.9, 0.88, 0.8), 0.9)
    DOORM  = mat("DoorM",  (0.42, 0.26, 0.13), 0.8)

def pyramid_roof(name, cx, cy, z, w, d, h, material):
    # 4-sided cone scaled so base ellipse matches footprint w x d
    r = max(w, d) / 2 * 1.12
    o = cone(name, (cx, cy, z + h/2), r, 0.0, h, material, vertices=4, rot=(0, 0, math.radians(45)))
    o.scale = (w / (2*r), d / (2*r), 1)
    return o

def wall_with_door(name, cx, cy, z, w, h, d, door_w, door_h, material, door_x=0.0):
    """Front wall (facing -Y) with doorway gap. cy = wall center Y."""
    t = 0.18  # wall thickness
    y = cy
    # left segment
    x0 = -w/2; x1 = door_x - door_w/2; x2 = door_x + door_w/2; x3 = w/2
    if x1 - x0 > 0.05:
        box(name+"_L", ((x0+x1)/2, y, z + h/2), (x1-x0, t, h), material)
    if x3 - x2 > 0.05:
        box(name+"_R", ((x2+x3)/2, y, z + h/2), (x3-x2, t, h), material)
    # lintel above door
    box(name+"_Top", (door_x, y, z + door_h + (h-door_h)/2), (door_w, t, h-door_h), material)

def window(name, x, y, z, w, h, facing=-1):
    # glass pane slightly proud of wall; facing -1 front, +1 back
    box(name, (x, y + facing*0.02, z), (w, 0.1, h), GLASS)
    # frame
    box(name+"_fT", (x, y + facing*0.03, z + h/2 + 0.05), (w+0.15, 0.12, 0.1), WOOD)
    box(name+"_fB", (x, y + facing*0.03, z - h/2 - 0.05), (w+0.15, 0.12, 0.1), WOOD)

def interior(name, w, d, wallh, with_bed=True):
    """Floor + simple furniture inside."""
    box(name+"_Floor", (0, 0, 0.06), (w-0.3, d-0.3, 0.12), FLOORW)
    if with_bed:
        box(name+"_Bed", (-w/4+0.4, d/4-0.4, 0.35), (1.1, 2.1, 0.3), WOODL)         # frame
        box(name+"_Mattress", (-w/4+0.4, d/4-0.4, 0.55), (1.0, 2.0, 0.22), SHEET)    # mattress
        box(name+"_Pillow", (-w/4+0.4, d/4+0.7, 0.72), (0.6, 0.35, 0.12), SHEET)     # pillow
        box(name+"_Blanket", (-w/4+0.4, d/4-0.9, 0.68), (1.02, 0.9, 0.1), BEDM)      # blanket
    box(name+"_Table", (w/4-0.3, -d/4+0.3, 0.75), (1.2, 0.8, 0.08), WOODL)
    box(name+"_TableLeg", (w/4-0.3, -d/4+0.3, 0.37), (0.12, 0.12, 0.74), WOOD)
    box(name+"_Shelf", (w/2-0.35, 0, wallh*0.55), (0.35, 1.4, 0.08), WOODL)
    box(name+"_Shelf2", (w/2-0.35, 0, wallh*0.8), (0.35, 1.4, 0.08), WOODL)
    # crates
    box(name+"_Crate1", (-w/2+0.5, -d/2+0.5, 0.4), (0.7, 0.7, 0.8), WOOD)
    box(name+"_Crate2", (-w/2+0.5, -d/2+0.5, 0.95), (0.55, 0.55, 0.55), WOODL)
    # lamp on table
    cyl(name+"_LampPole", (w/4-0.3, -d/4+0.3, 0.95), 0.03, 0.4, STONE, vertices=6)
    cone(name+"_LampShade", (w/4-0.3, -d/4+0.3, 1.2), 0.18, 0.05, 0.25, GOLD, vertices=8)

# ============ STAGE 1: SHED ============
def make_house_shed():
    w, d, h = 3.6, 3.0, 2.6
    t = 0.16
    # back wall solid, side walls solid, front with door
    box("Back", (0, d/2, h/2), (w, t, h), WALL2)
    box("Left", (-w/2, 0, h/2), (t, d, h), WALL2)
    box("Right", (w/2, 0, h/2), (t, d, h), WALL2)
    wall_with_door("Front", 0, -d/2, 0, w, h, t, 1.0, 1.9, WALL2)
    pyramid_roof("Roof", 0, 0, h, w + 0.5, d + 0.5, 1.1, ROOF)
    interior("Int", w, d, h, with_bed=False)
    finish("house_shed")

# ============ STAGE 2: CABIN ============
def make_house_cabin():
    w, d, h = 4.6, 4.0, 3.0
    t = 0.18
    box("Back", (0, d/2, h/2), (w, t, h), WALL2)
    box("Left", (-w/2, 0, h/2), (t, d, h), WALL2)
    box("Right", (w/2, 0, h/2), (t, d, h), WALL2)
    wall_with_door("Front", 0, -d/2, 0, w, h, t, 1.05, 2.1, WALL)
    # windows on sides
    box("WinL", (-w/2-0.02, 0, h*0.6), (0.08, 1.0, 0.9), GLASS)
    box("WinR", (w/2+0.02, 0, h*0.6), (0.08, 1.0, 0.9), GLASS)
    window("WinF1", -1.2, -d/2, h*0.65, 0.9, 0.8, facing=-1)
    pyramid_roof("Roof", 0, 0, h, w, d, 1.6, ROOF)
    interior("Int", w, d, h, with_bed=True)
    finish("house_cabin")

# ============ STAGE 3: HOUSE (current, bigger + enterable) ============
def make_house():
    w, d, h = 7.0, 6.0, 3.6
    t = 0.2
    # ground floor walls with door gap
    box("Back", (0, d/2, h/2), (w, t, h), WALL)
    box("Left", (-w/2, 0, h/2), (t, d, h), WALL)
    box("Right", (w/2, 0, h/2), (t, d, h), WALL)
    wall_with_door("Front", 0, -d/2, 0, w, h, t, 1.15, 2.2, WALL)
    # second floor
    h2 = 2.8
    box("Back2", (0, d/2+0.05, h+h2/2), (w-0.4, t, h2), WALL3)
    box("Left2", (-w/2+0.05, 0, h+h2/2), (t, d-0.4, h2), WALL3)
    box("Right2", (w/2-0.05, 0, h+h2/2), (t, d-0.4, h2), WALL3)
    box("Front2", (0, -d/2+0.05, h+h2/2), (w-0.4, t, h2), WALL3)
    # windows ground floor front/back/sides
    for x in (-2.3, 2.3):
        window(f"WF{x}", x, -d/2, 1.8, 1.1, 1.1, facing=-1)
        window(f"WB{x}", x, d/2, 1.8, 1.1, 1.1, facing=1)
        box(f"WL{x}", (-w/2, x*0.8, 1.9), (0.08, 1.0, 1.0), GLASS)
        box(f"WR{x}", (w/2, x*0.8, 1.9), (0.08, 1.0, 1.0), GLASS)
    # windows second floor
    for x in (-2.0, 0, 2.0):
        window(f"W2F{x}", x, -d/2+0.1, h+1.5, 1.0, 0.9, facing=-1)
        window(f"W2B{x}", x, d/2-0.1, h+1.5, 1.0, 0.9, facing=1)
    # porch
    box("Porch", (0, -d/2-1.3, 0.12), (w*0.8, 2.4, 0.24), WOODL)
    for px in (-w*0.38, w*0.38):
        box("PorchPost", (px, -d/2-2.2, 1.5), (0.2, 0.2, 3.0), WOOD)
    box("PorchRoof", (0, -d/2-1.3, 3.1), (w*0.85, 2.8, 0.14), ROOF2)
    # chimney
    box("Chimney", (w/4, d/4, h+h2+1.2), (0.7, 0.7, 2.4), STONE)
    # roof
    pyramid_roof("Roof", 0, 0, h+h2, w, d, 2.0, ROOF)
    interior("Int", w, d, h, with_bed=True)
    finish("house")

# ============ STAGE 4: VILLA ============
def make_house_big():
    w, d, h = 9.0, 8.0, 4.0
    t = 0.22
    box("Back", (0, d/2, h/2), (w, t, h), WALL3)
    box("Left", (-w/2, 0, h/2), (t, d, h), WALL3)
    box("Right", (w/2, 0, h/2), (t, d, h), WALL3)
    wall_with_door("Front", 0, -d/2, 0, w, h, t, 1.3, 2.3, WALL3)
    # two floors
    h2 = 3.0
    box("Back2", (0, d/2+0.05, h+h2/2), (w-0.4, t, h2), WALL2)
    box("Left2", (-w/2+0.05, 0, h+h2/2), (t, d-0.4, h2), WALL2)
    box("Right2", (w/2-0.05, 0, h+h2/2), (t, d-0.4, h2), WALL2)
    box("Front2", (0, -d/2+0.05, h+h2/2), (w-0.4, t, h2), WALL2)
    # many windows
    for x in (-3.0, -1.0, 1.0, 3.0):
        window(f"WF{x}", x, -d/2, 1.9, 1.2, 1.2, facing=-1)
        window(f"WB{x}", x, d/2, 1.9, 1.2, 1.2, facing=1)
        box(f"WL{x}", (-w/2, x, 1.9), (0.08, 1.1, 1.1), GLASS)
        box(f"WR{x}", (w/2, x, 1.9), (0.08, 1.1, 1.1), GLASS)
    for x in (-2.5, 0, 2.5):
        window(f"W2F{x}", x, -d/2+0.1, h+1.6, 1.2, 1.0, facing=-1)
        window(f"W2B{x}", x, d/2-0.1, h+1.6, 1.2, 1.0, facing=1)
    # columns at entrance
    for px in (-2.0, 2.0):
        cyl("Column", (px, -d/2-1.8, 1.8), 0.18, 3.6, STONE, vertices=8)
    box("Porch", (0, -d/2-1.6, 0.15), (w*0.7, 3.4, 0.3), STONE)
    pyramid_roof("PorchRoof", 0, -d/2-1.6, 3.6, w*0.75, 3.8, 1.0, ROOF3)
    box("Chimney", (w/4, -d/4, h+h2+1.0), (0.8, 0.8, 2.6), STONE)
    pyramid_roof("Roof", 0, 0, h+h2, w, d, 2.4, ROOF2)
    # balcony rail second floor front
    box("Balcony", (0, -d/2-0.4, h+0.15), (w*0.6, 0.8, 0.18), WOODL)
    box("BalconyRail", (0, -d/2-0.75, h+0.75), (w*0.6, 0.08, 0.8), WOOD)
    interior("Int", w, d, h, with_bed=True)
    finish("house_big")

# ============ STAGE 5: MANSION ============
def make_house_palace():
    w, d, h = 11.0, 9.0, 4.4
    t = 0.25
    box("Back", (0, d/2, h/2), (w, t, h), WALL3)
    box("Left", (-w/2, 0, h/2), (t, d, h), WALL3)
    box("Right", (w/2, 0, h/2), (t, d, h), WALL3)
    wall_with_door("Front", 0, -d/2, 0, w, h, t, 1.5, 2.5, WALL3)
    # grand door frame
    box("DoorFrame", (0, -d/2-0.1, 1.3), (2.0, 0.3, 0.3), GOLD)
    box("DoorFrame2", (0, -d/2-0.1, 2.7), (2.0, 0.3, 0.3), GOLD)
    # two upper floors
    h2 = 3.0; h3 = 2.8
    for i, hh in enumerate([h2, h3]):
        z0 = h + sum([h2,h3][:i])
        box(f"BackU{i}", (0, d/2+0.05, z0+hh/2), (w-0.4, t, hh), WALL)
        box(f"LeftU{i}", (-w/2+0.05, 0, z0+hh/2), (t, d-0.4, hh), WALL)
        box(f"RightU{i}", (w/2-0.05, 0, z0+hh/2), (t, d-0.4, hh), WALL)
        box(f"FrontU{i}", (0, -d/2+0.05, z0+hh/2), (w-0.4, t, hh), WALL)
    ztop = h + h2 + h3
    # windows grid
    for x in (-3.8, -1.9, 0, 1.9, 3.8):
        window(f"WF{x}", x, -d/2, 1.9, 1.1, 1.4, facing=-1)
        window(f"WB{x}", x, d/2, 1.9, 1.1, 1.4, facing=1)
        box(f"WL{x}", (-w/2, x*0.9, 1.9), (0.08, 1.2, 1.2), GLASS)
        box(f"WR{x}", (w/2, x*0.9, 1.9), (0.08, 1.2, 1.2), GLASS)
    for fl in range(2):
        z0 = h + (h2 if fl==0 else h2+h3) + 1.4
        for x in (-2.8, 0, 2.8):
            window(f"W{fl}F{x}", x, -d/2+0.1, z0, 1.1, 1.0, facing=-1)
            window(f"W{fl}B{x}", x, d/2-0.1, z0, 1.1, 1.0, facing=1)
    # columns + pediment entrance
    for px in (-2.6, 2.6):
        cyl("Column", (px, -d/2-2.0, 2.2), 0.22, 4.4, STONE, vertices=8)
        box("ColumnBase", (px, -d/2-2.0, 0.15), (0.6, 0.6, 0.3), STONE)
    box("Porch", (0, -d/2-1.9, 0.18), (w*0.6, 4.0, 0.36), STONE)
    box("Pediment", (0, -d/2-1.9, 4.6), (5.6, 4.2, 0.5), WALL3)
    pyramid_roof("PorchRoof", 0, -d/2-1.9, 4.85, 5.6, 4.2, 1.0, ROOF3)
    # corner towers
    for tx, ty in [(-w/2+0.8, -d/2+0.8), (w/2-0.8, -d/2+0.8), (-w/2+0.8, d/2-0.8), (w/2-0.8, d/2-0.8)]:
        box("Tower", (tx, ty, ztop+1.5), (1.6, 1.6, 3.0), WALL2)
        cone("TowerRoof", (tx, ty, ztop+3.0+0.8), 1.3, 0.0, 1.6, ROOF, vertices=4, rot=(0,0,math.radians(45)))
    box("Chimney", (w/4, -d/4, ztop+1.4), (0.9, 0.9, 2.8), STONE)
    pyramid_roof("Roof", 0, 0, ztop, w, d, 2.8, ROOF2)
    box("Trim", (0, -d/2-0.15, h-0.1), (w*0.7, 0.2, 0.3), GOLD)
    interior("Int", w, d, h, with_bed=True)
    finish("house_palace")

MODELS = [make_house_shed, make_house_cabin, make_house, make_house_big, make_house_palace]

for fn in MODELS:
    try:
        clean_scene()
        init_mats()
        fn()
        print("OK", fn.__name__)
    except Exception as e:
        print("FAIL", fn.__name__, e)

print("DONE")

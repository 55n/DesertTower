"""Run with Blender 5.2: blender --background --factory-startup --python this_file.
Creates a native editable blockout and a Unity-oriented FBX without touching a Unity scene.
All dimensions below are design proposals in meters. Blender Z is up.
"""
import bpy
import bmesh
import math
import json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

OUT = Path(__file__).resolve().parent
OUT.mkdir(parents=True, exist_ok=True)
EXPORT = OUT / 'Exports'
PREVIEW = OUT / 'Previews'
EXPORT.mkdir(exist_ok=True)
PREVIEW.mkdir(exist_ok=True)

PARAMS = dict(outer_half_extent=22.5, road_width=4.0, tier_height=3.0,
              lower_elevation=0.6, road_thickness=0.22, ramp_flat_fraction=0.18,
              slot_size=1.4, character_height=1.8)
R = PARAMS['outer_half_extent']
W = PARAMS['road_width']
H = PARAMS['tier_height']
Z0 = PARAMS['lower_elevation']
TOP_Z = Z0 + 3 * H

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for collection in list(bpy.data.collections):
    if collection.name != 'Collection':
        bpy.data.collections.remove(collection)
default = bpy.data.collections.get('Collection')
if default:
    bpy.data.collections.remove(default)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1.0
scene.unit_settings.length_unit = 'METERS'

collections = {}
for name in ['01_Solid_Pyramid', '02_Enemy_Road', '03_Core_Platform',
             '04_Build_Slots_8', '05_Player_Landing_Guides',
             '06_Route_Guides', '07_Scale_Reference', '08_Presentation']:
    c = bpy.data.collections.new(name)
    scene.collection.children.link(c)
    collections[name] = c

def move_to(obj, col):
    for c in list(obj.users_collection):
        c.objects.unlink(obj)
    collections[col].objects.link(obj)
    return obj

def material(name, color, roughness=0.85, emission=0.0):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bs = m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (*color, 1)
    bs.inputs['Roughness'].default_value = roughness
    if emission:
        bs.inputs['Emission Color'].default_value = (*color, 1)
        bs.inputs['Emission Strength'].default_value = emission
    return m

M_BODY = material('Sandstone_Support', (0.48, 0.34, 0.20))
M_ROADS = [material('Road_Lower', (0.68, 0.51, 0.30)),
           material('Road_Middle', (0.77, 0.60, 0.39)),
           material('Road_Upper', (0.87, 0.72, 0.50))]
M_RAMP = material('Connecting_Ramps', (0.71, 0.47, 0.22))
M_CORE_FLOOR = material('Summit_Floor', (0.91, 0.77, 0.55))
M_STONE = material('Dark_Stone', (0.24, 0.22, 0.19))
M_GROUND = material('Sand_Foundation', (0.63, 0.52, 0.37))
M_STUDIO = material('Studio_Background', (0.78, 0.76, 0.70))
M_SLOT = material('Build_Slot_Markers', (0.23, 0.29, 0.29))
M_CYAN = material('Player_Mobility_Cyan', (0.025, 0.55, 0.62), emission=0.3)
M_AMBER = material('Enemy_Route_Amber', (1.0, 0.47, 0.025), emission=0.15)
M_CORE = material('Core_Crystal', (0.04, 0.76, 0.85), roughness=0.22, emission=0.5)
M_GREY = material('Human_Scale_Reference', (0.13, 0.18, 0.20))

export_objects = []
terrain_objects = []
road_objects = []
slot_records = []
landing_records = []

def tag(obj, purpose, export=False):
    obj['purpose'] = purpose
    if export:
        export_objects.append(obj)
    return obj

def mesh_obj(name, verts, faces, mat, col, export=True):
    mesh = bpy.data.meshes.new(name + '_Mesh')
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bmesh.ops.triangulate(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    collections[col].objects.link(obj)
    obj.data.materials.append(mat)
    if export:
        export_objects.append(obj)
    return obj

def cube(name, loc, size, mat, col, export=True, bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    move_to(obj, col)
    obj.data.materials.append(mat)
    if bevel:
        mod = obj.modifiers.new('Small_Edge_Bevel', 'BEVEL')
        mod.width = bevel
        mod.segments = 2
    if export:
        export_objects.append(obj)
    return obj

def empty(name, loc, col, export=True):
    obj = bpy.data.objects.new(name, None)
    collections[col].objects.link(obj)
    obj.location = loc
    obj.empty_display_type = 'PLAIN_AXES'
    obj.empty_display_size = 0.4
    if export:
        export_objects.append(obj)
    return obj

def curve(name, coords, radius, mat, col, cyclic=False):
    data = bpy.data.curves.new(name, 'CURVE')
    data.dimensions = '3D'
    data.resolution_u = 1
    data.bevel_depth = radius
    data.bevel_resolution = 2
    spline = data.splines.new('POLY')
    spline.points.add(len(coords) - 1)
    for p, v in zip(spline.points, coords):
        p.co = (*v, 1)
    spline.use_cyclic_u = cyclic
    obj = bpy.data.objects.new(name, data)
    collections[col].objects.link(obj)
    data.materials.append(mat)
    obj['purpose'] = 'Visual guide only. Excluded from game geometry export.'
    return obj

def point(i, inner=False):
    r = R - i * W / 4 - (W if inner else 0)
    sx, sy = [(-1,-1), (1,-1), (1,1), (-1,1)][i % 4]
    return Vector((sx*r, sy*r, Z0 + (i//4)*H))

def surface(i, t, across=0.40):
    o = point(i).lerp(point(i+1), t)
    q = point(i, True).lerp(point(i+1, True), t)
    p = o.lerp(q, across)
    low = Z0 + (i//4)*H
    if i % 4 == 3:
        f = PARAMS['ramp_flat_fraction']
        a=(point(i)+point(i,True))/2
        b=(point(i+1)+point(i+1,True))/2
        a.z=b.z=0
        direction=(b-a).normalized()
        distance=(b-a).length
        projected=Vector((p.x,p.y,0))-a
        p.z = low + H * min(1.0, max(0.0, (projected.dot(direction)/distance-f)/(1-2*f)))
    else:
        p.z = low
    return p

def ramp_sections(i):
    # Perpendicular cuts yield genuinely planar ramp tops, unlike interpolating
    # corner heights across a trapezoid with angled mitered ends.
    outer_a,outer_b=point(i),point(i+1)
    inner_a,inner_b=point(i,True),point(i+1,True)
    for p in [outer_a,outer_b,inner_a,inner_b]:
        p.z=0
    center_a=(outer_a+inner_a)/2
    center_b=(outer_b+inner_b)/2
    direction=(center_b-center_a).normalized()
    length=(center_b-center_a).length
    low=Z0+(i//4)*H
    output=[(Vector((outer_a.x,outer_a.y,low)),Vector((inner_a.x,inner_a.y,low)))]
    for fraction,height in [(PARAMS['ramp_flat_fraction'],low),(1-PARAMS['ramp_flat_fraction'],low+H)]:
        row=[]
        for a,b in [(outer_a,outer_b),(inner_a,inner_b)]:
            t=(fraction*length-(a-center_a).dot(direction))/(b-a).dot(direction)
            assert 0<t<1
            p=a.lerp(b,t);p.z=height;row.append(p)
        output.append(tuple(row))
    output.append((Vector((outer_b.x,outer_b.y,low+H)),Vector((inner_b.x,inner_b.y,low+H))))
    return output

def ribbon_prism(name, sections, mat, col, bottom=None, thickness=0.22):
    verts = []
    for a, b in sections:
        za = bottom if bottom is not None else a.z-thickness
        zb = bottom if bottom is not None else b.z-thickness
        verts.extend([tuple(a), tuple(b), (a.x,a.y,za), (b.x,b.y,zb)])
    faces = []
    for j in range(len(sections)-1):
        a, b = j*4, (j+1)*4
        faces.extend([(a,b,b+1,a+1), (a+2,a+3,b+3,b+2),
                      (a,a+2,b+2,b), (a+1,b+1,b+3,a+3)])
    end = (len(sections)-1)*4
    faces.extend([(0,1,3,2), (end,end+2,end+3,end+1)])
    obj = mesh_obj(name, verts, faces, mat, col)
    terrain_objects.append(obj)
    return obj

def polygon_prism(name, poly, top, bottom, mat, col):
    n = len(poly)
    vs = [(p[0],p[1],top) for p in poly] + [(p[0],p[1],bottom) for p in poly]
    fs = [tuple(range(n)), tuple(reversed(range(n,2*n)))]
    fs += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh_obj(name, vs, fs, mat, col)

# Each quarter turn contracts by 1 m. The inner edge of turn n exactly matches
# the outer edge of turn n+4 in XY, providing a solid 3 m riser with no radial gap.
for i in range(12):
    tier = ['Lower','Middle','Upper'][i//4]
    side = ['Front','Right','Rear','Left'][i%4]
    ramp = i % 4 == 3
    ts = [0, PARAMS['ramp_flat_fraction'], 1-PARAMS['ramp_flat_fraction'], 1] if ramp else [0,1]
    sections = ramp_sections(i) if ramp else [(surface(i,t,0),surface(i,t,1)) for t in ts]
    body_sections = [(a-Vector((0,0,0.18)),b-Vector((0,0,0.18))) for a,b in sections]
    support = ribbon_prism(f'Body_{i+1:02d}_{tier}_{side}', body_sections,
                           M_BODY,'01_Solid_Pyramid',bottom=-0.15)
    tag(support, 'Closed solid retaining mass; no enterable interior.')
    obj = ribbon_prism(f'Road_{i+1:02d}_{tier}_{side}_'+('Ramp' if ramp else 'Flat'),
                       sections, M_RAMP if ramp else M_ROADS[i//4], '02_Enemy_Road')
    obj['tier'] = tier
    obj['segment_index'] = i
    obj['surface_type'] = 'Connecting ramp with level end landings' if ramp else 'Flat combat road'
    road_objects.append(obj)

# Solid summit fills the residual inner polygon. Its front diagonal edge is the
# exact final cross-section of the upper connecting ramp.
summit_polygon = [(point(i,True).x, point(i,True).y) for i in range(8,13)]
core_body = polygon_prism('Body_Summit_Solid',summit_polygon,TOP_Z-0.18,-0.15,
                         M_BODY,'01_Solid_Pyramid')
core_floor = polygon_prism('Road_13_Summit_Platform',summit_polygon,TOP_Z,TOP_Z-0.22,
                          M_CORE_FLOOR,'03_Core_Platform')
terrain_objects.extend([core_body,core_floor])
road_objects.append(core_floor)

# A ground approach joins the actual first road edge; no doorway or staircase.
entry_end = (surface(0,0,0),surface(0,0,1))
entry_start = tuple(Vector((p.x-6,p.y+6,0)) for p in entry_end)
entry = ribbon_prism('Road_00_Entry_Ramp',[entry_start,entry_end],M_RAMP,'02_Enemy_Road',thickness=.22)
road_objects.append(entry)
ground = cube('Ground_Foundation',(-2,0,-.4),(61,56,.8),M_GROUND,'01_Solid_Pyramid',bevel=.15)
tag(ground,'Exterior ground. Enemy entry is the approach ramp; road starts 0.6 m above ground.')

core = cube('Core_Plinth',(0,0,TOP_Z+.18),(1.7,1.7,.36),M_STONE,'03_Core_Platform',bevel=.08)
crystal_v = [(0,0,TOP_Z+.38),(0,0,TOP_Z+2.3)]
for j in range(6):
    a=2*math.pi*j/6
    crystal_v.append((.58*math.cos(a),.58*math.sin(a),TOP_Z+1.15))
crystal_f=[]
for j in range(6):
    n=2+(j+1)%6
    crystal_f.extend([(0,n,2+j),(1,2+j,n)])
mesh_obj('Core_Crystal',crystal_v,crystal_f,M_CORE,'03_Core_Platform')
empty('Core_Target',(0,0,TOP_Z),'03_Core_Platform')

# Eight movable markers. Their layout is a proposal, not a gameplay contract.
for index, segment in enumerate([0,1,2,4,5,6,8,9],1):
    p = surface(segment,.73,.79)
    direction=surface(segment,.75,.79)-surface(segment,.71,.79)
    angle=math.atan2(direction.y,direction.x)
    marker=cube(f'Slot_{index:02d}_Marker',p+Vector((0,0,.025)),
                (PARAMS['slot_size'],PARAMS['slot_size'],.05),M_SLOT,'04_Build_Slots_8')
    marker.rotation_euler.z=angle
    marker['purpose']='Placement marker only, not an installed tower or portal.'
    origin=empty(f'BuildSlot_{index:02d}',p,'04_Build_Slots_8')
    origin['tier']=['Lower','Middle','Upper'][segment//4]
    origin['slot_id']=index
    slot_records.append(dict(id=index,tier=origin['tier'],blender=list(p),rotation_z_degrees=math.degrees(angle)))
    marker['slot_id']=index

def landing_outline(name,p):
    coords=[(p.x+x,p.y+y,p.z+.028) for x,y in [(-.65,-.5),(.65,-.5),(.65,.5),(-.65,.5)]]
    return curve(name,coords,.022,M_CYAN,'05_Player_Landing_Guides',True)

# Vertical mobility lanes: take off beside the retaining wall, land just inside
# the rim above. The centers are ~1.28 m apart horizontally and 3 m vertically.
for side in [0,1]:
    for band in range(3):
        s=side+band*4
        a=surface(s,.5,.84)
        if band<2:
            b=surface(s+4,.5,.16)
        else:
            b=surface(s,.5,1)
            inward=surface(s,.5,1)-surface(s,.5,0)
            inward.z=0
            b+=inward.normalized()*.64
            b.z=TOP_Z
        name=f'Mobility_{"Front" if side==0 else "Right"}_{band+1}'
        landing_outline(name+'_Takeoff',a)
        landing_outline(name+'_Landing',b)
        arc=[]
        for j in range(21):
            t=j/20
            q=a.lerp(b,t)
            q.z+=.05+1.35*math.sin(math.pi*t)
            arc.append(q)
        curve(name+'_Ability_Guide',arc,.025,M_CYAN,'05_Player_Landing_Guides')
        start=empty(name+'_Takeoff',a,'05_Player_Landing_Guides',False)
        end=empty(name+'_Landing',b,'05_Player_Landing_Guides',False)
        landing_records.append(dict(name=name,takeoff=list(a),landing=list(b),
                                    rise=round(b.z-a.z,3),horizontal=round(Vector((b.x-a.x,b.y-a.y)).length,3)))

# Gameplay route data uses continuous terrain-space points, with a separate
# visible overlay collection. These curves/arrows never become collision meshes.
start=entry_start[0].lerp(entry_start[1],.4)
end=entry_end[0].lerp(entry_end[1],.4)
route_nodes=[start.lerp(end,.002),end]
for i in range(12):
    if i%4==3:
        ca=(point(i)+point(i,True))/2
        cb=(point(i+1)+point(i+1,True))/2
        ca.z=cb.z=0
        direction=(cb-ca).normalized()
        pa=surface(i,0,.4);pb=surface(i,1,.4)
        pa.z=pb.z=0
        for fraction in [PARAMS['ramp_flat_fraction'],1-PARAMS['ramp_flat_fraction']]:
            t=(fraction*(cb-ca).length-(pa-ca).dot(direction))/(pb-pa).dot(direction)
            route_nodes.append(surface(i,t,.4))
    route_nodes.append(surface(i,1,.4))
route_nodes.append(Vector((0,0,TOP_Z)))
waypoints=[route_nodes[0]]
for a,b in zip(route_nodes,route_nodes[1:]):
    count=math.ceil((b-a).length/.75)
    waypoints.extend(a.lerp(b,j/count) for j in range(1,count+1))
curve('Enemy_Route_Continuous_Guide',[p+Vector((0,0,.07)) for p in waypoints],.05,M_AMBER,'06_Route_Guides')
for i in range(12):
    for n,t in enumerate([.30,.62]):
        p=surface(i,t,.4)+Vector((0,0,.08))
        tangent=(surface(i,t+.015,.4)-surface(i,t-.015,.4)).normalized()
        across=Vector((-tangent.y,tangent.x,0)).normalized()
        coords=[p-tangent*.26+across*.18,p+tangent*.16,p-tangent*.26-across*.18]
        curve(f'Route_Chevron_{i+1:02d}_{n}',coords,.035,M_AMBER,'06_Route_Guides')
for n,p in enumerate(route_nodes):
    e=empty(f'EnemyWaypoint_{n:02d}',p,'06_Route_Guides',True)
    e['sequence']=n
    e.hide_render=True

# Human-height reference is isolated from exported geometry.
def cylinder_between(name,a,b,r,mat,col):
    delta=Vector(b)-Vector(a)
    bpy.ops.mesh.primitive_cylinder_add(vertices=10,radius=r,depth=delta.length,
                                      location=(Vector(a)+Vector(b))/2)
    obj=bpy.context.object
    obj.name=name
    obj.rotation_euler=delta.to_track_quat('Z','Y').to_euler()
    move_to(obj,col)
    obj.data.materials.append(mat)
    return obj

person_base=surface(0,.45,.4)
for side in [-1,1]:
    x=person_base.x+side*.115
    cylinder_between('Scale_Leg', (x,person_base.y,person_base.z+.08),
                     (x,person_base.y,person_base.z+.87),.085,M_GREY,'07_Scale_Reference')
cube('Scale_Torso',person_base+Vector((0,0,1.15)),(.39,.24,.61),M_GREY,'07_Scale_Reference',False,.05)
for side in [-1,1]:
    cylinder_between('Scale_Arm',person_base+Vector((side*.24,0,1.38)),
                     person_base+Vector((side*.28,0,.91)),.055,M_GREY,'07_Scale_Reference')
bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=8,radius=.16,
                                  location=person_base+Vector((0,0,1.64)))
move_to(bpy.context.object,'07_Scale_Reference').name='Scale_Head_1_8m'
bpy.context.object.data.materials.append(M_GREY)

# Studio objects are excluded from FBX.
cube('Studio_Floor',(-2,0,-.86),(200,200,.08),M_STUDIO,'08_Presentation',False)
world=bpy.data.worlds.new('Neutral_Studio')
world.use_nodes=True
world.node_tree.nodes['Background'].inputs['Color'].default_value=(.78,.80,.85,1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value=.65
scene.world=world

def area(name,loc,energy,size):
    data=bpy.data.lights.new(name,'AREA')
    data.energy=energy
    data.shape='DISK'
    data.size=size
    obj=bpy.data.objects.new(name,data)
    collections['08_Presentation'].objects.link(obj)
    obj.location=loc
    obj.rotation_euler=(Vector((0,0,3))-obj.location).to_track_quat('-Z','Y').to_euler()
    return obj
area('Key_Softbox',(-25,-32,50),11000,18)
area('Fill_Softbox',(25,-4,28),4500,22)
area('Rim_Softbox',(5,25,38),7000,18)

def camera(name,loc,target,scale):
    data=bpy.data.cameras.new(name)
    obj=bpy.data.objects.new(name,data)
    collections['08_Presentation'].objects.link(obj)
    obj.location=loc
    obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
    data.type='ORTHO'
    data.ortho_scale=scale
    data.lens=45
    data.clip_end=500
    return obj
hero=camera('Camera_Hero',(-58,-72,58),(-2,0,3.4),77)
rear=camera('Camera_Rear',(60,65,51),(-2,0,3.4),77)
top=camera('Camera_Top',(-2,0,85),(-2,0,0),66)
top.rotation_euler=(0,0,0)
scene.camera=hero
scene.render.engine='BLENDER_EEVEE'
scene.render.resolution_x=1600
scene.render.resolution_y=1200
scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.render.film_transparent=False
scene.render.fps=30
scene.view_settings.exposure=-.45
try:
    scene.view_settings.view_transform='AgX'
    scene.view_settings.look='AgX - Medium High Contrast'
except Exception:
    pass

# Validate the produced meshes and actual terrain, not only parameter values.
bpy.context.view_layer.update()
all_verts=[]
all_polys=[]
for obj in terrain_objects:
    start=len(all_verts)
    all_verts.extend([obj.matrix_world@v.co for v in obj.data.vertices])
    all_polys.extend([tuple(start+i for i in f.vertices) for f in obj.data.polygons])
bvh=BVHTree.FromPolygons(all_verts,all_polys)
def support_error(p):
    hit,normal,index,distance=bvh.ray_cast(Vector((p.x,p.y,TOP_Z+20)),Vector((0,0,-1)),60)
    return None if hit is None else abs(hit.z-p.z)

route_errors=[support_error(p) for p in waypoints]
missing=sum(e is None for e in route_errors)
max_error=max(e for e in route_errors if e is not None)
max_step=max(abs(a.z-b.z) for a,b in zip(waypoints,waypoints[1:]))
max_slope=0
for obj in road_objects:
    for face in obj.data.polygons:
        if face.normal.z>.5:
            max_slope=max(max_slope,math.degrees(math.acos(min(1,face.normal.z))))
manifold_fail=[]
for obj in terrain_objects:
    bm=bmesh.new();bm.from_mesh(obj.data)
    if any(not e.is_manifold for e in bm.edges):
        manifold_fail.append(obj.name)
    bm.free()
landing_errors=[]
for item in landing_records:
    for key in ['takeoff','landing']:
        err=support_error(Vector(item[key]))
        if err is None or err>.03:
            landing_errors.append(dict(name=item['name'],point=key,error=err))
slot_clearance=[]
for item in slot_records:
    p=Vector(item['blender'])
    d=min(Vector((p.x-q.x,p.y-q.y)).length for q in waypoints if abs(p.z-q.z)<.1)
    slot_clearance.append(d-PARAMS['slot_size']*math.sqrt(2)/2)

route_length=sum((a-b).length for a,b in zip(waypoints,waypoints[1:]))
report=dict(blender_version=bpy.app.version_string,parameters=PARAMS,
            tier_elevations=[Z0,Z0+H,Z0+2*H,TOP_Z],build_slot_count=len(slot_records),
            sampled_route_points=len(waypoints),route_length_m=round(route_length,2),
            route_missing_support=missing,max_surface_error_m=round(max_error,5),
            max_sample_vertical_change_m=round(max_step,4),max_road_slope_degrees=round(max_slope,2),
            nonmanifold_terrain_meshes=manifold_fail,landing_surface_errors=landing_errors,
            min_slot_to_route_clearance_m=round(min(slot_clearance),3),
            note='Geometric validation only. Unity movement, NavMesh and camera playtests remain.')
assert len(slot_records)==8
assert not missing and max_error<.04, report
assert max_slope<22, report
assert not manifold_fail, report
assert not landing_errors, report
assert min(slot_clearance)>.40, report
(OUT/'validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')

def unity(p):
    return [round(p[0],5),round(p[2],5),round(-p[1],5)]
data=dict(units='meters',blender_axes='X right / Y depth / Z up',
          unity_conversion='(x,y,z) Blender -> (x,z,-y) Unity',
          parameters=PARAMS,core_target_blender=[0,0,TOP_Z],
          core_target_unity=[0,TOP_Z,0],
          enemy_route_blender=[[round(x,5) for x in p] for p in waypoints],
          enemy_route_unity=[unity(p) for p in waypoints],
          exact_route_nodes_blender=[[round(x,5) for x in p] for p in route_nodes],
          exact_route_nodes_unity=[unity(p) for p in route_nodes],
          build_slots=[dict(**s,unity=unity(s['blender'])) for s in slot_records],
          player_mobility=landing_records)
(EXPORT/'SandGuard_Spiral_Blockout_v1_layout.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')

readme='''SANDGUARD / SPIRAL BLOCKOUT v1
Native editable Blender scene. Units: meters.
Three flat combat tiers at 0.6 / 3.6 / 6.6 m. Summit: 9.6 m.
Each tier follows three flat exterior sides and one connecting ramp.
One inward square spiral, no doors, interiors, stairs or portals.
Orange: enemy route guide. Cyan: player mobility and landing guides.
Eight slot markers and separate BuildSlot empties. Layout is provisional.
Collections 05-08 are guides/presentation; guide geometry is not in FBX.
Terrain and slot meshes, core and route/slot empties are exported to FBX.
Player jump pairs: 3 m rise, about 1.28 m horizontal displacement.
Actual movement range is not implemented here. Validate in Unity.
The source generator is included alongside this file; change PARAMS to resize.
Saved cameras: Hero / Rear / Top. Validation report: validation.json.
'''
textblock=bpy.data.texts.new('READ_ME_FIRST')
textblock.write(readme)
source=bpy.data.texts.new('build_spiral_blockout.py')
source.write(Path(__file__).read_text(encoding='utf-8'))
scene['design_revision']='User: solid exterior pyramid; enemy spiral ramps; player vertical mobility; no portals/interior.'
scene['tier_height_m']=H
scene['road_width_m']=W
scene['build_slots']=8
scene['validation_status']='Geometry checks passed; Unity playtest still required.'

# Save a useful initial viewport with the full level visible.
bpy.ops.object.select_all(action='DESELECT')
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            space=area.spaces.active
            space.shading.type='MATERIAL'
            space.region_3d.view_distance=78
            space.region_3d.view_location=Vector((-2,0,3.4))
            space.region_3d.view_rotation=hero.rotation_euler.to_quaternion()
            space.clip_end=500

blend_path=OUT/'SandGuard_Spiral_Blockout_v1.blend'
bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))

# Explicit FBX export avoids requiring Unity to import a .blend through Blender.
for obj in export_objects:
    obj.select_set(True)
bpy.context.view_layer.objects.active=ground
bpy.ops.export_scene.fbx(filepath=str(EXPORT/'SandGuard_Spiral_Blockout_v1.fbx'),
                         use_selection=True,object_types={'MESH','EMPTY'},
                         axis_forward='-Z',axis_up='Y',apply_unit_scale=True,
                         global_scale=1.0,bake_space_transform=True,
                         use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False,
                         use_custom_props=True)
bpy.ops.object.select_all(action='DESELECT')
for name,cam,res in [('Hero',hero,(1600,1200)),('Top',top,(1500,1500)),('Rear',rear,(1600,1200))]:
    scene.camera=cam
    scene.render.resolution_x,scene.render.resolution_y=res
    scene.render.filepath=str(PREVIEW/f'SandGuard_Blockout_{name}.png')
    bpy.ops.render.render(write_still=True)
scene.camera=hero
scene.render.resolution_x,scene.render.resolution_y=1600,1200
scene.render.filepath=str(PREVIEW/'SandGuard_Blockout_Hero.png')
# Do not generate .blend1 backups for this newly generated deliverable.
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))
print('BLOCKOUT_COMPLETE',str(blend_path),json.dumps(report))

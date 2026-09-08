import bpy
import json
from pathlib import Path
from mathutils import Vector

root=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(root/'SandGuard_Spiral_Blockout_v1.blend'))
def bounds(objects):
    points=[obj.matrix_world@Vector(v) for obj in objects if obj.type=='MESH' for v in obj.bound_box]
    return [[min(v[i] for v in points) for i in range(3)],
            [max(v[i] for v in points) for i in range(3)]]
names=['01_Solid_Pyramid','02_Enemy_Road','03_Core_Platform','04_Build_Slots_8']
original_objects=[obj for name in names for obj in bpy.data.collections[name].objects]
original_bounds=bounds(original_objects)
assert len([o for o in bpy.data.objects if o.name.startswith('BuildSlot_')])==8
assert len([o for o in bpy.data.objects if o.name.startswith('EnemyWaypoint_')])==21
assert all(bpy.data.objects.get(n) for n in ['Camera_Hero','Camera_Top','Camera_Rear'])
assert bpy.context.scene.unit_settings.scale_length==1.0
assert bpy.data.texts.get('READ_ME_FIRST')
original_meshes=len([o for o in original_objects if o.type=='MESH'])
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(root/'Exports/SandGuard_Spiral_Blockout_v1.fbx'))
bpy.context.view_layer.update()
imported_bounds=bounds(bpy.data.objects)
error=max(abs(a-b) for x,y in zip(original_bounds,imported_bounds) for a,b in zip(x,y))
assert error<.005,(original_bounds,imported_bounds)
assert len([o for o in bpy.data.objects if o.type=='MESH'])==original_meshes
assert len([o for o in bpy.data.objects if o.name.startswith('BuildSlot_')])==8
assert len([o for o in bpy.data.objects if o.name.startswith('EnemyWaypoint_')])==21
assert not any(o.type in {'LIGHT','CAMERA','CURVE'} for o in bpy.data.objects)
result=dict(saved_blend_reopened=True,fbx_reimported=True,mesh_count=original_meshes,
            build_slot_empties=8,route_node_empties=21,
            max_roundtrip_bounds_error_m=error,
            original_bounds_blender=original_bounds,fbx_bounds_blender=imported_bounds,
            note='File and FBX round-trip checked in Blender. Unity runtime not tested.')
(root/'file_verification.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print('FILES_VERIFIED',json.dumps(result))

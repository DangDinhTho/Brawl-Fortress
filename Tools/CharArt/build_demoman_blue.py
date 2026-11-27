import bpy, os
from mathutils import Vector
# Provide the legacy helper's expected armature; the static toy model is not skinned.
arm_data = bpy.data.armatures.new('demo.qc_skeleton')
arm = bpy.data.objects.new('demo.qc_skeleton', arm_data)
bpy.context.scene.collection.objects.link(arm)
bpy.context.view_layer.objects.active = arm
arm.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
b = arm_data.edit_bones.new('root')
b.head = (0,0,0); b.tail = (0,0,1)
bpy.ops.object.mode_set(mode='OBJECT')
arm.hide_viewport = True; arm.hide_render = True
ns = bpy.app.driver_namespace
ns['toy_team'] = 2
ns['toy_w'] = 900; ns['toy_h'] = 1100
ns['toy_out'] = r'E:\UnityProjects\Brawl Fortress\Tools\CharArt\preview'
ns['toy_tag'] = 'Demoman_Blue'
ns['toy_shots'] = ('Front','34')
source_dir = r'E:\UnityProjects\Brawl Fortress\Tools\CharArt'
exec(open(os.path.join(source_dir, 'toy_demo_refined.py'), encoding='utf-8').read())
# Add the held prop shown in each reference image.
def prop_material(name, color, rough=0.55):
    m = bpy.data.materials.new(name); m.diffuse_color = (*color, 1); m.use_nodes = True
    m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (*color, 1)
    m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = rough
    return m

def prop_cube(name, loc, dims, mat, bevel=0.35):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc); o=bpy.context.object; o.name=name; o.dimensions=dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True); o.data.materials.append(mat)
    if bevel:
        mod=o.modifiers.new('Soft edges','BEVEL'); mod.width=bevel; mod.segments=3
        bpy.context.view_layer.objects.active=o; bpy.ops.object.modifier_apply(modifier=mod.name)
    return o

def prop_cyl(name, loc, radius, depth, mat, verts=20):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=radius, depth=depth, location=loc)
    o=bpy.context.object; o.name=name; o.data.materials.append(mat)
    for p in o.data.polygons: p.use_smooth=True
    mod=o.modifiers.new('Rounded ends','BEVEL'); mod.width=min(0.3, radius*0.15); mod.segments=2
    bpy.context.view_layer.objects.active=o; bpy.ops.object.modifier_apply(modifier=mod.name)
    return o

if TEAM == 1:
    iron=prop_material('Gun - blued steel',(0.035,0.04,0.05))
    edge=prop_material('Gun - worn steel',(0.12,0.13,0.15))
    grip=prop_material('Gun - dark wood grip',(0.12,0.07,0.045))
    bits=[prop_cube('Launcher receiver',(-22,-9,19),(6.2,5.0,8.0),iron),
          prop_cyl('Long barrel',(-22,-9,8.0),2.0,16,edge),
          prop_cyl('Muzzle',(-22,-9,0.7),2.55,1.3,iron),
          prop_cube('Wood grip',(-22,-3.2,21.0),(4.2,4.2,6.0),grip,0.7),
          prop_cube('Sight',(-22,-9,24.0),(2.3,2.4,1.1),edge,0.2),
          prop_cyl('Side drum',(-18.2,-9,16.3),2.6,2.1,iron)]
    propname='Held Grenade Launcher'
else:
    glass=prop_material('Bottle - amber glass',(0.34,0.20,0.075),0.28)
    cap=prop_material('Bottle - brass cap',(0.64,0.48,0.21),0.35)
    paper=prop_material('Bottle - cream label',(0.78,0.71,0.52),0.82)
    bits=[prop_cyl('Bottle body',(-22,-9,29),3.8,12.5,glass),
          prop_cyl('Bottle shoulder',(-22,-9,36.1),2.8,2.3,glass),
          prop_cyl('Bottle neck',(-22,-9,38.5),1.55,3.0,glass),
          prop_cyl('Bottle cap',(-22,-9,40.2),1.75,1.0,cap),
          prop_cube('Bottle label',(-22,-12.85,29),(4.8,0.24,4.5),paper,0.35)]
    propname='Held Amber Bottle'
# Join prop pieces so the Unity FBX contains the complete held item.
bpy.ops.object.select_all(action='DESELECT')
for o in bits: o.select_set(True)
bpy.context.view_layer.objects.active=bits[0]; bpy.ops.object.join(); prop=bpy.context.object; prop.name=propname
# Re-render after the held prop is attached so the review image matches the saved model.
for shot in ns.get('toy_shots', ('Front','34')):
    sc.camera = cams[shot]
    sc.render.filepath = os.path.join(ns.get('toy_out', source_dir), ns.get('toy_tag','toy') + '_' + shot + '.png')
    bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(source_dir,'Brawl_Demoman_Blue.blend'))
# Export a Unity-ready static mesh with vertex colors and modifiers applied.
ob = bpy.data.objects['ToyDemo']
bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); prop.select_set(True); bpy.context.view_layer.objects.active=ob
bpy.ops.object.modifier_apply(modifier='Subsurf')
bpy.ops.export_scene.fbx(filepath=os.path.join(source_dir,'Brawl_Demoman_Blue.fbx'), use_selection=True, apply_scale_options='FBX_SCALE_ALL', add_leaf_bones=False, bake_space_transform=True, path_mode='COPY', embed_textures=True)






bl_info = {
    "name": "The Simpsons Game Mesh Importer",
    "author": "Turk & Mister_Nebula",
    "version": (1, 0, 1),
    "blender": (4, 0, 0),
    "location": "File > Import-Export",
    "description": "Import .rws.preinstanced, .dff.preinstanced mesh files from The Simpsons Game (PS3)",
    "warning": "",
    "category": "Import-Export",
}

import bpy
import bmesh
import os
import struct
import re
import io
import math
import mathutils
import numpy as np

from bpy.props import (
    BoolProperty,
    FloatProperty,
    StringProperty,
    EnumProperty,
    CollectionProperty
)
from bpy_extras.io_utils import ImportHelper

def sanitize_uvs(uv_layer: bpy.types.MeshUVLoopLayer) -> None:
    """
    Replace NaN UV coordinates in the given UV layer with (0.0, 0.0).
    """
    print(f"[Sanitize] Checking UV layer: {uv_layer.name}")
    for i, uv in enumerate(uv_layer.data):
        if any([uv.uv.x != uv.uv.x, uv.uv.y != uv.uv.y]):  # NaN check
            print(f"[Sanitize] NaN UV at index {i} replaced with (0.0, 0.0)")
            uv.uv.x = 0.0
            uv.uv.y = 0.0

class SimpGameImport(bpy.types.Operator, ImportHelper):
    """
    A Blender operator for importing mesh files from The Simpsons Game.
    This operator handles .rws.preinstanced and .dff.preinstanced file formats,
    parsing their data and creating corresponding Blender objects.
    """
    print("== The Simpsons Game Import Log ==")

    bl_idname = "custom_import_scene.simpgame"
    bl_label = "Import"
    bl_options = {'PRESET', 'UNDO'}
    filter_glob: StringProperty(
        default="*.preinstanced",
        options={'HIDDEN'},
    )
    filepath: StringProperty(subtype='FILE_PATH',)
    files: CollectionProperty(type=bpy.types.PropertyGroup)

    def draw(self, context: bpy.types.Context) -> None:
        """
        Draws the operator UI (currently empty).
        """
        pass

    def execute(self, context: bpy.types.Context) -> set:
        """
        Executes the import operation for the selected file.
        Returns a set indicating the result status.
        """
        cur_file = open(self.filepath, "rb")
        print("== The Simpsons Game Import Log ==")

        cur_collection = bpy.data.collections.new("New Mesh")
        bpy.context.scene.collection.children.link(cur_collection)

        tmpRead = cur_file.read()
        mshBytes = re.compile(b"\x33\xEA\x00\x00....\x2D\x00\x02\x1C", re.DOTALL)
        mesh_iter = 0

        for x in mshBytes.finditer(tmpRead):
            cur_file.seek(x.end() + 4)
            FaceDataOff = int.from_bytes(cur_file.read(4), byteorder='little')
            MeshDataSize = int.from_bytes(cur_file.read(4), byteorder='little')
            MeshChunkStart = cur_file.tell()
            cur_file.seek(0x14, 1)
            mDataTableCount = int.from_bytes(cur_file.read(4), byteorder='big')
            mDataSubCount = int.from_bytes(cur_file.read(4), byteorder='big')

            for i in range(mDataTableCount):
                cur_file.seek(4, 1)
                cur_file.read(4)  # Skipping offsets

            mDataSubStart = cur_file.tell()

            for i in range(mDataSubCount):
                cur_file.seek(mDataSubStart + i * 0xC + 8)
                offset = int.from_bytes(cur_file.read(4), byteorder='big')
                cur_file.seek(offset + MeshChunkStart + 0xC)
                VertCountDataOff = int.from_bytes(cur_file.read(4), byteorder='big') + MeshChunkStart
                cur_file.seek(VertCountDataOff)
                VertChunkTotalSize = int.from_bytes(cur_file.read(4), byteorder='big')
                VertChunkSize = int.from_bytes(cur_file.read(4), byteorder='big')
                VertCount = int(VertChunkTotalSize / VertChunkSize)
                cur_file.seek(8, 1)
                VertexStart = int.from_bytes(cur_file.read(4), byteorder='big') + FaceDataOff + MeshChunkStart
                cur_file.seek(0x14, 1)
                FaceCount = int(int.from_bytes(cur_file.read(4), byteorder='big') / 2)
                cur_file.seek(4, 1)
                FaceStart = int.from_bytes(cur_file.read(4), byteorder='big') + FaceDataOff + MeshChunkStart

                cur_file.seek(FaceStart)
                StripList = []
                tmpList = []
                for f in range(FaceCount):
                    Indice = int.from_bytes(cur_file.read(2), byteorder='big')
                    if Indice == 65535:
                        StripList.append(tmpList.copy())
                        tmpList.clear()
                    else:
                        tmpList.append(Indice)

                FaceTable = []
                for f in StripList:
                    for f2 in strip2face(f):
                        FaceTable.append(f2)

                VertTable = []
                UVTable = []
                CMTable = []
                for v in range(VertCount):
                    cur_file.seek(VertexStart + v * VertChunkSize)
                    TempVert = struct.unpack('>fff', cur_file.read(4 * 3))
                    VertTable.append(TempVert)

                    cur_file.seek(VertexStart + v * VertChunkSize + VertChunkSize - 16)
                    TempUV = struct.unpack('>ff', cur_file.read(4 * 2))
                    UVTable.append((TempUV[0], 1 - TempUV[1]))

                    cur_file.seek(VertexStart + v * VertChunkSize + VertChunkSize - 8)
                    TempCM = struct.unpack('>ff', cur_file.read(4 * 2))
                    CMTable.append((TempCM[0], 1 - TempCM[1]))

                mesh1 = bpy.data.meshes.new("Mesh")
                mesh1.use_auto_smooth = True
                obj = bpy.data.objects.new("Mesh_" + str(mesh_iter) + "_" + str(i), mesh1)
                cur_collection.objects.link(obj)
                bpy.context.view_layer.objects.active = obj
                obj.select_set(True)
                mesh = bpy.context.object.data
                bm = bmesh.new()

                for v in VertTable:
                    bm.verts.new((v[0], v[1], v[2]))
                list = [v for v in bm.verts]

                for f in FaceTable:
                    try:
                        bm.faces.new((list[f[0]], list[f[1]], list[f[2]]))
                    except Exception as e:
                        print(f"[FaceError] Failed to create face {f}: {e}")
                        continue

                bm.to_mesh(mesh)

                uv_layer = bm.loops.layers.uv.verify()
                cm_layer = bm.loops.layers.uv.new()
                for f in bm.faces:
                    f.smooth = True
                    for l in f.loops:
                        luv = l[uv_layer]
                        lcm = l[cm_layer]
                        try:
                            luv.uv = UVTable[l.vert.index]
                            lcm.uv = CMTable[l.vert.index]
                        except Exception as e:
                            print(f"[UVError] Failed to assign UV for vert {l.vert.index}: {e}")
                            continue

                bm.to_mesh(mesh)
                sanitize_uvs(mesh.uv_layers[uv_layer.name])
                sanitize_uvs(mesh.uv_layers[cm_layer.name])

                bm.free()
                obj.rotation_euler = (1.5707963705062866, 0, 0)

            mesh_iter += 1

        cur_file.close()
        print("== Import Complete ==")
        return {'FINISHED'}

def strip2face(strip: list) -> list:
    """
    Converts a triangle strip to a list of triangle faces.
    """
    print(f"[Strip2Face] Converting strip of length {len(strip)} to faces")

    flipped = False
    tmpTable = []
    for x in range(len(strip)-2):
        if flipped:
            tmpTable.append((strip[x+2],strip[x+1],strip[x]))
        else:
            tmpTable.append((strip[x+1],strip[x+2],strip[x]))
        flipped = not flipped
    return tmpTable

def utils_set_mode(mode: str) -> None:
    """
    Sets the Blender object mode to the specified mode.
    """
    print(f"[SetMode] Setting mode to {mode}")

    if bpy.ops.object.mode_set.poll():
        bpy.ops.object.mode_set(mode=mode, toggle=False)

def menu_func_import(self, context: bpy.types.Context) -> None:
    """
    Adds the custom import operator to the Blender import menu.
    """
    print("[MenuFunc] Adding import option to menu")

    self.layout.operator(SimpGameImport.bl_idname, text="The Simpson Game (.rws,dff)")

def register() -> None:
    """
    Registers the import operator and menu function with Blender.
    """
    print("[Register] Registering import operator and menu function")

    bpy.utils.register_class(SimpGameImport)
    bpy.types.TOPBAR_MT_file_import.append(menu_func_import)

def unregister() -> None:
    """
    Unregisters the import operator and menu function from Blender.
    """
    print("[Unregister] Unregistering import operator and menu function")

    bpy.utils.unregister_class(SimpGameImport)
    bpy.types.TOPBAR_MT_file_import.remove(menu_func_import)

if __name__ == "__main__":
    print("[Main] Running as main script")
    register()

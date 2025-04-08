"""
This module provides an importer for The Simpsons Game files.
its loaded by the main.py script and used to import .preinstanced files into Blender.
It is designed to be run as a Blender addon, and it includes error handling and logging for debugging purposes.

"""

bl_info = {
    "name": "Simpsons Game Importer Test 6",
    "author": "samarixum",
    "version": (1, 6, 0),
    "blender": (4, 3, 2),
    "location": "File > Import-Export",
    "description": "test",
    "warning": "experimental",
    "category": "Import-Export",
}

buttontext = "Import Simpsons Game file V1.6 - script"

import bpy
import bmesh
import time
import os
import io
import struct
import math
import mathutils
import numpy as np
import re
from bpy.props import (BoolProperty, FloatProperty, StringProperty, EnumProperty, CollectionProperty)
from bpy_extras.io_utils import ImportHelper

class SimpGameImport(bpy.types.Operator, ImportHelper):
    """
    This class defines a Blender operator for importing .preinstanced files from The Simpsons Game.
    It handles file reading, mesh creation, and error reporting during the import process.
    """
    bl_idname = "custom_import_scene.simpgame"
    bl_label = "Import"
    bl_options = {'PRESET', 'UNDO'}
    filter_glob = StringProperty(default="*.preinstanced", options={'HIDDEN'},)
    filepath = StringProperty(subtype='FILE_PATH',)
    files = CollectionProperty(type=bpy.types.PropertyGroup)
    def draw(self, context):
        pass
    def execute(self, context):
        print(f"Importing file: {self.filepath} [Step 1]")
        time.sleep(1)
        try:
            CurFile = open(self.filepath,"rb")
            print(f"File opened successfully: {self.filepath} [Step 2]")
        except FileNotFoundError:
            self.report({'ERROR'}, f"Error 1: File not found: {self.filepath}")
            return {'CANCELLED'}
        except PermissionError:
            self.report({'ERROR'}, f"Error 2: Permission denied for: {self.filepath}")
            return {'CANCELLED'}
        except Exception as e:
            self.report({'ERROR'}, f"Error 3: Error opening file: {e}")
            return {'CANCELLED'}

        try:
            CurCollection = bpy.data.collections.new("New Mesh")
            bpy.context.scene.collection.children.link(CurCollection)
            print(f"Collection created: {CurCollection.name} [Step 3]")
        except Exception as e:
            self.report({'ERROR'}, f"Error 4: Error creating collection: {e}")
            CurFile.close()
            return {'CANCELLED'}

        try:
            tmpRead = CurFile.read()
            mshBytes = re.compile(b"\x33\xEA\x00\x00....\x2D\x00\x02\x1C",re.DOTALL)
            iter = 0
            print(f"Starting mesh chunk processing [Step 4]")
            for x in mshBytes.finditer(tmpRead):
                print(f"Processing mesh chunk {iter} [Step 5]")
                try:
                    CurFile.seek(x.end()+4)
                    FaceDataOff = int.from_bytes(CurFile.read(4),byteorder='little')
                    MeshDataSize = int.from_bytes(CurFile.read(4),byteorder='little')
                    MeshChunkStart = CurFile.tell()
                    CurFile.seek(0x14,1)
                    mDataTableCount = int.from_bytes(CurFile.read(4),byteorder='big')
                    mDataSubCount = int.from_bytes(CurFile.read(4),byteorder='big')
                    mDataOffsets = []
                    for i in range(mDataTableCount):
                        CurFile.seek(4,1)
                        mDataOffsets.append(int.from_bytes(CurFile.read(4),byteorder='big'))
                    mDataSubStart = CurFile.tell()
                    for i in range(mDataSubCount):#mDataSubCount
                        CurFile.seek(mDataSubStart+i*0xc+8)
                        offset = int.from_bytes(CurFile.read(4),byteorder='big')
                        chunkHead = CurFile.seek(offset+MeshChunkStart+0xC)
                        VertCountDataOff = int.from_bytes(CurFile.read(4),byteorder='big')+MeshChunkStart
                        CurFile.seek(VertCountDataOff)
                        VertChunkTotalSize = int.from_bytes(CurFile.read(4),byteorder='big')
                        VertChunkSize = int.from_bytes(CurFile.read(4),byteorder='big')
                        VertCount = int(VertChunkTotalSize/VertChunkSize)
                        CurFile.seek(8,1)
                        VertexStart = int.from_bytes(CurFile.read(4),byteorder='big')+FaceDataOff+MeshChunkStart
                        CurFile.seek(0x14,1)
                        FaceCount = int(int.from_bytes(CurFile.read(4),byteorder='big')/2)
                        CurFile.seek(4,1)
                        FaceStart = int.from_bytes(CurFile.read(4),byteorder='big')+FaceDataOff+MeshChunkStart

                    print(f"Found {VertCount} vertices and {FaceCount} faces in chunk {iter} [Output 1]")

                    CurFile.seek(FaceStart)
                    StripList = []
                    tmpList = []
                    for f in range(FaceCount):
                        Indice = int.from_bytes(CurFile.read(2),byteorder='big')
                        if Indice == 65535:
                            StripList.append(tmpList.copy())
                            tmpList.clear()
                        else:
                            tmpList.append(Indice)
                    FaceTable = []
                    for f in StripList:
                        try:
                            for f2 in strip2face(f):
                                FaceTable.append(f2)
                        except Exception as e:
                            self.report({'WARNING'}, f"Warning 1 in chunk {iter}: Error processing face strip: {e}")
                            continue

                    VertTable = []
                    UVTable = []
                    for v in range(VertCount):
                        CurFile.seek(VertexStart+v*VertChunkSize)
                        TempVert = struct.unpack('>fff', CurFile.read(4*3))
                        VertTable.append(TempVert)
                        CurFile.seek(VertexStart+v*VertChunkSize+VertChunkSize-8)
                        TempUV = struct.unpack('>ff', CurFile.read(4*2))
                        UVTable.append((TempUV[0],1-TempUV[1]))

                    print(f"Building mesh {iter}_{i} [Step 6]")

                    #build mesh
                    try:
                        mesh1 = bpy.data.meshes.new("Mesh")
                        obj = bpy.data.objects.new("Mesh_"+str(iter)+"_"+str(i), mesh1)
                        CurCollection.objects.link(obj)
                        bpy.context.view_layer.objects.active = obj
                        obj.select_set(True)
                        mesh = obj.data
                        bm = bmesh.new()
                        for v in VertTable:
                            bm.verts.new((v[0],v[1],v[2]))
                        list = [v for v in bm.verts]
                        for f in FaceTable:
                            try:
                                bm.faces.new((list[f[0]],list[f[1]],list[f[2]]))
                            except Exception as e:
                                #self.report({'WARNING'}, f"Warning 2 in chunk {iter}: Error creating face: {e} - Indices: {f}")
                                continue
                        bm.to_mesh(mesh)

                        uv_layer = bm.loops.layers.uv.verify()
                        for face in bm.faces:
                            face.smooth=True
                            for loop in face.loops:
                                try:
                                    luv = loop[uv_layer]
                                    luv.uv = UVTable[loop.vert.index]
                                except IndexError:
                                    self.report({'WARNING'}, f"Warning 3 in chunk {iter}: UV index out of range for vertex {loop.vert.index}")
                                except Exception as e:
                                    self.report({'WARNING'}, f"Warning 4 in chunk {iter}: Error setting UV: {e} for vertex {loop.vert.index}")
                        bm.to_mesh(mesh)
                        print(f"Mesh {obj.name} built successfully in chunk {iter} [Output 2]")
                    except Exception as e:
                        self.report({'ERROR'}, f"Error 5 in chunk {iter}: Error building mesh {iter}_{i}: {e}")
                    finally:
                        if 'bm' in locals() and bm.is_valid:
                            bm.free()
                    obj.rotation_euler = (1.5707963705062866,0,0)
                    iter += 1
                except struct.error as e:
                    self.report({'ERROR'}, f"Error 6 in chunk {iter}: Error reading binary data in chunk {iter}: {e} at offset {CurFile.tell()}")
                    continue
                except Exception as e:
                    self.report({'ERROR'}, f"Error 7 in chunk {iter}: Error processing mesh chunk {iter}: {e}")
                    continue
        except Exception as e:
            self.report({'ERROR'}, f"Error 8: General error during import: {e}")
        finally:
            if 'CurFile' in locals() and not CurFile.closed:
                CurFile.close()
                print("File closed. [Step 7]")
            del CurFile
            print("Import complete [Step 8]")
            return {'FINISHED'}


def strip2face(strip):
    try:
        flipped = False
        tmpTable = []
        for x in range(len(strip)-2):
            if flipped:
                tmpTable.append((strip[x+2],strip[x+1],strip[x]))
            else:
                tmpTable.append((strip[x+1],strip[x+2],strip[x]))
            flipped = not flipped
        print(f"Processed face strip of length {len(strip)} [Output 3]")
        return tmpTable
    except Exception as e:
        print(f"Error 9 in strip2face: {e}")
        return []

def utils_set_mode(mode):
    try:
        if bpy.ops.object.mode_set.poll():
            bpy.ops.object.mode_set(mode=mode, toggle=False)
        print(f"Mode set to {mode} [Output 4]")
    except Exception as e:
        print(f"Error 10 in utils_set_mode: {e}")

def menu_func_import(self, context):
    try:
        self.layout.operator(SimpGameImport.bl_idname, text=buttontext)
        print(f"Menu operator added [Output 5]")
    except Exception as e:
        print(f"Error 11 in menu_func_import: {e}")

def register():
    try:
        bpy.utils.register_class(SimpGameImport)
        bpy.types.TOPBAR_MT_file_import.append(menu_func_import)
        print(f"Addon registered [Output 6]")
    except Exception as e:
        print(f"Error 12 during registration: {e}")

def unregister():
    try:
        bpy.utils.unregister_class(SimpGameImport)
        bpy.types.TOPBAR_MT_file_import.remove(menu_func_import)
        print(f"Addon unregistered [Output 7]")
    except Exception as e:
        print(f"Error 13 during unregistration: {e}")

if __name__ == "__main__":
    register()
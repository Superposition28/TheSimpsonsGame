"""
This script automates the process of opening a Blender file, importing a .preinstanced file,
exporting the scene to .glb format, and then quitting Blender.
"""

import bpy
import sys
import os
import time

try:
    print("")
    # Get file paths from arguments
    base_blend_file_index = sys.argv.index('--') + 1 # example value "A:\TMP_TSG_LNKS\cc07225ea3a876253ae1dce564aba1f2_blend\play_sound.dff.PS3.blend"
    base_blend_file = sys.argv[base_blend_file_index]  # Full path to the base blend file
    print(f"1: \033[32mOpening blend file: {base_blend_file}\033[0m")

    input_preinstanced_file_index = sys.argv.index('--') + 2 # example value "A:\TMP_TSG_LNKS\cc07225ea3a876253ae1dce564aba1f2_preinstanced\play_sound.dff.PS3.preinstanced"
    input_preinstanced_file_name = sys.argv[input_preinstanced_file_index]
    input_preinstanced_file = input_preinstanced_file_name  # Full path to the input preinstanced file
    print(f"2: \033[32mImporting preinstanced file: {input_preinstanced_file}\033[0m")

    output_glb_index = sys.argv.index('--') + 3 # example value "A:\TMP_TSG_LNKS\cc07225ea3a876253ae1dce564aba1f2_glb\play_sound.dff.PS3.glb"
    output_glb_name = sys.argv[output_glb_index]
    output_glb = output_glb_name  # Full path to the output glb file
    print(f"3: \033[32mExporting to GLB file: {output_glb}\033[0m")

    pythonextension_file_index = sys.argv.index('--') + 4
    pythonextension_file = sys.argv[pythonextension_file_index]
    print(f"4: \033[32mUsing python extension: {pythonextension_file}\033[0m")

    print("")

    verbose_index = sys.argv.index('--') + 5
    verbose = sys.argv[verbose_index]
    if verbose == "true":
        print("Verbose mode enabled. Debugging information will be printed.")

    debugsleep_index = sys.argv.index('--') + 6
    debugsleep = sys.argv[debugsleep_index]
    if debugsleep == "true":
        print("Debug sleep mode enabled. The script will pause for debugging.")
        time.sleep(0.5)


    # Check if base_blend_file exists
    if not os.path.exists(base_blend_file):
        if debugsleep == "true":
            print("Debug sleep mode enabled. The script will pause for debugging.")
            time.sleep(5)
        raise FileNotFoundError(f"9: Blend file not found: {base_blend_file}")
    print(f"10: Blend file exists: {base_blend_file}")

    # Check if input_preinstanced_file exists
    if not os.path.exists(input_preinstanced_file):
        if debugsleep == "true":
            print("Debug sleep mode enabled. The script will pause for debugging.")
            time.sleep(5)
        raise FileNotFoundError(f"11: Preinstanced file not found: {input_preinstanced_file}")
    print(f"12: Preinstanced file exists: {input_preinstanced_file}")

    # Get the directory for output_glb and check if it exists
    output_dir = os.path.dirname(output_glb)

    if output_dir and not os.path.exists(output_dir):
        print(f"Creating output directory: {output_dir}")
        #os.makedirs(output_dir, exist_ok=True)
        print(f"13: Output directory cannot be made using symbolic link: {output_dir}")
        exit(1)
    #elif output_dir:
        #print(f"14: Output directory exists: {output_dir}")

    # Check if pythonextension_file exists
    if not os.path.exists(pythonextension_file):
        if debugsleep == "true":
            print("Debug sleep mode enabled. The script will pause for debugging.")
            time.sleep(5)
        raise FileNotFoundError(f"16: Python extension file not found: {pythonextension_file}")
    print(f"17: Python extension file exists: {pythonextension_file}")

    try:
        # Open the blend file
        bpy.ops.wm.open_mainfile(filepath=base_blend_file)
        print(f"18: Blend file opened: {base_blend_file}")
    except Exception as e:
        if debugsleep == "true":
            print("Debug sleep mode enabled. The script will pause for debugging.")
            time.sleep(5)
        raise Exception(f"19: Error opening blend file: {e}")

    #try:
    # Ensure your extension is enabled
    addon_module_name = 'io_import_simpson_game_fork'

    if not bpy.context.preferences.addons.get(addon_module_name):
        print(f"20: \033[32mEnabling {addon_module_name} addon\033[0m")
        # Path to the addon file
        pythonextension_file = os.path.abspath(pythonextension_file)
        print(f"Addon path: {pythonextension_file}")

        # Verify if the addon file exists
        if not os.path.isfile(pythonextension_file):
            raise FileNotFoundError(f"Addon file not found at: {pythonextension_file}")
        else:
            print(f"Addon file exists at: {pythonextension_file}")

        try:
            # Install the addon
            bpy.ops.preferences.addon_install(filepath=pythonextension_file, overwrite=True)
            print(f"21: Addon installed from: {pythonextension_file}")
            bpy.ops.preferences.addon_enable(module=addon_module_name)

        except Exception as e:
            if debugsleep == "true":
                print("Debug sleep mode enabled. The script will pause for debugging.")
                time.sleep(5)
            #raise Exception(f"22: Error installing addon: {e}")
            print(f"23: Error installing addon: {e}")

        try:
            # Enable the addon
            bpy.ops.preferences.addon_enable(module=addon_module_name)
            print(f"26: Addon {addon_module_name} enabled.")

            # Attempt to re-import the module
            import importlib
            importlib.invalidate_caches()  # Clear any cached module information
            addon_module = importlib.import_module(addon_module_name)
            print(f"26.1: Addon {addon_module_name} re-imported successfully.")
        except ModuleNotFoundError as e:
            #raise ModuleNotFoundError(f"27: Error enabling addon {addon_module_name}: {e}. " f"Ensure the addon file is correctly installed and named.")
            print(f"27: Error enabling addon {addon_module_name}: {e}. " f"Ensure the addon file is correctly installed and named.")
        except Exception as e:
            if debugsleep == "true":
                print("Debug sleep mode enabled. The script will pause for debugging.")
                time.sleep(5)
            raise Exception(f"27: Error enabling addon {addon_module_name}: {e}")
    else:
        print(f"28: \033[32m{addon_module_name} addon is already enabled\033[0m")
        ### previously removed and reinstalled the addon to ensure it was working, but currently disabled

        #try:
        #    bpy.ops.preferences.addon_disable(module=addon_module_name)
        #    bpy.ops.preferences.addon_remove(module=addon_module_name)
        #    print(f"29: Addon {addon_module_name} disabled and removed.")
        #except Exception as e:
        #    print(f"30: Error disabling/removing addon: {e}")

        #try:
        #    # Install the addon
        #    bpy.ops.preferences.addon_install(filepath=pythonextension_file, overwrite=True)
        #    print(f"31: Addon re-installed from: {pythonextension_file}")
        #    bpy.ops.preferences.addon_enable(module=addon_module_name)
        #except Exception as e:
        #    if debugsleep == "true":
        #        print("Debug sleep mode enabled. The script will pause for debugging.")
        #        time.sleep(5)
        #    print(f"32: Error re-installing addon: {e}")

        #try:
        #    # Enable the addon
        #    bpy.ops.preferences.addon_enable(module=addon_module_name)
        #    print(f"33: Addon {addon_module_name} enabled.")

        #    # Attempt to re-import the module
        #    import importlib
        #    importlib.invalidate_caches()  # Clear any cached module information
        #    addon_module = importlib.import_module(addon_module_name)
        #    print(f"33.1: Addon {addon_module_name} re-imported successfully.")
        #except ModuleNotFoundError as e:
        #    print(f"34: Error enabling addon {addon_module_name}: {e}. " f"Ensure the addon file is correctly installed and named.")
        #except Exception as e:
        #    if debugsleep == "true":
        #        print("Debug sleep mode enabled. The script will pause for debugging.")
        #        time.sleep(5)
        #    raise Exception(f"35: Error enabling addon {addon_module_name}: {e}")

    print(f"\033[32mImporting preinstanced file: {input_preinstanced_file}\033[0m")  # Green text for importing file

    try:
        # Call your custom import operator
        bpy.ops.custom_import_scene.simpgame(filepath=input_preinstanced_file)
        print(f"32: Preinstanced file imported: {input_preinstanced_file}")
    except Exception as e:
        if debugsleep == "true":
            print("Debug sleep mode enabled. The script will pause for debugging.")
            time.sleep(5)
        raise Exception(f"33: Error importing preinstanced file: {e}")

    try:
        # Save a copy of the blend file with the imported content
        saved_base_blend_file = os.path.splitext(base_blend_file)[0] + ".blend"
        print(f"34: Saving blend file as: {saved_base_blend_file}")

        # Ensure the directory exists
        saved_blend_dir = os.path.dirname(saved_base_blend_file)
        if not os.path.exists(saved_blend_dir):
            os.makedirs(saved_blend_dir, exist_ok=True)  # Create the directory if it doesn't exist
            print(f"35: Created directory for saved blend file: {saved_blend_dir}")
        else:
            print(f"36: Directory for saved blend file exists: {saved_blend_dir}")

        # Save the blend file
        #bpy.ops.wm.save_as_mainfile(filepath=saved_base_blend_file, check_existing=False)
        if bpy.data.is_dirty:
            bpy.ops.wm.save_mainfile()
            print("File saved.")
        else:
            print("No changes to save.")
        print(f"37: \033[32mSaved imported blend file: {saved_base_blend_file}\033[0m")  # Green text for saved file
    except Exception as e:
        if debugsleep == "true":
            print("Debug sleep mode enabled. The script will pause for debugging.")
            time.sleep(5)
        raise Exception(f"38: Error saving blend file: {e}")

    print(f"\033[32mExporting to GLB file: {output_glb}\033[0m")  # Green text for exporting file

    try:
        # Export the scene to .glb
        bpy.ops.export_scene.gltf(filepath=output_glb)
        print(f"39: Exported to GLB file: {output_glb}")
    except Exception as e:
        if debugsleep == "true":
            print("Debug sleep mode enabled. The script will pause for debugging.")
            time.sleep(5)
        raise Exception(f"40: Error exporting to GLB: {e}")

    print("41: \033[32mExport complete. Exiting Blender.\033[0m")  # Green text for export complete

    #except Exception as e:
    #    print(f"42: \033[31mError in main processing: {e}\033[0m")  # Red text for error message
    #    if debugsleep == "true":
    #        print("Debug sleep mode enabled. The script will pause for debugging.")
    #        time.sleep(5)
    #    #sys.exit(1)  # Exit script on error

except ValueError as e:
    print(f"43: \033[31mError: Incorrect number of arguments provided: {e}\033[0m")
    print("Usage: blender -b --python <script_name.py> -- <base_blend_file> <input_preinstanced_file> <output_glb> <pythonextension_file>")
    if debugsleep == "true":
        print("Debug sleep mode enabled. The script will pause for debugging.")
        time.sleep(5)
    sys.exit(1)
except FileNotFoundError as e:
    print(f"44: \033[31mError: File not found: {e}\033[0m")
    if debugsleep == "true":
        print("Debug sleep mode enabled. The script will pause for debugging.")
        time.sleep(5)
    sys.exit(1)
except Exception as e:
    print(f"45: \033[31mAn unexpected error occurred at the top level: {e}\033[0m")
    if debugsleep == "true":
        print("Debug sleep mode enabled. The script will pause for debugging.")
        time.sleep(5)
    sys.exit(1)
finally:
    # Exit Blender
    bpy.ops.wm.quit_blender()
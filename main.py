"""
This script processes game files for The Simpsons Game by performing various tasks such as:
- Extracting assets from STR archives
- Converting 3D models to .blend files
- Extracting textures from .txd archives
- Converting video files from .vp6 to .ogv
- Converting audio files from .snu to .wav
"""

from pathlib import Path
from printer import print, colours
import init
import os

status, path_value = init.main()

print(colours.CYAN, "Tool initialization complete press any key to continue...")
input()


if Path('Modules').exists():

    if Path('Modules\\Extract').exists() and Path('Modules\\Model').exists() and Path('Modules\\Texture').exists() and Path('Modules\\Video').exists() and Path('Modules\\Audio').exists() and Path('Modules\\Godot').exists():
        # menu to select which modules to run, or run all modules
        # clear all console output
        os.system('cls' if os.name == 'nt' else 'clear')
        print(colours.CYAN, "Select which modules to run:")
        print(colours.CYAN, "1. Extract")
        print(colours.CYAN, "2. Model")
        print(colours.CYAN, "3. Texture")
        print(colours.CYAN, "4. Video")
        print(colours.CYAN, "5. Audio")
        print(colours.CYAN, "6. Godot")
        print(colours.CYAN, "7. All")
        print(colours.CYAN, "0. Exit")
        choice = input("Enter your choice: ").strip()
        if choice == "1":
            import Modules.Extract.run as run_qbms
            run_qbms.main()
        elif choice == "2":
            import Modules.Model.run as run_model
            run_model.main()
        elif choice == "3":
            import Modules.Texture.run as run_texture
            run_texture.main()
        elif choice == "4":
            import Modules.Video.run as run_video
            run_video.main()
        elif choice == "5":
            import Modules.Audio.run as run_audio
            run_audio.main()
        elif choice == "6":
            import Modules.Godot.run as run_godot
            run_godot.main()
        elif choice == "7":
            # step A.1 obtain game files
            ## .vp6 video files, .snu audio files, .STR asset archives.

            # step A.2 extract assets from STR archives
            ## .preinstanced 3d models, .txd texture archives, etc.

            # step A.3 convert .preinstanced 3d models to .blend files
            ## uses blender and addon to import .preinstanced 3d models into .blend files
            ## optional: export .blend files to fbx and/or gLTF

            # step A.4 extract textures from .txd archives
            ## generates groups of png files for each .txd archive
            ## output is a folder with the same name as the .txd archive with _txdfiles appended to the name
            ## these folders are located are moved from the txd directory to a textures directory, maintaining the directory structure

            # step B.1 convert .vp6 video files to .ogv video files

            # step B.2 convert .snu audio files to .wav audio files


            # step A.2
            if Path('Modules\\Extract').exists():
                import Modules.Extract.run as run_qbms

                if not Path('Modules\\Extract\\GameFiles\\quickbms_out').exists():
                    print(colours.CYAN, "Running run_qbms")
                    run_qbms.main()
                else:
                    print(colours.CYAN, "QuickBMS output already exists. Skipping init and run.")

            # step A.3
            if Path('Modules\\Model').exists():
                import Modules.Model.run as run_model
                import Modules.Model.init as init_model
                import Modules.Model.proj_init as proj_init

                if not Path('Modules\\Model\\GameFiles\\blend_out').exists():
                    print(colours.CYAN, "Running proj_init")
                    proj_init.main()

                    print(colours.CYAN, "Running init_model")
                    init_model.main()

                    print(colours.CYAN, "Running run_model")
                    verbose_input = input("verbose Y/N: ").strip().upper()
                    if verbose_input == "Y":
                        verbose = True
                    elif verbose_input == "N":
                        verbose = False
                    else:
                        print(colours.CYAN, "Invalid input. Defaulting to False.")
                        verbose = False

                    debug_sleep_input = input("debug_sleep Y/N: ").strip().upper()
                    if debug_sleep_input == "Y":
                        debug_sleep = True
                    elif debug_sleep_input == "N":
                        debug_sleep = False
                    else:
                        print(colours.CYAN, "Invalid input. Defaulting to False.")
                        debug_sleep = False

                    export_input = input("export Y/N: ").strip().upper()
                    if export_input == "Y":
                        export = True
                    elif export_input == "N":
                        export = False
                    else:
                        print(colours.CYAN, "Invalid input. Defaulting to True.")
                        export = True

                    run_model.main(verbose, debug_sleep, export)
                else:
                    print(colours.CYAN, "Model output already exists.")

            # step A.4
            if Path('Modules\\Texture').exists():
                import Modules.Texture.run as run_texture
                if not Path('Modules\\Texture\\GameFiles\\Textures_out').exists():

                    print(colours.CYAN, "Running run_texture")
                    run_texture.main()

            # step B.1
            if Path('Modules\\Video').exists():
                import Modules.Video.run as run_video
                if not Path('Modules\\Video\\GameFiles\\Assets_1_Video_Movies').exists():
                    print(colours.CYAN, "Running run_video")
                    run_video.main()
                else:
                    print(colours.CYAN, "Video output already exists.")

            # step B.2
            if Path('Modules\\Audio').exists():
                import Modules.Audio.run as run_audio
                if not Path('Modules\\Audio\\GameFiles\\Assets_1_Audio_Streams').exists():
                    print(colours.CYAN, "Running run_audio")
                    run_audio.main()
                else:
                    print(colours.CYAN, "Audio output already exists.")
        elif choice == "0":
            print(colours.CYAN, "Exiting...")
            exit()

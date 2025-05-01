"""
This script processes game files for The Simpsons Game by performing various tasks such as:
- Extracting assets from STR archives
- Converting 3D models to .blend files
- Extracting textures from .txd archives
- Converting video files from .vp6 to .ogv
- Converting audio files from .snu to .wav
"""

from pathlib import Path


if Path('Modules').exists():

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
        import Modules.Extract.init as init_qbms

        if not Path('Modules\\Extract\\GameFiles\\quickbms_out').exists():
            print("Running init_qbms")
            init_qbms.main()

            print("Running run_qbms")
            run_qbms.main()
        else:
            print("QuickBMS output already exists. Skipping init and run.")

	# step A.3
    if Path('Modules\\Model').exists():
        import Modules.Model.run as run_model
        import Modules.Model.init as init_model
        import Modules.Model.proj_init as proj_init

        if not Path('Modules\\Model\\GameFiles\\blend_out').exists():
            print("Running proj_init")
            proj_init.main()

            print("Running init_model")
            init_model.main()

            print("Running run_model")
            verbose_input = input("verbose Y/N: ").strip().upper()
            if verbose_input == "Y":
                verbose = True
            elif verbose_input == "N":
                verbose = False
            else:
                print("Invalid input. Defaulting to False.")
                verbose = False

            debug_sleep_input = input("debug_sleep Y/N: ").strip().upper()
            if debug_sleep_input == "Y":
                debug_sleep = True
            elif debug_sleep_input == "N":
                debug_sleep = False
            else:
                print("Invalid input. Defaulting to False.")
                debug_sleep = False

            export_input = input("export Y/N: ").strip().upper()
            if export_input == "Y":
                export = True
            elif export_input == "N":
                export = False
            else:
                print("Invalid input. Defaulting to True.")
                export = True

            run_model.main(verbose, debug_sleep, export)
        else:
            print("Model output already exists.")

	# step A.4
    if Path('Modules\\Texture').exists():
        import Modules.Texture.run as run_texture

        print("Running run_texture")
        run_texture.main()

	# step B.1
    if Path('Modules\\Video').exists():
        import Modules.Video.run as run_video
        if not Path('Modules\\Video\\GameFiles\\Assets_1_Video_Movies').exists():
            print("Running run_video")
            run_video.main()
        else:
            print("Video output already exists.")

	# step B.2
    if Path('Modules\\Audio').exists():
        import Modules.Audio.run as run_audio
        if not Path('Modules\\Audio\\GameFiles\\Assets_1_Audio_Streams').exists():
            print("Running run_audio")
            run_audio.main()
        else:
            print("Audio output already exists.")


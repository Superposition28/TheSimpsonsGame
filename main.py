
import os

if os.path.exists('Modules'):
    if os.path.exists('Modules\\QBMS_TSG'):
        import Modules.QBMS_TSG.run as run_qbms
        import Modules.QBMS_TSG.init as init_qbms

        if not os.path.exists('Modules\\QBMS_TSG\\GameFiles\\quickbms_out'):
            print("Running init_qbms")
            init_qbms.main()

            print("Running run_qbms")
            run_qbms.main()
        else:
            print("QuickBMS output already exists. Skipping init and run.")

    if os.path.exists('Modules\\Model'):
        import Modules.Model.run as run_model
        import Modules.Model.init as init_model
        import Modules.Model.proj_init as proj_init

        if not os.path.exists('Modules\\Model\\GameFiles\\blend_out'):
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
            print("Model output already exists. Skipping init and run.")

    if os.path.exists('Modules\\Video'):
        import Modules.Video.run as run_video
        import Modules.Video.init as init_video

        if not os.path.exists('Modules\\Video\\GameFiles\\Assets_1_Video_Movies'):
            print("Running init_video")
            init_video.main()

            print("Running run_video")
            run_video.main()
        else:
            print("Video output already exists. Skipping init and run.")


    if os.path.exists('Modules\\Audio'):
        import Modules.Audio.run as run_audio
        if not os.path.exists('Modules\\Audio\\GameFiles\\Assets_1_Audio'):
            print("Running run_audio")
            run_audio.main()
        else:
            print("Video output already exists. Skipping init and run.")


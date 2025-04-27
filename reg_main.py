import os
import json
import hashlib
import shutil  # For copying directories

def generate_uuid(file_path):
    """Generates a UUID based on the file hash and path hash."""
    with open(file_path, 'rb') as f:
        file_content = f.read()
        file_hash = hashlib.sha256(file_content).hexdigest()
    path_hash = hashlib.md5(file_path.encode()).hexdigest()
    return f"{file_hash[:16]}_{path_hash[:16]}"

def predict_converted_path(source_path, asset_type, stage, source_name):
    """
    Predicts the path of a converted asset based on its source path, type, and stage.
    """
    source_dir, filename = os.path.split(source_path)
    name, ext = os.path.splitext(filename)

    if asset_type == "models":
        if stage == ".preinstanced":
            return source_path  # No change
        elif stage == ".blend":
            return os.path.join(r"Modules\Model\GameFiles\blend_out", os.path.relpath(source_dir, r"Modules\QBMS_TSG\GameFiles\quickbms_out"), f"{name}.blend")
        elif stage == ".glb":
            return os.path.join(r"Modules\Model\GameFiles\blend_out_glb", os.path.relpath(source_dir, r"Modules\QBMS_TSG\GameFiles\quickbms_out"), f"{name}.glb")
        elif stage == ".fbx":
            return os.path.join(r"Modules\Model\GameFiles\blend_out_fbx", os.path.relpath(source_dir, r"Modules\QBMS_TSG\GameFiles\quickbms_out"), f"{name}.fbx")
    elif asset_type == "textures":
        if stage == ".txd":
            # Create folder name based on the source TXD file
            return os.path.join(source_dir, f"{name}.txd_files")
        elif stage == ".png_directory":
            return os.path.join(r"Modules\Texture_Assets\GameFiles\Textures_out", os.path.relpath(source_dir, r"Modules\QBMS_TSG\GameFiles\quickbms_out"), f"{name}.png")

    elif asset_type == "audio":
        if stage == ".wav":
            return os.path.join(r"Modules\Audio\GameFiles", os.path.relpath(source_dir, r"Modules\QBMS_TSG\GameFiles\USRDIR"), f"{name}.wav")
    elif asset_type == "video":
        if stage == ".ogv":
            return os.path.join(r"A:\Dev\Games\TheSimpsonsGame\PAL\Modules\Video\GameFiles\Assets_1_Video_Movies", os.path.relpath(source_dir, r"Modules\QBMS_TSG\GameFiles\USRDIR"), f"{name}.ogv")
    return None

def scan_directories(directories):
    """
    Scans the specified directories for source assets and generates the asset_index.json
    with predicted converted paths.

    Args:
        directories: A list of root directory paths to scan.
    """
    asset_index = {"models": [], "textures": [], "audio": [], "video": [], "unknown": []}

    for directory in directories:
        print(f"Scanning directory: {directory}")
        for root, _, files in os.walk(directory):
            print(f"Processing directory: {root}")
            for filename in files:
                #print(f"Processing file: {filename}")

                file_path = os.path.join(root, filename)
                try:
                    file_hash_full = hashlib.sha256()
                    with open(file_path, "rb") as f:
                        while chunk := f.read(4096):
                            file_hash_full.update(chunk)
                    file_hash = file_hash_full.hexdigest()
                    relative_path = os.path.relpath(file_path, os.getcwd())
                    path_name_hash_md5 = hashlib.md5(relative_path.encode()).hexdigest()
                    uuid = f"{file_hash[:16]}_{path_name_hash_md5[:16]}"
                    entry = {
                        "uuid": uuid,
                        "sourceFileName": filename,
                        "sourcePath": relative_path,
                        "fileHash": file_hash,
                        "pathNameHashMD5": path_name_hash_md5,
                        "stages": {} # To track different stages of the asset
                    }

                    asset_type = "unknown"
                    current_stage = None
                    source_name = os.path.splitext(filename)[0] # Added source_name

                    if filename.lower().endswith(('.preinstanced')):
                        asset_type = "models"
                        current_stage = ".preinstanced"
                    elif filename.lower().endswith(('.txd')):
                        asset_type = "textures"
                        current_stage = ".txd"
                    elif filename.lower().endswith(('.snu')):
                        asset_type = "audio"
                        current_stage = ".snu"
                    elif filename.lower().endswith(('.vp6')):
                        asset_type = "video"
                        current_stage = ".vp6"
                    else:
                        #asset_index["unknown"].append(entry)
                        continue # Skip the rest of the processing for unknown files

                    entry["stages"][current_stage] = {"exists": os.path.exists(file_path), "path": relative_path}

                    # Predict other stages
                    if asset_type == "models":
                        predicted_blend = predict_converted_path(relative_path, "models", ".blend", source_name)
                        if predicted_blend:
                            blend_exists = os.path.exists(predicted_blend)
                            entry["stages"][".blend"] = {"exists": blend_exists, "path": predicted_blend}
                        predicted_glb = predict_converted_path(relative_path, "models", ".glb", source_name)
                        if predicted_glb:
                            glb_exists = os.path.exists(predicted_glb)
                            entry["stages"][".glb"] = {"exists": glb_exists, "path": predicted_glb}
                        predicted_fbx = predict_converted_path(relative_path, "models", ".fbx", source_name)
                        if predicted_fbx:
                            fbx_exists = os.path.exists(predicted_fbx)
                            entry["stages"][".fbx"] = {"exists": fbx_exists, "path": predicted_fbx}
                    elif asset_type == "textures":
                        txd_dir = predict_converted_path(relative_path, "textures", ".txd", source_name)
                        if txd_dir:
                            txd_exists = os.path.exists(txd_dir)
                            entry["stages"][".png_directory"] = {"exists": txd_exists, "path": txd_dir}
                    elif asset_type == "audio":
                        predicted_wav = predict_converted_path(relative_path, "audio", ".wav", source_name)
                        if predicted_wav:
                            wav_exists = os.path.exists(predicted_wav)
                            entry["stages"][".wav"] = {"exists": wav_exists, "path": predicted_wav}
                    elif asset_type == "video":
                        predicted_ogv = predict_converted_path(relative_path, "video", ".ogv", source_name)
                        if predicted_ogv:
                            ogv_exists = os.path.exists(predicted_ogv)
                            entry["stages"][".ogv"] = {"exists": ogv_exists, "path": predicted_ogv}

                    asset_index[asset_type].append(entry)

                except Exception as e:
                    print(f"Error processing file: {file_path} - {e}")

    with open("asset_index.json", "w") as f:
        json.dump(asset_index, f, indent=4)

if __name__ == "__main__":
    directories_to_scan = [
        r"Modules\QBMS_TSG\GameFiles\quickbms_out",
        r"Modules\QBMS_TSG\GameFiles\USRDIR\Assets_1_Audio_Streams\EN",
        r"Modules\QBMS_TSG\GameFiles\USRDIR\Assets_1_Audio_Streams\Global",
        r"Modules\QBMS_TSG\GameFiles\USRDIR\Assets_1_Video_Movies\en",
		r"Modules\QBMS_TSG\GameFiles\USRDIR\Assets_1_Video_Movies\sf",
    ]

    scan_directories(directories_to_scan)
    print("Generated asset_index.json")
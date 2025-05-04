import os
import json

def get_current_directory() -> str:
    """Returns the full path of the current directory."""
    return os.path.abspath(os.getcwd())

def detect_external_tools(base_directory: str) -> dict:
    """Detects specified external tools in subdirectories (excluding blacklisted ones)."""
    blacklist = ["GameFiles"]

    tools = {
        "QuickBMSjson": "Extract.json"
    }

    found_tools = {}
    for root, dirs, files in os.walk(base_directory):
        dirs[:] = [d for d in dirs if d not in blacklist]
        for key, tool in tools.items():
            if tool in files:
                found_tools[key] = os.path.join(root, tool)
    return found_tools

def generate_config_with_tools(file_path: str, tools: dict) -> None:
    """Generates a JSON configuration file pre-filled with detected external tool paths."""
    config_data = {
        'FilePaths': {},
        'Tools': tools
    }

    with open(file_path, 'w') as configfile:
        json.dump(config_data, configfile, indent=4)
    print(f"Configuration file generated with tool paths at {file_path}")

def read_config(file_path: str) -> dict:
    """Reads and returns the contents of a JSON configuration file."""
    with open(file_path, 'r') as configfile:
        config_data = json.load(configfile)
    return config_data

if __name__ == "__main__":
    config_file_path = "project.json"
    base_dir = get_current_directory()

    # Ensure GameFiles directory exists
    game_files_dir = os.path.join(base_dir, "GameFiles")
    if not os.path.exists(game_files_dir):
        os.makedirs(game_files_dir)
        print(f"Created GameFiles directory at {game_files_dir}")

    if not os.path.exists(config_file_path):
        print(f"Configuration file {config_file_path} does not exist. Creating a new one.")
        # Detect tools and generate the config
        tools = detect_external_tools(base_dir)
        generate_config_with_tools(config_file_path, tools)
    else:
        print(f"Configuration file {config_file_path} already exists. Reading the existing one.")
        # Read the existing config
        config_data = read_config(config_file_path)
        print(config_data)


import re
import os

class ConfigReader:
    def __init__(self, ini_path):
        self.ini_path = ini_path
        self.config = {}
        self._parse_ini()

    def _parse_ini(self):
        if not os.path.exists(self.ini_path):
            raise FileNotFoundError(f"INI file not found: {self.ini_path}")

        with open(self.ini_path, 'r') as file:
            lines = file.readlines()

        current_section = None
        section_dict = {}

        for line in lines:
            line = line.strip()

            # Ignore empty lines
            if not line:
                continue

            # Check if the line is a section header
            if line.startswith("[") and line.endswith("]"):
                # Store the previous section
                if current_section is not None:
                    self.config[current_section] = section_dict

                # Update the current section
                current_section = line[1:-1].strip()
                section_dict = {}
            else:
                # Parse key-value pairs
                match = re.match(r"^([^=]+)=(.+)$", line)
                if match and current_section is not None:
                    key = match.group(1).strip()
                    value = match.group(2).strip()
                    section_dict[key] = value

        # Add the last section
        if current_section is not None:
            self.config[current_section] = section_dict

    def get_config_value(self, section, key, default_value=""):
        return self.config.get(section, {}).get(key, default_value)


def main(section: str, key: str, default_value: str, ini_file_path: str = "./config.ini") -> str:
    """
    Main function to retrieve a configuration value from the INI file.
    Accepts parameters: Section, Key, DefaultValue, and optional INI file path.
    Returns the retrieved configuration value as a string.
    """
    # Create an instance of the ConfigReader class
    config_reader = ConfigReader(ini_file_path)

    # Get the value from the config
    value = config_reader.get_config_value(section, key, default_value)

    # Return the value
    return value

# Read the file
print("Reading the file: audio.tree")
with open("audiotree.tree", "r", encoding="utf-8") as f:
    lines = f.readlines()

# Remove duplicates while preserving order
print("Removing duplicate lines...")
seen = set()
unique_lines = []
for line in lines:
    if line not in seen:
        seen.add(line)
        unique_lines.append(line)

# Write the cleaned data back to a new file
print("Writing cleaned data to Audio.md")
with open("Audio.md", "w", encoding="utf-8") as f:
    f.writelines(unique_lines)

print("Process completed successfully.")

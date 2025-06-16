import json
import argparse
import pathlib
from collections.abc import Generator

"""
Python file that reads a json file and converts the format to be approppriate for xaml in C# MAUI

To use this file, place the JSON file containing colors from Figma named "colors.json" next to this
file (in the same directory) and run the script, it then outputs a "colors.xaml" file.

You can also take your old <colors.xaml>, rename it to <old_colors.xaml> and the script will
print to terminal, telling what files are added or missing from the new colors.

> Note: The diff is generated on a per-line basis, meaning it will show a change even if it was just
fixing a spelling error or one letter/digit in the hex-code changed.
"""

p = pathlib.Path(__file__).parent.resolve()
json_path = p / pathlib.Path("colors.json")
new_colors_path = p / ("../NeuroAccessMaui/Resources/Styles/colors.xaml")
old_colors_path = p / ("old_colors.xaml")

# Converts hex color from figma to one supported by maui
# Change #XXXXXXAA -> #AAXXXXXX Where A is alpha value
def figma_hex_to_maui(str) -> str:
    if len(str) == 7:
        return str
    elif len(str) == 9:
        return f"#{str[-2:]}{str[1:-2]}"

#Remove Whitespace from names and replace ampersands
def filter_name(str) -> str:
    return "".join(str.split(" ")).replace("&", "").replace("/", "").replace("-", "")

# Gets JSON Object from color list and returns the name and color value.
def json_color_list_to_name_color_pairs(list) -> Generator[str, None, None]:
    for color in list:
        yield (filter_name(color["name"]), figma_hex_to_maui(color["value"]))

# Takes the Name and Color pairs and creates the lines in colors.xaml
def json_color_list_xaml_list(list, dark_mode) -> Generator[str, None, None]:
    for color_name, color_value in json_color_list_to_name_color_pairs(list):
        yield f'\t<Color x:Key="{color_name}{"Dark" if dark_mode else "Light"}">{color_value}</Color>'

xaml_doc_start = """
<?xml version="1.0" encoding="UTF-8" ?>
<?xaml-comp compile="true" ?>
<ResourceDictionary
\txmlns="http://schemas.microsoft.com/dotnet/2021/maui"
\txmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">
"""

xaml_doc_end = "</ResourceDictionary>"

def create_xaml_file(alternative: bool) -> str:
    with open(json_path) as f:
        data = json.load(f)[0]["values"]
        if alternative:
            formatted_light_colors = list(json_color_list_xaml_list(data[3]["color"], False))
            formatted_dark_colors = list(json_color_list_xaml_list(data[2]["color"], True))
        else:
            formatted_light_colors = list(json_color_list_xaml_list(data[0]["color"], False))
            formatted_dark_colors = list(json_color_list_xaml_list(data[1]["color"], True))

    result = "\n".join([xaml_doc_start] + formatted_light_colors + formatted_dark_colors + [xaml_doc_end])
    
    with open(new_colors_path, "w") as f:
        f.write(result[1:])

    return result


parser = argparse.ArgumentParser(
    prog='ColorConverter',
    description='Converts JSON colors from figma to colors.xaml, usable in a C# MAUI app',
    epilog='Wow you actually read documentation, good for you. Happy easter (Egg)'
)

parser.add_argument("-a", "--alternative", action="store_true", help="If flag is set, creates the xaml file used for second light/dark pair of themes")

args = parser.parse_args()

result = create_xaml_file(args.alternative)

# Check if there is a file containing the old colors and print information of what colors have changed
try:
    with open(old_colors_path, "rt") as f:
        f = f.read()
        
        print("Added lines in new file:")
        for line in result.split("\n"):
            if line not in f:
                print(line)

        print("Removed lines in new file:")
        for line in f.split("\n"):
            if line not in result:
                print(f"{line}")

except:
    print("No old file to provide a diff of colors changed")
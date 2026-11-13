"""Generate Bus Jam visual refresh PNGs. Requires Pillow; originals stay untouched."""

from hashlib import md5
from pathlib import Path
import re

from PIL import Image, ImageDraw, ImageFilter


PROJECT = Path(__file__).resolve().parents[1]
ASSETS = PROJECT / "Assets"
UI = ASSETS / "Art" / "Sprites" / "UI"
OUT_UI = UI / "Refresh"
VEHICLES = ASSETS / "Resources" / "Vehicles"
OUT_BUS = VEHICLES / "bus_compact"
SCALE = 2


def guid(path):
    return md5(("bus-jam-visual-refresh-v1:" + path).encode("utf-8")).hexdigest()


def folder_meta(folder):
    path = folder.with_name(folder.name + ".meta")
    if not path.exists():
        rel = folder.relative_to(ASSETS).as_posix()
        path.write_text(
            f"fileFormatVersion: 2\nguid: {guid(rel)}\nfolderAsset: yes\n"
            "DefaultImporter:\n  externalObjects: {}\n  userData: \n"
            "  assetBundleName: \n  assetBundleVariant: \n",
            encoding="utf-8",
        )


def save_sprite(image, output, template):
    output.parent.mkdir(parents=True, exist_ok=True)
    folder_meta(output.parent)
    image.resize((image.width // SCALE, image.height // SCALE), Image.Resampling.LANCZOS).save(output)
    rel = output.relative_to(ASSETS).as_posix()
    meta = template.with_name(template.name + ".meta").read_text(encoding="utf-8")
    meta = re.sub(r"^guid: \S+", f"guid: {guid(rel)}", meta, count=1, flags=re.MULTILINE)
    output.with_name(output.name + ".meta").write_text(meta, encoding="utf-8")
    print(rel, guid(rel))


def canvas(size):
    return Image.new("RGBA", (size[0] * SCALE, size[1] * SCALE), (0, 0, 0, 0))


def box(coords):
    return tuple(int(v * SCALE) for v in coords)


def rr(draw, coords, radius, fill, outline=None, width=1):
    draw.rounded_rectangle(box(coords), radius * SCALE, fill, outline, width * SCALE)


def line(draw, points, color, width):
    draw.line([(int(x * SCALE), int(y * SCALE)) for x, y in points], fill=color,
              width=width * SCALE, joint="curve")


def bus(color):
    image = canvas((768, 256))
    draw = ImageDraw.Draw(image)
    dark, base, light = color
    # Underbody and wheels stay within the 3-cell canvas.
    rr(draw, (70, 22, 700, 246), 92, (17, 33, 50, 60))
    for x in (170, 535):
        rr(draw, (x, 10, x + 88, 43), 13, (23, 38, 55, 255))
        rr(draw, (x, 213, x + 88, 246), 13, (23, 38, 55, 255))
        rr(draw, (x + 13, 13, x + 75, 25), 6, (78, 99, 114, 255))
        rr(draw, (x + 13, 231, x + 75, 242), 6, (78, 99, 114, 255))
    rr(draw, (65, 22, 703, 231), 88, dark, (18, 45, 67, 255), 5)
    rr(draw, (75, 29, 693, 222), 79, base, light, 4)
    rr(draw, (87, 38, 681, 212), 70, light)
    rr(draw, (94, 45, 674, 205), 63, base)
    # Roof and three individually drawn seats, not a stretched two-seat image.
    rr(draw, (146, 54, 555, 196), 42, (22, 47, 68, 255), (10, 31, 49, 255), 4)
    rr(draw, (155, 62, 547, 188), 35, (53, 94, 119, 255), (110, 168, 183, 255), 2)
    line(draw, [(171, 76), (524, 76)], (171, 218, 225, 145), 3)
    for x in (188, 317, 446):
        rr(draw, (x, 83, x + 77, 169), 24, (21, 54, 77, 255), (134, 195, 209, 255), 2)
        rr(draw, (x + 10, 91, x + 67, 130), 14, (102, 164, 188, 255))
        rr(draw, (x + 10, 136, x + 67, 160), 10, (75, 132, 158, 255))
    # Windshield, lights and body highlights.
    rr(draw, (571, 52, 655, 198), 32, (23, 49, 72, 255), (13, 32, 49, 255), 4)
    rr(draw, (580, 62, 644, 188), 24, (71, 128, 150, 255))
    line(draw, [(590, 75), (623, 75)], (190, 230, 235, 180), 4)
    rr(draw, (669, 56, 687, 91), 8, (244, 253, 230, 255))
    rr(draw, (669, 165, 687, 200), 8, (244, 253, 230, 255))
    rr(draw, (82, 57, 96, 86), 6, (255, 114, 103, 255))
    rr(draw, (82, 171, 96, 200), 6, (255, 114, 103, 255))
    line(draw, [(148, 38), (544, 38)], (255, 255, 255, 155), 3)
    line(draw, [(148, 214), (544, 214)], (15, 44, 65, 105), 3)
    return image


def panel():
    image = canvas((760, 920))
    draw = ImageDraw.Draw(image)
    rr(draw, (17, 23, 743, 908), 55, (21, 42, 61, 80))
    rr(draw, (12, 10, 748, 892), 56, (26, 70, 89, 255))
    rr(draw, (20, 17, 740, 882), 50, (240, 248, 245, 255))
    rr(draw, (29, 26, 731, 872), 43, (255, 253, 244, 255))
    line(draw, [(66, 59), (694, 59)], (216, 234, 224, 255), 3)
    return image


def pill(size, outer, inner, shine, radius=None):
    image = canvas(size)
    d = ImageDraw.Draw(image)
    w, h = size
    r = radius or int(h * 0.45)
    rr(d, (4, 7, w - 4, h - 2), r, (12, 39, 52, 85))
    rr(d, (4, 2, w - 4, h - 9), r, outer)
    rr(d, (10, 7, w - 10, h - 15), max(3, r - 5), inner)
    rr(d, (16, 10, w - 16, int(h * 0.45)), max(3, r - 9), shine)
    return image


def round_button(size, outer, inner, shine, symbol):
    image = pill(size, outer, inner, shine, int(size[1] * 0.32))
    d = ImageDraw.Draw(image)
    w, h = size
    if symbol == "pause":
        rr(d, (w * 0.36, h * 0.28, w * 0.45, h * 0.68), 6, (255, 255, 249, 255))
        rr(d, (w * 0.55, h * 0.28, w * 0.64, h * 0.68), 6, (255, 255, 249, 255))
    elif symbol == "close":
        line(d, [(w * 0.34, h * 0.30), (w * 0.66, h * 0.64)], (255, 255, 250, 255), 11)
        line(d, [(w * 0.66, h * 0.30), (w * 0.34, h * 0.64)], (255, 255, 250, 255), 11)
    return image


def reward_panel():
    image = canvas((560, 250))
    d = ImageDraw.Draw(image)
    rr(d, (5, 9, 555, 245), 30, (28, 92, 102, 255))
    rr(d, (11, 10, 549, 236), 26, (217, 243, 231, 255))
    rr(d, (17, 16, 543, 229), 22, (235, 250, 240, 255))
    line(d, [(46, 43), (514, 43)], (255, 255, 255, 195), 3)
    return image


def main():
    palettes = {
        "blue": ((24, 85, 158, 255), (45, 149, 224, 255), (111, 202, 250, 255)),
        "red": ((151, 48, 49, 255), (224, 83, 72, 255), (255, 157, 131, 255)),
        "green": ((33, 111, 75, 255), (62, 180, 110, 255), (145, 232, 165, 255)),
        "yellow": ((156, 100, 25, 255), (236, 180, 46, 255), (255, 225, 126, 255)),
    }
    for name, colors in palettes.items():
        src = VEHICLES / "bus_seat" / f"bus_seat_{name}_right.png"
        save_sprite(bus(colors), OUT_BUS / f"bus_compact_{name}_right.png", src)

if __name__ == "__main__":
    main()

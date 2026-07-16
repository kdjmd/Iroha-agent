from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
BASE_PATH = ROOT / "assets" / "character" / "iroha-portrait.png"
SOURCE_DIR = ROOT / "assets" / "character" / "expression-sources"
OUTPUT_DIR = ROOT / "assets" / "character" / "expressions"

FRAME_MASKS = {
    "iroha-blink-half.png": (
        [(340, 170, 443, 275), (416, 150, 526, 265)],
        10,
    ),
    "iroha-blink-closed.png": (
        [(340, 170, 443, 275), (416, 150, 526, 265)],
        10,
    ),
    "iroha-speak-small.png": (
        [(402, 240, 495, 326)],
        12,
    ),
    "iroha-speak-open.png": (
        [(402, 240, 495, 326)],
        12,
    ),
}


def build_mask(size, regions, feather):
    mask = Image.new("L", size, 0)
    draw = ImageDraw.Draw(mask)
    for region in regions:
        draw.ellipse(region, fill=255)
    return mask.filter(ImageFilter.GaussianBlur(feather))


def composite_frame(base, source, regions, feather):
    mask = build_mask(base.size, regions, feather)
    source_alpha = source.getchannel("A")
    patch = source.copy()
    patch.putalpha(ImageChops.multiply(mask, source_alpha))
    result = base.copy()
    result.alpha_composite(patch)
    return result


def main():
    base = Image.open(BASE_PATH).convert("RGBA")
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

    for filename, (regions, feather) in FRAME_MASKS.items():
        source_path = SOURCE_DIR / filename
        if not source_path.exists():
            raise FileNotFoundError(source_path)
        source = Image.open(source_path).convert("RGBA")
        if source.size != base.size:
            raise ValueError(f"{filename}: expected {base.size}, got {source.size}")
        result = composite_frame(base, source, regions, feather)
        result.save(OUTPUT_DIR / filename, optimize=True)
        print(f"Built {OUTPUT_DIR / filename}")


if __name__ == "__main__":
    main()

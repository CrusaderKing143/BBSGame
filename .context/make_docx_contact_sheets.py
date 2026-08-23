from pathlib import Path
import sys

from PIL import Image, ImageDraw


SOURCE = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(
    r"E:/unitypro/pro1/BBSGame/.context/docx_after_word/pages"
)
OUTPUT = Path(sys.argv[2]) if len(sys.argv) > 2 else Path(
    r"E:/unitypro/pro1/BBSGame/.context/docx_after_word/contact_sheets"
)
THUMB_WIDTH = 360
GAP = 24
LABEL_HEIGHT = 34
COLS = 3
ROWS = 3


def page_number(path: Path) -> int:
    return int(path.stem.split("-")[-1])


pages = sorted(SOURCE.glob("page-*.png"), key=page_number)
OUTPUT.mkdir(parents=True, exist_ok=True)

with Image.open(pages[0]) as sample:
    thumb_height = round(sample.height * THUMB_WIDTH / sample.width)

sheet_width = GAP + COLS * (THUMB_WIDTH + GAP)
sheet_height = GAP + ROWS * (LABEL_HEIGHT + thumb_height + GAP)

for sheet_index in range(0, len(pages), COLS * ROWS):
    batch = pages[sheet_index : sheet_index + COLS * ROWS]
    sheet = Image.new("RGB", (sheet_width, sheet_height), "#d6d6d6")
    draw = ImageDraw.Draw(sheet)
    for index, path in enumerate(batch):
        row, col = divmod(index, COLS)
        x = GAP + col * (THUMB_WIDTH + GAP)
        y = GAP + row * (LABEL_HEIGHT + thumb_height + GAP)
        draw.text((x, y + 4), f"Page {page_number(path)}", fill="black")
        with Image.open(path) as page:
            page = page.convert("RGB")
            page.thumbnail((THUMB_WIDTH, thumb_height), Image.Resampling.LANCZOS)
            sheet.paste(page, (x, y + LABEL_HEIGHT))
    first = page_number(batch[0])
    last = page_number(batch[-1])
    sheet.save(OUTPUT / f"pages-{first:02d}-{last:02d}.png", optimize=True)

print(f"Created {(len(pages) + 8) // 9} contact sheets from {len(pages)} pages.")

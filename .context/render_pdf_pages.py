from __future__ import annotations

import sys
from pathlib import Path

import pypdfium2 as pdfium


def main() -> None:
    if len(sys.argv) != 3:
        raise SystemExit("Usage: render_pdf_pages.py <input.pdf> <output-dir>")

    source = Path(sys.argv[1])
    output_dir = Path(sys.argv[2])
    output_dir.mkdir(parents=True, exist_ok=True)

    document = pdfium.PdfDocument(source)
    for index in range(len(document)):
        page = document[index]
        bitmap = page.render(scale=2)
        bitmap.to_pil().convert("RGB").save(output_dir / f"page-{index + 1}.png")
    print(f"Rendered {len(document)} pages.")


if __name__ == "__main__":
    main()

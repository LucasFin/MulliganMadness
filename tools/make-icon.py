"""
Thunderstore icon for MulliganMadness.

Keeps the original TAKE ALL / card-fan art and stamps the family ROUNDS blob
in the bottom-left corner (same sticker treatment as LeanAndMeanCards).

The unpainted original lives at tools/icon-base.png. This script writes
icon-preview.png only — copy that to icon.png when shipping.
"""
import os
from PIL import Image, ImageDraw, ImageFilter

S = 256
SS = 4
W = S * SS

CREAM = (245, 239, 225, 255)
INK = (13, 15, 20, 255)
ACCENT = (110, 214, 72, 255)


def layer():
    return Image.new("RGBA", (W, W), (0, 0, 0, 0))


def grow(src, radius, colour):
    a = src.split()[3].point(lambda v: 255 if v > 110 else 0)
    step = 9
    for _ in range(max(1, round(radius / 4))):
        a = a.filter(ImageFilter.MaxFilter(step))
    a = a.filter(ImageFilter.GaussianBlur(1.2))
    out = Image.new("RGBA", src.size, colour)
    out.putalpha(a)
    return out


def sticker(base, art, cream_px=26, ink_px=11):
    base.alpha_composite(grow(art, cream_px + ink_px, CREAM))
    base.alpha_composite(grow(art, ink_px, INK))
    base.alpha_composite(art)


def blob(size, body):
    """The ROUNDS blob: round body, stubby feet, angular scowl."""
    lay = layer()
    d = ImageDraw.Draw(lay)
    cx = cy = size // 2
    rr = int(size * 0.40)

    fw, fh = int(size * 0.19), int(size * 0.12)
    for fx in (cx - int(size * 0.21), cx + int(size * 0.21) - fw):
        d.rounded_rectangle(
            [fx, cy + rr - int(size * 0.05), fx + fw, cy + rr + fh],
            radius=fh // 2, fill=body)
    d.ellipse([cx - rr, cy - rr, cx + rr, cy + rr], fill=body)

    ey = cy - int(size * 0.06)
    ew, eh = int(size * 0.20), int(size * 0.20)
    for sx in (-1, 1):
        ex = cx + sx * int(size * 0.16)
        outer_top = (ex - sx * ew * 0.5, ey - eh * 0.30)
        inner_top = (ex + sx * ew * 0.5, ey - eh * 0.72)
        inner_bot = (ex + sx * ew * 0.42, ey + eh * 0.20)
        outer_bot = (ex - sx * ew * 0.42, ey + eh * 0.48)
        d.polygon([outer_top, inner_top, inner_bot, outer_bot], fill=INK)

    mw = int(size * 0.26)
    my = cy + int(size * 0.20)
    zig = []
    for i in range(5):
        zig.append((cx - mw // 2 + mw * i / 4, my + (0 if i % 2 == 0 else size * 0.055)))
    for i in range(4, -1, -1):
        zig.append((cx - mw // 2 + mw * i / 4,
                    my + size * 0.035 + (0 if i % 2 == 0 else size * 0.055)))
    d.polygon(zig, fill=INK)
    return lay


def _place(small, x, y):
    lay = layer()
    lay.alpha_composite(small, (x, y))
    return lay


def mulligan_madness(base_path):
    base = Image.open(base_path).convert("RGBA").resize((W, W), Image.LANCZOS)
    img = Image.new("RGBA", (W, W), (0, 0, 0, 0))
    img.alpha_composite(base)

    # Cover the original corner skull (and its sparks) so the blob sits clean.
    cover = layer()
    ImageDraw.Draw(cover).ellipse(
        [int(-W * 0.08), int(W * 0.62), int(W * 0.38), int(W * 1.12)],
        fill=(1, 14, 24, 255))
    img.alpha_composite(cover)

    b = blob(int(W * 0.30), ACCENT)
    sticker(img, _place(b, int(W * 0.02), int(W * 0.66)), cream_px=17, ink_px=8)
    return img


def save(img, path):
    out = img.resize((S, S), Image.LANCZOS)
    flat = Image.new("RGB", (S, S), (1, 14, 24))
    flat.paste(out, mask=out.split()[3])
    flat.save(path, "PNG")
    print(f"wrote {path}  {S}x{S} RGB")


if __name__ == "__main__":
    root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    save(
        mulligan_madness(os.path.join(root, "tools", "icon-base.png")),
        os.path.join(root, "icon-preview.png"),
    )

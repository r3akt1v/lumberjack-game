#!/usr/bin/env python3
"""Generate seamless-ish textures for LumberjackGame into Assets/Resources/Textures/."""
import os, random, math
from PIL import Image, ImageDraw, ImageFilter

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "Resources", "Textures")
os.makedirs(OUT, exist_ok=True)
random.seed(7)
S = 256

def noise_field(cells, seed=0):
    """Low-res random field upscaled = value noise."""
    r = random.Random(seed)
    g = Image.new("L", (cells, cells))
    px = g.load()
    for y in range(cells):
        for x in range(cells):
            px[x, y] = r.randrange(256)
    return g.resize((S, S), Image.BILINEAR)

def blend(base, layers):
    """base color tuple + list of (noise_img, amount, tint) -> Image RGB."""
    img = Image.new("RGB", (S, S), base)
    px = img.load()
    for nimg, amt, tint in layers:
        np_ = nimg.load()
        for y in range(0, S, 1):
            for x in range(0, S, 1):
                v = np_[x, y] / 255.0
                f = v * amt
                r, g, b = px[x, y]
                px[x, y] = (
                    max(0, min(255, int(r + (tint[0] - r) * f))),
                    max(0, min(255, int(g + (tint[1] - g) * f))),
                    max(0, min(255, int(b + (tint[2] - b) * f))),
                )
    return img

def save(img, name):
    img.save(os.path.join(OUT, name + ".png"))
    print("OK", name)

def streaks_vertical(img, n=40, dark=(30, 18, 8), light=(90, 60, 32)):
    """Vertical bark streaks."""
    d = ImageDraw.Draw(img)
    for i in range(n):
        x = random.randrange(S)
        w = random.choice([1, 1, 2, 3])
        col = dark if random.random() < 0.6 else light
        alpha = random.randint(60, 140)
        c = tuple(min(255, int(v * alpha / 255 + v * (1 - alpha / 255))) for v in col)
        d.line([(x, 0), (x + random.randint(-3, 3), S)], fill=c, width=w)
    return img.filter(ImageFilter.GaussianBlur(0.6))

def horizontal_grain(img, rows=14, col_dark=(60, 38, 20), col_light=(120, 85, 50)):
    d = ImageDraw.Draw(img)
    for i in range(rows):
        y = int(i * S / rows + random.randint(-4, 4))
        for x in range(0, S, 8):
            col = col_dark if random.random() < 0.5 else col_light
            d.line([(x, y + random.randint(-2, 2)), (min(S, x + 8), y + random.randint(-2, 2))], fill=col, width=1)
    return img.filter(ImageFilter.GaussianBlur(0.5))

def rings(img, cx=None, cy=None):
    """Growth rings for cut wood."""
    d = ImageDraw.Draw(img)
    cx = cx or S // 2; cy = cy or S // 2
    for r in range(4, S, random.choice([5, 7, 9])):
        col = (150 + random.randint(-25, 25), 110 + random.randint(-20, 20), 60 + random.randint(-15, 15))
        d.ellipse([cx - r, cy - r * 0.7, cx + r, cy + r * 0.7], outline=col, width=2)
    return img.filter(ImageFilter.GaussianBlur(0.4))

def shingles(img, rows_n=8, base=(110, 45, 30), dark=(70, 28, 18), light=(140, 60, 40)):
    d = ImageDraw.Draw(img)
    rh = S // rows_n
    for row in range(rows_n):
        y = row * rh
        off = (row % 2) * rh
        for x in range(-rh, S + rh, rh):
            col = base if random.random() < 0.5 else (dark if random.random() < 0.5 else light)
            d.rectangle([x + off, y, x + off + rh - 3, y + rh - 2], fill=col)
            d.line([(x + off, y), (x + off, y + rh)], fill=dark, width=1)
    return img.filter(ImageFilter.GaussianBlur(0.4))

def speckle(img, n=900, dark=(0, 0, 0), light=(255, 255, 255), size=1):
    d = ImageDraw.Draw(img)
    for _ in range(n):
        x, y = random.randrange(S), random.randrange(S)
        col = dark if random.random() < 0.5 else light
        a = random.randint(15, 60)
        c = tuple(int(v * a / 255 + v * (1 - a / 255)) for v in col)
        d.point((x, y), fill=c)
    return img

def planks(img, n=6, base=(150, 105, 60), dark=(95, 62, 32), light=(185, 135, 85)):
    d = ImageDraw.Draw(img)
    pw = S // n
    for i in range(n):
        x = i * pw
        col = base if i % 2 == 0 else tuple(int(v * 0.9) for v in base)
        d.rectangle([x, 0, x + pw - 2, S], fill=col)
        d.line([(x, 0), (x, S)], fill=dark, width=2)
        # grain
        for _ in range(6):
            yy = random.randrange(S)
            d.line([(x + 3, yy), (x + pw - 4, yy + random.randint(-6, 6))], fill=dark if random.random() < 0.5 else light, width=1)
    return img.filter(ImageFilter.GaussianBlur(0.3))

def gradient_v(img, top, bottom):
    px = img.load()
    for y in range(S):
        t = y / S
        c = tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3))
        for x in range(S):
            px[x, y] = c
    return img

# ---------- textures ----------
# bark: brown base + vertical streaks + noise
img = blend((62, 40, 22), [(noise_field(24, 1), 0.5, (40, 26, 14)), (noise_field(64, 2), 0.35, (85, 55, 30))])
save(streaks_vertical(img), "bark")

# cutwood: light wood + rings
img = blend((168, 128, 82), [(noise_field(32, 3), 0.3, (140, 100, 60)), (noise_field(80, 4), 0.2, (200, 165, 115))])
save(rings(img), "cutwood")

# wood: warm planks grain
img = blend((128, 84, 46), [(noise_field(48, 5), 0.4, (95, 60, 30)), (noise_field(90, 6), 0.25, (160, 115, 70))])
save(horizontal_grain(img), "wood")

# wooddark
img = blend((84, 52, 28), [(noise_field(48, 7), 0.4, (55, 32, 16)), (noise_field(90, 8), 0.25, (115, 75, 42))])
save(horizontal_grain(img, col_dark=(35, 20, 10), col_light=(110, 72, 40)), "wooddark")

# needles: dark green speckle
img = blend((26, 62, 24), [(noise_field(48, 9), 0.55, (14, 40, 14)), (noise_field(96, 10), 0.4, (50, 95, 35))])
save(speckle(img, 1400, (10, 30, 10), (80, 130, 60), 1), "needles")

# bushleaf: mid green blotches
img = blend((46, 96, 34), [(noise_field(40, 11), 0.5, (28, 70, 22)), (noise_field(90, 12), 0.4, (80, 140, 55))])
save(speckle(img, 900, (20, 50, 15), (120, 170, 80), 2), "bushleaf")

# grassblade: vertical gradient + streaks
img = gradient_v(Image.new("RGB", (S, S)), (70, 130, 45), (120, 175, 60))
img = blend_img = img
px = img.load()
for x in range(S):
    for y in range(S):
        if random.random() < 0.12:
            r, g, b = px[x, y]
            px[x, y] = (max(0, r - 20), min(255, g + 15), max(0, b - 10))
save(img, "grassblade")

# rock: gray noise + cracks
img = blend((105, 104, 108), [(noise_field(40, 13), 0.5, (70, 70, 74)), (noise_field(90, 14), 0.4, (145, 144, 148))])
img = speckle(img, 700, (40, 40, 44), (170, 170, 175), 1)
d = ImageDraw.Draw(img)
for _ in range(12):
    x, y = random.randrange(S), random.randrange(S)
    pts = [(x, y)]
    for _ in range(4):
        pts.append((pts[-1][0] + random.randint(-18, 18), pts[-1][1] + random.randint(-18, 18)))
    d.line(pts, fill=(60, 60, 64), width=1)
save(img.filter(ImageFilter.GaussianBlur(0.5)), "rock")

# roof shingles
img = blend((115, 48, 32), [(noise_field(60, 15), 0.25, (90, 36, 24)), (noise_field(120, 16), 0.15, (150, 70, 48))])
save(shingles(img), "roof")

# plaster: subtle off-white
img = blend((205, 195, 178), [(noise_field(50, 17), 0.18, (180, 168, 150)), (noise_field(110, 18), 0.12, (225, 218, 200))])
save(speckle(img, 500, (160, 150, 135), (235, 230, 215), 1), "plaster")

# floorwood: plank strips
img = blend((140, 98, 58), [(noise_field(50, 19), 0.3, (105, 70, 40)), (noise_field(100, 20), 0.2, (175, 130, 85))])
save(planks(img, 6), "floorwood")

# metal: light gray brushed
img = blend((165, 168, 172), [(noise_field(80, 21), 0.3, (130, 133, 138)), (noise_field(160, 22), 0.2, (200, 202, 205))])
d = ImageDraw.Draw(img)
for y in range(0, S, 3):
    d.line([(0, y), (S, y)], fill=(150, 152, 156), width=1)
save(img.filter(ImageFilter.GaussianBlur(0.4)), "metal")

print("ALL DONE")

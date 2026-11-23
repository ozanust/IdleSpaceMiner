"""Store art for Idle Space Miner: framed screenshots with captions, feature graphic and icons."""
import os, math, random
from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageEnhance, ImageChops

REPO = r"H:/Repos/IdleSpaceMiner"
RAW = os.path.join(os.path.dirname(os.path.abspath(__file__)), "raw")
OUT = os.path.join(REPO, "StoreAssets")
BOLD = os.path.join(REPO, "Assets/Fonts/Rajdhani/Rajdhani-Bold.ttf")
SEMI = os.path.join(REPO, "Assets/Fonts/Rajdhani/Rajdhani-SemiBold.ttf")
BG = {k: os.path.join(REPO, f"Assets/OrbitalMobile/Backgrounds/BG_{k}.jpg") for k in ("TidalVeil", "EmberRift", "CinderBelt")}
PLANETS = os.path.join(REPO, "Assets/GFX/Sprites/Planets")
SHIP = os.path.join(REPO, "Assets/GFX/Sprites/ship.png")
CARGO = os.path.join(REPO, "Assets/GFX/Sprites/cargoship.png")
ICON = os.path.join(REPO, "Assets/GFX/Icons/gameicon.png")

GOLD_TOP, GOLD_BOTTOM = (255, 236, 160), (255, 168, 40)
CYAN = (110, 230, 255)

SHOTS = [  # raw render, headline, subline, background, decor planets, callout (crop box in raw px, position)
    ("world_close", "MINE THE GALAXY", "Build your idle space mining empire", "TidalVeil", ["planet_lava", "planet_pearl"], None),
    ("planet", "UPGRADE PLANETS", "Boost mining, ship speed and cargo", "EmberRift", ["planet_pearl", "planet_ocean"], (18, 600, 1272, 2130)),
    ("world", "DISCOVER 16 PLANETS", "Telescopes reveal new worlds and rare ores", "TidalVeil", ["planet_nebula", "planet_ice"], None),
    ("production", "SMELT & CRAFT", "Turn ore into bars and high-value items", "CinderBelt", ["planet_lava", "planet_desert"], (18, 905, 1272, 2130)),
    ("research", "RESEARCH NEW TECH", "Unlock smelters, crafters and telescopes", "TidalVeil", ["planet_toxic", "planet_abyss"], (18, 905, 1272, 2130)),
    ("managers", "HIRE MANAGERS", "Star managers multiply your output", "EmberRift", ["planet_mech", "planet_ocean"], (18, 905, 1272, 2130)),
    ("mothership", "SELL THE GALAXY", "Earn Dark Matter and come back stronger", "CinderBelt", ["planet_abyss", "planet_pearl"], (18, 905, 1272, 2130)),
    ("resources", "MINE RARE ORES", "From copper to gold, sell at the right time", "TidalVeil", ["planet_desert", "planet_ice"], (18, 905, 1272, 2130)),
]


def font(path, size):
    return ImageFont.truetype(path, size)


def cover(img, w, h):
    """Scale and crop to fill w x h."""
    s = max(w / img.width, h / img.height)
    img = img.resize((math.ceil(img.width * s), math.ceil(img.height * s)), Image.LANCZOS)
    x, y = (img.width - w) // 2, (img.height - h) // 2
    return img.crop((x, y, x + w, y + h))


def background(name, w, h):
    bg = cover(Image.open(BG[name]).convert("RGB"), w, h)
    bg = ImageEnhance.Brightness(bg).enhance(0.78)
    # darker top for the caption, soft vignette
    shade = Image.new("L", (w, h), 0)
    d = ImageDraw.Draw(shade)
    for y in range(h):
        t = y / h
        d.line([(0, y), (w, y)], fill=int(150 * max(0, 1 - t / 0.35)))
    bg = Image.composite(Image.new("RGB", (w, h), (3, 8, 20)), bg, shade)
    vignette = Image.new("L", (w, h), 0)
    ImageDraw.Draw(vignette).ellipse((-w * 0.35, -h * 0.2, w * 1.35, h * 1.2), fill=255)
    vignette = vignette.filter(ImageFilter.GaussianBlur(w * 0.12))
    return Image.composite(bg, Image.new("RGB", (w, h), (2, 5, 14)), vignette)


def stars(img, count, seed):
    rnd = random.Random(seed)
    d = ImageDraw.Draw(img)
    for _ in range(count):
        x, y = rnd.randrange(img.width), rnd.randrange(img.height)
        r = rnd.choice([1, 1, 1, 2, 2, 3])
        a = rnd.randint(120, 255)
        d.ellipse((x - r, y - r, x + r, y + r), fill=(200, 235, 255, a))


def paste_glow(base, sprite, xy, glow_color=(90, 200, 255), glow=0.6, blur=30):
    """Paste an RGBA sprite with a soft coloured glow behind it."""
    a = sprite.split()[-1]
    halo = Image.new("RGBA", sprite.size, glow_color + (0,))
    halo.putalpha(a.point(lambda v: int(v * glow)))
    pad = blur * 3
    layer = Image.new("RGBA", (sprite.width + pad * 2, sprite.height + pad * 2), (0, 0, 0, 0))
    layer.paste(halo, (pad, pad), halo)
    layer = layer.filter(ImageFilter.GaussianBlur(blur))
    base.alpha_composite(layer, (xy[0] - pad, xy[1] - pad))
    base.alpha_composite(sprite, xy)


def planet(name, size):
    p = Image.open(os.path.join(PLANETS, name + ".png")).convert("RGBA")
    return p.resize((size, size), Image.LANCZOS)


def gradient_text(base, text, fnt, center_x, top, top_color, bottom_color, stroke=6, shadow=True):
    """Bold headline with a vertical colour gradient, dark outline and drop shadow."""
    d = ImageDraw.Draw(base)
    bbox = d.textbbox((0, 0), text, font=fnt, stroke_width=stroke)
    w, h = bbox[2] - bbox[0], bbox[3] - bbox[1]
    x = int(center_x - w / 2 - bbox[0])
    y = int(top - bbox[1])
    if shadow:
        sh = Image.new("RGBA", base.size, (0, 0, 0, 0))
        ImageDraw.Draw(sh).text((x, y + stroke * 1.5), text, font=fnt, fill=(0, 0, 0, 200), stroke_width=stroke, stroke_fill=(0, 0, 0, 200))
        base.alpha_composite(sh.filter(ImageFilter.GaussianBlur(stroke * 1.2)))
    outline = Image.new("RGBA", base.size, (0, 0, 0, 0))
    ImageDraw.Draw(outline).text((x, y), text, font=fnt, fill=(20, 12, 4, 255), stroke_width=stroke, stroke_fill=(20, 12, 4, 255))
    base.alpha_composite(outline)
    mask = Image.new("L", base.size, 0)
    ImageDraw.Draw(mask).text((x, y), text, font=fnt, fill=255)
    grad = Image.new("RGBA", base.size)
    gd = ImageDraw.Draw(grad)
    y0, y1 = top, top + h
    for yy in range(max(0, y0 - 5), min(base.height, y1 + 5)):
        t = min(1, max(0, (yy - y0) / max(1, h)))
        c = tuple(int(top_color[i] + (bottom_color[i] - top_color[i]) * t) for i in range(3))
        gd.line([(0, yy), (base.width, yy)], fill=c + (255,))
    base.paste(grad, (0, 0), mask)
    return h


def fit_font(path, text, max_width, start):
    size = start
    while size > 20:
        f = font(path, size)
        b = ImageDraw.Draw(Image.new("L", (1, 1))).textbbox((0, 0), text, font=f, stroke_width=6)
        if b[2] - b[0] <= max_width:
            return f
        size -= 4
    return font(path, size)


def rounded_mask(w, h, r):
    m = Image.new("L", (w, h), 0)
    ImageDraw.Draw(m).rounded_rectangle((0, 0, w - 1, h - 1), r, fill=255)
    return m


def device(shot, w):
    """The game screen in a slim metallic frame with a cyan rim light."""
    h = int(shot.height * w / shot.width)
    screen = shot.resize((w, h), Image.LANCZOS).convert("RGBA")
    r = int(w * 0.07)
    bezel = int(w * 0.022)
    fw, fh = w + bezel * 2, h + bezel * 2
    frame = Image.new("RGBA", (fw, fh), (0, 0, 0, 0))
    body = Image.new("RGBA", (fw, fh))
    bd = ImageDraw.Draw(body)
    for y in range(fh):
        t = y / fh
        c = int(46 + 30 * math.sin(t * math.pi))
        bd.line([(0, y), (fw, y)], fill=(c, c + 6, c + 16, 255))
    frame.paste(body, (0, 0), rounded_mask(fw, fh, r + bezel))
    ImageDraw.Draw(frame).rounded_rectangle((1, 1, fw - 2, fh - 2), r + bezel, outline=(140, 220, 255, 220), width=max(2, bezel // 6))
    screen.putalpha(rounded_mask(w, h, r))
    frame.alpha_composite(screen, (bezel, bezel))
    return frame


def screenshot(raw, headline, sub, bg_name, decor, w, h, seed, out_path, tablet=False, callout=None):
    canvas = background(bg_name, w, h).convert("RGBA")
    stars(canvas, int(w * h / 9000), seed)
    unit = w / 1080
    # decor planets peeking in from the sides behind the device
    p1 = planet(decor[0], int(330 * unit))
    paste_glow(canvas, p1, (int(-110 * unit), int(h * (0.30 if not tablet else 0.25))), (255, 150, 80), 0.5, int(40 * unit))
    p2 = planet(decor[1], int(250 * unit))
    paste_glow(canvas, p2, (int(w - 150 * unit), int(h * 0.52)), (120, 200, 255), 0.5, int(40 * unit))

    caption_top = int(h * (0.045 if not tablet else 0.04))
    hf = fit_font(BOLD, headline, int(w * 0.9), int((150 if not tablet else 118) * unit))
    hh = gradient_text(canvas, headline, hf, w / 2, caption_top, GOLD_TOP, GOLD_BOTTOM, stroke=max(4, int(7 * unit)))
    sf = fit_font(SEMI, sub, int(w * 0.86), int((64 if not tablet else 50) * unit))
    d = ImageDraw.Draw(canvas)
    sy = caption_top + hh + int(28 * unit)
    sb = d.textbbox((0, 0), sub, font=sf)
    d.text((w / 2 - (sb[2] - sb[0]) / 2 - sb[0], sy), sub, font=sf, fill=CYAN + (255,), stroke_width=max(2, int(3 * unit)), stroke_fill=(2, 10, 25, 255))
    caption_bottom = sy + (sb[3] - sb[1]) + int(40 * unit)

    shot = Image.open(os.path.join(RAW, raw + ".png")).convert("RGB")
    # The phone fills the space under the caption on every canvas size
    space = h - caption_bottom - int(36 * unit)
    dev_w = int(min(w * 0.88, space / (shot.height / shot.width) / 1.05))
    dev = device(shot, dev_w)
    x = (w - dev.width) // 2
    y = caption_bottom + max(0, (space - dev.height) // 2)
    shadow = Image.new("RGBA", (dev.width + 200, dev.height + 200), (0, 0, 0, 0))
    ImageDraw.Draw(shadow).rounded_rectangle((100, 120, 100 + dev.width, 120 + dev.height), int(dev.width * 0.08), fill=(0, 0, 0, 170))
    canvas.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(40)), (x - 100, y - 100))
    glow = Image.new("RGBA", (dev.width + 240, dev.height + 240), (0, 0, 0, 0))
    ImageDraw.Draw(glow).rounded_rectangle((120, 120, 120 + dev.width, 120 + dev.height), int(dev.width * 0.08), outline=(80, 200, 255, 140), width=int(10 * unit))
    canvas.alpha_composite(glow.filter(ImageFilter.GaussianBlur(28 * unit)), (x - 120, y - 120))
    canvas.alpha_composite(dev, (x, y))
    if callout:
        pop_out(canvas, shot, callout, dev, x, y, unit)
    canvas.convert("RGB").save(out_path, quality=95)


def pop_out(canvas, shot, box, dev, dev_x, dev_y, unit):
    """The open window lifted off the phone, a little bigger, so it reads in store thumbnails."""
    bezel = (dev.width - dev.width / 1.044) / 2
    screen_scale = (dev.width - 2 * bezel) / shot.width
    crop = shot.crop(box).convert("RGBA")
    target_w = int(min(canvas.width * 0.94, dev.width * 1.1))
    s = target_w / crop.width
    crop = crop.resize((target_w, int(crop.height * s)), Image.LANCZOS)
    crop.putalpha(rounded_mask(crop.width, crop.height, int(24 * unit)))
    cx = dev_x + bezel + (box[0] + box[2]) / 2 * screen_scale
    cy = dev_y + bezel + (box[1] + box[3]) / 2 * screen_scale
    px = int(cx - crop.width / 2)
    py = int(min(cy - crop.height / 2, canvas.height - crop.height - 30 * unit))
    pad = int(90 * unit)
    sh = Image.new("RGBA", (crop.width + pad * 2, crop.height + pad * 2), (0, 0, 0, 0))
    ImageDraw.Draw(sh).rounded_rectangle((pad, pad + 25 * unit, pad + crop.width, pad + crop.height + 25 * unit), int(30 * unit), fill=(0, 0, 0, 210))
    canvas.alpha_composite(sh.filter(ImageFilter.GaussianBlur(35 * unit)), (px - pad, py - pad))
    glow = Image.new("RGBA", (crop.width + pad * 2, crop.height + pad * 2), (0, 0, 0, 0))
    ImageDraw.Draw(glow).rounded_rectangle((pad, pad, pad + crop.width, pad + crop.height), int(30 * unit), outline=(90, 215, 255, 170), width=int(8 * unit))
    canvas.alpha_composite(glow.filter(ImageFilter.GaussianBlur(18 * unit)), (px - pad, py - pad))
    canvas.alpha_composite(crop, (px, py))


def feature_graphic(path):
    w, h = 1024, 500
    canvas = cover(Image.open(BG["TidalVeil"]).convert("RGB").rotate(90, expand=True), w, h)
    canvas = ImageEnhance.Brightness(canvas).enhance(0.75).convert("RGBA")
    shade = Image.new("L", (w, h), 0)
    sd = ImageDraw.Draw(shade)
    for x in range(w):
        sd.line([(x, 0), (x, h)], fill=int(190 * max(0, 1 - x / (w * 0.62))))
    canvas = Image.composite(Image.new("RGBA", (w, h), (3, 8, 22, 255)), canvas, shade)
    stars(canvas, 260, 7)
    paste_glow(canvas, planet("planet_lava", 300), (700, -70), (255, 120, 60), 0.55, 40)
    paste_glow(canvas, planet("planet_pearl", 190), (880, 300), (200, 170, 255), 0.5, 35)
    paste_glow(canvas, planet("planet_ocean", 120), (560, 360), (90, 200, 255), 0.5, 25)
    # ore stream: cargo ships flying from the planet to the mothership
    cargo = Image.open(CARGO).convert("RGBA")
    for i, (cx, cy, s, rot) in enumerate([(780, 250, 70, 125), (705, 300, 58, 125), (640, 340, 46, 125)]):
        c = cargo.resize((s, s), Image.LANCZOS).rotate(rot, expand=True, resample=Image.BICUBIC)
        paste_glow(canvas, c, (cx - c.width // 2, cy - c.height // 2), (255, 200, 120), 0.6, 10)
    ship = Image.open(SHIP).convert("RGBA").resize((290, 290), Image.LANCZOS).rotate(-35, expand=True, resample=Image.BICUBIC)
    paste_glow(canvas, ship, (520, 120), (90, 210, 255), 0.65, 30)
    gradient_text(canvas, "IDLE SPACE", font(BOLD, 96), 270, 120, GOLD_TOP, GOLD_BOTTOM, stroke=6)
    gradient_text(canvas, "MINER", font(BOLD, 150), 270, 210, GOLD_TOP, GOLD_BOTTOM, stroke=7)
    d = ImageDraw.Draw(canvas)
    tag = "Mine. Smelt. Conquer the galaxy."
    f = font(SEMI, 34)
    b = d.textbbox((0, 0), tag, font=f)
    d.text((270 - (b[2] - b[0]) / 2, 385), tag, font=f, fill=CYAN + (255,), stroke_width=2, stroke_fill=(2, 10, 25, 255))
    canvas.convert("RGB").save(path, quality=95)


def icons():
    icon = Image.open(ICON).convert("RGBA")
    # The source has rounded transparent corners; stores round the icon themselves, so fill to the edges
    zoom = 1.13
    big = icon.resize((int(icon.width * zoom), int(icon.height * zoom)), Image.LANCZOS)
    off = (big.width - icon.width) // 2
    full = big.crop((off, off, off + icon.width, off + icon.height))
    flat = Image.new("RGB", full.size, (20, 16, 40))
    flat.paste(full, (0, 0), full)
    flat.resize((1024, 1024), Image.LANCZOS).save(os.path.join(OUT, "appstore", "icon_1024.png"))
    flat.resize((512, 512), Image.LANCZOS).save(os.path.join(OUT, "googleplay", "icon_512.png"))


def main():
    for sub in ("googleplay/phone", "appstore/iphone_6.9", "appstore/ipad_13"):
        os.makedirs(os.path.join(OUT, sub), exist_ok=True)
    for i, (raw, head, sub, bg, decor, box) in enumerate(SHOTS, 1):
        name = f"{i:02d}_{raw}"
        screenshot(raw, head, sub, bg, decor, 1080, 1920, i, os.path.join(OUT, "googleplay/phone", name + ".jpg"), callout=box)
        screenshot(raw, head, sub, bg, decor, 1320, 2868, i, os.path.join(OUT, "appstore/iphone_6.9", name + ".jpg"), callout=box)
        screenshot(raw, head, sub, bg, decor, 2064, 2752, i, os.path.join(OUT, "appstore/ipad_13", name + ".jpg"), tablet=True, callout=box)
    feature_graphic(os.path.join(OUT, "googleplay", "feature_graphic_1024x500.jpg"))
    icons()


if __name__ == "__main__":
    main()

"""Shared helpers for SmartData "diagonal" game icons.

Composition (same for every game): 1024x1024, white top-left triangle with the SmartData mark,
game motif in the bottom-right triangle, magenta seam on the diagonal. Drawn at 4x, downscaled with LANCZOS.
"""
from PIL import Image, ImageDraw, ImageFilter, ImageFont
import math, os, sys

S = 1024; K = 4; N = S * K
MAG = (173, 41, 82)


def P(v):
    """fraction of the icon -> canvas pixels"""
    return int(round(v * N))


def vgrad(top, bot, w=N, h=N):
    g = Image.new('RGB', (1, h))
    for y in range(h):
        t = y / (h - 1)
        g.putpixel((0, y), tuple(int(top[i] + (bot[i] - top[i]) * t) for i in range(3)))
    return g.resize((w, h))


def radial(inner, outer, cx=0.7, cy=0.7, rad=0.75):
    """radial gradient background, centre in icon fractions"""
    small = 256
    g = Image.new('RGB', (small, small))
    px = g.load()
    for y in range(small):
        for x in range(small):
            d = math.hypot(x / small - cx, y / small - cy) / rad
            t = min(1.0, d)
            t = t * t * (3 - 2 * t)
            px[x, y] = tuple(int(inner[i] + (outer[i] - inner[i]) * t) for i in range(3))
    return g.resize((N, N), Image.BICUBIC)


def layer():
    return Image.new('RGBA', (N, N), (0, 0, 0, 0))


class Icon:
    def __init__(self, bg):
        self.img = bg.convert('RGB')
        self.mask = Image.new('L', (N, N), 0)   # union of motif elements, for the diagonal clearance check

    def add(self, lay, shadow=True, soff=(0.018, 0.026), sblur=0.018, salpha=110, scol=(0, 0, 0), track=True):
        a = lay.split()[3]
        if shadow:
            sh = Image.new('L', (N, N), 0)
            sh.paste(a, (P(soff[0]), P(soff[1])))
            sh = sh.filter(ImageFilter.GaussianBlur(P(sblur)))
            sh = sh.point(lambda v: v * salpha // 255)
            self.img = Image.composite(Image.new('RGB', (N, N), scol), self.img, sh)
        base = self.img.convert('RGBA')
        base.alpha_composite(lay)
        self.img = base.convert('RGB')
        if track:
            self.mask = _lighter(self.mask, a)

    def glow(self, lay, blur=0.03, strength=1.0):
        a = lay.split()[3].filter(ImageFilter.GaussianBlur(P(blur)))
        if strength != 1.0:
            a = a.point(lambda v: min(255, int(v * strength)))
        col = lay.copy(); col.putalpha(a)
        base = self.img.convert('RGBA'); base.alpha_composite(col); self.img = base.convert('RGB')

    def clearance(self):
        """min distance (icon fraction) from any motif pixel to the diagonal x + y = 1"""
        small = self.mask.resize((512, 512), Image.BILINEAR)
        px = small.load(); best = 9.0
        for y in range(512):
            for x in range(512):
                if px[x, y] > 40:
                    d = ((x + 0.5) / 512 + (y + 0.5) / 512 - 1) / math.sqrt(2)
                    if d < best: best = d
        return best


def _lighter(a, b):
    from PIL import ImageChops
    return ImageChops.lighter(a, b)


def sphere(base, r, light=(-0.38, -0.42), amb=0.42, spec=0.55, rim=None):
    rr = max(8, r // 2)
    b = Image.new('RGBA', (2 * rr, 2 * rr), (0, 0, 0, 0)); px = b.load(); lx, ly = light
    for y in range(2 * rr):
        for x in range(2 * rr):
            dx = (x - rr + 0.5) / rr; dy = (y - rr + 0.5) / rr; dd = dx * dx + dy * dy
            if dd > 1: continue
            dz = math.sqrt(1 - dd); lam = max(0, -(dx * lx + dy * ly) + dz * 0.82); k = amb + 0.68 * lam
            s = max(0, 1 - ((dx - lx * 0.6) ** 2 + (dy - ly * 0.6) ** 2) / 0.09) ** 2 * spec
            col = [min(255, int(base[i] * k + 255 * s)) for i in range(3)]
            if rim and dd > 0.82:
                t = (dd - 0.82) / 0.18; col = [int(col[i] * (1 - t * 0.5) + rim[i] * t * 0.5) for i in range(3)]
            px[x, y] = (*col, 255)
    return b.resize((2 * r, 2 * r), Image.LANCZOS)


def put_sphere(lay, base, cx, cy, r, **kw):
    b = sphere(base, P(r), **kw); lay.alpha_composite(b, (P(cx) - P(r), P(cy) - P(r)))


def shade_rrect(box, radius, top, bot, outline=None, ow=0.0, gloss=0.0):
    """rounded rect layer with vertical gradient (+ optional gloss band and outline)"""
    x0, y0, x1, y1 = [P(v) for v in box]
    w, h = x1 - x0, y1 - y0
    g = vgrad(top, bot, w, h).convert('RGBA')
    m = Image.new('L', (w, h), 0); ImageDraw.Draw(m).rounded_rectangle([0, 0, w - 1, h - 1], radius=P(radius), fill=255)
    if gloss:
        gl = Image.new('L', (w, h), 0); ImageDraw.Draw(gl).rounded_rectangle([int(w * 0.08), int(h * 0.06), int(w * 0.92), int(h * 0.42)], radius=P(radius * 0.7), fill=int(255 * gloss))
        gl = gl.filter(ImageFilter.GaussianBlur(max(1, int(h * 0.04))))
        white = Image.new('RGBA', (w, h), (255, 255, 255, 0)); white.putalpha(gl)
        g.alpha_composite(white)
    if outline:
        d = ImageDraw.Draw(g); d.rounded_rectangle([0, 0, w - 1, h - 1], radius=P(radius), outline=outline, width=max(1, P(ow)))
    g.putalpha(m)
    lay = layer(); lay.alpha_composite(g, (x0, y0)); return lay


def rotated(lay, angle, cx, cy):
    return lay.rotate(angle, resample=Image.BICUBIC, center=(P(cx), P(cy)))


def font(size_frac, bold=True):
    for f in ['/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf', '/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf']:
        if os.path.exists(f):
            return ImageFont.truetype(f, P(size_frac))
    return ImageFont.load_default()


def finish(icon, logo_path, out_dir='.', min_clear=0.03):
    img = Image.new('RGB', (N, N), (255, 255, 255))
    tri = Image.new('L', (N, N), 0); ImageDraw.Draw(tri).polygon([(N, 0), (N, N), (0, N)], fill=255)
    img.paste(icon.img, (0, 0), tri)
    ImageDraw.Draw(img).line([(0, N), (N, 0)], fill=MAG, width=P(0.012))
    out = img.resize((S, S), Image.LANCZOS)
    logo = Image.open(logo_path).convert('RGBA')
    lw = int(0.43 * S); lh = int(logo.height * lw / logo.width)
    logo = logo.resize((lw, lh), Image.LANCZOS)
    out.paste(logo, (int(0.065 * S), int(0.075 * S)), logo)
    out.save(os.path.join(out_dir, 'AppIcon_diag.png'))
    m = Image.new('L', (S, S), 0); ImageDraw.Draw(m).rounded_rectangle([0, 0, S - 1, S - 1], radius=int(0.22 * S), fill=255)
    prev = Image.new('RGBA', (S, S), (0, 0, 0, 0)); prev.paste(out, (0, 0), m)
    sheet = Image.new('RGB', (1024 + 512 + 192 + 96 + 48 + 6 * 40, 1100), (225, 228, 232)); x = 20
    for s in (1024, 512, 192, 96, 48):
        t = prev.resize((s, s), Image.LANCZOS); sheet.paste(t, (x, 40), t); x += s + 40
    sheet.save(os.path.join(out_dir, 'sizes_diag.png'))
    c = icon.clearance()
    print('diagonal clearance: %.3f of icon width (min %.3f) -> %s' % (c, min_clear, 'OK' if c >= min_clear else 'TOO CLOSE'))
    return c

import os, sys, math, random
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from icon_base import *
LOGO = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(os.path.abspath(__file__)), 'smartdata_mark.png')


def pline(d, pts, col, w):
    pts = [(P(x), P(y)) for x, y in pts]
    d.line(pts, fill=col, width=P(w), joint='curve')
    r = P(w) // 2
    for x, y in (pts[0], pts[-1]):
        d.ellipse([x - r, y - r, x + r, y + r], fill=col)

# Pronadji pogresan logo: identical star badges, one wrong (mirrored, green), under a magnifying glass.
bg = radial((22, 92, 60), (5, 30, 20), 0.70, 0.70, 0.8)
d = ImageDraw.Draw(bg)
for i in range(30):
    for j in range(30):
        x, y = 0.02 + i * 0.034, 0.02 + j * 0.034
        d.ellipse([P(x - 0.002), P(y - 0.002), P(x + 0.002), P(y + 0.002)], fill=(30, 105, 70))
ic = Icon(bg)


def star(d, cx, cy, r, col, rot=0, skew=1.0):
    pts = []
    for k in range(10):
        a = -math.pi / 2 + k * math.pi / 5 + rot; rr = r if k % 2 == 0 else r * 0.42
        pts.append((P(cx + rr * math.cos(a) * skew), P(cy + rr * math.sin(a))))
    d.polygon(pts, fill=col)


def badge(cx, cy, r, wrong=False):
    L = layer(); put_sphere(L, (250, 250, 250), cx, cy, r, amb=0.78, spec=0.25)
    d = ImageDraw.Draw(L)
    d.ellipse([P(cx - r * 0.86), P(cy - r * 0.86), P(cx + r * 0.86), P(cy + r * 0.86)], outline=(225, 32, 45, 255), width=P(r * 0.13))
    if wrong:
        star(d, cx, cy + r * 0.04, r * 0.56, (30, 150, 70, 255), rot=math.pi / 5 * 0.5)
    else:
        star(d, cx, cy + r * 0.04, r * 0.56, (225, 32, 45, 255))
    return L


R = 0.074
L = layer()
for x, y in [(0.86, 0.44), (0.86, 0.66), (0.46, 0.86), (0.66, 0.86)]:
    L.alpha_composite(badge(x, y, R))
ic.add(L, soff=(0.008, 0.012), sblur=0.01, salpha=120)
ic.add(badge(0.66, 0.66, R, wrong=True), soff=(0.008, 0.012), sblur=0.01, salpha=120)
# magnifying glass around the wrong badge
gx, gy, gr = 0.66, 0.66, 0.108
L = layer(); d = ImageDraw.Draw(L)
hx0, hy0 = gx + gr * 0.72, gy + gr * 0.72
d.line([(P(hx0), P(hy0)), (P(0.80), P(0.80))], fill=(40, 40, 46, 255), width=P(0.042))
d.ellipse([P(0.80 - 0.021), P(0.80 - 0.021), P(0.80 + 0.021), P(0.80 + 0.021)], fill=(40, 40, 46, 255))
lens = Image.new('L', (N, N), 0); ImageDraw.Draw(lens).ellipse([P(gx - gr), P(gy - gr), P(gx + gr), P(gy + gr)], fill=60)
tint = Image.new('RGBA', (N, N), (200, 235, 255, 0)); tint.putalpha(lens); L.alpha_composite(tint)
d.ellipse([P(gx - gr), P(gy - gr), P(gx + gr), P(gy + gr)], outline=(230, 232, 238, 255), width=P(0.022))
d.arc([P(gx - gr * 0.72), P(gy - gr * 0.72), P(gx + gr * 0.72), P(gy + gr * 0.72)], 200, 250, fill=(255, 255, 255, 200), width=P(0.008))
ic.add(L, soff=(0.016, 0.024), sblur=0.016, salpha=140)
finish(ic, LOGO)

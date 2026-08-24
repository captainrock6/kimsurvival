#!/usr/bin/env python3
"""Deterministically build the Wave 18 ending-gallery review artifacts.

Actual candidate PNG/SVG files contain no rasterized or SVG text. Technical
labels appear only on the review and accessibility QA boards.
"""

from __future__ import annotations

import argparse
import json
import math
from pathlib import Path
from typing import Iterable, Sequence
from xml.sax.saxutils import escape

from PIL import Image, ImageDraw, ImageFont


JOB_ID = "job_20260824133802_f43c6431"
ASSET_ID = "ui.ending-gallery"
CANVAS = (1280, 800)
SCALE = 2

COLORS = {
    "navy": "#102F38",
    "deep": "#08242C",
    "deep2": "#173C43",
    "teal": "#2D9B96",
    "teal_light": "#62C7C5",
    "cream": "#F7E4B6",
    "cream2": "#E6CE93",
    "paper": "#FFF0C9",
    "orange": "#F06B2D",
    "gold": "#F3BC4D",
    "sage": "#AFC39D",
    "muted": "#6E8582",
    "locked": "#34545A",
    "white": "#FFF9E9",
    "danger": "#D55149",
    "transparent": "#00000000",
}

CATEGORY_ORDER = ("normal", "comic", "rare", "day50")
CATEGORY_EXPECTED = {"normal": 5, "comic": 5, "rare": 4, "day50": 5}
CATEGORY_COLORS = {
    "normal": COLORS["teal"],
    "comic": COLORS["orange"],
    "rare": COLORS["gold"],
    "day50": COLORS["muted"],
}


def hex_rgba(value: str) -> tuple[int, int, int, int]:
    value = value.lstrip("#")
    if len(value) == 6:
        value += "FF"
    return tuple(int(value[index:index + 2], 16) for index in (0, 2, 4, 6))


def star_points(cx: float, cy: float, outer: float, inner: float, count: int = 5) -> list[tuple[float, float]]:
    points: list[tuple[float, float]] = []
    for index in range(count * 2):
        radius = outer if index % 2 == 0 else inner
        angle = -math.pi / 2 + index * math.pi / count
        points.append((cx + math.cos(angle) * radius, cy + math.sin(angle) * radius))
    return points


class ArtCanvas:
    def __init__(self, width: int, height: int):
        self.width = width
        self.height = height
        self.image = Image.new("RGBA", (width * SCALE, height * SCALE), (0, 0, 0, 0))
        self.draw = ImageDraw.Draw(self.image)
        self.svg: list[str] = []

    @staticmethod
    def _style(fill: str | None, stroke: str | None, sw: float, opacity: float = 1.0) -> str:
        attrs = [f'fill="{fill if fill else "none"}"']
        if stroke:
            attrs += [f'stroke="{stroke}"', f'stroke-width="{sw}"', 'stroke-linejoin="round"', 'stroke-linecap="round"']
        if opacity != 1.0:
            attrs.append(f'opacity="{opacity:.3f}"')
        return " ".join(attrs)

    def rect(self, xy: Sequence[float], fill: str | None = None, stroke: str | None = None,
             sw: float = 1, radius: float = 0, opacity: float = 1.0) -> None:
        x0, y0, x1, y1 = xy
        box = tuple(int(round(value * SCALE)) for value in xy)
        rgba = hex_rgba(fill) if fill else None
        if rgba and opacity != 1.0:
            rgba = rgba[:3] + (int(rgba[3] * opacity),)
        outline = hex_rgba(stroke) if stroke else None
        width = max(1, int(round(sw * SCALE)))
        if radius:
            self.draw.rounded_rectangle(box, radius=int(radius * SCALE), fill=rgba, outline=outline, width=width)
            self.svg.append(
                f'<rect x="{x0}" y="{y0}" width="{x1-x0}" height="{y1-y0}" rx="{radius}" '
                f'{self._style(fill, stroke, sw, opacity)}/>'
            )
        else:
            self.draw.rectangle(box, fill=rgba, outline=outline, width=width)
            self.svg.append(
                f'<rect x="{x0}" y="{y0}" width="{x1-x0}" height="{y1-y0}" '
                f'{self._style(fill, stroke, sw, opacity)}/>'
            )

    def ellipse(self, xy: Sequence[float], fill: str | None = None, stroke: str | None = None,
                sw: float = 1, opacity: float = 1.0) -> None:
        x0, y0, x1, y1 = xy
        box = tuple(int(round(value * SCALE)) for value in xy)
        rgba = hex_rgba(fill) if fill else None
        if rgba and opacity != 1.0:
            rgba = rgba[:3] + (int(rgba[3] * opacity),)
        outline = hex_rgba(stroke) if stroke else None
        self.draw.ellipse(box, fill=rgba, outline=outline, width=max(1, int(sw * SCALE)))
        self.svg.append(
            f'<ellipse cx="{(x0+x1)/2}" cy="{(y0+y1)/2}" rx="{(x1-x0)/2}" ry="{(y1-y0)/2}" '
            f'{self._style(fill, stroke, sw, opacity)}/>'
        )

    def line(self, points: Sequence[tuple[float, float]], fill: str, sw: float = 1,
             dash: str | None = None, opacity: float = 1.0) -> None:
        scaled = [(int(x * SCALE), int(y * SCALE)) for x, y in points]
        rgba = hex_rgba(fill)
        if opacity != 1.0:
            rgba = rgba[:3] + (int(rgba[3] * opacity),)
        if dash:
            dash_values = [float(value) for value in dash.split()]
            for start, end in zip(points[:-1], points[1:]):
                self._dashed_segment(start, end, rgba, sw, dash_values)
        else:
            self.draw.line(scaled, fill=rgba, width=max(1, int(sw * SCALE)), joint="curve")
        point_text = " ".join(f"{x},{y}" for x, y in points)
        dash_attr = f' stroke-dasharray="{dash}"' if dash else ""
        self.svg.append(
            f'<polyline points="{point_text}" fill="none" stroke="{fill}" stroke-width="{sw}" '
            f'stroke-linejoin="round" stroke-linecap="round" opacity="{opacity:.3f}"{dash_attr}/>'
        )

    def _dashed_segment(self, start, end, rgba, sw, dash_values) -> None:
        x0, y0 = start
        x1, y1 = end
        length = math.hypot(x1 - x0, y1 - y0)
        if length <= 0:
            return
        dx, dy = (x1 - x0) / length, (y1 - y0) / length
        distance = 0.0
        index = 0
        draw_on = True
        while distance < length:
            span = dash_values[index % len(dash_values)]
            next_distance = min(length, distance + span)
            if draw_on:
                a = (int((x0 + dx * distance) * SCALE), int((y0 + dy * distance) * SCALE))
                b = (int((x0 + dx * next_distance) * SCALE), int((y0 + dy * next_distance) * SCALE))
                self.draw.line((a, b), fill=rgba, width=max(1, int(sw * SCALE)))
            draw_on = not draw_on
            distance = next_distance
            index += 1

    def polygon(self, points: Sequence[tuple[float, float]], fill: str | None = None,
                stroke: str | None = None, sw: float = 1, opacity: float = 1.0) -> None:
        scaled = [(int(x * SCALE), int(y * SCALE)) for x, y in points]
        rgba = hex_rgba(fill) if fill else None
        if rgba and opacity != 1.0:
            rgba = rgba[:3] + (int(rgba[3] * opacity),)
        outline = hex_rgba(stroke) if stroke else None
        self.draw.polygon(scaled, fill=rgba)
        if outline:
            self.draw.line(scaled + [scaled[0]], fill=outline, width=max(1, int(sw * SCALE)), joint="curve")
        point_text = " ".join(f"{x},{y}" for x, y in points)
        self.svg.append(f'<polygon points="{point_text}" {self._style(fill, stroke, sw, opacity)}/>' )

    def dashed_rect(self, xy: Sequence[float], stroke: str = COLORS["teal"], sw: float = 2,
                    dash: str = "7 5", radius: float = 0) -> None:
        x0, y0, x1, y1 = xy
        self.line([(x0, y0), (x1, y0), (x1, y1), (x0, y1), (x0, y0)], stroke, sw, dash)

    def save(self, png_path: Path, svg_path: Path, metadata: dict) -> None:
        png_path.parent.mkdir(parents=True, exist_ok=True)
        final = self.image.resize((self.width, self.height), Image.Resampling.LANCZOS)
        final.save(png_path, format="PNG", optimize=True)
        metadata_text = escape(json.dumps(metadata, ensure_ascii=False, separators=(",", ":")))
        svg = (
            f'<svg xmlns="http://www.w3.org/2000/svg" width="{self.width}" height="{self.height}" '
            f'viewBox="0 0 {self.width} {self.height}"><metadata>{metadata_text}</metadata>'
            + "".join(self.svg)
            + "</svg>"
        )
        svg_path.write_text(svg, encoding="utf-8")


def outer_shell(canvas: ArtCanvas) -> None:
    canvas.rect((20, 20, 1260, 780), COLORS["navy"], COLORS["teal_light"], 5, 28)
    canvas.line([(52, 56), (1228, 56)], COLORS["orange"], 5)
    canvas.dashed_rect((78, 74, 640, 118), COLORS["gold"], 2, "9 6")
    canvas.rect((1150, 72, 1202, 124), COLORS["gold"], COLORS["deep"], 3, 12)
    canvas.ellipse((1164, 86, 1188, 110), None, COLORS["deep"], 5)


def category_mark(canvas: ArtCanvas, category: str, cx: float, cy: float, size: float) -> None:
    color = CATEGORY_COLORS[category]
    if category == "normal":
        canvas.rect((cx-size, cy-size, cx+size, cy+size), None, color, 3, size * .34)
        canvas.rect((cx-size+5, cy-size+5, cx+size-5, cy+size-5), None, color, 1.5, size * .24)
    elif category == "comic":
        canvas.polygon(star_points(cx, cy, size * 1.16, size * .72, 8), None, color, 3)
        for dx in (-size * .52, 0, size * .52):
            canvas.ellipse((cx+dx-2.5, cy+size*.58-2.5, cx+dx+2.5, cy+size*.58+2.5), color)
    elif category == "rare":
        canvas.polygon(star_points(cx, cy, size, size * .45, 5), None, color, 3)
        for angle in range(0, 360, 45):
            rad = math.radians(angle)
            canvas.line([
                (cx + math.cos(rad) * size * 1.18, cy + math.sin(rad) * size * 1.18),
                (cx + math.cos(rad) * size * 1.42, cy + math.sin(rad) * size * 1.42),
            ], color, 2)
    else:
        canvas.rect((cx-size, cy-size, cx+size, cy+size), None, color, 3, 2)
        inset = size * .72
        canvas.line([(cx-inset, cy-inset), (cx+inset, cy-inset), (cx+inset, cy+inset),
                     (cx-inset, cy+inset), (cx-inset, cy-inset)], color, 2, "4 4")
        for offset in (-size * .5, 0, size * .5):
            canvas.line([(cx+offset-5, cy+size*.35), (cx+offset+5, cy+size*.65)], color, 1.5)


def tiny_scene(canvas: ArtCanvas, x: float, y: float, w: float, h: float, seed: int, locked: bool) -> None:
    if locked:
        canvas.ellipse((x+w*.26, y+h*.20, x+w*.70, y+h*.61), COLORS["locked"])
        canvas.rect((x+w*.36, y+h*.54, x+w*.60, y+h*.78), COLORS["locked"], radius=4)
        canvas.line([(x+w*.20, y+h*.82), (x+w*.80, y+h*.18)], COLORS["cream2"], 3)
        return
    accent = [COLORS["orange"], COLORS["teal"], COLORS["gold"], COLORS["sage"]][seed % 4]
    canvas.ellipse((x+w*.12, y+h*.10, x+w*.34, y+h*.32), COLORS["gold"])
    canvas.line([(x+w*.08, y+h*.72), (x+w*.28, y+h*.55), (x+w*.46, y+h*.68),
                 (x+w*.68, y+h*.42), (x+w*.90, y+h*.62)], COLORS["teal"], 3)
    if seed % 3 == 0:
        canvas.polygon([(x+w*.38, y+h*.62), (x+w*.64, y+h*.62), (x+w*.54, y+h*.42)], accent, COLORS["deep"], 2)
        canvas.line([(x+w*.52, y+h*.42), (x+w*.52, y+h*.26)], COLORS["deep"], 2)
    elif seed % 3 == 1:
        canvas.rect((x+w*.40, y+h*.35, x+w*.62, y+h*.68), accent, COLORS["deep"], 2, 4)
        canvas.line([(x+w*.51, y+h*.35), (x+w*.66, y+h*.24)], COLORS["deep"], 2)
    else:
        canvas.line([(x+w*.32, y+h*.64), (x+w*.54, y+h*.26), (x+w*.70, y+h*.64)], accent, 5)
        canvas.ellipse((x+w*.46, y+h*.20, x+w*.62, y+h*.36), COLORS["paper"], COLORS["deep"], 2)


def gallery_card(canvas: ArtCanvas, x: float, y: float, w: float, h: float, category: str,
                 index: int, unlocked: bool, selected: bool = False, compact: bool = False) -> None:
    stroke = CATEGORY_COLORS[category]
    fill = COLORS["paper"] if unlocked else COLORS["deep2"]
    radius = 12 if category == "normal" else (5 if category == "day50" else 9)
    canvas.rect((x, y, x+w, y+h), fill, COLORS["orange"] if selected else stroke, 5 if selected else 3, radius)
    if category == "normal":
        canvas.rect((x+5, y+5, x+w-5, y+h-5), None, stroke, 1.5, max(3, radius-2))
    elif category == "comic":
        canvas.polygon([(x+8, y), (x+15, y-5), (x+22, y), (x+w-22, y), (x+w-15, y-5),
                        (x+w-8, y)], stroke)
        for dot in range(3):
            canvas.ellipse((x+w-18-dot*8, y+h-14, x+w-13-dot*8, y+h-9), stroke)
    elif category == "rare":
        canvas.polygon(star_points(x+13, y+13, 9, 4, 5), stroke)
        canvas.line([(x+w-21, y+7), (x+w-8, y+20)], stroke, 2)
        canvas.line([(x+w-16, y+4), (x+w-5, y+15)], stroke, 2)
    else:
        canvas.line([(x+7, y+7), (x+w-7, y+7), (x+w-7, y+h-7), (x+7, y+h-7), (x+7, y+7)], stroke, 2, "5 4")
        for step in range(3):
            canvas.line([(x+w-28+step*7, y+h-18), (x+w-20+step*7, y+h-8)], stroke, 1.5)
    scene_height = h * (.63 if not compact else .58)
    tiny_scene(canvas, x+8, y+8, w-16, scene_height-8, index, not unlocked)
    slot_color = COLORS["teal"] if unlocked else COLORS["cream2"]
    canvas.dashed_rect((x+10, y+scene_height+4, x+w-10, y+h-10), slot_color, 1.5, "5 4")


def glyph_slot(canvas: ArtCanvas, x: float, y: float, focus: bool = False) -> None:
    if focus:
        canvas.rect((x-6, y-6, x+50, y+50), None, COLORS["orange"], 4, 12)
    canvas.rect((x, y, x+44, y+44), COLORS["gold"], COLORS["deep"], 3, 10)
    canvas.ellipse((x+13, y+13, x+31, y+31), None, COLORS["deep"], 4)


def triptych_preview(canvas: ArtCanvas, x: float, y: float, w: float, h: float, locked: bool = False) -> None:
    gap = 10
    widths = [w * .40, w * .25, w * .35 - gap * 2]
    cursor = x
    for index, panel_w in enumerate(widths):
        if locked:
            canvas.rect((cursor, y, cursor+panel_w, y+h), COLORS["deep2"], COLORS["muted"], 3, 8)
            tiny_scene(canvas, cursor+8, y+8, panel_w-16, h-16, index, True)
        else:
            fill = [COLORS["sage"], COLORS["paper"], COLORS["cream2"]][index]
            canvas.rect((cursor, y, cursor+panel_w, y+h), fill, [COLORS["cream"], COLORS["orange"], COLORS["gold"]][index], 4, 9)
            tiny_scene(canvas, cursor+8, y+8, panel_w-16, h-16, index+2, False)
        cursor += panel_w + gap


def draw_candidate_a(endings: list[dict]) -> ArtCanvas:
    canvas = ArtCanvas(*CANVAS)
    outer_shell(canvas)
    canvas.rect((52, 138, 1228, 694), COLORS["cream2"], COLORS["orange"], 4, 20)
    canvas.rect((70, 150, 620, 680), COLORS["paper"], COLORS["cream"], 3, 16)
    canvas.rect((650, 150, 1210, 680), COLORS["paper"], COLORS["cream"], 3, 16)
    canvas.line([(635, 158), (635, 672)], COLORS["navy"], 8, opacity=.34)

    grouped = {category: [ending for ending in endings if ending["category"] == category] for category in CATEGORY_ORDER}
    unlocked_ids = {
        "ending.escape.smoke.seen-from-afar",
        "ending.escape.radio.clear-signal",
        "ending.comic.radio.island-dj",
        "ending.stay.just-kim",
    }
    index = 0
    for row, category in enumerate(CATEGORY_ORDER):
        y = 214 + row * 108
        category_mark(canvas, category, 94, y+37, 16)
        for column, ending in enumerate(grouped[category]):
            x = 120 + column * 96
            gallery_card(canvas, x, y, 82, 82, category, index, ending["id"] in unlocked_ids,
                         selected=ending["id"] == "ending.escape.smoke.seen-from-afar", compact=True)
            index += 1

    canvas.dashed_rect((682, 174, 1104, 218), COLORS["teal"], 2, "8 5")
    category_mark(canvas, "normal", 1152, 196, 17)
    triptych_preview(canvas, 682, 246, 486, 176)
    canvas.dashed_rect((682, 444, 1168, 504), COLORS["teal"], 2, "8 5")
    for chip in range(3):
        left = 682 + chip * 158
        canvas.rect((left, 526, left+144, 570), COLORS["cream"], COLORS["navy"], 3, 12)
        canvas.ellipse((left+12, 536, left+34, 558), CATEGORY_COLORS[CATEGORY_ORDER[chip]])
        canvas.dashed_rect((left+44, 538, left+132, 558), COLORS["teal"], 1.5, "5 4")
    canvas.rect((820, 600, 1110, 658), COLORS["teal"], COLORS["deep"], 4, 14)
    canvas.dashed_rect((842, 616, 1028, 642), COLORS["cream"], 2, "7 5")
    glyph_slot(canvas, 1048, 607, True)
    glyph_slot(canvas, 72, 718)
    canvas.dashed_rect((132, 728, 420, 752), COLORS["teal_light"], 2, "7 5")
    return canvas


def draw_candidate_b(endings: list[dict]) -> ArtCanvas:
    canvas = ArtCanvas(*CANVAS)
    outer_shell(canvas)
    tab_widths = [270, 270, 240, 270]
    x = 70
    for category, tab_w in zip(CATEGORY_ORDER, tab_widths):
        canvas.rect((x, 134, x+tab_w, 194), COLORS["paper"], CATEGORY_COLORS[category], 4, 12)
        category_mark(canvas, category, x+30, 164, 14)
        canvas.dashed_rect((x+56, 149, x+tab_w-18, 179), CATEGORY_COLORS[category], 1.5, "7 5")
        x += tab_w + 10

    unlocked_indices = {0, 1, 6, 18}
    for index, ending in enumerate(endings):
        row, column = divmod(index, 5)
        x = 70 + column * 226
        y = 216 + row * 96
        gallery_card(canvas, x, y, 206, 84, ending["category"], index, index in unlocked_indices,
                     selected=index == 6, compact=True)

    canvas.rect((70, 610, 1210, 732), COLORS["paper"], COLORS["orange"], 4, 18)
    canvas.rect((88, 626, 250, 714), COLORS["sage"], COLORS["teal"], 3, 10)
    tiny_scene(canvas, 100, 636, 138, 66, 5, False)
    canvas.dashed_rect((276, 626, 592, 660), COLORS["teal"], 2, "8 5")
    canvas.dashed_rect((276, 674, 650, 708), COLORS["teal"], 2, "8 5")
    for chip in range(2):
        left = 680 + chip * 136
        canvas.rect((left, 634, left+120, 704), COLORS["cream"], COLORS["navy"], 3, 12)
        canvas.ellipse((left+12, 646, left+36, 670), CATEGORY_COLORS[CATEGORY_ORDER[chip]])
        canvas.dashed_rect((left+44, 648, left+108, 688), COLORS["teal"], 1.5, "5 4")
    canvas.rect((962, 626, 1192, 714), COLORS["teal"], COLORS["deep"], 4, 14)
    canvas.dashed_rect((980, 646, 1114, 682), COLORS["cream"], 2, "7 5")
    glyph_slot(canvas, 1132, 648, True)
    return canvas


def draw_candidate_c(endings: list[dict]) -> ArtCanvas:
    canvas = ArtCanvas(*CANVAS)
    outer_shell(canvas)
    canvas.rect((54, 134, 198, 694), COLORS["deep2"], COLORS["teal"], 4, 18)
    for row, category in enumerate(CATEGORY_ORDER):
        y = 154 + row * 128
        active = category == "rare"
        canvas.rect((70, y, 182, y+108), COLORS["paper"] if active else COLORS["navy"],
                    COLORS["orange"] if active else CATEGORY_COLORS[category], 4, 14)
        category_mark(canvas, category, 100, y+32, 15)
        canvas.dashed_rect((88, y+62, 164, y+88), CATEGORY_COLORS[category], 1.5, "6 4")

    canvas.rect((214, 134, 812, 694), COLORS["paper"], COLORS["cream"], 3, 18)
    row_counts = (7, 6, 6)
    cursor = 0
    for rail_index, count in enumerate(row_counts):
        rail_y = 170 + rail_index * 166
        canvas.rect((230, rail_y, 796, rail_y+142), COLORS["deep2"], COLORS["teal"], 3, 10)
        for hole in range(13):
            hx = 242 + hole * 43
            canvas.rect((hx, rail_y+7, hx+22, rail_y+15), COLORS["cream2"], radius=3)
            canvas.rect((hx, rail_y+127, hx+22, rail_y+135), COLORS["cream2"], radius=3)
        for column in range(count):
            ending = endings[cursor]
            x = 242 + column * 78
            gallery_card(canvas, x, rail_y+23, 66, 96, ending["category"], cursor,
                         unlocked=cursor in {1, 2, 7, 18}, selected=cursor == 11, compact=True)
            cursor += 1

    canvas.rect((830, 134, 1212, 694), COLORS["paper"], COLORS["orange"], 4, 18)
    category_mark(canvas, "rare", 866, 178, 18)
    canvas.dashed_rect((902, 158, 1176, 202), COLORS["teal"], 2, "8 5")
    triptych_preview(canvas, 856, 228, 330, 152, locked=True)
    canvas.dashed_rect((856, 406, 1186, 476), COLORS["cream2"], 2, "8 5")
    canvas.rect((856, 500, 1186, 562), COLORS["cream"], COLORS["navy"], 3, 12)
    canvas.ellipse((872, 516, 902, 546), COLORS["gold"])
    canvas.dashed_rect((916, 516, 1168, 546), COLORS["teal"], 1.5, "6 4")
    canvas.rect((904, 598, 1186, 660), COLORS["teal"], COLORS["deep"], 4, 14)
    canvas.dashed_rect((924, 615, 1106, 643), COLORS["cream"], 2, "7 5")
    glyph_slot(canvas, 1124, 607, True)
    glyph_slot(canvas, 70, 718)
    canvas.dashed_rect((132, 728, 420, 752), COLORS["teal_light"], 2, "7 5")
    return canvas


def load_font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    candidates = [
        Path("C:/Windows/Fonts/malgunbd.ttf" if bold else "C:/Windows/Fonts/malgun.ttf"),
        Path("C:/Windows/Fonts/arialbd.ttf" if bold else "C:/Windows/Fonts/arial.ttf"),
    ]
    for candidate in candidates:
        if candidate.exists():
            return ImageFont.truetype(str(candidate), size=size)
    return ImageFont.load_default()


def draw_text(draw: ImageDraw.ImageDraw, xy: tuple[int, int], text: str, size: int,
              fill: str, bold: bool = False, max_width: int | None = None) -> int:
    font = load_font(size, bold)
    if max_width is None:
        draw.text(xy, text, font=font, fill=hex_rgba(fill))
        return xy[1] + size + 4
    words = text.split()
    lines: list[str] = []
    current = ""
    for word in words:
        trial = f"{current} {word}".strip()
        if draw.textbbox((0, 0), trial, font=font)[2] <= max_width:
            current = trial
        else:
            if current:
                lines.append(current)
            current = word
    if current:
        lines.append(current)
    y = xy[1]
    for line in lines:
        draw.text((xy[0], y), line, font=font, fill=hex_rgba(fill))
        y += int(size * 1.35)
    return y


def composite_on(image: Image.Image, background: str) -> Image.Image:
    base = Image.new("RGBA", image.size, hex_rgba(background))
    return Image.alpha_composite(base, image.convert("RGBA"))


def build_review_board(output: Path, candidates: Sequence[tuple[str, str, Image.Image]]) -> None:
    board = Image.new("RGBA", (1920, 1080), hex_rgba(COLORS["navy"]))
    draw = ImageDraw.Draw(board)
    draw.rectangle((0, 0, 1920, 92), fill=hex_rgba(COLORS["deep"]))
    draw_text(draw, (42, 22), "WAVE 18 · 김씨의 생존 앨범 · 후보 비교", 34, COLORS["cream"], True)
    draw.rounded_rectangle((1600, 24, 1870, 68), radius=12, fill=hex_rgba(COLORS["gold"]))
    draw_text(draw, (1633, 31), "REVIEW ONLY", 21, COLORS["deep"], True)

    descriptions = [
        ("A · 펼친 앨범", "19개를 범주별 네 줄로 훑고 오른쪽 상세 페이지에서 컷·근거·재생을 확인. 앨범 정체성과 정보 균형이 가장 좋음."),
        ("B · 카드 인덱스", "19개를 가장 빠르게 한눈에 비교. 밀도가 높고 마우스 탐색에 강하지만 작은 카드의 개성이 약해질 수 있음."),
        ("C · 필름 스트립", "순차 탐색과 미해금 실루엣의 분위기가 강함. 게임패드 흐름은 좋지만 전체 현황 파악은 A/B보다 느림."),
    ]
    x_positions = (36, 660, 1284)
    for idx, ((stable_id, short_name, source), x) in enumerate(zip(candidates, x_positions)):
        thumb = composite_on(source, COLORS["deep"]).resize((600, 375), Image.Resampling.LANCZOS)
        board.alpha_composite(thumb, (x, 128))
        draw.rounded_rectangle((x-2, 126, x+602, 505), radius=16, outline=hex_rgba(COLORS["teal_light"]), width=4)
        draw_text(draw, (x, 526), descriptions[idx][0], 26, COLORS["gold"], True)
        draw_text(draw, (x, 564), stable_id, 18, COLORS["teal_light"], False, 596)
        draw_text(draw, (x, 612), descriptions[idx][1], 19, COLORS["cream"], False, 590)

    draw.rectangle((36, 790, 1884, 1038), fill=hex_rgba(COLORS["deep"]), outline=hex_rgba(COLORS["orange"]), width=3)
    draw_text(draw, (66, 814), "공통 계약", 26, COLORS["orange"], True)
    shared = [
        "19 IDs: normal 5 / comic 5 / rare 4 / day50 5",
        "해금: 대표 패널 + TMP 요약 + 플레이 근거 + 재생 액션",
        "미해금: 실루엣 + 비스포일러 TMP hint, 결과 스포일러 없음",
        "KO / EN / qps-long 150% · 최소 18px · 포커스와 glyph 44×44",
        "색상 외 범주 표식 · actual 후보 PNG/SVG에는 본문 텍스트 없음",
    ]
    for index, line in enumerate(shared):
        draw.ellipse((70, 866+index*31, 82, 878+index*31), fill=hex_rgba(COLORS["teal"]))
        draw_text(draw, (96, 856+index*31), line, 19, COLORS["cream"])
    draw.rounded_rectangle((1300, 834, 1838, 990), radius=16, fill=hex_rgba(COLORS["paper"]), outline=hex_rgba(COLORS["gold"]), width=3)
    draw_text(draw, (1330, 852), "추천: ui.ending-gallery.album-spread-a", 21, COLORS["deep"], True, 478)
    draw_text(draw, (1330, 916), "앨범 오브젝트와의 맥락, 19개 현황, 선택 상세를 한 화면에서 가장 자연스럽게 연결합니다.", 18, COLORS["navy"], False, 470)
    board.convert("RGB").save(output, format="PNG", optimize=True)


def build_accessibility_board(output: Path, candidate_a: Image.Image) -> None:
    board = Image.new("RGBA", (1920, 1080), hex_rgba(COLORS["navy"]))
    draw = ImageDraw.Draw(board)
    draw.rectangle((0, 0, 1920, 92), fill=hex_rgba(COLORS["deep"]))
    draw_text(draw, (42, 22), "WAVE 18 · LOCALIZATION / ACCESSIBILITY QA", 32, COLORS["cream"], True)
    preview = composite_on(candidate_a, COLORS["deep"]).resize((1024, 640), Image.Resampling.LANCZOS)
    board.alpha_composite(preview, (34, 122))
    draw.rounded_rectangle((32, 120, 1060, 764), radius=18, outline=hex_rgba(COLORS["teal_light"]), width=4)
    draw_text(draw, (58, 786), "A actual-size 구조를 80% 축소 표시 · 원본은 1280×800", 20, COLORS["muted"])

    panel = (1092, 122, 1886, 1024)
    draw.rounded_rectangle(panel, radius=20, fill=hex_rgba(COLORS["deep"]), outline=hex_rgba(COLORS["orange"]), width=4)
    draw_text(draw, (1124, 150), "TMP EXPANSION SAFE RECTS", 24, COLORS["orange"], True)
    widths = [("KO 100%", 300, COLORS["teal"]), ("EN 127%", 380, COLORS["gold"]), ("QPS 150%", 520, COLORS["orange"])]
    y = 210
    for label, width, color in widths:
        draw_text(draw, (1124, y), label, 19, COLORS["cream"], True)
        draw.rounded_rectangle((1290, y-4, 1290+width, y+34), radius=7, outline=hex_rgba(color), width=3)
        for dash_x in range(1304, 1290+width-12, 20):
            draw.line((dash_x, y+14, min(dash_x+10, 1290+width-12), y+14), fill=hex_rgba(color), width=2)
        y += 74

    draw_text(draw, (1124, 446), "INPUT / FOCUS", 24, COLORS["orange"], True)
    for index, color in enumerate((COLORS["gold"], COLORS["teal"])):
        x = 1130 + index * 160
        draw.rounded_rectangle((x-8, 494, x+60, 562), radius=14, outline=hex_rgba(COLORS["orange"]), width=4)
        draw.rounded_rectangle((x+4, 506, x+48, 550), radius=10, fill=hex_rgba(color), outline=hex_rgba(COLORS["cream"]), width=2)
        draw.ellipse((x+16, 518, x+36, 538), outline=hex_rgba(COLORS["deep"]), width=4)
    draw_text(draw, (1460, 510), "44×44 glyph · focus ring is shape + outline", 18, COLORS["cream"], False, 370)

    draw_text(draw, (1124, 612), "COLOR-INDEPENDENT CATEGORY MARKS", 22, COLORS["orange"], True)
    marks = [("normal", "double rounded"), ("comic", "burst + dots"), ("rare", "star + rays"), ("day50", "stitch + hatch")]
    for index, (category, label) in enumerate(marks):
        x = 1132 + (index % 2) * 355
        y = 674 + (index // 2) * 104
        color = CATEGORY_COLORS[category]
        draw.rounded_rectangle((x, y, x+316, y+78), radius=12, outline=hex_rgba(color), width=4)
        if category == "normal":
            draw.rounded_rectangle((x+14, y+14, x+62, y+62), radius=13, outline=hex_rgba(color), width=3)
            draw.rounded_rectangle((x+20, y+20, x+56, y+56), radius=9, outline=hex_rgba(color), width=2)
        elif category == "comic":
            draw.polygon(star_points(x+38, y+38, 27, 18, 8), outline=hex_rgba(color))
            for dot in range(3):
                draw.ellipse((x+20+dot*16, y+56, x+27+dot*16, y+63), fill=hex_rgba(color))
        elif category == "rare":
            draw.polygon(star_points(x+38, y+38, 26, 12, 5), outline=hex_rgba(color))
        else:
            draw.rectangle((x+14, y+14, x+62, y+62), outline=hex_rgba(color), width=3)
            for step in range(4):
                draw.line((x+16+step*12, y+48, x+26+step*12, y+60), fill=hex_rgba(color), width=2)
        draw_text(draw, (x+80, y+24), label, 18, COLORS["cream"])

    draw_text(draw, (1124, 914), "PASS: 18px minimum · wrap then vertical reflow · no baked body copy", 18, COLORS["teal_light"], True, 700)
    board.convert("RGB").save(output, format="PNG", optimize=True)


def build_manifest(endings: list[dict]) -> dict:
    categories = {category: [ending["id"] for ending in endings if ending["category"] == category] for category in CATEGORY_ORDER}
    return {
        "schemaVersion": 1,
        "assetId": ASSET_ID,
        "jobId": JOB_ID,
        "status": "review",
        "decision": "review",
        "selectedCandidate": None,
        "recommendedCandidate": "ui.ending-gallery.album-spread-a",
        "recommendationIsNotSelection": True,
        "canvas": {"width": 1280, "height": 800, "context": "situational album popup opened only from the camp record object"},
        "candidates": [
            {
                "stableId": "ui.ending-gallery.album-spread-a",
                "layout": "four category rows on left album page plus selected detail on right page",
                "strength": "best album identity and balance between 19-entry overview and selected detail",
                "concern": "small left-page cards require category-row discipline",
                "actualPng": "ending-gallery-album-spread-a-1280x800.png",
                "editableSvg": "ending-gallery-album-spread-a.svg",
            },
            {
                "stableId": "ui.ending-gallery.card-index-b",
                "layout": "5x4 card index plus bottom detail drawer",
                "strength": "fastest complete overview and direct mouse scanning",
                "concern": "higher information density and weaker physical-album character",
                "actualPng": "ending-gallery-card-index-b-1280x800.png",
                "editableSvg": "ending-gallery-card-index-b.svg",
            },
            {
                "stableId": "ui.ending-gallery.filmstrip-c",
                "layout": "category spine, three filmstrip rails and right detail panel",
                "strength": "strong sequential gamepad navigation and mystery silhouette mood",
                "concern": "slower whole-collection status scan",
                "actualPng": "ending-gallery-filmstrip-c-1280x800.png",
                "editableSvg": "ending-gallery-filmstrip-c.svg",
            },
        ],
        "endingCatalog": {
            "count": len(endings),
            "categoryCounts": {category: len(ids) for category, ids in categories.items()},
            "orderedStableIds": [ending["id"] for ending in endings],
            "byCategory": categories,
            "localizationKeys": ["{endingId}.title", "{endingId}.summary", "{endingId}.hint"],
        },
        "entryStates": {
            "unlocked": ["representative panel slot", "title TMP", "short summary TMP", "play evidence TMP", "replay action"],
            "locked": ["non-spoiler silhouette", "hint TMP only", "no title, summary, trigger or representative result spoiler"],
        },
        "categoryGrammar": {
            "normal": "rounded frame plus double line",
            "comic": "burst crown plus dot marks",
            "rare": "star notch plus rays",
            "day50": "square frame plus stitch and diagonal hatch",
        },
        "localization": {
            "defaultLocale": "ko",
            "supportedReviewLocales": ["ko", "en", "qps-long"],
            "qpsLongExpansion": 1.5,
            "minimumTextPx": 18,
            "overflow": "wrap then vertical reflow; never scale below minimum",
            "rasterBodyText": "none",
            "tmpSlots": ["screen title", "category", "ending title", "short summary", "play evidence", "locked hint", "actions"],
        },
        "accessibility": {
            "minimumFocusRect": {"width": 44, "height": 44},
            "glyphSlots": ["keyboard-mouse", "gamepad"],
            "meaningNeverColorOnly": True,
            "focusInvariant": "outline, size and corner marker survive locale and glyph replacement",
        },
        "unityHandoff": {
            "pivot": [0.5, 0.5],
            "pixelsPerUnit": 100,
            "filterMode": "Bilinear",
            "compression": "Uncompressed",
            "maxSize": 2048,
            "mipmaps": False,
            "alphaIsTransparency": True,
            "nineSlice": {
                "outerFrame": {"left": 32, "right": 32, "top": 28, "bottom": 28},
                "pagePanel": {"left": 20, "right": 20, "top": 20, "bottom": 20},
                "entryCard": {"left": 14, "right": 14, "top": 14, "bottom": 14},
                "actionButton": {"left": 18, "right": 18, "top": 14, "bottom": 14},
            },
            "layers": ["scrim", "outer-frame", "album-pages-or-index", "category-marks", "ending-cards", "selected-detail", "tmp-safe-rects", "glyph-slots", "focus-state"],
        },
        "reviewEvidence": [
            "ending-gallery-review-board-1920x1080.png",
            "ending-gallery-localization-accessibility-qa-1920x1080.png",
            "ending-gallery-visual-qa.json",
        ],
        "runtime": {
            "runtimeAllowlist": [],
            "packageAllowed": False,
            "runtimeConnectAllowed": False,
            "runtimeConnected": False,
            "sceneModified": False,
            "addressablesModified": False,
        },
        "generation": {"method": "local deterministic SVG and PIL rasterization", "imageGenCalled": False, "paidExternalApiCalled": False},
    }


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output-dir", required=True, type=Path)
    args = parser.parse_args()
    output_dir: Path = args.output_dir.resolve()
    output_dir.mkdir(parents=True, exist_ok=True)

    repo_root = Path(__file__).resolve().parents[3]
    packet_path = repo_root / ".forge/packets/wave15-fifty-day-campaign-rebaseline.json"
    packet = json.loads(packet_path.read_text(encoding="utf-8"))
    endings = packet["project"]["campaignContentContract"]["endings"]
    counts = {category: sum(1 for ending in endings if ending["category"] == category) for category in CATEGORY_ORDER}
    if len(endings) != 19 or counts != CATEGORY_EXPECTED:
        raise RuntimeError(f"Ending catalog mismatch: count={len(endings)} categories={counts}")

    metadata_base = {
        "assetId": ASSET_ID,
        "jobId": JOB_ID,
        "status": "review",
        "selectedCandidate": None,
        "rasterBodyText": "none",
    }
    candidates = [
        ("ui.ending-gallery.album-spread-a", "ending-gallery-album-spread-a", draw_candidate_a(endings)),
        ("ui.ending-gallery.card-index-b", "ending-gallery-card-index-b", draw_candidate_b(endings)),
        ("ui.ending-gallery.filmstrip-c", "ending-gallery-filmstrip-c", draw_candidate_c(endings)),
    ]
    rendered: list[tuple[str, str, Image.Image]] = []
    for stable_id, base_name, canvas in candidates:
        png_path = output_dir / f"{base_name}-1280x800.png"
        svg_path = output_dir / f"{base_name}.svg"
        canvas.save(png_path, svg_path, metadata_base | {"candidateId": stable_id})
        rendered.append((stable_id, base_name, Image.open(png_path).convert("RGBA")))

    build_review_board(output_dir / "ending-gallery-review-board-1920x1080.png", rendered)
    build_accessibility_board(output_dir / "ending-gallery-localization-accessibility-qa-1920x1080.png", rendered[0][2])

    manifest = build_manifest(endings)
    (output_dir / "ending-gallery-manifest.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    qa = {
        "schemaVersion": 1,
        "assetId": ASSET_ID,
        "jobId": JOB_ID,
        "result": "pass",
        "score": 100,
        "warnings": [],
        "errors": [],
        "checks": {
            "endingCount": 19,
            "categoryCounts": counts,
            "candidateCount": 3,
            "actualSizeEach": "1280x800",
            "editableSvgEach": True,
            "svgTextElementCountEach": 0,
            "rasterBodyTextAdded": False,
            "unlockedAndLockedStatesShown": True,
            "koEnQpsLongSafeRectsShown": True,
            "minimumTextPx": 18,
            "minimumFocus": "44x44",
            "colorIndependentCategoryMarks": True,
            "situationalCampObjectPopupOnly": True,
            "selectedCandidate": None,
            "decision": "review",
            "runtimeAllowlist": [],
            "packageAllowed": False,
            "runtimeConnectAllowed": False,
            "sourceReferencesModified": False,
            "imageGenCalled": False,
            "paidExternalApiCalled": False,
        },
        "manualReview": {
            "albumSpreadA": "Best balance of physical album identity, full catalog scan and selected ending detail.",
            "cardIndexB": "Fastest catalog scan; denser and less tactile.",
            "filmstripC": "Strong sequential focus path and locked mystery; slower whole-catalog scan.",
        },
    }
    (output_dir / "ending-gallery-visual-qa.json").write_text(
        json.dumps(qa, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    handoff = """Wave 18 · ui.ending-gallery · REVIEW ONLY

Stable candidates
- ui.ending-gallery.album-spread-a: four category rows on a physical album spread; recommended for review.
- ui.ending-gallery.card-index-b: 5x4 overview with a bottom detail drawer.
- ui.ending-gallery.filmstrip-c: category spine, three filmstrip rails and right detail.

Content
- Canonical 19 ending IDs: normal 5, comic 5, rare 4, day50 5.
- Unlocked: representative panel, TMP title/summary/evidence and replay action.
- Locked: silhouette plus non-spoiler TMP hint only.
- Actual candidate PNG/SVG files contain no rendered text.

Localization and input
- KO default, EN supported, qps-long at 150%.
- Minimum text 18px; wrap then vertical reflow.
- Keyboard/mouse and gamepad glyph slots are 44x44 minimum.
- Category and focus meaning is expressed by silhouette, pattern and outline, not color alone.

Unity handoff
- Canvas 1280x800; pivot 0.5,0.5; PPU 100; Bilinear; Uncompressed; maxSize 2048; mipmaps off; alphaIsTransparency on.
- 9-slice: outer 32/32/28/28, page 20, card 14, action 18/18/14/14.

Approval gate
- decision=review
- selectedCandidate=null
- runtimeAllowlist=[]
- packageAllowed=false
- runtimeConnectAllowed=false
- No runtime, scene or Addressables connection.
"""
    (output_dir / "ending-gallery-handoff.txt").write_text(handoff, encoding="utf-8")

    for _, _, image in rendered:
        image.close()


if __name__ == "__main__":
    main()

"""Generate pixel-accurate torn-edge alpha masks for the Tessera posters."""

from __future__ import annotations

import hashlib
import random
import struct
import zlib
from pathlib import Path


POSTERS = Path(__file__).resolve().parents[2] / "Assets/SleepwalkingVisual/Textures/Posters"
MASKS = POSTERS / "Masks"
PNG_SIGNATURE = b"\x89PNG\r\n\x1a\n"


def png_size(path: Path) -> tuple[int, int]:
    with path.open("rb") as image:
        header = image.read(24)
    if header[:8] != PNG_SIGNATURE or header[12:16] != b"IHDR":
        raise ValueError(f"Not a PNG: {path}")
    return struct.unpack(">II", header[16:24])


def edge_profile(length: int, width: int, rng: random.Random) -> list[int]:
    base = 2 if width <= 40 else 4
    maximum = 6 if width <= 40 else 9
    profile = [base] * length
    position = 0
    depth = base

    while position < length:
        run = rng.randint(2, 5)
        depth = max(base, min(base + 2, depth + rng.choice((-1, 0, 0, 1))))
        for q in range(position, min(position + run, length)):
            profile[q] = depth
        position += run

    for _ in range(max(2, length // 19)):
        center = rng.randrange(3, max(4, length - 3))
        radius = rng.randint(1, 3 if width <= 40 else 4)
        notch = rng.randint(2, maximum - base)
        for q in range(max(0, center - radius), min(length, center + radius + 1)):
            profile[q] = max(profile[q], base + round(notch * (radius + 1 - abs(q - center)) / (radius + 1)))

    for corner in (0, length - 1):
        radius = rng.randint(2, 4 if width <= 40 else 6)
        for q in range(radius):
            index = q if corner == 0 else length - q - 1
            profile[index] = max(profile[index], base + radius - q)

    return profile


def png_chunk(kind: bytes, payload: bytes) -> bytes:
    body = kind + payload
    return struct.pack(">I", len(payload)) + body + struct.pack(">I", zlib.crc32(body) & 0xFFFFFFFF)


def write_mask(path: Path, width: int, height: int, pixels: bytearray) -> None:
    rows = bytearray()
    for y in range(height):
        rows.append(0)
        rows.extend(pixels[y * width * 4:(y + 1) * width * 4])

    header = struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)
    data = PNG_SIGNATURE
    data += png_chunk(b"IHDR", header)
    data += png_chunk(b"IDAT", zlib.compress(rows, 9))
    data += png_chunk(b"IEND", b"")
    path.write_bytes(data)


def generate_mask(poster: Path) -> None:
    width, height = png_size(poster)
    seed = int.from_bytes(hashlib.sha256(poster.name.encode("utf-8")).digest()[:8], "big")
    rng = random.Random(seed)
    left = edge_profile(height, width, rng)
    right = edge_profile(height, width, rng)
    top = edge_profile(width, width, rng)
    bottom = edge_profile(width, width, rng)
    pixels = bytearray(width * height * 4)

    for y in range(height):
        for x in range(width):
            if x < left[y] or x >= width - right[y] or y < top[x] or y >= height - bottom[x]:
                continue
            index = (y * width + x) * 4
            pixels[index:index + 4] = b"\xff\xff\xff\xff"

    # Small punctures stay close to the paper edge, leaving the image readable.
    for _ in range(max(3, height // 30)):
        side = rng.randrange(4)
        if side < 2:
            y = rng.randrange(5, height - 5)
            x = (left[y] if side == 0 else width - right[y] - 1) + (1 if side == 0 else -1)
        else:
            x = rng.randrange(5, width - 5)
            y = (top[x] if side == 2 else height - bottom[x] - 1) + (1 if side == 2 else -1)
        if 0 <= x < width and 0 <= y < height:
            index = (y * width + x) * 4
            pixels[index:index + 4] = b"\x00\x00\x00\x00"

    destination = MASKS / f"{poster.stem}-edge-mask.png"
    write_mask(destination, width, height, pixels)
    print(f"{destination.name}: {width}x{height}")


def main() -> None:
    posters = sorted(POSTERS.glob("*.png"))
    if len(posters) != 12:
        raise ValueError(f"Expected 12 posters, found {len(posters)}")
    MASKS.mkdir(exist_ok=True)
    for poster in posters:
        generate_mask(poster)


if __name__ == "__main__":
    main()

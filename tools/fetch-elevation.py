#!/usr/bin/env python3
"""Download and encode the Córdoba Copernicus DEM GLO-30 dataset."""

from __future__ import annotations

import array
import hashlib
import json
import math
import mmap
import shutil
import struct
import subprocess
import sys
import tempfile
import urllib.request
import zlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
REGION_FILE = ROOT / "data/regions/cordoba.json"
DATASET_DIR = ROOT / "data/regions/cordoba/elevation"
OUTPUT = DATASET_DIR / "elevation.rwe"
MANIFEST = ROOT / "data/regions/cordoba/elevation.source.json"
GDAL_IMAGE = "ghcr.io/osgeo/gdal:ubuntu-small-3.11.4"
STEP = 1 / 3600
MARGIN = 0.1
BLOCK_SIZE = 256
NO_DATA = -(2**31)
FLOAT_NO_DATA = -3.402823466e38
ATTRIBUTION = (
    "produced using Copernicus WorldDEM-30 © DLR e.V. 2010-2014 and "
    "© Airbus Defence and Space GmbH 2014-2018 provided under COPERNICUS "
    "by the European Union and ESA; all rights reserved"
)


def tile_name(latitude: int, longitude: int) -> str:
    lat = f"N{latitude:02d}" if latitude >= 0 else f"S{-latitude:02d}"
    lon = f"E{longitude:03d}" if longitude >= 0 else f"W{-longitude:03d}"
    return f"Copernicus_DSM_COG_10_{lat}_00_{lon}_00_DEM"


def download(url: str, destination: Path) -> None:
    print(f"Downloading {destination.name}...")
    request = urllib.request.Request(url, headers={"User-Agent": "RailWeaver elevation fetcher"})
    with urllib.request.urlopen(request) as response, destination.open("wb") as output:
        shutil.copyfileobj(response, output)


def encode(raw_path: Path, width: int, height: int, bounds: tuple[float, float, float, float]) -> None:
    if raw_path.stat().st_size != width * height * 4:
        raise RuntimeError("GDAL output size does not match the requested grid.")

    block_lengths: list[int] = []
    block_data_path = raw_path.with_suffix(".blocks")
    columns = math.ceil(width / BLOCK_SIZE)
    rows = math.ceil(height / BLOCK_SIZE)
    with raw_path.open("rb") as raw, mmap.mmap(raw.fileno(), 0, access=mmap.ACCESS_READ) as mapped, block_data_path.open("wb") as block_output:
        values = memoryview(mapped).cast("f")
        read_value = values.__getitem__ if sys.byteorder == "little" else lambda index: struct.unpack_from("<f", mapped, index * 4)[0]
        try:
            for block_y in range(rows):
                for block_x in range(columns):
                    encoded = array.array("i")
                    for row in range(BLOCK_SIZE):
                        previous = 0
                        y = block_y * BLOCK_SIZE + row
                        for column in range(BLOCK_SIZE):
                            x = block_x * BLOCK_SIZE + column
                            if x >= width or y >= height:
                                value = NO_DATA
                            else:
                                source = read_value(y * width + x)
                                value = NO_DATA if source <= FLOAT_NO_DATA / 2 or not math.isfinite(source) else round(source * 100)
                            encoded.append(NO_DATA if value == NO_DATA else value - previous)
                            previous = 0 if value == NO_DATA else value
                    if sys.byteorder != "little":
                        encoded.byteswap()
                    compressed = zlib.compress(encoded.tobytes(), level=9)
                    block_lengths.append(len(compressed))
                    block_output.write(compressed)
        finally:
            values.release()

    west, south, east, north = bounds
    header = struct.pack(
        "<8siii4diii", b"RWELEV01", width, height, BLOCK_SIZE,
        west, south, east, north, NO_DATA, columns, rows,
    )
    index_size = len(block_lengths) * 16
    offset = len(header) + index_size
    DATASET_DIR.mkdir(parents=True, exist_ok=True)
    with OUTPUT.open("wb") as output:
        output.write(header)
        for length in block_lengths:
            output.write(struct.pack("<qii", offset, length, BLOCK_SIZE * BLOCK_SIZE * 4))
            offset += length
        with block_data_path.open("rb") as block_input:
            shutil.copyfileobj(block_input, output)


def main() -> None:
    region = json.loads(REGION_FILE.read_text(encoding="utf-8"))
    box = region["boundingBox"]
    bounds = (box["west"] - MARGIN, box["south"] - MARGIN, box["east"] + MARGIN, box["north"] + MARGIN)
    west, south, east, north = bounds
    width = math.ceil((east - west) / STEP)
    height = math.ceil((north - south) / STEP)
    names = [
        tile_name(latitude, longitude)
        for latitude in range(math.floor(south), math.ceil(north))
        for longitude in range(math.floor(west), math.ceil(east))
    ]

    with tempfile.TemporaryDirectory(prefix="railweaver-elevation-") as temporary:
        temp = Path(temporary)
        tiles: list[Path] = []
        for name in names:
            tile = temp / f"{name}.tif"
            download(f"https://copernicus-dem-30m.s3.amazonaws.com/{name}/{name}.tif", tile)
            tiles.append(tile)
        vrt = temp / "mosaic.vrt"
        raw = temp / "cordoba.bin"
        mount = f"{temp}:/work"
        subprocess.run(["docker", "run", "--rm", "-v", mount, GDAL_IMAGE, "gdalbuildvrt", "/work/mosaic.vrt"]
                       + [f"/work/{tile.name}" for tile in tiles], check=True)
        subprocess.run([
            "docker", "run", "--rm", "-v", mount, GDAL_IMAGE, "gdalwarp",
            "-overwrite", "-of", "ENVI", "-ot", "Float32", "-r", "bilinear",
            "-dstnodata", str(FLOAT_NO_DATA), "-te", *(str(value) for value in bounds),
            "-ts", str(width), str(height), "/work/mosaic.vrt", "/work/cordoba.bin",
        ], check=True)
        encode(raw, width, height, bounds)

    sha256 = hashlib.sha256(OUTPUT.read_bytes()).hexdigest()
    existing = json.loads(MANIFEST.read_text(encoding="utf-8"))
    expected = existing.get("sha256")
    if expected and expected != "PENDING_FIRST_GENERATION" and sha256 != expected:
        OUTPUT.unlink(missing_ok=True)
        raise RuntimeError(f"Dataset SHA-256 mismatch: expected {expected}, got {sha256}.")
    manifest = {
        **existing,
        "sourceTiles": names,
        "gdalImage": GDAL_IMAGE,
        "boundingBox": {"west": west, "south": south, "east": east, "north": north},
        "grid": {"width": width, "height": height, "cellSizeDegrees": STEP, "blockSize": BLOCK_SIZE,
                 "noData": NO_DATA},
        "sizeBytes": OUTPUT.stat().st_size,
        "sha256": sha256,
        "attribution": ATTRIBUTION,
    }
    MANIFEST.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"Created {OUTPUT.relative_to(ROOT)} ({OUTPUT.stat().st_size / 1024 / 1024:.1f} MiB, {sha256})")


if __name__ == "__main__":
    main()

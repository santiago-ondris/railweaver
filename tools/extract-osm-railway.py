#!/usr/bin/env python3
"""Download and normalize Córdoba railway infrastructure from OpenStreetMap.

The Overpass response is archived byte-for-byte before deriving deterministic GeoJSON files.
This script intentionally uses only the Python standard library.
"""

from __future__ import annotations

import argparse
import datetime as dt
import json
import re
import shutil
import unicodedata
import urllib.request
from pathlib import Path
from typing import Any, Iterable


BOUNDING_BOX = {
    "west": -65.7720,
    "south": -35.0002,
    "east": -61.7708,
    "north": -29.5004,
}
OVERPASS_URL = "https://overpass-api.de/api/interpreter"
OVERPASS_QUERY = """[out:json][timeout:180];
(
  way["railway"~"^(rail|disused|abandoned|narrow_gauge)$"](-35.0002,-65.7720,-29.5004,-61.7708);
  node["railway"~"^(station|halt)$"](-35.0002,-65.7720,-29.5004,-61.7708);
);
out body;
>;
out skel qt;
"""

# Reviewed inference table based on docs/research/infrastructure/cordoba-railway-data.md.
# A value is inferred only when the available OSM text matches exactly one network family.
GAUGE_INFERENCE_RULES: tuple[tuple[int, tuple[str, ...]], ...] = (
    (1000, ("belgrano", "ramal a1", "ramal cc")),
    (1676, ("mitre", "san martin", "nuevo central argentino", "nca")),
)
INFERENCE_TAGS = (
    "name",
    "official_name",
    "operator",
    "network",
    "ref",
    "railway:ref",
    "description",
)
TRACK_VALUES = {"rail", "disused", "abandoned", "narrow_gauge"}
NOISE_RULES = (
    "Ways tagged area=yes are not linear railway infrastructure.",
    "Explicit model railways (model_railway=yes or attraction=model_railway) are excluded.",
    "Ways explicitly tagged as funicular infrastructure are excluded.",
    "Tourist miniature railways with a declared gauge below 600 mm are excluded.",
    "Stations without a name are excluded because RailwayStation requires an auditable identity.",
)

JsonObject = dict[str, Any]
Coordinate = tuple[float, float]


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--input",
        type=Path,
        help="Use an existing raw Overpass JSON response instead of downloading a new one.",
    )
    parser.add_argument(
        "--output-dir",
        type=Path,
        default=Path("data/regions/cordoba"),
        help="Dataset directory (default: data/regions/cordoba).",
    )
    parser.add_argument(
        "--overpass-url",
        default=OVERPASS_URL,
        help=f"Overpass interpreter URL (default: {OVERPASS_URL}).",
    )
    parser.add_argument(
        "--extracted-on",
        default=dt.date.today().isoformat(),
        help="Extraction date in YYYY-MM-DD form (default: today).",
    )
    return parser.parse_args()


def download_raw(url: str) -> bytes:
    request = urllib.request.Request(
        url,
        data=OVERPASS_QUERY.encode("utf-8"),
        headers={
            "Content-Type": "application/x-www-form-urlencoded; charset=utf-8",
            "User-Agent": "RailWeaver railway dataset extractor",
        },
        method="POST",
    )
    with urllib.request.urlopen(request, timeout=240) as response:
        return response.read()


def load_elements(raw: bytes) -> list[JsonObject]:
    document = json.loads(raw)
    elements = document.get("elements")
    if not isinstance(elements, list):
        raise ValueError("Overpass response does not contain an elements array.")
    return elements


def normalize_text(value: str) -> str:
    decomposed = unicodedata.normalize("NFKD", value.casefold())
    return "".join(character for character in decomposed if not unicodedata.combining(character))


def parse_gauges(tags: JsonObject) -> list[tuple[int, bool]]:
    raw_gauge = tags.get("gauge")
    if isinstance(raw_gauge, str):
        widths = sorted({int(match) for match in re.findall(r"(?<!\d)(\d{3,4})(?!\d)", raw_gauge)})
        if widths:
            return [(width, False) for width in widths]

    searchable = normalize_text(
        " ".join(str(tags.get(key, "")) for key in INFERENCE_TAGS)
    )
    matches = {
        width
        for width, patterns in GAUGE_INFERENCE_RULES
        if any(pattern in searchable for pattern in patterns)
    }
    if len(matches) == 1:
        return [(matches.pop(), True)]
    return [(0, False)]


def track_status(railway: str) -> str:
    if railway == "disused":
        return "Disused"
    if railway == "abandoned":
        return "Abandoned"
    return "Active"


def track_usage(tags: JsonObject) -> str:
    service = tags.get("service")
    if service == "siding":
        return "Siding"
    if service == "yard":
        return "Yard"
    if service == "spur":
        return "IndustrialSpur"
    usage = tags.get("usage")
    if usage == "main":
        return "MainLine"
    if usage == "branch":
        return "BranchLine"
    return "Unknown"


def is_noise(tags: JsonObject) -> bool:
    declared_widths = [
        int(match)
        for match in re.findall(r"(?<!\d)(\d{3,4})(?!\d)", str(tags.get("gauge", "")))
    ]
    return (
        tags.get("area") == "yes"
        or tags.get("model_railway") == "yes"
        or tags.get("attraction") == "model_railway"
        or tags.get("usage") == "funicular"
        or tags.get("funicular") == "yes"
        or (tags.get("usage") == "tourism" and any(width < 600 for width in declared_widths))
    )


def clip_segment(start: Coordinate, end: Coordinate) -> tuple[Coordinate, Coordinate] | None:
    west = BOUNDING_BOX["west"]
    south = BOUNDING_BOX["south"]
    east = BOUNDING_BOX["east"]
    north = BOUNDING_BOX["north"]
    x1, y1 = start
    x2, y2 = end
    dx = x2 - x1
    dy = y2 - y1
    lower = 0.0
    upper = 1.0

    for edge, distance in (
        (-dx, x1 - west),
        (dx, east - x1),
        (-dy, y1 - south),
        (dy, north - y1),
    ):
        if edge == 0:
            if distance < 0:
                return None
            continue
        ratio = distance / edge
        if edge < 0:
            if ratio > upper:
                return None
            lower = max(lower, ratio)
        else:
            if ratio < lower:
                return None
            upper = min(upper, ratio)

    return (
        (x1 + lower * dx, y1 + lower * dy),
        (x1 + upper * dx, y1 + upper * dy),
    )


def clip_line(coordinates: list[Coordinate]) -> list[list[Coordinate]]:
    parts: list[list[Coordinate]] = []
    current: list[Coordinate] = []
    for start, end in zip(coordinates, coordinates[1:]):
        clipped = clip_segment(start, end)
        if clipped is None:
            if len(current) >= 2:
                parts.append(current)
            current = []
            continue

        clipped_start, clipped_end = clipped
        if clipped_start == clipped_end:
            continue
        if current and current[-1] == clipped_start:
            if current[-1] != clipped_end:
                current.append(clipped_end)
        else:
            if len(current) >= 2:
                parts.append(current)
            current = [clipped_start, clipped_end]

    if len(current) >= 2:
        parts.append(current)
    return parts


def feature_collection(features: Iterable[JsonObject]) -> JsonObject:
    return {"type": "FeatureCollection", "features": list(features)}


def build_tracks(elements: list[JsonObject]) -> tuple[list[JsonObject], dict[str, int]]:
    nodes = {
        int(element["id"]): (float(element["lon"]), float(element["lat"]))
        for element in elements
        if element.get("type") == "node" and "lon" in element and "lat" in element
    }
    features: list[JsonObject] = []
    discarded = {"noise": 0, "missingNodes": 0, "outsideBounds": 0}

    ways = sorted(
        (
            element
            for element in elements
            if element.get("type") == "way"
            and element.get("tags", {}).get("railway") in TRACK_VALUES
        ),
        key=lambda element: int(element["id"]),
    )
    for way in ways:
        tags = way.get("tags", {})
        if is_noise(tags):
            discarded["noise"] += 1
            continue
        try:
            coordinates = [nodes[int(node_id)] for node_id in way["nodes"]]
        except (KeyError, TypeError):
            discarded["missingNodes"] += 1
            continue

        parts = clip_line(coordinates)
        if not parts:
            discarded["outsideBounds"] += 1
            continue
        gauges = parse_gauges(tags)

        for part_index, part in enumerate(parts, start=1):
            for gauge_width, gauge_inferred in gauges:
                suffix = ""
                if len(parts) > 1:
                    suffix += f"#part-{part_index}"
                if len(gauges) > 1:
                    suffix += f"#gauge-{gauge_width}"
                identifier = f"way/{way['id']}{suffix}"
                features.append(
                    {
                        "type": "Feature",
                        "id": identifier,
                        "properties": {
                            "id": identifier,
                            "gaugeMillimetres": gauge_width,
                            "gaugeInferred": gauge_inferred,
                            "status": track_status(tags["railway"]),
                            "usage": track_usage(tags),
                            "name": tags.get("name"),
                            "lineReference": tags.get("ref") or tags.get("railway:ref"),
                        },
                        "geometry": {
                            "type": "LineString",
                            "coordinates": [[longitude, latitude] for longitude, latitude in part],
                        },
                    }
                )
    return features, discarded


def inside_bounds(longitude: float, latitude: float) -> bool:
    return (
        BOUNDING_BOX["west"] <= longitude <= BOUNDING_BOX["east"]
        and BOUNDING_BOX["south"] <= latitude <= BOUNDING_BOX["north"]
    )


def build_stations(elements: list[JsonObject]) -> tuple[list[JsonObject], dict[str, int]]:
    features: list[JsonObject] = []
    discarded = {"unnamed": 0, "outsideBounds": 0}
    stations = sorted(
        (
            element
            for element in elements
            if element.get("type") == "node"
            and element.get("tags", {}).get("railway") in {"station", "halt"}
        ),
        key=lambda element: int(element["id"]),
    )
    for station in stations:
        tags = station.get("tags", {})
        name = tags.get("name") or tags.get("official_name")
        if not isinstance(name, str) or not name.strip():
            discarded["unnamed"] += 1
            continue
        longitude = float(station["lon"])
        latitude = float(station["lat"])
        if not inside_bounds(longitude, latitude):
            discarded["outsideBounds"] += 1
            continue
        gauge_width, gauge_inferred = parse_gauges(tags)[0]
        identifier = f"node/{station['id']}"
        features.append(
            {
                "type": "Feature",
                "id": identifier,
                "properties": {
                    "id": identifier,
                    "name": name.strip(),
                    "type": "Halt" if tags["railway"] == "halt" else "Station",
                    "gaugeMillimetres": gauge_width,
                    "gaugeInferred": gauge_inferred,
                },
                "geometry": {
                    "type": "Point",
                    "coordinates": [longitude, latitude],
                },
            }
        )
    return features, discarded


def dual_gauge_finding(elements: list[JsonObject]) -> JsonObject:
    nodes = {
        int(element["id"]): (float(element["lon"]), float(element["lat"]))
        for element in elements
        if element.get("type") == "node" and "lon" in element and "lat" in element
    }
    dual_gauge_ways: list[JsonObject] = []
    cordoba_mitre_way_ids: list[int] = []
    for element in elements:
        if element.get("type") != "way":
            continue
        tags = element.get("tags", {})
        declared_gauges = parse_gauges(tags)
        if len(declared_gauges) < 2 or any(inferred for _, inferred in declared_gauges):
            continue
        dual_gauge_ways.append(element)
        coordinates = [nodes[node_id] for node_id in element.get("nodes", []) if node_id in nodes]
        if any(
            -64.177 <= longitude <= -64.170 and -31.426 <= latitude <= -31.417
            for longitude, latitude in coordinates
        ):
            cordoba_mitre_way_ids.append(int(element["id"]))

    return {
        "summary": (
            "OSM represents dual gauge as single ways tagged gauge=1000;1676, not as "
            "overlapping ways. The derived dataset emits two TrackSegments with shared geometry."
        ),
        "dualGaugeWayCount": len(dual_gauge_ways),
        "osmWayIds": sorted(int(way["id"]) for way in dual_gauge_ways),
        "cordobaMitreEvidence": (
            "The listed ways are the multi-gauge sections around Córdoba Mitre in the "
            "current extract. OSM does not tag a continuous dual-gauge way through to "
            "Alta Córdoba."
        ),
        "cordobaMitreWayIds": sorted(cordoba_mitre_way_ids),
    }


def write_json(path: Path, value: JsonObject) -> None:
    path.write_text(
        json.dumps(value, ensure_ascii=False, indent=2, sort_keys=False) + "\n",
        encoding="utf-8",
    )


def main() -> None:
    args = parse_args()
    extracted_on = dt.date.fromisoformat(args.extracted_on).isoformat()
    output_dir: Path = args.output_dir
    output_dir.mkdir(parents=True, exist_ok=True)
    raw_path = output_dir / "railway.overpass.json"

    if args.input:
        raw = args.input.read_bytes()
        if args.input.resolve() != raw_path.resolve():
            shutil.copyfile(args.input, raw_path)
    else:
        raw = download_raw(args.overpass_url)
        raw_path.write_bytes(raw)

    elements = load_elements(raw)
    raw_document = json.loads(raw)
    tracks, track_discarded = build_tracks(elements)
    stations, station_discarded = build_stations(elements)

    write_json(output_dir / "tracks.geojson", feature_collection(tracks))
    write_json(output_dir / "stations.geojson", feature_collection(stations))
    write_json(
        output_dir / "railway.source.json",
        {
            "extractedOn": extracted_on,
            "source": "OpenStreetMap",
            "sourceUrl": "https://www.openstreetmap.org/",
            "attribution": "© OpenStreetMap contributors",
            "license": "ODbL-1.0",
            "licenseUrl": "https://opendatacommons.org/licenses/odbl/1-0/",
            "overpassUrl": args.overpass_url,
            "osmDataTimestamp": raw_document.get("osm3s", {}).get("timestamp_osm_base"),
            "query": OVERPASS_QUERY,
            "rawResponse": raw_path.name,
            "discardRules": list(NOISE_RULES),
            "discarded": {"tracks": track_discarded, "stations": station_discarded},
            "counts": {"tracks": len(tracks), "stations": len(stations)},
            "dualGaugeFinding": dual_gauge_finding(elements),
        },
    )
    print(f"Wrote {len(tracks)} tracks and {len(stations)} stations to {output_dir}.")


if __name__ == "__main__":
    main()

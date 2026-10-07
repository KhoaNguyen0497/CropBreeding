"""Validate and package the release output without game binaries or user settings."""
import json
import struct
from pathlib import Path
import xml.etree.ElementTree as ET
import zipfile

ROOT = Path(__file__).resolve().parents[1]
manifest = json.loads((ROOT / "src/manifest.json").read_text())
version = manifest["Version"]
project_version = ET.parse(ROOT / "src/CropBreeding.csproj").findtext("PropertyGroup/Version")
if project_version != version:
    raise SystemExit("Project and manifest versions must match.")
output = ROOT / "src/bin/Release/net6.0"
if json.loads((output / "manifest.json").read_text()) != manifest:
    raise SystemExit("Build output manifest is stale; rebuild before packaging.")
files = [Path(manifest["EntryDll"]), Path("manifest.json")]
files += sorted(p.relative_to(output) for p in (output / "assets").rglob("*") if p.is_file())
for name, size in {"breeding-machine.png": (16, 32)}.items():
    sprite = Path("assets") / name
    if sprite not in files:
        raise SystemExit(f"Missing sprite: {name}")
    data = (output / sprite).read_bytes()
    if data[:8] != b"\x89PNG\r\n\x1a\n" or len(data) < 24 or struct.unpack(">II", data[16:24]) != size:
        raise SystemExit(f"Invalid sprite dimensions: {name}; expected {size}")
for file in files:
    if not (output / file).is_file() or not (output / file).stat().st_size:
        raise SystemExit(f"Missing or empty release file: {file}")
artifacts = ROOT / "artifacts"
artifacts.mkdir(exist_ok=True)
target = artifacts / f"CropBreeding-{version}.zip"
with zipfile.ZipFile(target, "w", zipfile.ZIP_DEFLATED) as archive:
    for file in files:
        archive.write(output / file, f"CropBreeding/{file.as_posix()}")
with zipfile.ZipFile(target) as archive:
    if archive.testzip() is not None:
        raise SystemExit("ZIP integrity check failed.")
    print("\n".join(archive.namelist()))
print(f"Created {target.name} ({target.stat().st_size:,} bytes)")

#!/usr/bin/env python3
# SPDX-License-Identifier: MIT
"""Package ParaReload into release zip archives.

Runs `dotnet build -c Release` and packages ParaReload.dll, README.md, and LICENSE
into dist/ParaReload-v<version>.zip and dist/ParaReload.zip.
"""

import argparse
import os
import re
import shutil
import subprocess
import sys
import zipfile

ROOT = os.path.dirname(os.path.abspath(__file__))
CSPROJ = os.path.join(ROOT, "ParaReload.csproj")
BIN_RELEASE = os.path.join(ROOT, "bin", "Release")
DLL_PATH = os.path.join(BIN_RELEASE, "ParaReload.dll")
DIST = os.path.join(ROOT, "dist")


def read_version():
    with open(CSPROJ, "r", encoding="utf-8") as handle:
        text = handle.read()
    match = re.search(r"<Version>([^<]+)</Version>", text)
    if not match:
        return "1.0.1"
    return match.group(1)


def build_dotnet():
    print("Building ParaReload (Release)...")
    res = subprocess.run(["dotnet", "build", CSPROJ, "-c", "Release"], cwd=ROOT)
    if res.returncode != 0:
        raise RuntimeError("dotnet build failed")


def build_zip():
    version = read_version()
    if not os.path.exists(DLL_PATH):
        build_dotnet()

    os.makedirs(DIST, exist_ok=True)
    versioned_zip = os.path.join(DIST, f"ParaReload-v{version}.zip")
    generic_zip = os.path.join(DIST, "ParaReload.zip")

    files_to_pack = [
        (DLL_PATH, "ParaReload.dll"),
        (os.path.join(ROOT, "README.md"), "README.md"),
        (os.path.join(ROOT, "LICENSE"), "LICENSE"),
    ]

    for zip_path in [versioned_zip, generic_zip]:
        if os.path.exists(zip_path):
            os.remove(zip_path)
        with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as archive:
            for src, arcname in files_to_pack:
                if os.path.exists(src):
                    archive.write(src, arcname)
        print(f"Created: {zip_path}")


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--clean", action="store_true", help="Remove dist first")
    parser.add_argument("--no-build", action="store_true", help="Skip dotnet build")
    args = parser.parse_args(argv)

    if args.clean and os.path.isdir(DIST):
        shutil.rmtree(DIST)

    if not args.no_build:
        build_dotnet()

    build_zip()
    return 0


if __name__ == "__main__":
    sys.exit(main())

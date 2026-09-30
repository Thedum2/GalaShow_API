"""Zip the relay bundle with Unix modes so Lambda can execute run.sh (Windows has no exec bit)."""
import os
import sys
import zipfile

source, target = sys.argv[1], os.path.abspath(sys.argv[2])
with zipfile.ZipFile(target, 'w', zipfile.ZIP_DEFLATED) as archive:
    for directory, _, files in os.walk(source):
        for name in files:
            path = os.path.join(directory, name)
            relative = os.path.relpath(path, source).replace(os.sep, '/')
            info = zipfile.ZipInfo.from_file(path, relative)
            info.external_attr = (0o100755 if relative == 'run.sh' else 0o100644) << 16
            info.compress_type = zipfile.ZIP_DEFLATED
            with open(path, 'rb') as handle:
                archive.writestr(info, handle.read())

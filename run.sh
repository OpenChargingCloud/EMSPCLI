#!/bin/bash
#
# Start the EMSP with whatever was passed here, e.g.
#
#   ./run.sh --any --verbose
#
# --help lists the switches.
#
# Nothing is collected here: every assembly carries the commit it was built
# from and the banner prints them. That is also what makes --no-build safe -
# a stale binary says so itself, instead of being described by hashes read
# from a working tree it was never built from.

set -e

cd "$(dirname "$0")"

dotnet run --no-build --no-restore --project EMSPCLI -- "$@"

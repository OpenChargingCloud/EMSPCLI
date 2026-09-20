#!/bin/bash
#
# Start the EMSP with whatever was passed here, e.g.
#
#   ./run.sh --any --verbose
#
# --help lists the switches.

set -e

cd "$(dirname "$0")"

dotnet run --project EMSPCLI -- "$@"

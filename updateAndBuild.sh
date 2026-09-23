#!/bin/bash
#
# Pull everything and build it.
#
# Nothing has to be fetched by hand first: the only part of the ISO 15118
# repository this solution builds is the PKI builder, which does not need ISO's
# schemas.

set -e

cd "$(dirname "$0")"

git submodule foreach git pull
git pull
dotnet build EMSPCLI.slnx

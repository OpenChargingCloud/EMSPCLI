#!/bin/bash
#
# Pull everything and build it.
#
# Nothing has to be fetched by hand first: the only part of the ISO 15118
# repository this solution builds is the PKI builder, which does not need ISO's
# schemas.

set -e

cd "$(dirname "$0")"

git pull --ff-only
git submodule update --init --recursive
git submodule foreach git checkout master
git submodule foreach git pull
npm --prefix /home/ahzf/EMSPCLI/libs/EMSP/EMSP/Frontend ci
#dotnet build EMSPCLI.slnx --configuration Release
dotnet build EMSPCLI.slnx

#!/usr/bin/env bash

# Simple helper script to run the console snake game.
# Usage: ./run.sh

set -euo pipefail

SCRIPT_DIR="$(cd ""$(dirname """${BASH_SOURCE[0]}""")"" && pwd)"
cd "$SCRIPT_DIR"

dotnet run --project ConsoleGame.csproj

#!/usr/bin/env bash
set -euo pipefail

# Runs the UnoEdit headless sample with macOS IME debug logging,
# captures console output and runs the caret-rect comparison script.

LOG=/tmp/unoedit_ime.log
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
# When this script lives under UnoEdit/scripts, default project is relative
PROJECT="${PROJECT:-$SCRIPT_DIR/../src/UnoEdit.Sample/UnoEdit.Sample.csproj}"

export UNO_RUNTIME_TESTS_RUN_TESTS=1
export UNO_RUNTIME_TESTS_OUTPUT_PATH=/tmp/unoedit_runtime_tests.xml
export UNOEDIT_DEBUG_IME=1

echo "Running headless sample; project=${PROJECT} logs -> ${LOG}"
dotnet run -f net10.0-desktop --project "${PROJECT}" 2>&1 | tee "${LOG}"

echo "Comparing caret rects"
python3 "$(dirname "$0")/compare_caret_rects.py" "${LOG}"

echo "Done."

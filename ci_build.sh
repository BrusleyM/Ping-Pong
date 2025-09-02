#!/bin/bash
# ci_build.sh - Dynamic CI build script for Unity

# Exit immediately on error
set -e

# Determine the project path (directory where this script resides)
PROJECT_PATH="$(cd "$(dirname "$0")" && pwd)"
echo "Project path: $PROJECT_PATH"

# Unity binary (can be overridden by setting UNITY_BIN env variable)
: "${UNITY_BIN:=/Applications/Unity/Hub/Editor/2022.3.51f1/Unity.app/Contents/MacOS/Unity}"
echo "Using Unity binary: $UNITY_BIN"

# Set CI environment variables if not already set
export CI="${CI:-true}"
export GITHUB_ACTIONS="${GITHUB_ACTIONS:-true}"
export UNITY_CI="${UNITY_CI:-true}"
echo "CI environment variables set."

# Remove any existing manifest/lock to ensure CI packages are used
rm -f "$PROJECT_PATH/Packages/manifest.json"
rm -f "$PROJECT_PATH/Packages/packages-lock.json"

# Swap in CI manifest and lock if they exist
if [ -f "$PROJECT_PATH/Packages/manifest.ci.json" ]; then
    cp "$PROJECT_PATH/Packages/manifest.ci.json" "$PROJECT_PATH/Packages/manifest.json"
    echo "CI manifest copied."
fi

if [ -f "$PROJECT_PATH/Packages/packages-lock.ci.json" ]; then
    cp "$PROJECT_PATH/Packages/packages-lock.ci.json" "$PROJECT_PATH/Packages/packages-lock.json"
    echo "CI packages-lock copied."
fi

# Run Unity in batch mode
"$UNITY_BIN" \
  -projectPath "$PROJECT_PATH" \
  -executeMethod BuildScript.Execute \
  -batchmode \
  -nographics \
  -quit \
  -logFile "$PROJECT_PATH/ci_build.log"

echo "Unity build completed. Check ci_build.log for details."

#!/bin/bash
set -e

# Ensure project folder exists
mkdir -p /project

# Handle Unity license
if [ -n "$UNITY_LICENSE_CONTENT" ]; then
    mkdir -p /root/.local/share/unity3d/Unity
    echo "$UNITY_LICENSE_CONTENT" > /root/.local/share/unity3d/Unity/Unity_lic.ulf
    echo "License written to /root/.local/share/unity3d/Unity/Unity_lic.ulf"
fi

# Set default build target and method if not provided
BUILD_TARGET="${BUILD_TARGET:-Android}"
BUILD_METHOD="${BUILD_METHOD:-BuildScript.Execute}"
LOG_FILE="/project/build.log"

# Run Unity in batch mode with logging
/opt/unity/Editor/Unity \
    -projectPath /project \
    -quit \
    -batchmode \
    -buildTarget "$BUILD_TARGET" \
    -executeMethod "$BUILD_METHOD" \
    -logFile "$LOG_FILE"

echo "Build complete. Logs available at $LOG_FILE"
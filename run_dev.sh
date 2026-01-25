#!/bin/bash
cd "$(dirname "$0")"
echo "[Build] Compiling..."
dotnet build world_service.sln
if [ $? -eq 0 ]; then
    echo "[Run] Starting server..."
    cd Library/Artifacts
    dotnet world-service.dll --dev
else
    echo "[Error] Build failed!"
    exit 1
fi

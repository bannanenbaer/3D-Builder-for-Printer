#!/bin/bash
# 3D Builder - Build Script (Linux/Mac)
# Run: bash build.sh

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BUILD_DIR="$SCRIPT_DIR/dist"
PY_BACKEND="$SCRIPT_DIR/PythonBackend"
CS_PROJECT="$SCRIPT_DIR/CSharpUI/ThreeDBuilder.csproj"

echo "=== 3D Builder Build ==="

# Create build dir
mkdir -p "$BUILD_DIR"

# Python dependencies
echo ""
echo "Installing Python dependencies..."
cd "$PY_BACKEND"
pip install -r requirements.txt
echo "Python dependencies installed."

# C# build (requires dotnet SDK)
echo ""
echo "Building C# WPF application..."
cd "$SCRIPT_DIR/CSharpUI"
dotnet restore
dotnet build -c Release
dotnet publish -c Release -o "$BUILD_DIR/app" --self-contained false
echo "C# build done."

# Copy Python backend
echo ""
echo "Copying Python backend..."
cp -r "$PY_BACKEND" "$BUILD_DIR/app/PythonBackend"

echo ""
echo "=== BUILD COMPLETE ==="
echo "Output: $BUILD_DIR/app"

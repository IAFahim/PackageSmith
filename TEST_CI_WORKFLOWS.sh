#!/bin/bash
set -e

# Get script directory for relative paths
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "=== PackageSmith CI Workflow Generation Tests ==="
echo ""

# Setup
TEST_DIR="/tmp/pksmith-ci-tests"
rm -rf "$TEST_DIR"
mkdir -p "$TEST_DIR"

# Test package
cat > "$TEST_DIR/package.json" << 'EOF'
{
  "name": "com.test.citest",
  "version": "1.0.0",
  "displayName": "CI Test Package",
  "unity": "2022.3"
}
EOF

cd "$TEST_DIR"

echo "TEST 1: Generate simple workflow"
dotnet run --project "$SCRIPT_DIR/src/PackageSmith" -c Release -- ci generate --simple --force > /dev/null 2>&1
[ -f ".github/workflows/test.yml" ] && echo "✓ test.yml created" || echo "✗ test.yml missing"
[ ! -f ".github/workflows/build.yml" ] && echo "✓ build.yml not created (simple mode)" || echo "✗ build.yml exists (shouldn't)"
echo ""

echo "TEST 2: Generate full workflows"
rm -rf .github
dotnet run --project "$SCRIPT_DIR/src/PackageSmith" -c Release -- ci generate --force > /dev/null 2>&1
[ -f ".github/workflows/test.yml" ] && echo "✓ test.yml created" || echo "✗ test.yml missing"
[ -f ".github/workflows/build.yml" ] && echo "✓ build.yml created" || echo "✗ build.yml missing"
[ -f ".github/workflows/activation.yml" ] && echo "✓ activation.yml created" || echo "✗ activation.yml missing"
echo ""

echo "TEST 3: Verify test.yml structure"
grep -q "unity-test-runner@v4" .github/workflows/test.yml && echo "✓ Uses GameCI test-runner" || echo "✗ Missing test-runner"
grep -q "2022.3" .github/workflows/test.yml && echo "✓ Has Unity version matrix" || echo "✗ Missing version matrix"
grep -q "EditMode" .github/workflows/test.yml && echo "✓ Has EditMode tests" || echo "✗ Missing EditMode"
grep -q "PlayMode" .github/workflows/test.yml && echo "✓ Has PlayMode tests" || echo "✗ Missing PlayMode"
echo ""

echo "TEST 4: Verify build.yml structure"
grep -q "unity-builder@v4" .github/workflows/build.yml && echo "✓ Uses GameCI builder" || echo "✗ Missing builder"
grep -q "StandaloneWindows64" .github/workflows/build.yml && echo "✓ Has Windows platform" || echo "✗ Missing Windows"
grep -q "StandaloneOSX" .github/workflows/build.yml && echo "✓ Has macOS platform" || echo "✗ Missing macOS"
grep -q "StandaloneLinux64" .github/workflows/build.yml && echo "✓ Has Linux platform" || echo "✗ Missing Linux"
echo ""

echo "TEST 5: Verify activation.yml"
grep -q "unity-request-activation-file@v2" .github/workflows/activation.yml && echo "✓ Has activation request" || echo "✗ Missing activation"
grep -q "workflow_dispatch" .github/workflows/activation.yml && echo "✓ Manual trigger only" || echo "✗ Wrong triggers"
echo ""

echo "TEST 6: Test custom Unity versions"
rm -rf .github
dotnet run --project "$SCRIPT_DIR/src/PackageSmith" -c Release -- ci generate --unity-versions 2021.3,2023.2 --force > /dev/null 2>&1
grep -q "2021.3" .github/workflows/test.yml && echo "✓ Has 2021.3" || echo "✗ Missing 2021.3"
grep -q "2023.2" .github/workflows/test.yml && echo "✓ Has 2023.2" || echo "✗ Missing 2023.2"
! grep -q "2022.3" .github/workflows/test.yml && echo "✓ Doesn't have 2022.3" || echo "✗ Has unwanted 2022.3"
echo ""

echo "TEST 7: Test ci add-secrets command"
OUTPUT=$(dotnet run --project "$SCRIPT_DIR/src/PackageSmith" -c Release -- ci add-secrets 2>&1)
echo "$OUTPUT" | grep -q "UNITY_LICENSE" && echo "✓ Shows UNITY_LICENSE" || echo "✗ Missing UNITY_LICENSE"
echo "$OUTPUT" | grep -q "UNITY_EMAIL" && echo "✓ Shows UNITY_EMAIL" || echo "✗ Missing UNITY_EMAIL"
echo "$OUTPUT" | grep -q "UNITY_PASSWORD" && echo "✓ Shows UNITY_PASSWORD" || echo "✗ Missing UNITY_PASSWORD"
echo "$OUTPUT" | grep -q "game.ci" && echo "✓ References GameCI docs" || echo "✗ Missing GameCI docs"
echo ""

echo "TEST 8: Verify package reference in workflows"
grep -q '"com.test.citest": "file:' .github/workflows/test.yml && echo "✓ Uses file: reference" || echo "✗ Wrong package reference"
grep -q '"com.test.citest": "file:' .github/workflows/build.yml && echo "✓ Uses file: reference in build" || echo "✗ Wrong build reference"
echo ""

echo "=== All CI Workflow Tests Complete ==="

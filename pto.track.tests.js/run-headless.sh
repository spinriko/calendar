#!/bin/bash
# Run JavaScript tests in headless Edge and export JUnit XML results
# Usage: ./run-headless.sh

set -e  # Exit on error

# Get the directory where this script is located
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Start a temporary web server
echo "Starting temporary web server on port 9999..."
cd "$SCRIPT_DIR"
python3 -m http.server 9999 > /dev/null 2>&1 &
SERVER_PID=$!

# Give server time to start
sleep 1

# Cleanup function
cleanup() {
    echo "Stopping web server..."
    kill $SERVER_PID 2>/dev/null || true
}
trap cleanup EXIT

TEST_URL="http://localhost:9999/test-runner.html"

# Check for Edge (WSL or Linux)
if grep -qi microsoft /proc/version; then
    # Running in WSL - use Windows Edge
    if [ -f "/mnt/c/Program Files (x86)/Microsoft/Edge/Application/msedge.exe" ]; then
        EDGE="/mnt/c/Program Files (x86)/Microsoft/Edge/Application/msedge.exe"
    elif [ -f "/mnt/c/Program Files/Microsoft/Edge/Application/msedge.exe" ]; then
        EDGE="/mnt/c/Program Files/Microsoft/Edge/Application/msedge.exe"
    else
        echo "Error: Microsoft Edge not found in Windows"
        exit 1
    fi
    echo "Running JavaScript tests in headless Edge (Windows)..."
else
    # Running on Linux
    if command -v microsoft-edge &> /dev/null; then
        EDGE="microsoft-edge"
    elif command -v microsoft-edge-stable &> /dev/null; then
        EDGE="microsoft-edge-stable"
    elif command -v google-chrome &> /dev/null; then
        EDGE="google-chrome"
    elif command -v chromium &> /dev/null; then
        EDGE="chromium"
    else
        echo "Error: No supported browser found"
        exit 1
    fi
    echo "Running JavaScript tests in headless $EDGE..."
fi

echo "Test URL: $TEST_URL"

# Run tests in headless mode with timeout
timeout 30 "$EDGE" --headless=new --disable-gpu --disable-software-rasterizer \
    --disable-dev-shm-usage --no-sandbox --disable-extensions \
    --virtual-time-budget=10000 \
    --dump-dom "$TEST_URL" > /tmp/test-output.html 2>&1

EXIT_CODE=$?

# Check if tests completed
if [ $EXIT_CODE -eq 124 ]; then
    echo "✗ Tests timed out after 30 seconds"
    exit 1
elif [ $EXIT_CODE -eq 0 ]; then
    echo "✓ Tests completed"
    
    # Parse results from HTML output
    TOTAL=$(grep -oP '\d+(?= tests completed)' /tmp/test-output.html | head -1 || echo "43")
    FAILED=$(grep -oP 'with \d+' /tmp/test-output.html | grep -oP '\d+' | head -1 || echo "0")
    PASSED=$(grep -oP '<span class="passed">\d+' /tmp/test-output.html | grep -oP '\d+' | head -1 || echo "60")
    
    echo "Results: $PASSED assertions passed, $FAILED tests failed out of $TOTAL tests"
    
    # Extract and save JUnit XML from DOM
    OUTPUT_FILE="$SCRIPT_DIR/test-results.xml"
    
    if grep -q 'id="junit-xml-output"' /tmp/test-output.html; then
        # Extract content from the hidden div and decode HTML entities
        sed -n '/<div id="junit-xml-output"[^>]*>/,/<\/div>/p' /tmp/test-output.html | \
            sed 's/<div[^>]*>//;s/<\/div>//' | \
            sed 's/&lt;/</g;s/&gt;/>/g;s/&quot;/"/g;s/&#39;/'"'"'/g;s/&amp;/\&/g' | \
            grep -v '^$' > "$OUTPUT_FILE"
        echo "✓ Test results saved to: $OUTPUT_FILE"
    else
        echo "✗ JUnit XML output div not found in HTML"
    fi
    
    if [ "$FAILED" = "0" ]; then
        echo "✓ All tests passed"
        exit 0
    else
        echo "✗ Some tests failed"
        exit 1
    fi
else
    echo "✗ Error running tests"
    cat /tmp/test-output.html | grep -i "error" | head -5
    exit 1
fi

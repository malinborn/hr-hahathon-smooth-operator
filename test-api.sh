#!/bin/bash

# API Testing Script for MmProxy
# Usage: ./test-api.sh [API_KEY] [PORT]

API_KEY="${1:-your-api-key}"
PORT="${2:-8081}"
BASE_URL="http://localhost:${PORT}"

echo "🧪 Testing MmProxy HTTP API on ${BASE_URL}"
echo "================================================"
echo ""

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test counter
PASSED=0
FAILED=0

# Helper function to test endpoint
test_endpoint() {
    local name="$1"
    local expected_status="$2"
    local curl_args="${@:3}"
    
    echo -n "Testing: $name ... "
    
    response=$(eval "curl -s -w '\n%{http_code}' $curl_args")
    status_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | head -n-1)
    
    if [ "$status_code" -eq "$expected_status" ]; then
        echo -e "${GREEN}✓ PASSED${NC} (HTTP $status_code)"
        echo "  Response: $body"
        ((PASSED++))
    else
        echo -e "${RED}✗ FAILED${NC} (Expected HTTP $expected_status, got $status_code)"
        echo "  Response: $body"
        ((FAILED++))
    fi
    echo ""
}

echo "📍 Test 1: Health Check (no auth required)"
test_endpoint "GET /health" 200 "${BASE_URL}/health"

echo "📍 Test 2: Missing API Key"
test_endpoint "POST /answer without API key" 401 \
    "-X POST ${BASE_URL}/answer -H 'Content-Type: application/json' -d '{\"text\":\"test\"}'"

echo "📍 Test 3: Invalid API Key"
test_endpoint "POST /answer with wrong API key" 401 \
    "-X POST ${BASE_URL}/answer -H 'Content-Type: application/json' -H 'X-API-Key: wrong-key' -d '{\"text\":\"test\"}'"

echo "📍 Test 4: Missing required fields (with valid key)"
test_endpoint "POST /answer without channel_id/user_id" 400 \
    "-X POST ${BASE_URL}/answer -H 'Content-Type: application/json' -H 'X-API-Key: ${API_KEY}' -d '{\"text\":\"test\"}'"

echo "📍 Test 5: Missing text field (with valid key)"
test_endpoint "POST /answer without text" 400 \
    "-X POST ${BASE_URL}/answer -H 'Content-Type: application/json' -H 'X-API-Key: ${API_KEY}' -d '{\"channel_id\":\"abc\",\"root_id\":\"xyz\"}'"

echo "📍 Test 6: GET /get_thread missing root_id"
test_endpoint "POST /get_thread without root_id" 400 \
    "-X POST ${BASE_URL}/get_thread -H 'Content-Type: application/json' -H 'X-API-Key: ${API_KEY}' -d '{}'"

echo "📍 Test 7: GET /get_thread with invalid order"
test_endpoint "POST /get_thread with invalid order" 400 \
    "-X POST ${BASE_URL}/get_thread -H 'Content-Type: application/json' -H 'X-API-Key: ${API_KEY}' -d '{\"root_id\":\"test\",\"order\":\"invalid\"}'"

echo "================================================"
echo -e "Results: ${GREEN}${PASSED} passed${NC}, ${RED}${FAILED} failed${NC}"
echo ""

if [ "$FAILED" -eq 0 ]; then
    echo -e "${GREEN}✓ All validation tests passed!${NC}"
    echo ""
    echo "Note: Tests requiring real Mattermost data (valid channel_id, root_id, etc.)"
    echo "will return 502/404 errors from Mattermost API. This is expected."
    exit 0
else
    echo -e "${RED}✗ Some tests failed${NC}"
    exit 1
fi

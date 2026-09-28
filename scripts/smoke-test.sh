#!/usr/bin/env bash
set -euo pipefail
API="${API:-http://localhost:8080/api}"

echo "1) Login as customer"
LOGIN=$(curl -fsS -H 'Content-Type: application/json' -d '{"email":"customer@demo.local","password":"Customer123!"}' "$API/auth/login")
TOKEN=$(printf '%s' "$LOGIN" | python3 -c 'import json,sys; print(json.load(sys.stdin)["token"])')
AUTH=(-H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json')

echo "2) Read plans and current usage"
curl -fsS "${AUTH[@]}" "$API/plans" >/dev/null
curl -fsS "${AUTH[@]}" "$API/usage/current" >/dev/null

echo "3) Record usage"
EVENT="smoke-$(date +%s)-$RANDOM"
curl -fsS "${AUTH[@]}" -d "{\"externalEventId\":\"$EVENT\",\"channel\":\"WhatsApp\",\"eventType\":\"Message\",\"quantity\":10,\"timestamp\":\"$(date -u +%Y-%m-%dT%H:%M:%SZ)\"}" "$API/usage/events" >/dev/null

echo "4) Generate invoice"
INVOICE=$(curl -fsS "${AUTH[@]}" -X POST "$API/invoices/generate-current")
INVOICE_ID=$(printf '%s' "$INVOICE" | python3 -c 'import json,sys; print(json.load(sys.stdin)["id"])')

echo "5) Pay invoice idempotently"
KEY="smoke-payment-$RANDOM-$(date +%s)"
PAY1=$(curl -fsS "${AUTH[@]}" -H "Idempotency-Key: $KEY" -d '{"simulateFailure":false}' "$API/invoices/$INVOICE_ID/pay")
PAY2=$(curl -fsS "${AUTH[@]}" -H "Idempotency-Key: $KEY" -d '{"simulateFailure":false}' "$API/invoices/$INVOICE_ID/pay")
ID1=$(printf '%s' "$PAY1" | python3 -c 'import json,sys; print(json.load(sys.stdin)["id"])')
ID2=$(printf '%s' "$PAY2" | python3 -c 'import json,sys; print(json.load(sys.stdin)["id"])')
[[ "$ID1" == "$ID2" ]] || { echo "Idempotency failed"; exit 1; }

echo "Smoke test passed. Payment $ID1 was not duplicated."

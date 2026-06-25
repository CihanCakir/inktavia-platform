# 05 — Add Test Scripts and Variable Capture

Add basic Postman tests to every request.

For login/refresh requests, capture tokens defensively from possible Aizen envelope shapes.

For create/list/detail endpoints, capture useful sample variables only when the response contains IDs and the field names are clear.

Never fail a request only because seed data is missing. Prefer non-500 and valid JSON tests.

# 08 — Fix Dashboard, Vessel, and ServiceRequest Response Errors

Fix active endpoint failures for:

```text
/dashboard/overview
/vessels
/vessels/:id/media
/vessels/:id/status-history
/service-requests
/service-requests?status=PENDING
```

Do not hide errors with fake success.

If failures are caused by the auth scheme, confirm Step 02 fixed them.

If failures are caused by missing mock data, document and coordinate with the mock data seeding package.

If failures are caused by missing BFF endpoint contracts, implement them using active module contracts.

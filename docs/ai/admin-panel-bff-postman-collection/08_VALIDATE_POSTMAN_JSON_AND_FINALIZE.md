# 08 — Validate Postman JSON and Finalize

Run:

```bash
python -m json.tool docs/postman/admin-panel-bff/Inktavia_AdminPanel_BFF.postman_collection.json > /tmp/admin_panel_bff_collection_validated.json
python -m json.tool docs/postman/admin-panel-bff/Inktavia_AdminPanel_BFF_Local.postman_environment.json > /tmp/admin_panel_bff_environment_validated.json
```

If validation fails, fix JSON and rerun.

Update final report with validation results.

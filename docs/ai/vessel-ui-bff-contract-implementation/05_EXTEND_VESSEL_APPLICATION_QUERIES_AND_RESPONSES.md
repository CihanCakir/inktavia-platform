# 05 — Extend Vessel Application Queries and Responses

Implement/extend application layer for MVP GET endpoints.

## List

Add filters:

- Search
- AssetTypes
- OwnershipStatuses
- OperationalStatuses

Return all UI list fields where available. Use null for non-critical optional fields if a safe source is unavailable.

## Detail

Return:

- base vessel fields
- spec fields
- primary media hero image
- engine
- latest location
- document summaries

## Documents

Return document versions ordered by latest first.

## Media

Return media fields and apply mediaType filter.

Important: Avoid `IPaginate<T>` in DTOs crossing HTTP/Refit boundaries. Use concrete `Paginate<T>` or BFF-owned page DTO.

Update `docs/reports/vessel-module-application-query-command-report.md`.

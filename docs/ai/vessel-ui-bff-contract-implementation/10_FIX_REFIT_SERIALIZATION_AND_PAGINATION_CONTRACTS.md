# 10 — Fix Refit Serialization and Pagination Contracts

Audit all response DTOs used by AdminPanel BFF remote clients.

Fix patterns that break `System.Text.Json` / Refit:

- `IPaginate<T>` response properties
- interface-typed response bodies
- abstract DTO properties
- non-public setters without supported constructors

Preferred options:

1. Use concrete `MiniUow.Paging.Paginate<T>` in module HTTP response contracts, or
2. Map to BFF-owned `PageBffDto<T>` before returning to frontend.

Do not break existing module internal query handler return types unless needed. If handler returns `IPaginate<T>` internally, cast/map safely before HTTP response.

Document all serialization changes in the application/query report.

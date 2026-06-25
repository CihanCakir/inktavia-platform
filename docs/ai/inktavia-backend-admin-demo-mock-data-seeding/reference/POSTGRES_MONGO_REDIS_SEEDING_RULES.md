# PostgreSQL, MongoDB and Redis Seeding Rules

## PostgreSQL

If the module uses EF Core/PostgreSQL:

- Use the real module DbContext.
- Respect schema names and table mappings.
- Respect base entity audit fields.
- Use UTC timestamps.
- Do not bypass aggregate invariants if the repository has factory methods or constructors.
- Do not violate required indexes/unique constraints.

## MongoDB

If the module uses Mongo documents/read models:

- Inspect document classes and Mongo context.
- Seed documents only if the module already owns those documents.
- Keep document IDs consistent with PostgreSQL aggregate IDs where the module expects it.
- Do not create fake read models if the module does not use Mongo for mock/demo state.

## Redis

Mock data seeding should generally not seed Redis business state unless existing module conventions require it.

Redis may be used for:

- cache invalidation after seed
- marking seed completion only if existing convention supports it

Do not put source mock data only in Redis. JSON files must be the source of truth.

## Cache invalidation

After seeding write-side data, invalidate relevant query caches if the module has cacheable queries.

Use existing cache invalidation conventions.

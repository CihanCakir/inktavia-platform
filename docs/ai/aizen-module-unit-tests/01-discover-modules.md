# 01 - Discover Modules

Do not create tests yet.

Scan:

```text
Modules/*
```

A valid module is a directory containing:

```text
src/
```

Optionally:

```text
tests/
docs/
```

For each module, identify:

- Module folder name, for example `Identity`
- API project:
  - `Modules/{Module}/src/Aizen.Modules.{Module}/Aizen.Modules.{Module}.csproj`
- Application project:
  - `Modules/{Module}/src/Aizen.Modules.{Module}.Application/Aizen.Modules.{Module}.Application.csproj`
- Domain project:
  - `Modules/{Module}/src/Aizen.Modules.{Module}.Domain/Aizen.Modules.{Module}.Domain.csproj`
- Repository project:
  - `Modules/{Module}/src/Aizen.Modules.{Module}.Repository/Aizen.Modules.{Module}.Repository.csproj`
- Abstraction project:
  - `Modules/{Module}/src/Aizen.Modules.{Module}.Abstraction/Aizen.Modules.{Module}.Abstraction.csproj`

Use actual detected paths, not assumptions only.

## Required Output

Produce:

```md
# Module Discovery Report

| Module | API Project | Application Project | Domain Project | Tests Directory |
|---|---|---|---|---|
| ... | ... | ... | ... | ... |
```

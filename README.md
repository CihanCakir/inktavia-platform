# inktavia-platform

Inktavia backend platform repository built on the Aizen modular architecture.  
This repo contains the **Core** framework, **Modules** (microservice-aligned domains), **BFF**, **Gateway**, and infrastructure assets to run and govern the platform with Gitflow, CI checks, and engineering documentation.

---

## Scope

This repository is designed to deliver:
- A production-grade backend platform (modular + microservice-friendly)
- Engineering competency outputs (Gitflow governance, clean code, security, performance, load tests, messaging, cache, realtime)

---

## Repository Root Layout

The repository follows the Aizen standard skeleton:

```
.
├─ Aizen.sln
├─ README.md
├─ docker-compose.yaml
├─ Core/
├─ Modules/
├─ Bff/
└─ Gateway/
```

### Core
`Core/` contains shared platform capabilities used by all modules (cross-cutting concerns, infra abstractions, and base building blocks).

```
Core/
├─ Api            Configuration  EFCore         IOC            Scheduler      UnitOfWork
├─ Auth           CQRS           EventStore     Logging        Security       Validation
├─ Cache          Data           InfoAccessor   Messagebus     Serialization
├─ Common         Domain         Infrastructure RemoteCall     Starter
```

### Modules
`Modules/` contains domain-aligned modules that can be operated as independent services (or as modular deployments depending on environment).

Current modules (will evolve):
```
Modules/
├─ Identity
├─ Profile
├─ Venue
├─ Activity
└─ Payment
```

Each module follows the same internal structure (standardized for repeatability and governance):

```
Modules/<ModuleName>/
├─ build/                    # Dockerfile and build artifacts
├─ deploy/                   # deployment templates/assets
├─ docs/                     # module documentation
├─ src/                      # module source projects
├─ tests/                    # test assets (postman, etc.)
└─ README.md                 # module-level README
```

### BFF and Gateway
- `Gateway/`: Edge routing layer (API gateway / reverse proxy) responsible for routing, policies, and edge concerns.
- `Bff/`: Backend-for-Frontend layer for client-specific aggregation and orchestration (mobile/web).

---

## Getting Started (Local)

> This section will be finalized as soon as the initial module skeletons and `docker-compose.yaml` are committed.

### Prerequisites
- .NET SDK 8
- Docker + Docker Compose

### Start dependencies
```bash
docker compose -f docker-compose.yaml up -d
```

### Run a module (example)
```bash
consider updating this path to the actual csproj once modules are committed
dotnet run --project Modules/Identity/src/Aizen.Modules.Identity
```

> The actual project names/paths will match the module skeleton in `Modules/<ModuleName>/src`.

---

## Engineering Governance

### Branching Model (Gitflow)
- `main`: production-ready stable branch
- `develop`: integration/staging branch
- `feature/*`: feature development branches → merge into `develop`
- `release/*`: release preparation → merge into `main` then back-merge to `develop`
- `hotfix/*`: production hotfix → merge into `main` then back-merge to `develop`

### Pull Requests and Rulesets
Protected branches (`main`, `develop`) enforce:
- PR required (no direct pushes)
- Minimum **1 approval**
- Required status checks (**build** + **test**)
- Force pushes disabled
- (Optional) stale approvals dismissed on new commits

Evidence and governance documentation will be tracked under:
- `docs/09-gitflow-code-review.md` (root governance docs, to be added)

---

## Deliverables Coverage (KIP)

This repository will produce measurable outputs aligned with the competency tasks:
- Performance optimizations (threading/DB/cache/transaction scope)
- Messaging (RabbitMQ/Kafka)
- NoSQL + Redis usage
- Database design & query/index optimization
- Microservices architecture patterns
- Gitflow governance and code review process
- Socket-based realtime workflows (e.g., attendance + feedback)
- OAuth2/OIDC implementation
- Load testing (k6/jmeter) and reporting
- Secure coding training and applied security practices

---

## Contributing

### Commit Message Convention
Use a conventional format for clarity:
- `feat: ...`
- `fix: ...`
- `chore: ...`
- `docs: ...`
- `refactor: ...`
- `test: ...`

### Guidelines
- Keep PRs small and focused
- Update docs when behavior changes
- Prefer deterministic tests and clear error handling

---

## Roadmap (Near Term)
- [ ] Initialize GitHub rulesets (main/develop), PR/issue templates, CI build+test
- [ ] Import Aizen Core and align solution structure (`Aizen.sln`, `Core/`, `Modules/`)
- [ ] Create module skeletons (Identity, Profile, Venue, Activity, Payment)
- [ ] Add infra dependencies (DB, Redis, broker) into `docker-compose.yaml`
- [ ] Add load test scripts and baseline performance report

---



# Scope and Goals

## Repository Scope
`inktavia-platform` is the backend platform repository built on the Aizen modular architecture. The repository is structured to support:
- A shared **Core** framework containing cross-cutting capabilities
- **Modules** aligned to business domains and microservice boundaries
- **Gateway** and **BFF** layers for edge routing and client-specific orchestration
- Infrastructure assets (e.g., docker-compose) to run local and non-prod environments

This repo is also used as an engineering competency deliverable to demonstrate:
- Git governance (Gitflow, rulesets, PR discipline, CI checks)
- Microservice and domain-driven structure
- Performance, security, load testing, and data architecture practices

## Target Outcomes (KIP Alignment)
The following goals map directly to the competency list and are tracked with documentation + PR evidence.

### 1) Gitflow / Governance
**Goal:** Enforce PR-only development for `main` and `develop`, with mandatory review and CI checks.
**Deliverables:**
- Rulesets for `main` and `develop`
- PR template and issue templates
- CI pipeline producing `build` and `test` checks
- Auto-assign and code owner enforcement
- Auto-merge for `develop` (controlled)

**Evidence location:**
- `docs/01-git-governance/*`
- `docs/01-git-governance/05-proof-log.md`

### 2) Microservice Architecture
**Goal:** Run domain modules as independent deployable units (or modular monolith in early phase) while preserving service boundaries.
**Deliverables:**
- Service boundary definition per domain
- Standard module layout (`build/ deploy/ docs/ src/ tests/`)
- Gateway + BFF responsibilities clarified

**Evidence location:**
- `docs/02-microservices/*`
- Module READMEs under `Modules/<Module>/README.md`

### 3) Database Design & Optimization
**Goal:** Apply correct data ownership per service and implement normalization, indexes, query patterns, and performance-safe migrations.
**Deliverables:**
- Data ownership matrix (service → tables/collections)
- Index strategy notes and query optimization records
- Persistence patterns per domain (EF Core / JSONB / etc.)

**Evidence location:**
- `docs/04-database/*`

### 4) NoSQL + Redis
**Goal:** Use Redis for caching / rate limiting / idempotency and integrate at least one NoSQL use case where appropriate.
**Deliverables:**
- Redis caching policy and invalidation strategy
- NoSQL use case definition (read model / event store / document store etc.)

**Evidence location:**
- `docs/06-nosql-redis/*`

### 5) RabbitMQ / Kafka (Messaging)
**Goal:** Demonstrate event-driven integration using a broker (Kafka or RabbitMQ), with clear contracts and consumer semantics.
**Deliverables:**
- Message contracts and topic/queue naming standard
- Producer/consumer example inside at least one module
- Retry/poison/dead-letter strategy

**Evidence location:**
- `docs/05-messaging/*`

### 6) Performance Optimizations
**Goal:** Apply performance improvements across thread usage, DB usage, cache, transaction scopes, and IO patterns.
**Deliverables:**
- Baseline vs improved metrics
- Identified bottlenecks and mitigation steps
- Profiling notes and technical decisions

**Evidence location:**
- `docs/03-performance/*`

### 7) OAuth 2.0 / OpenID Connect
**Goal:** Provide a standards-aligned identity implementation and document flows.
**Deliverables:**
- OAuth2/OIDC flow selection and reasoning
- Token strategy (access/refresh), device sessions, revocation policy
- Security notes (claims, scopes, consent if applicable)

**Evidence location:**
- `docs/02-microservices/01-identity.md`
- Module docs: `Modules/Identity/docs/*`

### 8) Socket / Realtime
**Goal:** Implement at least one realtime use case (e.g., attendance, status updates, notifications).
**Deliverables:**
- Socket protocol choice (WebSocket/SignalR)
- Connection/auth strategy
- Domain use case mapping and event propagation

**Evidence location:**
- `docs/02-microservices/*` (realtime integration section)
- Module docs under Activity/Notification/Realtime

### 9) Load Testing
**Goal:** Provide reproducible load tests (k6/JMeter) and document results.
**Deliverables:**
- Load test scripts
- Test scenario definition (RPS, concurrency, duration)
- Result report and capacity notes

**Evidence location:**
- `docs/08-load-test/*`

### 10) Application Security
**Goal:** Demonstrate secure coding practices and document applied controls.
**Deliverables:**
- Secure coding checklist
- Implemented controls (input validation, authZ, secrets, headers, etc.)
- Training completion reference (as applicable)

**Evidence location:**
- `docs/07-security/*`

## Definition of Done (Documentation Standard)
Each major deliverable is considered complete when:
- Documentation exists describing **what** was implemented and **why**
- Implementation exists in code (or configuration)
- Evidence exists as PR links in `docs/01-git-governance/05-proof-log.md` and/or module docs

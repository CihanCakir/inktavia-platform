# High-Level Architecture

## Overview
`inktavia-platform` uses an Aizen-based modular architecture designed to support:
- Clear domain boundaries
- Microservice-friendly deployment
- Shared cross-cutting capabilities through `Core/`
- Controlled edge routing and client orchestration via `Gateway/` and `Bff/`

## Repository Structure
Root layout:

```
.
├─ Aizen.sln
├─ Core/
├─ Modules/
├─ Gateway/
├─ Bff/
└─ docker-compose.yaml
```

### Core
`Core/` contains reusable platform capabilities that every module can depend on, such as:
- API primitives and configuration
- CQRS base constructs
- Domain abstractions and shared kernel
- EF Core setup and UnitOfWork patterns
- Logging, security, validation
- Cache and message bus abstractions
- Serialization, remote calls, scheduler, event store

**Key principle:** Modules depend on Core; Core does not depend on Modules.

### Modules
`Modules/` contains domain-aligned modules that can be operated as:
- Independent microservices (preferred end state), or
- Modular deployments in early environments

Modules follow a standardized internal layout:

```
Modules/<ModuleName>/
├─ build/
├─ deploy/
├─ docs/
├─ src/
├─ tests/
└─ README.md
```

**Module examples (target):**
- Identity
- Profile
- Venue
- Activity
- Payment
- Notification (optional depending on roadmap)

### Gateway
`Gateway/` is responsible for edge-level concerns:
- Routing to services
- Policy enforcement at the edge (rate limiting, basic security headers, etc.)
- API composition boundaries (as needed)

### BFF
`Bff/` provides client-specific orchestration:
- Mobile/web tailored endpoints
- Aggregation of multiple module responses
- DTO shaping without leaking internal domain models

## Architecture Principles

### 1) Domain Ownership
Each domain/module owns its data and business rules. Cross-domain interaction is explicit via:
- API calls (Gateway/BFF routing)
- Messaging (Kafka/RabbitMQ) for asynchronous workflows

### 2) CQRS Discipline
Where applicable:
- Commands modify state
- Queries read optimized projections/read models
- Validation is standardized in Core

### 3) Secure by Default
Security is not “added later”; it is built into:
- Identity and token handling
- Authorization policies and claims/scopes
- Input validation and consistent error handling
- Secrets management for deployments

### 4) Observability
All modules should emit consistent logs/metrics:
- Structured logging (Core/Logging)
- Correlation IDs and trace propagation
- Error classification and alerting readiness

### 5) Performance and Scalability
Scaling is achieved through:
- Caching (Redis) for hot paths
- Efficient DB access patterns and indexes
- Async IO and controlled transaction scopes
- Load tests and capacity planning

## Interaction Diagram (Conceptual)
Client → Gateway → (BFF) → Modules  
Modules ↔ DB (per module ownership)  
Modules ↔ Redis (cache/idempotency)  
Modules ↔ Broker (Kafka/RabbitMQ) for async events

Notes:
- BFF is optional per endpoint; some endpoints may route directly Gateway → Module.
- Messaging is preferred for cross-domain eventual consistency workflows.

## Technology Baseline (Current)
- Runtime: .NET 9
- Architecture: Aizen modular platform
- CI: GitHub Actions (build + test)
- Governance: Rulesets for main/develop, code owner reviews, auto-merge (develop only)

## Documentation Map
- Governance: `docs/01-git-governance/*`
- Service boundaries: `docs/02-microservices/*`
- Non-functional: performance/security/load test plans under `docs/03+`

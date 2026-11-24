# AI Working Journal — APAC Claims & Exposure Platform

## 1. Service Decomposition & Architecture
- **Prompt / Goal:** Decompose the multi-market APAC motor/property claims workflow into services, communication patterns, and data models within a 3-hour sprint window.
- **AI Recommendation:** Suggested separating Claims Processing, Notifications, and Exposure Analytics into independent microservices backed by PostgreSQL and Kafka.
- **Challenge & Override:** Overrode full microservices in favor of a **Modular Monolith** in .NET 8 with clear domain boundaries (`Domain`, `Features`, `Infrastructure`). Distributed deployment overhead would eat into implementation and testing time while adding zero functional value for an initial platform build, whereas a modular monolith preserves clean boundaries for future extraction.

## 2. Asynchronous Messaging & Local Infrastructure Trade-off
- **Prompt / Goal:** Decide what operations require synchronous REST vs. asynchronous event-driven processing, and configure local infrastructure.
- **AI Recommendation:** Generated a Docker Compose setup using PostgreSQL and Redpanda (Kafka), alongside `Confluent.Kafka` producers/consumers.
- **Challenge & Override:** On Windows, Docker Desktop required enabling Virtual Machine Platform and a full OS reboot during the sprint window. I overrode the runtime dependency by introducing an `IClaimEventPublisher` abstraction backed by an in-process `System.Threading.Channels` async worker (`InMemoryKafkaChannelBus`) and SQLite persistence, while retaining `docker-compose.yml` for containerized Kafka/Postgres deployment. This kept local startup zero-dependency (`dotnet run`) while preserving true async event-driven read-model updates (`ExposureSummary`).

## 3. Domain State Machine Enforcement
- **Prompt / Goal:** Implement API endpoints for claim lifecycle transitions (submit, assign, request info, provide info, settle, reject).
- **AI Recommendation:** Initial controller scaffolding exposed status updates via generic DTO property assignments.
- **Challenge & Override:** Challenged this because generic status updates allow illegal transitions (e.g., settling an already-rejected claim or submitting unsolicited info responses). Refactored all transitions into explicit domain methods on `Claim.cs` (`AssignOfficer`, `RequestAdditionalInfo`, `ProvideAdditionalInfo`, `Settle`, `Reject`) with guard clauses and automatic immutable `ClaimAuditEntry` creation, verified via xUnit tests.

## 4. Angular 21 Standalone Architecture & Dual-Persona UX
- **Prompt / Goal:** Build a single Angular application serving both Claimants and Claims Staff.
- **AI Recommendation:** Suggested building full JWT authentication, multi-page routing, and separate state management libraries.
- **Challenge & Override:** Scoped out OAuth/JWT boilerplate in favor of an instant top-bar **Persona Switcher** (`Claimant Portal` vs. `Claims Staff & Exposure`) and officer selector, allowing reviewers to test end-to-end interactions (e.g., officer requests info -> claimant replies -> officer settles -> exposure dashboard updates asynchronously) in seconds. Also adapted AI-generated `app.component.*` filenames to Angular 21's `app.*` convention.
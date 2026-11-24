# APAC Regional Claims & Liability Exposure Platform

A full-stack claims management and real-time liability exposure platform built with **.NET 8 (C#)** and **Angular 21**, supporting Motor and Property claims across six APAC markets (`SG`, `MY`, `HK`, `TH`, `AU`, `JP`).

## Architectural Highlights

1. **Modular Monolith (.NET 8):**
   - `Domain/`: Encapsulates core business invariants and state-machine transitions inside the `Claim` aggregate root, guaranteeing an immutable `ClaimAuditEntry` timeline for every action.
   - `Features/`: Vertical-slice REST controllers (`ClaimsController`, `DashboardController`) for synchronous commands and queries.
   - `Infrastructure/`: EF Core persistence (SQLite for zero-config local execution) and an asynchronous event bus (`IClaimEventPublisher` + `ExposureConsumerWorker`).
2. **Sync (REST) vs. Async (Event-Driven) Split:**
   - **REST/HTTP:** Handles user commands requiring immediate validation feedback (submitting claims, requesting/providing info, settling/rejecting).
   - **Event-Driven Consumer:** State transitions publish a `ClaimDomainEvent` asynchronously. The `ExposureConsumerWorker` consumes these events to recalculate the `ExposureSummary` CQRS read model by Market and Product type without slowing down transactional writes.
3. **Unified Angular Frontend:**
   - Single-page application with an instant **Active Persona Switcher** (`Claimant Portal` and `Claims Staff & Exposure`) to demonstrate end-to-end workflows seamlessly.

## How to Run Locally

### 1. Start the Backend API (.NET 8)
```bash
cd backend/ClaimsPlatform.Api
dotnet run
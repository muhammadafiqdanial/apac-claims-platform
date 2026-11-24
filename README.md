# APAC Regional Claims & Liability Exposure Platform

A full-stack claims management and real-time liability exposure platform built with **.NET 8 (C#)** and **Angular**, supporting Motor and Property claims across six APAC markets (`SG`, `MY`, `HK`, `TH`, `AU`, `JP`).

## Architectural Highlights

1. **Modular Monolith (.NET 8):**
   - `Domain/`: Encapsulates core business invariants and state-machine transitions inside the `Claim` aggregate root, guaranteeing an immutable `ClaimAuditEntry` timeline for every action.
   - `Features/`: Vertical-slice REST controllers (`ClaimsController`, `DashboardController`) for synchronous commands and queries.
   - `Infrastructure/`: EF Core persistence (SQLite for zero-config local execution) and an asynchronous event bus (`IClaimEventPublisher` + `ExposureConsumerWorker`).
2. **Sync (REST) vs. Async (Event-Driven) Split:**
   - **REST/HTTP:** Handles user commands requiring immediate validation feedback (submitting claims, requesting/providing info, settling/rejecting).
   - **Event-Driven Consumer:** State transitions publish a `ClaimDomainEvent` asynchronously. The `ExposureConsumerWorker` consumes these events to recalculate the `ExposureSummary` CQRS read model by Market and Product type without slowing down transactional writes.
3. **Unified Angular Frontend & Multi-Currency Reporting:**
   - Single-page application with an instant **Active Persona Switcher** (`Claimant Portal` and `Claims Staff & Exposure`) to demonstrate end-to-end workflows seamlessly.
   - Includes a top-bar **Reporting Currency Switcher** (`MYR`, `USD`, `SGD`, `HKD`, `THB`, `AUD`, `JPY`) that converts normalized base liability figures dynamically for regional managers.
4. **Production Readiness (`docker-compose.yml`):**
   - Includes a `docker-compose.yml` reference blueprint demonstrating how `IClaimEventPublisher` and EF Core scale out to Redpanda/Kafka and PostgreSQL in production without changing domain logic.

---

## How to Run Locally

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+ & npm](https://nodejs.org/)

### 1. Start the Backend API (.NET 8)
```bash
cd backend/ClaimsPlatform.Api
dotnet run
```
- The API runs on **`http://localhost:5102`**.
- Automatically initializes the local SQLite database (`claims.db`) with sample APAC claims and starts the background `ExposureConsumerWorker`.

### 2. Start the Angular Frontend
Open a second terminal:
```bash
cd frontend
npm install
npm start
```
- Open **`http://localhost:4200`** in your browser.
- Use the top navigation bar to switch between **Claimant Portal** and **Claims Staff & Exposure**, or change the **Currency** (`MYR`, `USD`, `SGD`, etc.).

### 3. Run Automated Unit Tests
```bash
dotnet test backend/ClaimsPlatform.Tests
```
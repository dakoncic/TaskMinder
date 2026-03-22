# TaskMinder

TaskMinder is a full-stack task planning application built with ASP.NET Core, EF Core, and Angular. The core design challenge is modeling the difference between recurring task templates and concrete task occurrences while supporting weekly scheduling, completion flows, reorder logic, and validation-heavy API interactions.

I use this project as a portfolio example for layered architecture, domain-driven business rules, backend testing, and typed frontend integration.

## What This Project Demonstrates

- Layered backend architecture across API, application/domain, and persistence projects.
- A domain model that separates task templates from task occurrences.
- Business rules for recurring tasks, due dates, committed scheduling, and row ordering.
- Backend testing for service behavior, API validation, and error handling.
- Standardized RFC 7807 ProblemDetails responses with trace identifiers.
- Angular integration with a generated API client and environment-specific builds.

## Domain Model

The key design decision is splitting task data into two concepts:

- `TaskTemplate`: the durable definition of a task, including whether it is recurring, its recurrence interval, and its ordering inside a group.
- `TaskOccurrence`: a concrete instance of work that can be scheduled, committed to a day, completed, or regenerated from a recurring template.

That split avoids treating recurring work as simple CRUD. It allows the service layer to handle distinct flows such as:

- completing a one-time task and removing it from active ordering
- completing a recurring task and generating the next occurrence
- moving overdue committed tasks into the current business window
- reordering tasks inside backlog and scheduled groups

## Architecture

The solution is split into focused projects:

- `MyFeatures`: ASP.NET Core API, controllers, DTOs, validation, middleware, and application composition.
- `Core`: domain models, business services, recurrence logic, ordering rules, and service contracts.
- `Infrastructure`: EF Core entities, database context, repositories, unit of work, and migrations.
- `MyFeaturesUI`: Angular frontend with generated API client integration.
- `Core.Tests`: unit and integration tests covering backend behavior.

## Key Engineering Decisions

- `TimeProvider` is injected into the service layer so time-based rules remain testable.
- FluentValidation is used for request validation and returns consistent problem responses.
- Global exception handling is implemented with middleware that emits `application/problem+json` payloads.
- Mapster is used to map between DTOs, domain models, and persistence entities.
- EF Core migrations are tracked in source control and applied through the application startup path for local environments.

## Testing Approach

The backend test suite covers behavior that matters to the domain, not only object creation.

- Unit tests verify recurrence, completion, scheduling, and time-sensitive behavior in `TaskTemplateService`.
- Integration tests verify validation failures, not-found behavior, and unhandled exception responses through the HTTP layer.
- Validation tests cover DTO rules and nested request structures.

## Stack

- .NET 8 / ASP.NET Core Web API
- EF Core 8
- Angular 17
- FluentValidation
- Mapster
- Serilog
- xUnit
- Moq
- EF Core InMemory

## Companion Portfolio Pieces

This repository focuses on application architecture and domain behavior. Related portfolio work covers:

- authentication and authorization flows with Firebase
- CI/CD pipelines
- Docker-based packaging
- deployment automation

## Running The Project

### Backend

```bash
dotnet build .\MyFeatures.sln
dotnet test Core.Tests\Core.Tests.csproj -c Release
```

### Frontend

```bash
cd MyFeaturesUI
npm run build
```



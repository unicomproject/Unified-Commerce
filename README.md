# E_POS Backend

Clean Architecture backend scaffold for E_POS MVP.

## Architecture

```text
API -> Application -> Domain
Infrastructure -> Application + Domain
```

The backend uses service/use-case orchestration with repository contracts. It does not use CQRS, MediatR, Redis, or event sourcing.

## Projects

```text
src/
  E_POS.Api/
  E_POS.Application/
  E_POS.Domain/
  E_POS.Infrastructure/
tests/
  E_POS.UnitTests/
  E_POS.IntegrationTests/
  E_POS.ApiTests/
```

## Rules

- Keep controllers thin.
- Put workflow logic in Application services/use cases.
- Keep Domain free of EF Core, HTTP, and infrastructure dependencies.
- Implement repository contracts in Infrastructure.
- Use DTOs for API input/output.
- Enforce tenant isolation, feature entitlements, permissions, audit, and idempotency in protected workflows.
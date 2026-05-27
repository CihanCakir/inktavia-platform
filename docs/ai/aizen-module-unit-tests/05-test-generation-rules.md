# 05 - Test Generation Rules

## API Controller Tests

For source:

```text
Modules/{Module}/src/Aizen.Modules.{Module}/Controller/**/*.cs
Modules/{Module}/src/Aizen.Modules.{Module}/Controllers/**/*.cs
```

create tests under:

```text
Modules/{Module}/tests/Aizen.Modules.{Module}.Api.UnitTests/Controller/...
```

Test:

- route/action method behavior where possible
- validation behavior when controller has explicit model validation handling
- success response wrapping if AizenApiResponse is used
- authorization attributes only if they are present and testable without integration setup
- command/query dispatch calls through fake mediator/CQRS processor if used

Avoid full HTTP integration tests unless specifically requested.

## Application Command/Query Handler Tests

For source:

```text
Modules/{Module}/src/Aizen.Modules.{Module}.Application/**/Command/**/*Handler.cs
Modules/{Module}/src/Aizen.Modules.{Module}.Application/**/Query/**/*Handler.cs
```

create tests under:

```text
Modules/{Module}/tests/Aizen.Modules.{Module}.Application.UnitTests/Command/...
Modules/{Module}/tests/Aizen.Modules.{Module}.Application.UnitTests/Query/...
```

Test:

- happy path
- not found / validation / business exception path
- repository interaction through hand-written fakes where possible
- AizenBusinessException error code checks
- no null reference for InfoAccessor-driven user context
- cancellation token usage where meaningful

## Validators

If FluentValidation validators exist, create validator tests.

Test:

- required fields
- min/max length
- compare rules
- enum range
- invalid id values

## Domain Tests

For source:

```text
Modules/{Module}/src/Aizen.Modules.{Module}.Domain/**/*.cs
```

create tests under:

```text
Modules/{Module}/tests/Aizen.Modules.{Module}.Domain.UnitTests/...
```

Test:

- factory/create methods
- domain methods
- guard clauses
- state transitions
- entity invariants
- value object equality
- domain exceptions

## Naming

Use test class names:

```text
{SourceClassName}Tests
```

Use test method names:

```text
MethodName_Should_ExpectedBehavior_When_Condition
```

Example:

```csharp
ChangePassword_Should_ThrowUserNotFound_When_UserInfoIsMissing()
```

## Existing Architecture

Preserve current Aizen patterns.

Do not force a new testing framework if the repository already has one.

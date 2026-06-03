# Request and Response Generation Rules

## Request generation

Every endpoint request must be generated from actual code-level contracts.

Inspect in order:

1. Controller action signature.
2. Attributes: `[FromBody]`, `[FromQuery]`, `[FromRoute]`, `[FromForm]`.
3. Request DTO class.
4. Command/query class if mapped directly.
5. FluentValidation validators.
6. Enum definitions.
7. XML comments or DocumentationInfo.

Never leave `{}` unless the endpoint truly has no input.

## Response generation

Every endpoint response expectation must be generated from actual code-level contracts.

Inspect in order:

1. Controller action return type.
2. ProducesResponseType attributes.
3. Application handler return type.
4. Response DTO.
5. Aizen/Metropol response wrapper.
6. Existing captured examples.

If a response cannot be inferred, mark it as `needs manual response example`.

## Test generation

Every request should include tests for:

- Status is not 500.
- Expected status code.
- JSON structure when applicable.
- `header.isSuccess` for wrapped responses.
- `body` presence.
- id extraction when relevant.

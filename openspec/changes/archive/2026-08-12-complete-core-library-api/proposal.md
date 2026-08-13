## Why

The library API compiles but does not satisfy its documented core contract: book creation and library deletion are unavailable, missing parent libraries are not distinguished from empty collections, and library updates can target an ID different from the route. Completing and clarifying this behavior provides a stable API baseline before authentication, infrastructure, or frontend work proceeds.

## What Changes

- Define the supported library and nested-book HTTP contract, including success and not-found responses.
- Add book creation under an existing library and return `201 Created` on success.
- Return `404 Not Found` when listing or creating books for a library that does not exist.
- Add library deletion and rely on the existing required relationship to cascade-delete its books.
- Make the route library ID authoritative during updates so a conflicting request-body ID cannot select another resource.
- Return `201 Created` when creating a library.
- Use database-generated IDs for newly created libraries and books; client-supplied IDs do not choose the persisted resource ID.
- Remove or narrow unused book service operations that are outside the supported API contract.
- Add or update integration coverage for all behavior changed by this proposal.
- Keep current endpoint authentication behavior unchanged; authorization redesign is a separate change.

## Capabilities

### New Capabilities

- `library-catalog-api`: Defines CRUD behavior for libraries and creation/listing behavior for books nested under a library.

### Modified Capabilities

None. The main specification store has no existing capabilities.

## Impact

- Affects the library and book controllers, their service interfaces and implementations, EF Core interactions, and HTTP integration tests.
- Changes successful library creation from `200 OK` to `201 Created` and prevents body IDs from selecting the update target.
- Preserves the existing PostgreSQL schema and `Library` to `Book` cascade-delete relationship; no migration is expected.
- Does not redesign JWT authentication, PostgreSQL configuration, DTO architecture, operational middleware, or add a frontend.

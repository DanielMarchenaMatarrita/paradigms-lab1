## Context

See `proposal.md` for motivation. The application is a single ASP.NET Core project using controllers, thin service interfaces, EF Core, and a required `Book.LibraryId` relationship configured for cascade deletion. Core reads and library creation/update already use this path, while book creation and library deletion are unfinished. The active integration tests host the real application with SQLite in memory, but they reflect the original unauthenticated book contract while the current book GET action requires JWT.

## Goals / Non-Goals

**Goals:**

- Complete the specified behavior using the existing controller-service-DbContext architecture.
- Keep parent existence checks and persistence mutations explicit and testable.
- Ensure route identifiers, rather than request-body identifiers, select parent and updated resources.
- Exercise the changed behavior through HTTP integration tests with isolated relational state.

**Non-Goals:**

- Introducing repositories, a mediator, a separate domain layer, or a broad DTO redesign.
- Redesigning JWT policies, credentials, roles, or token issuance.
- Changing the database schema or migration strategy.
- Modernizing all test dependencies, configuration, namespaces, warnings, or documentation beyond what is required to verify this contract.
- Implementing book update or book deletion endpoints.

## Decisions

### Keep the existing controller-service-EF Core flow

Controllers will remain responsible for translating resource existence and service results into HTTP responses. Services will remain responsible for EF Core queries and mutations. This is the smallest change consistent with the current architecture and avoids adding a repository abstraction over `DbContext`.

Alternative considered: introduce a repository or richer domain layer. Rejected because current behavior contains no domain complexity that justifies another abstraction, and it would broaden this change into an architectural rewrite.

### Check parent existence before nested-book operations

The book controller will query the library service before listing or creating books. A missing parent returns `404`; an existing parent continues to the book service. This preserves the important distinction between a nonexistent library and a library with zero books.

Alternative considered: infer parent existence from the book query result. Rejected because an empty result cannot distinguish the two states. A service-level composite result was also considered but would unnecessarily change service contracts for this small application.

### Make route IDs authoritative at the HTTP boundary

Before persistence, controllers will overwrite or otherwise disregard client-supplied entity IDs that conflict with route semantics. Library updates use `libraryId` from the route, and book creation uses the route library ID while allowing the database to generate the book ID. Library creation likewise allows the database to generate its ID.

Alternative considered: reject conflicting IDs with `400 Bad Request`. Rejected because the selected contract favors route authority and compatibility with existing entity-shaped request bodies. A future DTO-focused change can prohibit IDs in create requests entirely.

### Use existing EF Core cascade deletion

Library deletion will load the target library, remove it through the service, and save once. The existing required foreign key and cascade behavior will remove dependent books in the same database operation. No explicit per-book deletion loop or schema migration is needed.

Alternative considered: explicitly delete books before deleting the library. Rejected because it duplicates configured relational behavior and creates additional queries and failure points.

### Return created resources using existing routes

Library creation will return `201 Created` with a location for the library GET route. Book creation will return `201 Created` and the created book; because there is no single-book GET endpoint in scope, it need not invent one solely to generate a location.

Alternative considered: add `GET /api/libraries/{libraryId}/books/{bookId}`. Rejected because that expands the externally supported API beyond the requested core contract.

### Preserve current production authorization behavior and adapt tests narrowly

This change will not add or remove authorization attributes. Integration tests that exercise the currently protected book-list endpoint will authenticate through the existing login flow and send the resulting Bearer token. Book creation will retain whatever authorization state its new action explicitly adopts from the current controller policy; because the controller has no class-level policy, the new action remains anonymous unless a separate authentication change specifies otherwise.

Alternative considered: override authentication in tests or remove authorization from book GET. Rejected because either would hide production behavior or alter security policy within a CRUD-focused change.

### Narrow service interfaces to supported behavior when safe

Unimplemented book update/delete operations that have no controller or consumer may be removed from the interface and implementation. This avoids leaving runtime traps that imply unsupported capabilities. The decision is contingent on confirming no external project references them during implementation.

Alternative considered: implement all declared service methods. Rejected because no API requirements exercise book update/delete and doing so would add unrequested behavior.

## Risks / Trade-offs

- [Nested operations perform a separate parent lookup before the book query or insert] -> Accept the additional query for clear semantics; optimize only if profiling later identifies a problem.
- [Entity-shaped request bodies still expose persistence fields] -> Enforce route and server authority now; move to dedicated request DTOs in the planned API-boundary change.
- [SQLite tests do not prove PostgreSQL-specific cascade behavior] -> Assert cascade behavior through relational integration tests now and retain the existing PostgreSQL migration as the production source of truth; provider-specific tests can follow in the configuration/test modernization change.
- [Preserving mixed authorization means adjacent book endpoints may have different access rules] -> Document this as unchanged policy and resolve it in the dedicated authentication proposal rather than silently expanding this change.
- [Returning `201` for library creation changes existing successful response behavior] -> Cover the intended status and response through integration tests and call it out in the proposal.

## Migration Plan

1. Update service contracts and persistence methods without changing the schema.
2. Add and correct controller actions and response mappings.
3. Update integration tests to authenticate where current production behavior requires it and verify all new scenarios.
4. Build and execute the test suite before deployment.
5. Deploy the application normally; existing data remains compatible and no database migration is required.

Rollback consists of reverting the application deployment. No data transformation or schema rollback is necessary.

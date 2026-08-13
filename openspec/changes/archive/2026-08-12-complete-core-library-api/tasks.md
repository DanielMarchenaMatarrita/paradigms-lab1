## 1. Service Persistence Behavior

- [x] 1.1 Implement book creation in `BooksService` so it persists once and returns the server-assigned book ID.
- [x] 1.2 Implement library deletion in `LibrariesService` so it removes the selected library and saves once, relying on the configured cascade for books.
- [x] 1.3 Remove unconsumed book update/delete interface members and stubs after confirming there are no callers outside the current solution.

## 2. Library HTTP Contract

- [x] 2.1 Change library creation to disregard client-supplied IDs and return `201 Created` with the persisted library and library GET location.
- [x] 2.2 Make the update route ID authoritative, returning `404` for a missing route library and preventing a conflicting body ID from selecting another library.
- [x] 2.3 Add `DELETE /api/libraries/{libraryId}` with `204 No Content` for an existing library and `404 Not Found` for a missing library.

## 3. Nested Book HTTP Contract

- [x] 3.1 Update `GET /api/libraries/{libraryId}/books` to verify the parent library and return `404` for a missing parent while preserving the current authorization requirement.
- [x] 3.2 Add `POST /api/libraries/{libraryId}/books` to verify the parent, ignore client-supplied book and parent IDs, bind to the route library, and return `201 Created` with the persisted book.
- [x] 3.3 Confirm nested book responses serialize without loading or emitting an unintended recursive library graph.

## 4. Integration Coverage

- [x] 4.1 Isolate SQLite in-memory database state between integration tests and add a helper that obtains and applies the existing JWT for protected book-list requests.
- [x] 4.2 Cover library listing, retrieval, server-ID creation, route-authoritative update, missing-resource responses, and deletion status codes.
- [x] 4.3 Cover book creation for existing and missing libraries, server and route identifier authority, populated and empty book lists, and missing-parent listing.
- [x] 4.4 Verify deleting a library removes its books and that repeated deletion and subsequent nested-book access return `404`.
- [x] 4.5 Replace blocking response-body reads touched by these tests with awaited asynchronous reads.

## 5. Verification

- [x] 5.1 Build `HackerRank1.sln` and resolve errors introduced by the change without broad unrelated warning cleanup.
- [x] 5.2 Run the canonical `LibraryService.Integration.Test` suite and confirm every `library-catalog-api` scenario is covered and passing.
- [x] 5.3 Confirm no EF Core migration is generated and review the final API surface to ensure book update/delete, authentication redesign, infrastructure modernization, and frontend work were not added.

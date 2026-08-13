## Purpose

Defines the externally observable HTTP behavior for managing libraries and the books that belong to each library.

## ADDED Requirements

### Requirement: List libraries
The system SHALL return all libraries from `GET /api/libraries` with HTTP `200 OK`.

#### Scenario: Libraries are available
- **WHEN** a client requests `GET /api/libraries` and libraries exist
- **THEN** the system returns `200 OK` with all libraries in the response body

#### Scenario: No libraries are available
- **WHEN** a client requests `GET /api/libraries` and no libraries exist
- **THEN** the system returns `200 OK` with an empty collection

### Requirement: Get a library
The system SHALL return the requested library from `GET /api/libraries/{libraryId}` when it exists and SHALL return `404 Not Found` otherwise.

#### Scenario: Library exists
- **WHEN** a client requests an existing library ID
- **THEN** the system returns `200 OK` with that library in the response body

#### Scenario: Library does not exist
- **WHEN** a client requests a library ID that does not exist
- **THEN** the system returns `404 Not Found`

### Requirement: Create a library
The system SHALL create a library from `POST /api/libraries`, assign its persistent ID on the server, and return HTTP `201 Created` with the created library.

#### Scenario: Library is created
- **WHEN** a client submits a valid library name and location
- **THEN** the system persists the library with a server-assigned ID and returns `201 Created` with the created library in the response body

#### Scenario: Client supplies a library ID
- **WHEN** a client submits a valid library representation containing an ID
- **THEN** the system ignores that ID for resource selection and persists the library using a server-assigned ID

### Requirement: Update a library by route ID
The system SHALL update the library identified by `libraryId` in `PUT /api/libraries/{libraryId}` and SHALL treat the route ID as authoritative.

#### Scenario: Library exists
- **WHEN** a client submits valid changes for an existing route library ID
- **THEN** the system updates that route-selected library and returns `204 No Content`

#### Scenario: Library does not exist
- **WHEN** a client submits changes for a route library ID that does not exist
- **THEN** the system returns `404 Not Found` and does not update any library

#### Scenario: Body ID conflicts with route ID
- **WHEN** a client submits a body ID different from an existing route library ID
- **THEN** the system updates only the route-selected library and does not modify the library identified by the body ID

### Requirement: Delete a library
The system SHALL delete the library identified by `DELETE /api/libraries/{libraryId}` when it exists and SHALL return `404 Not Found` otherwise.

#### Scenario: Library exists
- **WHEN** a client deletes an existing library ID
- **THEN** the system deletes the library and returns `204 No Content`

#### Scenario: Library does not exist
- **WHEN** a client deletes a library ID that does not exist
- **THEN** the system returns `404 Not Found`

#### Scenario: Deleted library contains books
- **WHEN** a client deletes an existing library that contains books
- **THEN** the system also deletes those books as part of the same operation

### Requirement: List books for a library
The system SHALL return the books belonging to an existing library from `GET /api/libraries/{libraryId}/books` and SHALL distinguish a missing library from an empty book collection.

#### Scenario: Library has books
- **WHEN** a client requests books for an existing library that contains books and satisfies the endpoint's current authorization requirement
- **THEN** the system returns `200 OK` with all books belonging to that library

#### Scenario: Existing library has no books
- **WHEN** a client requests books for an existing library with no books and satisfies the endpoint's current authorization requirement
- **THEN** the system returns `200 OK` with an empty collection

#### Scenario: Library does not exist
- **WHEN** a client requests books for a library ID that does not exist and satisfies the endpoint's current authorization requirement
- **THEN** the system returns `404 Not Found`

### Requirement: Create a book in a library
The system SHALL create a book through `POST /api/libraries/{libraryId}/books` only when the parent library exists, SHALL bind the book to the route library ID, and SHALL assign the book ID on the server.

#### Scenario: Parent library exists
- **WHEN** a client submits a valid book name and category for an existing library
- **THEN** the system persists the book under the route-selected library with a server-assigned ID and returns `201 Created` with the created book

#### Scenario: Parent library does not exist
- **WHEN** a client submits a book for a library ID that does not exist
- **THEN** the system returns `404 Not Found` and does not persist the book

#### Scenario: Body identifiers conflict with the route
- **WHEN** a client submits a book containing an ID or a library ID different from the route library ID
- **THEN** the system ignores those identifiers for resource selection, assigns the book ID on the server, and associates the book only with the route-selected library

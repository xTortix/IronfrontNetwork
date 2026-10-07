# Ironfront.Shared

`Ironfront.Shared` contains shared contracts used by multiple Ironfront backend projects.

This project should be referenced by services such as:

* `Ironfront.Gateway`
* `Ironfront.AuthService`
* `Ironfront.MatchmakingService`
* `Ironfront.BattleServer`

## Responsibilities

This project should contain shared data contracts only:

* DTOs
* Request models
* Response models
* Enums
* Error codes
* Service constants

## What belongs here

Examples:

```text
Contracts/LoginRequest.cs
Contracts/LoginResponse.cs
Contracts/ApiResponse.cs
Contracts/ServiceStatusDto.cs
Constants/ServiceNames.cs
Enums/MatchState.cs
Enums/TankNation.cs
```

## What does NOT belong here

Do not place actual service logic in this project.

Avoid adding:

* Database contexts
* Entity Framework migrations
* Password hashing logic
* Matchmaking logic
* Battle simulation logic
* HTTP clients
* Service implementations

## Design rule

`Ironfront.Shared` defines the common language between services.

It should not become a dumping ground for random shared code.

Good:

```text
Shared = contracts, DTOs, enums, constants
```

Bad:

```text
Shared = business logic, database logic, service logic
```

Keeping this project clean helps prevent tight coupling between backend services.

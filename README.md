# Wonga Developer Assessment

A full-stack user authentication application built with **Angular 17**, **C# .NET 8 Clean Architecture**, **PostgreSQL**, and **Docker**.

---

## Architecture

```
wonga-assessment/
├── backend/
│   ├── UserAuth.Domain/           # Entities, interfaces, domain exceptions
│   ├── UserAuth.Application/      # Use cases, DTOs, application services
│   ├── UserAuth.Infrastructure/   # EF Core, repositories, JWT, SQS publisher
│   ├── UserAuth.API/              # Controllers, middleware, Program.cs
│   ├── UserAuth.Tests/            # xUnit unit & integration tests (22 tests)
│   └── Dockerfile
├── frontend/
│   ├── src/app/
│   │   ├── guards/                # AuthGuard (route protection)
│   │   ├── interceptors/          # JWT token interceptor
│   │   ├── pages/                 # Login, Register, Profile
│   │   └── services/              # AuthService, UserService
│   ├── Dockerfile
│   └── nginx.conf
├── notification-service/          # .NET Worker Service — polls SQS, sends email
│   └── Dockerfile
├── localstack/
│   └── init/01-create-queues.sh   # Creates SQS queues on LocalStack startup
├── docker-compose.yml
├── build.sh
└── .env.example
```

---

## Tech Stack

| Layer         | Technology                              |
|---------------|-----------------------------------------|
| Frontend      | Angular 17 (Standalone Components)      |
| Backend       | C# .NET 8 — Clean Architecture          |
| Database      | PostgreSQL 16 + EF Core migrations      |
| Auth          | JWT Bearer Tokens (HS256, 60 min)       |
| Messaging     | AWS SQS via LocalStack (Docker)         |
| Email         | MailKit — Gmail SMTP                    |
| Rate Limiting | ASP.NET Core fixed-window (10 req/min)  |
| Health Checks | ASP.NET Core + EF Core DB check         |
| Logging       | Serilog — console + rolling daily files |
| Tests         | xUnit, Moq, FluentAssertions (22 tests) |
| Container     | Docker + Docker Compose (5 services)    |

---

## Quick Start (Docker)

### Prerequisites
- [Docker Desktop](https://docs.docker.com/get-started/get-docker/) installed and running

### 1. Clone & configure

```bash
git clone <your-repo-url>
cd wonga-assessment

cp .env.example .env
# Edit .env — required for welcome emails (see Email Configuration below)
```

### 2. Launch

```bash
docker compose up -d --build
```

### 3. Access

| Service           | URL                              |
|-------------------|----------------------------------|
| Frontend          | http://localhost:4200            |
| API               | http://localhost:8080            |
| Swagger UI        | http://localhost:8080/swagger    |
| Health Check      | http://localhost:8080/health     |
| Notification Svc  | http://localhost:8081/health     |
| LocalStack (SQS)  | http://localhost:4566            |
| PostgreSQL        | localhost:5432                   |

### 4. Stop

```bash
docker compose down
```

---

## Services Overview

```
[Angular Frontend] → [C# API] → [PostgreSQL]
                          ↓
                     [LocalStack SQS]
                          ↓
               [Notification Worker Service]
                          ↓
                    [Gmail SMTP]
```

When a user registers:
1. The API creates the user in PostgreSQL and returns a JWT
2. The API publishes a message to the SQS `welcome-emails` queue (via LocalStack)
3. The Notification Worker polls the queue, picks up the message, and sends a welcome email via Gmail SMTP

---

## Local Development

### Backend

**Prerequisites:** .NET 8 SDK, PostgreSQL running on port 5433

```bash
cd backend
dotnet restore
dotnet run --project UserAuth.API
# API at http://localhost:5000
# Swagger at http://localhost:5000/swagger
```

### Frontend

**Prerequisites:** Node.js 20+

```bash
cd frontend
npm install
npm start
# App at http://localhost:4200
```

---

## Running Tests

```bash
cd backend

# Run all 22 tests (unit + integration)
dotnet test

# With detailed output
dotnet test --logger "console;verbosity=normal"

# Unit tests only
dotnet test --filter "FullyQualifiedName~Unit"

# Integration tests only
dotnet test --filter "FullyQualifiedName~Integration"
```

Tests use an InMemory database — no PostgreSQL connection required.

---

## Build Script

```bash
chmod +x build.sh

# Full build: backend → tests → frontend → Docker
./build.sh

# With options
./build.sh --skip-tests     # Skip test run
./build.sh --no-cache       # Force Docker rebuild from scratch
./build.sh --up             # Start containers after build
./build.sh --help           # Show all options
```

---

## API Reference

### POST /api/auth/register

**Request:**
```json
{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com",
  "password": "Password123!"
}
```

Password must contain: 8+ characters, uppercase, lowercase, number, special character.

**Response `201 Created`:**
```json
{
  "token": "<jwt>",
  "email": "john.doe@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "expiresAt": "2024-01-01T01:00:00Z"
}
```

**Error responses:** `400 Bad Request` (validation), `409 Conflict` (email already exists)

---

### POST /api/auth/login

**Request:**
```json
{
  "email": "john.doe@example.com",
  "password": "Password123!"
}
```

**Response `200 OK`:** Same shape as register response.

**Error responses:** `401 Unauthorized` (wrong credentials), `429 Too Many Requests` (rate limit exceeded)

---

### GET /api/users/me

Requires `Authorization: Bearer <token>` header.

**Response `200 OK`:**
```json
{
  "id": 1,
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com",
  "createdAt": "2024-01-01T00:00:00Z"
}
```

**Error responses:** `401 Unauthorized` (missing or expired token)

---

### GET /health

**Response `200 OK`:**
```json
{
  "status": "Healthy",
  "checks": [{ "name": "database", "status": "Healthy" }],
  "duration": "00:00:00.012"
}
```

---

## Email Configuration

Welcome emails are sent via Gmail SMTP by the notification microservice.

1. Enable **2-Step Verification** on your Google account
2. Go to https://myaccount.google.com/apppasswords
3. Create an app password (name it anything, e.g. "Wonga")
4. Add to your `.env` file:

```env
EMAIL_FROM=your.email@gmail.com
EMAIL_USERNAME=your.email@gmail.com
EMAIL_PASSWORD="xxxx xxxx xxxx xxxx"
```

> If not configured, the notification service logs a warning and skips sending — all other features work normally.

---

## Security

- Passwords hashed with **bcrypt** (cost factor 11)
- JWT tokens expire after **60 minutes**, clock skew set to zero
- Rate limiting: **10 requests/min per IP** on auth endpoints (returns 429)
- CORS restricted to configured origins
- All inputs validated server-side with data annotations
- Domain exceptions (409, 401) never expose stack traces to clients

---

## Frontend Routes

| Route       | Description              | Protected |
|-------------|--------------------------|-----------|
| `/login`    | Login form               | No        |
| `/register` | Registration form        | No        |
| `/profile`  | Authenticated user profile | **Yes** |

Unauthenticated access to `/profile` redirects to `/login`.

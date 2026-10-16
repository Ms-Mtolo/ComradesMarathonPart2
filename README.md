# Comrades Marathon API

A RESTful Web API built with **ASP.NET Core 8**, **Entity Framework Core (Code-First)**,
and **SQL Server**. It powers the RaceDay race management system: organisers create
events and categories, participants enrol in categories, and organisers capture
finishing results.

The API is documented with **Swagger** and includes a full **xUnit** test suite that
runs automatically through **GitHub Actions** on every push.



## Table of Contents

- [Overview](#overview)
- [The Two Roles](#the-two-roles)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Setup Instructions](#setup-instructions)
- [Running the API](#running-the-api)
- [Running the Tests](#running-the-tests)
- [API Endpoints](#api-endpoints)
- [CI/CD Pipeline](#cicd-pipeline)
- [CI/CD Screenshot](#cicd-screenshot)

---

## Overview

The system manages the full lifecycle of a race event:

1. An **Organiser** registers and logs in.
2. The Organiser creates an **Event** (e.g. *Comrades 2026*) and defines one or
   more **Categories** for it (e.g. *5km*, *10km*, *Half Marathon — Men*, *Half Marathon — Women*).
3. A **Participant** registers and logs in.
4. The Participant **enrols** in a category of their choice. The system links the
   Participant, the Event, and the chosen Category together.
5. After the race, the Organiser **captures finishing times and positions** for each enrolment.
6. Participants **view their own results**; Organisers view all enrolments and results
   for the events they own.

Authentication is **session-based** (cookie sessions). Every protected endpoint checks
both the user’s identity and their role.

---

## The Two Roles

The system recognises two primary roles. A third, `Admin`, exists in the database for
future administrative functionality but is not part of the participant-facing flow.

### 1. Organiser

- Can **create, update, and delete events** they own.
- Can **define categories** (age groups or distances) for their events.
- Can **view all enrolments** for their events.
- Can **capture results** (finish time, overall position, status) for each enrolment.
- Cannot enrol in events or view results as a participant.

### 2. Participant

- Can **register and log in**.
- Can **view public events and their categories**.
- Can **enrol** in any category of any event.
- Can **view their own enrolments** and **their own results**.
- Cannot create, update, or delete events.
- Cannot view other participants’ enrolments or results.

Role-based access is enforced with `[Authorize(Roles = "Organiser")]` and
`[Authorize(Roles = "Participant")]` on the relevant controller actions. Attempts to
access a resource without the required role return **403 Forbidden**; unauthenticated
requests to protected endpoints return **401 Unauthorized**.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Language | C# 12 (.NET 8) |
| Framework | ASP.NET Core Web API |
| ORM | Entity Framework Core (Code-First) |
| Database | SQL Server / SQL Server Express |
| Authentication | Session cookies with role claims |
| Password Hashing | BCrypt.Net-Next |
| API Documentation | Swagger / Swashbuckle |
| Testing | xUnit + EF Core InMemory |
| CI/CD | GitHub Actions |

---

## Project Structure

```
ComradesMarathonAPI/
├── .github/
│   └── workflows/
│       └── ci.yml                       ← GitHub Actions workflow
├── ComradesMarathonAPI/                 ← API project
│   ├── Controllers/                     ← API endpoints
│   │   ├── AuthController.cs
│   │   ├── UsersController.cs
│   │   ├── EventsController.cs
│   │   ├── CategoriesController.cs
│   │   ├── EnrolmentsController.cs
│   │   ├── ResultsController.cs
│   │   └── PaymentController.cs
│   ├── Models/                          ← Entity classes
│   │   ├── User.cs, Role.cs, Event.cs, Category.cs
│   │   └── Enrolment.cs, Result.cs, Payment.cs
│   ├── DTOs/
│   │   └── Dtos.cs                      ← Request/response shapes
│   ├── Data/
│   │   └── AppDbContext.cs              ← EF Core DbContext
│   ├── Services/
│   │   └── PasswordService.cs           ← BCrypt hashing helper
│   ├── Migrations/                      ← EF Core migrations
│   ├── Program.cs                       ← App startup
│   └── appsettings.json
├── ComradesMarathonAPI.Tests/           ← xUnit test project
│   ├── ComradesMarathonAPI.Tests.csproj
│   └── EventsControllerTests.cs
├── ComradesMarathonAPI.slnx
└── README.md
```

---

## Prerequisites

Before you can run this project, make sure you have:

- **Visual Studio 2022** (17.8 or later) with the **ASP.NET and web development** workload
- **.NET 8 SDK** — download from https://dotnet.microsoft.com/download/dotnet/8.0
- **SQL Server Express** (or a full SQL Server instance)
  - Download: https://www.microsoft.com/en-us/sql-server/sql-server-downloads
  - Or use **LocalDB** that ships with Visual Studio
- **SQL Server Management Studio (SSMS)** — optional but recommended for inspecting the database
- **Git** — https://git-scm.com/downloads

To confirm .NET is installed:

```bash
dotnet --version
# Should print 8.x.x
```

---

## Setup Instructions

### 1. Clone the repository

```bash
git clone https://github.com/YOUR-USERNAME/YOUR-REPO.git
cd YOUR-REPO
```

### 2. Open the solution

Open `ComradesMarathonAPI.sln` in **Visual Studio 2022**.

### 3. Configure the connection string

The connection string lives in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Server=.\SQLEXPRESS;Database=ComradesMarathonDb;Integrated Security=True;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;"
  }
}
```

Change `Server=.\\SQLEXPRESS` if your SQL Server instance has a different name.
Common alternatives:

- `Server=(localdb)\\mssqllocaldb` — LocalDB
- `Server=localhost` — default instance
- `Server=YOUR-MACHINE\\SQLEXPRESS` — named instance

### 4. Restore NuGet packages

In Visual Studio, right-click the solution → **Restore NuGet Packages**.

Or from the CLI:

```bash
dotnet restore
```

### 5. Create the database and apply migrations

The app **applies migrations automatically on startup**, so you don't strictly need to
do this step. But you can also do it manually in **Package Manager Console**
(**Tools → NuGet Package Manager → Package Manager Console**):

```powershell
Update-Database
```

Or from the CLI:

```bash
dotnet ef database update --project ComradesMarathonAPI
```

If you don't have a migration yet, create one first:

```bash
dotnet ef migrations add InitialCreate --project ComradesMarathonAPI
```

---

## Running the API

### From Visual Studio

1. Set `ComradesMarathonAPI` as the **Startup Project**.
2. Press **F5** (or **Ctrl + F5** to skip the debugger).
3. The browser opens at:

   ```
   https://localhost:7157/swagger
   ```

### From the CLI

```bash
cd ComradesMarathonAPI
dotnet run
```

Then open `https://localhost:7157/swagger`.

### What happens on first run

- The database `ComradesMarathonDb` is created (if it does not already exist).
- All EF Core migrations are applied — the tables `Roles`, `Users`, `Events`,
  `Categories`, `Enrolments`, `Results`, and `Payments` are created.
- Three roles are seeded: **Admin**, **Organiser**, **Participant** (only if the
  `Roles` table is empty, so re-runs do not duplicate).

### Try it out in Swagger

1. **Create an Organiser** — expand `POST /api/auth/signin` → **Try it out** → paste:

   ```json
   {
     "firstName": "Jane",
     "email": "jane@raceday.com",
     "password": "Password123!",
     "confirmPassword": "Password123!",
     "role": "Organiser"
   }
   ```

   Expect **201 Created**.

2. **Create a Participant** — same endpoint, different email and `"role": "Participant"`.

3. **Create an Event** (Organiser only) — expand `POST /api/events` and submit the
   event details. The session cookie from `signin` is sent automatically.

4. **Create a Category** — `POST /api/events/{eventId}/categories`.

5. **Log in as the Participant** — `POST /api/auth/login` with the participant’s
   credentials, then enrol via `POST /api/enrolments`.

The session cookie stays in the browser, so all subsequent calls inherit the current
user’s identity and role.

---

## Running the Tests

The project uses **xUnit** with an **EF Core InMemory** database. No real SQL Server
is required to run the tests — they run entirely in memory.

### From Visual Studio

**Test → Test Explorer → Run All**

### From the CLI

```bash
dotnet test
```

Expected output:

```
Passed!  - Failed: 0, Passed: 8, Skipped: 0, Total: 8, Duration: 245 ms
```

### What the tests cover

The `EventsControllerTests` class verifies success and failure paths for every HTTP
verb on the `EventsController`:

| Test | What it verifies |
|---|---|
| `GetAll_ReturnsAllEvents` | Public list endpoint returns all events |
| `GetById_ExistingEvent_ReturnsOk` | Public detail endpoint returns the event |
| `GetById_NonExistingEvent_ReturnsNotFound` | 404 on a missing id |
| `Create_AsOrganiser_ReturnsCreated` | Organiser POST creates an event (201) |
| `Update_AsOwner_ReturnsOk` | Organiser PUT updates own event (200) |
| `Update_AsDifferentOrganiser_ReturnsForbid` | Ownership enforcement (403) |
| `Delete_AsOwner_ReturnsNoContent` | Organiser DELETE removes event (204) |
| `Delete_NonExisting_ReturnsNotFound` | 404 on a missing id |

---

## API Endpoints

All endpoints are exposed through Swagger at `/swagger`.

### Authentication — `/api/auth`

| Method | Route | Access | Description |
|---|---|---|---|
| POST | `/api/auth/signin` | Public | Register a new user with a role |
| POST | `/api/auth/login` | Public | Log in and create a session |
| POST | `/api/auth/logout` | Authenticated | Clear the current session |

### Users — `/api/users`

| Method | Route | Access | Description |
|---|---|---|---|
| GET | `/api/users/email` | Authenticated | Get current user profile |
| PUT | `/api/users/profile` | Authenticated | Update current user profile |
| GET | `/api/users/{id}` | Authenticated | Get another user’s public profile |
| GET | `/api/users` | Admin | List all users |

### Events — `/api/events`

| Method | Route | Access | Description |
|---|---|---|---|
| GET | `/api/events` | Public | List events (filter by location, date) |
| GET | `/api/events/{id}` | Public | Event details with categories |
| POST | `/api/events` | Organiser | Create an event |
| PUT | `/api/events/{id}` | Organiser (owner) | Update an event |
| DELETE | `/api/events/{id}` | Organiser (owner) | Delete an event |

### Categories — `/api/events/{eventId}/categories`, `/api/categories/{id}`

| Method | Route | Access | Description |
|---|---|---|---|
| GET | `/api/events/{eventId}/categories` | Public | List categories for an event |
| POST | `/api/events/{eventId}/categories` | Organiser | Add a category |
| PUT | `/api/categories/{id}` | Organiser (owner) | Update a category |

### Enrolments — `/api/enrolments`, `/api/users/enrolments`

| Method | Route | Access | Description |
|---|---|---|---|
| POST | `/api/enrolments` | Participant | Enrol in a category |
| GET | `/api/users/enrolments` | Authenticated | Current user’s enrolments |
| PUT | `/api/enrolments/{id}` | Participant / Organiser | Update status |
| DELETE | `/api/enrolments/{id}` | Participant / Organiser | Cancel enrolment |

### Results — `/api/enrolments/{id}/result`, `/api/results/{id}`

| Method | Route | Access | Description |
|---|---|---|---|
| GET | `/api/enrolments/{id}/result` | Participant / Organiser | Get result |
| POST | `/api/enrolments/{id}/result` | Organiser | Add a result |
| PUT | `/api/results/{id}` | Organiser | Update result |
| DELETE | `/api/results/{id}` | Organiser | Delete result |

### Payments — `/api/payment`

| Method | Route | Access | Description |
|---|---|---|---|
| GET | `/api/payment/admin` | Admin | List all payments |
| GET | `/api/payment/participant` | Participant | Current user’s total |

---

## CI/CD Pipeline

The repository uses **GitHub Actions** to build the solution and run all unit tests on
every push to every branch, and on every pull request.

### Workflow file

`.github/workflows/ci.yml`

```yaml
name: Build and Test

on:
  push:
    branches:
      - '**'
  pull_request:
    branches:
      - '**'

jobs:
  build-and-test:
    name: Build and run unit tests
    runs-on: ubuntu-latest

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET 8
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
          restore-keys: |
            ${{ runner.os }}-nuget-

      - name: Restore dependencies
        run: dotnet restore ComradesMarathonAPI.sln

      - name: Build
        run: dotnet build ComradesMarathonAPI.sln --no-restore --configuration Release

      - name: Run tests
        run: dotnet test ComradesMarathonAPI.sln --no-build --configuration Release --verbosity normal
```

### What the workflow does

1. **Triggers** on every `push` and `pull_request` to every branch.
2. **Checks out** the repository on a fresh Ubuntu runner.
3. **Installs the .NET 8 SDK**.
4. **Caches NuGet packages** so subsequent runs are fast.
5. **Restores** all NuGet dependencies.
6. **Builds** the entire solution in Release configuration.
7. **Runs** all xUnit tests.

If the build or any test fails, the workflow fails and the commit is marked with a red
tick on GitHub — you will be notified before the broken code is merged.

### Viewing the CI results

1. Push your code: `git push`.
2. Open your repository on GitHub.
3. Click the **Actions** tab.
4. Click the most recent workflow run to see the log for each step.
5. A green tick means the build and tests passed. A red cross means something failed —
   click the failing step to see the exact error.

---

## CI/CD Screenshot

Below is a screenshot of the GitHub Actions workflow running on a push — all steps
passed, and all 8 unit tests succeeded.

![CI/CD pipeline running on GitHub Actions](docs/images/cicd-screenshot.png)

### How to take the screenshot

1. Push your code to GitHub.
2. Open the repository in a browser.
3. Click the **Actions** tab.
4. Click on the workflow run named **Build and Test**.
5. Wait for the run to complete (green tick at the top).
6. Expand the **Run tests** step to show the `Passed!` summary line.
7. Take a screenshot that includes:
   - The workflow name (**Build and Test**)
   - The commit hash / branch name
   - The green status of every step
   - The `Passed!  - Failed: 0, Passed: 8, Skipped: 0, Total: 8` output

Save the image as `docs/images/cicd-screenshot.png` in the repository and commit it
so the README renders it inline.

---

## License

This project was produced for the **PROG6212** module at **The Independent Institute
of Education (Pty) Ltd**. See `LICENSE.txt` for details.


Youtube link: https://youtu.be/UYkuYjgpXkI?si=SQdBimekWYpuvx0C

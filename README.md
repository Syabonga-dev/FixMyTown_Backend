# FixMyTown

> **A digital civic engagement platform for reporting, tracking, and managing community and municipal service issues.**

## Table of Contents

* [1. Project Overview](#1-project-overview)
* [2. Problem Statement](#2-problem-statement)
* [3. Project Objectives](#3-project-objectives)
* [4. Core Functionality](#4-core-functionality)
* [5. User Roles](#5-user-roles)
* [6. System Architecture](#6-system-architecture)
* [7. Technology Stack](#7-technology-stack)
* [8. Application Components](#8-application-components)
* [9. Issue Reporting Workflow](#9-issue-reporting-workflow)
* [10. Authentication and Authorization](#10-authentication-and-authorization)
* [11. Data Management](#11-data-management)
* [12. API](#12-api)
* [13. Frontend](#13-frontend)
* [14. Backend](#14-backend)
* [15. Project Structure](#15-project-structure)
* [16. Configuration](#16-configuration)
* [17. Local Development Setup](#17-local-development-setup)
* [18. Production Deployment](#18-production-deployment)
* [19. Security Considerations](#19-security-considerations)
* [20. Error Handling](#20-error-handling)
* [21. Testing](#21-testing)
* [22. Maintenance](#22-maintenance)
* [23. Known Limitations](#23-known-limitations)
* [24. Future Enhancements](#24-future-enhancements)
* [25. Development Guidelines](#25-development-guidelines)
* [26. License](#26-license)

---

# 1. Project Overview

**FixMyTown** is a civic issue reporting and management platform designed to provide communities with a structured digital channel for reporting problems affecting their local environment and public services.

The platform connects residents and community members with the appropriate administrative or municipal personnel by allowing issues to be digitally submitted, categorized, tracked, and managed.

Instead of relying exclusively on informal communication channels, telephone calls, physical reporting, or social-media complaints, FixMyTown provides a centralized system through which community issues can be recorded and monitored.

The system is designed around three fundamental principles:

1. **Accessibility** — residents should have a straightforward way to report problems.
2. **Transparency** — users should be able to understand the status of submitted issues.
3. **Accountability** — reported issues should be recorded and managed through a traceable workflow.

---

# 2. Problem Statement

Communities frequently encounter infrastructure and municipal service problems such as:

* Damaged roads
* Potholes
* Broken streetlights
* Water-related problems
* Waste-management issues
* Illegal dumping
* Damaged public infrastructure
* Other local service-delivery problems

Traditional reporting methods can make it difficult for residents to determine whether a complaint has been received, who is responsible for it, and whether action has been taken.

FixMyTown addresses this problem by providing a centralized digital platform for submitting and managing community issues.

---

# 3. Project Objectives

The primary objectives of FixMyTown are to:

* Provide residents with a digital mechanism for reporting local issues.
* Capture structured information about reported problems.
* Allow users to monitor the progress of submitted reports.
* Provide administrative personnel with tools for managing reports.
* Improve communication between citizens and responsible authorities.
* Maintain a centralized record of reported community issues.
* Improve visibility into unresolved and completed issues.
* Provide a foundation for data-driven identification of recurring community problems.

---

# 4. Core Functionality

The platform is designed to support the following major functionality.

## 4.1 User Registration

Users can create accounts that allow them to access authenticated platform functionality.

## 4.2 User Authentication

Registered users can authenticate with their credentials before accessing protected functionality.

Authentication is responsible for establishing the identity of the user.

## 4.3 Issue Reporting

Authenticated users can submit reports describing problems within their community.

A report may contain information such as:

* Issue title
* Description
* Issue category
* Location
* Supporting information
* Submission date
* Current status

Where supported by the implementation, reports may also include uploaded images or other evidence.

## 4.4 Issue Categorization

Issues can be organized into categories so that similar problems can be grouped and managed consistently.

Examples include:

* Roads
* Water
* Electricity
* Waste
* Public infrastructure
* Street lighting
* Other municipal/community services

## 4.5 Issue Tracking

A submitted issue progresses through a defined status lifecycle.

Typical statuses may include:

```text
Submitted
    ↓
Under Review
    ↓
In Progress
    ↓
Resolved
```

The exact statuses available to users are determined by the implemented backend workflow.

## 4.6 Administrative Management

Authorized personnel can manage submitted reports and update their status as work progresses.

Administrative functionality may include:

* Viewing reports
* Filtering reports
* Reviewing report details
* Updating report status
* Managing categories
* Managing users
* Monitoring unresolved issues

Access to administrative functionality is restricted through authorization controls.

---

# 5. User Roles

FixMyTown uses role-based access to distinguish between different types of users.

## Citizen / Resident

A standard user can:

* Register an account.
* Authenticate.
* Submit issues.
* View submitted issues.
* Monitor issue status.
* Access functionality available to standard users.

## Administrator

An administrator has elevated permissions and can perform management operations that are not available to ordinary users.

Administrative responsibilities may include:

* Reviewing submitted issues.
* Updating issue statuses.
* Managing system records.
* Managing users.
* Monitoring reports.

> **Implementation note:** The exact role names and permissions must match the roles configured in the application's authentication system.

---

# 6. System Architecture

FixMyTown follows a client-server architecture.

```text
┌──────────────────────────┐
│       User / Citizen     │
└────────────┬─────────────┘
             │
             │ HTTP / HTTPS
             ▼
┌──────────────────────────┐
│        Frontend          │
│  Web Application / UI    │
└────────────┬─────────────┘
             │
             │ REST API
             ▼
┌──────────────────────────┐
│         Backend          │
│   ASP.NET Core Web API   │
└────────────┬─────────────┘
             │
             │ Entity Framework /
             │ Database Provider
             ▼
┌──────────────────────────┐
│        Database          │
│       SQL Server         │
└──────────────────────────┘
```

The architecture separates presentation, application logic, and persistent data storage.

This separation improves maintainability and allows the frontend and backend to be developed and deployed independently.

---

# 7. Technology Stack

The project is built using a modern web application architecture.

| Layer                   | Technology                         |
| ----------------------- | ---------------------------------- |
| Frontend                | React                              |
| UI                      | React-based component architecture |
| Backend                 | ASP.NET Core Web API               |
| Programming Language    | C#                                 |
| ORM                     | Entity Framework Core              |
| Database                | Microsoft SQL Server               |
| API Style               | REST                               |
| Authentication          | Token-based authentication         |
| Development Environment | Visual Studio / VS Code            |
| Package Management      | npm / .NET tooling                 |
| Version Control         | Git / GitHub                       |

> **Important:** Package versions and specific libraries should be taken directly from the project's package manifests rather than inferred from this document.

---

# 8. Application Components

FixMyTown consists of several logical components.

## Frontend

The frontend provides the user-facing web interface.

Responsibilities include:

* Rendering application pages.
* Handling user interaction.
* Collecting report information.
* Sending requests to the backend API.
* Displaying API responses.
* Managing authentication state.
* Displaying issue statuses.

## Backend API

The backend provides the application's business logic and API layer.

Responsibilities include:

* Authentication.
* Authorization.
* Request validation.
* Business logic.
* Issue management.
* User management.
* Database operations.
* API response handling.

## Database

The database provides persistent storage for application data.

Potential data domains include:

* Users
* Roles
* Issues
* Categories
* Statuses
* Locations
* Report metadata
* Audit information

The exact schema is defined by the application's Entity Framework Core models and migrations.

---

# 9. Issue Reporting Workflow

The general issue-reporting lifecycle is:

```text
1. User authenticates
          │
          ▼
2. User creates a report
          │
          ▼
3. Frontend validates input
          │
          ▼
4. Frontend sends API request
          │
          ▼
5. Backend validates request
          │
          ▼
6. Report is persisted
          │
          ▼
7. Report becomes available
   for administrative review
          │
          ▼
8. Administrator updates status
          │
          ▼
9. User can monitor progress
```

This workflow provides a consistent lifecycle for community reports.

---

# 10. Authentication and Authorization

Authentication and authorization are separate security concerns within the system.

### Authentication

Authentication determines **who the user is**.

### Authorization

Authorization determines **what the authenticated user is allowed to do**.

Protected API endpoints should require valid authentication credentials where appropriate.

Administrative endpoints should additionally enforce the required role or permission.

A typical authorization model is:

```text
Unauthenticated User
        │
        ├── Public functionality
        │
        ▼
Authenticated User
        │
        ├── Citizen functionality
        │
        ▼
Administrator
        │
        └── Administrative functionality
```

Authentication credentials and cryptographic secrets must never be committed to source control.

---

# 11. Data Management

FixMyTown uses persistent database storage to ensure that reports and associated application information survive application restarts.

Entity Framework Core provides the application's data-access layer.

The database model should maintain appropriate relationships between entities.

For example:

```text
User
 │
 └──< Issue
        │
        ├── Category
        │
        ├── Status
        │
        └── Location
```

The actual relationships are defined by the project's entity models and database configuration.

---

# 12. API

The backend exposes RESTful endpoints consumed by the frontend.

Typical API responsibilities include:

### Authentication

```text
POST /api/auth/login
POST /api/auth/register
```

### Issues

```text
GET    /api/issues
GET    /api/issues/{id}
POST   /api/issues
PUT    /api/issues/{id}
DELETE /api/issues/{id}
```

### Categories

```text
GET /api/categories
```

### Users

```text
GET /api/users
GET /api/users/{id}
```

> **Important:** The endpoint paths above represent the intended API organization and must be replaced with the exact routes defined by the controllers in the implementation.

When available, Swagger/OpenAPI should be used as the authoritative API reference.

---

# 13. Frontend

The frontend is responsible for presenting FixMyTown to end users.

Its responsibilities include:

* Navigation.
* Authentication interfaces.
* Registration.
* Login.
* Dashboard functionality.
* Issue submission.
* Issue listing.
* Issue details.
* Status visualization.
* Administrative interfaces.
* API communication.
* Client-side validation.

The frontend communicates with the backend through HTTP requests.

Example:

```text
React Component
      │
      ▼
API Request
      │
      ▼
ASP.NET Core Controller
      │
      ▼
Business Logic
      │
      ▼
Entity Framework Core
      │
      ▼
SQL Server
```

---

# 14. Backend

The backend is implemented as an ASP.NET Core Web API.

Its responsibilities include:

* Exposing REST endpoints.
* Processing HTTP requests.
* Validating incoming data.
* Enforcing authorization.
* Executing business logic.
* Interacting with SQL Server.
* Returning structured HTTP responses.

A conventional backend organization may contain:

```text
Controllers/
Models/
DTOs/
Data/
Services/
Middleware/
Migrations/
Properties/
Program.cs
appsettings.json
```

The actual project structure should be treated as authoritative.

---

# 15. Project Structure

A typical FixMyTown repository may be organized similarly to:

```text
FixMyTown/
│
├── Frontend/
│   ├── public/
│   ├── src/
│   │   ├── components/
│   │   ├── pages/
│   │   ├── services/
│   │   ├── assets/
│   │   └── ...
│   ├── package.json
│   └── ...
│
├── Backend/
│   ├── Controllers/
│   ├── Data/
│   ├── Models/
│   ├── DTOs/
│   ├── Services/
│   ├── Migrations/
│   ├── Program.cs
│   ├── appsettings.json
│   └── ...
│
├── README.md
└── ...
```

The actual directory names should reflect the repository rather than this conceptual structure.

---

# 16. Configuration

Application configuration should be environment-specific.

Typical backend configuration includes:

* Database connection string.
* Authentication configuration.
* JWT configuration, where applicable.
* CORS configuration.
* Logging configuration.
* External service configuration.

Sensitive values should be supplied through environment variables, deployment configuration, or secure secret storage.

### Example

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "<DATABASE_CONNECTION_STRING>"
  }
}
```

Never commit real passwords, API keys, JWT signing keys, or production credentials to GitHub.

---

# 17. Local Development Setup

## Prerequisites

Before running FixMyTown locally, install:

* Git
* Node.js
* npm
* .NET SDK compatible with the backend project
* Microsoft SQL Server
* SQL Server Management Studio or another SQL Server client

## Clone the repository

```bash
git clone <REPOSITORY_URL>
cd FixMyTown
```

## Backend

Navigate to the backend directory:

```bash
cd Backend
```

Restore dependencies:

```bash
dotnet restore
```

Build the project:

```bash
dotnet build
```

Run the API:

```bash
dotnet run
```

The API URL will be displayed in the terminal when the application starts.

## Database

Configure the database connection in the appropriate development configuration.

If Entity Framework Core migrations are used:

```bash
dotnet ef database update
```

The exact migration commands depend on the project's DbContext and project structure.

## Frontend

Navigate to the frontend:

```bash
cd Frontend
```

Install dependencies:

```bash
npm install
```

Start the development server:

```bash
npm run dev
```

or, if the project uses a different npm script:

```bash
npm start
```

The exact command is defined by the `scripts` section of `package.json`.

---

# 18. Production Deployment

Production deployment consists of two independent components:

```text
Frontend
   │
   └── Static production build

Backend
   │
   └── ASP.NET Core application

Database
   │
   └── SQL Server
```

## Frontend Build

Create a production build:

```bash
npm run build
```

For Vite-based applications, this normally generates:

```text
dist/
├── assets/
├── index.html
└── ...
```

The generated production files can then be deployed to the configured web server.

## Backend Deployment

The ASP.NET Core backend should be published using:

```bash
dotnet publish -c Release
```

The resulting published application can then be deployed to the target server.

## Production Configuration

Production deployment must provide:

* Production database connection.
* Correct CORS configuration.
* Secure authentication secrets.
* Correct API URL.
* HTTPS where supported.
* Appropriate server permissions.
* Database availability.

---

# 19. Security Considerations

Security is a fundamental requirement because FixMyTown handles user accounts and potentially location-related information.

The following practices should be maintained:

### Credentials

Never commit passwords or secrets.

### Authentication

Use strong authentication mechanisms and secure token handling.

### Authorization

Administrative operations must be protected by appropriate authorization policies.

### Input Validation

All user-submitted data must be validated on the server.

Client-side validation should not be considered a security boundary.

### SQL Injection

Database operations should use Entity Framework Core parameterized queries or equivalent safe data-access mechanisms.

### HTTPS

Production deployments should use HTTPS to protect credentials and application traffic.

### CORS

CORS should be configured to allow only trusted frontend origins in production.

### Error Responses

Production API responses should avoid exposing:

* Database connection strings.
* Stack traces.
* Internal server paths.
* Authentication secrets.
* Implementation details.

---

# 20. Error Handling

The application should provide predictable error responses.

Common HTTP status codes include:

| Status                      | Meaning                                    |
| --------------------------- | ------------------------------------------ |
| `200 OK`                    | Request completed successfully             |
| `201 Created`               | Resource successfully created              |
| `204 No Content`            | Request succeeded without response content |
| `400 Bad Request`           | Invalid request                            |
| `401 Unauthorized`          | Authentication required or invalid         |
| `403 Forbidden`             | Authenticated user lacks permission        |
| `404 Not Found`             | Resource does not exist                    |
| `409 Conflict`              | Request conflicts with current state       |
| `500 Internal Server Error` | Unexpected server-side failure             |

Frontend components should handle these responses appropriately and provide understandable feedback to users.

---

# 21. Testing

Testing should cover the application's critical functionality.

Recommended test areas include:

### Authentication

* Registration.
* Login.
* Invalid credentials.
* Unauthorized access.
* Role restrictions.

### Issue Management

* Creating reports.
* Viewing reports.
* Updating reports.
* Invalid report data.
* Status transitions.
* Access control.

### API

* Valid requests.
* Invalid requests.
* Missing authentication.
* Invalid identifiers.
* Database failures.

### Frontend

* Form validation.
* Navigation.
* API error handling.
* Authentication state.
* Responsive layout.
* Role-based UI.

---

# 22. Maintenance

Maintainers should regularly:

* Update dependencies.
* Review application logs.
* Back up the database.
* Monitor application availability.
* Review authentication configuration.
* Remove unused code.
* Apply security updates.
* Review database performance.
* Verify production configuration.

Database migrations should be tested before being applied to production.

---

# 23. Known Limitations

The following limitations should be documented according to the deployed implementation:

* Availability depends on the backend API and database.
* Users require network access to communicate with the hosted system.
* Some municipal workflows may require integration with external systems.
* Issue resolution depends on the responsible authority or administrator.
* Report accuracy depends on information supplied by users.
* Exact deployment capabilities depend on the infrastructure available to the institution.

---

# 24. Future Enhancements

Potential future improvements include:

* Real-time issue-status notifications.
* Email and SMS notifications.
* Push notifications.
* Interactive map-based reporting.
* GPS-assisted location capture.
* Image and document attachments.
* Duplicate-report detection.
* Advanced administrative analytics.
* Issue heat maps.
* Municipal department routing.
* SLA monitoring.
* Automated escalation.
* Citizen feedback after resolution.
* Report prioritization.
* Audit trails.
* Mobile applications.
* Progressive Web App support.
* Advanced accessibility improvements.

---

# 25. Development Guidelines

Contributors should follow these principles:

### Code Quality

Write readable, maintainable, and appropriately documented code.

### Separation of Concerns

Keep presentation, business logic, data access, and infrastructure responsibilities separated.

### Security

Never expose credentials or sensitive configuration through source control.

### Version Control

Use meaningful commit messages.

Example:

```text
feat: add issue status management
fix: resolve authentication token validation
refactor: improve issue service
docs: update deployment instructions
```

### API Changes

Changes to API contracts should be reflected in the frontend and documented appropriately.

### Database Changes

Database schema modifications should be implemented through the project's migration mechanism where applicable.

---

# 26. License

This project is intended for educational and/or institutional development purposes.

The applicable license and usage restrictions should be specified here according to the project's official distribution requirements.

---

## Project Status

**FixMyTown is an actively developed civic issue reporting and management platform.**

The application consists of a web-based frontend, an ASP.NET Core backend API, and persistent database storage.

The exact capabilities available in a deployment depend on the version of the application and the configuration of the target environment.

---


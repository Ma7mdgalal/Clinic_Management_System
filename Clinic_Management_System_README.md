# Clinic Management System (CMS)

## 1. Project Overview

The Clinic Management System (CMS) is a backend-focused web application designed to help clinics manage doctors, patients, appointments, medical records, prescriptions, invoices, and basic clinic operations.

You are joining the project as a **Junior Backend Developer**.

The goal is to build a maintainable RESTful API that can later be consumed by a web or mobile frontend.

> **Important:** This document describes the business requirements and project expectations. It intentionally does not provide implementation details or ready-made solutions.

---

## 2. Business Goal

The clinic currently manages its operations manually. This causes problems such as:

- Double-booked appointments.
- Difficulty finding patient history.
- Poor tracking of doctors' schedules.
- Manual invoice management.
- Lack of clear user permissions.
- Difficulty generating operational reports.

The new system should centralize these operations into one backend system.

---

## 3. Project Scope

### In Scope

The first version must support:

1. Authentication and authorization.
2. User management.
3. Doctor management.
4. Patient management.
5. Specialties.
6. Appointment management.
7. Patient medical records.
8. Prescriptions.
9. Invoices and payments.
10. Basic dashboard/reporting endpoints.
11. Validation and error handling.
12. Logging.
13. API documentation.

### Out of Scope for V1

The following are intentionally postponed:

- Online payment gateway integration.
- Video consultations.
- SMS/WhatsApp integration.
- AI diagnosis.
- Advanced analytics.
- Multi-branch support.
- Mobile applications.

These may become future requirements.

---

# 4. User Roles

The system will initially contain four roles.

## Admin

Can:

- Manage users.
- Manage doctors.
- Manage specialties.
- View all appointments.
- View reports.
- Manage system-level settings.

## Receptionist

Can:

- Register patients.
- View patients.
- Create appointments.
- Reschedule appointments.
- Cancel appointments.
- View doctors' schedules.
- Create invoices.

## Doctor

Can:

- View assigned appointments.
- View patient information.
- View patient medical history.
- Create/update medical records.
- Create prescriptions.

## Patient

Can:

- View their profile.
- View their appointments.
- View their medical records.
- View their prescriptions.
- View their invoices.

---

# 5. Core Modules

## 5.1 Authentication

The system must provide:

- User registration where applicable.
- Login.
- Secure password storage.
- JWT-based authentication.
- Role-based authorization.
- Current-user endpoint.
- Logout/token invalidation strategy if required by the implementation.

---

## 5.2 Doctor Management

A doctor should contain information such as:

- Id
- First Name
- Last Name
- Email
- Phone
- Specialty
- License Number
- Consultation Fee
- IsActive
- CreatedAt

Requirements:

- Admin can create doctors.
- Admin can update doctors.
- Admin can deactivate doctors.
- Admin can view doctors.
- Doctors belong to a specialty.
- License Number must be unique.

---

## 5.3 Specialty Management

A specialty contains:

- Id
- Name
- Description
- IsActive

Requirements:

- Admin can create specialties.
- Admin can update specialties.
- Admin can deactivate specialties.
- Specialty names should not be duplicated.

Examples:

- Cardiology
- Dermatology
- Pediatrics
- Dentistry
- Orthopedics

---

# 6. Patient Management

A patient should contain:

- Id
- First Name
- Last Name
- Date of Birth
- Gender
- Phone
- Email
- Address
- Emergency Contact
- CreatedAt
- IsActive

Requirements:

- Receptionist can register a patient.
- Receptionist can update patient information.
- Authorized users can search patients.
- Patient can view their own information.
- Patient data must not be exposed to unauthorized users.

---

# 7. Appointment Management

An appointment contains:

- Id
- PatientId
- DoctorId
- AppointmentDate
- StartTime
- EndTime
- Status
- Reason
- Notes
- CreatedAt

Appointment statuses:

```text
Scheduled
Confirmed
Completed
Cancelled
NoShow
```

## Business Rules

1. A doctor cannot have two active appointments at the same time.
2. A patient should not have overlapping appointments.
3. An appointment cannot be created for an inactive doctor.
4. An appointment cannot be created for an inactive patient.
5. Appointment duration must be valid.
6. Cancelled appointments cannot be completed.
7. Completed appointments should not be freely modified.
8. Only authorized roles can create/update/cancel appointments.

---

# 8. Doctor Schedule

The system should support basic doctor availability.

A doctor may define:

- Working day.
- Start time.
- End time.
- Break periods.

Example:

```text
Monday
09:00 -> 13:00
17:00 -> 21:00
```

Appointment creation should respect the doctor's configured availability.

> Exact scheduling implementation is left to the developer.

---

# 9. Medical Records

After an appointment, a doctor can create a medical record.

A medical record may contain:

- Id
- PatientId
- DoctorId
- AppointmentId
- Diagnosis
- Symptoms
- Notes
- Treatment
- CreatedAt
- UpdatedAt

Requirements:

- Medical records belong to a patient.
- A medical record should reference the responsible doctor.
- A doctor can create records for patients they treated.
- Patients can view their own records.
- Receptionists should not modify medical content.

---

# 10. Prescriptions

A prescription belongs to a medical record.

A prescription contains:

- Id
- MedicalRecordId
- DoctorId
- PatientId
- CreatedAt
- Notes

Each prescription contains one or more medication items.

Medication item:

- Medication Name
- Dosage
- Frequency
- Duration
- Instructions

Example:

```text
Medication: Amoxicillin
Dosage: 500mg
Frequency: 3 times/day
Duration: 7 days
Instructions: After meals
```

Requirements:

- Only authorized doctors can create prescriptions.
- A prescription cannot exist without a medical record.
- Prescription items must contain valid required data.

---

# 11. Billing & Invoices

An invoice contains:

- Id
- PatientId
- AppointmentId
- InvoiceNumber
- Amount
- Discount
- Tax
- TotalAmount
- Status
- CreatedAt
- PaidAt

Invoice statuses:

```text
Pending
Paid
Cancelled
```

Requirements:

- An invoice can be generated for an appointment.
- Invoice number must be unique.
- Total amount must be calculated correctly.
- Paid invoices cannot be edited freely.
- Authorized staff can mark invoices as paid.

---

# 12. Payment Gateway

### Future Requirement

Online payment integration is NOT required for V1.

However, the architecture should avoid making future payment integration unnecessarily difficult.

Possible future providers:

- Stripe
- Paymob
- PayPal

You should design the application so that payment-provider-specific code does not spread throughout the business logic.

---

# 13. Dashboard & Reporting

The API should eventually provide basic statistics such as:

- Total patients.
- Total doctors.
- Today's appointments.
- Completed appointments.
- Cancelled appointments.
- Pending invoices.
- Total revenue.

Example endpoint concept:

```http
GET /api/dashboard/summary
```

The exact response structure is left to the developer.

---

# 14. Technical Requirements

## Backend

Use:

- C#
- .NET / ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- LINQ
- RESTful API principles

## Authentication

Use:

- JWT
- ASP.NET Core Authentication/Authorization
- Role-based authorization

## API Documentation

Use:

- Swagger / OpenAPI

## Version Control

Use:

- Git
- GitHub

---

# 15. Suggested Architecture

The project should follow a clean and maintainable architecture.

Suggested structure:

```text
ClinicManagementSystem
│
├── API
│
├── Application
│
├── Domain
│
├── Infrastructure
│
└── Tests
```

Responsibilities should be separated.

### Domain

Contains core business entities and domain-related rules.

### Application

Contains:

- Use cases/services.
- DTOs.
- Interfaces.
- Business logic.
- Application-level abstractions.

### Infrastructure

Contains:

- EF Core.
- DbContext.
- Repositories if used.
- Database configuration.
- External service implementations.

### API

Contains:

- Controllers.
- Middleware.
- Authentication configuration.
- Dependency injection configuration.
- API-specific concerns.

### Tests

Contains automated tests.

> Do not blindly implement every design pattern you know. Use a pattern when it solves a real problem.

---

# 16. Database Requirements

The database should represent relationships between:

```text
User
Doctor
Specialty
Patient
DoctorSchedule
Appointment
MedicalRecord
Prescription
PrescriptionItem
Invoice
```

You are responsible for determining:

- Primary keys.
- Foreign keys.
- Required fields.
- Nullable fields.
- Indexes.
- Unique constraints.
- Delete behaviors.
- Relationships.

The database should be normalized appropriately.

---

# 17. API Requirements

Follow RESTful conventions.

Example resource structure:

```http
GET    /api/doctors
GET    /api/doctors/{id}
POST   /api/doctors
PUT    /api/doctors/{id}
DELETE /api/doctors/{id}
```

Use appropriate HTTP status codes.

Examples:

```text
200 OK
201 Created
204 No Content
400 Bad Request
401 Unauthorized
403 Forbidden
404 Not Found
409 Conflict
500 Internal Server Error
```

Do not return `200 OK` for every situation.

---

# 18. Validation

The API must validate incoming requests.

Examples:

- Required fields.
- Valid email format.
- Valid dates.
- Valid appointment times.
- Positive prices.
- Valid IDs.
- Duplicate license numbers.
- Appointment conflicts.

Validation errors should return a consistent response format.

---

# 19. Error Handling

The API should have centralized error handling.

Do not put large try/catch blocks inside every controller.

Errors should produce meaningful HTTP responses without exposing sensitive internal information.

Example concept:

```json
{
  "statusCode": 400,
  "message": "The request is invalid.",
  "errors": {
    "Email": [
      "A valid email address is required."
    ]
  }
}
```

Exact implementation is your responsibility.

---

# 20. Logging

The application should log important events and errors.

Examples:

- Application startup.
- Authentication failures.
- Unexpected exceptions.
- Important business operations.
- Database-related failures.

Do not log:

- Passwords.
- JWT tokens.
- Sensitive patient information unnecessarily.

---

# 21. Pagination, Filtering & Sorting

List endpoints should eventually support pagination.

Example:

```http
GET /api/patients?pageNumber=1&pageSize=10
```

Filtering examples:

```http
GET /api/doctors?specialtyId=2
GET /api/appointments?status=Scheduled
GET /api/patients?search=Ahmed
```

Sorting should be supported where useful.

---

# 22. DTOs

Do not expose EF Core entities directly from every API endpoint.

Use DTOs where appropriate.

Examples:

```text
CreateDoctorRequest
UpdateDoctorRequest
DoctorResponse
CreatePatientRequest
PatientResponse
CreateAppointmentRequest
AppointmentResponse
```

You are responsible for deciding the DTO boundaries.

---

# 23. Security Requirements

The application must:

- Hash passwords securely.
- Protect authenticated endpoints.
- Apply authorization rules.
- Validate user input.
- Avoid SQL injection.
- Avoid exposing sensitive data.
- Avoid returning passwords or security credentials.
- Follow the principle of least privilege.

---

# 24. Testing Requirements

Testing will be introduced gradually.

The project should eventually include:

### Unit Tests

Examples:

- Appointment conflict detection.
- Invoice calculation.
- Business validation.
- Authorization-related business rules.

### Integration Tests

Examples:

- Creating an appointment.
- Authenticating a user.
- Reading data from the database.
- API endpoint behavior.

---

# 25. Git Workflow

Use feature branches.

Example:

```text
main
develop
feature/authentication
feature/patient-management
feature/appointments
```

Commit messages should be meaningful.

Example:

```text
feat: add patient creation endpoint
feat: implement appointment conflict validation
fix: prevent duplicate doctor license numbers
test: add appointment service tests
```

Avoid:

```text
update
changes
final
test
aaa
done
```

---

# 26. Definition of Done

A task is considered complete only when:

- The code is implemented.
- The application builds successfully.
- The feature works as required.
- Validation exists where required.
- Appropriate HTTP status codes are used.
- No obvious bugs remain.
- The code follows the project's architecture.
- Tests are added when required by the task.
- The code is committed to Git.
- The task's acceptance criteria are satisfied.

---

# 27. Sprint Plan

The project will be divided into 6 sprints.

## Sprint 1 — Project Foundation

### Goal

Create the project foundation and establish the architecture.

### Tasks

**CMS-001 — Create solution structure**

Requirements:

- Create the solution.
- Create required projects.
- Configure project references.
- Ensure the solution builds successfully.

Acceptance Criteria:

- Solution builds without errors.
- Project responsibilities are separated.
- API starts successfully.

---

**CMS-002 — Configure SQL Server, EF Core & Serilog Foundation**

Requirements:

- Configure SQL Server.
- Create DbContext.
- Configure dependency injection.
- Create the initial migration.
- Configure Serilog as the application's logging provider.
- Enable structured console logging for application startup, errors, and important technical events.
- Ensure logging configuration is read from application configuration where appropriate.
- Do not log passwords, JWT tokens, or unnecessary patient-sensitive data.

Acceptance Criteria:

- Application can connect to SQL Server.
- Migration is generated successfully.
- Database can be created/updated.
- Serilog is active when the API starts.
- Startup and unhandled error events produce structured log entries.
- Sensitive credentials and patient data are not written to logs.

---

**CMS-003 — Create initial domain entities**

Create the initial entities required for:

- User
- Doctor
- Specialty
- Patient

Acceptance Criteria:

- Relationships are correctly modeled.
- Required fields are enforced.
- Appropriate constraints are considered.

---

**CMS-004 — Create Git repository**

Requirements:

- Initialize Git.
- Create `.gitignore`.
- Create initial commit.
- Push the project to GitHub.

---

## Sprint 2 — Authentication & Users

### Goal

Implement secure authentication and authorization.

### Tasks

**CMS-005 — User registration**

**CMS-006 — Login**

**CMS-007 — JWT authentication**

**CMS-008 — Role-based authorization**

**CMS-009 — Current user endpoint**

Acceptance Criteria:

- Users can authenticate.
- Protected endpoints reject unauthenticated requests.
- Roles restrict access correctly.
- Passwords are never returned through the API.

---

## Sprint 3 — Patients & Doctors

### Goal

Implement the main clinic resources.

### Tasks

**CMS-010 — Specialty CRUD**

**CMS-011 — Doctor CRUD**

**CMS-012 — Patient CRUD**

### CMS-012 Completion Checklist

Before CMS-012 is accepted:

- Admins and Receptionists can register, update, and view patients as required by the role matrix.
- A Patient can view only their own profile; patient data is never exposed to unrelated authenticated users.
- Doctor access to patient data is limited to an authorized clinical relationship when appointment support exists.
- Creating a patient profile either creates no login account or uses an explicit, secure account-creation/invitation flow. Passwords must never be predictable, derived from patient data, returned, or logged.
- All supplied patient fields, including Address, are persisted and returned accurately.
- Logs exclude unnecessary patient-sensitive data such as email, address, date of birth, and emergency contacts.
- Date of birth is valid and cannot be in the future; textual inputs are trimmed and validated.
- Deactivation behavior is defined for any linked Identity account.
- Tests cover authorization, self-profile access, validation, and account/profile creation behavior.
- The task is committed with clean migration history and a clean working tree.

**CMS-013 — Patient search**

**CMS-014 — Pagination**

Acceptance Criteria:

- Authorized users can manage required resources.
- Validation works.
- Pagination works.
- Duplicate constraints are enforced.

### Sprint 3 Completion & Hardening Checklist

Before Sprint 3 can be approved, review CMS-010 through CMS-014 together for:

- Resource authorization that follows the role matrix and does not expose data to unintended roles.
- Secure account creation: passwords must never be predictable, derived from profile data, returned, or logged.
- Complete request/response DTOs and validation for required fields, identifiers, emails, phone numbers, and positive monetary values.
- Correct handling of inactive related resources, such as assigning a doctor only to an active specialty.
- Deactivation behavior for linked accounts and resources.
- Database uniqueness constraints and API-level handling of duplicate conflicts with `409 Conflict`.
- Focused automated tests for important business rules and authorization behavior.
- Clean migrations, meaningful commits, and a clean working tree.
- Consistent boundaries between Identity accounts and Doctor/Patient profiles, so duplicated contact data and account lifecycle changes do not silently diverge.
- Shared, maintainable workflows for creating linked Identity accounts and clinic profiles, without repeating transaction and error-handling logic across services.

> Critical security issues and a task's stated acceptance criteria must be fixed within that task; this checklist is an additional cross-task review gate, not a reason to defer them.

---

## Sprint 4 — Appointment System

### Goal

Build the core appointment workflow.

### Tasks

**CMS-015 — Doctor schedules**

**CMS-016 — Create appointment**

**CMS-017 — Appointment conflict detection**

**CMS-018 — Update/reschedule appointment**

**CMS-019 — Cancel appointment**

**CMS-020 — Appointment listing/filtering**

Acceptance Criteria:

- Overlapping appointments are prevented.
- Doctor availability is respected.
- Appointment statuses behave correctly.
- Authorization rules are enforced.

---

## Sprint 5 — Medical Records & Billing

### Goal

Complete the main clinic workflow.

### Tasks

**CMS-021 — Medical records**

**CMS-022 — Prescriptions**

**CMS-023 — Prescription items**

**CMS-024 — Invoice generation**

**CMS-025 — Payment status**

**CMS-026 — Invoice calculations**

Acceptance Criteria:

- Doctors can create medical records for appropriate patients.
- Prescriptions are linked correctly.
- Invoice totals are correct.
- Unauthorized users cannot modify medical information.

---

## Sprint 6 — Production Readiness

### Goal

Improve quality and prepare the API for real usage.

### Tasks

**CMS-027 — Global exception handling**

**CMS-028 — Structured API responses**

**CMS-029 — Production logging improvements**

Expand the Serilog foundation established in CMS-002 with production-ready logging concerns, such as request logging, correlation/trace identifiers where useful, log-level review, and verification that sensitive data remains excluded.

**CMS-030 — API documentation**

**CMS-031 — Unit tests**

**CMS-032 — Integration tests**

**CMS-033 — Dashboard endpoints**

**CMS-034 — Final code review**

**CMS-035 — Deployment preparation**

Acceptance Criteria:

- API is documented.
- Important business rules are tested.
- Errors are handled consistently.
- Logging is available.
- Application can be prepared for deployment.

---

# 28. Working Rules

You are not expected to know everything before starting.

When you encounter something you don't know:

1. Identify exactly what you don't understand.
2. Research it.
3. Implement it.
4. Test it.
5. Explain why you chose your solution.

You are allowed to use:

- Official documentation.
- Stack Overflow.
- GitHub.
- Technical articles.
- AI tools.

However, you must understand the code you submit.

---

# 29. Developer Challenge Rules

For each task:

1. Read the requirements.
2. Break the task into smaller steps.
3. Design your solution.
4. Implement it.
5. Test it.
6. Commit it.
7. Report what you completed.

When asking for help, do not immediately send:

> "Give me the code."

Instead explain:

- What you are trying to do.
- What you tried.
- What happened.
- The error message if there is one.
- What you think the problem is.

The goal is to develop problem-solving ability, not just produce code.

---

# 30. Client Acceptance Process

For every task, the Team Lead will review:

- Correctness.
- Architecture.
- Code quality.
- Naming.
- Security.
- Edge cases.
- Error handling.
- Database design.
- Performance where relevant.
- Tests.

A task may be rejected even if it "works" if the implementation creates serious technical debt.

---

# 31. First Assignment

## CMS-001 — Create Solution Structure

### Priority

High

### Type

Backend / Architecture

### Objective

Set up the initial solution structure for the Clinic Management System.

### Requirements

Create a solution containing the required layers:

```text
API
Application
Domain
Infrastructure
Tests
```

Configure the project references so that dependencies follow a clean direction.

The API should start successfully.

Create a simple health endpoint:

```http
GET /api/health
```

Expected successful response:

```text
200 OK
```

The response should indicate that the API is running.

### Acceptance Criteria

- [ ] Solution created.
- [ ] Required projects created.
- [ ] Project references are correct.
- [ ] API starts successfully.
- [ ] `GET /api/health` works.
- [ ] Swagger is available.
- [ ] `.gitignore` exists.
- [ ] Initial Git commit created.
- [ ] Code is pushed to GitHub.

### Deliverable

Send the Team Lead:

1. GitHub repository.
2. Screenshot or response of `/api/health`.
3. Short explanation of the architecture you created.
4. Commit hash.

---

# 32. Important

Do **not** start Sprint 2 until Sprint 1 is reviewed.

The Team Lead will review your implementation and may:

- Accept the task.
- Ask for changes.
- Add an edge case.
- Give you a debugging task.
- Ask you to refactor part of the implementation.

This is intentional.

The objective is to simulate a real development environment rather than simply completing a tutorial.

---

# 33. Future Difficulty Levels

After completing this project, future projects will introduce:

### Level 1
CRUD + REST API + EF Core

### Level 2
Authentication + Authorization + Business Rules

### Level 3
Clean Architecture + Testing + Design Patterns

### Level 4
Caching + Background Jobs + External APIs

### Level 5
Payment Gateway + Notifications + File Storage

### Level 6
Docker + CI/CD + Cloud Deployment

### Level 7
Distributed systems + Microservices + Message Brokers

---

# Project Status

```text
Sprint: 1 / 6
Current Task: CMS-001
Status: NOT STARTED
```

## Team Lead Note

You are now officially assigned to **CMS-001**.

Do not implement the whole project at once.

**Start with CMS-001 only.**

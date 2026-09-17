# FixFlow AI — Intelligent Apartment Maintenance Management System

> **SE3090 – Software Engineering Frameworks, Assignment 1 (2026)**  
> **Faculty of Computing, Sri Lanka Institute of Information Technology (SLIIT)**

---

## 📌 Project Overview

**FixFlow AI** is a comprehensive software engineering platform designed to modernize residential apartment maintenance management for a single residential apartment complex. It features automated AI-driven request intake, risk-based priority scoring, multi-criteria technician assignment, and conflict-free work order scheduling across towers, floors, residential units, and common areas.

### Architecture Highlights

- **Strict API Gateway Architecture**:

  - `React Web App` → `ASP.NET Core 8 Web API` → `PostgreSQL`
  - `Flutter Mobile App` → `ASP.NET Core 8 Web API` → `PostgreSQL`
  - `ASP.NET Core 8 Web API` → `Internal Agentic AI Microservice (Python)`

- Neither React nor Flutter communicate directly with PostgreSQL or the internal Agentic AI service.

- **Merge-Conflict-Resistant Shared Foundation**: Contains 16 core shared foundation entities, JWT authentication, global middleware, distance calculation fallback, base UI themes, and agent orchestration.

---

## 👥 Student Ownership & Group Structure

Every team member has technical ownership across backend, database, web, mobile, AI, testing, git evidence, and documentation.

| Member | Business Component | Domain Focus | Feature Branch |
| :--- | :--- | :--- | :--- |
| **Member 1** | Request Intake & Classification | Issue classification, symptom capture, evidence tagging | `member-1/request-intake` |
| **Member 2** | Risk & Priority Assessment | Risk assessment, asset criticality, impact & likelihood evaluation, risk matrix & scoring, risk level, priority, SLA, escalation, and risk simulation | `member-2/priority` |
| **Member 3** | Technician Matching & Assignment | Skill matching, workload analysis, candidate ranking | `member-3/assignment` |
| **Member 4** | Scheduling & Work Order Management | Calendar booking, time-slot validation, status tracking | `member-4/scheduling` |

---

## 🚀 Quick Setup & Local Execution

### Prerequisites

Install the following software before running the project:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js (v18+)](https://nodejs.org/)
- [Flutter SDK (v3.19+)](https://flutter.dev/)
- [Python (v3.10+)](https://www.python.org/)
- [PostgreSQL](https://www.postgresql.org/)
- [pgAdmin 4](https://www.pgadmin.org/)

> **Note:** Docker is not required for this project. PostgreSQL is installed and managed locally using PostgreSQL and pgAdmin 4.

---

## 🗄️ Database Setup — PostgreSQL + pgAdmin

FixFlow AI uses **PostgreSQL** as its relational database.

The database is managed locally using **PostgreSQL** and **pgAdmin 4**.

### Step 1 — Install PostgreSQL

Install PostgreSQL on your local machine.

During installation, remember the following:

- PostgreSQL username
- PostgreSQL password
- PostgreSQL port

The default PostgreSQL port is:

```text
5432

```

### 2. Backend Web API (.NET 8)
```bash
cd backend/FixFlow.Api
dotnet restore
dotnet ef database update
dotnet run
```
API Swagger Documentation available at: `http://localhost:5000/swagger`

### 3. Frontend Web Application (React + Vite)
```bash
cd frontend/fixflow-web
npm install
npm run dev
```
Web app runs at `http://localhost:5173`

### 4. Mobile Application (Flutter)
```bash
cd mobile/fixflow_mobile
flutter pub get
flutter run
```

### 5. Agentic AI Service (Python)
```bash
cd agentic-ai
python -m venv venv
# On Windows:
venv\Scripts\activate
# On Linux/macOS:
source venv/bin/activate
pip install -r requirements.txt
uvicorn workflows.orchestrator:app --reload --port 8000
```

---

## 🔑 Demo User Accounts

The database seeder initializes 4 demo accounts:

| Role | Email | Password | Purpose |
| :--- | :--- | :--- | :--- |
| **Administrator** | `admin@fixflow.local` | `Admin123!` | System configuration, user management, audit logs |
| **Manager** | `manager@fixflow.local` | `Manager123!` | Dispatch board, human approvals, risk review |
| **Technician** | `tech@fixflow.local` | `Tech123!` | Mobile task execution, work order updates |
| **Requester** | `requester@fixflow.local` | `Requester123!` | Request submission, tracking, notifications |

---

## 🧪 Testing Commands

- **Backend**: `dotnet test backend/FixFlow.Tests/FixFlow.Tests.csproj`
- **React Web**: `npm test` inside `frontend/fixflow-web`
- **Flutter Mobile**: `flutter test` inside `mobile/fixflow_mobile`
- **Agentic AI**: `pytest` inside `agentic-ai`

---

## 📄 Documentation Directory
- [System Architecture](docs/architecture/system-architecture.md)
- [Git & Branching Strategy](docs/architecture/git-workflow.md)
- [Database Migration Strategy](docs/architecture/database-migration-strategy.md)
- [API Specification](docs/api/api-specification.md)
- [Local Setup Guide](docs/deployment/local-setup.md)
- [Testing Strategy](docs/testing/testing-strategy.md)
- [Agentic AI Contracts](docs/agentic-ai/agent-contracts.md)
- [Architectural Decision Records (ADRs)](docs/adr/)
- [Member Contribution Evidence Templates](docs/contributions/)

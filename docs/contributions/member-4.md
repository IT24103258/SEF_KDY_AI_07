# Component 4: Scheduling & Work Order Management
**Author:** Member 4  
**Project:** FixFlow AI — Intelligent Maintenance Request Management System  
**Component:** Component 4 (Scheduling & Work Order Management)

---

## 1. Executive Summary & Ownership

Component 4 is responsible for turning classified, prioritized, and matched maintenance requests into conflict-free, executable work orders with verifiable field execution.

### Key Capabilities
1. **Deterministic Schedule Generation & Conflict Detection**: Evaluates technician calendar availability, operational business hours, existing work orders (using `existingStart < proposedEnd && existingEnd > proposedStart`), SLA deadlines, and travel constraints.
2. **Scheduling Agent & Allow-Listed Tools**: Operates strictly within 5 allow-listed tools (`GetTechnicianCalendar`, `GetBusinessHours`, `GetExistingWorkOrders`, `CreateScheduleProposal`, `ValidateSchedule`) with structured Pydantic output and deterministic safety validation.
3. **Server-Side State Machine**: Authoritatively enforces valid status transitions:
   `Draft → Proposed → PendingManagerApproval → Approved → Scheduled → InProgress → Paused → Completed`
   with appropriate `Rejected` and `RevisionRequested` branching.
4. **Human-in-the-Loop Manager Approval Center**: Displays AI Decision Summary with auditable verification facts (no hidden chain-of-thought).
5. **Real-Time Calendar & Operational Analytics**: Interactive week/month board and PostgreSQL-backed KPI reporting.
6. **Mobile Field Execution (Flutter)**: Daily schedule, job details, live execution timer, field work notes, and digital customer completion sign-off.

---

## 2. Architecture & File Responsibility Mapping

| Layer | File | Responsibility |
|---|---|---|
| **Domain Models** | `backend/FixFlow.Api/Models/WorkOrderEntities.cs` | WorkOrder, WorkOrderStatusHistory, ScheduleProposal, WorkNote, CompletionEvidence, BusinessHours |
| **Enums** | `backend/FixFlow.Api/Models/Enums/WorkOrderEnums.cs` | `WorkOrderStatus` and `WorkOrderPriority` |
| **EF Configurations** | `backend/FixFlow.Api/Data/Configurations/WorkOrderConfiguration.cs` | Discovered by `ApplyConfigurationsFromAssembly` without modifying shared DbContext |
| **DTOs** | `backend/FixFlow.Api/DTOs/WorkOrderDtos.cs`, `SchedulingDtos.cs` | Structured request/response envelopes |
| **Validators** | `backend/FixFlow.Api/Validators/WorkOrderValidators.cs` | FluentValidation rules for all inputs |
| **Services** | `backend/FixFlow.Api/Services/SchedulingService.cs` | Deterministic conflict detection, SLA validation, slot search |
| **Services** | `backend/FixFlow.Api/Services/WorkOrderService.cs` | CRUD, role authorization, state machine, approval & completion |
| **Controllers** | `backend/FixFlow.Api/Controllers/WorkOrdersController.cs` | REST endpoints for Work Order lifecycle |
| **Controllers** | `backend/FixFlow.Api/Controllers/SchedulingControllers.cs` | Scheduling proposals, validation, calendar & reporting |
| **Seed Data** | `backend/FixFlow.Api/Data/Seed/WorkOrderSeeder.cs` | Demo work orders across all statuses, conflict scenarios, notes & signatures |
| **Agentic AI Tools** | `agentic-ai/tools/domain_tools.py` | 5 allow-listed scheduling tools in registry |
| **Agentic AI Skeletons**| `agentic-ai/agents/agent_skeletons.py` | Full `SchedulingAgent` implementation |
| **Agentic AI Validator**| `agentic-ai/validators/deterministic_validator.py` | Deterministic safety and approval checking |
| **React Web** | `frontend/fixflow-web/src/pages/work-orders/*` | List with pagination/search/filters & detailed timeline view |
| **React Web** | `frontend/fixflow-web/src/pages/approval/ApprovalCenterPage.jsx` | Manager review queue with AI Decision Summary |
| **React Web** | `frontend/fixflow-web/src/pages/calendar/CalendarPage.jsx` | Interactive schedule & dispatch board |
| **React Web** | `frontend/fixflow-web/src/pages/reports/WorkOrderReportsPage.jsx` | Operational analytics, workload & compliance KPIs |
| **Flutter Mobile** | `mobile/fixflow_mobile/lib/screens/technician_schedule_screen.dart` | Daily schedule & day selector |
| **Flutter Mobile** | `mobile/fixflow_mobile/lib/screens/job_details_screen.dart` | Job scope, notes, start job button |
| **Flutter Mobile** | `mobile/fixflow_mobile/lib/screens/job_execution_screen.dart` | Active timer, checklist, custom digital signature pad |

---

## 3. Deterministic Validation Rules

1. **Overlap Conflict Rule**:
   A proposed slot `[P_start, P_end]` conflicts with an existing booking `[E_start, E_end]` if and only if:
   $$\text{Conflict} \iff (E_{\text{start}} < P_{\text{end}}) \land (E_{\text{end}} > P_{\text{start}})$$
2. **Business Hours Rule**:
   - Monday–Friday: 08:00 to 17:00
   - Saturday: 08:00 to 13:00
   - Sunday: Closed
3. **SLA Target Rule**:
   Critical: 4 hours | High: 8 hours | Medium: 24 hours | Low: 48 hours
4. **State Machine Rule**:
   Only valid transitions are accepted. Direct jumps from `Draft` to `Approved` or `Completed` are rejected server-side with `InvalidOperationException`.

---

## 4. Test Evidence

### Backend Tests (`FixFlow.Tests`)
- **16 passed in 18 seconds** (0 failed)
- Tests include: conflict detection, business hours rejection, SLA breach, state transitions, manager approval/rejection, customer sign-off, technician role scoping, pagination and filtering.

### Agentic AI Golden Tests (`pytest`)
- **16 passed in 0.72 seconds** (0 failed)
- 12 golden test cases: valid scheduling, overlap detection, business hours, unavailable technician, SLA violation, safe failure fallback, invalid tool inputs, prompt-injection defense, approval bypass prevention, schema verification, tool call observability, and concurrency boundary checks.

### Frontend Web Tests (`vitest`)
- **5 passed in 11.27 seconds** (0 failed)
- Tests include: WorkOrdersPage render and search, ApprovalCenter proposal cards and verification facts, Calendar board controls, Reports KPI metrics.

### Mobile Flutter Tests (`flutter test`)
- Unit and widget tests verifying JSON serialization, provider state transitions, and schedule screen rendering.

---

## 5. Demo Guide for Manager & Technician

### Manager Workflow (React Web)
1. Login with demo account: `manager@fixflow.local` / `Manager123!`
2. Navigate to **Work Orders** (`/work-orders`) to view all active jobs with search, filtering by status/priority, and pagination.
3. Open **Approval Center** (`/approval-center`) to review pending AI proposals, auditable verification checklist, and conflict warnings.
4. Click **Approve & Dispatch** with manager comments.
5. Open **Calendar** (`/calendar`) to view the scheduled work order block on the interactive board.
6. Open **Reports** (`/reports/scheduling`) to inspect real-time KPIs, SLA compliance rate, and technician workload distribution.

### Technician Workflow (Flutter Mobile)
1. Login with technician account: `tech@fixflow.local` / `Tech123!`
2. View **My Daily Schedule** with horizontal day selector.
3. Tap on assigned work order to view **Job Details** and field notes.
4. Tap **Start Job Execution** (status transitions to `In Progress`).
5. Proceed to **Job Execution & Sign-off**:
   - Live timer tracks duration
   - Complete field checklist
   - Capture customer digital signature on the interactive pad
   - Tap **Submit Customer Sign-off & Complete Job** (status transitions to `Completed`).

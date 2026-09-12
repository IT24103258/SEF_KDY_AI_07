# Git & Branching Strategy — Team Ownership Guide

## Branching Model

```
main (Production Ready)
 └── feature/shared-foundation (Initial Base)
      ├── member-1/request-intake (Member 1 - Intake & Classification)
      ├── member-2/priority (Member 2 - Risk & Priority Assessment)
      ├── member-3/assignment (Member 3 - Technician Matching & Assignment)
      └── member-4/scheduling (Member 4 - Scheduling & Work Orders)
```

## Student Technical Ownership Guidelines

Each student must demonstrate individual contribution evidence across **all technical dimensions**:

1. **Member 1 (Request Intake & Classification)**
   - Backend: `IssueClassificationService`, `MaintenanceRequestsController` extension.
   - React: Intake form, category selector, request history page.
   - Flutter: Intake screen, camera/photo capture widget, request tracking.
   - Agent: `ClassificationAgent`.
   - Tests: Unit tests for classification logic & form validation.

2. **Member 2 (Risk & Priority Assessment)**
   - Backend: `PriorityAssessmentService`, `PriorityController`, SLA evaluation.
   - React: Risk dashboard, priority escalation queue, SLA violation tracker.
   - Flutter: Priority badge, hazard indicator widget.
   - Agent: `PriorityAgent`.
   - Tests: Priority calculation & SLA breach unit tests.

3. **Member 3 (Technician Matching & Assignment)**
   - Backend: `TechnicianMatchingService`, `AssignmentsController`, Workload calculator.
   - React: Technician dispatch board, skill-match comparison table, reassignment modal.
   - Flutter: Technician job list screen, job acceptance/decline widget.
   - Agent: `AssignmentAgent`.
   - Tests: Skill-distance multi-criteria matching tests.

4. **Member 4 (Scheduling & Work Order Management)**
   - Backend: `SchedulingService`, `WorkOrderService`, `WorkOrdersController`.
   - React: Scheduling calendar, time-slot picker, rescheduling drawer.
   - Flutter: Work order checklist, status transitions, work notes log.
   - Agent: `SchedulingAgent`.
   - Tests: Time-slot conflict detection unit tests.

## Pull Request Rules
- All PRs must target `main`.
- Include screenshots/logs of passing local tests in PR comments.
- Do NOT squash commits if individual commit history is required for grading evidence.

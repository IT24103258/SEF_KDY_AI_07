# Scheduling Agent Interface Contract (Component 4)
**Component Owner:** Member 4 (Scheduling & Work Order Management)  
**Agent Name:** `SchedulingAgent`  
**Pipeline Step:** Step 4 (Conflict-Free Work Order Scheduling)

---

## 1. Input Context Contract

| Field | Type | Description | Mandatory |
|---|---|---|---|
| `request_id` | string (UUID) | Maintenance request reference | Yes |
| `assigned_technician_id` / `technician_id` | string (UUID) | Technician assigned by Component 3 matching | Yes |
| `priority` / `priority_level` | string | Priority assigned by Component 2 | Yes |
| `estimated_duration_minutes` | integer | Estimated repair duration (15–1440 mins) | No (default: 60) |
| `preferred_start_time` | string (ISO 8601) | Requester preferred schedule slot | No |
| `sla_deadline` | string (ISO 8601) | Calculated SLA resolution deadline | No |

---

## 2. Allow-Listed Tools (Strict Registry)

Only the following 5 tools are authorized for invocation by the `SchedulingAgent`:

1. `GetTechnicianCalendar` (inspects shift windows & active assignments)
2. `GetBusinessHours` (retrieves open/close business hours)
3. `GetExistingWorkOrders` (retrieves active bookings to detect overlaps)
4. `CreateScheduleProposal` (formats the structured proposal)
5. `ValidateSchedule` (runs deterministic checklist)

Any call to unauthorized tools is intercepted and rejected with `Unauthorized tool invocation`.

---

## 3. Output Schema Contract (`ScheduleProposal`)

```json
{
  "request_id": "REQ-2026-0001",
  "technician_id": "TECH-001",
  "assigned_technician_id": "TECH-001",
  "proposed_start": "2026-09-24T14:00:00Z",
  "proposed_end": "2026-09-24T16:00:00Z",
  "proposed_start_time": "2026-09-24T14:00:00Z",
  "proposed_end_time": "2026-09-24T16:00:00Z",
  "estimated_duration_minutes": 120,
  "priority": "High",
  "sla_deadline": "2026-09-24T18:00:00Z",
  "conflict_detected": false,
  "conflict_details": [],
  "within_business_hours": true,
  "within_technician_availability": true,
  "sla_compliant": true,
  "proposal_status": "Proposed",
  "decision_summary": "Selected an available technician slot within business hours and before the SLA deadline. Existing bookings were checked and no overlapping booking was detected.",
  "validation_required": true,
  "is_conflict_free": true
}
```

---

## 4. Deterministic Validation & Human Approval Gates

1. **Deterministic Rule Authority**: The AI generates a proposal; the server-side validator has authoritative decision rights.
2. **Human-in-the-Loop Requirement**:
   - All high-impact and critical proposals require Manager sign-off.
   - Any proposal with `conflict_detected: true` triggers an approval block with red warning and alternative slot suggestion.
3. **No Hidden Chain-of-Thought**: Only concise, auditable decision facts are persisted and exposed to the Manager in the Approval Center.

# FixFlow AI — Database Documentation & Schema Strategy

## Overview
FixFlow AI uses PostgreSQL 16 managed strictly through Entity Framework Core (EF Core 8) Code-First Migrations.

## Rules for Database Schema Evolution
1. **Authoritative Mechanism**: EF Core Migrations are the single source of truth. Do NOT create or maintain manually executed SQL scripts (`init-db.sql`) alongside EF migrations.
2. **Shared Foundation DbContext**: `FixFlowDbContext.cs` contains ONLY the 16 shared foundation entities.
3. **Student Entity Configurations**: When adding student-owned entities (Members 1–4):
   - Do NOT edit `FixFlowDbContext.cs`.
   - Add a new configuration class in `Data/Configurations/<EntityName>Configuration.cs` implementing `IEntityTypeConfiguration<T>`.
   - EF Core automatically discovers configurations via `modelBuilder.ApplyConfigurationsFromAssembly(typeof(FixFlowDbContext).Assembly)`.
4. **Creating Migrations on Student Branches**:
   ```bash
   dotnet ef migrations add AddMember1RequestClassification --project backend/FixFlow.Api
   dotnet ef database update --project backend/FixFlow.Api
   ```

## Shared Foundation Entities
- `BaseEntity` (Id, CreatedAt, UpdatedAt, IsDeleted)
- `User` & `Role` (Authentication & RBAC)
- `Location` & `Asset` (Campus infrastructure mapping)
- `MaintenanceRequest` & `IssueCategory` (Intake context)
- `Technician` & `Skill` (Resource capabilities)
- `Notification` (Alerts)
- `AgentWorkflow`, `AgentStep`, `AgentToolCall`, `ApprovalAction` (Agentic AI state persistence & human-in-the-loop)
- `AuditLog` & `SLAConfiguration` (Governance & compliance)

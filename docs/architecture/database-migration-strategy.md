# Database Migration Strategy

## Principles
1. **EF Core Code-First**: All schema changes are generated via EF Core migrations (`dotnet ef migrations add`).
2. **No Shared DbContext Modification**: Students do NOT add `DbSet`s to `FixFlowDbContext.cs`.
3. **Automatic Configuration Registration**: EF Core automatically registers any class inheriting `IEntityTypeConfiguration<T>` in `Data/Configurations/`.
4. **Independent Student Migrations**: Each student creates a distinct migration on their branch when adding their entities.

## Workflow Example for Student Branch
```bash
# 1. Create student entity and configuration file in Data/Configurations/
# 2. Add EF Migration
dotnet ef migrations add AddMember1Entities --project backend/FixFlow.Api

# 3. Apply migration to local PostgreSQL container
dotnet ef database update --project backend/FixFlow.Api
```

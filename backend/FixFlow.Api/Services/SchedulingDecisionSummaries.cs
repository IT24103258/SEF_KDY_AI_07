namespace FixFlow.Api.Services;

public class SchedulingSummaryInput
{
    public string TechnicianName { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
    public bool HighImpactPriority { get; set; }
    public DateTime ProposedStartUtc { get; set; }
    public DateTime ProposedEndUtc { get; set; }
    public DateTime? SlaDeadlineUtc { get; set; }
    public bool SlaBreached { get; set; }
    public int SearchDays { get; set; } = 7;
    public bool ConflictDetected { get; set; }
    public bool OutsideBusinessHours { get; set; }
    public bool TechnicianUnavailable { get; set; }
    public bool NoSlotFound { get; set; }
    public bool AlternativeSlotFound { get; set; }
    public bool AutoScheduled { get; set; }
    public bool HeldSlotIsUsable { get; set; }
}

public static class SchedulingDecisionSummaries
{
    public static string Build(SchedulingSummaryInput input)
    {
        var slot = FormatSlotRange(input.ProposedStartUtc, input.ProposedEndUtc);
        var priorityNote = input.HighImpactPriority
            ? $" {input.Priority} priority also requires Manager sign-off."
            : string.Empty;

        if (input.AutoScheduled)
        {
            var summary = input.TechnicianUnavailable
                ? $"Technician {input.TechnicianName} was not available at the requested time. The nearest conflict-free slot {slot} was selected and the work order is scheduled."
                : input.AlternativeSlotFound
                    ? $"The requested time for technician {input.TechnicianName} was already booked. The nearest conflict-free slot {slot} was selected and the work order is scheduled."
                    : $"Technician {input.TechnicianName} is scheduled {slot}. The slot is within operating hours and has no booking conflicts.";

            if (input.SlaDeadlineUtc.HasValue)
            {
                summary += input.SlaBreached
                    ? $" Note: the work would finish after the SLA deadline ({FormatLocal(input.SlaDeadlineUtc.Value)}); expedited completion may be needed."
                    : $" The work is scheduled to finish before the SLA deadline ({FormatLocal(input.SlaDeadlineUtc.Value)}).";
            }

            return summary;
        }

        if (input.NoSlotFound)
        {
            var summary = input.TechnicianUnavailable
                ? $"Technician {input.TechnicianName} is unavailable and no valid slot was found within operating hours over the next {input.SearchDays} days."
                : input.ConflictDetected
                    ? $"The requested time for technician {input.TechnicianName} overlaps an existing booking and no conflict-free slot could be found within operating hours over the next {input.SearchDays} days."
                    : input.OutsideBusinessHours
                        ? $"The requested time falls outside operating hours for technician {input.TechnicianName} and no valid slot was found within operating hours over the next {input.SearchDays} days."
                        : $"No conflict-free slot within operating hours could be found for technician {input.TechnicianName} over the next {input.SearchDays} days."
                          + (input.SlaDeadlineUtc.HasValue ? " The SLA deadline cannot be met with the current technician availability." : string.Empty);

            summary += input.HeldSlotIsUsable
                ? $" A provisional slot {slot} has been held and requires Manager review to confirm or adjust."
                : " Manager review is required to schedule the work manually.";
            return summary;
        }

        if (input.TechnicianUnavailable)
        {
            return $"Technician {input.TechnicianName} was not available at the requested time. An alternative conflict-free slot {slot} has been proposed and awaits Manager approval.{priorityNote}";
        }

        if (input.ConflictDetected)
        {
            return $"The requested time for technician {input.TechnicianName} overlaps an existing booking. An alternative conflict-free slot {slot} has been proposed and awaits Manager approval.{priorityNote}";
        }

        if (input.OutsideBusinessHours)
        {
            return $"The requested time falls outside operating hours for technician {input.TechnicianName}. The next valid operational slot {slot} has been proposed and awaits Manager approval.{priorityNote}";
        }

        if (input.SlaBreached)
        {
            var slaText = input.SlaDeadlineUtc.HasValue
                ? $"The proposed slot {slot} for technician {input.TechnicianName} completes after the SLA deadline ({FormatLocal(input.SlaDeadlineUtc.Value)}). Manager approval is required to confirm the schedule or arrange expedited completion.{priorityNote}"
                : $"The proposed slot {slot} for technician {input.TechnicianName} completes after the SLA deadline. Manager approval is required to confirm the schedule or arrange expedited completion.{priorityNote}";
            return slaText;
        }

        return $"{input.Priority} priority work order for technician {input.TechnicianName}. A conflict-free slot {slot} has been reserved and awaits Manager approval before the schedule is confirmed.";
    }

    private static string FormatSlotRange(DateTime startUtc, DateTime endUtc)
    {
        var startLocal = SchedulingService.ConvertToLocal(startUtc);
        var endLocal = SchedulingService.ConvertToLocal(endUtc);

        if (startLocal.Date == endLocal.Date)
            return $"on {startLocal:ddd, dd MMM yyyy} from {startLocal:HH:mm} to {endLocal:HH:mm}";

        return $"from {startLocal:ddd, dd MMM yyyy} {startLocal:HH:mm} to {endLocal:ddd, dd MMM yyyy} {endLocal:HH:mm}";
    }

    private static string FormatLocal(DateTime utc) => SchedulingService.ConvertToLocal(utc).ToString("ddd, dd MMM yyyy HH:mm");
}

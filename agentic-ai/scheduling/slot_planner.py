import logging
from dataclasses import dataclass, field
from datetime import datetime, timedelta, timezone
from typing import Any, Dict, List, Optional, Tuple

logger = logging.getLogger(__name__)

IST = timezone(timedelta(hours=5, minutes=30))

DAY_NAMES = [
    "Monday", "Tuesday", "Wednesday", "Thursday",
    "Friday", "Saturday", "Sunday",
]

MORNING_START_HOUR = 8
MORNING_END_HOUR = 12
AFTERNOON_START_HOUR = 12
AFTERNOON_END_HOUR = 17
SLOT_STEP_MINUTES = 30


@dataclass
class SchedulingContext:
    technician_id: str
    duration_minutes: int
    priority: str = "Medium"
    preferred_start: Optional[datetime] = None
    preferred_end: Optional[datetime] = None
    sla_deadline: Optional[datetime] = None
    is_technician_available: bool = True
    shift_start: str = "08:00:00"
    shift_end: str = "17:00:00"
    working_days: Optional[List[str]] = None
    weekday_open: str = "08:00:00"
    weekday_close: str = "17:00:00"
    saturday_close: str = "13:00:00"
    is_working_day: bool = True
    existing_bookings: Optional[List[Dict[str, Any]]] = None
    preference_type: str = "flexible"
    preferred_period: Optional[str] = None
    urgency: str = "normal"
    avoid_periods: Optional[List[str]] = None
    flexibility: str = "flexible"
    requested_date: Optional[str] = None
    search_start: Optional[datetime] = None
    search_end: Optional[datetime] = None

    def get_working_days(self) -> List[str]:
        return self.working_days or ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"]

    def get_existing_bookings(self) -> List[Dict[str, Any]]:
        return self.existing_bookings or []


@dataclass
class SlotCandidate:
    start: datetime
    end: datetime
    rank_score: int = 0
    reason: str = ""


@dataclass
class SlotSearchResult:
    found: bool = False
    start: Optional[datetime] = None
    end: Optional[datetime] = None
    conflict_free: bool = False
    within_business_hours: bool = False
    within_technician_availability: bool = False
    sla_compliant: bool = False
    duration_minutes: int = 0
    candidates_evaluated: int = 0
    rejection_reasons: List[str] = field(default_factory=list)
    strategy_used: str = ""


class DeterministicSlotPlanner:

    def search(self, ctx: SchedulingContext) -> SlotSearchResult:
        if ctx.duration_minutes <= 0:
            return SlotSearchResult(
                found=False,
                rejection_reasons=["Duration must be positive."],
                strategy_used="rejected",
            )

        if not ctx.is_technician_available:
            return SlotSearchResult(
                found=False,
                within_technician_availability=False,
                rejection_reasons=["Technician is unavailable."],
                strategy_used="rejected",
            )

        candidates = self._generate_candidates(ctx)
        if not candidates:
            return SlotSearchResult(
                found=False,
                candidates_evaluated=0,
                rejection_reasons=["No candidate slots could be generated."],
                strategy_used="no_candidates",
            )

        ranked = self._rank_candidates(candidates, ctx)
        rejection_reasons = []

        for cand in ranked:
            bh_ok = self._check_business_hours(cand.start, cand.end, ctx)
            tech_ok = self._check_technician_shift(cand.start, cand.end, ctx)
            conflict_ok, conflict_info = self._check_conflicts(cand.start, cand.end, ctx)
            sla_ok = self._check_sla(cand.end, ctx)

            if bh_ok and tech_ok and conflict_ok and sla_ok:
                return SlotSearchResult(
                    found=True,
                    start=cand.start,
                    end=cand.end,
                    conflict_free=True,
                    within_business_hours=True,
                    within_technician_availability=True,
                    sla_compliant=True,
                    duration_minutes=ctx.duration_minutes,
                    candidates_evaluated=len(ranked),
                    strategy_used=cand.reason or "deterministic_search",
                )

            reasons = []
            if not bh_ok:
                reasons.append("Outside business hours")
            if not tech_ok:
                reasons.append("Outside technician shift")
            if not conflict_ok:
                reasons.append(f"Conflict: {conflict_info}")
            if not sla_ok:
                reasons.append("SLA breach")
            rejection_reasons.extend(reasons)

        return SlotSearchResult(
            found=False,
            candidates_evaluated=len(ranked),
            rejection_reasons=rejection_reasons,
            strategy_used="exhausted",
        )

    def _generate_candidates(self, ctx: SchedulingContext) -> List[SlotCandidate]:
        candidates = []
        duration = timedelta(minutes=ctx.duration_minutes)

        ref_dt = ctx.preferred_start or ctx.search_start
        if ref_dt is None:
            ref_dt = datetime.now(tz=IST)

        search_start = ctx.search_start or ref_dt
        search_end = ctx.search_end
        if search_end is None and ctx.sla_deadline is not None:
            search_end = ctx.sla_deadline
        if search_end is None:
            search_end = search_start + timedelta(days=7)

        if ctx.preferred_start and ctx.preferred_end:
            if ctx.preference_type not in ("morning", "afternoon"):
                exact_start = ctx.preferred_start
                exact_end = exact_start + duration
                candidates.append(SlotCandidate(
                    start=exact_start, end=exact_end,
                    rank_score=1000, reason="exact_preference",
                ))

        if ctx.preferred_period == "morning" or ctx.preference_type == "morning":
            morning_search_start = search_start
            if search_start.hour >= MORNING_END_HOUR:
                next_day = search_start.date() + timedelta(days=1)
                morning_search_start = datetime(
                    next_day.year, next_day.month, next_day.day,
                    MORNING_START_HOUR, 0, 0, tzinfo=IST,
                )
            morning_candidates = self._generate_period_candidates(
                morning_search_start, MORNING_START_HOUR, MORNING_END_HOUR,
                duration, morning_search_start, search_end, rank_base=800,
            )
            candidates.extend(morning_candidates)

        if ctx.preferred_period == "afternoon" or ctx.preference_type == "afternoon":
            afternoon_candidates = self._generate_period_candidates(
                ref_dt, AFTERNOON_START_HOUR, AFTERNOON_END_HOUR,
                duration, search_start, search_end, rank_base=800,
            )
            candidates.extend(afternoon_candidates)

        if ctx.preference_type == "earliest" or ctx.urgency == "urgent":
            earliest_candidates = self._generate_earliest_candidates(
                search_start, search_end, duration, rank_base=900,
            )
            candidates.extend(earliest_candidates)

        if ctx.preference_type not in ("morning", "afternoon"):
            flexible_candidates = self._generate_flexible_candidates(
                search_start, search_end, duration, rank_base=500,
            )
            candidates.extend(flexible_candidates)

        return candidates

    def _generate_period_candidates(
        self, ref_dt: datetime, period_start_hour: int, period_end_hour: int,
        duration: timedelta, search_start: datetime, search_end: datetime,
        rank_base: int,
    ) -> List[SlotCandidate]:
        candidates = []
        day = ref_dt.date()
        for day_offset in range(7):
            current_day = day + timedelta(days=day_offset)
            for hour in range(period_start_hour, period_end_hour):
                for minute in [0, 30]:
                    dt_start = datetime(
                        current_day.year, current_day.month, current_day.day,
                        hour, minute, 0, tzinfo=IST,
                    )
                    if dt_start < search_start:
                        continue
                    if dt_start >= search_end:
                        break
                    dt_end = dt_start + duration
                    rank = rank_base - day_offset * 10 - (hour * 60 + minute)
                    candidates.append(SlotCandidate(
                        start=dt_start, end=dt_end,
                        rank_score=rank, reason=f"{period_start_hour}-{period_end_hour}_window",
                    ))
        return candidates

    def _generate_earliest_candidates(
        self, search_start: datetime, search_end: datetime,
        duration: timedelta, rank_base: int,
    ) -> List[SlotCandidate]:
        candidates = []
        current = search_start
        step = timedelta(minutes=SLOT_STEP_MINUTES)
        max_slots = 200
        count = 0
        while current < search_end and count < max_slots:
            dt_end = current + duration
            rank = rank_base - count
            candidates.append(SlotCandidate(
                start=current, end=dt_end,
                rank_score=rank, reason="earliest_feasible",
            ))
            current += step
            count += 1
        return candidates

    def _generate_flexible_candidates(
        self, search_start: datetime, search_end: datetime,
        duration: timedelta, rank_base: int,
    ) -> List[SlotCandidate]:
        candidates = []
        current = search_start
        step = timedelta(minutes=SLOT_STEP_MINUTES)
        max_slots = 200
        count = 0
        while current < search_end and count < max_slots:
            dt_end = current + duration
            rank = rank_base - count
            candidates.append(SlotCandidate(
                start=current, end=dt_end,
                rank_score=rank, reason="flexible_search",
            ))
            current += step
            count += 1
        return candidates

    def _rank_candidates(
        self, candidates: List[SlotCandidate], ctx: SchedulingContext,
    ) -> List[SlotCandidate]:
        seen = set()
        unique = []
        for c in candidates:
            key = (c.start, c.end)
            if key not in seen:
                seen.add(key)
                unique.append(c)

        unique.sort(key=lambda c: (-c.rank_score, c.start))
        return unique

    def _check_business_hours(
        self, start: datetime, end: datetime, ctx: SchedulingContext,
    ) -> bool:
        day_name = DAY_NAMES[start.weekday()]
        working_days = ctx.get_working_days()

        if day_name not in working_days:
            return False

        open_h, open_m = _parse_hm(ctx.weekday_open)
        if day_name == "Saturday":
            close_h, close_m = _parse_hm(ctx.saturday_close)
        else:
            close_h, close_m = _parse_hm(ctx.weekday_close)

        biz_open = start.replace(hour=open_h, minute=open_m, second=0, microsecond=0)
        biz_close = start.replace(hour=close_h, minute=close_m, second=0, microsecond=0)

        if start < biz_open:
            return False
        if end > biz_close:
            return False
        return True

    def _check_technician_shift(
        self, start: datetime, end: datetime, ctx: SchedulingContext,
    ) -> bool:
        if not ctx.is_technician_available:
            return False

        day_name = DAY_NAMES[start.weekday()]
        working_days = ctx.get_working_days()
        if day_name not in working_days:
            return False

        shift_start_h, shift_start_m = _parse_hm(ctx.shift_start)
        shift_end_h, shift_end_m = _parse_hm(ctx.shift_end)

        shift_start_dt = start.replace(hour=shift_start_h, minute=shift_start_m, second=0, microsecond=0)
        shift_end_dt = start.replace(hour=shift_end_h, minute=shift_end_m, second=0, microsecond=0)

        if start < shift_start_dt:
            return False
        if end > shift_end_dt:
            return False
        return True

    def _check_conflicts(
        self, start: datetime, end: datetime, ctx: SchedulingContext,
    ) -> Tuple[bool, str]:
        for booking in ctx.get_existing_bookings():
            b_start = _parse_booking_time(booking, "start")
            b_end = _parse_booking_time(booking, "end")
            if b_start is None or b_end is None:
                continue
            if b_start < end and b_end > start:
                wo_id = booking.get("work_order_id", "existing booking")
                return False, f"Overlap with {wo_id}"
        return True, ""

    def _check_sla(self, end: datetime, ctx: SchedulingContext) -> bool:
        if ctx.sla_deadline is None:
            return True
        return end <= ctx.sla_deadline


def _parse_hm(time_str: str) -> Tuple[int, int]:
    parts = time_str.split(":")
    return int(parts[0]), int(parts[1]) if len(parts) > 1 else 0


def _parse_booking_time(booking: Dict[str, Any], key: str) -> Optional[datetime]:
    val = booking.get(f"{key}_time") or booking.get(key)
    if val is None:
        return None
    if isinstance(val, datetime):
        if val.tzinfo is None:
            return val.replace(tzinfo=IST)
        return val
    if isinstance(val, str):
        return parse_iso_to_aware(val)
    return None


def parse_iso_to_aware(ts: str) -> Optional[datetime]:
    if not ts or not isinstance(ts, str):
        return None
    try:
        clean = ts.replace("Z", "+00:00")
        dt = datetime.fromisoformat(clean)
        if dt.tzinfo is None:
            dt = dt.replace(tzinfo=IST)
        return dt
    except (ValueError, TypeError):
        return None


def format_aware_dt(dt: datetime) -> str:
    return dt.isoformat()

import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { PageHeader, Card, Button, StatusBadge, LoadingState, EmptyState } from '../components/SharedUI';
import { workOrderApi } from '../services/workOrderApi';
import {
  Calendar as CalendarIcon,
  ChevronLeft,
  ChevronRight,
  Clock,
  User,
  MapPin,
  AlertTriangle,
  Eye,
  Filter,
  RefreshCw
} from 'lucide-react';

export const CalendarPage = () => {
  const navigate = useNavigate();

  const [currentDate, setCurrentDate] = useState(new Date());
  const [viewMode, setViewMode] = useState('week'); // 'week' or 'month'
  const [events, setEvents] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Filters
  const [technicians, setTechnicians] = useState([]);
  const [selectedTech, setSelectedTech] = useState('');
  const [selectedPriority, setSelectedPriority] = useState('');
  const [selectedStatus, setSelectedStatus] = useState('');

  // Selected event modal
  const [selectedEvent, setSelectedEvent] = useState(null);

  const fetchCalendar = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      // Calculate date window based on viewMode
      let start = new Date(currentDate);
      let end = new Date(currentDate);

      if (viewMode === 'week') {
        const day = start.getDay();
        start.setDate(start.getDate() - day); // Start at Sunday
        start.setHours(0, 0, 0, 0);
        end.setDate(start.getDate() + 6);
        end.setHours(23, 59, 59, 999);
      } else {
        start = new Date(currentDate.getFullYear(), currentDate.getMonth(), 1);
        end = new Date(currentDate.getFullYear(), currentDate.getMonth() + 1, 0, 23, 59, 59);
      }

      const res = await workOrderApi.getCalendarEvents({
        startDate: start.toISOString(),
        endDate: end.toISOString(),
        technicianId: selectedTech || undefined,
        priority: selectedPriority || undefined,
        status: selectedStatus || undefined
      });

      if (res?.success) {
        setEvents(res.data || []);
      }
    } catch (err) {
      setError(err.message || 'Failed to fetch calendar events.');
    } finally {
      setLoading(false);
    }
  }, [currentDate, viewMode, selectedTech, selectedPriority, selectedStatus]);

  useEffect(() => {
    fetchCalendar();
  }, [fetchCalendar]);

  useEffect(() => {
    workOrderApi.getUsers().then((res) => {
      if (res?.success) setTechnicians(res.data || []);
    }).catch(() => {});
  }, []);

  const handlePrev = () => {
    const next = new Date(currentDate);
    if (viewMode === 'week') {
      next.setDate(next.getDate() - 7);
    } else {
      next.setMonth(next.getMonth() - 1);
    }
    setCurrentDate(next);
  };

  const handleNext = () => {
    const next = new Date(currentDate);
    if (viewMode === 'week') {
      next.setDate(next.getDate() + 7);
    } else {
      next.setMonth(next.getMonth() + 1);
    }
    setCurrentDate(next);
  };

  const handleToday = () => {
    setCurrentDate(new Date());
  };

  // Generate Week Days (Sun - Sat)
  const getWeekDays = () => {
    const days = [];
    const curr = new Date(currentDate);
    const first = curr.getDate() - curr.getDay();

    for (let i = 0; i < 7; i++) {
      const nextDay = new Date(curr.setDate(first + i));
      days.push(new Date(nextDay));
    }
    return days;
  };

  const weekDays = getWeekDays();

  const getEventsForDay = (dayDate) => {
    return events.filter((ev) => {
      const evDate = new Date(ev.start);
      return (
        evDate.getFullYear() === dayDate.getFullYear() &&
        evDate.getMonth() === dayDate.getMonth() &&
        evDate.getDate() === dayDate.getDate()
      );
    });
  };

  const formattedMonthYear = currentDate.toLocaleString('default', { month: 'long', year: 'numeric' });

  return (
    <div>
      <PageHeader
        title="Schedule & Dispatch Board"
        description="Visual calendar of scheduled maintenance jobs, technician assignments & conflict tracking"
        action={
          <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
            <Button
              variant={viewMode === 'week' ? 'primary' : 'secondary'}
              size="sm"
              onClick={() => setViewMode('week')}
            >
              Week View
            </Button>
            <Button
              variant={viewMode === 'month' ? 'primary' : 'secondary'}
              size="sm"
              onClick={() => setViewMode('month')}
            >
              Month View
            </Button>
          </div>
        }
      />

      {/* Control Bar: Navigation & Filters */}
      <Card style={{ marginBottom: '1.25rem', padding: '1rem' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '1rem' }}>
          {/* Navigation Controls */}
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <Button variant="secondary" size="sm" onClick={handleToday}>
              Today
            </Button>
            <Button variant="secondary" size="sm" onClick={handlePrev} title="Previous">
              <ChevronLeft size={16} />
            </Button>
            <Button variant="secondary" size="sm" onClick={handleNext} title="Next">
              <ChevronRight size={16} />
            </Button>
            <span style={{ fontFamily: 'var(--font-display)', fontWeight: 700, fontSize: '1.1rem', marginLeft: '8px' }}>
              {formattedMonthYear}
            </span>
          </div>

          {/* Filters */}
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px', flexWrap: 'wrap' }}>
            <select
              className="ff-input"
              value={selectedTech}
              onChange={(e) => setSelectedTech(e.target.value)}
              style={{ minWidth: '160px', padding: '6px 10px', fontSize: '0.82rem' }}
            >
              <option value="">All Technicians</option>
              {technicians.map((t) => (
                <option key={t.id} value={t.id}>
                  {t.firstName} {t.lastName}
                </option>
              ))}
            </select>

            <select
              className="ff-input"
              value={selectedPriority}
              onChange={(e) => setSelectedPriority(e.target.value)}
              style={{ minWidth: '130px', padding: '6px 10px', fontSize: '0.82rem' }}
            >
              <option value="">All Priorities</option>
              <option value="Critical">Critical</option>
              <option value="High">High</option>
              <option value="Medium">Medium</option>
              <option value="Low">Low</option>
            </select>

            <select
              className="ff-input"
              value={selectedStatus}
              onChange={(e) => setSelectedStatus(e.target.value)}
              style={{ minWidth: '130px', padding: '6px 10px', fontSize: '0.82rem' }}
            >
              <option value="">All Statuses</option>
              <option value="Scheduled">Scheduled</option>
              <option value="InProgress">In Progress</option>
              <option value="PendingManagerApproval">Pending</option>
              <option value="Completed">Completed</option>
            </select>
          </div>
        </div>
      </Card>

      {/* Week Calendar Grid */}
      {loading ? (
        <LoadingState message="Loading schedule calendar events..." />
      ) : error ? (
        <Card>
          <div style={{ color: 'var(--danger-color)', padding: '1rem', textAlign: 'center' }}>
            <AlertTriangle size={24} style={{ marginBottom: '8px' }} />
            <p>{error}</p>
          </div>
        </Card>
      ) : (
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(7, minmax(140px, 1fr))',
            gap: '10px',
            overflowX: 'auto',
            paddingBottom: '1rem'
          }}
        >
          {weekDays.map((day, idx) => {
            const isToday = day.toDateString() === new Date().toDateString();
            const dayEvents = getEventsForDay(day);

            return (
              <div
                key={idx}
                className="ff-card"
                style={{
                  minHeight: '420px',
                  padding: '10px',
                  backgroundColor: isToday ? 'rgba(59, 130, 246, 0.04)' : 'var(--card-bg)',
                  border: isToday ? '1px solid var(--primary-color)' : '1px solid var(--border-color)',
                  display: 'flex',
                  flexDirection: 'column'
                }}
              >
                {/* Day Header */}
                <div
                  style={{
                    textAlign: 'center',
                    paddingBottom: '8px',
                    borderBottom: '1px solid var(--border-color)',
                    marginBottom: '8px'
                  }}
                >
                  <div style={{ fontSize: '0.75rem', fontWeight: 600, color: 'var(--text-secondary)', textTransform: 'uppercase' }}>
                    {day.toLocaleDateString('default', { weekday: 'short' })}
                  </div>
                  <div
                    style={{
                      fontSize: '1.15rem',
                      fontWeight: 700,
                      color: isToday ? 'var(--primary-color)' : 'var(--text-primary)',
                      marginTop: '2px'
                    }}
                  >
                    {day.getDate()}
                  </div>
                </div>

                {/* Event Chips List */}
                <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', flex: 1 }}>
                  {dayEvents.length === 0 ? (
                    <div style={{ color: 'var(--text-secondary)', fontSize: '0.75rem', textAlign: 'center', marginTop: '2rem', fontStyle: 'italic' }}>
                      No jobs
                    </div>
                  ) : (
                    dayEvents.map((ev) => {
                      const isCrit = ev.priority === 'Critical';
                      const isHigh = ev.priority === 'High';
                      const hasConflict = ev.conflictDetected;

                      return (
                        <div
                          key={ev.id}
                          onClick={() => setSelectedEvent(ev)}
                          style={{
                            padding: '8px 10px',
                            borderRadius: '6px',
                            backgroundColor: hasConflict
                              ? 'rgba(255, 107, 113, 0.12)'
                              : isCrit
                              ? 'rgba(255, 107, 113, 0.08)'
                              : isHigh
                              ? 'rgba(251, 191, 36, 0.08)'
                              : 'var(--glass-bg)',
                            borderLeft: `4px solid ${
                              hasConflict
                                ? 'var(--danger-color)'
                                : isCrit
                                ? 'var(--danger-color)'
                                : isHigh
                                ? 'var(--warning-color)'
                                : 'var(--primary-color)'
                            }`,
                            border: hasConflict ? '1px solid var(--danger-color)' : '1px solid var(--border-color)',
                            cursor: 'pointer',
                            fontSize: '0.78rem',
                            transition: 'transform 0.1s ease, box-shadow 0.1s ease'
                          }}
                        >
                          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '2px' }}>
                            <span style={{ fontWeight: 700, color: 'var(--primary-color)', fontSize: '0.75rem' }}>
                              {ev.workOrderNumber}
                            </span>
                            {hasConflict && (
                              <span title="Schedule Conflict Detected" style={{ color: 'var(--danger-color)' }}>
                                <AlertTriangle size={12} />
                              </span>
                            )}
                          </div>

                          <div style={{ fontWeight: 650, color: 'var(--text-primary)', marginBottom: '4px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                            {ev.title}
                          </div>

                          <div style={{ color: 'var(--text-secondary)', fontSize: '0.72rem', display: 'flex', alignItems: 'center', gap: '4px' }}>
                            <Clock size={11} />
                            {new Date(ev.start).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} -{' '}
                            {new Date(ev.end).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                          </div>

                          <div style={{ color: 'var(--text-secondary)', fontSize: '0.72rem', marginTop: '2px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                            👤 {ev.technicianName}
                          </div>
                        </div>
                      );
                    })
                  )}
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Event Details Quick Modal */}
      {selectedEvent && (
        <div
          style={{
            position: 'fixed',
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            backgroundColor: 'rgba(0, 0, 0, 0.6)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 9999,
            padding: '1rem'
          }}
          onClick={() => setSelectedEvent(null)}
        >
          <div
            className="ff-card glass-strong"
            style={{ maxWidth: '440px', width: '100%', padding: '1.5rem', borderRadius: '12px' }}
            onClick={(e) => e.stopPropagation()}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '1rem' }}>
              <div>
                <span style={{ fontWeight: 700, color: 'var(--primary-color)', fontSize: '1.1rem' }}>
                  {selectedEvent.workOrderNumber}
                </span>
                <h3 style={{ margin: '4px 0', fontSize: '1.1rem', fontWeight: 650 }}>{selectedEvent.title}</h3>
              </div>
              <StatusBadge status={selectedEvent.status} />
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', fontSize: '0.88rem', marginBottom: '1.25rem' }}>
              <div>
                <span style={{ color: 'var(--text-secondary)' }}>Technician: </span>
                <strong>{selectedEvent.technicianName}</strong>
              </div>
              <div>
                <span style={{ color: 'var(--text-secondary)' }}>Location: </span>
                <strong>{selectedEvent.locationName}</strong>
              </div>
              <div>
                <span style={{ color: 'var(--text-secondary)' }}>Time: </span>
                <strong>
                  {new Date(selectedEvent.start).toLocaleString()} -{' '}
                  {new Date(selectedEvent.end).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                </strong>
              </div>
              <div>
                <span style={{ color: 'var(--text-secondary)' }}>Priority: </span>
                <strong>{selectedEvent.priority}</strong>
              </div>
            </div>

            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px' }}>
              <Button variant="secondary" size="sm" onClick={() => setSelectedEvent(null)}>
                Close
              </Button>
              <Button
                variant="primary"
                size="sm"
                onClick={() => {
                  navigate(`/work-orders/${selectedEvent.id}`);
                }}
              >
                <Eye size={14} /> Full Details
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

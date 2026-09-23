import React from 'react';

export const RiskBadge = ({ level }) => {
  const getBadgeClass = (lvl) => {
    switch (lvl?.toLowerCase()) {
      case 'critical':
        return 'ff-badge ff-badge-danger';
      case 'high':
        return 'ff-badge ff-badge-warning';
      case 'medium':
        return 'ff-badge ff-badge-info';
      case 'low':
      default:
        return 'ff-badge ff-badge-success';
    }
  };

  return (
    <span className={getBadgeClass(level)} role="status" aria-label={`Risk Level: ${level || 'Unknown'}`}>
      {level || 'Unknown'}
    </span>
  );
};

export const PriorityBadge = ({ priority }) => {
  const getBadgeClass = (p) => {
    switch (p?.toLowerCase()) {
      case 'critical':
        return 'ff-badge ff-badge-danger';
      case 'high':
        return 'ff-badge ff-badge-warning';
      case 'medium':
        return 'ff-badge ff-badge-info';
      case 'low':
      default:
        return 'ff-badge ff-badge-success';
    }
  };

  return (
    <span className={getBadgeClass(priority)} role="status" aria-label={`Priority: ${priority || 'Normal'}`}>
      {priority || 'Normal'}
    </span>
  );
};

export const CriticalityBadge = ({ criticality }) => {
  const getStyle = (crit) => {
    switch (crit?.toLowerCase()) {
      case 'critical':
        return { background: 'rgba(239, 68, 68, 0.15)', color: '#ef4444', border: '1px solid rgba(239, 68, 68, 0.3)' };
      case 'high':
        return { background: 'rgba(245, 158, 11, 0.15)', color: '#f59e0b', border: '1px solid rgba(245, 158, 11, 0.3)' };
      case 'medium':
        return { background: 'rgba(59, 130, 246, 0.15)', color: '#3b82f6', border: '1px solid rgba(59, 130, 246, 0.3)' };
      default:
        return { background: 'rgba(107, 114, 128, 0.15)', color: '#9ca3af', border: '1px solid rgba(107, 114, 128, 0.3)' };
    }
  };

  return (
    <span
      style={{
        padding: '0.2rem 0.6rem',
        borderRadius: '9999px',
        fontSize: '0.75rem',
        fontWeight: 600,
        ...getStyle(criticality)
      }}
    >
      {criticality || 'Medium'}
    </span>
  );
};

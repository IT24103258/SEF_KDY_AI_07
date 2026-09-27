import React from 'react';
import { RiskBadge, PriorityBadge } from './PriorityBadges';
import { ShieldAlert, CheckCircle2, Clock } from 'lucide-react';

export const EscalationQueue = ({ escalatedItems = [], onSelectRequest }) => {
  if (!escalatedItems || escalatedItems.length === 0) {
    return (
      <div className="ff-card glass-panel" style={{ padding: '2rem', textAlign: 'center' }}>
        <CheckCircle2 size={36} color="#10b981" style={{ marginBottom: '0.75rem' }} />
        <h4 style={{ margin: '0 0 0.5rem 0' }}>Escalation Queue Clear</h4>
        <p style={{ color: 'var(--text-secondary)', fontSize: '0.85rem', margin: 0 }}>
          No maintenance requests currently require urgent manager escalation.
        </p>
      </div>
    );
  }

  return (
    <div className="ff-card glass-panel" style={{ padding: '1.5rem', marginBottom: '2rem' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '1rem' }}>
        <ShieldAlert size={20} color="#ef4444" />
        <h3 style={{ fontSize: '1.1rem', fontWeight: 700, margin: 0 }}>
          High-Risk Escalation Queue ({escalatedItems.length})
        </h3>
      </div>

      <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
        {escalatedItems.map((item) => (
          <div
            key={item.id}
            onClick={() => onSelectRequest && onSelectRequest(item)}
            style={{
              padding: '1rem',
              background: 'rgba(239, 68, 68, 0.05)',
              border: '1px solid rgba(239, 68, 68, 0.25)',
              borderRadius: '8px',
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
              cursor: 'pointer',
              transition: 'background 0.15s ease'
            }}
          >
            <div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.35rem' }}>
                <span style={{ fontWeight: 700, fontSize: '0.9rem' }}>{item.requestNumber}</span>
                <RiskBadge level={item.riskLevel} />
                <PriorityBadge priority={item.priority} />
                <span style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>
                  Asset: {item.assetName || 'Complex Infrastructure'}
                </span>
              </div>
              <div style={{ fontSize: '0.85rem', fontWeight: 600 }}>{item.requestTitle}</div>
              <div style={{ fontSize: '0.8rem', color: '#ef4444', marginTop: '0.25rem' }}>
                <strong>Reason:</strong> {item.escalationReason || 'Critical risk score / safety hazard exceeded SLA thresholds'}
              </div>
            </div>

            <div style={{ textAlign: 'right', display: 'flex', flexDirection: 'column', alignItems: 'flex-end', gap: '0.35rem' }}>
              <span style={{ fontSize: '0.8rem', display: 'flex', alignItems: 'center', gap: '4px', color: '#ef4444', fontWeight: 600 }}>
                <Clock size={14} /> {item.recommendedResponseWindow}
              </span>
              <button className="ff-btn ff-btn-outline" style={{ fontSize: '0.75rem', padding: '0.3rem 0.6rem' }}>
                View Assessment
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

import React from 'react';
import { RiskBadge, PriorityBadge, CriticalityBadge } from './PriorityBadges';
import { AlertTriangle, Clock, ShieldAlert, Cpu } from 'lucide-react';

export const RiskScoreCard = ({ assessment, onSimulate, onEscalate }) => {
  if (!assessment) return null;

  const factors = assessment.contributingFactors || {};
  const isCritical = assessment.riskLevel === 'Critical';

  return (
    <div className="ff-card glass-panel" style={{ padding: '1.5rem', marginBottom: '1.5rem' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', flexWrap: 'wrap', gap: '1rem', marginBottom: '1rem' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '0.35rem' }}>
            <span style={{ fontSize: '0.85rem', fontWeight: 600, color: 'var(--text-secondary)' }}>
              {assessment.requestNumber}
            </span>
            <RiskBadge level={assessment.riskLevel} />
            <PriorityBadge priority={assessment.priority} />
            {assessment.escalationFlag && (
              <span className="ff-badge ff-badge-danger" style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
                <ShieldAlert size={12} /> ESCALATED
              </span>
            )}
          </div>
          <h2 style={{ fontSize: '1.25rem', fontWeight: 700, margin: 0 }}>
            {assessment.requestTitle || 'Maintenance Request Assessment'}
          </h2>
        </div>

        {/* Numeric Score Circle */}
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            flexDirection: 'column',
            width: 75,
            height: 75,
            borderRadius: '50%',
            background: isCritical ? 'rgba(239, 68, 68, 0.2)' : 'rgba(59, 130, 246, 0.2)',
            border: `3px solid ${isCritical ? '#ef4444' : '#3b82f6'}`,
            textAlign: 'center'
          }}
        >
          <span style={{ fontSize: '1.35rem', fontWeight: 800, lineHeight: 1 }}>{assessment.riskScore}</span>
          <span style={{ fontSize: '0.65rem', textTransform: 'uppercase', color: 'var(--text-secondary)' }}>/ 100</span>
        </div>
      </div>

      {/* Factor Grid */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))',
          gap: '12px',
          background: 'rgba(255, 255, 255, 0.03)',
          padding: '1rem',
          borderRadius: '8px',
          marginBottom: '1rem'
        }}
      >
        <div>
          <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Asset Criticality</div>
          <div style={{ marginTop: '4px' }}>
            <CriticalityBadge criticality={assessment.assetCriticality} />
          </div>
        </div>

        <div>
          <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Impact Level</div>
          <div style={{ fontWeight: 600, fontSize: '0.9rem', marginTop: '4px' }}>{assessment.impactLevel}</div>
        </div>

        <div>
          <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Likelihood Level</div>
          <div style={{ fontWeight: 600, fontSize: '0.9rem', marginTop: '4px' }}>{assessment.likelihoodLevel}</div>
        </div>

        <div>
          <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Response Window</div>
          <div style={{ fontWeight: 600, fontSize: '0.9rem', marginTop: '4px', display: 'flex', alignItems: 'center', gap: '4px' }}>
            <Clock size={14} color="#3b82f6" /> {assessment.recommendedResponseWindow}
          </div>
        </div>

        <div>
          <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Target SLA</div>
          <div style={{ fontWeight: 600, fontSize: '0.9rem', marginTop: '4px' }}>
            {assessment.responseTimeHours}h resp / {assessment.resolutionTimeHours}h fix
          </div>
        </div>
      </div>

      {/* Explainable AI & Deterministic Rule Summary */}
      <div
        style={{
          display: 'flex',
          gap: '0.75rem',
          padding: '0.75rem 1rem',
          background: 'rgba(59, 130, 246, 0.08)',
          borderLeft: '4px solid #3b82f6',
          borderRadius: '4px',
          marginBottom: '1rem'
        }}
      >
        <Cpu size={18} style={{ color: '#3b82f6', flexShrink: 0, marginTop: '2px' }} />
        <div style={{ fontSize: '0.85rem', lineHeight: 1.4 }}>
          <strong>Decision Audit:</strong> {assessment.explanation}
        </div>
      </div>

      {/* Safety Hazard Warning */}
      {factors.hasSafetyHazard && (
        <div
          style={{
            display: 'flex',
            gap: '0.75rem',
            padding: '0.75rem 1rem',
            background: 'rgba(239, 68, 68, 0.1)',
            borderLeft: '4px solid #ef4444',
            borderRadius: '4px',
            marginBottom: '1rem'
          }}
        >
          <AlertTriangle size={18} style={{ color: '#ef4444', flexShrink: 0, marginTop: '2px' }} />
          <div style={{ fontSize: '0.85rem', lineHeight: 1.4, color: '#ef4444' }}>
            <strong>Safety Hazard Enforced:</strong> Deterministic safety override active. Priority elevation applied to protect occupants.
          </div>
        </div>
      )}

      {/* Actions */}
      <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
        {onSimulate && (
          <button className="ff-btn ff-btn-outline" onClick={() => onSimulate(assessment)}>
            Simulate Scenarios
          </button>
        )}
        {onEscalate && !assessment.escalationFlag && (
          <button className="ff-btn ff-btn-danger" onClick={() => onEscalate(assessment)}>
            Escalate Request
          </button>
        )}
      </div>
    </div>
  );
};

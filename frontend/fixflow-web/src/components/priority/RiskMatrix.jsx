import React from 'react';

export const RiskMatrix = ({ assessments = [], onCellClick, selectedCell }) => {
  const impacts = ['Critical', 'High', 'Medium', 'Low'];
  const likelihoods = ['Low', 'Medium', 'High', 'Critical'];

  // Matrix cell risk classification
  const getCellRisk = (impact, likelihood) => {
    const i = impact === 'Critical' ? 4 : impact === 'High' ? 3 : impact === 'Medium' ? 2 : 1;
    const l = likelihood === 'Critical' ? 4 : likelihood === 'High' ? 3 : likelihood === 'Medium' ? 2 : 1;
    const score = i * l;

    if (score >= 12 || i === 4) return { level: 'Critical', bg: 'rgba(239, 68, 68, 0.25)', border: '#ef4444' };
    if (score >= 8 || i === 3) return { level: 'High', bg: 'rgba(245, 158, 11, 0.25)', border: '#f59e0b' };
    if (score >= 4) return { level: 'Medium', bg: 'rgba(59, 130, 246, 0.25)', border: '#3b82f6' };
    return { level: 'Low', bg: 'rgba(16, 185, 129, 0.25)', border: '#10b981' };
  };

  // Group assessments into cells
  const getCount = (impact, likelihood) => {
    return assessments.filter(
      (a) =>
        a.impactLevel?.toLowerCase() === impact.toLowerCase() &&
        a.likelihoodLevel?.toLowerCase() === likelihood.toLowerCase()
    ).length;
  };

  return (
    <div className="ff-card glass-panel" style={{ padding: '1.5rem', marginBottom: '2rem' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
        <div>
          <h3 style={{ fontSize: '1.1rem', fontWeight: 700, margin: 0 }}>Deterministic 4×4 Risk Matrix</h3>
          <p style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', margin: '0.25rem 0 0 0' }}>
            Impact vs. Likelihood distribution calculated by backend deterministic rules
          </p>
        </div>
        <div style={{ display: 'flex', gap: '1rem', fontSize: '0.8rem' }}>
          <span style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
            <span style={{ width: 10, height: 10, borderRadius: '50%', background: '#ef4444' }} /> Critical
          </span>
          <span style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
            <span style={{ width: 10, height: 10, borderRadius: '50%', background: '#f59e0b' }} /> High
          </span>
          <span style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
            <span style={{ width: 10, height: 10, borderRadius: '50%', background: '#3b82f6' }} /> Medium
          </span>
          <span style={{ display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
            <span style={{ width: 10, height: 10, borderRadius: '50%', background: '#10b981' }} /> Low
          </span>
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '80px repeat(4, 1fr)', gap: '8px', alignItems: 'center' }}>
        {/* Header Column */}
        <div style={{ fontWeight: 600, fontSize: '0.8rem', color: 'var(--text-secondary)' }}>Impact \ Likelihood</div>
        {likelihoods.map((l) => (
          <div key={l} style={{ textAlign: 'center', fontWeight: 600, fontSize: '0.85rem', padding: '0.4rem' }}>
            {l}
          </div>
        ))}

        {/* Matrix Rows */}
        {impacts.map((imp) => (
          <React.Fragment key={imp}>
            <div style={{ fontWeight: 600, fontSize: '0.85rem', color: 'var(--text-secondary)' }}>{imp}</div>
            {likelihoods.map((lik) => {
              const cell = getCellRisk(imp, lik);
              const count = getCount(imp, lik);
              const isSelected = selectedCell?.impact === imp && selectedCell?.likelihood === lik;

              return (
                <button
                  key={`${imp}-${lik}`}
                  onClick={() => onCellClick && onCellClick(imp, lik)}
                  style={{
                    background: cell.bg,
                    border: isSelected ? `2px solid ${cell.border}` : `1px solid ${cell.border}`,
                    borderRadius: '8px',
                    padding: '1.25rem 0.5rem',
                    textAlign: 'center',
                    cursor: onCellClick ? 'pointer' : 'default',
                    transition: 'all 0.15s ease',
                    boxShadow: isSelected ? `0 0 10px ${cell.border}` : 'none'
                  }}
                  title={`${imp} Impact × ${lik} Likelihood: ${count} requests`}
                >
                  <div style={{ fontSize: '1.2rem', fontWeight: 800 }}>{count}</div>
                  <div style={{ fontSize: '0.7rem', textTransform: 'uppercase', letterSpacing: '0.05em', opacity: 0.85 }}>
                    {cell.level}
                  </div>
                </button>
              );
            })}
          </React.Fragment>
        ))}
      </div>
    </div>
  );
};

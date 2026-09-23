import React, { useState, useEffect } from 'react';
import { priorityApi } from '../../services/priorityApi';
import { RiskBadge, PriorityBadge } from './PriorityBadges';
import { Play, RotateCcw, AlertTriangle, ArrowRight, ShieldCheck } from 'lucide-react';

export const RiskSimulator = ({ selectedRequest, onSimulationComplete }) => {
  const [criticality, setCriticality] = useState('Medium');
  const [impact, setImpact] = useState('Medium');
  const [likelihood, setLikelihood] = useState('Medium');
  const [safetyHazard, setSafetyHazard] = useState(false);
  const [failureCount, setFailureCount] = useState(0);
  const [highDensity, setHighDensity] = useState(false);

  const [loading, setLoading] = useState(false);
  const [simulationResult, setSimulationResult] = useState(null);
  const [error, setError] = useState(null);

  useEffect(() => {
    if (selectedRequest) {
      setCriticality(selectedRequest.assetCriticality || 'Medium');
      setImpact(selectedRequest.impactLevel || 'Medium');
      setLikelihood(selectedRequest.likelihoodLevel || 'Medium');
      setSafetyHazard(selectedRequest.contributingFactors?.hasSafetyHazard || false);
      setFailureCount(selectedRequest.contributingFactors?.recentFailureCount || 0);
      setSimulationResult(null);
      setError(null);
    }
  }, [selectedRequest]);

  const handleSimulate = async () => {
    if (!selectedRequest?.requestId) {
      setError('Please select an active maintenance request to simulate.');
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const res = await priorityApi.simulateRisk(selectedRequest.requestId, {
        assetCriticality: criticality,
        impactLevel: impact,
        likelihoodLevel: likelihood,
        hasSafetyHazard: safetyHazard,
        recentFailureCount: failureCount,
        isHighDensityLocation: highDensity
      });

      if (res?.success && res?.data) {
        setSimulationResult(res.data);
        if (onSimulationComplete) onSimulationComplete(res.data);
      } else {
        setError(res?.message || 'Simulation could not be calculated.');
      }
    } catch (err) {
      setError(err.message || 'Failed to execute risk simulation.');
    } finally {
      setLoading(false);
    }
  };

  const handleReset = () => {
    if (selectedRequest) {
      setCriticality(selectedRequest.assetCriticality || 'Medium');
      setImpact(selectedRequest.impactLevel || 'Medium');
      setLikelihood(selectedRequest.likelihoodLevel || 'Medium');
      setSafetyHazard(selectedRequest.contributingFactors?.hasSafetyHazard || false);
      setFailureCount(selectedRequest.contributingFactors?.recentFailureCount || 0);
    }
    setSimulationResult(null);
    setError(null);
  };

  if (!selectedRequest) {
    return (
      <div className="ff-card glass-panel" style={{ padding: '2rem', textAlign: 'center' }}>
        <ShieldCheck size={36} color="var(--primary)" style={{ marginBottom: '0.75rem' }} />
        <h3 style={{ margin: '0 0 0.5rem 0' }}>Risk Sandbox Simulator</h3>
        <p style={{ color: 'var(--text-secondary)', fontSize: '0.9rem' }}>
          Select any maintenance request from the table to simulate how environmental factors, safety hazards, or asset criticality would affect priority and SLA targets.
        </p>
      </div>
    );
  }

  return (
    <div className="ff-card glass-panel" style={{ padding: '1.5rem', marginBottom: '2rem' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <span className="ff-badge ff-badge-info" style={{ textTransform: 'uppercase', fontSize: '0.7rem' }}>
              Sandbox / Read-Only
            </span>
            <span style={{ fontSize: '0.85rem', color: 'var(--text-secondary)' }}>
              Target: {selectedRequest.requestNumber}
            </span>
          </div>
          <h3 style={{ fontSize: '1.15rem', fontWeight: 700, margin: '0.25rem 0 0 0' }}>
            Interactive Risk Scenario Simulator
          </h3>
        </div>

        <div style={{ display: 'flex', gap: '0.5rem' }}>
          <button className="ff-btn ff-btn-outline" onClick={handleReset} title="Reset to baseline">
            <RotateCcw size={14} style={{ marginRight: '4px' }} /> Reset
          </button>
          <button className="ff-btn ff-btn-primary" onClick={handleSimulate} disabled={loading}>
            <Play size={14} style={{ marginRight: '4px' }} /> {loading ? 'Simulating...' : 'Run Simulation'}
          </button>
        </div>
      </div>

      {error && (
        <div style={{ padding: '0.75rem 1rem', background: 'rgba(239, 68, 68, 0.1)', color: '#ef4444', borderRadius: '6px', marginBottom: '1rem', fontSize: '0.85rem' }}>
          {error}
        </div>
      )}

      {/* Simulator Variable Controls */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '1rem', marginBottom: '1.5rem' }}>
        <div>
          <label style={{ fontSize: '0.8rem', fontWeight: 600, color: 'var(--text-secondary)', display: 'block', marginBottom: '0.35rem' }}>
            Simulated Asset Criticality
          </label>
          <select className="ff-input" value={criticality} onChange={(e) => setCriticality(e.target.value)}>
            <option value="Low">Low</option>
            <option value="Medium">Medium</option>
            <option value="High">High</option>
            <option value="Critical">Critical</option>
          </select>
        </div>

        <div>
          <label style={{ fontSize: '0.8rem', fontWeight: 600, color: 'var(--text-secondary)', display: 'block', marginBottom: '0.35rem' }}>
            Operational Impact Level
          </label>
          <select className="ff-input" value={impact} onChange={(e) => setImpact(e.target.value)}>
            <option value="Low">Low</option>
            <option value="Medium">Medium</option>
            <option value="High">High</option>
            <option value="Critical">Critical</option>
          </select>
        </div>

        <div>
          <label style={{ fontSize: '0.8rem', fontWeight: 600, color: 'var(--text-secondary)', display: 'block', marginBottom: '0.35rem' }}>
            Likelihood Level
          </label>
          <select className="ff-input" value={likelihood} onChange={(e) => setLikelihood(e.target.value)}>
            <option value="Low">Low</option>
            <option value="Medium">Medium</option>
            <option value="High">High</option>
            <option value="Critical">Critical</option>
          </select>
        </div>

        <div>
          <label style={{ fontSize: '0.8rem', fontWeight: 600, color: 'var(--text-secondary)', display: 'block', marginBottom: '0.35rem' }}>
            Recent Failures (Past 30d)
          </label>
          <input
            type="number"
            className="ff-input"
            min="0"
            max="10"
            value={failureCount}
            onChange={(e) => setFailureCount(parseInt(e.target.value) || 0)}
          />
        </div>
      </div>

      <div style={{ display: 'flex', gap: '1.5rem', marginBottom: '1.5rem', flexWrap: 'wrap' }}>
        <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', cursor: 'pointer', fontSize: '0.85rem' }}>
          <input
            type="checkbox"
            checked={safetyHazard}
            onChange={(e) => setSafetyHazard(e.target.checked)}
          />
          <span style={{ fontWeight: 600, color: safetyHazard ? '#ef4444' : 'inherit' }}>
            Simulate Safety Hazard Present (Triggers Deterministic Override)
          </span>
        </label>

        <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', cursor: 'pointer', fontSize: '0.85rem' }}>
          <input
            type="checkbox"
            checked={highDensity}
            onChange={(e) => setHighDensity(e.target.checked)}
          />
          <span>High-Density / Public Common Area Modifier</span>
        </label>
      </div>

      {/* Simulation Result Comparison Card */}
      {simulationResult && (
        <div
          style={{
            background: 'rgba(59, 130, 246, 0.05)',
            border: '1px solid rgba(59, 130, 246, 0.25)',
            borderRadius: '8px',
            padding: '1.25rem'
          }}
        >
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem', flexWrap: 'wrap', gap: '0.5rem' }}>
            <span style={{ fontWeight: 700, fontSize: '1rem', color: 'var(--primary)' }}>
              Simulation Outcome Comparison
            </span>
            <span style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>
              {simulationResult.delta?.summary}
            </span>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '1rem' }}>
            {/* Baseline Box */}
            <div style={{ background: 'rgba(255, 255, 255, 0.02)', padding: '1rem', borderRadius: '6px', border: '1px solid var(--border)' }}>
              <div style={{ fontSize: '0.75rem', textTransform: 'uppercase', color: 'var(--text-secondary)', marginBottom: '0.5rem' }}>
                Current Baseline
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.5rem' }}>
                <span style={{ fontSize: '1.5rem', fontWeight: 800 }}>{simulationResult.baselineAssessment?.riskScore ?? selectedRequest.riskScore}</span>
                <span style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>/ 100</span>
                <RiskBadge level={simulationResult.baselineAssessment?.riskLevel ?? selectedRequest.riskLevel} />
              </div>
              <div style={{ fontSize: '0.85rem' }}>
                Priority: <PriorityBadge priority={simulationResult.baselineAssessment?.priority ?? selectedRequest.priority} />
              </div>
            </div>

            {/* Simulated Box */}
            <div style={{ background: 'rgba(59, 130, 246, 0.1)', padding: '1rem', borderRadius: '6px', border: '1px solid #3b82f6' }}>
              <div style={{ fontSize: '0.75rem', textTransform: 'uppercase', color: '#3b82f6', fontWeight: 700, marginBottom: '0.5rem' }}>
                Simulated Outcome
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.5rem' }}>
                <span style={{ fontSize: '1.5rem', fontWeight: 800, color: '#3b82f6' }}>
                  {simulationResult.simulatedAssessment?.riskScore}
                </span>
                <span style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>/ 100</span>
                <RiskBadge level={simulationResult.simulatedAssessment?.riskLevel} />
                {simulationResult.delta?.scoreDelta !== 0 && (
                  <span style={{ fontSize: '0.85rem', fontWeight: 700, color: simulationResult.delta.scoreDelta > 0 ? '#ef4444' : '#10b981' }}>
                    {simulationResult.delta.scoreDelta > 0 ? `+${simulationResult.delta.scoreDelta}` : simulationResult.delta.scoreDelta}
                  </span>
                )}
              </div>
              <div style={{ fontSize: '0.85rem', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                Priority: <PriorityBadge priority={simulationResult.simulatedAssessment?.priority} />
              </div>
              <div style={{ fontSize: '0.8rem', color: 'var(--text-secondary)', marginTop: '0.5rem' }}>
                Window: {simulationResult.simulatedAssessment?.recommendedResponseWindow}
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

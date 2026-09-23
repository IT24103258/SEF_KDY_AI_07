import React, { useState, useEffect, useMemo } from 'react';
import { priorityApi } from '../services/priorityApi';
import { RiskBadge, PriorityBadge } from '../components/priority/PriorityBadges';
import { RiskMatrix } from '../components/priority/RiskMatrix';
import { RiskScoreCard } from '../components/priority/RiskScoreCard';
import { RiskSimulator } from '../components/priority/RiskSimulator';
import { EscalationQueue } from '../components/priority/EscalationQueue';
import {
  ShieldAlert,
  Search,
  Filter,
  RefreshCw,
  SlidersHorizontal,
  Flame,
  Clock,
  Layers,
  CheckCircle,
  AlertCircle
} from 'lucide-react';

export const PriorityDashboard = () => {
  const [activeTab, setActiveTab] = useState('overview'); // overview, matrix, queue, simulator
  const [assessments, setAssessments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Filters
  const [searchTerm, setSearchTerm] = useState('');
  const [priorityFilter, setPriorityFilter] = useState('');
  const [riskFilter, setRiskFilter] = useState('');
  const [escalatedOnly, setEscalatedOnly] = useState(false);

  // Selected for detail view or simulator
  const [selectedAssessment, setSelectedAssessment] = useState(null);

  // Escalation Modal
  const [escalatingItem, setEscalatingItem] = useState(null);
  const [escalationReason, setEscalationReason] = useState('');
  const [immediateHazard, setImmediateHazard] = useState(false);
  const [submittingEscalation, setSubmittingEscalation] = useState(false);

  const fetchAssessments = async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await priorityApi.getPriorityAssessments({
        searchTerm,
        priority: priorityFilter,
        riskLevel: riskFilter,
        escalatedOnly,
        pageSize: 50
      });

      if (res?.success) {
        const items = res.data?.items || [];
        setAssessments(items);
        if (items.length > 0 && !selectedAssessment) {
          setSelectedAssessment(items[0]);
        }
      } else {
        setError(res?.message || 'Failed to load assessments.');
      }
    } catch (err) {
      setError(err.message || 'Error communicating with priority service.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchAssessments();
  }, [priorityFilter, riskFilter, escalatedOnly]);

  const handleSearchSubmit = (e) => {
    e.preventDefault();
    fetchAssessments();
  };

  // Metrics
  const metrics = useMemo(() => {
    const total = assessments.length;
    const critical = assessments.filter((a) => a.riskLevel === 'Critical').length;
    const escalated = assessments.filter((a) => a.escalationFlag).length;
    const avgScore = total > 0 ? Math.round(assessments.reduce((acc, a) => acc + (a.riskScore || 0), 0) / total) : 0;
    return { total, critical, escalated, avgScore };
  }, [assessments]);

  const escalatedList = useMemo(() => {
    return assessments.filter((a) => a.escalationFlag || a.riskLevel === 'Critical');
  }, [assessments]);

  const handleEscalateSubmit = async (e) => {
    e.preventDefault();
    if (!escalatingItem || !escalationReason.trim()) return;

    setSubmittingEscalation(true);
    try {
      const res = await priorityApi.escalateRequest(escalatingItem.requestId, {
        reason: escalationReason,
        immediateHazard
      });

      if (res?.success) {
        setEscalatingItem(null);
        setEscalationReason('');
        setImmediateHazard(false);
        fetchAssessments();
      } else {
        alert(res?.message || 'Escalation failed.');
      }
    } catch (err) {
      alert(err.message || 'Error escalating request.');
    } finally {
      setSubmittingEscalation(false);
    }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
      {/* Page Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '1rem' }}>
        <div>
          <h1 style={{ fontSize: '1.75rem', fontWeight: 800, margin: 0, letterSpacing: '-0.02em' }}>
            Risk & Priority Assessment
          </h1>
          <p style={{ color: 'var(--text-secondary)', fontSize: '0.9rem', margin: '0.25rem 0 0 0' }}>
            Deterministic Risk Evaluation, SLA Calculation, and Sandbox Simulation
          </p>
        </div>

        <div style={{ display: 'flex', gap: '0.75rem' }}>
          <button className="ff-btn ff-btn-outline" onClick={fetchAssessments} title="Refresh data">
            <RefreshCw size={14} style={{ marginRight: '4px' }} /> Refresh
          </button>
        </div>
      </div>

      {/* KPI Summary Cards */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '1rem' }}>
        <div className="ff-card glass-panel" style={{ padding: '1.25rem' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', color: 'var(--text-secondary)', fontSize: '0.8rem', fontWeight: 600 }}>
            <span>TOTAL ASSESSED</span>
            <Layers size={16} />
          </div>
          <div style={{ fontSize: '1.8rem', fontWeight: 800, marginTop: '0.5rem' }}>{metrics.total}</div>
          <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', marginTop: '0.25rem' }}>Maintenance issues evaluated</div>
        </div>

        <div className="ff-card glass-panel" style={{ padding: '1.25rem' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', color: '#ef4444', fontSize: '0.8rem', fontWeight: 600 }}>
            <span>CRITICAL RISK</span>
            <Flame size={16} />
          </div>
          <div style={{ fontSize: '1.8rem', fontWeight: 800, color: '#ef4444', marginTop: '0.5rem' }}>{metrics.critical}</div>
          <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', marginTop: '0.25rem' }}>Immediate response required</div>
        </div>

        <div className="ff-card glass-panel" style={{ padding: '1.25rem' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', color: '#f59e0b', fontSize: '0.8rem', fontWeight: 600 }}>
            <span>ESCALATION QUEUE</span>
            <ShieldAlert size={16} />
          </div>
          <div style={{ fontSize: '1.8rem', fontWeight: 800, color: '#f59e0b', marginTop: '0.5rem' }}>{metrics.escalated}</div>
          <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', marginTop: '0.25rem' }}>Flagged for manager review</div>
        </div>

        <div className="ff-card glass-panel" style={{ padding: '1.25rem' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', color: '#3b82f6', fontSize: '0.8rem', fontWeight: 600 }}>
            <span>AVERAGE RISK SCORE</span>
            <Clock size={16} />
          </div>
          <div style={{ fontSize: '1.8rem', fontWeight: 800, color: '#3b82f6', marginTop: '0.5rem' }}>{metrics.avgScore} <span style={{ fontSize: '0.9rem', color: 'var(--text-secondary)' }}>/100</span></div>
          <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', marginTop: '0.25rem' }}>Apartment complex health index</div>
        </div>
      </div>

      {/* Tabs */}
      <div style={{ display: 'flex', borderBottom: '1px solid var(--border)', gap: '1rem' }}>
        <button
          onClick={() => setActiveTab('overview')}
          style={{
            padding: '0.75rem 1rem',
            background: 'none',
            border: 'none',
            borderBottom: activeTab === 'overview' ? '3px solid var(--primary)' : '3px solid transparent',
            color: activeTab === 'overview' ? 'var(--primary)' : 'var(--text-secondary)',
            fontWeight: 700,
            fontSize: '0.9rem',
            cursor: 'pointer'
          }}
        >
          Assessments & Scoring
        </button>

        <button
          onClick={() => setActiveTab('matrix')}
          style={{
            padding: '0.75rem 1rem',
            background: 'none',
            border: 'none',
            borderBottom: activeTab === 'matrix' ? '3px solid var(--primary)' : '3px solid transparent',
            color: activeTab === 'matrix' ? 'var(--primary)' : 'var(--text-secondary)',
            fontWeight: 700,
            fontSize: '0.9rem',
            cursor: 'pointer'
          }}
        >
          4×4 Risk Matrix Visualizer
        </button>

        <button
          onClick={() => setActiveTab('queue')}
          style={{
            padding: '0.75rem 1rem',
            background: 'none',
            border: 'none',
            borderBottom: activeTab === 'queue' ? '3px solid var(--primary)' : '3px solid transparent',
            color: activeTab === 'queue' ? 'var(--primary)' : 'var(--text-secondary)',
            fontWeight: 700,
            fontSize: '0.9rem',
            cursor: 'pointer',
            display: 'flex',
            alignItems: 'center',
            gap: '6px'
          }}
        >
          Escalations ({metrics.escalated})
        </button>

        <button
          onClick={() => setActiveTab('simulator')}
          style={{
            padding: '0.75rem 1rem',
            background: 'none',
            border: 'none',
            borderBottom: activeTab === 'simulator' ? '3px solid var(--primary)' : '3px solid transparent',
            color: activeTab === 'simulator' ? 'var(--primary)' : 'var(--text-secondary)',
            fontWeight: 700,
            fontSize: '0.9rem',
            cursor: 'pointer',
            display: 'flex',
            alignItems: 'center',
            gap: '6px'
          }}
        >
          <SlidersHorizontal size={14} /> Risk Sandbox Simulator
        </button>
      </div>

      {/* Tab 1: Assessments & Scoring Overview */}
      {activeTab === 'overview' && (
        <>
          {/* Filter Bar */}
          <div className="ff-card glass-panel" style={{ padding: '1rem' }}>
            <form onSubmit={handleSearchSubmit} style={{ display: 'flex', gap: '0.75rem', flexWrap: 'wrap', alignItems: 'center' }}>
              <div style={{ flex: 1, minWidth: '220px', position: 'relative' }}>
                <input
                  type="text"
                  className="ff-input"
                  placeholder="Search by request #, title, or asset..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  style={{ paddingLeft: '2rem' }}
                />
                <Search size={14} style={{ position: 'absolute', left: '0.75rem', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-secondary)' }} />
              </div>

              <select className="ff-input" value={priorityFilter} onChange={(e) => setPriorityFilter(e.target.value)} style={{ width: '150px' }}>
                <option value="">All Priorities</option>
                <option value="Critical">Critical</option>
                <option value="High">High</option>
                <option value="Medium">Medium</option>
                <option value="Low">Low</option>
              </select>

              <select className="ff-input" value={riskFilter} onChange={(e) => setRiskFilter(e.target.value)} style={{ width: '150px' }}>
                <option value="">All Risk Levels</option>
                <option value="Critical">Critical</option>
                <option value="High">High</option>
                <option value="Medium">Medium</option>
                <option value="Low">Low</option>
              </select>

              <label style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', fontSize: '0.85rem', cursor: 'pointer' }}>
                <input type="checkbox" checked={escalatedOnly} onChange={(e) => setEscalatedOnly(e.target.checked)} />
                <span>Escalated Only</span>
              </label>

              <button type="submit" className="ff-btn ff-btn-primary">
                <Filter size={14} style={{ marginRight: '4px' }} /> Apply
              </button>
            </form>
          </div>

          {/* Detailed Selected Card */}
          {selectedAssessment && (
            <RiskScoreCard
              assessment={selectedAssessment}
              onSimulate={(item) => {
                setSelectedAssessment(item);
                setActiveTab('simulator');
              }}
              onEscalate={(item) => setEscalatingItem(item)}
            />
          )}

          {/* Assessment Table */}
          <div className="ff-card glass-panel" style={{ padding: 0, overflow: 'hidden' }}>
            <div style={{ padding: '1rem 1.25rem', borderBottom: '1px solid var(--border)', fontWeight: 700 }}>
              Evaluated Maintenance Requests ({assessments.length})
            </div>

            {loading ? (
              <div style={{ padding: '3rem', textAlign: 'center', color: 'var(--text-secondary)' }}>
                <RefreshCw size={24} className="ff-spin" style={{ marginBottom: '0.5rem' }} />
                <div>Loading priority assessments...</div>
              </div>
            ) : assessments.length === 0 ? (
              <div style={{ padding: '3rem', textAlign: 'center', color: 'var(--text-secondary)' }}>
                <AlertCircle size={32} style={{ marginBottom: '0.5rem' }} />
                <h4>No Assessments Found</h4>
                <p style={{ fontSize: '0.85rem' }}>No maintenance requests match the current filters.</p>
              </div>
            ) : (
              <div style={{ overflowX: 'auto' }}>
                <table className="ff-table" style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
                  <thead>
                    <tr style={{ background: 'rgba(255, 255, 255, 0.02)', borderBottom: '1px solid var(--border)', fontSize: '0.8rem', color: 'var(--text-secondary)' }}>
                      <th style={{ padding: '0.75rem 1.25rem' }}>REQUEST</th>
                      <th style={{ padding: '0.75rem' }}>ASSET</th>
                      <th style={{ padding: '0.75rem' }}>RISK SCORE</th>
                      <th style={{ padding: '0.75rem' }}>RISK LEVEL</th>
                      <th style={{ padding: '0.75rem' }}>PRIORITY</th>
                      <th style={{ padding: '0.75rem' }}>RESPONSE WINDOW</th>
                      <th style={{ padding: '0.75rem' }}>STATUS</th>
                      <th style={{ padding: '0.75rem 1.25rem', textAlign: 'right' }}>ACTION</th>
                    </tr>
                  </thead>
                  <tbody>
                    {assessments.map((a) => {
                      const isSelected = selectedAssessment?.id === a.id;
                      return (
                        <tr
                          key={a.id}
                          onClick={() => setSelectedAssessment(a)}
                          style={{
                            borderBottom: '1px solid var(--border)',
                            background: isSelected ? 'rgba(59, 130, 246, 0.08)' : 'transparent',
                            cursor: 'pointer'
                          }}
                        >
                          <td style={{ padding: '0.85rem 1.25rem' }}>
                            <div style={{ fontWeight: 700, fontSize: '0.85rem' }}>{a.requestNumber}</div>
                            <div style={{ fontSize: '0.8rem', color: 'var(--text-secondary)', maxWidth: '220px', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                              {a.requestTitle}
                            </div>
                          </td>
                          <td style={{ padding: '0.85rem' }}>
                            <div style={{ fontSize: '0.85rem' }}>{a.assetName || 'Complex Asset'}</div>
                            <div style={{ fontSize: '0.7rem', color: 'var(--text-secondary)' }}>{a.assetCriticality} Criticality</div>
                          </td>
                          <td style={{ padding: '0.85rem' }}>
                            <span style={{ fontWeight: 800, fontSize: '1rem' }}>{a.riskScore}</span>
                            <span style={{ fontSize: '0.7rem', color: 'var(--text-secondary)' }}>/100</span>
                          </td>
                          <td style={{ padding: '0.85rem' }}>
                            <RiskBadge level={a.riskLevel} />
                          </td>
                          <td style={{ padding: '0.85rem' }}>
                            <PriorityBadge priority={a.priority} />
                          </td>
                          <td style={{ padding: '0.85rem', fontSize: '0.8rem' }}>
                            {a.recommendedResponseWindow}
                          </td>
                          <td style={{ padding: '0.85rem' }}>
                            {a.escalationFlag ? (
                              <span className="ff-badge ff-badge-danger">Escalated</span>
                            ) : (
                              <span className="ff-badge ff-badge-success">Standard</span>
                            )}
                          </td>
                          <td style={{ padding: '0.85rem 1.25rem', textAlign: 'right' }}>
                            <button
                              className="ff-btn ff-btn-outline"
                              style={{ padding: '0.25rem 0.5rem', fontSize: '0.75rem' }}
                              onClick={(e) => {
                                e.stopPropagation();
                                setSelectedAssessment(a);
                              }}
                            >
                              Inspect
                            </button>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </>
      )}

      {/* Tab 2: 4x4 Risk Matrix Visualizer */}
      {activeTab === 'matrix' && (
        <div>
          <RiskMatrix
            assessments={assessments}
            selectedCell={null}
            onCellClick={(impact, likelihood) => {
              const matched = assessments.find(
                (a) =>
                  a.impactLevel?.toLowerCase() === impact.toLowerCase() &&
                  a.likelihoodLevel?.toLowerCase() === likelihood.toLowerCase()
              );
              if (matched) setSelectedAssessment(matched);
            }}
          />

          {selectedAssessment && (
            <RiskScoreCard
              assessment={selectedAssessment}
              onSimulate={() => setActiveTab('simulator')}
              onEscalate={() => setEscalatingItem(selectedAssessment)}
            />
          )}
        </div>
      )}

      {/* Tab 3: Escalation Queue */}
      {activeTab === 'queue' && (
        <div>
          <EscalationQueue
            escalatedItems={escalatedList}
            onSelectRequest={(item) => {
              setSelectedAssessment(item);
              setActiveTab('overview');
            }}
          />
        </div>
      )}

      {/* Tab 4: Risk Sandbox Simulator */}
      {activeTab === 'simulator' && (
        <div>
          <RiskSimulator
            selectedRequest={selectedAssessment || assessments[0]}
            onSimulationComplete={() => {}}
          />
        </div>
      )}

      {/* Manager Escalation Dialog Modal */}
      {escalatingItem && (
        <div
          style={{
            position: 'fixed',
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            background: 'rgba(0, 0, 0, 0.75)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 9999,
            padding: '1rem'
          }}
        >
          <div className="ff-card glass-strong" style={{ maxWidth: '480px', width: '100%', padding: '1.5rem' }}>
            <h3 style={{ margin: '0 0 0.5rem 0', display: 'flex', alignItems: 'center', gap: '8px', color: '#ef4444' }}>
              <ShieldAlert size={20} /> Escalate Request
            </h3>
            <p style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', marginBottom: '1rem' }}>
              Triggering escalation will elevate priority to Critical, enforce a 1-hour response SLA, and notify dispatch managers.
            </p>

            <form onSubmit={handleEscalateSubmit}>
              <div style={{ marginBottom: '1rem' }}>
                <label style={{ fontSize: '0.8rem', fontWeight: 600, display: 'block', marginBottom: '0.35rem' }}>
                  Escalation Reason *
                </label>
                <textarea
                  className="ff-input"
                  rows={3}
                  required
                  placeholder="Explain why immediate intervention is required..."
                  value={escalationReason}
                  onChange={(e) => setEscalationReason(e.target.value)}
                />
              </div>

              <div style={{ marginBottom: '1.5rem' }}>
                <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', fontSize: '0.85rem', cursor: 'pointer' }}>
                  <input
                    type="checkbox"
                    checked={immediateHazard}
                    onChange={(e) => setImmediateHazard(e.target.checked)}
                  />
                  <span style={{ fontWeight: 600, color: '#ef4444' }}>
                    Immediate Safety / Environmental Hazard
                  </span>
                </label>
              </div>

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
                <button
                  type="button"
                  className="ff-btn ff-btn-outline"
                  onClick={() => setEscalatingItem(null)}
                  disabled={submittingEscalation}
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="ff-btn ff-btn-danger"
                  disabled={submittingEscalation || !escalationReason.trim()}
                >
                  {submittingEscalation ? 'Escalating...' : 'Confirm Escalation'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

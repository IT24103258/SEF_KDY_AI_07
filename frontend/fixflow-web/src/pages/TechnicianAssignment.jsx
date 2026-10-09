import React, { useState, useEffect } from 'react';
import { UserCheck, Sparkles, CheckCircle } from 'lucide-react';
import { api , technicianApi } from '../services/api';

// Priority ranking used to order the matching list: Critical → High → Medium → Low.
// Requests without a Priority Agent output rank last.
const PRIORITY_RANK = { Critical: 4, High: 3, Medium: 2, Low: 1 };

// A request may run the AI Assignment Agent ONLY when BOTH agent outputs are valid:
// a Required Skill from the Classification Agent, and an available (non-Pending)
// Priority from the Priority Agent.
const isAssignmentReady = (r) => {
  const skill = r.requiredSkill ? String(r.requiredSkill).trim() : '';
  const priority = r.priorityLevel ? String(r.priorityLevel).trim() : '';
  return skill !== '' && priority !== '' && priority.toLowerCase() !== 'pending';
};

export default function TechnicianAssignment() {
  const [technicians, setTechnicians] = useState([]);
  const [apiError, setApiError] = useState('');
  const [assignStatus, setAssignStatus] = useState('');

  // Agent outputs (Classification + Priority) for EVERY unassigned maintenance request.
  const [requestOutputs, setRequestOutputs] = useState([]);
  // Id of the request currently going through the one-click assignment flow.
  const [processingRequestId, setProcessingRequestId] = useState(null);
  // Result of the last completed one-click assignment (display only).
  const [lastAssignment, setLastAssignment] = useState(null);

  const fetchTechnicians = async () => {
    try {
      setApiError('');
      const data = await technicianApi.fetchTechnicians();
      setTechnicians(data);
    } catch (err) {
      console.error("Error fetching technicians:", err);
      setApiError(`Failed to load Technicians: ${err.message}`);
    }
  };

  // Loads every page of a paged list endpoint (e.g. /requests, /priority-assessments).
  const fetchAllPages = async (endpoint) => {
    const allItems = [];
    let page = 1;
    let totalPages = 1;
    do {
      const res = await api.get(`${endpoint}?page=${page}&pageSize=100`);
      const data = res.data || {};
      const items = data.items || [];
      allItems.push(...items);
      totalPages = data.totalPages || 1;
      page += 1;
    } while (page <= totalPages && allItems.length > 0);
    return allItems;
  };

  // Loads the Classification Agent output (category/required skill) and the Priority Agent
  // output (Risk & Priority Assessment) for EVERY maintenance request that has them,
  // not just the latest request.
  const fetchRequestAgentOutputs = async () => {
    try {
      // Retrieving ALL maintenance requests (the endpoint returns them newest-first).
      const requestsList = await fetchAllPages('/requests');

      // Retrieving ALL Priority Agent outputs (Risk & Priority Assessments), newest-first.
      let assessmentsList = [];
      try {
        assessmentsList = await fetchAllPages('/priority-assessments');
      } catch (priorityErr) {
        console.warn("Could not fetch priority assessments:", priorityErr);
      }

      // Latest assessment per request (the list is newest-first, so the first match wins).
      const assessmentByRequest = new Map();
      assessmentsList.forEach(a => {
        const key = String(a.requestId || a.id || '');
        if (!assessmentByRequest.has(key)) {
          assessmentByRequest.set(key, a);
        }
      });

      // Combining the Classification + Priority agent outputs for every request that has them.
      const outputs = requestsList
        .map(req => {
          const requestId = req.id || req.requestId;
          const assessment = assessmentByRequest.get(String(requestId));
          return {
            requestId: requestId,
            requestNumber: req.requestNumber || requestId,
            title: req.title || '',
            // Category/Skill obtained from the Classification Agent
            requiredSkill: req.category || req.requiredSkill || null,
            hasClassification: !!(req.category || req.requiredSkill || req.hasClassification),
            // Priority obtained from the Priority Agent (Risk & Priority Assessment)
            priorityLevel: assessment ? (assessment.priority || assessment.riskLevel || null) : null,
            hasPriority: !!assessment
          };
        })
        .filter(r => r.hasClassification || r.hasPriority);

      // Hiding requests that already have a technician assigned (Assignment record with
      // status "Assigned"). "Recommended" is only the AI agent's suggestion that has not
      // been approved yet, so those requests remain visible for the manager to assign.
      const unassigned = await Promise.all(outputs.map(async (output) => {
        try {
          const res = await workOrderApi.getAssignmentForRequest(output.requestId);
          const assignment = res?.success && res.data ? res.data : null;
          const assignmentStatus = assignment?.assignmentStatus || assignment?.AssignmentStatus;
          return assignmentStatus === 'Assigned' ? null : output;
        } catch (assignErr) {
          console.warn(`Could not check assignment for request ${output.requestId}:`, assignErr);
          return output;
        }
      }));

      setRequestOutputs(unassigned.filter(Boolean));
    } catch (err) {
      console.error("Error fetching dynamic request data:", err);
    }
  };

  useEffect(() => {
    fetchTechnicians();
    fetchRequestAgentOutputs();
  }, []);

  // ONE-CLICK flow for a single request: recommend the best technician via the existing
  // Assignment Agent matching logic, then immediately assign that technician. Uses this
  // specific request's Classification Agent output (required skill) and Priority Agent
  // output (priority level). One technician is assigned per request; technician
  // availability is NOT modified (a technician may receive multiple requests).
  const handleRunAssignment = async (req) => {
    if (processingRequestId) return;
    // Guard: assignment requires a valid Required Skill AND a non-Pending Priority.
    if (!isAssignmentReady(req)) return;
    setAssignStatus('');
    setLastAssignment(null);
    setProcessingRequestId(req.requestId);
    try {
      // Guard: never process a request that already has a technician assigned.
      try {
        const check = await workOrderApi.getAssignmentForRequest(req.requestId);
        const existing = check?.success && check.data ? check.data : null;
        const existingStatus = existing?.assignmentStatus || existing?.AssignmentStatus;
        if (existingStatus === 'Assigned') {
          setRequestOutputs(prev =>
            prev.filter(r => String(r.requestId) !== String(req.requestId))
          );
          setAssignStatus(`Request #${req.requestNumber} already has a technician assigned — removed from the matching list.`);
          return;
        }
      } catch (checkErr) {
        console.warn("Could not verify existing assignment:", checkErr);
      }

      // 1. Existing technician recommendation API (unchanged matching logic).
      const res = await technicianApi.fetchRecommendation({
        requestId: req.requestId,
        requiredSkill: req.requiredSkill || '',
        priorityLevel: req.priorityLevel || 'Normal'
      });
      const rec = res?.data || res;
      const techId =
        rec?.recommendedTechnicianId ||
        rec?.RecommendedTechnicianId ||
        rec?.top_match_id ||
        rec?.id;
      if (!techId) {
        throw new Error('The Assignment Agent did not return a recommended technician.');
      }

      // 2. Existing assignment API — saves the Assignment record with status "Assigned".
      await technicianApi.assignTechnician({
        requestId: req.requestId,
        requiredSkill: req.requiredSkill || '',
        priorityLevel: req.priorityLevel || 'Normal',
        technicianId: techId
      });

      // 3. Remove the request from the matching list locally (no page reload).
      setRequestOutputs(prev =>
        prev.filter(r => String(r.requestId) !== String(req.requestId))
      );

      setLastAssignment({
        requestNumber: req.requestNumber,
        technicianName: rec?.technicianName || rec?.TechnicianName || 'N/A',
        matchScore: rec?.matchScore ?? rec?.MatchScore ?? 0.85,
        reasoning: rec?.reasoningSummary || rec?.ReasoningSummary || rec?.reasoning || 'Best fit based on availability and skills.'
      });
      setAssignStatus('Technician assigned successfully!');
    } catch (err) {
      console.error("Error during AI assignment:", err);
      setAssignStatus(`Assignment failed for request #${req.requestNumber}: ${err.message}`);
    } finally {
      setProcessingRequestId(null);
    }
  };

  // Priority order: Critical → High → Medium → Low. The sort is stable, so requests
  // with the same priority keep their existing newest-first order.
  const sortedRequestOutputs = [...requestOutputs].sort(
    (a, b) => (PRIORITY_RANK[b.priorityLevel] || 0) - (PRIORITY_RANK[a.priorityLevel] || 0)
  );

  return (
    <div style={{ padding: '30px', fontFamily: 'sans-serif', minHeight: '100vh' }}>
      <h2>FixFlow AI - Manager Dashboard </h2>
      <p style={{ color: '#666' }}>Technician Skill Matching & Intelligent Assignment Subsystem</p>

      {apiError && (
        <div style={{ padding: '10px', background: '#fee2e2', color: '#dc2626', borderRadius: '6px', marginBottom: '15px' }}>
          <strong>API Connectivity Issue:</strong> {apiError}
        </div>
      )}

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '20px', marginTop: '20px' }}>
        
        {/* Left Column: Registered Technicians */}
        <div style={{ background: 'var(--bg-secondary, #fff)', padding: '20px', borderRadius: '8px', boxShadow: '0 2px 5px rgba(0,0,0,0.1)' }}>
          <h3><UserCheck size={20} /> Available Technicians</h3>
          <table style={{ width: '100%', borderCollapse: 'collapse', marginTop: '10px' }}>
            <thead>
              <tr style={{ background: '#3d6187', textAlign: 'left' }}>
                <th style={{ padding: '8px' }}>Code</th>
                <th style={{ padding: '8px' }}>Name</th>
                <th style={{ padding: '8px' }}>Skills</th>
                <th style={{ padding: '8px' }}>Status</th>
              </tr>
            </thead>
            <tbody>
              {technicians.length === 0 ? (
                <tr>
                  <td colSpan="4" style={{ padding: '12px', textAlign: 'center', color: '#999' }}>
                    No technicians found
                  </td>
                </tr>
              ) : (
                technicians.map((t) => (
                  <tr key={t.id} style={{ borderBottom: '1px solid #ddd' }}>
                    <td style={{ padding: '8px' }}>{t.employeeCode || t.employeeId}</td>
                    <td style={{ padding: '8px' }}><strong>{t.fullName || t.name}</strong></td>
                    <td style={{ padding: '8px' }}>{t.skills ? (Array.isArray(t.skills) ? t.skills.map(s => s.name || s).join(', ') : t.skills) : 'None'}</td>
                    <td style={{ padding: '8px', color: (t.status === 'Active' || t.isAvailable) ? 'green' : 'red' }}>
                      {t.status || (t.isAvailable ? 'Available' : 'Busy')}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* Right Column: AI Matching Panel */}
        <div style={{ background: 'var(--bg-secondary, #fff)', padding: '20px', borderRadius: '8px', boxShadow: '0 2px 5px rgba(0,0,0,0.1)' }}>
          <h3><Sparkles size={20} color="#6366f1" /> AI Technician Matching</h3>
          
          <div style={{ background: '#f8fafc', padding: '15px', borderRadius: '6px', margin: '15px 0', color: '#333' }}>
            <h4>Maintenance Requests — Agent Outputs ({requestOutputs.length})</h4>
            <div style={{ maxHeight: '300px', overflowY: 'auto' }}>
              {requestOutputs.length === 0 ? (
                <p>No Classification / Priority agent outputs available yet.</p>
              ) : (
                sortedRequestOutputs.map(r => (
                  <div key={r.requestId} style={{ padding: '10px 0', borderBottom: '1px solid #e2e8f0' }}>
                    <strong>Request #{r.requestNumber}{r.title ? ` — ${r.title}` : ''}</strong>
                    <p style={{ margin: '6px 0 0 0' }}><strong>Required Skill:</strong> {r.requiredSkill || 'Pending...'}</p>
                    <p style={{ margin: '2px 0 0 0' }}><strong>Priority:</strong> {r.priorityLevel || 'Pending...'}</p>
                    <button
                      onClick={() => handleRunAssignment(r)}
                      disabled={processingRequestId !== null || !isAssignmentReady(r)}
                      style={{
                        backgroundColor: '#6366f1', color: '#fff', border: 'none', padding: '10px 18px',
                        borderRadius: '5px', cursor: 'pointer', fontWeight: 'bold', marginTop: '8px'
                      }}
                    >
                      {String(processingRequestId) === String(r.requestId) ? 'AI Agent Analyzing...' : 'Run AI Assignment Agent'}
                    </button>
                  </div>
                ))
              )}
            </div>
          </div>

          {lastAssignment && (
            <div style={{ marginTop: '20px', padding: '15px', border: '2px solid #6366f1', borderRadius: '8px', background: '#eef2ff', color: '#333' }}>
              <h4 style={{ margin: '0 0 10px 0', color: '#4338ca' }}>Assigned Technician — Request #{lastAssignment.requestNumber}</h4>
              <p><strong>Technician:</strong> {lastAssignment.technicianName}</p>
              <p><strong>Match Confidence:</strong> {(lastAssignment.matchScore * 100).toFixed(0)}%</p>
              <p><strong>Reasoning:</strong> {lastAssignment.reasoning}</p>
            </div>
          )}

          {assignStatus && (
            <p style={{ marginTop: '10px', color: assignStatus.includes('failed') ? 'red' : 'green', fontWeight: 'bold' }}>
              <CheckCircle size={16} /> {assignStatus}
            </p>
          )}
        </div>

      </div>
    </div>
  );
}
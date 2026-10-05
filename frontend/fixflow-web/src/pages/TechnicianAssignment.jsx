import React, { useState, useEffect } from 'react';
import { UserCheck, Sparkles, CheckCircle } from 'lucide-react';
import { api , technicianApi } from '../services/api';

export default function TechnicianAssignment() {
  const [technicians, setTechnicians] = useState([]);
  const [loading, setLoading] = useState(false);
  const [apiError, setApiError] = useState('');
  
  const [requestData, setRequestData] = useState({
    requestId: '',
    requiredSkill: '',
    priorityLevel: ''
  });

  const [recommendation, setRecommendation] = useState(null);
  const [assignStatus, setAssignStatus] = useState('');

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

  const fetchLatestRequest = async () => {
    try {
      // Retrieving the basic data of the latest request and the required skill identified by the Classification Agent.
      const requestsRes = await technicianApi.fetchRequests();
      const requestsList = requestsRes.data?.items || requestsRes.data || [];
      
      if (requestsList.length > 0) {
        const latestReq = requestsList[0];
        const requestId = latestReq.id || latestReq.requestId;

        // Category/Skill obtained from Classification
        const dynamicSkill = latestReq.category || latestReq.requiredSkill || 'General';

        // Obtaining the priority level relevant to this specific request from the Priority Agent (Risk & Priority Assessment).
        let dynamicPriority = 'Normal';
        try {
          const assessmentRes = await api.get('/priority-assessments');
          const assessmentsList = assessmentRes.data?.items || assessmentRes.data || [];
          
          const matchedAssessment = assessmentsList.find(
            a => a.requestId === requestId || a.id === requestId
          ) || assessmentsList[0];
          
          if (matchedAssessment) {
            dynamicPriority = matchedAssessment.priority || matchedAssessment.riskLevel || 'Normal';
          }
        } catch (priorityErr) {
          console.warn("Could not fetch dynamic priority assessment:", priorityErr);
        }

        // Dynamically incorporating the actual outputs of both agents into the State.
        setRequestData({
          requestId: requestId,
          requiredSkill: dynamicSkill,
          priorityLevel: dynamicPriority
        });
      }
    } catch (err) {
      console.error("Error fetching dynamic request data:", err);
    }
  };

  useEffect(() => {
    fetchTechnicians();
    fetchLatestRequest();
  }, []);

  const handleGetRecommendation = async () => {
    setLoading(true);
    setAssignStatus('');
    try {
      const res = await technicianApi.fetchRecommendation({
        requestId: requestData.requestId,
        requiredSkill: requestData.requiredSkill,
        priorityLevel: requestData.priorityLevel
      });
      // Passing only the `.data` from the Axios response to the state.
      setRecommendation(res.data || res);
    } catch (err) {
      console.error("Error connecting via Gateway:", err);
      alert("Error: Make sure C# / Python backends are running.");
    } finally {
      setLoading(false);
    }
  };

  const handleAssign = async () => {
    // Obtaining the correct Technician ID from the 'topCandidate' or recommendation.
    const techId = 
      recommendation?.recommendedTechnicianId || 
      recommendation?.RecommendedTechnicianId || 
      recommendation?.top_match_id || 
      recommendation?.id ||
      // Obtaining the correct Technician ID from the 'topCandidate' or recommendation.
      technicians.find(t => (t.fullName || t.name) === displayTechnicianName)?.id;
    
    if (!techId) {
      alert("Please select a valid technician before assigning!");
      return;
    }

    try {
      await technicianApi.assignTechnician({
        requestId: String(requestData.requestId), // Sending the GUID as a string
        requiredSkill: String(requestData.requiredSkill),
        priorityLevel: String(requestData.priorityLevel),
        technicianId: techId
      });

      setAssignStatus('Technician assigned successfully!');
    } catch (err) {
      console.error("Error assigning technician:", err);
      setAssignStatus(`Assignment failed: ${err.message}`);
    }
  };

  // To securely handle various property formats coming from the backend
  const topCandidate = recommendation?.recommended_candidates?.[0] || recommendation || {};
  
  const displayTechnicianName = 
    topCandidate.technicianName || 
    topCandidate.TechnicianName || 
    topCandidate.technician_name || 
    topCandidate.name || 
    topCandidate.employeeCode || 
    'N/A';

  const displayMatchScore = topCandidate.matchScore || topCandidate.match_score || 0.85;
  const displayReasoning = topCandidate.reasoningSummary || topCandidate.ReasoningSummary || topCandidate.reasoning || 'Best fit based on availability and skills.';

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
            <h4>Maintenance Request (#{requestData.requestId || 'Loading...'})</h4>
            <p><strong>Required Skill:</strong> {requestData.requiredSkill || 'Pending...'}</p>
            <p><strong>Priority:</strong> {requestData.priorityLevel || 'Pending...'}</p>
          </div>

          <button 
            onClick={handleGetRecommendation}
            disabled={loading || !requestData.requestId}
            style={{
              backgroundColor: '#6366f1', color: '#fff', border: 'none', padding: '10px 18px',
              borderRadius: '5px', cursor: 'pointer', fontWeight: 'bold'
            }}
          >
            {loading ? 'AI Agent Analyzing...' : 'Run AI Assignment Agent'}
          </button>

          {recommendation && (
            <div style={{ marginTop: '20px', padding: '15px', border: '2px solid #6366f1', borderRadius: '8px', background: '#eef2ff', color: '#333' }}>
              <h4 style={{ margin: '0 0 10px 0', color: '#4338ca' }}>Recommended Candidate</h4>
              <p><strong>Technician:</strong> {displayTechnicianName}</p>
              <p><strong>Match Confidence:</strong> {(displayMatchScore * 100).toFixed(0)}%</p>
              <p><strong>Reasoning:</strong> {displayReasoning}</p>

              <button 
                onClick={handleAssign}
                style={{
                  backgroundColor: '#10b981', color: '#fff', border: 'none', padding: '8px 15px',
                  borderRadius: '5px', cursor: 'pointer', marginTop: '10px', fontWeight: 'bold'
                }}
              >
                Approve & Assign Job
              </button>
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
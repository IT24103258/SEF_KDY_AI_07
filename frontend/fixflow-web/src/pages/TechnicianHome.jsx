import React, { useState, useEffect } from 'react';
import { PageHeader, Card, LoadingState } from '../components/SharedUI';
import { technicianApi } from '../services/api';
import { useAuth } from '../hooks';

export const TechnicianHome = () => {
  const [jobs, setJobs] = useState([]);
  const [loading, setLoading] = useState(true);
  
  const { user } = useAuth(); 

  const userEmail = user?.email || localStorage.getItem('user_email');

  useEffect(() => {
    if (!userEmail) {
      setLoading(false);
      return;
    }

    technicianApi.fetchMyJobs(userEmail)
      .then((data) => {
        const jobsList = Array.isArray(data) 
          ? data 
          : (data?.jobs || data?.data || []);

        setJobs(jobsList);
      })
      .catch((err) => {
        console.error("Jobs fetch error:", err);
        setJobs([]);
      })
      .finally(() => setLoading(false));
  }, [userEmail]);

  if (loading) return <LoadingState />;

  const hasJobs = Array.isArray(jobs) && jobs.length > 0;

  return (
    <div>
      <PageHeader
        title="Technician Home"
        description={`Technician workspace for: ${userEmail || 'Technician'}`}
      />

      <Card title="My Assigned Work Orders">
        {!hasJobs ? (
          <p style={{ color: 'var(--text-secondary)' }}>
            No assigned maintenance requests found for you.
          </p>
        ) : (
          <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '0.9rem' }}>
            <thead>
              <tr style={{ borderBottom: '1px solid var(--border-color)', color: 'var(--text-secondary)' }}>
                <th style={{ padding: '10px' }}>Request ID</th>
                <th style={{ padding: '10px' }}>Assignment Note</th>
                <th style={{ padding: '10px' }}>Priority</th>
                <th style={{ padding: '10px' }}>Status</th>
              </tr>
            </thead>
            <tbody>
              {jobs.map((job, index) => (
                <tr key={job.assignmentId || index} style={{ borderBottom: '1px solid var(--border-color)' }}>
                  <td style={{ padding: '10px' }}>#{job.requestId}</td>
                  <td style={{ padding: '10px' }}>{job.requiredSkill || 'General'}</td>
                  <td style={{ padding: '10px' }}>{job.priorityLevel || 'Medium'}</td>
                  <td style={{ padding: '10px', color: 'var(--success-color)' }}>{job.status || 'Assigned'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Card>
    </div>
  );
};
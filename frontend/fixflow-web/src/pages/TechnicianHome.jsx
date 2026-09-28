import React, { useState, useEffect } from 'react';
import { PageHeader, Card, LoadingState } from '../components/SharedUI';
import { technicianApi } from '../services/api';

export const TechnicianHome = () => {
  const [jobs, setJobs] = useState([]);
  const [loading, setLoading] = useState(true);
  const userEmail = localStorage.getItem('user_email') || 'tech@fixflow.local';

  useEffect(() => {
    technicianApi.fetchMyJobs(userEmail)
      .then((data) => {
        // Check whether the response is a direct array or an array contained within an object
        const jobsList = Array.isArray(data) 
          ? data 
          : (data?.jobs || data?.data || []);

        setJobs(jobsList);
      })
      .catch((err) => {
        console.error("Jobs fetch error:", err);
        setJobs([]); // If an error occurs, set an empty array
      })
      .finally(() => setLoading(false));
  }, [userEmail]);

  if (loading) return <LoadingState />;

  // Safely checking if it is an array
  const hasJobs = Array.isArray(jobs) && jobs.length > 0;

  return (
    <div>
      <PageHeader
        title="Technician Home"
        description="Technician workspace for managing assigned maintenance requests"
      />

      <Card title="My Assigned Work Orders">
        {!hasJobs ? (
          <p style={{ color: 'var(--text-secondary)' }}>
            No assigned maintenance requests found.
          </p>
        ) : (
          <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '0.9rem' }}>
            <thead>
              <tr style={{ borderBottom: '1px solid var(--border-color)', color: 'var(--text-secondary)' }}>
                <th style={{ padding: '10px' }}>Request ID</th>
                <th style={{ padding: '10px' }}>Required Skill</th>
                <th style={{ padding: '10px' }}>Priority</th>
                <th style={{ padding: '10px' }}>Status</th>
              </tr>
            </thead>
            <tbody>
              {jobs.map((job, index) => (
                <tr key={job.requestId || job.id || index} style={{ borderBottom: '1px solid var(--border-color)' }}>
                  <td style={{ padding: '10px' }}>#{job.requestId || job.id}</td>
                  <td style={{ padding: '10px' }}>{job.requiredSkill || job.skill || 'General'}</td>
                  <td style={{ padding: '10px' }}>{job.priorityLevel || job.priority || 'Normal'}</td>
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
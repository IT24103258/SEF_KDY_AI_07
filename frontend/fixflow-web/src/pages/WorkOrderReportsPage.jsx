import React, { useState, useEffect } from 'react';
import { PageHeader, Card, StatusBadge, LoadingState, Button } from '../components/SharedUI';
import { workOrderApi } from '../services/workOrderApi';
import {
  BarChart3,
  CheckCircle2,
  Clock,
  AlertTriangle,
  Users,
  Activity,
  Calendar,
  TrendingUp,
  ShieldCheck,
  RefreshCw
} from 'lucide-react';

export const WorkOrderReportsPage = () => {
  const [report, setReport] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const fetchReports = async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await workOrderApi.getReports();
      if (res?.success) {
        setReport(res.data);
      }
    } catch (err) {
      setError(err.message || 'Failed to load scheduling reports.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchReports();
  }, []);

  if (loading) return <LoadingState message="Calculating scheduling analytics from database..." />;
  if (error || !report) {
    return (
      <div>
        <PageHeader title="Scheduling & Work Order Reports" description="Operational KPIs & performance analytics" />
        <Card style={{ color: 'var(--danger-color)', textAlign: 'center' }}>
          <p>{error || 'Failed to load report data.'}</p>
        </Card>
      </div>
    );
  }

  const maxWorkload = Math.max(...(report.technicianWorkloads?.map((t) => t.assignedJobs) || [1]), 1);

  return (
    <div>
      <PageHeader
        title="Scheduling & Work Order Analytics"
        description="Real-time operational KPI metrics, technician workload & SLA compliance tracking"
        action={
          <Button
            variant="secondary"
            onClick={fetchReports}
            style={{ display: 'flex', alignItems: 'center', gap: '6px' }}
          >
            <RefreshCw size={14} /> Refresh Data
          </Button>
        }
      />

      {/* KPI Cards Grid */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))',
          gap: '1rem',
          marginBottom: '1.5rem'
        }}
      >
        <Card>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '4px' }}>
            <span style={{ fontSize: '0.78rem', fontWeight: 650, color: 'var(--text-secondary)', textTransform: 'uppercase' }}>
              Total Jobs
            </span>
            <Activity size={18} color="var(--primary-color)" />
          </div>
          <div style={{ fontSize: '1.8rem', fontWeight: 700, color: 'var(--primary-color)' }}>
            {report.totalWorkOrders}
          </div>
        </Card>

        <Card>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '4px' }}>
            <span style={{ fontSize: '0.78rem', fontWeight: 650, color: 'var(--text-secondary)', textTransform: 'uppercase' }}>
              Completed
            </span>
            <CheckCircle2 size={18} color="var(--success-color)" />
          </div>
          <div style={{ fontSize: '1.8rem', fontWeight: 700, color: 'var(--success-color)' }}>
            {report.completedCount}
          </div>
        </Card>

        <Card>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '4px' }}>
            <span style={{ fontSize: '0.78rem', fontWeight: 650, color: 'var(--text-secondary)', textTransform: 'uppercase' }}>
              In Progress
            </span>
            <Clock size={18} color="#3b82f6" />
          </div>
          <div style={{ fontSize: '1.8rem', fontWeight: 700, color: '#3b82f6' }}>
            {report.inProgressCount}
          </div>
        </Card>

        <Card>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '4px' }}>
            <span style={{ fontSize: '0.78rem', fontWeight: 650, color: 'var(--text-secondary)', textTransform: 'uppercase' }}>
              Pending Review
            </span>
            <Clock size={18} color="var(--warning-color)" />
          </div>
          <div style={{ fontSize: '1.8rem', fontWeight: 700, color: 'var(--warning-color)' }}>
            {report.pendingApprovalCount}
          </div>
        </Card>

        <Card>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '4px' }}>
            <span style={{ fontSize: '0.78rem', fontWeight: 650, color: 'var(--text-secondary)', textTransform: 'uppercase' }}>
              SLA Compliance
            </span>
            <ShieldCheck size={18} color={report.slaComplianceRate >= 90 ? 'var(--success-color)' : 'var(--warning-color)'} />
          </div>
          <div style={{ fontSize: '1.8rem', fontWeight: 700, color: report.slaComplianceRate >= 90 ? 'var(--success-color)' : 'var(--warning-color)' }}>
            {report.slaComplianceRate}%
          </div>
        </Card>

        <Card>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '4px' }}>
            <span style={{ fontSize: '0.78rem', fontWeight: 650, color: 'var(--text-secondary)', textTransform: 'uppercase' }}>
              Conflicts Detected
            </span>
            <AlertTriangle size={18} color="var(--danger-color)" />
          </div>
          <div style={{ fontSize: '1.8rem', fontWeight: 700, color: 'var(--danger-color)' }}>
            {report.conflictCount}
          </div>
        </Card>
      </div>

      {/* 2-Column Analytics Visualizations */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1.5rem', marginBottom: '1.5rem' }}>
        {/* Status Distribution */}
        <Card title="Work Orders by Status">
          <div style={{ display: 'flex', flexDirection: 'column', gap: '12px', marginTop: '0.5rem' }}>
            {report.statusBreakdown && report.statusBreakdown.length > 0 ? (
              report.statusBreakdown.map((s, idx) => (
                <div key={idx}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.85rem', marginBottom: '4px' }}>
                    <span style={{ fontWeight: 600 }}>{s.status}</span>
                    <span style={{ color: 'var(--text-secondary)' }}>
                      {s.count} ({s.percentage}%)
                    </span>
                  </div>
                  <div
                    style={{
                      height: '8px',
                      backgroundColor: 'var(--glass-bg)',
                      borderRadius: '4px',
                      overflow: 'hidden'
                    }}
                  >
                    <div
                      style={{
                        width: `${s.percentage}%`,
                        height: '100%',
                        backgroundColor:
                          s.status === 'Completed'
                            ? 'var(--success-color)'
                            : s.status === 'InProgress'
                            ? '#3b82f6'
                            : s.status === 'Scheduled'
                            ? 'var(--primary-color)'
                            : 'var(--warning-color)',
                        borderRadius: '4px',
                        transition: 'width 0.3s ease'
                      }}
                    />
                  </div>
                </div>
              ))
            ) : (
              <p style={{ color: 'var(--text-secondary)', fontSize: '0.85rem' }}>No status data available.</p>
            )}
          </div>
        </Card>

        {/* Priority Breakdown */}
        <Card title="Work Orders by Priority">
          <div style={{ display: 'flex', flexDirection: 'column', gap: '12px', marginTop: '0.5rem' }}>
            {report.priorityBreakdown && report.priorityBreakdown.length > 0 ? (
              report.priorityBreakdown.map((p, idx) => {
                const total = report.totalWorkOrders || 1;
                const pct = Math.round((p.count / total) * 100);
                const color =
                  p.priority === 'Critical'
                    ? 'var(--danger-color)'
                    : p.priority === 'High'
                    ? 'var(--warning-color)'
                    : p.priority === 'Medium'
                    ? '#3b82f6'
                    : '#94a3b8';

                return (
                  <div key={idx}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.85rem', marginBottom: '4px' }}>
                      <span style={{ fontWeight: 600, color }}>{p.priority}</span>
                      <span style={{ color: 'var(--text-secondary)' }}>
                        {p.count} jobs ({pct}%)
                      </span>
                    </div>
                    <div
                      style={{
                        height: '8px',
                        backgroundColor: 'var(--glass-bg)',
                        borderRadius: '4px',
                        overflow: 'hidden'
                      }}
                    >
                      <div
                        style={{
                          width: `${pct}%`,
                          height: '100%',
                          backgroundColor: color,
                          borderRadius: '4px',
                          transition: 'width 0.3s ease'
                        }}
                      />
                    </div>
                  </div>
                );
              })
            ) : (
              <p style={{ color: 'var(--text-secondary)', fontSize: '0.85rem' }}>No priority data available.</p>
            )}
          </div>
        </Card>
      </div>

      {/* Technician Workload & Performance */}
      <Card title="Technician Workload & Performance">
        <div style={{ overflowX: 'auto', marginTop: '0.5rem' }}>
          <table
            style={{
              width: '100%',
              borderCollapse: 'collapse',
              textAlign: 'left',
              fontSize: '0.88rem'
            }}
          >
            <thead>
              <tr
                style={{
                  borderBottom: '1px solid var(--border-color)',
                  color: 'var(--text-secondary)',
                  fontWeight: 600
                }}
              >
                <th style={{ padding: '10px' }}>Technician</th>
                <th style={{ padding: '10px' }}>Assigned Jobs</th>
                <th style={{ padding: '10px' }}>In Progress</th>
                <th style={{ padding: '10px' }}>Completed</th>
                <th style={{ padding: '10px' }}>Workload Distribution</th>
              </tr>
            </thead>
            <tbody>
              {report.technicianWorkloads && report.technicianWorkloads.length > 0 ? (
                report.technicianWorkloads.map((tw) => (
                  <tr key={tw.technicianId} style={{ borderBottom: '1px solid var(--border-color)' }}>
                    <td style={{ padding: '10px', fontWeight: 600 }}>{tw.technicianName}</td>
                    <td style={{ padding: '10px' }}>{tw.assignedJobs}</td>
                    <td style={{ padding: '10px', color: '#3b82f6', fontWeight: 600 }}>{tw.inProgressJobs}</td>
                    <td style={{ padding: '10px', color: 'var(--success-color)', fontWeight: 600 }}>{tw.completedJobs}</td>
                    <td style={{ padding: '10px', width: '35%' }}>
                      <div
                        style={{
                          height: '10px',
                          backgroundColor: 'var(--glass-bg)',
                          borderRadius: '5px',
                          overflow: 'hidden'
                        }}
                      >
                        <div
                          style={{
                            width: `${Math.round((tw.assignedJobs / maxWorkload) * 100)}%`,
                            height: '100%',
                            backgroundColor: 'var(--primary-color)',
                            borderRadius: '5px'
                          }}
                        />
                      </div>
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={5} style={{ padding: '10px', textAlign: 'center', color: 'var(--text-secondary)' }}>
                    No technician workload data found.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </Card>
    </div>
  );
};

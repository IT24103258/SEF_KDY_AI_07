import React, { useEffect, useState } from 'react';
import { PageHeader, Card, StatusBadge, LoadingState } from '../components/SharedUI';
import { api } from '../services/api';

export { TechnicianHome } from './TechnicianHome';

export const Dashboard = () => {
  const [summary, setSummary] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.get('/reports/summary')
      .then(res => {
        if (res?.success) setSummary(res.data);
      })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <LoadingState />;

  return (
    <div>
      <PageHeader
        title="System Dashboard"
        description="Overview of apartment complex maintenance activity & Agentic AI workflows"
      />

      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
          gap: '1rem',
          marginBottom: '2rem'
        }}
      >
        <Card title="Total Requests">
          <p
            style={{
              fontSize: '2rem',
              fontWeight: '700',
              color: 'var(--primary-color)'
            }}
          >
            {summary?.totalRequests || 0}
          </p>
        </Card>

        <Card title="Pending Approvals">
          <p
            style={{
              fontSize: '2rem',
              fontWeight: '700',
              color: 'var(--warning-color)'
            }}
          >
            {summary?.pendingApprovals || 0}
          </p>
        </Card>

        <Card title="Active Workflows">
          <p
            style={{
              fontSize: '2rem',
              fontWeight: '700',
              color: 'var(--accent-color)'
            }}
          >
            {summary?.activeWorkflows || 0}
          </p>
        </Card>

        <Card title="Completed Orders">
          <p
            style={{
              fontSize: '2rem',
              fontWeight: '700',
              color: 'var(--success-color)'
            }}
          >
            {summary?.completedWorkOrders || 0}
          </p>
        </Card>
      </div>

      <Card title="System Status & API Gateway">
        <p
          style={{
            fontSize: '0.9rem',
            color: 'var(--text-secondary)'
          }}
        >
          ASP.NET Core Web API is active. React communicates strictly through
          REST gateway endpoints.
        </p>
      </Card>
    </div>
  );
};

export const Users = () => {
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.get('/users')
      .then(res => {
        if (res?.success) setUsers(res.data);
      })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <LoadingState />;

  return (
    <div>
      <PageHeader
        title="User Directory"
        description="Registered system users & role assignments"
      />

      <Card>
        <table
          style={{
            width: '100%',
            borderCollapse: 'collapse',
            textAlign: 'left',
            fontSize: '0.9rem'
          }}
        >
          <thead>
            <tr
              style={{
                borderBottom: '1px solid var(--border-color)',
                color: 'var(--text-secondary)'
              }}
            >
              <th style={{ padding: '10px' }}>Name</th>
              <th style={{ padding: '10px' }}>Email</th>
              <th style={{ padding: '10px' }}>Phone</th>
              <th style={{ padding: '10px' }}>Role</th>
            </tr>
          </thead>

          <tbody>
            {users.map(u => (
              <tr
                key={u.id}
                style={{
                  borderBottom: '1px solid var(--border-color)'
                }}
              >
                <td style={{ padding: '10px' }}>
                  {u.firstName} {u.lastName}
                </td>

                <td style={{ padding: '10px' }}>
                  {u.email}
                </td>

                <td style={{ padding: '10px' }}>
                  {u.phoneNumber}
                </td>

                <td style={{ padding: '10px' }}>
                  <StatusBadge status={u.role} />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </Card>
    </div>
  );
};

export const Locations = () => {
  const [locations, setLocations] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.get('/locations')
      .then(res => {
        if (res?.success) setLocations(res.data);
      })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <LoadingState />;

  return (
    <div>
      <PageHeader
        title="Apartment Complex Locations"
        description="Registered towers, floors, apartment units & common areas"
      />

      <Card>
        <table
          style={{
            width: '100%',
            borderCollapse: 'collapse',
            textAlign: 'left',
            fontSize: '0.9rem'
          }}
        >
          <thead>
            <tr
              style={{
                borderBottom: '1px solid var(--border-color)',
                color: 'var(--text-secondary)'
              }}
            >
              <th style={{ padding: '10px' }}>Location Name</th>
              <th style={{ padding: '10px' }}>Building</th>
              <th style={{ padding: '10px' }}>Floor</th>
              <th style={{ padding: '10px' }}>Room</th>
              <th style={{ padding: '10px' }}>Coordinates</th>
            </tr>
          </thead>

          <tbody>
            {locations.map(l => (
              <tr
                key={l.id}
                style={{
                  borderBottom: '1px solid var(--border-color)'
                }}
              >
                <td
                  style={{
                    padding: '10px',
                    fontWeight: '600'
                  }}
                >
                  {l.name}
                </td>

                <td style={{ padding: '10px' }}>
                  {l.building}
                </td>

                <td style={{ padding: '10px' }}>
                  {l.floor}
                </td>

                <td style={{ padding: '10px' }}>
                  {l.room}
                </td>

                <td style={{ padding: '10px' }}>
                  {l.latitude}, {l.longitude}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </Card>
    </div>
  );
};

export const Assets = () => {
  const [assets, setAssets] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.get('/assets')
      .then(res => {
        if (res?.success) setAssets(res.data);
      })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <LoadingState />;

  return (
    <div>
      <PageHeader
        title="Complex Assets"
        description="Apartment complex equipment registry & criticality classification"
      />

      <Card>
        <table
          style={{
            width: '100%',
            borderCollapse: 'collapse',
            textAlign: 'left',
            fontSize: '0.9rem'
          }}
        >
          <thead>
            <tr
              style={{
                borderBottom: '1px solid var(--border-color)',
                color: 'var(--text-secondary)'
              }}
            >
              <th style={{ padding: '10px' }}>Asset Name</th>
              <th style={{ padding: '10px' }}>Code</th>
              <th style={{ padding: '10px' }}>Category</th>
              <th style={{ padding: '10px' }}>Criticality</th>
              <th style={{ padding: '10px' }}>Location</th>
            </tr>
          </thead>

          <tbody>
            {assets.map(a => (
              <tr
                key={a.id}
                style={{
                  borderBottom: '1px solid var(--border-color)'
                }}
              >
                <td
                  style={{
                    padding: '10px',
                    fontWeight: '600'
                  }}
                >
                  {a.name}
                </td>

                <td style={{ padding: '10px' }}>
                  {a.assetCode}
                </td>

                <td style={{ padding: '10px' }}>
                  {a.category}
                </td>

                <td style={{ padding: '10px' }}>
                  <StatusBadge status={a.criticality} />
                </td>

                <td style={{ padding: '10px' }}>
                  {a.locationName}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </Card>
    </div>
  );
};

export const Workflows = () => (
  <div>
    <PageHeader
      title="Agentic AI Workflows"
      description="Persisted execution pipelines & tool call logs"
    />

    <Card title="Agent Pipeline State">
      <p style={{ color: 'var(--text-secondary)' }}>
        Workflow executions are tracked in PostgreSQL and exposed via
        ASP.NET Core API.
      </p>
    </Card>
  </div>
);

export const Approvals = () => (
  <div>
    <PageHeader
      title="Human Approvals"
      description="Human-in-the-loop review queue for high-risk agent decisions"
    />

    <Card title="Pending Review Queue">
      <p style={{ color: 'var(--text-secondary)' }}>
        No pending approvals requiring manual intervention at this time.
      </p>
    </Card>
  </div>
);

export const Reports = () => (
  <div>
    <PageHeader
      title="Reports & Analytics"
      description="System metrics & maintenance performance"
    />

    <Card title="Operational Summary">
      <p style={{ color: 'var(--text-secondary)' }}>
        Detailed metrics will populate as maintenance workflows execute.
      </p>
    </Card>
  </div>
);

/* Customer requests landing page */
export const CustomerRequests = () => (
  <div>
    <PageHeader
      title="My Maintenance Requests"
      description="View and track your submitted maintenance requests"
    />

    <Card title="Customer Requests">
      <p style={{ color: 'var(--text-secondary)' }}>
        Your submitted maintenance requests and their current statuses
        will appear here.
      </p>
    </Card>
  </div>
);

export const NotFound = () => (
  <div
    style={{
      textAlign: 'center',
      padding: '5rem 1rem'
    }}
  >
    <h1
      style={{
        fontSize: '3rem',
        fontWeight: '800',
        color: 'var(--primary-color)'
      }}
    >
      404
    </h1>

    <p
      style={{
        fontSize: '1.2rem',
        color: 'var(--text-secondary)',
        marginBottom: '1.5rem'
      }}
    >
      Page Not Found
    </p>
  </div>
);
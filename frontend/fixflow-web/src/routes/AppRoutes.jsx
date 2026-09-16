import React from 'react';
import {
  Routes,
  Route,
  Navigate
} from 'react-router-dom';

import { useAuth } from '../hooks';
import { MainLayout } from '../layouts/MainLayout';

import { Login } from '../pages/Login';
import { Register } from '../pages/Register';

import {
  Dashboard,
  Users,
  Locations,
  Assets,
  Workflows,
  Approvals,
  Reports,
  TechnicianHome,
  CustomerRequests,
  NotFound
} from '../pages/Pages';

import TechnicianAssignment from '../pages/TechnicianAssignment';

import { getHomePath } from '../utils/roleRoutes';

/*
================================================================================
FIXFLOW SHARED FOUNDATION APP ROUTES
================================================================================

IMPORTANT FOR ALL 4 MEMBERS:

Add your page imports and route elements directly into your designated
section below.

Do NOT create complex dynamic route registries.
================================================================================
*/

const ADMIN_ROLES = [
  'Administrator',
  'Manager'
];

/*
================================================================================
PROTECTED ROUTE
================================================================================

- Checks whether the user is authenticated.
- If roles are provided, checks whether the user's role is allowed.
- If the role is not allowed, redirects the user to their role-specific home.
================================================================================
*/

const ProtectedRoute = ({
  children,
  roles
}) => {
  const {
    isAuthenticated,
    loading,
    user
  } = useAuth();

  // Wait until authentication state is restored
  if (loading) {
    return null;
  }

  // User is not logged in
  if (!isAuthenticated) {
    return (
      <Navigate
        to="/login"
        replace
      />
    );
  }

  // User does not have permission
  if (
    roles &&
    !roles.includes(user?.role)
  ) {
    return (
      <Navigate
        to={getHomePath(user?.role)}
        replace
      />
    );
  }

  // User is authenticated and authorized
  return (
    <MainLayout>
      {children}
    </MainLayout>
  );
};

export const AppRoutes = () => {
  return (
    <Routes>

      {/* ============================================================
          AUTHENTICATION ROUTES
          ============================================================ */}

      <Route
        path="/login"
        element={<Login />}
      />

      <Route
        path="/register"
        element={<Register />}
      />


      {/* ============================================================
          SHARED FOUNDATION — ADMIN / MANAGER ROUTES
          ============================================================ */}

      <Route
        path="/"
        element={
          <ProtectedRoute
            roles={ADMIN_ROLES}
          >
            <Dashboard />
          </ProtectedRoute>
        }
      />

      <Route
        path="/users"
        element={
          <ProtectedRoute
            roles={ADMIN_ROLES}
          >
            <Users />
          </ProtectedRoute>
        }
      />

      <Route
        path="/locations"
        element={
          <ProtectedRoute
            roles={ADMIN_ROLES}
          >
            <Locations />
          </ProtectedRoute>
        }
      />

      <Route
        path="/assets"
        element={
          <ProtectedRoute
            roles={ADMIN_ROLES}
          >
            <Assets />
          </ProtectedRoute>
        }
      />

      <Route
        path="/workflows"
        element={
          <ProtectedRoute
            roles={ADMIN_ROLES}
          >
            <Workflows />
          </ProtectedRoute>
        }
      />

      <Route
        path="/approvals"
        element={
          <ProtectedRoute
            roles={ADMIN_ROLES}
          >
            <Approvals />
          </ProtectedRoute>
        }
      />

      <Route
        path="/reports"
        element={
          <ProtectedRoute
            roles={ADMIN_ROLES}
          >
            <Reports />
          </ProtectedRoute>
        }
      />


      {/* ============================================================
          MEMBER 1 — REQUEST INTAKE & CLASSIFICATION
          ADD YOUR ROUTES ONLY IN THIS SECTION
          ============================================================ */}

      {/* Example:

      <Route
        path="/intake"
        element={
          <ProtectedRoute
            roles={['Requester']}
          >
            <RequestIntakePage />
          </ProtectedRoute>
        }
      />

      */}


      {/* ============================================================
          MEMBER 2 — RISK & PRIORITY ASSESSMENT
          ADD YOUR ROUTES ONLY IN THIS SECTION
          ============================================================ */}

      {/* Example:

      <Route
        path="/priority-dashboard"
        element={
          <ProtectedRoute
            roles={ADMIN_ROLES}
          >
            <RiskDashboardPage />
          </ProtectedRoute>
        }
      />

      */}


      {/* ============================================================
          MEMBER 3 — TECHNICIAN MATCHING & ASSIGNMENT
          ADD YOUR ROUTES ONLY IN THIS SECTION
          ============================================================ */}

      <Route
        path="/technician"
        element={
          <ProtectedRoute
            roles={['Technician']}
          >
            <TechnicianHome />
          </ProtectedRoute>
        }
      />

      <Route
        path="/assignments"
        element={
          <ProtectedRoute>
            <TechnicianAssignment />
          </ProtectedRoute>
        }
      />

      {/* Example:

      <Route
        path="/dispatch"
        element={
          <ProtectedRoute
            roles={[
              'Administrator',
              'Manager'
            ]}
          >
            <DispatchBoardPage />
          </ProtectedRoute>
        }
      />

      */}


      {/* ============================================================
          MEMBER 4 — SCHEDULING & WORK ORDER MANAGEMENT
          ============================================================ */}

      <Route
        path="/my-requests"
        element={
          <ProtectedRoute
            roles={['Requester']}
          >
            <CustomerRequests />
          </ProtectedRoute>
        }
      />

      {/* Example:

      <Route
        path="/calendar"
        element={
          <ProtectedRoute
            roles={ADMIN_ROLES}
          >
            <SchedulingCalendarPage />
          </ProtectedRoute>
        }
      />

      */}


      {/* ============================================================
          404 CATCH-ALL ROUTE
          ============================================================ */}

      <Route
        path="*"
        element={
          <ProtectedRoute>
            <NotFound />
          </ProtectedRoute>
        }
      />

    </Routes>
  );
};
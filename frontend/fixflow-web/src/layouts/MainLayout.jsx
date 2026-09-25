import React, { useState } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import {
  LayoutDashboard,
  Users,
  MapPin,
  Boxes,
  Workflow,
  BarChart3,
  Wrench,
  ClipboardList,
  CalendarDays,
  CheckCircle,
  TrendingUp,
  Sun,
  Moon,
  LogOut,
  Menu,
  X
} from 'lucide-react';

import { useAuth } from '../hooks';
import { useTheme } from '../hooks';
import { BrandMark } from '../components/BrandMark';

const ADMIN_ROLES = ['Administrator', 'Manager'];

// Flat nav items for non-grouped entries
const topNavItems = [
  {
    label: 'Dashboard',
    path: '/',
    icon: LayoutDashboard,
    roles: ADMIN_ROLES
  },
  {
    label: 'Users',
    path: '/users',
    icon: Users,
    roles: ADMIN_ROLES
  },
  {
    label: 'Locations',
    path: '/locations',
    icon: MapPin,
    roles: ADMIN_ROLES
  },
  {
    label: 'Assets',
    path: '/assets',
    icon: Boxes,
    roles: ADMIN_ROLES
  },
  {
    label: 'Workflows',
    path: '/workflows',
    icon: Workflow,
    roles: ADMIN_ROLES
  },
  {
    label: 'My Work Orders',
    path: '/technician',
    icon: Wrench,
    roles: ['Technician']
  },
  {
    label: 'My Requests',
    path: '/my-requests',
    icon: ClipboardList,
    roles: ['Requester']
  }
];

// Component 4 grouped navigation
const schedulingGroup = {
  label: 'Scheduling & Work Orders',
  icon: Wrench,
  roles: ADMIN_ROLES,
  children: [
    { label: 'Work Orders', path: '/work-orders', icon: ClipboardList },
    { label: 'Approval Center', path: '/approval-center', icon: CheckCircle },
    { label: 'Schedule Board', path: '/calendar', icon: CalendarDays },
    { label: 'Reports & Analytics', path: '/reports/scheduling', icon: TrendingUp }
  ]
};

export const MainLayout = ({ children }) => {
  const { user, logout } = useAuth();
  const { theme, toggleTheme } = useTheme();

  const navigate = useNavigate();
  const location = useLocation();

  const [menuOpen, setMenuOpen] = useState(false);

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const initials =
    `${user?.firstName?.[0] || ''}${user?.lastName?.[0] || ''}`
      .toUpperCase() || 'U';

  // Show only navigation items allowed for the logged-in user's role
  const visibleTopItems = topNavItems.filter(item =>
    item.roles.includes(user?.role)
  );

  const showSchedulingGroup = schedulingGroup.roles.includes(user?.role);

  const NavLink = ({ item }) => {
    const active = location.pathname === item.path ||
      (item.path !== '/' && location.pathname.startsWith(item.path));
    const Icon = item.icon;

    return (
      <Link
        to={item.path}
        className={`ff-nav-link ${active ? 'active' : ''}`}
        onClick={() => setMenuOpen(false)}
      >
        <Icon size={16} />
        {item.label}
      </Link>
    );
  };

  // Sub-nav link for grouped items (slightly indented in mobile, same style on desktop)
  const SubNavLink = ({ item }) => {
    const active = location.pathname === item.path ||
      (item.path !== '/' && location.pathname.startsWith(item.path));
    const Icon = item.icon;

    return (
      <Link
        to={item.path}
        className={`ff-nav-link ${active ? 'active' : ''}`}
        onClick={() => setMenuOpen(false)}
        style={{ paddingLeft: '18px', fontSize: '0.82rem' }}
      >
        <Icon size={14} />
        {item.label}
      </Link>
    );
  };

  // Scheduling group is active if any child route matches
  const schedulingActive = schedulingGroup.children.some(
    c => location.pathname === c.path || location.pathname.startsWith(c.path)
  );

  const GroupIcon = schedulingGroup.icon;

  return (
    <div style={{ minHeight: '100vh' }}>

      {/* Top Navigation Bar */}
      <header className="ff-topbar glass-strong">

        <BrandMark />

        {/* Desktop Navigation */}
        <nav className="ff-nav-links" style={{ alignItems: 'center', position: 'relative' }}>
          {visibleTopItems.map(item => (
            <NavLink key={item.path} item={item} />
          ))}

          {/* Scheduling & Work Orders Tab -> Navigates directly to /work-orders without dropdown arrow */}
          {showSchedulingGroup && (
            <Link
              to="/work-orders"
              className={`ff-nav-link ${schedulingActive ? 'active' : ''}`}
              onClick={() => setMenuOpen(false)}
            >
              <GroupIcon size={16} />
              Scheduling &amp; Work Orders
            </Link>
          )}
        </nav>

        {/* Navigation Actions */}
        <div className="ff-nav-actions">

          {/* Theme Toggle */}
          <button
            className="ff-icon-btn"
            onClick={toggleTheme}
            aria-label="Toggle theme"
            title="Toggle theme"
          >
            {theme === 'light' ? (
              <Moon size={17} />
            ) : (
              <Sun size={17} />
            )}
          </button>

          {/* User Information */}
          <div className="ff-user-chip">
            <div className="ff-avatar">
              {initials}
            </div>

            <div
              style={{
                display: 'flex',
                flexDirection: 'column',
                lineHeight: 1.15
              }}
            >
              <span
                style={{
                  fontSize: '0.8rem',
                  fontWeight: 600
                }}
              >
                {user?.firstName} {user?.lastName}
              </span>

              <span
                style={{
                  fontSize: '0.7rem',
                  color: 'var(--text-secondary)'
                }}
              >
                {user?.role}
              </span>
            </div>
          </div>

          {/* Logout */}
          <button
            className="ff-icon-btn"
            onClick={handleLogout}
            aria-label="Log out"
            title="Log out"
          >
            <LogOut size={17} />
          </button>

          {/* Mobile Menu Toggle */}
          <button
            className="ff-icon-btn ff-mobile-toggle"
            onClick={() => setMenuOpen(o => !o)}
            aria-label="Toggle menu"
          >
            {menuOpen ? (
              <X size={18} />
            ) : (
              <Menu size={18} />
            )}
          </button>

        </div>
      </header>

      {/* Component 4 Sub-Navigation Bar */}
      {showSchedulingGroup && schedulingActive && (
        <div
          className="glass"
          style={{
            borderBottom: '1px solid var(--border-color)',
            backgroundColor: 'var(--glass-bg)',
            backdropFilter: 'blur(var(--glass-blur))',
            WebkitBackdropFilter: 'blur(var(--glass-blur))'
          }}
        >
          <div
            style={{
              maxWidth: '1280px',
              margin: '0 auto',
              padding: '6px 1.5rem',
              display: 'flex',
              alignItems: 'center',
              gap: '6px',
              overflowX: 'auto',
              scrollbarWidth: 'none'
            }}
          >
            {schedulingGroup.children.map(child => {
              const isSubActive =
                location.pathname === child.path ||
                (child.path === '/work-orders' && location.pathname.startsWith('/work-orders/'));
              const SubIcon = child.icon;

              return (
                <Link
                  key={child.path}
                  to={child.path}
                  className={`ff-nav-link ${isSubActive ? 'active' : ''}`}
                  style={{
                    fontSize: '0.84rem',
                    padding: '5px 12px',
                    borderRadius: 'var(--radius-pill)'
                  }}
                >
                  <SubIcon size={14} />
                  {child.label}
                </Link>
              );
            })}
          </div>
        </div>
      )}

      {/* Mobile Navigation */}
      <div
        className={`ff-mobile-panel glass-strong ${
          menuOpen ? 'open' : ''
        }`}
      >
        {visibleTopItems.map(item => (
          <NavLink key={item.path} item={item} />
        ))}

        {/* Scheduling group in mobile */}
        {showSchedulingGroup && (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '2px', marginTop: '4px' }}>
            <Link
              to="/work-orders"
              className={`ff-nav-link ${schedulingActive ? 'active' : ''}`}
              onClick={() => setMenuOpen(false)}
            >
              <GroupIcon size={16} />
              Scheduling &amp; Work Orders
            </Link>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '2px', paddingLeft: '14px' }}>
              {schedulingGroup.children.map(child => {
                const isSubActive =
                  location.pathname === child.path ||
                  (child.path === '/work-orders' && location.pathname.startsWith('/work-orders/'));
                const SubIcon = child.icon;
                return (
                  <Link
                    key={child.path}
                    to={child.path}
                    className={`ff-nav-link ${isSubActive ? 'active' : ''}`}
                    onClick={() => setMenuOpen(false)}
                    style={{ fontSize: '0.82rem' }}
                  >
                    <SubIcon size={14} />
                    {child.label}
                  </Link>
                );
              })}
            </div>
          </div>
        )}
      </div>

      {/* Main Content */}
      <main
        style={{
          padding: '2rem',
          maxWidth: '1280px',
          margin: '0 auto'
        }}
      >
        {children}
      </main>

    </div>
  );
};
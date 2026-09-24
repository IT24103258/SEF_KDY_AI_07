import React, { useState } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import {
  LayoutDashboard,
  Users,
  MapPin,
  Boxes,
  Workflow,
  CheckSquare,
  BarChart3,
  Wrench,
  ClipboardList,
  Calendar,
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

const navItems = [
  {
    label: 'Dashboard',
    path: '/',
    icon: LayoutDashboard,
    roles: ADMIN_ROLES
  },
  {
    label: 'Work Orders',
    path: '/work-orders',
    icon: Wrench,
    roles: ADMIN_ROLES
  },
  {
    label: 'Approval Center',
    path: '/approval-center',
    icon: CheckSquare,
    roles: ADMIN_ROLES
  },
  {
    label: 'Calendar',
    path: '/calendar',
    icon: Calendar,
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
    label: 'Reports',
    path: '/reports/scheduling',
    icon: BarChart3,
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
  const visibleNavItems = navItems.filter(item =>
    item.roles.includes(user?.role)
  );

  const NavLink = ({ item }) => {
    const active = location.pathname === item.path;
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

  return (
    <div style={{ minHeight: '100vh' }}>

      {/* Top Navigation Bar */}
      <header className="ff-topbar glass-strong">

        <BrandMark />

        {/* Desktop Navigation */}
        <nav className="ff-nav-links">
          {visibleNavItems.map(item => (
            <NavLink key={item.path} item={item} />
          ))}
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

      {/* Mobile Navigation */}
      <div
        className={`ff-mobile-panel glass-strong ${
          menuOpen ? 'open' : ''
        }`}
      >
        {visibleNavItems.map(item => (
          <NavLink key={item.path} item={item} />
        ))}
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
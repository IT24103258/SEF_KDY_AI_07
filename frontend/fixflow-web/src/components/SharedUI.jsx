import React from 'react';

export const Button = ({ children, variant = 'primary', size = 'md', className = '', ...props }) => {
  const sizeClass = size === 'sm' ? 'ff-btn-sm' : size === 'lg' ? 'ff-btn-lg' : '';
  const variantClass = `ff-btn-${variant}`;

  return (
    <button className={`ff-btn ${variantClass} ${sizeClass} ${className}`.trim()} {...props}>
      {children}
    </button>
  );
};

export const Input = ({ label, error, icon, className = '', ...props }) => (
  <div className="ff-field">
    {label && <label className="ff-label">{label}</label>}
    <div className="ff-input-wrap">
      {icon && <span className="ff-input-icon">{icon}</span>}
      <input
        className={`ff-input ${icon ? 'ff-input-has-icon' : ''} ${error ? 'ff-input-error' : ''} ${className}`.trim()}
        {...props}
      />
    </div>
    {error && <span className="ff-error-text">{error}</span>}
  </div>
);

export const Card = ({ title, subtitle, children, action, className = '' }) => (
  <div className={`ff-card ${className}`.trim()}>
    {(title || action) && (
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
        <div>
          {title && <h3 style={{ fontFamily: 'var(--font-display)', fontSize: '1.05rem', fontWeight: '650' }}>{title}</h3>}
          {subtitle && <p style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', marginTop: '2px' }}>{subtitle}</p>}
        </div>
        {action}
      </div>
    )}
    {children}
  </div>
);

export const StatusBadge = ({ status }) => {
  const getColors = () => {
    switch (status?.toLowerCase()) {
      case 'completed': return { bg: 'rgba(74, 222, 128, 0.16)', color: 'var(--success-color)' };
      case 'inreview':
      case 'pending': return { bg: 'rgba(251, 191, 36, 0.16)', color: 'var(--warning-color)' };
      case 'approved': return { bg: 'var(--primary-light)', color: 'var(--primary-color)' };
      case 'rejected':
      case 'cancelled': return { bg: 'rgba(255, 107, 113, 0.16)', color: 'var(--danger-color)' };
      default: return { bg: 'var(--glass-bg)', color: 'var(--text-secondary)' };
    }
  };

  const style = getColors();

  return (
    <span className="ff-badge" style={{ backgroundColor: style.bg, color: style.color }}>
      {status}
    </span>
  );
};

export const LoadingState = ({ message = 'Loading FixFlow AI data...' }) => (
  <div style={{ padding: '3rem', textAlign: 'center', color: 'var(--text-secondary)' }}>
    <p>{message}</p>
  </div>
);

export const EmptyState = ({ title = 'No Data Available', description = 'There are no items to display.' }) => (
  <div className="ff-empty">
    <h4 style={{ color: 'var(--text-primary)', marginBottom: '0.5rem', fontFamily: 'var(--font-display)' }}>{title}</h4>
    <p style={{ color: 'var(--text-secondary)', fontSize: '0.9rem' }}>{description}</p>
  </div>
);

export const PageHeader = ({ title, description, action }) => (
  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.75rem', flexWrap: 'wrap', gap: '1rem' }}>
    <div>
      <h1 style={{ fontFamily: 'var(--font-display)', fontSize: '1.6rem', fontWeight: '700' }}>{title}</h1>
      {description && <p style={{ color: 'var(--text-secondary)', fontSize: '0.92rem', marginTop: '4px' }}>{description}</p>}
    </div>
    {action}
  </div>
);

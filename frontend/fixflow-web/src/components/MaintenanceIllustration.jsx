import React from 'react';

/**
 * Original decorative artwork for the login screen: a maintenance ticket
 * flowing through an AI agent hub and out to a completed work order.
 * Built from plain shapes (no external imagery) so it themes cleanly with
 * the app's CSS variables in both light and dark mode.
 */
export const MaintenanceIllustration = () => (
  <svg viewBox="0 0 640 620" width="100%" height="100%" style={{ maxWidth: 560 }} aria-hidden="true">
    <defs>
      <linearGradient id="hubGradient" x1="0" y1="0" x2="1" y2="1">
        <stop offset="0%" stopColor="var(--primary-color)" />
        <stop offset="100%" stopColor="var(--accent-color)" />
      </linearGradient>
      <linearGradient id="panelGradient" x1="0" y1="0" x2="0" y2="1">
        <stop offset="0%" stopColor="var(--glass-bg-strong)" />
        <stop offset="100%" stopColor="var(--glass-bg)" />
      </linearGradient>
    </defs>

    {/* backdrop glass panel */}
    <rect x="40" y="60" width="560" height="500" rx="34" fill="url(#panelGradient)" stroke="var(--glass-border)" />

    {/* flow paths */}
    <path d="M 168 190 C 260 170, 300 230, 360 260" fill="none" stroke="var(--primary-color)" strokeWidth="2.5" strokeDasharray="2 10" strokeLinecap="round" opacity="0.8" />
    <path d="M 400 320 C 420 380, 400 430, 330 470" fill="none" stroke="var(--accent-color)" strokeWidth="2.5" strokeDasharray="2 10" strokeLinecap="round" opacity="0.8" />
    <path d="M 400 300 C 460 280, 480 220, 470 170" fill="none" stroke="var(--warning-color)" strokeWidth="2.5" strokeDasharray="2 10" strokeLinecap="round" opacity="0.7" />

    {/* ticket card (incoming request) */}
    <g transform="translate(88, 128)">
      <rect width="180" height="118" rx="18" fill="var(--bg-card)" stroke="var(--glass-border)" />
      <circle cx="26" cy="28" r="7" fill="var(--warning-color)" />
      <rect x="42" y="22" width="86" height="10" rx="5" fill="var(--text-secondary)" opacity="0.55" />
      <rect x="18" y="54" width="144" height="8" rx="4" fill="var(--text-secondary)" opacity="0.3" />
      <rect x="18" y="72" width="112" height="8" rx="4" fill="var(--text-secondary)" opacity="0.3" />
      <rect x="18" y="94" width="60" height="16" rx="8" fill="var(--primary-light)" />
      <rect x="26" y="99" width="44" height="6" rx="3" fill="var(--primary-color)" />
    </g>

    {/* AI agent hub */}
    <g transform="translate(320, 250)">
      <circle r="62" fill="url(#hubGradient)" opacity="0.16" />
      <circle r="46" fill="url(#hubGradient)" />
      <circle r="46" fill="none" stroke="var(--bg-primary)" strokeOpacity="0.15" strokeWidth="1" />
      <g stroke="#04141a" strokeWidth="3.4" strokeLinecap="round">
        <circle r="15" fill="none" />
        <line x1="0" y1="-24" x2="0" y2="-16" />
        <line x1="0" y1="24" x2="0" y2="16" />
        <line x1="-24" y1="0" x2="-16" y2="0" />
        <line x1="24" y1="0" x2="16" y2="0" />
        <line x1="-17" y1="-17" x2="-11" y2="-11" />
        <line x1="17" y1="17" x2="11" y2="11" />
        <line x1="-17" y1="17" x2="-11" y2="11" />
        <line x1="17" y1="-17" x2="11" y2="-11" />
      </g>
    </g>

    {/* technician chip */}
    <g transform="translate(432, 118)">
      <rect width="112" height="56" rx="16" fill="var(--bg-card)" stroke="var(--glass-border)" />
      <circle cx="28" cy="28" r="14" fill="var(--accent-light)" />
      <path d="M 22 28 l 4 4 l 8 -9" fill="none" stroke="var(--accent-color)" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round" />
      <rect x="52" y="18" width="46" height="7" rx="3.5" fill="var(--text-secondary)" opacity="0.5" />
      <rect x="52" y="32" width="34" height="7" rx="3.5" fill="var(--text-secondary)" opacity="0.3" />
    </g>

    {/* completed work order chip */}
    <g transform="translate(258, 452)">
      <rect width="150" height="70" rx="18" fill="var(--bg-card)" stroke="var(--glass-border)" />
      <circle cx="30" cy="35" r="16" fill="var(--primary-light)" />
      <path d="M 23 35 l 5 5 l 10 -11" fill="none" stroke="var(--primary-color)" strokeWidth="2.6" strokeLinecap="round" strokeLinejoin="round" />
      <rect x="58" y="22" width="76" height="8" rx="4" fill="var(--text-secondary)" opacity="0.5" />
      <rect x="58" y="40" width="52" height="8" rx="4" fill="var(--success-color)" opacity="0.6" />
    </g>

    {/* floating dots */}
    <circle cx="140" cy="320" r="5" fill="var(--warning-color)" opacity="0.7" />
    <circle cx="500" cy="360" r="6" fill="var(--accent-color)" opacity="0.6" />
    <circle cx="470" cy="470" r="4" fill="var(--primary-color)" opacity="0.7" />
    <circle cx="110" cy="440" r="4" fill="var(--accent-color)" opacity="0.5" />
  </svg>
);

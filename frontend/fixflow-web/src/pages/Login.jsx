import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Mail, Lock, ShieldCheck } from 'lucide-react';

import { useAuth } from '../hooks';
import { getHomePath } from '../utils/roleRoutes';

import { Button, Input } from '../components/SharedUI';
import { BrandMark } from '../components/BrandMark';
import { MaintenanceIllustration } from '../components/MaintenanceIllustration';

export const Login = () => {
  const [email, setEmail] = useState('admin@fixflow.local');
  const [password, setPassword] = useState('Admin123!');
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const { login } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();

    setError('');
    setSubmitting(true);

    try {
      const result = await login(email, password);

      // Redirect user according to their role
      navigate(getHomePath(result.user?.role), {
        replace: true
      });
    } catch (err) {
      setError(err.message || 'Invalid credentials');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="ff-login-shell">

      {/* Login Form Column */}
      <div className="ff-login-form-col">
        <div className="ff-login-card glass-strong">

          <div className="ff-login-header">
            <BrandMark />

            <div
              className="ff-login-badge"
              style={{ marginTop: '1.4rem' }}
            >
              <ShieldCheck size={14} />
              Maintenance Management Gateway
            </div>

            <h1 className="ff-login-title">
              Sign in to FixFlow
            </h1>

            <p className="ff-login-subtitle">
              Track requests, dispatch technicians and let the
              agent workflow do the routing.
            </p>
          </div>

          {/* Error Message */}
          {error && (
            <div className="ff-alert">
              {error}
            </div>
          )}

          {/* Login Form */}
          <form onSubmit={handleSubmit}>

            <Input
              label="Email address"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              icon={<Mail size={16} />}
              placeholder="you@fixflow.local"
              required
            />

            <Input
              label="Password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              icon={<Lock size={16} />}
              placeholder="••••••••"
              required
            />

            <Button
              type="submit"
              variant="primary"
              size="lg"
              style={{
                width: '100%',
                marginTop: '0.4rem'
              }}
              disabled={submitting}
            >
              {submitting ? 'Signing in…' : 'Sign in'}
            </Button>

          </form>

          {/* Register Link */}
          <p
            style={{
              marginTop: '1.2rem',
              fontSize: '0.85rem',
              color: 'var(--text-secondary)',
              textAlign: 'center'
            }}
          >
            New here?{' '}
            <Link
              to="/register"
              style={{ fontWeight: 600 }}
            >
              Create an account
            </Link>
          </p>

          {/* Demo Accounts */}
          <div className="ff-demo-box">
            <strong>Demo accounts</strong>

            <span>
              Admin — admin@fixflow.local / Admin123!
            </span>

            <span>
              Manager — manager@fixflow.local / Manager123!
            </span>
          </div>

        </div>
      </div>

      {/* Illustration Column */}
      <div className="ff-login-art-col">
        <MaintenanceIllustration />
      </div>

    </div>
  );
};
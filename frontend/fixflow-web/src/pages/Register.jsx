import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Mail, Lock, User, Phone, ShieldCheck, Wrench } from 'lucide-react';
import { useAuth } from '../hooks';
import { Button, Input } from '../components/SharedUI';
import { BrandMark } from '../components/BrandMark';
import { MaintenanceIllustration } from '../components/MaintenanceIllustration';
import { getHomePath } from '../utils/roleRoutes';

const initialForm = {
  firstName: '',
  lastName: '',
  email: '',
  phoneNumber: '',
  password: '',
  confirmPassword: '',
  roleName: 'Requester', // Requester = customer/requester account
  specialization: 'General' // For Technician skill
};

const validate = (form) => {
  const errors = {};

  if (!form.firstName.trim()) errors.firstName = 'First name is required';
  if (!form.lastName.trim()) errors.lastName = 'Last name is required';

  if (!form.email.trim()) errors.email = 'Email is required';
  else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email)) errors.email = 'Enter a valid email address';

  const cleanedPhone = form.phoneNumber.replace(/\s|-/g, '');
  if (!cleanedPhone) errors.phoneNumber = 'Phone number is required';
  else if (!/^\+?[0-9]{9,15}$/.test(cleanedPhone)) errors.phoneNumber = 'Enter a valid phone number';

  if (!form.password) errors.password = 'Password is required';
  else if (form.password.length < 8) errors.password = 'Use at least 8 characters';
  else if (!/[A-Za-z]/.test(form.password) || !/[0-9]/.test(form.password)) {
    errors.password = 'Include at least one letter and one number';
  }

  if (form.confirmPassword !== form.password) errors.confirmPassword = 'Passwords do not match';

  // If you are a technician, skill is important.
  if (form.roleName === 'Technician' && !form.specialization.trim()) {
    errors.specialization = 'Specialization / Skill is required';
  }

  return errors;
};

export const Register = () => {
  const [form, setForm] = useState(initialForm);
  const [errors, setErrors] = useState({});
  const [formError, setFormError] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const { register, login } = useAuth();
  const navigate = useNavigate();

  const handleChange = (field) => (e) => {
    setForm((prev) => ({ ...prev, [field]: e.target.value }));
    setErrors((prev) => ({ ...prev, [field]: undefined }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setFormError('');

    const validationErrors = validate(form);
    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      return;
    }

    setSubmitting(true);
    try {
      await register({
        firstName: form.firstName.trim(),
        lastName: form.lastName.trim(),
        email: form.email.trim(),
        phoneNumber: form.phoneNumber.trim(),
        password: form.password,
        roleName: form.roleName,
        specialization: form.roleName === 'Technician' ? form.specialization : undefined
      });

      // Auto sign-in right after registering, then land on the right page for the role.
      const loginResult = await login(form.email.trim(), form.password);
      navigate(getHomePath(loginResult.user?.role), { replace: true });
    } catch (err) {
      setFormError(err.message || 'Could not create your account');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="ff-login-shell">
      <div className="ff-login-form-col">
        <div className="ff-login-card glass-strong" style={{ maxWidth: '460px' }}>
          <div className="ff-login-header">
            <BrandMark />
            <div className="ff-login-badge" style={{ marginTop: '1.4rem' }}>
              <ShieldCheck size={14} />
              Create your account
            </div>
            <h1 className="ff-login-title">Join FixFlow</h1>
            <p className="ff-login-subtitle">Register as a customer to raise requests, or as a technician to get assigned work.</p>
          </div>

          {formError && <div className="ff-alert">{formError}</div>}

          <form onSubmit={handleSubmit}>
            <div className="ff-field">
              <label className="ff-label">I'm registering as</label>
              <select
                className="ff-input"
                value={form.roleName}
                onChange={handleChange('roleName')}
              >
                <option value="Requester">Customer — I want to submit maintenance requests</option>
                <option value="Technician">Technician — I carry out work orders</option>
              </select>
            </div>

            {/* Skill/Specialization dropdown that appears only for a technician. */}
            {form.roleName === 'Technician' && (
              <div className="ff-field">
                <label className="ff-label">Primary Skill / Specialization</label>
                <div style={{ position: 'relative' }}>
                  <select
                    className="ff-input"
                    value={form.specialization}
                    onChange={handleChange('specialization')}
                  >
                    <option value="Common">Common Maintenance</option>
                    <option value="Electrical">Electrical</option>
                    <option value="Elevator/Lift">Elevator/Lift</option>
                    <option value="HVAC">HVAC</option>
                    <option value="Plumbing">Plumbing</option>
                    <option value="Structural">Structural</option>
                    <option value="Water Supply">Water Supply</option>
                  </select>
                </div>
                {errors.specialization && <span style={{ color: 'var(--error, red)', fontSize: '0.8rem' }}>{errors.specialization}</span>}
              </div>
            )}

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem' }}>
              <Input
                label="First name"
                value={form.firstName}
                onChange={handleChange('firstName')}
                icon={<User size={16} />}
                error={errors.firstName}
                placeholder="Nadeesha"
              />
              <Input
                label="Last name"
                value={form.lastName}
                onChange={handleChange('lastName')}
                icon={<User size={16} />}
                error={errors.lastName}
                placeholder="Perera"
              />
            </div>

            <Input
              label="Email address"
              type="email"
              value={form.email}
              onChange={handleChange('email')}
              icon={<Mail size={16} />}
              error={errors.email}
              placeholder="you@example.com"
            />

            <Input
              label="Phone number"
              value={form.phoneNumber}
              onChange={handleChange('phoneNumber')}
              icon={<Phone size={16} />}
              error={errors.phoneNumber}
              placeholder="+94 77 123 4567"
            />

            <Input
              label="Password"
              type="password"
              value={form.password}
              onChange={handleChange('password')}
              icon={<Lock size={16} />}
              error={errors.password}
              placeholder="At least 8 characters"
            />

            <Input
              label="Confirm password"
              type="password"
              value={form.confirmPassword}
              onChange={handleChange('confirmPassword')}
              icon={<Lock size={16} />}
              error={errors.confirmPassword}
              placeholder="Re-enter your password"
            />

            <Button type="submit" variant="primary" size="lg" style={{ width: '100%', marginTop: '0.4rem' }} disabled={submitting}>
              {submitting ? 'Creating account…' : 'Create account'}
            </Button>
          </form>

          <p style={{ marginTop: '1.4rem', fontSize: '0.85rem', color: 'var(--text-secondary)', textAlign: 'center' }}>
            Already have an account? <Link to="/login" style={{ fontWeight: 600 }}>Sign in</Link>
          </p>
        </div>
      </div>

      <div className="ff-login-art-col">
        <MaintenanceIllustration />
      </div>
    </div>
  );
};
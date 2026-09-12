import React, { createContext, useState, useEffect } from 'react';
import { api } from '../services/api';

export const AuthContext = createContext(null);

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [token, setToken] = useState(
    localStorage.getItem('fixflow_token')
  );
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (token) {
      api
        .get('/auth/me')
        .then((res) => {
          if (res?.success) {
            setUser(res.data);
          }
        })
        .catch(() => {
          logout();
        })
        .finally(() => {
          setLoading(false);
        });
    } else {
      setLoading(false);
    }
  }, [token]);

  // Login
  const login = async (email, password) => {
    const res = await api.post('/auth/login', {
      email,
      password
    });

    if (res?.success && res.data) {
      localStorage.setItem(
        'fixflow_token',
        res.data.token
      );

      setToken(res.data.token);
      setUser(res.data.user);

      return res.data;
    }

    throw new Error(
      res?.message || 'Login failed'
    );
  };

  // Register
  const register = async (payload) => {
    const res = await api.post(
      '/auth/register',
      payload
    );

    if (res?.success) {
      return res.data;
    }

    throw new Error(
      res?.message || 'Registration failed'
    );
  };

  // Logout
  const logout = () => {
    localStorage.removeItem('fixflow_token');
    setToken(null);
    setUser(null);
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        loading,
        login,
        logout,
        register,
        isAuthenticated: !!user
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};
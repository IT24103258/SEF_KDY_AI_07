import { useContext } from 'react';
import { AuthContext } from '../context/AuthContext';
import { ThemeContext } from '../context/ThemeContext';

export const useAuth = () => useContext(AuthContext);
export const useTheme = () => useContext(ThemeContext);

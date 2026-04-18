import { createContext, useContext, useState, useCallback } from 'react';
import type { ReactNode } from 'react';
import type { AuthUser, LoginRequest } from '../types';
import { login as apiLogin } from '../api/auth';

interface AuthContextValue {
  user: AuthUser | null;
  login: (req: LoginRequest) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

function loadUser(): AuthUser | null {
  const raw = localStorage.getItem('auth');
  return raw ? JSON.parse(raw) : null;
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(loadUser);

  const login = useCallback(async (req: LoginRequest) => {
    const res = await apiLogin(req);
    const auth: AuthUser = { ...res, tenantCode: req.tenantCode };
    localStorage.setItem('auth', JSON.stringify(auth));
    setUser(auth);
  }, []);

  const logout = useCallback(() => {
    localStorage.removeItem('auth');
    setUser(null);
  }, []);

  return (
    <AuthContext.Provider value={{ user, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider');
  return ctx;
}

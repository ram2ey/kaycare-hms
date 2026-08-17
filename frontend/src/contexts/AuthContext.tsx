import { createContext, useContext, useState, useCallback, useEffect } from 'react';
import type { ReactNode } from 'react';
import type { AuthUser, LoginRequest } from '../types';
import { login as apiLogin, getMe, logout as apiLogout } from '../api/auth';

interface AuthContextValue {
  user: AuthUser | null;
  login: (req: LoginRequest) => Promise<AuthUser>;
  logout: () => Promise<void>;
  refreshUser: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

function pickAuthUser(u: {
  email: string;
  fullName: string;
  role: string;
  tenantCode: string;
  mustChangePassword: boolean;
}): AuthUser {
  return {
    email: u.email,
    fullName: u.fullName,
    role: u.role,
    tenantCode: u.tenantCode,
    mustChangePassword: u.mustChangePassword,
  };
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [loading, setLoading] = useState(true);

  const bootstrap = useCallback(async () => {
    try {
      setUser(pickAuthUser(await getMe()));
    } catch {
      setUser(null);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    bootstrap();
  }, [bootstrap]);

  const login = useCallback(async (req: LoginRequest): Promise<AuthUser> => {
    const auth = pickAuthUser(await apiLogin(req));
    setUser(auth);
    return auth;
  }, []);

  const logout = useCallback(async () => {
    try {
      await apiLogout();
    } finally {
      setUser(null);
    }
  }, []);

  const refreshUser = useCallback(() => bootstrap(), [bootstrap]);

  // The httpOnly auth cookie can't be read synchronously by JS, so "am I logged in" now requires
  // a round trip to /auth/me. Gating rendering here (rather than threading a loading flag through
  // ProtectedRoute/LoginPage) means neither of those needs to change — they don't mount until
  // this resolves, so their existing synchronous `if (!user)` checks keep working as-is.
  if (loading) return null;

  return (
    <AuthContext.Provider value={{ user, login, logout, refreshUser }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider');
  return ctx;
}

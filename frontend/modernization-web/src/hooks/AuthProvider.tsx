import { useCallback, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { AuthContext } from './useAuth';
import { authStorage } from '../services/authStorage';

type AuthProviderProps = {
  children: ReactNode;
};

export function AuthProvider({ children }: AuthProviderProps) {
  const [token, setToken] = useState<string | null>(() => authStorage.getToken());

  const login = useCallback((newToken: string) => {
    authStorage.setToken(newToken);
    setToken(newToken);
  }, []);

  const logout = useCallback(() => {
    authStorage.clearToken();
    setToken(null);
  }, []);

  const value = useMemo(
    () => ({
      token,
      isAuthenticated: Boolean(token),
      login,
      logout,
    }),
    [token, login, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

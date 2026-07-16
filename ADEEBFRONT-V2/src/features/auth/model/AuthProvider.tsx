import type { QueryClient } from "@tanstack/react-query";
import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { authApi } from "@/features/auth/api/auth.api";
import type {
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  UserResponse,
} from "@/features/auth/model/auth.types";
import {
  AuthContext,
  type AuthContextValue,
} from "@/features/auth/model/auth-context";
import {
  configureRefreshManager,
  refreshOnce,
} from "@/shared/auth/refresh-manager";
import { tokenStore } from "@/shared/auth/token-store";

type AuthProviderProps = {
  queryClient: QueryClient;
  children: ReactNode;
};

function applyAuthResponse(
  response: AuthResponse,
  setUser: (user: UserResponse | null) => void,
) {
  tokenStore.setAccessToken(response.tokens.accessToken);
  tokenStore.setRefreshToken(response.tokens.refreshToken);
  setUser(response.user);
}

export function AuthProvider({ queryClient, children }: AuthProviderProps) {
  const [user, setUser] = useState<UserResponse | null>(null);
  const [isBootstrapping, setIsBootstrapping] = useState(true);

  const clearAuth = useCallback(() => {
    tokenStore.clear();
    setUser(null);
    queryClient.clear();
  }, [queryClient]);

  useEffect(() => {
    configureRefreshManager({
      refresh: async (refreshToken) => {
        const response = await authApi.refresh(refreshToken);
        return {
          accessToken: response.tokens.accessToken,
          refreshToken: response.tokens.refreshToken,
        };
      },
      onRefreshFailed: clearAuth,
    });
  }, [clearAuth]);

  useEffect(() => {
    let active = true;

    async function bootstrap() {
      try {
        const refreshToken = tokenStore.getRefreshToken();
        if (!refreshToken) return;

        await refreshOnce();
        if (!active) return;

        const currentUser = await authApi.me();
        if (active) setUser(currentUser);
      } catch {
        if (active) clearAuth();
      } finally {
        if (active) setIsBootstrapping(false);
      }
    }

    void bootstrap();

    return () => {
      active = false;
    };
  }, [clearAuth]);

  const login = useCallback(async (request: LoginRequest) => {
    const response = await authApi.login(request);
    applyAuthResponse(response, setUser);
    return response;
  }, []);

  const register = useCallback(async (request: RegisterRequest) => {
    const response = await authApi.register(request);
    applyAuthResponse(response, setUser);
    return response;
  }, []);

  const logout = useCallback(async () => {
    try {
      await authApi.logout();
    } finally {
      clearAuth();
    }
  }, [clearAuth]);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: user !== null,
      isBootstrapping,
      login,
      register,
      logout,
    }),
    [isBootstrapping, login, logout, register, user],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

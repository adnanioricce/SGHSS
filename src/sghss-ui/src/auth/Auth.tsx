import React from "react";
import { apiClient } from "../ApiClient";
import type { User } from "../models";

export function useApi() {
    const [loading, setLoading] = React.useState(false);
    const [error, setError] = React.useState(null);
  
    const call = React.useCallback(async (apiCall:any) => {
      setLoading(true);
      setError(null);
      
      try {
        const result = await apiCall();
        return result;
      } catch (err: Error | any) {
        setError(err.message);
        throw err;
      } finally {
        setLoading(false);
      }
    }, []);
  
    return { call, loading, error };
  }
const login = async (email: string, password: string) => {
    return apiClient.login(email, password);
}
const logout = async () => {
    return apiClient.logout();
}
const sampleUser: User | null = {
    id: 1,
    nome: 'John Doe',
    email: '',
    role: 'Administrador'
};
const initialAuthState = {
    user: sampleUser as User | null,
    isAuthenticated: false,
    loading: false,
    login: login,
    logout: logout
};    
const AuthContext = React.createContext<{
    user: User | null;
    isAuthenticated: boolean;
    loading: boolean;
    login: (email: string, password: string) => Promise<any>;
    logout: () => Promise<void>;
}>(initialAuthState);
  
export function AuthProvider(args:any) {
    const { children } = args;
    const [user, setUser] = React.useState(sampleUser);
    const [isAuthenticated, setIsAuthenticated] = React.useState(false);
    const [loading, setLoading] = React.useState(true);
    
    React.useEffect(() => {
        const checkAuth = async () => {
            if (apiClient.token) {
                try {
                    const profile = await apiClient.getProfile();
                    setUser(profile);
                    setIsAuthenticated(true);
                } catch (error) {
                    console.error('Auth check failed:', error);
                    apiClient.clearTokens();
                }
            }
            setLoading(false);
        };

        checkAuth();
    }, []);

    const login = async (email: string, password: string) => {
        const response = await apiClient.login(email, password);
        setUser(response);
        setIsAuthenticated(true);
        return response;
    };

    const logout = async () => {
        await apiClient.logout();
        setUser(null);
        setIsAuthenticated(false);
    };

    const value = {
        user,
        isAuthenticated,
        loading,
        login,
        logout,
    };

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
    const context = React.useContext(AuthContext);
    if (!context) {
        throw new Error('useAuth must be used within AuthProvider');
    }
    return context;
}
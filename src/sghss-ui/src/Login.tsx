import React from "react";
import { useApi, useAuth } from "./auth/Auth";

export function LoginPage(args: any) {
    const { onLogin } = args;
    const [credentials, setCredentials] = React.useState({ email: '', password: '' });
    const { login } = useAuth();
    const { call, loading, error } = useApi();
  
    const handleLogin = async () => {
      if (!credentials.email || !credentials.password) {
        return;
      }
  
      try {
        await call(() => login(credentials.email, credentials.password));
        onLogin?.();
      } catch (err) {
        console.error('Login failed:', err);
      }
    };
  
    return (
      <div className="max-w-md mx-auto bg-white rounded-lg shadow-md p-6">
        <h2 className="text-2xl font-bold mb-6 text-center text-gray-800">Login</h2>
        
        {error && (
          <div className="mb-4 p-3 bg-red-100 border border-red-400 text-red-700 rounded">
            {error}
          </div>
        )}
        
        <div className="space-y-4">
          <div>
            <label className="block text-gray-700 text-sm font-bold mb-2">
              Email
            </label>
            <input
              type="email"
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:border-blue-500"
              value={credentials.email}
              onChange={(e) => setCredentials({...credentials, email: e.target.value})}
              disabled={loading}
              required
            />
          </div>
          <div>
            <label className="block text-gray-700 text-sm font-bold mb-2">
              Senha
            </label>
            <input
              type="password"
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:border-blue-500"
              value={credentials.password}
              onChange={(e) => setCredentials({...credentials, password: e.target.value})}
              disabled={loading}
              required
            />
          </div>
          <button
            onClick={handleLogin}
            disabled={loading}
            className="w-full bg-blue-600 text-white py-2 px-4 rounded-md hover:bg-blue-700 transition duration-200 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {loading ? 'Entrando...' : 'Entrar'}
          </button>
        </div>
      </div>
    );
  }
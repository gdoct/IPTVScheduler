import { config } from '../config';
const AUTH_TOKEN_KEY = 'auth_token';

interface LoginResponse {
  token: string;
}

interface LoginRequest {
  username: string;
  password: string;
}
const AUTH_BASE_URI = config.apiBaseUrl;

export const AuthService = {
  login: async (username: string, password: string): Promise<boolean> => {
    try {
      const response = await fetch(`${AUTH_BASE_URI}/login`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ username, password } as LoginRequest),
      });

      if (!response.ok) {
        return false;
      }

      const data: LoginResponse = await response.json();
      // Store token synchronously to ensure it's available immediately
      localStorage.setItem(AUTH_TOKEN_KEY, data.token);
      return true;
    } catch (error) {
      console.error('Login error:', error);
      return false;
    }
  },

  logout: (): void => {
    localStorage.removeItem(AUTH_TOKEN_KEY);
  },

  isAuthenticated: (): boolean => {
    return !!localStorage.getItem(AUTH_TOKEN_KEY);
  },

  getToken: (): string | null => {
    return localStorage.getItem(AUTH_TOKEN_KEY);
  },

  setToken: (token: string): void => {
    localStorage.setItem(AUTH_TOKEN_KEY, token);
  },
};

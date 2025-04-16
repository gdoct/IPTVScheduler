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
  
  validateToken: async (): Promise<boolean> => {
    const token = localStorage.getItem(AUTH_TOKEN_KEY);
    
    // If no token exists, token is invalid
    if (!token) {
      return false;
    }
    
    try {
      // JWT tokens are structured as header.payload.signature
      const parts = token.split('.');
      if (parts.length !== 3) {
        // Not a valid JWT token format
        AuthService.logout();
        window.location.href = '/login';
        return false;
      }
      
      // Parse the payload to check expiration
      const payload = JSON.parse(atob(parts[1]));
      
      // Check if token has an expiration claim
      if (payload.exp) {
        // exp is in seconds since epoch, current time is in milliseconds
        const currentTime = Math.floor(Date.now() / 1000);
        
        if (payload.exp < currentTime) {
          console.log('Token expired, redirecting to login');
          AuthService.logout();
          window.location.href = '/login';
          return false;
        }
      }
      
      // Optional: Make a lightweight API call to verify token validity on server
      // This is useful for cases where the token might be revoked on the server
      // but still valid by expiration date
      try {
        const response = await fetch(`${AUTH_BASE_URI}/validate-token`, {
          headers: {
            'Authorization': `Bearer ${token}`
          }
        });
        
        if (!response.ok) {
          console.log('Token validation failed on server, redirecting to login');
          AuthService.logout();
          window.location.href = '/login';
          return false;
        }
      } catch (error) {
        // If server validation fails but token looks valid locally,
        // we can still consider it valid (optional behavior)
        console.warn('Token server validation error:', error);
      }
      
      return true;
    } catch (error) {
      console.error('Token validation error:', error);
      AuthService.logout();
      window.location.href = '/login';
      return false;
    }
  }
};

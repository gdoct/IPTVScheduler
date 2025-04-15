/**
 * Application configuration with environment-specific settings
 */

// Determine the base API URL based on the environment
let apiBaseUrl = '';

// If in development (localhost), use the full URL
if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
  apiBaseUrl = 'http://localhost:5000/api';
} else {
  // In production, use relative URLs which will use the same domain
  apiBaseUrl = '/api';
}

export const config = {
  apiBaseUrl,
};
import { config } from '../config';
import { ChannelInfo } from '../types/recordings';

// Use the base URL from the config
const API_BASE_URL = config.apiBaseUrl;

// Cache system for API responses
type CacheEntry = {
  timestamp: number;
  data: any;
};

const apiCache = new Map<string, CacheEntry>();
const CACHE_TTL = 5000; // 5 seconds cache time-to-live

// Get cached data or perform fetch
const cachedFetch = async <T>(key: string, fetcher: () => Promise<T>): Promise<T> => {
  const now = Date.now();
  const cached = apiCache.get(key);
  
  // Return cached data if it exists and is still valid
  if (cached && now - cached.timestamp < CACHE_TTL) {
    console.log(`Using cached data for: ${key}`);
    return cached.data as T;
  }

  // Otherwise fetch fresh data
  console.log(`Fetching fresh data for: ${key}`);
  const data = await fetcher();
  
  // Store in cache
  apiCache.set(key, {
    timestamp: now,
    data
  });
  
  return data;
};

export const searchChannels = async (query: string): Promise<ChannelInfo[]> => {
  // Always ensure we have a valid query string
  if (!query || query.trim() === '') return [];
  
  const cacheKey = `search:${query}`;
  
  return cachedFetch<ChannelInfo[]>(cacheKey, async () => {
    try {
      console.log(`Searching channels with query: "${query}" at ${API_BASE_URL}/channels/search`);
      
      const response = await fetch(`${API_BASE_URL}/channels/search?query=${encodeURIComponent(query)}`, {
        method: 'GET',
        headers: {
          'Accept': 'application/json',
          'Content-Type': 'application/json',
        },
      });
      
      if (!response.ok) {
        console.error(`Error searching channels: ${response.status} ${response.statusText}`);
        // Try to get more error details if available
        try {
          const errorText = await response.text();
          console.error(`Error response body: ${errorText}`);
        } catch (textError) {
          console.error('Could not read error response body');
        }
        throw new Error(`Error ${response.status}: ${response.statusText}`);
      }
      
      const data = await response.json();
      console.log(`Found ${data.length} channels matching query: "${query}"`);
      return data;
    } catch (error) {
      console.error('Error searching channels:', error);
      return [];
    }
  });
};

// Cache for channel logos to avoid repeated API calls
const channelLogoCache = new Map<string, string>();

export const getChannelLogo = async (channelUri: string, channelName: string): Promise<string> => {
  // Check cache first
  if (channelLogoCache.has(channelUri)) {
    return channelLogoCache.get(channelUri) || '';
  }
  
  const cacheKey = `logo:${channelUri}`;
  
  return cachedFetch<string>(cacheKey, async () => {
    try {
      // Verify we have a valid channel name
      if (!channelName || channelName.trim() === '') {
        console.log('Channel name is empty, cannot search for logo');
        return '';
      }
      
      // Search with exact channel name to find the logo immediately
      console.log(`Searching for channel logo with exact name: "${channelName}"`);
      
      // Use the full channel name for the search
      const results = await searchChannels(channelName.trim());
      
      // First try exact match on URI
      let match = results.find(c => c.uri === channelUri);
      
      // If no exact URI match, try exact name match
      if (!match) {
        match = results.find(c => c.name === channelName);
      }
      
      // If still no match, use the first result if available
      if (!match && results.length > 0) {
        match = results[0];
        console.log(`No exact match found, using first result: ${match.name}`);
      }
      
      if (match && match.logo) {
        // Store in cache for future use
        channelLogoCache.set(channelUri, match.logo);
        console.log(`Found logo for channel ${channelName}: ${match.logo}`);
        return match.logo;
      }
      
      console.log(`No logo found for channel: ${channelName}`);
      return ''; // No logo found
    } catch (error) {
      console.error('Error fetching channel logo:', error);
      return '';
    }
  });
};
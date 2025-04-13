import { config } from '../config';
import { HomeRecordingsViewModel, ScheduledRecording, SchedulerSettings, TaskDefinitionModel } from '../types/recordings';

// Base API URL from config
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

// Helper to invalidate cache entries that match a prefix
const invalidateCacheByPrefix = (prefix: string): void => {
  const keysToDelete: string[] = [];
  apiCache.forEach((_, key) => {
    if (key.startsWith(prefix)) {
      keysToDelete.push(key);
    }
  });
  
  keysToDelete.forEach(key => apiCache.delete(key));
};

// Helper for common fetch options
const getCommonOptions = () => ({
  headers: {
    'Content-Type': 'application/json',
    // Add any auth headers if needed
  }
});

// Get CSRF token from the page if available
const getCsrfToken = (): string | null => {
  const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
  return tokenElement ? (tokenElement as HTMLInputElement).value : null;
};

// Add CSRF token to request headers if available
const withCsrf = (options: RequestInit): RequestInit => {
  const token = getCsrfToken();
  if (token) {
    return {
      ...options,
      headers: {
        ...options.headers,
        'RequestVerificationToken': token
      }
    };
  }
  return options;
};

// API functions
export const recordingsApi = {
  // Get all recordings
  getAllRecordings: async (): Promise<ScheduledRecording[]> => {
    const cacheKey = 'recordings:all';
    
    return cachedFetch<ScheduledRecording[]>(cacheKey, async () => {
      const response = await fetch(`${API_BASE_URL}`, getCommonOptions());
      if (!response.ok) throw new Error('Failed to fetch recordings');
      return await response.json();
    });
  },

  // Get a single recording
  getRecording: async (id: string): Promise<ScheduledRecording> => {
    const cacheKey = `recordings:${id}`;
    
    return cachedFetch<ScheduledRecording>(cacheKey, async () => {
      const response = await fetch(`${API_BASE_URL}/${id}`, getCommonOptions());
      if (!response.ok) throw new Error('Failed to fetch recording');
      return await response.json();
    });
  },

  // Create a new recording
  createRecording: async (recording: ScheduledRecording): Promise<ScheduledRecording> => {
    const options = withCsrf({
      ...getCommonOptions(),
      method: 'POST',
      body: JSON.stringify(recording)
    });
    
    const response = await fetch(`${API_BASE_URL}`, options);
    if (!response.ok) throw new Error('Failed to create recording');
    
    // Invalidate relevant cache entries
    invalidateCacheByPrefix('recordings:');
    
    return await response.json();
  },

  // Update an existing recording
  updateRecording: async (id: string, recording: ScheduledRecording): Promise<void> => {
    const options = withCsrf({
      ...getCommonOptions(),
      method: 'PUT',
      body: JSON.stringify(recording)
    });
    
    const response = await fetch(`${API_BASE_URL}/${id}`, options);
    if (!response.ok) throw new Error('Failed to update recording');
    
    // Invalidate relevant cache entries
    invalidateCacheByPrefix('recordings:');
  },

  // Delete a recording
  deleteRecording: async (id: string): Promise<void> => {
    const options = withCsrf({
      ...getCommonOptions(),
      method: 'DELETE'
    });
    
    const response = await fetch(`${API_BASE_URL}/${id}`, options);
    if (!response.ok) throw new Error('Failed to delete recording');
    
    // Invalidate relevant cache entries
    invalidateCacheByPrefix('recordings:');
  },

  // Get task definition
  getTaskDefinition: async (id: string): Promise<TaskDefinitionModel> => {
    const cacheKey = `task:${id}`;
    
    return cachedFetch<TaskDefinitionModel>(cacheKey, async () => {
      const response = await fetch(`${API_BASE_URL}/task/${id}`, getCommonOptions());
      if (!response.ok) throw new Error('Failed to fetch task definition');
      return await response.json();
    });
  },

  // Update task definition
  updateTaskDefinition: async (id: string, taskFile: string): Promise<void> => {
    const options = withCsrf({
      ...getCommonOptions(),
      method: 'PUT',
      body: JSON.stringify({ id, taskfile: taskFile })
    });
    
    const response = await fetch(`${API_BASE_URL}/task/${id}`, options);
    if (!response.ok) throw new Error('Failed to update task definition');
    
    // Invalidate relevant cache entries
    invalidateCacheByPrefix(`task:${id}`);
  },

  // Get channel count
  getChannelCount: async (): Promise<number> => {
    const cacheKey = 'channels:count';
    
    return cachedFetch<number>(cacheKey, async () => {
      try {
        const response = await fetch(`${API_BASE_URL}/channelcount`, {
          ...getCommonOptions(),
          headers: {
            ...getCommonOptions().headers,
            'Accept': 'application/json'}
        });
        
        if (!response.ok) return 0;
        // the backend returns a number wrapped in json
        const { count } = await response.json();
        return count;
      } catch (error) {
        console.error('Error fetching channel count:', error);
        return 0;
      }
    });
  },

  // Get settings
  getSettings: async (): Promise<SchedulerSettings> => {
    const cacheKey = 'settings';
    
    return cachedFetch<SchedulerSettings>(cacheKey, async () => {
      const response = await fetch(`${API_BASE_URL}/settings`, getCommonOptions());
      if (!response.ok) throw new Error('Failed to fetch settings');
      return await response.json();
    });
  },

  // Update settings
  updateSettings: async (settings: SchedulerSettings): Promise<void> => {
    const options = withCsrf({
      ...getCommonOptions(),
      method: 'PUT',
      body: JSON.stringify(settings)
    });
    
    const response = await fetch(`${API_BASE_URL}/settings`, options);
    if (!response.ok) throw new Error('Failed to update settings');
    
    // Invalidate settings cache
    invalidateCacheByPrefix('settings');
  },

  // Upload M3U playlist
  uploadM3uPlaylist: async (file: File): Promise<{ message: string }> => {
    const formData = new FormData();
    formData.append('file', file);
    
    const options = withCsrf({
      method: 'POST',
      body: formData,
      // Don't set Content-Type header when using FormData
      headers: {
        'RequestVerificationToken': getCsrfToken() || ''
      }
    });
    
    const response = await fetch(`${API_BASE_URL}/upload-m3u`, options);
    if (!response.ok) throw new Error('Failed to upload M3U file');
    
    // Invalidate channels cache
    invalidateCacheByPrefix('channels:');
    
    return await response.json();
  },

  // Get home recordings view model (combines channels and recordings)
  getHomeRecordingsViewModel: async (): Promise<HomeRecordingsViewModel> => {
    const cacheKey = 'home:recordings';
    
    return cachedFetch<HomeRecordingsViewModel>(cacheKey, async () => {
      // In a real implementation, this might be a single API call
      // Here we combine multiple calls for demonstration
      const [recordings, channelsCount, settings] = await Promise.all([
        recordingsApi.getAllRecordings(),
        recordingsApi.getChannelCount(),
        recordingsApi.getSettings()
      ]);
      
      return {
        recordings,
        channelsCount,
        recordingPath: settings.mediaPath || '/recordings'
      };
    });
  }
};
// Recording related types

export interface ScheduledRecording {
  id: string;
  name: string;
  description: string;
  channelUri: string;
  channelName: string;
  startTime: string;
  endTime: string;
  filename: string;
}

export interface ChannelInfo {
  name: string;
  uri: string;
  logo: string;
  group?: string;
}

export interface TaskDefinitionModel {
  id: string;
  name: string;
  content: string;
}

export interface HomeRecordingsViewModel {
  recordings: ScheduledRecording[];
  channels: ChannelInfo[];
  recordingPath: string;
}

export interface TaskEditModel {
  id: string;
  taskfile: string;
}

// Helper function to obfuscate channel URI
export const obfuscateChannelUri = (uri: string): string => {
  // Transform "http://secret.host.tv/username/password/219885" to "http://secr.../219885"
  const parts = uri.split('/');
  const first4lettersOfHostname = parts[2].substring(0, 4);
  const lastPart = parts[parts.length - 1];
  return `${parts[0]}://${first4lettersOfHostname}.../${lastPart}`;
};

// Helper function to format dates
export const formatNiceDate = (date: Date): string => {
  const options: Intl.DateTimeFormatOptions = { 
    year: 'numeric', 
    month: 'short', 
    day: '2-digit', 
    hour: '2-digit', 
    minute: '2-digit' 
  };
  return date.toLocaleString('en-US', options).replace(',', '');
};

export const formatFileDate = (date: Date): string => {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  const hours = String(date.getHours()).padStart(2, '0');
  const minutes = String(date.getMinutes()).padStart(2, '0');
  return `${year}${month}${day}_${hours}${minutes}`;
};
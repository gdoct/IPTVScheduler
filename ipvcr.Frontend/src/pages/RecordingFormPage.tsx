import React, { useEffect, useRef, useState } from 'react';
import { Alert, Button, Card, Col, Container, Form, InputGroup, ListGroup, Row } from 'react-bootstrap';
import { useLocation, useNavigate, useParams } from 'react-router-dom';
import FolderBrowserModal from '../components/FolderBrowserModal';
import { searchChannels } from '../services/api';
import { recordingsApi } from '../services/RecordingsApi';
import { ChannelInfo, formatFileDate, formatNiceDate, ScheduledRecording } from '../types/recordings';

const RecordingFormPage: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const location = useLocation();
  const baseRecordingPath = location.state?.recordingPath || '';
  
  const isEdit = Boolean(id && id !== '00000000-0000-0000-0000-000000000000');

  // State management
  const [loading, setLoading] = useState<boolean>(isEdit);
  const [error, setError] = useState<string | null>(null);
  const [formData, setFormData] = useState<Partial<ScheduledRecording>>({
    id: id || '00000000-0000-0000-0000-000000000000',
    name: '',
    description: '',
    channelUri: '',
    channelName: '',
    startTime: getTomorrowDatetime(),
    endTime: getTomorrowDatetimePlusHour(),
    filename: ''
  });
  
  // Folder browser state
  const [showFolderBrowser, setShowFolderBrowser] = useState(false);
  const [selectedFolder, setSelectedFolder] = useState('');
  
  const [channelLogo, setChannelLogo] = useState<string>('');
  const [channelQuery, setChannelQuery] = useState<string>('');
  const [searchResults, setSearchResults] = useState<ChannelInfo[]>([]);
  const [isSearching, setIsSearching] = useState<boolean>(false);
  const [showDropdown, setShowDropdown] = useState<boolean>(false);
  const searchTimeoutRef = useRef<NodeJS.Timeout | null>(null);

  // Load recording data if in edit mode
  useEffect(() => {
    if (isEdit && id) {
      const fetchRecording = async () => {
        try {
          setLoading(true);
          const recording = await recordingsApi.getRecording(id);
          if (recording) {
            setFormData({
              ...recording,
              startTime: formatDateForDatePicker(recording.startTime),
              endTime: formatDateForDatePicker(recording.endTime),
            });
            setChannelQuery(recording.channelName);
            
            // Extract folder from filename
            const folderPath = extractFolderPath(recording.filename);
            setSelectedFolder(folderPath);
            
            // Try to fetch the channel logo
            fetchChannelInfoForName(recording.channelName, recording.channelUri);
          }
        } catch (err) {
          console.error('Error fetching recording:', err);
          setError('Failed to load recording details');
        } finally {
          setLoading(false);
        }
      };
      
      fetchRecording();
    } else {
      // For new recordings, initialize with the base path
      setSelectedFolder(baseRecordingPath);
    }
  }, [id, isEdit, baseRecordingPath]);

  // Helper to extract folder path from filename
  const extractFolderPath = (filename: string): string => {
    if (!filename) return baseRecordingPath;
    
    const lastSlashIndex = filename.lastIndexOf('/');
    if (lastSlashIndex === -1) return baseRecordingPath;
    
    return filename.substring(0, lastSlashIndex);
  };

  // Utility function to fetch channel info by name
  const fetchChannelInfoForName = async (channelName: string, channelUri: string) => {
    if (channelName) {
      setIsSearching(true);
      try {
        // Search with exact channel name
        const results = await searchChannels(channelName);
        
        // Find exact match by URI
        const matchedChannel = results.find(c => c.uri === channelUri);
        if (matchedChannel) {
          setChannelLogo(matchedChannel.logo);
        } else if (results.length > 0) {
          // If no exact match, try to use the first result
          setChannelLogo(results[0].logo);
        }
      } catch (error) {
        console.error('Error fetching channel info:', error);
      } finally {
        setIsSearching(false);
      }
    }
  };

  // Utility function to format date for the date picker
  const formatDateForDatePicker = (dateString: string) => {
    const date = new Date(dateString);
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${year}-${month}-${day}T${hours}:${minutes}`;
  };

  // Helper to get tomorrow's date
  function getTomorrowDatetime(): string {
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    return tomorrow.toISOString().slice(0, 16);
  }

  // Helper to get tomorrow plus one hour
  function getTomorrowDatetimePlusHour(): string {
    const tomorrowPlusHour = new Date();
    tomorrowPlusHour.setDate(tomorrowPlusHour.getDate() + 1);
    tomorrowPlusHour.setHours(tomorrowPlusHour.getHours() + 1);
    return tomorrowPlusHour.toISOString().slice(0, 16);
  }

  // Helper to parse channel name
  function parseChannelName(fullChannelName: string): string {
    const parts = fullChannelName.split('|');
    if (parts.length > 1) {
      return parts[1].trim();
    }
    return fullChannelName.trim();
  }

  // Handle channel search with debounce
  const handleChannelSearch = (query: string) => {
    setChannelQuery(query);
    
    // Always show dropdown if we have a query
    if (query) {
      setShowDropdown(true);
    } else {
      setShowDropdown(false);
      setSearchResults([]);
      return;
    }
    
    // Clear previous timeout
    if (searchTimeoutRef.current) {
      clearTimeout(searchTimeoutRef.current);
    }
    
    // Set a new timeout for debouncing
    searchTimeoutRef.current = setTimeout(async () => {
      if (query) {
        setIsSearching(true);
        try {
          // Use the full query for search regardless of length
          console.log(`Searching channels with query: "${query}"`);
          const results = await searchChannels(query);
          setSearchResults(results);
        } catch (error) {
          console.error('Error searching channels:', error);
          setSearchResults([]);
        } finally {
          setIsSearching(false);
        }
      } else {
        setSearchResults([]);
      }
    }, 300); // 300ms debounce
  };

  // Handle channel selection
  const handleChannelSelect = (channel: ChannelInfo) => {
    setFormData(prev => ({
      ...prev,
      channelUri: channel.uri,
      channelName: parseChannelName(channel.name)
    }));
    setChannelQuery(parseChannelName(channel.name));
    setChannelLogo(channel.logo);
    setShowDropdown(false);
    
    // Update filename and description when channel changes
    updateFilenameAndDescription(
      formData.name || '', 
      formData.startTime || '', 
      parseChannelName(channel.name)
    );
  };

  // Handler for input changes
  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    
    setFormData(prev => ({ ...prev, [name]: value }));
    
    // Update filename and description when necessary fields change
    if (['name', 'startTime'].includes(name)) {
      updateFilenameAndDescription(
        name === 'name' ? value : (formData.name || ''),
        name === 'startTime' ? value : (formData.startTime || ''),
        formData.channelName || ''
      );
    }
  };

  // Handle folder selection
  const handleFolderSelect = (folderPath: string) => {
    setSelectedFolder(folderPath);
    
    // Update filename with the new folder path
    updateFilenameAndDescription(
      formData.name || '',
      formData.startTime || '',
      formData.channelName || '',
      folderPath
    );
  };

  // Update filename and description based on form data
  const updateFilenameAndDescription = (
    name: string,
    startTimeStr: string, 
    channelName: string,
    folderPath?: string
  ) => {
    if (name && startTimeStr && channelName) {
      // Generate filename
      const startDate = new Date(startTimeStr);
      const startTimeFormatted = formatFileDate(startDate) + 
        startTimeStr.split('T')[1].replace(':', '').substring(0, 4);
      
      const sanitizedName = name.replace(/ /g, '_').toLowerCase();
      const useFolderPath = folderPath || selectedFolder || baseRecordingPath;
      const filename = `${useFolderPath}/${sanitizedName}_${startTimeFormatted}.mp4`;
      
      // Generate description
      const niceStartTime = formatNiceDate(startDate);
      const description = `${name} - recorded from ${channelName} at ${niceStartTime}`;
      
      setFormData(prev => ({
        ...prev,
        filename,
        description
      }));
    }
  };

  // Form submission handler
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    // Validate form
    if (!formData.name || !formData.startTime || !formData.endTime || !formData.channelUri) {
      setError('Please fill all required fields');
      return;
    }
    
    try {
      // Submit the form
      if (isEdit && id) {
        await recordingsApi.updateRecording(id, formData as ScheduledRecording);
      } else {
        await recordingsApi.createRecording(formData as ScheduledRecording);
      }
      // Redirect back to recordings page on success
      navigate('/');
    } catch (err) {
      console.error('Error saving recording:', err);
      setError('Failed to save recording');
    }
  };

  // Check if form is valid
  const isFormValid = 
    formData.name && 
    formData.startTime && 
    formData.endTime && 
    formData.channelUri &&
    formData.channelName;

  // Handle cancel button click
  const handleCancel = () => {
    navigate('/');
  };

  // Get the relative path for display
  const getRelativeFolderPath = () => {
    if (!selectedFolder) return '';
    
    // Show only the path after the base recording path
    if (selectedFolder.startsWith(baseRecordingPath)) {
      const relativePath = selectedFolder.substring(baseRecordingPath.length);
      if (!relativePath) return '/';
      return relativePath.startsWith('/') ? relativePath : '/' + relativePath;
    }
    
    return selectedFolder;
  };

  const getFullFolderpath = () => {
    let root = baseRecordingPath;
    if (!root.endsWith('/')) {
      root += '/';
    }
    let relative = getRelativeFolderPath();
    if(relative.startsWith('/')) {
      relative = relative.substring(1);
    }
    return root + relative;
  }

  return (
    <Container fluid className="px-4 py-3">
      <Card className="mb-4 border-0 shadow">
        <Card.Header className="bg-dark text-white py-3">
          <h4 className="mb-0">
            {isEdit ? (
              <><i className="bi bi-pencil-square me-2"></i>Edit Recording</>
            ) : (
              <><i className="bi bi-plus-circle me-2"></i>Add New Recording</>
            )}
          </h4>
        </Card.Header>
        <Card.Body>
          {loading ? (
            <div className="text-center py-5">
              <div className="spinner-border text-primary" role="status">
                <span className="visually-hidden">Loading...</span>
              </div>
              <p className="mt-2">Loading recording details...</p>
            </div>
          ) : (
            <>
              {error && (
                <Alert variant="danger" onClose={() => setError(null)} dismissible>
                  <i className="bi bi-exclamation-triangle-fill me-2"></i>
                  {error}
                </Alert>
              )}
              
              <Form onSubmit={handleSubmit} className="modern-form">
                <input type="hidden" name="id" value={formData.id || ''} />
                
                <Row className="mb-4 g-4">
                  <Col lg={3} md={4} className="text-center">
                    <div className="channel-logo-container bg-light rounded d-flex align-items-center justify-content-center mb-2 mx-auto" 
                      style={{ height: '140px', width: '140px' }}>
                      {channelLogo !== '' ? (
                        <img 
                          src={channelLogo} 
                          alt=""
                          className="img-fluid rounded" 
                          style={{ maxHeight: '120px', maxWidth: '120px', objectFit: 'contain' }} 
                        />
                      ) : (
                        <i className="bi bi-tv-fill text-muted" style={{ fontSize: '3rem' }}></i>
                      )}
                    </div>
                  </Col>
                  
                  <Col lg={9} md={8}>
                    <Form.Group className="mb-3">
                      <Form.Label className="fw-bold text-dark">Recording Name</Form.Label>
                      <Form.Control
                        type="text"
                        name="name"
                        value={formData.name || ''}
                        onChange={handleInputChange}
                        required
                        data-testid="recording-name-input"
                        className="form-control-lg"
                        placeholder="Enter a descriptive name for this recording"
                      />
                    </Form.Group>
                    
                    <Form.Group className="mb-3">
                      <Form.Label className="fw-bold text-dark">Description</Form.Label>
                      <Form.Control
                        as="textarea"
                        rows={2}
                        name="description"
                        value={formData.description || ''}
                        onChange={handleInputChange}
                        readOnly
                        className="bg-light text-muted"
                      />
                    </Form.Group>
                  </Col>
                </Row>
                
                <Row className="mb-4 g-4">
                  <Col lg={6}>
                    <Form.Group className="mb-3 position-relative">
                      <Form.Label className="fw-bold text-dark">Channel</Form.Label>
                      <InputGroup>
                        {channelLogo && (
                          <InputGroup.Text className="px-2">
                            <img 
                              src={channelLogo} 
                              alt="" 
                              style={{ height: '24px', width: '24px', objectFit: 'contain' }}
                            />
                          </InputGroup.Text>
                        )}
                        <Form.Control 
                          type="text"
                          placeholder="Search for a channel..."
                          value={channelQuery}
                          onChange={(e) => handleChannelSearch(e.target.value)}
                          onFocus={() => channelQuery && setShowDropdown(true)}
                          required
                          data-testid="channel-search-input"
                        />
                        {isSearching && (
                          <InputGroup.Text>
                            <div className="spinner-border spinner-border-sm" role="status">
                              <span className="visually-hidden">Loading...</span>
                            </div>
                          </InputGroup.Text>
                        )}
                      </InputGroup>
                      
                      {showDropdown && (
                        <ListGroup 
                          className="position-absolute w-100 mt-1 z-3 shadow channel-dropdown" 
                          style={{ maxHeight: '200px', overflowY: 'auto' }}
                          data-testid="channel-dropdown"
                        >
                          {searchResults.length === 0 ? (
                            <ListGroup.Item className="text-muted">
                              {isSearching ? 'Searching...' : 'No channels found'}
                            </ListGroup.Item>
                          ) : (
                            searchResults.map((channel) => (
                              <ListGroup.Item 
                                key={channel.uri}
                                action
                                onClick={() => handleChannelSelect(channel)}
                                className="d-flex align-items-center"
                                data-testid={`channel-option-${channel.name.replace(/\s+/g, '-').toLowerCase()}`}
                              >
                                {channel.logo && (
                                  <img 
                                    src={channel.logo} 
                                    alt="Logo" 
                                    className="me-2" 
                                    style={{ height: '24px', width: '24px', objectFit: 'contain' }}
                                  />
                                )}
                                <span>{channel.name}</span>
                              </ListGroup.Item>
                            ))
                          )}
                        </ListGroup>
                      )}
                    </Form.Group>
                  </Col>
                  <Col lg={6}>
                    <Row className="g-3">
                      <Col md={6}>
                        <Form.Group className="mb-3">
                          <Form.Label className="fw-bold text-dark">Start Time</Form.Label>
                          <Form.Control
                            type="datetime-local"
                            name="startTime"
                            value={formData.startTime || ''}
                            onChange={handleInputChange}
                            required
                          />
                        </Form.Group>
                      </Col>
                      <Col md={6}>
                        <Form.Group className="mb-3">
                          <Form.Label className="fw-bold text-dark">End Time</Form.Label>
                          <Form.Control
                            type="datetime-local"
                            name="endTime"
                            value={formData.endTime || ''}
                            onChange={handleInputChange}
                            required
                          />
                        </Form.Group>
                      </Col>
                    </Row>
                  </Col>
                </Row>

                <Card className="mb-4 bg-light border">
                  <Card.Body>
                    <Form.Group>
                      <Form.Label className="fw-bold text-dark d-flex align-items-center">
                        <i className="bi bi-folder-fill me-2 text-primary"></i>
                        Save Recording To
                      </Form.Label>
                      <InputGroup>
                        <InputGroup.Text className="bg-dark text-white">
                          <i className="bi bi-folder-fill"></i>
                        </InputGroup.Text>
                        <Form.Control
                          readOnly
                          value={getFullFolderpath()}
                          className="bg-white"
                        />
                        <Button 
                          variant="outline-primary" 
                          onClick={() => setShowFolderBrowser(true)}
                          title="Browse folders"
                        >
                          <i className="bi bi-folder-symlink me-2"></i>
                          Browse
                        </Button>
                      </InputGroup>
                      <Form.Text className="text-muted">
                        Select a subfolder to organize your recordings
                      </Form.Text>
                    </Form.Group>

                    {formData.filename && (
                      <div className="mt-3">
                        <Form.Label className="fw-bold text-dark">File</Form.Label>
                        <p className="bg-white p-2 border rounded text-truncate">
                          <i className="bi bi-file-earmark-play text-primary me-1"></i>
                          {formData.filename}
                        </p>
                      </div>
                    )}
                  </Card.Body>
                </Card>
                
                <div className="d-flex justify-content-end mt-4">
                  <Button variant="outline-secondary" className="me-2" onClick={handleCancel}>
                    <i className="bi bi-x-circle me-1"></i>
                    Cancel
                  </Button>
                  <Button 
                    variant="primary" 
                    type="submit" 
                    disabled={!isFormValid}
                    data-testid="save-recording-btn"
                    className="px-4"
                  >
                    <i className="bi bi-save me-1"></i>
                    Save
                  </Button>
                </div>
              </Form>
            </>
          )}
        </Card.Body>
      </Card>

      {/* Folder browser modal */}
      <FolderBrowserModal
        show={showFolderBrowser}
        onHide={() => setShowFolderBrowser(false)}
        onSelect={handleFolderSelect}
        basePath={baseRecordingPath}
        initialPath={selectedFolder || baseRecordingPath}
      />
    </Container>
  );
};

export default RecordingFormPage;
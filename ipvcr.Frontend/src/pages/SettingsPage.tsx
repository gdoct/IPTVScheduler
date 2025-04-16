import React, { useEffect, useRef, useState } from 'react';
import { Alert, Button, Card, Container, Form, InputGroup, Table } from 'react-bootstrap';
import { AuthService } from '../services/AuthService';
import { recordingsApi } from '../services/RecordingsApi';
import { SchedulerSettings } from '../types/recordings';

const SettingsPage: React.FC = () => {
  // State for settings
  const [settings, setSettings] = useState<SchedulerSettings>({
    mediaPath: '',
    dataPath: '',
    m3uPlaylistPath: '',
    adminUsername: ''
  });
  const [adminPassword, setAdminPassword] = useState<string>('');
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [uploading, setUploading] = useState<boolean>(false);
  const [changingPassword, setChangingPassword] = useState<boolean>(false);
  
  // Reference to file input
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Validate token on page load
  useEffect(() => {
    const validateTokenOnLoad = async () => {
      try {
        // This will automatically redirect to login if token is invalid
        await AuthService.validateToken();
      } catch (error) {
        console.error('Token validation error:', error);
        // The validateToken function will handle redirection if token is invalid
      }
    };
    
    validateTokenOnLoad();
  }, []);

  // Load settings on component mount
  useEffect(() => {
    fetchSettings();
  }, []);

  // Fetch settings from API
  const fetchSettings = async () => {
    setLoading(true);
    try {
      const data = await recordingsApi.getSettings();
      setSettings({
        mediaPath: data.mediaPath || '',
        dataPath: data.dataPath || '',
        m3uPlaylistPath: data.m3uPlaylistPath || '',
        adminUsername: data.adminUsername || ''
      });
      setError(null);
    } catch (err) {
      console.error('Error fetching settings:', err);
      setError('Failed to load settings');
    } finally {
      setLoading(false);
    }
  };

  // Handle input changes
  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    if (name === 'adminPassword') {
      setAdminPassword(value);
    } else {
      setSettings(prev => ({
        ...prev,
        [name]: value
      }));
    }
  };

  // Handle form submission
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    try {
      await recordingsApi.updateSettings(settings);
      setSuccess('Settings saved successfully');
      setError(null);
      
      // Reset success message after 3 seconds
      setTimeout(() => {
        setSuccess(null);
      }, 3000);
    } catch (err) {
      console.error('Error saving settings:', err);
      setError('Failed to save settings');
      setSuccess(null);
    }
  };

  // Handle password change
  const handlePasswordChange = async () => {
    if (!adminPassword) {
      setError('Please enter a new password');
      return;
    }
    
    setChangingPassword(true);
    try {
      const response = await fetch(`${process.env.REACT_APP_API_URL || '/api'}/login/changepassword`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${AuthService.getToken()}`
        },
        body: JSON.stringify({
          username: settings.adminUsername,
          password: adminPassword
        })
      });
      
      if (response.ok) {
        setSuccess('Password changed successfully');
        setAdminPassword(''); // Clear password field after successful change
      } else {
        const errorData = await response.json();
        throw new Error(errorData.message || 'Failed to update password');
      }
    } catch (err) {
      console.error('Error changing password:', err);
      setError('Failed to update password');
    } finally {
      setChangingPassword(false);
    }
  };

  // Handle M3U file upload
  const handleUploadM3U = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!fileInputRef.current || !fileInputRef.current.files || fileInputRef.current.files.length === 0) {
      setError('Please select a file to upload');
      return;
    }
    
    const file = fileInputRef.current.files[0];
    setUploading(true);
    setError(null);
    
    try {
      const result = await recordingsApi.uploadM3uPlaylist(file);
      setSuccess(result.message || 'M3U file uploaded successfully');
      
      // Refresh settings to get updated M3U path
      fetchSettings();
      
      // Reset file input
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    } catch (err) {
      console.error('Error uploading file:', err);
      setError('Failed to upload M3U file');
      setSuccess(null);
    } finally {
      setUploading(false);
    }
  };

  return (
    <Container fluid className="px-4">
        <div className="hero-section mb-4">
        <img 
          src="/ipvcr.png" 
          className="img-fluid w-60" 
          alt="IPVCR Hero Image" 
          style={{ objectFit: 'cover', maxHeight: '250px', width: '40%' }}
        />
      </div>
      <h2 className="mb-4">Settings</h2>
      
      {error && (
        <Alert variant="danger" onClose={() => setError(null)} dismissible>
          <i className="bi bi-exclamation-triangle-fill me-2"></i>
          {error}
        </Alert>
      )}
      
      {success && (
        <Alert variant="success" onClose={() => setSuccess(null)} dismissible>
          <i className="bi bi-check-circle-fill me-2"></i>
          {success}
        </Alert>
      )}
      
      {loading ? (
        <div className="text-center py-5">
          <div className="spinner-border text-primary" role="status">
            <span className="visually-hidden">Loading...</span>
          </div>
          <p className="mt-2">Loading settings...</p>
        </div>
      ) : (
        <>
          <Card className="mb-4">
            <Card.Body>
              <Form onSubmit={handleSubmit}>
                <Table>
                  <thead>
                    <tr>
                      <th>Setting</th>
                      <th>Value</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr>
                      <td>Media Path</td>
                      <td>
                        <Form.Control
                          type="text"
                          name="mediaPath"
                          value={settings.mediaPath}
                          onChange={handleInputChange}
                        />
                      </td>
                    </tr>
                    <tr>
                      <td>Data Path</td>
                      <td>
                        <Form.Control
                          type="text"
                          name="dataPath"
                          value={settings.dataPath}
                          onChange={handleInputChange}
                        />
                      </td>
                    </tr>
                    <tr>
                      <td>M3u Playlist Path</td>
                      <td>
                        <Form.Control
                          type="text"
                          name="m3uPlaylistPath"
                          value={settings.m3uPlaylistPath}
                          onChange={handleInputChange}
                        />
                      </td>
                    </tr>
                    <tr>
                      <td>Admin Username</td>
                      <td>
                        <Form.Control
                          type="text"
                          name="adminUsername"
                          value={settings.adminUsername}
                          onChange={handleInputChange}
                        />
                      </td>
                    </tr>
                    <tr>
                      <td>Admin Password</td>
                      <td>
                        <InputGroup>
                          <Form.Control
                            type="password"
                            name="adminPassword"
                            placeholder="Enter new password"
                            value={adminPassword}
                            onChange={handleInputChange}
                          />
                          <Button 
                            variant="outline-secondary" 
                            onClick={handlePasswordChange}
                            disabled={changingPassword}
                          >
                            {changingPassword ? (
                              <>
                                <span className="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>
                                Changing...
                              </>
                            ) : (
                              <>Change Password</>
                            )}
                          </Button>
                        </InputGroup>
                        <Form.Text className="text-muted">
                          This won't be saved with other settings. Use the Change Password button to update it.
                        </Form.Text>
                      </td>
                    </tr>
                  </tbody>
                </Table>
                <Button type="submit" variant="success" disabled={loading || uploading}>
                  <i className="bi bi-save me-2"></i>Save Settings
                </Button>
              </Form>
            </Card.Body>
          </Card>

          <h3>Upload M3U playlist</h3>
          <Card>
            <Card.Body>
              <Form onSubmit={handleUploadM3U}>
                <Form.Group className="mb-3">
                  <Form.Label>Select M3U playlist</Form.Label>
                  <Form.Control
                    type="file"
                    ref={fileInputRef}
                    accept=".m3u,.m3u8"
                  />
                </Form.Group>
                <Button 
                  type="submit" 
                  variant="primary" 
                  disabled={uploading}
                >
                  {uploading ? (
                    <>
                      <span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
                      Uploading...
                    </>
                  ) : (
                    <>
                      <i className="bi bi-cloud-upload me-2"></i>Upload
                    </>
                  )}
                </Button>
              </Form>
            </Card.Body>
          </Card>
        </>
      )}
    </Container>
  );
};

export default SettingsPage;
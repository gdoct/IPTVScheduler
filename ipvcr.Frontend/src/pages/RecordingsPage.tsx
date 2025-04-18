import React, { useEffect, useState } from 'react';
import { Alert, Button, Card, Container } from 'react-bootstrap';
import { useLocation, useNavigate } from 'react-router-dom';
import RecordingsTable from '../components/RecordingsTable';
import { useRouterReset } from '../components/RouterReset';
import TaskEditor from '../components/TaskEditor';
import { AuthService } from '../services/AuthService';
import { recordingsApi } from '../services/RecordingsApi';
import { HomeRecordingsViewModel, TaskDefinitionModel } from '../types/recordings';

// Refresh interval in milliseconds (30 seconds)
const REFRESH_INTERVAL = 300000;

const RecordingsPage: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { resetRouter } = useRouterReset();
  
  // State management
  const [data, setData] = useState<HomeRecordingsViewModel>({
    recordings: [],
    channelsCount: 0,
    recordingPath: ''
  });
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  
  // Task editor modal state (we'll keep this as a modal for now)
  const [showTaskEditor, setShowTaskEditor] = useState<boolean>(false);
  const [selectedTask, setSelectedTask] = useState<TaskDefinitionModel | null>(null);

  // Added diagnostic state
  const [routerState, setRouterState] = useState<any>(null);
  const [lastDeleted, setLastDeleted] = useState<string | null>(null);

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

  // Load data on component mount and set up refresh interval
  useEffect(() => {
    // Initial data fetch
    fetchData();
    
    // Set up interval for periodic refresh
    const intervalId = setInterval(() => {
      fetchData();
    }, REFRESH_INTERVAL);
    
    // Clean up interval on component unmount
    return () => clearInterval(intervalId);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []); // fetchData is intentionally excluded to avoid unnecessary recreation of interval

  // Fetch all required data
  const fetchData = async () => {
    setLoading(prevLoading => {
      // Only show loading indicator on initial load, not on background refreshes
      return prevLoading && data.recordings.length === 0;
    });
    
    try {
      const viewModel = await recordingsApi.getHomeRecordingsViewModel();
      setData(viewModel);
      setError(null);
    } catch (err) {
      console.error('Error fetching data:', err);
      setError('Failed to load recordings data');
    } finally {
      setLoading(false);
    }
  };

  // Handler for navigating to add recording page
  const handleAddRecording = () => {
    navigate('/recordings/new', { state: { recordingPath: data.recordingPath } });
  };

  // Handler for navigating to edit recording page
  const handleEditRecording = (id: string) => {
    navigate(`/recordings/${id}`, { state: { recordingPath: data.recordingPath } });
  };

  // Enhanced handler for deleting a recording with a hard reset approach
  const handleDeleteRecording = async (id: string) => {
    if (window.confirm('Are you sure you want to delete this recording?')) {
      try {
        console.log('Performing deletion of recording:', id);
        // Call the API to delete the recording
        await recordingsApi.deleteRecording(id);
        
        // Show immediate feedback by updating the UI
        setData(prevData => ({
          ...prevData,
          recordings: prevData.recordings.filter(r => r.id !== id)
        }));

        // The hard reset approach - store current path then force a complete app reload
        const currentPath = window.location.pathname;
        
        // Alert the user about the upcoming refresh (can be removed in production)
        const alertMsg = document.createElement('div');
        alertMsg.style.position = 'fixed';
        alertMsg.style.top = '20px';
        alertMsg.style.left = '50%';
        alertMsg.style.transform = 'translateX(-50%)';
        alertMsg.style.backgroundColor = '#28a745';
        alertMsg.style.color = 'white';
        alertMsg.style.padding = '10px 20px';
        alertMsg.style.borderRadius = '4px';
        alertMsg.style.zIndex = '9999';
        alertMsg.style.boxShadow = '0 4px 8px rgba(0,0,0,0.1)';
        alertMsg.innerText = 'Deletion successful! Refreshing page...';
        document.body.appendChild(alertMsg);
        
        // Give a short delay for visual feedback, then reload
        setTimeout(() => {
          // Use the history API to generate a clean state
          window.history.replaceState({}, '', currentPath);
          window.location.reload();
        }, 800);
        
      } catch (err) {
        console.error('Error deleting recording:', err);
        setError('Failed to delete recording');
      }
    }
  };

  // Diagnostic function to attempt recovery
  const handleManualRouterReset = () => {
    // Reset router state and force reload
    resetRouter();
    window.setTimeout(() => {
      window.location.reload();
    }, 50);
  };

  // Handler for editing a task
  const handleEditTask = async (id: string) => {
    try {
      const task = await recordingsApi.getTaskDefinition(id);
      setSelectedTask(task);
      setShowTaskEditor(true);
    } catch (err) {
      console.error('Error fetching task definition:', err);
      setError('Failed to load task definition');
    }
  };

  // Handler for saving a task definition
  const handleSaveTask = async (id: string, content: string) => {
    try {
      await recordingsApi.updateTaskDefinition(id, content);
      setShowTaskEditor(false);
      fetchData();
    } catch (err) {
      console.error('Error saving task definition:', err);
      setError('Failed to save task definition');
    }
  };

  return (
    <Container fluid className="px-4">
      {/* Hero section */}
      <div className="hero-section mb-4">
        <img 
          src="/ipvcr.png" 
          className="img-fluid w-60" 
          alt="IPVCR Logo" 
          style={{ objectFit: 'cover', maxHeight: '250px', width: '40%' }}
        />
      </div>

      {/* Error message */}
      {error && (
        <Alert variant="danger" onClose={() => setError(null)} dismissible>
          <i className="bi bi-exclamation-triangle-fill me-2"></i>
          {error}
        </Alert>
      )}

      {/* No channels warning */}
      {data.channelsCount === 0 ? (
        <Alert variant="warning">
          <i className="bi bi-exclamation-triangle-fill me-2"></i>
          No channels available! Use the settings page to upload a m3u file.
        </Alert>
      ) : (
        <div className="row mb-4">
          <div className="col-md-12">
            <Card className="bg-light">
              <Card.Body>
                <div className="d-flex align-items-center">
                  <i className="bi bi-info-circle text-primary me-2 fs-4"></i>
                  <div>
                    <div><strong>Recording path:</strong> {data.recordingPath}</div>
                    <div><strong>Channel count:</strong> {data.channelsCount}</div>
                  </div>
                </div>
              </Card.Body>
            </Card>
          </div>
        </div>
      )}

      {/* Router Diagnostic Panel (only shows after deletion) */}
      {lastDeleted && (
        <div className="row mb-4">
          <div className="col-md-12">
            <Card className="border-warning">
              <Card.Header className="bg-warning text-dark">
                <i className="bi bi-bug me-2"></i>
                Diagnostic Panel (Record {lastDeleted} was deleted)
              </Card.Header>
              <Card.Body>
                <div className="mb-3">
                  <p>Router State: {JSON.stringify(routerState)}</p>
                  <p>Current Location: {JSON.stringify({
                    pathname: location.pathname,
                    search: location.search,
                    hash: location.hash,
                  })}</p>
                </div>
                <div>
                  <p>If links aren't working properly, try one of these solutions:</p>
                  <Button 
                    variant="outline-primary" 
                    className="me-2"
                    onClick={() => { navigate('/'); }}
                  >
                    Navigate to Home
                  </Button>
                  <Button 
                    variant="outline-secondary" 
                    className="me-2"
                    onClick={() => { navigate('/settings'); }}
                  >
                    Navigate to Settings
                  </Button>
                  <Button 
                    variant="outline-danger"
                    onClick={handleManualRouterReset}
                  >
                    Reload Page
                  </Button>
                </div>
              </Card.Body>
            </Card>
          </div>
        </div>
      )}

      {/* Recordings section */}
      <div className="row">
        <div className="col-12">
          {loading ? (
            <div className="text-center py-5">
              <div className="spinner-border text-primary" role="status">
                <span className="visually-hidden">Loading...</span>
              </div>
              <p className="mt-2">Loading recordings...</p>
            </div>
          ) : (
            <RecordingsTable
              recordings={data.recordings}
              onEdit={handleEditRecording}
              onEditTask={handleEditTask}
              onDelete={handleDeleteRecording}
              onAdd={handleAddRecording} 
              showAddButton={data.channelsCount > 0}
            />
          )}
        </div>
      </div>

      {/* Task Editor Modal - keeping this as modal for now */}
      <TaskEditor
        show={showTaskEditor}
        onHide={() => setShowTaskEditor(false)}
        onSave={handleSaveTask}
        taskDefinition={selectedTask}
      />
    </Container>
  );
};

export default RecordingsPage;
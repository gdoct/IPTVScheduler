import React, { useEffect, useState } from 'react';
import { Alert, Card, Container } from 'react-bootstrap';
import RecordingForm from '../components/RecordingForm';
import RecordingsTable from '../components/RecordingsTable';
import TaskEditor from '../components/TaskEditor';
import { recordingsApi } from '../services/RecordingsApi';
import { HomeRecordingsViewModel, ScheduledRecording, TaskDefinitionModel } from '../types/recordings';

const RecordingsPage: React.FC = () => {
  // State management
  const [data, setData] = useState<HomeRecordingsViewModel>({
    recordings: [],
    channels: [],
    recordingPath: ''
  });
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  
  // Modal state
  const [showRecordingForm, setShowRecordingForm] = useState<boolean>(false);
  const [showTaskEditor, setShowTaskEditor] = useState<boolean>(false);
  const [selectedRecording, setSelectedRecording] = useState<ScheduledRecording | null>(null);
  const [selectedTask, setSelectedTask] = useState<TaskDefinitionModel | null>(null);

  // Load data on component mount
  useEffect(() => {
    fetchData();
  }, []);

  // Fetch all required data
  const fetchData = async () => {
    setLoading(true);
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

  // Handler for opening the recording form to create a new recording
  const handleAddRecording = () => {
    setSelectedRecording(null);
    setShowRecordingForm(true);
  };

  // Handler for editing an existing recording
  const handleEditRecording = async (id: string) => {
    try {
      const recording = await recordingsApi.getRecording(id);
      setSelectedRecording(recording);
      setShowRecordingForm(true);
    } catch (err) {
      console.error('Error fetching recording:', err);
      setError('Failed to load recording details');
    }
  };

  // Handler for deleting a recording
  const handleDeleteRecording = async (id: string) => {
    if (window.confirm('Are you sure you want to delete this recording?')) {
      try {
        await recordingsApi.deleteRecording(id);
        // Refresh data after delete
        fetchData();
      } catch (err) {
        console.error('Error deleting recording:', err);
        setError('Failed to delete recording');
      }
    }
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

  // Handler for saving a recording (create or update)
  const handleSaveRecording = async (recording: ScheduledRecording) => {
    try {
      if (recording.id && recording.id !== '00000000-0000-0000-0000-000000000000') {
        await recordingsApi.updateRecording(recording.id, recording);
      } else {
        await recordingsApi.createRecording(recording);
      }
      setShowRecordingForm(false);
      fetchData();
    } catch (err) {
      console.error('Error saving recording:', err);
      setError('Failed to save recording');
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
          alt="IPVCR Hero Image" 
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
      {data.channels.length === 0 ? (
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
                    <div><strong>Channel count:</strong> {data.channels.length}</div>
                  </div>
                </div>
              </Card.Body>
            </Card>
          </div>
        </div>
      )}

      {/* Recordings section */}
      <div className="row">
        <div className="col-12">
          {/* Removing the button from here */}
          
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
              channels={data.channels}
              onEdit={handleEditRecording}
              onEditTask={handleEditTask}
              onDelete={handleDeleteRecording}
              onAdd={handleAddRecording} 
              showAddButton={data.channels.length > 0}  
            />
          )}
        </div>
      </div>

      {/* Recording Form Modal */}
      <RecordingForm
        show={showRecordingForm}
        onHide={() => setShowRecordingForm(false)}
        onSave={handleSaveRecording}
        recording={selectedRecording}
        channels={data.channels}
        recordingPath={data.recordingPath}
      />

      {/* Task Editor Modal */}
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
import React, { useEffect, useState } from 'react';
import { Button, Col, Form, InputGroup, Modal, Row } from 'react-bootstrap';
import { ChannelInfo, formatFileDate, formatNiceDate, ScheduledRecording } from '../types/recordings';

interface RecordingFormProps {
  show: boolean;
  onHide: () => void;
  onSave: (recording: ScheduledRecording) => void;
  recording?: ScheduledRecording | null;
  channels: ChannelInfo[];
  recordingPath: string;
}

const RecordingForm: React.FC<RecordingFormProps> = ({
  show,
  onHide,
  onSave,
  recording,
  channels,
  recordingPath
}) => {
  const isEdit = Boolean(recording && recording.id);

  // Initialize form state
  const [formData, setFormData] = useState<Partial<ScheduledRecording>>({
    id: '',
    name: '',
    description: '',
    channelUri: channels.length > 0 ? channels[0].uri : '',
    channelName: channels.length > 0 ? parseChannelName(channels[0].name) : '',
    startTime: getTomorrowDatetime(),
    endTime: getTomorrowDatetimePlusHour(),
    filename: ''
  });

  const [channelLogo, setChannelLogo] = useState<string>(
    channels.length > 0 ? channels[0].logo : ''
  );

  // Update form when recording changes or modal opens
  useEffect(() => {
    if (recording) {
      setFormData(recording);
      const selectedChannel = channels.find(c => c.uri === recording.channelUri);
      if (selectedChannel) {
        setChannelLogo(selectedChannel.logo);
      }
    } else {
      // Reset form to default values
      setFormData({
        id: '',
        name: '',
        description: '',
        channelUri: channels.length > 0 ? channels[0].uri : '',
        channelName: channels.length > 0 ? parseChannelName(channels[0].name) : '',
        startTime: getTomorrowDatetime(),
        endTime: getTomorrowDatetimePlusHour(),
        filename: ''
      });
      if (channels.length > 0) {
        setChannelLogo(channels[0].logo);
      }
    }
  }, [recording, show, channels]);

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

  // Handler for input changes - Updated to include HTMLTextAreaElement for Form.Control
  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    
    // Special handling for channel selection
    if (name === 'channelUri') {
      const selectedChannel = channels.find(c => c.uri === value);
      if (selectedChannel) {
        setChannelLogo(selectedChannel.logo);
        setFormData(prev => ({
          ...prev,
          channelUri: value,
          channelName: parseChannelName(selectedChannel.name)
        }));
      }
    } else {
      setFormData(prev => ({ ...prev, [name]: value }));
    }
    
    // Update filename and description when necessary fields change
    if (['name', 'startTime', 'channelUri'].includes(name)) {
      updateFilenameAndDescription();
    }
  };

  // Update filename and description based on form data
  const updateFilenameAndDescription = () => {
    if (formData.name && formData.startTime && formData.channelName) {
      // Generate filename
      const startDate = new Date(formData.startTime);
      const startTimeFormatted = formatFileDate(startDate) + 
        formData.startTime.split('T')[1].replace(':', '').substring(0, 4);
      
      const sanitizedName = formData.name.replace(/ /g, '_').toLowerCase();
      const filename = `${recordingPath}/${sanitizedName}_${startTimeFormatted}.mp4`;
      
      // Generate description
      const niceStartTime = formatNiceDate(startDate);
      const description = `${formData.name} - recorded from ${formData.channelName} at ${niceStartTime}`;
      
      setFormData(prev => ({
        ...prev,
        filename,
        description
      }));
    }
  };

  // Form submission handler
  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    // Validate form
    if (!formData.name || !formData.startTime || !formData.endTime || !formData.channelUri) {
      return; // Form validation failed
    }
    
    // Submit the form
    onSave(formData as ScheduledRecording);
  };

  // Check if form is valid
  const isFormValid = 
    formData.name && 
    formData.startTime && 
    formData.endTime && 
    formData.channelUri &&
    formData.channelName;

  return (
    <Modal show={show} onHide={onHide} size="lg">
      <Modal.Header className="bg-primary text-white">
        <Modal.Title>
          {isEdit ? (
            <><i className="bi bi-pencil-square me-2"></i>Edit Recording</>
          ) : (
            <><i className="bi bi-plus-circle me-2"></i>Add New Recording</>
          )}
        </Modal.Title>
        <Button variant="close" onClick={onHide} className="btn-close-white" aria-label="Close" />
      </Modal.Header>
      <Modal.Body>
        <Form onSubmit={handleSubmit}>
          <input type="hidden" name="id" value={formData.id || ''} />
          
          <Row className="mb-3">
            <Col md={3} className="text-center">
              <img 
                src={channelLogo} 
                alt="Channel Logo"
                className="img-fluid rounded mb-2" 
                style={{ maxHeight: '100px', maxWidth: '100px', objectFit: 'contain' }} 
              />
            </Col>
            <Col md={9}>
              <Form.Group className="mb-3">
                <Form.Label className="fw-bold">Recording name</Form.Label>
                <Form.Control
                  type="text"
                  name="name"
                  value={formData.name || ''}
                  onChange={handleInputChange}
                  required
                />
              </Form.Group>
              
              <Form.Group className="mb-3">
                <Form.Label className="fw-bold">Description</Form.Label>
                <Form.Control
                  type="text"
                  className="bg-light text-muted"
                  value={formData.description || ''}
                  disabled
                />
              </Form.Group>
            </Col>
          </Row>

          <Row>
            <Col md={6}>
              <Form.Group className="mb-3">
                <Form.Label className="fw-bold">Channel</Form.Label>
                <Form.Select
                  name="channelUri"
                  value={formData.channelUri || ''}
                  onChange={handleInputChange}
                  required
                >
                  {channels.map(channel => (
                    <option key={channel.uri} value={channel.uri}>
                      {channel.name}
                    </option>
                  ))}
                </Form.Select>
              </Form.Group>
            </Col>
            <Col md={6}>
              <Row>
                <Col md={6}>
                  <Form.Group className="mb-3">
                    <Form.Label className="fw-bold">Start Time</Form.Label>
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
                    <Form.Label className="fw-bold">End Time</Form.Label>
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

          {formData.filename && (
            <Form.Group className="mb-3">
              <Form.Label className="fw-bold">Recording will be saved to</Form.Label>
              <InputGroup>
                <InputGroup.Text>
                  <i className="bi bi-file-earmark-play"></i>
                </InputGroup.Text>
                <Form.Control
                  className="bg-light text-muted text-truncate"
                  value={formData.filename}
                  disabled
                />
              </InputGroup>
            </Form.Group>
          )}
        </Form>
      </Modal.Body>
      <div className="card-footer bg-light p-3 d-flex justify-content-end">
        <Button variant="secondary" className="me-2" onClick={onHide}>
          <i className="bi bi-x-circle me-1"></i>Cancel
        </Button>
        <Button 
          variant="primary" 
          onClick={handleSubmit} 
          disabled={!isFormValid}
        >
          <i className="bi bi-save me-1"></i>Save
        </Button>
      </div>
    </Modal>
  );
};

export default RecordingForm;
import React from 'react';
import { Card, Col, Form, InputGroup, Row } from 'react-bootstrap';
import { FfmpegSettings } from '../../types/recordings';

interface FfmpegSettingsProps {
  settings: FfmpegSettings;
  handleInputChange: (e: React.ChangeEvent<HTMLSelectElement>) => void;
}

// Define dropdown options for each field
const fileTypeOptions = ['default', 'mp4', 'mkv', 'avi', 'mov', 'flv', 'webm', 'ts'];
const videoCodecOptions = ['default', 'libx264', 'libx265', 'mpeg4', 'libvpx', 'libvpx-vp9'];
const audioCodecOptions = ['default', 'aac', 'mp3', 'ac3', 'flac', 'opus', 'vorbis'];
const videoBitrateOptions = ['default', '500k', '1000k', '2000k', '3000k', '5000k', '8000k', '10000k'];
const audioBitrateOptions = ['default', '64k', '96k', '128k', '192k', '256k', '320k'];
const resolutionOptions = ['default', '640x480', '800x600', '1280x720', '1920x1080', '2560x1440', '3840x2160'];
const frameRateOptions = ['default', '24', '25', '30', '50', '60'];
const aspectRatioOptions = ['default', '4:3', '16:9', '21:9', '1:1'];
const outputFormatOptions = ['default', 'mp4', 'mkv', 'avi', 'mov', 'flv', 'webm', 'ts'];

const FfmpegSettingsComponent: React.FC<FfmpegSettingsProps> = ({ settings, handleInputChange }) => {
  
  // Helper function to get display value (showing 'default' when value is empty)
  const getDisplayValue = (value: string) => {
    return value === '' ? 'default' : value;
  };
  
  return (
    <Card className="mb-4">
      <Card.Header className="bg-light">
        <i className="bi bi-film me-2"></i>FFmpeg Settings
      </Card.Header>
      <Card.Body>
        <Row className="mb-3">
          <Col md={12} lg={6}>
            <Form.Group className="mb-3">
              <Form.Label>File Type</Form.Label>
              <InputGroup>
                <InputGroup.Text>
                  <i className="bi bi-file-earmark"></i>
                </InputGroup.Text>
                <Form.Select
                  name="fileType"
                  value={getDisplayValue(settings.fileType)}
                  onChange={handleInputChange}
                >
                  {fileTypeOptions.map(option => (
                    <option key={option} value={option === 'default' ? '' : option}>
                      {option}
                    </option>
                  ))}
                </Form.Select>
              </InputGroup>
              <Form.Text className="text-muted">
                Output file type (e.g., mp4, mkv)
              </Form.Text>
            </Form.Group>
          </Col>
          
          <Col md={12} lg={6}>
            <Form.Group className="mb-3">
              <Form.Label>Output Format</Form.Label>
              <InputGroup>
                <InputGroup.Text>
                  <i className="bi bi-file-earmark-binary"></i>
                </InputGroup.Text>
                <Form.Select
                  name="outputFormat"
                  value={getDisplayValue(settings.outputFormat)}
                  onChange={handleInputChange}
                >
                  {outputFormatOptions.map(option => (
                    <option key={option} value={option === 'default' ? '' : option}>
                      {option}
                    </option>
                  ))}
                </Form.Select>
              </InputGroup>
              <Form.Text className="text-muted">
                Output format for ffmpeg (e.g., mp4, mkv)
              </Form.Text>
            </Form.Group>
          </Col>
        </Row>

        <Row className="mb-3">
          <Col md={12} lg={6}>
            <Form.Group className="mb-3">
              <Form.Label>Video Codec</Form.Label>
              <InputGroup>
                <InputGroup.Text>
                  <i className="bi bi-camera-video"></i>
                </InputGroup.Text>
                <Form.Select
                  name="codec"
                  value={getDisplayValue(settings.codec)}
                  onChange={handleInputChange}
                >
                  {videoCodecOptions.map(option => (
                    <option key={option} value={option === 'default' ? '' : option}>
                      {option}
                    </option>
                  ))}
                </Form.Select>
              </InputGroup>
              <Form.Text className="text-muted">
                Video codec (e.g., libx264, libx265)
              </Form.Text>
            </Form.Group>
          </Col>
          
          <Col md={12} lg={6}>
            <Form.Group className="mb-3">
              <Form.Label>Audio Codec</Form.Label>
              <InputGroup>
                <InputGroup.Text>
                  <i className="bi bi-music-note-beamed"></i>
                </InputGroup.Text>
                <Form.Select
                  name="audioCodec"
                  value={getDisplayValue(settings.audioCodec)}
                  onChange={handleInputChange}
                >
                  {audioCodecOptions.map(option => (
                    <option key={option} value={option === 'default' ? '' : option}>
                      {option}
                    </option>
                  ))}
                </Form.Select>
              </InputGroup>
              <Form.Text className="text-muted">
                Audio codec (e.g., aac, mp3)
              </Form.Text>
            </Form.Group>
          </Col>
        </Row>

        <Row className="mb-3">
          <Col md={12} lg={6}>
            <Form.Group className="mb-3">
              <Form.Label>Video Bitrate</Form.Label>
              <InputGroup>
                <InputGroup.Text>
                  <i className="bi bi-speedometer"></i>
                </InputGroup.Text>
                <Form.Select
                  name="videoBitrate"
                  value={getDisplayValue(settings.videoBitrate)}
                  onChange={handleInputChange}
                >
                  {videoBitrateOptions.map(option => (
                    <option key={option} value={option === 'default' ? '' : option}>
                      {option}
                    </option>
                  ))}
                </Form.Select>
              </InputGroup>
              <Form.Text className="text-muted">
                Video bitrate (e.g., 1000k, 2M)
              </Form.Text>
            </Form.Group>
          </Col>
          
          <Col md={12} lg={6}>
            <Form.Group className="mb-3">
              <Form.Label>Audio Bitrate</Form.Label>
              <InputGroup>
                <InputGroup.Text>
                  <i className="bi bi-soundwave"></i>
                </InputGroup.Text>
                <Form.Select
                  name="audioBitrate"
                  value={getDisplayValue(settings.audioBitrate)}
                  onChange={handleInputChange}
                >
                  {audioBitrateOptions.map(option => (
                    <option key={option} value={option === 'default' ? '' : option}>
                      {option}
                    </option>
                  ))}
                </Form.Select>
              </InputGroup>
              <Form.Text className="text-muted">
                Audio bitrate (e.g., 128k, 192k)
              </Form.Text>
            </Form.Group>
          </Col>
        </Row>

        <Row className="mb-3">
          <Col md={12} lg={4}>
            <Form.Group className="mb-3">
              <Form.Label>Resolution</Form.Label>
              <InputGroup>
                <InputGroup.Text>
                  <i className="bi bi-display"></i>
                </InputGroup.Text>
                <Form.Select
                  name="resolution"
                  value={getDisplayValue(settings.resolution)}
                  onChange={handleInputChange}
                >
                  {resolutionOptions.map(option => (
                    <option key={option} value={option === 'default' ? '' : option}>
                      {option}
                    </option>
                  ))}
                </Form.Select>
              </InputGroup>
              <Form.Text className="text-muted">
                Video resolution (e.g., 1280x720)
              </Form.Text>
            </Form.Group>
          </Col>
          
          <Col md={12} lg={4}>
            <Form.Group className="mb-3">
              <Form.Label>Frame Rate</Form.Label>
              <InputGroup>
                <InputGroup.Text>
                  <i className="bi bi-stopwatch"></i>
                </InputGroup.Text>
                <Form.Select
                  name="frameRate"
                  value={getDisplayValue(settings.frameRate)}
                  onChange={handleInputChange}
                >
                  {frameRateOptions.map(option => (
                    <option key={option} value={option === 'default' ? '' : option}>
                      {option}
                    </option>
                  ))}
                </Form.Select>
              </InputGroup>
              <Form.Text className="text-muted">
                Video frame rate (e.g., 30, 60)
              </Form.Text>
            </Form.Group>
          </Col>
          
          <Col md={12} lg={4}>
            <Form.Group className="mb-3">
              <Form.Label>Aspect Ratio</Form.Label>
              <InputGroup>
                <InputGroup.Text>
                  <i className="bi bi-aspect-ratio"></i>
                </InputGroup.Text>
                <Form.Select
                  name="aspectRatio"
                  value={getDisplayValue(settings.aspectRatio)}
                  onChange={handleInputChange}
                >
                  {aspectRatioOptions.map(option => (
                    <option key={option} value={option === 'default' ? '' : option}>
                      {option}
                    </option>
                  ))}
                </Form.Select>
              </InputGroup>
              <Form.Text className="text-muted">
                Video aspect ratio (e.g., 16:9)
              </Form.Text>
            </Form.Group>
          </Col>
        </Row>
      </Card.Body>
    </Card>
  );
};

export default FfmpegSettingsComponent;
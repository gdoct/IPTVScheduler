import React from 'react';
import { Badge, Button, ButtonGroup, Table } from 'react-bootstrap';
import { ChannelInfo, obfuscateChannelUri, ScheduledRecording } from '../types/recordings';

interface RecordingsTableProps {
  recordings: ScheduledRecording[];
  channels: ChannelInfo[];
  onEdit: (id: string) => void;
  onEditTask: (id: string) => void;
  onDelete: (id: string) => void;
}

// Helper function to find channel info
const findChannel = (channelUri: string, channels: ChannelInfo[]): ChannelInfo => {
  return channels.find(c => c.uri === channelUri) || { name: '', uri: '', logo: '' };
};

const RecordingsTable: React.FC<RecordingsTableProps> = ({ 
  recordings, 
  channels, 
  onEdit, 
  onEditTask, 
  onDelete 
}) => {
  if (!recordings || recordings.length === 0) {
    return (
      <div className="card shadow-sm">
        <div className="card-body text-center py-5">
          <i className="bi bi-calendar-x text-muted" style={{ fontSize: '3rem' }}></i>
          <h4 className="mt-3">No recordings scheduled</h4>
          <p className="text-muted">Start by adding a new recording.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="card shadow-sm mb-4">
      <div className="card-header bg-primary text-white d-flex justify-content-between align-items-center">
        <h5 className="mb-0">
          <i className="bi bi-calendar-event me-2"></i>Upcoming Recordings
        </h5>
      </div>
      <div className="card-body p-0">
        <div className="table-responsive">
          <Table hover striped className="mb-0">
            <thead className="table-light">
              <tr>
                <th style={{ width: '60px' }}></th>
                <th>Time</th>
                <th>Channel</th>
                <th>Program</th>
                <th className="text-center" style={{ width: '100px' }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {recordings.map(item => {
                const chan = findChannel(item.channelUri, channels);
                return (
                  <tr key={item.id}>
                    <td className="align-middle text-center">
                      <img src={chan.logo} className="img-fluid rounded" 
                        style={{ maxHeight: '40px', maxWidth: '40px' }} alt="Channel logo" />
                    </td>
                    <td className="align-middle">
                      <div className="d-flex flex-column">
                        <span className="fw-bold">
                          <i className="bi bi-play-fill text-success"></i> {new Date(item.startTime).toLocaleString()}
                        </span>
                        <span className="text-muted small">
                          <i className="bi bi-stop-fill text-danger"></i> {new Date(item.endTime).toLocaleString()}
                        </span>
                      </div>
                    </td>
                    <td className="align-middle" title={item.channelName}>
                      <div className="fw-bold">{item.channelName}</div>
                      <Badge bg="light" text="dark" className="small">{obfuscateChannelUri(item.channelUri)}</Badge>
                    </td>
                    <td className="align-middle" title={item.filename}>
                      <div>{item.name}</div>
                      <small className="text-muted text-truncate d-inline-block" style={{ maxWidth: '250px' }}>
                        <i className="bi bi-file-earmark-play"></i> {item.filename.split('/').pop()}
                      </small>
                    </td>
                    <td className="align-middle text-center">
                      <ButtonGroup size="sm">
                        <Button variant="outline-primary" title="Edit" onClick={() => onEdit(item.id)}>
                          <i className="bi bi-pencil"></i>
                        </Button>
                        <Button variant="outline-secondary" title="Edit Code" onClick={() => onEditTask(item.id)}>
                          <i className="bi bi-braces"></i>
                        </Button>
                        <Button variant="outline-danger" title="Delete" onClick={() => onDelete(item.id)}>
                          <i className="bi bi-trash"></i>
                        </Button>
                      </ButtonGroup>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </Table>
        </div>
      </div>
    </div>
  );
};

export default RecordingsTable;
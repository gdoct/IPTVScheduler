import React, { useEffect, useRef, useState } from 'react';
import { Button, Modal } from 'react-bootstrap';
import { Light as SyntaxHighlighter } from 'react-syntax-highlighter';
import bash from 'react-syntax-highlighter/dist/esm/languages/hljs/bash';
import { docco } from 'react-syntax-highlighter/dist/esm/styles/hljs';
import { TaskDefinitionModel } from '../types/recordings';

// Register language for syntax highlighting
SyntaxHighlighter.registerLanguage('bash', bash);

interface TaskEditorProps {
  show: boolean;
  onHide: () => void;
  onSave: (id: string, content: string) => void;
  taskDefinition: TaskDefinitionModel | null;
}

const TaskEditor: React.FC<TaskEditorProps> = ({
  show,
  onHide,
  onSave,
  taskDefinition
}) => {
  const [content, setContent] = useState<string>('');
  const editorRef = useRef<HTMLPreElement>(null); // Updated from HTMLDivElement to HTMLPreElement
  const resizableRef = useRef<HTMLDivElement>(null);
  
  // Initialize editor with task definition
  useEffect(() => {
    if (taskDefinition) {
      setContent(taskDefinition.content);
    }
  }, [taskDefinition]);

  // Handle content change
  const handleContentChange = (e: React.FormEvent<HTMLPreElement>) => {
    setContent(e.currentTarget.textContent || '');
  };

  // Handle save button click
  const handleSave = () => {
    if (taskDefinition?.id) {
      onSave(taskDefinition.id, content);
    }
  };

  // Make modal resizable from bottom-right corner
  useEffect(() => {
    const resizableElement = resizableRef.current;
    if (!resizableElement || !show) return;

    let startX = 0, startY = 0, startWidth = 0, startHeight = 0;

    const initResize = (e: MouseEvent) => {
      e.preventDefault();
      startX = e.clientX;
      startY = e.clientY;

      if (resizableElement) {
        startWidth = resizableElement.offsetWidth;
        startHeight = resizableElement.offsetHeight;
      }

      document.addEventListener('mousemove', handleResize);
      document.addEventListener('mouseup', stopResize);
    };

    const handleResize = (e: MouseEvent) => {
      if (resizableElement) {
        const newWidth = startWidth + e.clientX - startX;
        const newHeight = startHeight + e.clientY - startY;
        resizableElement.style.width = `${Math.max(500, newWidth)}px`;
        resizableElement.style.height = `${Math.max(300, newHeight)}px`;
      }
    };

    const stopResize = () => {
      document.removeEventListener('mousemove', handleResize);
      document.removeEventListener('mouseup', stopResize);
    };

    // Create a resize handle
    const resizeHandle = document.createElement('div');
    resizeHandle.style.position = 'absolute';
    resizeHandle.style.right = '0';
    resizeHandle.style.bottom = '0';
    resizeHandle.style.width = '10px';
    resizeHandle.style.height = '10px';
    resizeHandle.style.cursor = 'nwse-resize';
    resizeHandle.style.zIndex = '1000';

    resizableElement.style.position = 'relative';
    resizableElement.appendChild(resizeHandle);

    resizeHandle.addEventListener('mousedown', initResize);

    return () => {
      if (resizableElement && resizableElement.contains(resizeHandle)) {
        resizableElement.removeChild(resizeHandle);
      }
      resizeHandle.removeEventListener('mousedown', initResize);
      document.removeEventListener('mousemove', handleResize);
      document.removeEventListener('mouseup', stopResize);
    };
  }, [show]);

  return (
    <Modal 
      show={show} 
      onHide={onHide} 
      size="lg" 
      backdrop="static"
      dialogClassName="task-editor-modal"
      ref={resizableRef}
    >
      <Modal.Header className="bg-secondary text-white">
        <Modal.Title>
          <i className="bi bi-braces me-2"></i>
          Edit Task Definition: {taskDefinition?.name || ''}
        </Modal.Title>
        <Button variant="close" onClick={onHide} className="btn-close-white" aria-label="Close" />
      </Modal.Header>
      <Modal.Body className="d-flex flex-column" style={{ overflow: 'auto' }}>
        <div className="mb-2 d-flex justify-content-between align-items-center">
          <label className="form-label fw-bold">Task Definition</label>
          <small className="text-muted">Edit the task definition below</small>
        </div>
        
        <div className="flex-grow-1 border rounded" style={{ position: 'relative', minHeight: '200px' }}>
          <pre 
            ref={editorRef}
            contentEditable={true}
            spellCheck={false}
            className="m-0 h-100 p-3"
            style={{ 
              fontSize: '0.9rem', 
              outline: 'none',
              overflowY: 'auto', 
              minHeight: '200px', 
              fontFamily: 'monospace' 
            }}
            onInput={handleContentChange}
            onKeyDown={(e) => {
              // Handle tab key for indentation
              if (e.key === 'Tab') {
                e.preventDefault();
                document.execCommand('insertText', false, '    ');
              }
            }}
          >
            {content}
          </pre>
          
          {/* Optional overlay for syntax highlighting */}
          <div style={{ 
            position: 'absolute', 
            top: 0, 
            left: 0, 
            right: 0, 
            bottom: 0, 
            pointerEvents: 'none', 
            opacity: 0 
          }}>
            <SyntaxHighlighter language="bash" style={docco} wrapLongLines={true}>
              {content}
            </SyntaxHighlighter>
          </div>
        </div>
      </Modal.Body>
      <div className="card-footer bg-light p-3 d-flex justify-content-between">
        <div>
          <small className="text-muted">Drag the bottom-right corner to resize</small>
        </div>
        <div>
          <Button variant="secondary" className="me-2" onClick={onHide}>
            <i className="bi bi-x-circle me-1"></i>Cancel
          </Button>
          <Button 
            variant="primary" 
            onClick={handleSave}
          >
            <i className="bi bi-save me-1"></i>Save
          </Button>
        </div>
      </div>
    </Modal>
  );
};

export default TaskEditor;
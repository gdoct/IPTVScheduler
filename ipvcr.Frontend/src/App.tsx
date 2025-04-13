import 'bootstrap-icons/font/bootstrap-icons.css';
import 'bootstrap/dist/css/bootstrap.min.css';
import React from 'react';
import { Link, Route, BrowserRouter as Router, Routes, useLocation } from 'react-router-dom';
import './App.css';
import RecordingsPage from './pages/RecordingsPage';
import SettingsPage from './pages/SettingsPage';

// Navigation component
const Navigation: React.FC = () => {
  const location = useLocation();
  
  return (
    <header className="bg-dark text-white py-2">
      <div className="container-fluid d-flex justify-content-between align-items-center">
      <Link 
            to="/" 
            className={`text-white me-3 ${location.pathname === '/' ? 'fw-bold' : ''}`}
            style={{ textDecoration: 'none' }}
          >
            <i className="bi bi-house-door me-1"></i>
            IPTV Scheduler
          </Link>
        <div className="d-flex">
          <Link 
            to="/settings" 
            className={`text-white ${location.pathname === '/settings' ? 'fw-bold' : ''}`}
            style={{ textDecoration: 'none' }}
          >
            <i className="bi bi-gear me-1"></i>
          </Link>
        </div>
      </div>
    </header>
  );
};

function App() {
  return (
    <Router>
      <div className="App">
        <Navigation />
        <main>
          <Routes>
            <Route path="/" element={<RecordingsPage />} />
            <Route path="/settings" element={<SettingsPage />} />
          </Routes>
        </main>
        <footer className="bg-light text-center text-muted py-3 mt-5">
          <div className="container-fluid">
            <p className="mb-0">IPTV Scheduler &copy; {new Date().getFullYear()}</p>
          </div>
        </footer>
      </div>
    </Router>
  );
}

export default App;

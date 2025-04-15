import 'bootstrap-icons/font/bootstrap-icons.css';
import 'bootstrap/dist/css/bootstrap.min.css';
import React, { useState } from 'react';
import { Link, Route, BrowserRouter as Router, Routes, useLocation, useNavigate } from 'react-router-dom';
import './App.css';
import RequireAuth from './components/RequireAuth';
import LoginPage from './pages/LoginPage';
import RecordingsPage from './pages/RecordingsPage';
import SettingsPage from './pages/SettingsPage';
import { AuthService } from './services/AuthService';

// Navigation component
const Navigation: React.FC = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const [isLoggingOut, setIsLoggingOut] = useState(false);
  
  const handleLogout = () => {
    if (isLoggingOut) return; // Prevent multiple logout calls

    setIsLoggingOut(true);
    AuthService.logout();
    navigate('/login', { replace: true });
  };
  
  return (
    <header className="bg-dark text-white py-2">
      <div className="container-fluid d-flex justify-content-between align-items-center">
      <Link 
            to="/" 
            className={`text-white me-3 ${location.pathname === '/' ? 'fw-bold' : ''}`}
            style={{ textDecoration: 'none' }}
          >
            <i className="bi bi-house-door me-1"></i>
            ipvcr
          </Link>
        <div className="d-flex">
          <Link 
              to="/settings" 
              className={`text-white ${location.pathname === '/settings' ? 'fw-bold' : ''}`}
              style={{ textDecoration: 'none' }}
            >
              <i className="bi bi-gear me-1"></i>
          </Link>
          &nbsp;
          <RequireAuth>
          <button 
            className="btn btn-link text-white p-0 ms-3" 
            style={{ textDecoration: 'none' }}
            onClick={handleLogout}
            disabled={isLoggingOut}
          >
            <i className="bi bi-box-arrow-right"></i>
          </button>
          </RequireAuth>
        </div>
      </div>
    </header>
  );
};

function App() {
  return (
    <Router>
      <AppContent />
    </Router>
  );
}

function AppContent() {
  const location = useLocation();

  return (
    <div className="App">
      {/* Render Navigation only if not on the login page */}
      {location.pathname !== '/login' && <Navigation />}
      <main>
        <Routes>
          <Route path="/" element={<RequireAuth><RecordingsPage /></RequireAuth>} />
          <Route path="/recordings" element={<RequireAuth><RecordingsPage /></RequireAuth>} />
          <Route path="/settings" element={<RequireAuth><SettingsPage /></RequireAuth>} />
          <Route path="/login" element={<LoginPage />} />
        </Routes>
      </main>
      <footer className="bg-light text-center text-muted py-3 mt-5">
        <div className="container-fluid">
          <p className="mb-0">ipvcr &copy; {new Date().getFullYear()}</p>
        </div>
      </footer>
    </div>
  );
}

export default App;

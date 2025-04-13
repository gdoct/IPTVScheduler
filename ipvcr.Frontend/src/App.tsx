import 'bootstrap/dist/css/bootstrap.min.css';
import './App.css';
import RecordingsPage from './pages/RecordingsPage';

function App() {
  return (
    <div className="App">
      <header className="App-header bg-dark text-white py-2 mb-4">
        <div className="container-fluid">
          <h1 className="h3">IPTV Scheduler</h1>
        </div>
      </header>
      <main>
        <RecordingsPage />
      </main>
      <footer className="bg-light text-center text-muted py-3 mt-5">
        <div className="container-fluid">
          <p className="mb-0">IPTV Scheduler &copy; {new Date().getFullYear()}</p>
        </div>
      </footer>
    </div>
  );
}

export default App;

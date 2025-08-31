// import reactLogo from './assets/react.svg'
// import viteLogo from '/vite.svg'
import './App.css'
import React from 'react';
import { HomePage } from './Home';
import { LoginPage } from './Login';
import { PacientesPage  } from "./Paciente"
import { AgendamentosPage } from "./Agendamento"
import { ProntuariosPage } from "./Prontuario"
// import './styles.css';

function App() {
  const [currentPage, setCurrentPage] = React.useState('home');

  const renderPage = () => {
    switch(currentPage) {
      case 'login':
        return <LoginPage />;
      case 'pacientes':
        return <PacientesPage />;
      case 'agendamentos':
        return <AgendamentosPage />;
      case 'prontuarios':
        return <ProntuariosPage />;
      default:
        return <HomePage />;
    }
  };

  return (
    <div className="min-h-screen bg-gray-50">      
      <header className="bg-blue-600 text-white shadow-md">
        <div className="max-w-7xl mx-auto px-4 py-4">
          <h1 className="text-2xl font-bold">SGHSS - Sistema de Gestão Hospitalar</h1>
        </div>
      </header>

      {/* Pequeno menu pra navegação */}
      <nav className="bg-blue-500 text-white">
        <div className="max-w-7xl mx-auto px-4">
          <div className="flex space-x-4 py-2">
            <button 
              onClick={() => setCurrentPage('home')}
              className={`px-3 py-2 rounded ${currentPage === 'home' ? 'bg-blue-700' : 'hover:bg-blue-400'}`}
            >
              Início
            </button>
            <button 
              onClick={() => setCurrentPage('pacientes')}
              className={`px-3 py-2 rounded ${currentPage === 'pacientes' ? 'bg-blue-700' : 'hover:bg-blue-400'}`}
            >
              Pacientes
            </button>
            <button 
              onClick={() => setCurrentPage('agendamentos')}
              className={`px-3 py-2 rounded ${currentPage === 'agendamentos' ? 'bg-blue-700' : 'hover:bg-blue-400'}`}
            >
              Agendamentos
            </button>
            <button 
              onClick={() => setCurrentPage('prontuarios')}
              className={`px-3 py-2 rounded ${currentPage === 'prontuarios' ? 'bg-blue-700' : 'hover:bg-blue-400'}`}
            >
              Prontuários
            </button>
            <button 
              onClick={() => setCurrentPage('login')}
              className={`px-3 py-2 rounded ml-auto ${currentPage === 'login' ? 'bg-blue-700' : 'hover:bg-blue-400'}`}
            >
              Login
            </button>
          </div>
        </div>
      </nav>

      {/* No geral, é aqui que as coisas vão ser renderizadas */}
      <main className="max-w-7xl mx-auto px-4 py-8">
        {renderPage()}
      </main>

      {/* Footer, só uma descrição básica. */}
      <footer className="bg-gray-800 text-white mt-auto">
        <div className="max-w-7xl mx-auto px-4 py-4 text-center">
          <p>&copy; 2025 SGHSS - Sistema de Gestão Hospitalar</p>
        </div>
      </footer>
    </div>
  );
}


export default App;

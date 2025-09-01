// import reactLogo from './assets/react.svg'
// import viteLogo from '/vite.svg'
import './App.css'
import React from 'react';
import { HomePage } from './Home';
import { LoginPage } from './Login';
import { PacientesPage  } from "./Paciente"
import { AgendamentosPage } from "./Agendamento"
import { ProntuariosPage } from "./Prontuario"
import { AuthProvider, useAuth } from './auth/Auth';
// import './styles.css';

export function AppContent() {
  const [currentPage, setCurrentPage] = React.useState('home');
  const { isAuthenticated, user, logout } = useAuth();

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

      {/* Menu para navegação */}
      <nav className="bg-blue-500 text-white">
        <div className="max-w-7xl mx-auto px-4">
          <div className="flex space-x-4 py-2">
            {isAuthenticated ? (
              <>
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
                <div className="ml-auto flex items-center space-x-4">
                  <span className="text-sm">Olá, {user?.nome || 'Usuário'}</span>
                  <button 
                    onClick={logout}
                    className="px-3 py-2 rounded hover:bg-blue-400"
                  >
                    Sair
                  </button>
                </div>
              </>
            ) : (
              <button 
                onClick={() => setCurrentPage('login')}
                className={`px-3 py-2 rounded ml-auto ${currentPage === 'login' ? 'bg-blue-700' : 'hover:bg-blue-400'}`}
              >
                Login
              </button>
            )}
          </div>
        </div>
      </nav>

      {/* Aqui é onde de fato as coisas vão aparecer */}
      <main className="max-w-7xl mx-auto px-4 py-8">
        {!isAuthenticated && currentPage !== 'login' ? (
          <LoginPage onLogin={() => setCurrentPage('home')} />
        ) : (
          renderPage()
        )}
      </main>

      {/* Footer */}
      <footer className="bg-gray-800 text-white mt-auto">
        <div className="max-w-7xl mx-auto px-4 py-4 text-center">
          <p>&copy; 2025 SGHSS - Sistema de Gestão Hospitalar</p>
        </div>
      </footer>
    </div>
  );
}

// Principal componente que envolve tudo.
// AuthProvider fornece o contexto de autenticação para toda a aplicação.
function App() {
  return (
    <AuthProvider>
      <AppContent />
    </AuthProvider>
  );
}

export default App;

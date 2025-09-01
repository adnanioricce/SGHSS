import React from "react";
import { useApi } from "./auth/Auth";
import { apiClient } from "./ApiClient";
export type Agendamento = {
    id: number;
    pacienteId: number;
    profissionalId: number;
    profissionalNome?: string;
    paciente: string;
    tipo: string;
    dataHora: Date;
    status: 'Confirmado' | 'Pendente' | 'Cancelado';
};
// Agendamentos Page Component with API integration
export function AgendamentosPage() {
    const initialData: Agendamento[] = [];
    const [agendamentos, setAgendamentos] = React.useState(initialData);
    const [stats, setStats] = React.useState({ hoje: 0, semana: 0, pendentes: 0 });
    const { call, loading, error } = useApi();
  
    // Load agendamentos on component mount
    React.useEffect(() => {
      const loadAgendamentos = async () => {
        try {
          const response = await call(() => apiClient.getAgendamentos(1, 50));
          const agendamentosData: Agendamento[] = response.data || response || [];
          setAgendamentos(agendamentosData);
          
          // Calculate stats
          const hoje = new Date().toDateString();
          const semanaAtual = new Date();
          const proximaSemana = new Date(semanaAtual.getTime() + 7 * 24 * 60 * 60 * 1000);
          
          const todayCount = agendamentosData.filter((a:Agendamento) => 
            new Date(a.dataHora).toDateString() === hoje
          ).length;
          
          const weekCount = agendamentosData.filter(a => {
            const agendDate = new Date(a.dataHora);
            return agendDate >= semanaAtual && agendDate <= proximaSemana;
          }).length;
          
          const pendingCount = agendamentosData.filter(a => 
            a.status === 'Cancelado' || a.status === 'Confirmado'
          ).length;
          
          setStats({ hoje: todayCount, semana: weekCount, pendentes: pendingCount });
        } catch (err) {
          console.error('Failed to load agendamentos:', err);
        }
      };
  
      loadAgendamentos();
    }, [call]);
  
    const formatDateTime = (dateTime: Date) => {
      return new Date(dateTime).toLocaleString('pt-BR');
    };
  
    const getStatusColor = (status: string) => {
      switch (status?.toLowerCase()) {
        case 'confirmado': return 'bg-green-100 text-green-800';
        case 'pendente': case 'agendado': return 'bg-yellow-100 text-yellow-800';
        case 'cancelado': return 'bg-red-100 text-red-800';
        case 'realizado': return 'bg-blue-100 text-blue-800';
        default: return 'bg-gray-100 text-gray-800';
      }
    };
  
    return (
      <div>
        <div className="flex justify-between items-center mb-6">
          <h2 className="text-2xl font-bold text-gray-800">Agendamentos</h2>
          <button className="bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700">
            Novo Agendamento
          </button>
        </div>
        
        {error && (
          <div className="mb-4 p-3 bg-red-100 border border-red-400 text-red-700 rounded">
            Erro ao carregar agendamentos: {error}
          </div>
        )}
        
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-6">
          <div className="bg-white p-4 rounded-lg shadow-md">
            <h3 className="text-lg font-semibold text-gray-800 mb-2">Hoje</h3>
            <p className="text-2xl font-bold text-blue-600">{loading ? '...' : stats.hoje}</p>
            <p className="text-sm text-gray-600">agendamentos</p>
          </div>
          <div className="bg-white p-4 rounded-lg shadow-md">
            <h3 className="text-lg font-semibold text-gray-800 mb-2">Esta Semana</h3>
            <p className="text-2xl font-bold text-green-600">{loading ? '...' : stats.semana}</p>
            <p className="text-sm text-gray-600">agendamentos</p>
          </div>
          <div className="bg-white p-4 rounded-lg shadow-md">
            <h3 className="text-lg font-semibold text-gray-800 mb-2">Pendentes</h3>
            <p className="text-2xl font-bold text-orange-600">{loading ? '...' : stats.pendentes}</p>
            <p className="text-sm text-gray-600">confirmações</p>
          </div>
        </div>
  
        <div className="bg-white rounded-lg shadow-md p-6">
          <h3 className="text-lg font-semibold mb-4">Próximos Agendamentos</h3>
          
          {loading ? (
            <div className="text-center py-8">
              <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
              <p className="mt-2 text-gray-600">Carregando agendamentos...</p>
            </div>
          ) : agendamentos.length === 0 ? (
            <p className="text-gray-600">Nenhum agendamento encontrado</p>
          ) : (
            <div className="space-y-4">
              {agendamentos.slice(0, 10).map((agendamento: Agendamento) => (
                <div key={agendamento.id} className="border-l-4 border-blue-500 pl-4 py-2">
                  <div className="flex justify-between items-start">
                    <div>
                      <p className="font-semibold">{agendamento.paciente || `Paciente ID: ${agendamento.pacienteId}`}</p>
                      <p className="text-sm text-gray-600">{agendamento.tipo || 'Consulta'}</p>
                      <p className="text-sm text-gray-500">{formatDateTime(agendamento.dataHora)}</p>
                      {agendamento.profissionalNome && (
                        <p className="text-sm text-gray-500">Dr(a). {agendamento.profissionalNome}</p>
                      )}
                    </div>
                    <div className="flex items-center space-x-2">
                      <span className={`px-2 py-1 rounded text-xs ${getStatusColor(agendamento.status)}`}>
                        {agendamento.status}
                      </span>
                      <button className="text-blue-600 hover:text-blue-800 text-sm">
                        Ver
                      </button>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    );
  }
import React from "react";
import { useApi } from "./auth/Auth";
import { apiClient } from "./ApiClient";
export type Prontuario = {
    id: number;
    pacienteId: number;
    profissionalId: number;
    profissionalNome?: string;
    dataConsulta: Date;
    queixaPrincipal: string;
    historicoDoencaAtual: string;
    antecedentesPessoais: string;
    antecedentesFamiliares: string;
    examesFisicos: string;
    prescricoes: string[];
    examesSolicitados: string[];
    procedimentos: string[];
    alergias: string[];
    medicamentosEmUso: string[];
    paciente: string;
    cpf: string;
    ultimaConsulta: string;
};
export function ProntuariosPage() {
    const [prontuarios, setProntuarios] = React.useState([]);
    const [searchTerm, setSearchTerm] = React.useState('');
    const { call, loading, error } = useApi();
      
    React.useEffect(() => {
      const loadProntuarios = async () => {
        try {
          const response = await call(() => apiClient.getProntuarios(1, 50));
          setProntuarios(response.data || response || []);
        } catch (err) {
          console.error('Failed to load prontuários:', err);
        }
      };
  
      loadProntuarios();
    }, [call]);
  
    // Filter prontuarios based on search
    const filteredProntuarios = prontuarios.filter((prontuario: Prontuario) =>
      prontuario.paciente?.toLowerCase().includes(searchTerm.toLowerCase()) ||
      prontuario.cpf?.includes(searchTerm)
    );
  
    const formatDate = (date: Date) => {
      return new Date(date).toLocaleDateString('pt-BR');
    };
  
    return (
      <div>
        <div className="flex justify-between items-center mb-6">
          <h2 className="text-2xl font-bold text-gray-800">Prontuários</h2>
          <button className="bg-purple-600 text-white px-4 py-2 rounded-md hover:bg-purple-700">
            Novo Prontuário
          </button>
        </div>
        
        {error && (
          <div className="mb-4 p-3 bg-red-100 border border-red-400 text-red-700 rounded">
            Erro ao carregar prontuários: {error}
          </div>
        )}
        
        <div className="bg-white rounded-lg shadow-md p-6">
          <div className="mb-4">
            <input
              type="text"
              placeholder="Buscar prontuário por paciente ou CPF..."
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:border-blue-500"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              disabled={loading}
            />
          </div>
          
          {loading ? (
            <div className="text-center py-8">
              <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-purple-600"></div>
              <p className="mt-2 text-gray-600">Carregando prontuários...</p>
            </div>
          ) : filteredProntuarios.length === 0 ? (
            <p className="text-gray-600 text-center py-8">
              {searchTerm ? 'Nenhum prontuário encontrado para a busca' : 'Nenhum prontuário encontrado'}
            </p>
          ) : (
            <div className="space-y-4">
              {filteredProntuarios.map((prontuario: Prontuario) => (
                <div key={prontuario.id} className="border rounded-lg p-4 hover:bg-gray-50">
                  <div className="flex justify-between items-start">
                    <div>
                      <h4 className="font-semibold text-lg">
                        {prontuario.paciente || `Paciente ID: ${prontuario.pacienteId}`}
                      </h4>
                      {prontuario.cpf && (
                        <p className="text-sm text-gray-600">CPF: {prontuario.cpf}</p>
                      )}
                      <p className="text-sm text-gray-500">
                        Data da consulta: {formatDate(prontuario.dataConsulta)}
                      </p>
                      <p className="text-sm text-gray-500">
                        Profissional: {prontuario.profissionalNome || `ID: ${prontuario.profissionalId}`}
                      </p>
                      {prontuario.queixaPrincipal && (
                        <p className="text-sm text-gray-600 mt-2">
                          <strong>Queixa:</strong> {prontuario.queixaPrincipal.substring(0, 100)}
                          {prontuario.queixaPrincipal.length > 100 ? '...' : ''}
                        </p>
                      )}
                    </div>
                    <div className="flex flex-col space-y-2">
                      <button className="text-purple-600 hover:text-purple-800 text-sm">
                        Ver Prontuário
                      </button>
                      <button className="text-blue-600 hover:text-blue-800 text-sm">
                        Nova Consulta
                      </button>
                      <button className="text-green-600 hover:text-green-800 text-sm">
                        Editar
                      </button>
                    </div>
                  </div>
                  
                  {/* Additional info */}
                  <div className="mt-3 pt-3 border-t border-gray-200">
                    <div className="flex flex-wrap gap-4 text-xs text-gray-500">
                      {prontuario.prescricoes && prontuario.prescricoes.length > 0 && (
                        <span className="bg-blue-100 text-blue-800 px-2 py-1 rounded">
                          {prontuario.prescricoes.length} prescrição(ões)
                        </span>
                      )}
                      {prontuario.examesSolicitados && prontuario.examesSolicitados.length > 0 && (
                        <span className="bg-green-100 text-green-800 px-2 py-1 rounded">
                          {prontuario.examesSolicitados.length} exame(s)
                        </span>
                      )}
                      {prontuario.procedimentos && prontuario.procedimentos.length > 0 && (
                        <span className="bg-yellow-100 text-yellow-800 px-2 py-1 rounded">
                          {prontuario.procedimentos.length} procedimento(s)
                        </span>
                      )}
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
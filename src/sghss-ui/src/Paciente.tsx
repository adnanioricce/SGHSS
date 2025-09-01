import React from "react";
import { useApi } from "./auth/Auth";
import { apiClient } from "./ApiClient";
export type Paciente = {
    id: number;
    nome: string;
    cpf: string;
    telefone: string;
};

export function PacientesPage() {
    const [searchTerm, setSearchTerm] = React.useState('');
    const [pacientes, setPacientes] = React.useState([]);
    const { call, loading, error } = useApi();

    // Load pacientes on component mount
    React.useEffect(() => {
        const loadPacientes = async () => {
        try {
            const response = await call(() => apiClient.getPacientes(1, 50, searchTerm));
            setPacientes(response.data || response || []);
        } catch (err) {
            console.error('Failed to load pacientes:', err);
        }
        };

        loadPacientes();
    }, [searchTerm, call]);

    const handleSearch = (term: string) => {
        setSearchTerm(term);
    };
  

    return (
        <div>
            <div className="flex justify-between items-center mb-6">
                <h2 className="text-2xl font-bold text-gray-800">Pacientes</h2>
                <button className="bg-green-600 text-white px-4 py-2 rounded-md hover:bg-green-700">
                Novo Paciente
                </button>
            </div>
            {error && (
                <div className="mb-4 p-3 bg-red-100 border border-red-400 text-red-700 rounded">
                Erro ao carregar pacientes: {error}
                </div>
            )}
            <div className="bg-white rounded-lg shadow-md p-6">
                <div className="mb-4">
                    <input
                        type="text"
                        placeholder="Buscar paciente..."
                        className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:border-blue-500"
                        value={searchTerm}
                        onChange={(e) => handleSearch(e.target.value)}
                        disabled={loading}
                    />
                </div>
                {loading ? (
                <div className="text-center py-8">
                    <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
                    <p className="mt-2 text-gray-600">Carregando pacientes...</p>
                </div>
                ) : (                
                    <div className="overflow-x-auto">
                        <table className="min-w-full table-auto">
                        <thead>
                            <tr className="bg-gray-50">
                            <th className="px-4 py-2 text-left text-sm font-semibold text-gray-600">Nome</th>
                            <th className="px-4 py-2 text-left text-sm font-semibold text-gray-600">CPF</th>
                            <th className="px-4 py-2 text-left text-sm font-semibold text-gray-600">Telefone</th>
                            <th className="px-4 py-2 text-left text-sm font-semibold text-gray-600">Ações</th>
                            </tr>
                        </thead>
                        <tbody>
                            {pacientes.length === 0 ? (
                            <tr className="border-t">
                                <td className="px-4 py-2 text-gray-800 text-center" colSpan={4}>
                                {searchTerm ? 'Nenhum paciente encontrado para a busca' : 'Nenhum paciente cadastrado'}
                                </td>
                            </tr>
                            ) : (
                            pacientes.map((paciente: Paciente) => (
                                <tr key={paciente.id} className="border-t hover:bg-gray-50">
                                <td className="px-4 py-2 text-gray-800">{paciente.nome}</td>
                                <td className="px-4 py-2 text-gray-800">{paciente.cpf}</td>
                                <td className="px-4 py-2 text-gray-800">{paciente.telefone}</td>
                                <td className="px-4 py-2">
                                    <button className="text-blue-600 hover:text-blue-800 mr-2">
                                    Ver
                                    </button>
                                    <button className="text-green-600 hover:text-green-800 mr-2">
                                    Editar
                                    </button>
                                </td>
                                </tr>
                            ))
                        )}
                        </tbody>
                    </table>
                </div>            
                )}
            </div>
        </div>        
    );
}
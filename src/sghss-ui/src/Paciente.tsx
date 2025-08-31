import React from "react";
export type Paciente = {
    id: number;
    nome: string;
    cpf: string;
    telefone: string;
};

export function PacientesPage() {
  const [searchTerm, setSearchTerm] = React.useState('');
  const [pacientes] = React.useState([]);

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold text-gray-800">Pacientes</h2>
        <button className="bg-green-600 text-white px-4 py-2 rounded-md hover:bg-green-700">
          Novo Paciente
        </button>
      </div>
      
      <div className="bg-white rounded-lg shadow-md p-6">
        <div className="mb-4">
          <input
            type="text"
            placeholder="Buscar paciente..."
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:border-blue-500"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
          />
        </div>
        
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
                    Nenhum paciente encontrado
                  </td>
                </tr>
              ) : (
                pacientes.map((paciente:Paciente, index) => (
                  <tr key={index} className="border-t hover:bg-gray-50">
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
      </div>
    </div>
  );
}
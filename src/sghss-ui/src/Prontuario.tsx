import React from "react";
export type Prontuario = {
    id: number;
    paciente: string;
    cpf: string;
    ultimaConsulta: string;
};
export function ProntuariosPage() {
  const [prontuarios] = React.useState([]);

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold text-gray-800">Prontuários</h2>
        <button className="bg-purple-600 text-white px-4 py-2 rounded-md hover:bg-purple-700">
          Novo Prontuário
        </button>
      </div>
      
      <div className="bg-white rounded-lg shadow-md p-6">
        <div className="mb-4">
          <input
            type="text"
            placeholder="Buscar prontuário por paciente..."
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:border-blue-500"
          />
        </div>
        
        {prontuarios.length === 0 ? (
          <p className="text-gray-600 text-center py-8">Nenhum prontuário encontrado</p>
        ) : (
          <div className="space-y-4">
            {prontuarios.map((prontuario: Prontuario, index) => (
              <div key={index} className="border rounded-lg p-4 hover:bg-gray-50">
                <div className="flex justify-between items-start">
                  <div>
                    <h4 className="font-semibold text-lg">{prontuario.paciente}</h4>
                    <p className="text-sm text-gray-600">CPF: {prontuario.cpf}</p>
                    <p className="text-sm text-gray-500">Última consulta: {prontuario.ultimaConsulta}</p>
                  </div>
                  <div className="space-x-2">
                    <button className="text-purple-600 hover:text-purple-800">
                      Ver Prontuário
                    </button>
                    <button className="text-blue-600 hover:text-blue-800">
                      Nova Consulta
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
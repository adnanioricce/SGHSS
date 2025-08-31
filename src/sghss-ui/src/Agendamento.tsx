import React from "react";
export type Agendamento = {
    id: number;
    paciente: string;
    tipo: string;
    dataHora: string;
    status: 'Confirmado' | 'Pendente' | 'Cancelado';
};
export function AgendamentosPage() {
  const [agendamentos] = React.useState([]);

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold text-gray-800">Agendamentos</h2>
        <button className="bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700">
          Novo Agendamento
        </button>
      </div>
      
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-6">
        <div className="bg-white p-4 rounded-lg shadow-md">
          <h3 className="text-lg font-semibold text-gray-800 mb-2">Hoje</h3>
          <p className="text-2xl font-bold text-blue-600">0</p>
          <p className="text-sm text-gray-600">agendamentos</p>
        </div>
        <div className="bg-white p-4 rounded-lg shadow-md">
          <h3 className="text-lg font-semibold text-gray-800 mb-2">Esta Semana</h3>
          <p className="text-2xl font-bold text-green-600">0</p>
          <p className="text-sm text-gray-600">agendamentos</p>
        </div>
        <div className="bg-white p-4 rounded-lg shadow-md">
          <h3 className="text-lg font-semibold text-gray-800 mb-2">Pendentes</h3>
          <p className="text-2xl font-bold text-orange-600">0</p>
          <p className="text-sm text-gray-600">confirmações</p>
        </div>
      </div>

      <div className="bg-white rounded-lg shadow-md p-6">
        <h3 className="text-lg font-semibold mb-4">Próximos Agendamentos</h3>
        {agendamentos.length === 0 ? (
          <p className="text-gray-600">Nenhum agendamento encontrado</p>
        ) : (
          <div className="space-y-4">
            {agendamentos.map((agendamento:Agendamento, index) => (
              <div key={index} className="border-l-4 border-blue-500 pl-4 py-2">
                <div className="flex justify-between items-start">
                  <div>
                    <p className="font-semibold">{agendamento.paciente}</p>
                    <p className="text-sm text-gray-600">{agendamento.tipo}</p>
                    <p className="text-sm text-gray-500">{agendamento.dataHora}</p>
                  </div>
                  <span className={`px-2 py-1 rounded text-xs ${
                    agendamento.status === 'Confirmado' ? 'bg-green-100 text-green-800' :
                    agendamento.status === 'Pendente' ? 'bg-yellow-100 text-yellow-800' :
                    'bg-gray-100 text-gray-800'
                  }`}>
                    {agendamento.status}
                  </span>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
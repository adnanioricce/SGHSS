export function HomePage() {
  return (
    <div className="text-center">
      <h2 className="text-3xl font-bold mb-6 text-gray-800">
        Bem-vindo ao SGHSS
      </h2>
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <div className="bg-white p-6 rounded-lg shadow-md">
          <h3 className="text-xl font-semibold mb-4 text-blue-600">Pacientes</h3>
          <p className="text-gray-600">Gerencie informações dos pacientes</p>
        </div>
        <div className="bg-white p-6 rounded-lg shadow-md">
          <h3 className="text-xl font-semibold mb-4 text-green-600">Agendamentos</h3>
          <p className="text-gray-600">Controle de consultas e procedimentos</p>
        </div>
        <div className="bg-white p-6 rounded-lg shadow-md">
          <h3 className="text-xl font-semibold mb-4 text-purple-600">Prontuários</h3>
          <p className="text-gray-600">Histórico médico completo</p>
        </div>
        <div className="bg-white p-6 rounded-lg shadow-md">
          <h3 className="text-xl font-semibold mb-4 text-orange-600">Telemedicina</h3>
          <p className="text-gray-600">Consultas remotas</p>
        </div>
      </div>
    </div>
  );
}
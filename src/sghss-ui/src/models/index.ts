export type User = {
    id: number;
    nome: string;
    email: string;
    role: 'Administrador' | 'Medico' | 'Enfermeiro' | 'Recepcionista' | 'Paciente' | 'Auditoria' | 'Farmacia' | 'Laboratorio';
}
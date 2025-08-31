const API_BASE_URL = 'http://localhost:58078';

export class ApiClient {
    baseURL: string;
    token: string | null;
    refreshToken: string | null;
    constructor() {
        this.baseURL = API_BASE_URL;
        this.token = localStorage.getItem('authToken');
        this.refreshToken = localStorage.getItem('refreshToken');
    }
    
    setTokens(token: string, refreshToken: string) {
        this.token = token;
        this.refreshToken = refreshToken;
        localStorage.setItem('authToken', token);
        localStorage.setItem('refreshToken', refreshToken);
    }
    
    clearTokens() {
        this.token = null;
        this.refreshToken = null;
        localStorage.removeItem('authToken');
        localStorage.removeItem('refreshToken');
    }
    
    async request(endpoint: string, options: any = {}) {
        const url = `${this.baseURL}${endpoint}`;
        const config = {
        headers: {
            'Content-Type': 'application/json',
            ...options.headers,
        },
        ...options,
        };
        
        if (this.token) {
        config.headers.Authorization = `Bearer ${this.token}`;
        }

        try {
        let response = await fetch(url, config);

        if (response.status === 401 && this.refreshToken) {
            const refreshed = await this.refreshAuthToken();
            if (refreshed) {          
            config.headers.Authorization = `Bearer ${this.token}`;
            response = await fetch(url, config);
            }
        }
        
        if (response.status === 204) {
            return null;
        }

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.error || `HTTP ${response.status}`);
        }

        return data;
        } catch (error) {
        console.error('API Request failed:', error);
        throw error;
        }
    }
    
    async login(email: string, password: string) {
        const response = await this.request('/login', {
        method: 'POST',
        body: JSON.stringify({ email, password }),
        });

        if (response.token && response.refreshToken) {
        this.setTokens(response.token, response.refreshToken);
        }

        return response;
    }

    async refreshAuthToken() {
        try {
        const response = await this.request('/refresh', {
            method: 'POST',
            body: JSON.stringify({ refreshToken: this.refreshToken }),
        });

        if (response.token) {
            this.setTokens(response.token, response.refreshToken || this.refreshToken);
            return true;
        }
        } catch (error) {
        console.error('Token refresh failed:', error);
        this.clearTokens();
        }
        return false;
    }

    async logout() {
        try {
        await this.request('/logout', { method: 'POST' });
        } finally {
        this.clearTokens();
        }
    }

    async getProfile() {
        return await this.request('/profile');
    }

    async changePassword(currentPassword: string, newPassword: string) {
        return await this.request('/change-password', {
        method: 'POST',
        body: JSON.stringify({ currentPassword, newPassword }),
        });
    }

    
    async getPacientes(page = 1, limit = 10, search = '') {
        const rec: Record<string,string> = { page: page.toString(), limit: limit.toString(), search }
        const params = new URLSearchParams(rec);
        return await this.request(`/pacientes?${params}`);
    }

    async getPacienteById(id: number) {
        return await this.request(`/pacientes/${id}`);
    }

    async createPaciente(pacienteData: any) {
        return await this.request('/pacientes', {
        method: 'POST',
        body: JSON.stringify(pacienteData),
        });
    }

    async updatePaciente(id: number, pacienteData: any) {
        return await this.request(`/pacientes/${id}`, {
        method: 'PUT',
        body: JSON.stringify(pacienteData),
        });
    }

    async deletePaciente(id: number) {
        return await this.request(`/pacientes/${id}`, {
        method: 'DELETE',
        });
    }

    
    async getAgendamentos(page = 1, limit = 10) {
        const rec:Record<string,string> = { page: page.toString(), limit: limit.toString() }
        const params = new URLSearchParams(rec);
        return await this.request(`/agendamentos?${params}`);
    }

    async getAgendamentoById(id: number) {
        return await this.request(`/agendamentos/${id}`);
    }

    async getAgendamentoDetalhes(id: number) {
        return await this.request(`/agendamentos/${id}/detalhes`);
    }

    async getAgendamentosPorPaciente(pacienteId: number) {
        return await this.request(`/agendamentos/paciente/${pacienteId}`);
    }

    async getAgendamentosPorProfissional(profissionalId: number) {
        return await this.request(`/agendamentos/profissional/${profissionalId}`);
    }

    async createAgendamento(agendamentoData: any) {
        return await this.request('/agendamentos', {
        method: 'POST',
        body: JSON.stringify(agendamentoData),
        });
    }

    async updateAgendamento(id: number, agendamentoData: any) {
        return await this.request(`/agendamentos/${id}`, {
        method: 'PUT',
        body: JSON.stringify(agendamentoData),
        });
    }

    async updateStatusAgendamento(id: number, status: string) {
        return await this.request(`/agendamentos/${id}/status`, {
        method: 'PUT',
        body: JSON.stringify({ status }),
        });
    }

    async confirmarAgendamento(id: number) {
        return await this.request(`/agendamentos/${id}/confirmar`, {
        method: 'POST',
        });
    }

    async marcarComoRealizado(id: number) {
        return await this.request(`/agendamentos/${id}/realizar`, {
        method: 'POST',
        });
    }

    async cancelarAgendamento(id: number) {
        return await this.request(`/agendamentos/${id}`, {
        method: 'DELETE',
        });
    }

    async verificarDisponibilidade(profissionalId: number, dataHora: Date, duracao: number) {
        const strParams: Record<string,string> = { dataHora: dataHora.toString(), duracao: duracao.toString()}
        const params = new URLSearchParams(strParams);
        return await this.request(`/agendamentos/profissional/${profissionalId}/disponibilidade?${params}`);
    }

    
    async getProntuarios(page = 1, limit = 10, pacienteId = null) {
        const strParams: Record<string,string> = { page: page.toString(), limit: limit.toString()}
        const params = new URLSearchParams(strParams);
        if (pacienteId) params.append('pacienteId', pacienteId);
        return await this.request(`/prontuarios?${params}`);
    }

    async getProntuarioById(id: number) {
        return await this.request(`/prontuarios/${id}`);
    }

    async getProntuariosPorPaciente(pacienteId: number) {
        return await this.request(`/prontuarios/paciente/${pacienteId}`);
    }

    async createProntuario(prontuarioData: any) {
        return await this.request('/prontuarios', {
        method: 'POST',
        body: JSON.stringify(prontuarioData),
        });
    }

    async updateProntuario(id: number, prontuarioData: any) {
        return await this.request(`/prontuarios/${id}`, {
        method: 'PUT',
        body: JSON.stringify(prontuarioData),
        });
    }

    async adicionarPrescricao(prontuarioId: number, prescricaoData: any) {
        return await this.request(`/prontuarios/${prontuarioId}/prescricoes`, {
        method: 'POST',
        body: JSON.stringify(prescricaoData),
        });
    }

    async adicionarExameSolicitado(prontuarioId: number, exameData: any) {
        return await this.request(`/prontuarios/${prontuarioId}/exames`, {
        method: 'POST',
        body: JSON.stringify(exameData),
        });
    }

    async adicionarResultadoExame(exameId: number, resultado: any, arquivoUrl = null) {
        return await this.request(`/prontuarios/exames/${exameId}/resultado`, {
        method: 'POST',
        body: JSON.stringify({ resultado, arquivoUrl }),
        });
    }

    async adicionarProcedimento(prontuarioId: number, procedimentoData: any) {
        return await this.request(`/prontuarios/${prontuarioId}/procedimentos`, {
        method: 'POST',
        body: JSON.stringify(procedimentoData),
        });
    }
    
    async getProfissionais() {
        return await this.request('/profissionais');
    }

    async getProfissionalById(id: number) {
        return await this.request(`/profissionais/${id}`);
    }
}

export const apiClient = new ApiClient();
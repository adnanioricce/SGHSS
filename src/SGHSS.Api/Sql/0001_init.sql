-- =====================================================
-- SGHSS (Sistema de Gestão Hospitalar e de Serviços de Saúde)
-- Complete Database Schema - PostgreSQL
-- =====================================================

-- Enable UUID extension for better ID generation (optional)
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- =====================================================
-- 1. CORE TABLES - Foundation
-- =====================================================

-- Unidades (Hospital units - hospitals, clinics, labs, home care)
CREATE TABLE unidades (
                          id SERIAL PRIMARY KEY,
                          nome VARCHAR(200) NOT NULL,
                          cnpj VARCHAR(14) UNIQUE NOT NULL,
                          tipo_unidade VARCHAR(20) NOT NULL CHECK (tipo_unidade IN ('HOSPITAL', 'CLINICA', 'LABORATORIO', 'HOMECARE', 'UPA', 'POSTOSAUDE')),
                          endereco TEXT NOT NULL,
                          telefone VARCHAR(20),
                          email VARCHAR(100),
                          responsavel VARCHAR(200),
                          capacidade_leitos INTEGER DEFAULT 0,
                          ativa BOOLEAN DEFAULT true,
                          data_cadastro TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                          data_atualizacao TIMESTAMP
);

-- Endereços (Addresses for patients and professionals)
CREATE TABLE enderecos (
                           id SERIAL PRIMARY KEY,
                           logradouro VARCHAR(200) NOT NULL,
                           numero VARCHAR(20) NOT NULL,
                           complemento VARCHAR(100),
                           bairro VARCHAR(100) NOT NULL,
                           cidade VARCHAR(100) NOT NULL,
                           estado VARCHAR(2) NOT NULL,
                           cep VARCHAR(8) NOT NULL,
                           pais VARCHAR(50) DEFAULT 'Brasil'
);

-- =====================================================
-- 2. PEOPLE MANAGEMENT - Patients and Professionals
-- =====================================================

-- Pacientes (Patients)
CREATE TABLE pacientes (
                           id SERIAL PRIMARY KEY,
                           nome VARCHAR(200) NOT NULL,
                           cpf VARCHAR(11) UNIQUE NOT NULL,
                           rg VARCHAR(20),
                           data_nascimento DATE NOT NULL,
                           sexo CHAR(1) NOT NULL CHECK (sexo IN ('M', 'F', 'O')),
                           estado_civil VARCHAR(20),
                           profissao VARCHAR(100),
                           email VARCHAR(100),
                           telefone VARCHAR(20) NOT NULL,
                           telefone_secundario VARCHAR(20),
                           endereco_id INTEGER REFERENCES enderecos(id),
                           plano_saude VARCHAR(100),
                           numero_carteirinha VARCHAR(50),
                           observacoes TEXT,
                           data_cadastro TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                           data_atualizacao TIMESTAMP,
                           ativo BOOLEAN DEFAULT true
);

-- Contatos de Emergência
CREATE TABLE contatos_emergencia (
                                     id SERIAL PRIMARY KEY,
                                     paciente_id INTEGER REFERENCES pacientes(id) NOT NULL,
                                     nome VARCHAR(200) NOT NULL,
                                     parentesco VARCHAR(50),
                                     telefone VARCHAR(20) NOT NULL,
                                     email VARCHAR(100)
);

-- Especialidades médicas
CREATE TABLE especialidades (
                                id SERIAL PRIMARY KEY,
                                nome VARCHAR(100) NOT NULL,
                                codigo VARCHAR(10) UNIQUE NOT NULL,
                                conselho_regulamentador VARCHAR(50)
);

-- Profissionais (Healthcare professionals)
CREATE TABLE profissionais (
                               id SERIAL PRIMARY KEY,
                               nome VARCHAR(200) NOT NULL,
                               cpf VARCHAR(11) UNIQUE NOT NULL,
                               crm VARCHAR(20),
                               tipo_profissional VARCHAR(50) NOT NULL CHECK (tipo_profissional IN ('MEDICO', 'ENFERMEIRO', 'TECNICO', 'FISIOTERAPEUTA', 'PSICOLOGO', 'NUTRICIONISTA', 'FARMACEUTICO', 'ADMINISTRATIVO')),
                               email VARCHAR(100) UNIQUE NOT NULL,
                               telefone VARCHAR(20),
                               data_admissao DATE NOT NULL,
                               data_demissao DATE,
                               ativo BOOLEAN DEFAULT true,
                               unidade_id INTEGER REFERENCES unidades(id),
                               permite_telemedicina BOOLEAN DEFAULT false,
                               data_cadastro TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                               data_atualizacao TIMESTAMP
);

-- Relacionamento Profissional-Especialidade (many-to-many)
CREATE TABLE profissionais_especialidades (
                                              id SERIAL PRIMARY KEY,
                                              profissional_id INTEGER REFERENCES profissionais(id) NOT NULL,
                                              especialidade_id INTEGER REFERENCES especialidades(id) NOT NULL,
                                              principal BOOLEAN DEFAULT false,
                                              data_certificacao DATE,
                                              UNIQUE(profissional_id, especialidade_id)
);

-- Horários de Atendimento
CREATE TABLE horarios_atendimento (
                                      id SERIAL PRIMARY KEY,
                                      profissional_id INTEGER REFERENCES profissionais(id) NOT NULL,
                                      dia_semana INTEGER NOT NULL CHECK (dia_semana BETWEEN 0 AND 6), -- 0=domingo, 6=sábado
                                      hora_inicio TIME NOT NULL,
                                      hora_fim TIME NOT NULL,
                                      ativo BOOLEAN DEFAULT true
);

-- =====================================================
-- 3. INFRASTRUCTURE - Rooms and Equipment
-- =====================================================

-- Salas (Rooms for consultations, exams, surgeries)
CREATE TABLE salas (
                       id SERIAL PRIMARY KEY,
                       nome VARCHAR(100) NOT NULL,
                       unidade_id INTEGER REFERENCES unidades(id) NOT NULL,
                       capacidade INTEGER DEFAULT 1,
                       tipo_sala VARCHAR(50) CHECK (tipo_sala IN ('CONSULTORIO', 'EXAME', 'CIRURGIA', 'EMERGENCIA', 'ADMINISTRATIVA')),
                       equipamentos TEXT,
                       ativa BOOLEAN DEFAULT true,
                       data_cadastro TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Leitos (Hospital beds)
CREATE TABLE leitos (
                        id SERIAL PRIMARY KEY,
                        unidade_id INTEGER REFERENCES unidades(id) NOT NULL,
                        numero VARCHAR(20) NOT NULL,
                        setor VARCHAR(100) NOT NULL,
                        tipo_leito VARCHAR(20) NOT NULL CHECK (tipo_leito IN ('UTI', 'SEMIUTI', 'ENFERMARIA', 'PARTICULAR', 'ISOLAMENTO', 'PEDIATRIA', 'MATERNIDADE')),
                        status VARCHAR(20) NOT NULL DEFAULT 'LIVRE' CHECK (status IN ('LIVRE', 'OCUPADO', 'LIMPEZA', 'MANUTENCAO', 'BLOQUEADO')),
                        paciente_id INTEGER REFERENCES pacientes(id),
                        data_ocupacao TIMESTAMP,
                        data_liberacao TIMESTAMP,
                        observacoes_status TEXT,
                        valor_diaria DECIMAL(10,2),
                        equipamentos TEXT, -- Separated by semicolon
                        capacidade_acompanhantes INTEGER DEFAULT 0,
                        data_cadastro TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        data_atualizacao TIMESTAMP,
                        UNIQUE(unidade_id, numero)
);

-- =====================================================
-- 4. SCHEDULING SYSTEM - Appointments
-- =====================================================

-- Agendamentos (Appointments)
CREATE TABLE agendamentos (
                              id SERIAL PRIMARY KEY,
                              paciente_id INTEGER REFERENCES pacientes(id) NOT NULL,
                              profissional_id INTEGER REFERENCES profissionais(id) NOT NULL,
                              tipo_agendamento VARCHAR(20) NOT NULL CHECK (tipo_agendamento IN ('CONSULTA', 'EXAME', 'CIRURGIA', 'TELECONSULTA', 'RETORNO', 'PROCEDIMENTO')),
                              data_hora TIMESTAMP NOT NULL,
                              duracao INTERVAL NOT NULL DEFAULT '30 minutes',
                              status VARCHAR(20) NOT NULL DEFAULT 'AGENDADO' CHECK (status IN ('AGENDADO', 'CONFIRMADO', 'CANCELADO', 'REALIZADO', 'FALTOU', 'REAGENDADO')),
                              observacoes TEXT,
                              unidade_id INTEGER REFERENCES unidades(id) NOT NULL,
                              sala_id INTEGER REFERENCES salas(id),
                              valor_consulta DECIMAL(10,2),
                              plano_saude_cobertura BOOLEAN DEFAULT false,
                              data_cadastro TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                              data_atualizacao TIMESTAMP,
                              cancelado_por INTEGER REFERENCES profissionais(id),
                              motivo_cancel TEXT
);

-- =====================================================
-- 5. MEDICAL RECORDS SYSTEM - Prontuários
-- =====================================================

-- Prontuários (Medical records)
CREATE TABLE prontuarios (
                             id SERIAL PRIMARY KEY,
                             paciente_id INTEGER REFERENCES pacientes(id) NOT NULL,
                             profissional_id INTEGER REFERENCES profissionais(id) NOT NULL,
                             data_atendimento TIMESTAMP NOT NULL,
                             tipo_atendimento VARCHAR(20) NOT NULL CHECK (tipo_atendimento IN ('CONSULTA', 'EXAME', 'INTERNACAO', 'EMERGENCIA', 'TELECONSULTA')),
                             queixa_principal TEXT NOT NULL,
                             historia_doenca_atual TEXT NOT NULL,
                             exame_fisico TEXT,
                             hipoteses TEXT, -- Separated by semicolon                             
                             cid10 VARCHAR(10),
                             observacoes TEXT,
                             plano_tratamento TEXT,
                             seguimento TEXT,
                             unidade_id INTEGER REFERENCES unidades(id) NOT NULL,
                             assinado BOOLEAN DEFAULT false,
                             assinado_em TIMESTAMP,
                             agendamento_id INTEGER REFERENCES agendamentos(id),
                             data_cadastro TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                             data_atualizacao TIMESTAMP
);

-- Prescrições (Prescriptions)
CREATE TABLE prescricoes (
                             id SERIAL PRIMARY KEY,
                             prontuario_id INTEGER REFERENCES prontuarios(id) NOT NULL,
                             medicamento VARCHAR(200) NOT NULL,
                             dosagem VARCHAR(100) NOT NULL,
                             frequencia VARCHAR(100) NOT NULL,
                             duracao VARCHAR(100) NOT NULL,
                             orientacoes TEXT,
                             ativo BOOLEAN DEFAULT true,
                             data_criacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                             data_vencimento TIMESTAMP
);

-- Exames Solicitados
CREATE TABLE exames_solicitados (
                                    id SERIAL PRIMARY KEY,
                                    prontuario_id INTEGER REFERENCES prontuarios(id) NOT NULL,
                                    tipo_exame VARCHAR(100) NOT NULL,
                                    descricao TEXT NOT NULL,
                                    urgente BOOLEAN DEFAULT false,
                                    observacoes TEXT,
                                    data_solicitacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                    realizado BOOLEAN DEFAULT false,
                                    data_realizacao TIMESTAMP,
                                    resultado TEXT,
                                    arquivo_resultado VARCHAR(500), -- URL to file
                                    laboratorio_id INTEGER REFERENCES unidades(id)
);

-- Procedimentos
CREATE TABLE procedimentos (
                               id SERIAL PRIMARY KEY,
                               prontuario_id INTEGER REFERENCES prontuarios(id) NOT NULL,
                               nome VARCHAR(200) NOT NULL,
                               codigo VARCHAR(20), -- TUSS code or similar
                               descricao TEXT NOT NULL,
                               data_realizacao TIMESTAMP NOT NULL,
                               profissional_id INTEGER REFERENCES profissionais(id) NOT NULL,
                               observacoes TEXT,
                               valor DECIMAL(10,2),
                               status VARCHAR(20) DEFAULT 'SOLICITADO' CHECK (status IN ('SOLICITADO', 'AUTORIZADO', 'REALIZADO', 'CANCELADO'))
);

-- =====================================================
-- 6. TELEMEDICINE SYSTEM
-- =====================================================

-- Configurações de Telemedicina por Profissional
CREATE TABLE configuracoes_telemedicina (
                                            id SERIAL PRIMARY KEY,
                                            profissional_id INTEGER REFERENCES profissionais(id) NOT NULL UNIQUE,
                                            plataforma_preferida VARCHAR(50) NOT NULL DEFAULT 'JITSI',
                                            permite_gravacao BOOLEAN DEFAULT false,
                                            duracao_maxima_sessao INTERVAL DEFAULT '60 minutes',
                                            permite_sala_espera BOOLEAN DEFAULT true,
                                            notificacoes_email BOOLEAN DEFAULT true,
                                            notificacoes_sms BOOLEAN DEFAULT false,
                                            horario_atendimento_inicio TIME DEFAULT '08:00',
                                            horario_atendimento_fim TIME DEFAULT '18:00',
                                            dias_atendimento VARCHAR(20) DEFAULT '1,2,3,4,5', -- 1=monday, 7=sunday
                                            ativo BOOLEAN DEFAULT true,
                                            data_criacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                            data_atualizacao TIMESTAMP
);

-- Sessões de Telemedicina
CREATE TABLE sessoes_telemedicina (
                                      id SERIAL PRIMARY KEY,
                                      agendamento_id INTEGER REFERENCES agendamentos(id) NOT NULL,
                                      paciente_id INTEGER REFERENCES pacientes(id) NOT NULL,
                                      profissional_id INTEGER REFERENCES profissionais(id) NOT NULL,
                                      link_sessao VARCHAR(500) NOT NULL,
                                      token_acesso VARCHAR(50) NOT NULL,
                                      senha_paciente VARCHAR(10),
                                      data_inicio TIMESTAMP,
                                      data_fim TIMESTAMP,
                                      status VARCHAR(20) NOT NULL DEFAULT 'AGENDADA' CHECK (status IN ('AGENDADA', 'INICIADA', 'EMANDAMENTO', 'FINALIZADA', 'CANCELADA', 'FALHA')),
                                      gravacao_url VARCHAR(500),
                                      gravacao_permitida BOOLEAN DEFAULT false,
                                      observacoes_finais TEXT,
                                      prontuario_id INTEGER REFERENCES prontuarios(id),
                                      qualidade_conexao VARCHAR(20), -- EXCELENTE, BOA, REGULAR, RUIM
                                      plataforma_video VARCHAR(50) NOT NULL DEFAULT 'JITSI',
                                      data_criacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                      data_atualizacao TIMESTAMP,
                                      cancelado_por INTEGER REFERENCES profissionais(id),
                                      motivo_cancel TEXT
);

-- Participantes das Sessões
CREATE TABLE participantes_sessao (
                                      id SERIAL PRIMARY KEY,
                                      sessao_id INTEGER REFERENCES sessoes_telemedicina(id) NOT NULL,
                                      usuario_id INTEGER NOT NULL,
                                      tipo_participante VARCHAR(20) NOT NULL CHECK (tipo_participante IN ('PACIENTE', 'MEDICO', 'OBSERVADOR', 'ACOMPANHANTE')),
                                      nome VARCHAR(200) NOT NULL,
                                      email VARCHAR(100),
                                      data_entrada TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                      data_saida TIMESTAMP,
                                      tempo_conectado INTERVAL,
                                      ativo BOOLEAN DEFAULT true,
                                      dispositivo_utilizado VARCHAR(100),
                                      ip_address INET
);

-- Histórico de Eventos das Sessões
CREATE TABLE historico_sessoes (
                                   id SERIAL PRIMARY KEY,
                                   sessao_id INTEGER REFERENCES sessoes_telemedicina(id) NOT NULL,
                                   evento VARCHAR(50) NOT NULL,
                                   descricao TEXT NOT NULL,
                                   usuario_id INTEGER,
                                   timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                   dados_adicionais JSONB
);

-- =====================================================
-- 7. ADMINISTRATION SYSTEM - Admissions and Supplies
-- =====================================================

-- Internações (Hospital admissions)
CREATE TABLE internacoes (
                             id SERIAL PRIMARY KEY,
                             paciente_id INTEGER REFERENCES pacientes(id) NOT NULL,
                             leito_id INTEGER REFERENCES leitos(id) NOT NULL,
                             medico_responsavel_id INTEGER REFERENCES profissionais(id) NOT NULL,
                             data_internacao TIMESTAMP NOT NULL,
                             data_alta_prevista TIMESTAMP,
                             data_alta TIMESTAMP,
                             tipo_internacao VARCHAR(20) NOT NULL CHECK (tipo_internacao IN ('ELETIVA', 'EMERGENCIA', 'URGENCIA', 'TRANSFERENCIA', 'CIRURGICA')),
                             motivo_internacao TEXT NOT NULL,
                             diagnostico TEXT,
                             cid10_principal VARCHAR(10),
                             cid10_secundarios TEXT, -- Separated by semicolon
                             status VARCHAR(20) NOT NULL DEFAULT 'INTERNADO' CHECK (status IN ('INTERNADO', 'ALTAMEDICA', 'ALTAHOSPITALAR', 'TRANSFERIDO', 'OBITO', 'ALTAADMINISTRATIVA')),
                             observacoes_alta TEXT,
                             valor_total DECIMAL(12,2),
                             plano_saude_cobertura BOOLEAN DEFAULT false,
                             numero_guia VARCHAR(50),
                             unidade_id INTEGER REFERENCES unidades(id) NOT NULL,
                             data_cadastro TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                             data_atualizacao TIMESTAMP
);
-- Suprimentos (Medical supplies and inventory)
CREATE TABLE suprimentos (
                             id SERIAL PRIMARY KEY,
                             nome VARCHAR(200) NOT NULL,
                             categoria VARCHAR(20) NOT NULL CHECK (categoria IN ('MEDICAMENTO', 'MATERIALMEDICO', 'EQUIPAMENTO', 'LIMPEZA', 'ALIMENTACAO', 'ADMINISTRATIVO')),
                             codigo VARCHAR(50) UNIQUE,
                             descricao TEXT NOT NULL,
                             unidade_medida VARCHAR(10) NOT NULL,
                             quantidade_estoque DECIMAL(12,3) DEFAULT 0,
                             quantidade_minima DECIMAL(12,3) NOT NULL,
                             quantidade_maxima DECIMAL(12,3) NOT NULL,
                             valor_unitario DECIMAL(10,2) NOT NULL,
                             fornecedor VARCHAR(200),
                             data_vencimento DATE,
                             status VARCHAR(20) NOT NULL DEFAULT 'EMESTOQUE' CHECK (status IN ('EMESTOQUE', 'ESTOQUEBAIXO', 'ESTOQUEZERADO', 'VENCIDO', 'DESCARTADO')),
                             unidade_id INTEGER REFERENCES unidades(id) NOT NULL,
                             localizacao_estoque VARCHAR(100),
                             data_cadastro TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                             data_atualizacao TIMESTAMP
);

-- Movimentações de Estoque
CREATE TABLE movimentacoes_estoque (
                                       id SERIAL PRIMARY KEY,
                                       suprimento_id INTEGER REFERENCES suprimentos(id) NOT NULL,
                                       tipo_movimento VARCHAR(20) NOT NULL CHECK (tipo_movimento IN ('ENTRADA', 'SAIDA', 'AJUSTE', 'DESCARTE')),
                                       quantidade DECIMAL(12,3) NOT NULL,
                                       valor_unitario DECIMAL(10,2),
                                       motivo TEXT NOT NULL,
                                       responsavel_id INTEGER REFERENCES profissionais(id) NOT NULL,
                                       data_movimento TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                       observacoes TEXT,
                                       nota_fiscal VARCHAR(50)
);

-- =====================================================
-- 8. FINANCIAL SYSTEM - Reports and Billing
-- =====================================================

-- Itens Financeiros (Financial transactions)
CREATE TABLE itens_financeiros (
                                   id SERIAL PRIMARY KEY,
                                   descricao VARCHAR(200) NOT NULL,
                                   tipo VARCHAR(10) NOT NULL CHECK (tipo IN ('RECEITA', 'DESPESA')),
                                   categoria VARCHAR(50) NOT NULL,
                                   valor DECIMAL(12,2) NOT NULL,
                                   data_lancamento DATE NOT NULL,
                                   unidade_id INTEGER REFERENCES unidades(id) NOT NULL,
                                   paciente_id INTEGER REFERENCES pacientes(id),
                                   profissional_id INTEGER REFERENCES profissionais(id),
                                   agendamento_id INTEGER REFERENCES agendamentos(id),
                                   internacao_id INTEGER REFERENCES internacoes(id),
                                   observacoes TEXT,
                                   data_cadastro TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Relatórios Financeiros
CREATE TABLE relatorios_financeiros (
                                        id SERIAL PRIMARY KEY,
                                        unidade_id INTEGER REFERENCES unidades(id) NOT NULL,
                                        periodo VARCHAR(20) NOT NULL,
                                        tipo_relatorio VARCHAR(20) NOT NULL CHECK (tipo_relatorio IN ('MENSAL', 'TRIMESTRAL', 'ANUAL')),
                                        total_receita DECIMAL(12,2) DEFAULT 0,
                                        total_despesa DECIMAL(12,2) DEFAULT 0,
                                        lucro_liquido DECIMAL(12,2) DEFAULT 0,
                                        consultas_realizadas INTEGER DEFAULT 0,
                                        exames_realizados INTEGER DEFAULT 0,
                                        internacoes_total INTEGER DEFAULT 0,
                                        sessoes_telemedicina INTEGER DEFAULT 0,
                                        ticket_medio_consulta DECIMAL(10,2) DEFAULT 0,
                                        taxa_ocupacao_leitos DECIMAL(5,2) DEFAULT 0,
                                        data_geracao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                        gerado_por INTEGER REFERENCES profissionais(id) NOT NULL
);

-- =====================================================
-- 9. AUDIT AND SECURITY SYSTEM - LGPD Compliance
-- =====================================================

-- Logs de Auditoria (Audit logs for LGPD compliance)
CREATE TABLE audit_logs (
                            id SERIAL PRIMARY KEY,
                            user_id INTEGER NOT NULL,
                            action VARCHAR(100) NOT NULL,
                            entity_type VARCHAR(100) NOT NULL,
                            entity_id VARCHAR(100) NOT NULL,
                            old_values JSONB,
                            new_values JSONB,
                            ip_address INET,
                            user_agent TEXT,
                            timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                            success BOOLEAN DEFAULT true,
                            error_message TEXT
);

-- Controle de Acesso (Access control for different user roles)
CREATE TABLE usuarios_sistema (
                                  id SERIAL PRIMARY KEY,
                                  username VARCHAR(100) UNIQUE NOT NULL,
                                  password_hash VARCHAR(255) NOT NULL,
                                  salt VARCHAR(100) NOT NULL,
                                  email VARCHAR(100) UNIQUE NOT NULL,
                                  nome_completo VARCHAR(200) NOT NULL,
                                  ativo BOOLEAN DEFAULT true,
                                  ultimo_login TIMESTAMP,
                                  tentativas_login INTEGER DEFAULT 0,
                                  bloqueado_ate TIMESTAMP,
                                  profissional_id INTEGER REFERENCES profissionais(id),
                                  paciente_id INTEGER REFERENCES pacientes(id),
                                  unidade_id INTEGER REFERENCES unidades(id),
                                  data_cadastro TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                  data_atualizacao TIMESTAMP,
                                  CONSTRAINT check_user_reference CHECK (
                                      (profissional_id IS NOT NULL AND paciente_id IS NULL) OR
                                      (profissional_id IS NULL AND paciente_id IS NOT NULL)
                                      )
);

-- Perfis de Acesso
CREATE TABLE perfis_acesso (
                               id SERIAL PRIMARY KEY,
                               nome VARCHAR(100) UNIQUE NOT NULL,
                               descricao TEXT,
                               nivel_acesso INTEGER NOT NULL, -- 1=Básico, 2=Intermediário, 3=Avançado, 4=Administrador
                               ativo BOOLEAN DEFAULT true
);

-- Relacionamento Usuário-Perfil
CREATE TABLE usuarios_perfis (
                                 id SERIAL PRIMARY KEY,
                                 usuario_id INTEGER REFERENCES usuarios_sistema(id) NOT NULL,
                                 perfil_id INTEGER REFERENCES perfis_acesso(id) NOT NULL,
                                 data_atribuicao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                 data_revogacao TIMESTAMP,
                                 ativo BOOLEAN DEFAULT true,
                                 UNIQUE(usuario_id, perfil_id)
);

-- Permissões do Sistema
CREATE TABLE permissoes (
                            id SERIAL PRIMARY KEY,
                            nome VARCHAR(100) UNIQUE NOT NULL,
                            descricao TEXT,
                            modulo VARCHAR(50) NOT NULL, -- PACIENTE, PROFISSIONAL, AGENDAMENTO, etc.
                            acao VARCHAR(50) NOT NULL -- CREATE, READ, UPDATE, DELETE, EXECUTE
);

-- Relacionamento Perfil-Permissão
CREATE TABLE perfis_permissoes (
                                   id SERIAL PRIMARY KEY,
                                   perfil_id INTEGER REFERENCES perfis_acesso(id) NOT NULL,
                                   permissao_id INTEGER REFERENCES permissoes(id) NOT NULL,
                                   UNIQUE(perfil_id, permissao_id)
);

-- =====================================================
-- 10. PERFORMANCE INDEXES
-- =====================================================

-- Patient indexes
CREATE INDEX idx_pacientes_cpf ON pacientes(cpf);
CREATE INDEX idx_pacientes_nome ON pacientes USING gin(to_tsvector('portuguese', nome));
CREATE INDEX idx_pacientes_ativo ON pacientes(ativo, data_cadastro);

-- Professional indexes
CREATE INDEX idx_profissionais_cpf ON profissionais(cpf);
CREATE INDEX idx_profissionais_crm ON profissionais(crm);
CREATE INDEX idx_profissionais_ativo_unidade ON profissionais(ativo, unidade_id);
CREATE INDEX idx_profissionais_tipo ON profissionais(tipo_profissional, ativo);

-- Appointment indexes
CREATE INDEX idx_agendamentos_data_hora ON agendamentos(data_hora);
CREATE INDEX idx_agendamentos_paciente_data ON agendamentos(paciente_id, data_hora);
CREATE INDEX idx_agendamentos_profissional_data ON agendamentos(profissional_id, data_hora);
CREATE INDEX idx_agendamentos_status_unidade ON agendamentos(status, unidade_id);
CREATE INDEX idx_agendamentos_tipo_data ON agendamentos(tipo_agendamento, data_hora);

-- Medical records indexes
CREATE INDEX idx_prontuarios_paciente ON prontuarios(paciente_id, data_atendimento DESC);
CREATE INDEX idx_prontuarios_profissional ON prontuarios(profissional_id, data_atendimento DESC);
CREATE INDEX idx_prontuarios_data_tipo ON prontuarios(data_atendimento, tipo_atendimento);
CREATE INDEX idx_prontuarios_assinado ON prontuarios(assinado, data_cadastro);

-- Prescription indexes
CREATE INDEX idx_prescricoes_prontuario ON prescricoes(prontuario_id, ativo);
CREATE INDEX idx_prescricoes_vencimento ON prescricoes(data_vencimento, ativo) WHERE data_vencimento IS NOT NULL;

-- Telemedicine indexes
CREATE INDEX idx_sessoes_telemedicina_profissional ON sessoes_telemedicina(profissional_id, data_criacao);
CREATE INDEX idx_sessoes_telemedicina_paciente ON sessoes_telemedicina(paciente_id, data_criacao);
CREATE INDEX idx_sessoes_telemedicina_status ON sessoes_telemedicina(status, data_criacao);
CREATE INDEX idx_participantes_sessao_ativo ON participantes_sessao(sessao_id, ativo);

-- Administration indexes
CREATE INDEX idx_leitos_unidade_status ON leitos(unidade_id, status);
CREATE INDEX idx_leitos_paciente ON leitos(paciente_id) WHERE paciente_id IS NOT NULL;
CREATE INDEX idx_internacoes_paciente_data ON internacoes(paciente_id, data_internacao);
CREATE INDEX idx_internacoes_leito ON internacoes(leito_id);
CREATE INDEX idx_internacoes_status_unidade ON internacoes(status, unidade_id);
CREATE INDEX idx_internacoes_medico_data ON internacoes(medico_responsavel_id, data_internacao);

-- Supply indexes
CREATE INDEX idx_suprimentos_categoria_unidade ON suprimentos(categoria, unidade_id);
CREATE INDEX idx_suprimentos_status_vencimento ON suprimentos(status, data_vencimento);
CREATE INDEX idx_suprimentos_estoque_minimo ON suprimentos(quantidade_estoque, quantidade_minima) WHERE status != 'DESCARTADO';
CREATE INDEX idx_movimentacoes_suprimento_data ON movimentacoes_estoque(suprimento_id, data_movimento DESC);

-- Financial indexes
CREATE INDEX idx_itens_financeiros_unidade_data ON itens_financeiros(unidade_id, data_lancamento);
CREATE INDEX idx_itens_financeiros_tipo_categoria ON itens_financeiros(tipo, categoria, data_lancamento);

-- Audit indexes
CREATE INDEX idx_audit_logs_timestamp ON audit_logs(timestamp DESC);
CREATE INDEX idx_audit_logs_user_action ON audit_logs(user_id, action, timestamp);
CREATE INDEX idx_audit_logs_entity ON audit_logs(entity_type, entity_id);

-- Security indexes
CREATE INDEX idx_usuarios_sistema_username ON usuarios_sistema(username);
CREATE INDEX idx_usuarios_sistema_email ON usuarios_sistema(email);
CREATE INDEX idx_usuarios_sistema_ativo ON usuarios_sistema(ativo, ultimo_login);

-- =====================================================
-- 11. TRIGGERS AND FUNCTIONS
-- =====================================================

-- Function to update data_atualizacao automatically
CREATE OR REPLACE FUNCTION update_data_atualizacao()
RETURNS TRIGGER AS $$
BEGIN
    NEW.data_atualizacao := CURRENT_TIMESTAMP;
RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Triggers for automatic timestamp updates
CREATE TRIGGER trigger_update_pacientes_data_atualizacao
    BEFORE UPDATE ON pacientes
    FOR EACH ROW
    EXECUTE FUNCTION update_data_atualizacao();

CREATE TRIGGER trigger_update_profissionais_data_atualizacao
    BEFORE UPDATE ON profissionais
    FOR EACH ROW
    EXECUTE FUNCTION update_data_atualizacao();

CREATE TRIGGER trigger_update_agendamentos_data_atualizacao
    BEFORE UPDATE ON agendamentos
    FOR EACH ROW
    EXECUTE FUNCTION update_data_atualizacao();

CREATE TRIGGER trigger_update_prontuarios_data_atualizacao
    BEFORE UPDATE ON prontuarios
    FOR EACH ROW
    EXECUTE FUNCTION update_data_atualizacao();

CREATE TRIGGER trigger_update_sessoes_telemedicina_data_atualizacao
    BEFORE UPDATE ON sessoes_telemedicina
    FOR EACH ROW
    EXECUTE FUNCTION update_data_atualizacao();

CREATE TRIGGER trigger_update_unidades_data_atualizacao
    BEFORE UPDATE ON unidades
    FOR EACH ROW
    EXECUTE FUNCTION update_data_atualizacao();

CREATE TRIGGER trigger_update_leitos_data_atualizacao
    BEFORE UPDATE ON leitos
    FOR EACH ROW
    EXECUTE FUNCTION update_data_atualizacao();

CREATE TRIGGER trigger_update_internacoes_data_atualizacao
    BEFORE UPDATE ON internacoes
    FOR EACH ROW
    EXECUTE FUNCTION update_data_atualizacao();

CREATE TRIGGER trigger_update_suprimentos_data_atualizacao
    BEFORE UPDATE ON suprimentos
    FOR EACH ROW
    EXECUTE FUNCTION update_data_atualizacao();

-- Business logic triggers

-- Prevent double booking of professionals
CREATE OR REPLACE FUNCTION check_professional_availability()
RETURNS TRIGGER AS $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM agendamentos 
        WHERE profissional_id = NEW.profissional_id 
        AND status NOT IN ('CANCELADO', 'FALTOU')
        AND id != COALESCE(NEW.id, 0)
        AND (
            (data_hora <= NEW.data_hora AND (data_hora + duracao) > NEW.data_hora) OR
            (data_hora < (NEW.data_hora + NEW.duracao) AND (data_hora + duracao) >= (NEW.data_hora + NEW.duracao)) OR
            (data_hora >= NEW.data_hora AND data_hora < (NEW.data_hora + NEW.duracao))
        )
    ) THEN
        RAISE EXCEPTION 'Profissional já possui agendamento neste horário';
END IF;
RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_check_professional_availability
    BEFORE INSERT OR UPDATE ON agendamentos
                         FOR EACH ROW
                         EXECUTE FUNCTION check_professional_availability();

-- Ensure only one patient per bed
CREATE OR REPLACE FUNCTION check_leito_ocupacao()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.status = 'OCUPADO' AND NEW.paciente_id IS NOT NULL THEN
        -- Check if patient is already in another bed
        IF EXISTS (
            SELECT 1 FROM leitos 
            WHERE id != NEW.id 
            AND paciente_id = NEW.paciente_id 
            AND status = 'OCUPADO'
        ) THEN
            RAISE EXCEPTION 'Paciente já está internado em outro leito';
END IF;
END IF;
    
    -- Clear patient_id when bed is not occupied
    IF NEW.status != 'OCUPADO' THEN
        NEW.paciente_id := NULL;
END IF;

RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_check_leito_ocupacao
    BEFORE UPDATE ON leitos
    FOR EACH ROW
    EXECUTE FUNCTION check_leito_ocupacao();

-- Update supply status based on quantity
CREATE OR REPLACE FUNCTION update_suprimento_status()
RETURNS TRIGGER AS $$
BEGIN
    -- Update status based on quantity
    IF NEW.quantidade_estoque <= 0 THEN
        NEW.status := 'ESTOQUEZERADO';
    ELSIF NEW.quantidade_estoque <= NEW.quantidade_minima THEN
        NEW.status := 'ESTOQUEBAIXO';
ELSE
        NEW.status := 'EMESTOQUE';
END IF;
    
    -- Check expiration
    IF NEW.data_vencimento IS NOT NULL AND NEW.data_vencimento <= CURRENT_DATE THEN
        NEW.status := 'VENCIDO';
END IF;

RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_update_suprimento_status
    BEFORE UPDATE OF quantidade_estoque ON suprimentos
    FOR EACH ROW
    EXECUTE FUNCTION update_suprimento_status();

-- Update appointment status when telemedicine session changes
CREATE OR REPLACE FUNCTION update_agendamento_status_telemedicina()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.status = 'FINALIZADA' THEN
UPDATE agendamentos
SET status = 'REALIZADO', data_atualizacao = CURRENT_TIMESTAMP
WHERE id = NEW.agendamento_id;
ELSIF NEW.status = 'CANCELADA' THEN
UPDATE agendamentos
SET status = 'CANCELADO', data_atualizacao = CURRENT_TIMESTAMP,
    cancelado_por = NEW.cancelado_por, motivo_cancel = NEW.motivo_cancel
WHERE id = NEW.agendamento_id;
END IF;
RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_update_agendamento_status_telemedicina
    AFTER UPDATE OF status ON sessoes_telemedicina
    FOR EACH ROW
    EXECUTE FUNCTION update_agendamento_status_telemedicina();

-- =====================================================
-- 12. USEFUL VIEWS FOR REPORTING
-- =====================================================

-- Bed occupancy view
CREATE OR REPLACE VIEW view_ocupacao_leitos AS
SELECT
    u.id as unidade_id,
    u.nome as unidade_nome,
    l.tipo_leito,
    COUNT(*) as total_leitos,
    COUNT(CASE WHEN l.status = 'OCUPADO' THEN 1 END) as leitos_ocupados,
    COUNT(CASE WHEN l.status = 'LIVRE' THEN 1 END) as leitos_livres,
    COUNT(CASE WHEN l.status = 'MANUTENCAO' THEN 1 END) as leitos_manutencao,
    ROUND(
            (COUNT(CASE WHEN l.status = 'OCUPADO' THEN 1 END)::DECIMAL / 
         NULLIF(COUNT(CASE WHEN l.status IN ('OCUPADO', 'LIVRE') THEN 1 END), 0)) * 100,
            2
    ) as taxa_ocupacao
FROM unidades u
         LEFT JOIN leitos l ON u.id = l.unidade_id
WHERE u.ativa = true
GROUP BY u.id, u.nome, l.tipo_leito
ORDER BY u.nome, l.tipo_leito;

-- Active admissions view
CREATE OR REPLACE VIEW view_internacoes_ativas AS
SELECT
    i.id,
    p.nome as paciente_nome,
    p.cpf as paciente_cpf,
    l.numero as leito_numero,
    l.setor as leito_setor,
    l.tipo_leito,
    pr.nome as medico_nome,
    pr.crm as medico_crm,
    u.nome as unidade_nome,
    i.data_internacao,
    i.data_alta_prevista,
    CURRENT_DATE - i.data_internacao::DATE as dias_internado,
        i.tipo_internacao,
    i.motivo_internacao,
    i.diagnostico
FROM internacoes i
         INNER JOIN pacientes p ON i.paciente_id = p.id
         INNER JOIN leitos l ON i.leito_id = l.id
         INNER JOIN profissionais pr ON i.medico_responsavel_id = pr.id
         INNER JOIN unidades u ON i.unidade_id = u.id
WHERE i.status = 'INTERNADO'
ORDER BY i.data_internacao;

-- Critical supplies view
CREATE OR REPLACE VIEW view_suprimentos_criticos AS
SELECT
    s.id,
    s.nome,
    s.categoria,
    s.quantidade_estoque,
    s.quantidade_minima,
    s.data_vencimento,
    u.nome as unidade_nome,
    s.localizacao_estoque,
    CASE
        WHEN s.quantidade_estoque <= 0 THEN 'ZERADO'
        WHEN s.quantidade_estoque <= s.quantidade_minima THEN 'BAIXO'
        WHEN s.data_vencimento <= CURRENT_DATE + INTERVAL '30 days' THEN 'VENCENDO'
        WHEN s.data_vencimento <= CURRENT_DATE THEN 'VENCIDO'
        ELSE 'OK'
END as criticidade,
    CASE 
        WHEN s.data_vencimento IS NOT NULL THEN 
            CURRENT_DATE - s.data_vencimento
        ELSE NULL
END as dias_para_vencimento
FROM suprimentos s
INNER JOIN unidades u ON s.unidade_id = u.id
WHERE s.status NOT IN ('DESCARTADO')
AND (
    s.quantidade_estoque <= s.quantidade_minima 
    OR s.data_vencimento <= CURRENT_DATE + INTERVAL '30 days'
)
ORDER BY 
    CASE s.status 
        WHEN 'ESTOQUEZERADO' THEN 1
        WHEN 'ESTOQUEBAIXO' THEN 2
        WHEN 'VENCIDO' THEN 3
        ELSE 4
END,
    s.data_vencimento NULLS LAST;

-- Professional schedule view
CREATE OR REPLACE VIEW view_agenda_profissionais AS
SELECT
    a.id,
    a.data_hora,
    a.duracao,
    a.status,
    a.tipo_agendamento,
    p.nome as paciente_nome,
    p.telefone as paciente_telefone,
    pr.nome as profissional_nome,
    pr.crm as profissional_crm,
    u.nome as unidade_nome,
    s.nome as sala_nome,
    a.observacoes,
    a.valor_consulta
FROM agendamentos a
         INNER JOIN pacientes p ON a.paciente_id = p.id
         INNER JOIN profissionais pr ON a.profissional_id = pr.id
         INNER JOIN unidades u ON a.unidade_id = u.id
         LEFT JOIN salas s ON a.sala_id = s.id
WHERE a.data_hora >= CURRENT_DATE
ORDER BY pr.nome, a.data_hora;

-- Financial summary view
CREATE OR REPLACE VIEW view_resumo_financeiro AS
SELECT
    u.id as unidade_id,
    u.nome as unidade_nome,
    DATE_TRUNC('month', if.data_lancamento) as mes_ano,
    SUM(CASE WHEN if.tipo = 'RECEITA' THEN if.valor ELSE 0 END) as total_receita,
    SUM(CASE WHEN if.tipo = 'DESPESA' THEN if.valor ELSE 0 END) as total_despesa,
    SUM(CASE WHEN if.tipo = 'RECEITA' THEN if.valor ELSE -if.valor END) as lucro_liquido,
    COUNT(CASE WHEN if.tipo = 'RECEITA' AND if.categoria = 'CONSULTA' THEN 1 END) as consultas_realizadas,
    COUNT(CASE WHEN if.tipo = 'RECEITA' AND if.categoria = 'EXAME' THEN 1 END) as exames_realizados,
    AVG(CASE WHEN if.tipo = 'RECEITA' AND if.categoria = 'CONSULTA' THEN if.valor END) as ticket_medio_consulta
FROM unidades u
         LEFT JOIN itens_financeiros if ON u.id = if.unidade_id
WHERE u.ativa = true
GROUP BY u.id, u.nome, DATE_TRUNC('month', if.data_lancamento)
ORDER BY u.nome, mes_ano DESC;

-- Patient medical history view
CREATE OR REPLACE VIEW view_historico_paciente AS
SELECT
    p.id as paciente_id,
    p.nome as paciente_nome,
    p.cpf,
    p.data_nascimento,
    pr.id as prontuario_id,
    pr.data_atendimento,
    pr.tipo_atendimento,
    pr.queixa_principal,
    i.diagnostico,
    pr.cid10,
    prof.nome as profissional_nome,
    prof.crm,
    u.nome as unidade_nome,
    pr.assinado,
    COUNT(pre.id) as total_prescricoes,
    COUNT(ex.id) as total_exames
FROM pacientes p
         LEFT JOIN prontuarios pr ON p.id = pr.paciente_id
         LEFT JOIN profissionais prof ON pr.profissional_id = prof.id
         LEFT JOIN unidades u ON pr.unidade_id = u.id
         LEFT JOIN prescricoes pre ON pr.id = pre.prontuario_id AND pre.ativo = true
         LEFT JOIN exames_solicitados ex ON pr.id = ex.prontuario_id
         left join internacoes i on u.id = i.unidade_id
WHERE p.ativo = true
GROUP BY p.id, p.nome, p.cpf, p.data_nascimento, pr.id, pr.data_atendimento,
         pr.tipo_atendimento, pr.queixa_principal, i.diagnostico, pr.cid10,
         prof.nome, prof.crm, u.nome, pr.assinado
ORDER BY p.nome, pr.data_atendimento DESC;

-- =====================================================
-- 13. INITIAL DATA SETUP
-- =====================================================

-- Insert default unit
INSERT INTO unidades (nome, cnpj, tipo_unidade, endereco, telefone, email, responsavel, capacidade_leitos)
VALUES ('Hospital Central SGHSS', '12345678000123', 'HOSPITAL', 'Rua da Saúde, 123 - Centro', '(11) 3333-4444', 'contato@sghss.com', 'Dr. João Silva', 50)
    ON CONFLICT (cnpj) DO NOTHING;

-- Insert medical specialties
INSERT INTO especialidades (nome, codigo, conselho_regulamentador) VALUES
                                                                       ('Clínica Médica', 'CLM', 'CRM'),
                                                                       ('Cardiologia', 'CAR', 'CRM'),
                                                                       ('Pediatria', 'PED', 'CRM'),
                                                                       ('Ginecologia e Obstetrícia', 'GO', 'CRM'),
                                                                       ('Ortopedia e Traumatologia', 'ORT', 'CRM'),
                                                                       ('Neurologia', 'NEU', 'CRM'),
                                                                       ('Psiquiatria', 'PSI', 'CRM'),
                                                                       ('Dermatologia', 'DER', 'CRM'),
                                                                       ('Oftalmologia', 'OFT', 'CRM'),
                                                                       ('Otorrinolaringologia', 'ORL', 'CRM'),
                                                                       ('Enfermagem', 'ENF', 'COREN'),
                                                                       ('Fisioterapia', 'FIS', 'CREFITO'),
                                                                       ('Psicologia', 'PSI', 'CRP'),
                                                                       ('Nutrição', 'NUT', 'CRN'),
                                                                       ('Farmácia', 'FAR', 'CRF')
    ON CONFLICT (codigo) DO NOTHING;

-- Insert access profiles
INSERT INTO perfis_acesso (nome, descricao, nivel_acesso) VALUES
                                                              ('Administrador', 'Acesso total ao sistema', 4),
                                                              ('Médico', 'Acesso a funcionalidades médicas', 3),
                                                              ('Enfermeiro', 'Acesso a funcionalidades de enfermagem', 2),
                                                              ('Recepcionista', 'Acesso a agendamentos e cadastros básicos', 2),
                                                              ('Paciente', 'Acesso limitado a informações próprias', 1),
                                                              ('Financeiro', 'Acesso a relatórios financeiros', 3),
                                                              ('Farmácia', 'Acesso ao controle de medicamentos', 2),
                                                              ('Laboratório', 'Acesso a exames e resultados', 2)
    ON CONFLICT (nome) DO NOTHING;

-- Insert system permissions
INSERT INTO permissoes (nome, descricao, modulo, acao) VALUES
                                                           -- Patient module
                                                           ('PACIENTE_CREATE', 'Criar novos pacientes', 'PACIENTE', 'CREATE'),
                                                           ('PACIENTE_READ', 'Visualizar dados de pacientes', 'PACIENTE', 'READ'),
                                                           ('PACIENTE_UPDATE', 'Atualizar dados de pacientes', 'PACIENTE', 'UPDATE'),
                                                           ('PACIENTE_DELETE', 'Excluir pacientes', 'PACIENTE', 'DELETE'),

                                                           -- Professional module
                                                           ('PROFISSIONAL_CREATE', 'Criar novos profissionais', 'PROFISSIONAL', 'CREATE'),
                                                           ('PROFISSIONAL_READ', 'Visualizar dados de profissionais', 'PROFISSIONAL', 'READ'),
                                                           ('PROFISSIONAL_UPDATE', 'Atualizar dados de profissionais', 'PROFISSIONAL', 'UPDATE'),
                                                           ('PROFISSIONAL_DELETE', 'Excluir profissionais', 'PROFISSIONAL', 'DELETE'),

                                                           -- Appointment module
                                                           ('AGENDAMENTO_CREATE', 'Criar agendamentos', 'AGENDAMENTO', 'CREATE'),
                                                           ('AGENDAMENTO_READ', 'Visualizar agendamentos', 'AGENDAMENTO', 'READ'),
                                                           ('AGENDAMENTO_UPDATE', 'Atualizar agendamentos', 'AGENDAMENTO', 'UPDATE'),
                                                           ('AGENDAMENTO_DELETE', 'Cancelar agendamentos', 'AGENDAMENTO', 'DELETE'),

                                                           -- Medical records module
                                                           ('PRONTUARIO_CREATE', 'Criar prontuários', 'PRONTUARIO', 'CREATE'),
                                                           ('PRONTUARIO_READ', 'Visualizar prontuários', 'PRONTUARIO', 'READ'),
                                                           ('PRONTUARIO_UPDATE', 'Atualizar prontuários', 'PRONTUARIO', 'UPDATE'),
                                                           ('PRONTUARIO_SIGN', 'Assinar prontuários', 'PRONTUARIO', 'EXECUTE'),

                                                           -- Telemedicine module
                                                           ('TELEMEDICINA_CREATE', 'Criar sessões de telemedicina', 'TELEMEDICINA', 'CREATE'),
                                                           ('TELEMEDICINA_READ', 'Visualizar sessões de telemedicina', 'TELEMEDICINA', 'READ'),
                                                           ('TELEMEDICINA_UPDATE', 'Gerenciar sessões de telemedicina', 'TELEMEDICINA', 'UPDATE'),

                                                           -- Administration module
                                                           ('ADMIN_LEITOS', 'Gerenciar leitos', 'ADMINISTRACAO', 'UPDATE'),
                                                           ('ADMIN_INTERNACOES', 'Gerenciar internações', 'ADMINISTRACAO', 'UPDATE'),
                                                           ('ADMIN_SUPRIMENTOS', 'Gerenciar suprimentos', 'ADMINISTRACAO', 'UPDATE'),
                                                           ('ADMIN_RELATORIOS', 'Gerar relatórios', 'ADMINISTRACAO', 'READ'),

                                                           -- System administration
                                                           ('SISTEMA_CONFIG', 'Configurar sistema', 'SISTEMA', 'UPDATE'),
                                                           ('USUARIO_MANAGE', 'Gerenciar usuários', 'SISTEMA', 'UPDATE')
    ON CONFLICT (nome) DO NOTHING;

-- Associate permissions to profiles
INSERT INTO perfis_permissoes (perfil_id, permissao_id)
SELECT p.id, pe.id
FROM perfis_acesso p, permissoes pe
WHERE p.nome = 'Administrador'
    ON CONFLICT (perfil_id, permissao_id) DO NOTHING;

-- Associate medical permissions to doctor profile
INSERT INTO perfis_permissoes (perfil_id, permissao_id)
SELECT p.id, pe.id
FROM perfis_acesso p, permissoes pe
WHERE p.nome = 'Médico'
  AND pe.nome IN ('PACIENTE_READ', 'PACIENTE_UPDATE', 'AGENDAMENTO_READ', 'AGENDAMENTO_UPDATE',
                  'PRONTUARIO_CREATE', 'PRONTUARIO_READ', 'PRONTUARIO_UPDATE', 'PRONTUARIO_SIGN',
                  'TELEMEDICINA_CREATE', 'TELEMEDICINA_READ', 'TELEMEDICINA_UPDATE')
    ON CONFLICT (perfil_id, permissao_id) DO NOTHING;
select * from unidades u
-- Insert sample rooms
    INSERT INTO salas (nome, unidade_id, tipo_sala, capacidade)
SELECT 'Consultório ' || generate_series(1, 10), 2, 'CONSULTORIO', 1
    WHERE NOT EXISTS (SELECT 1 FROM salas WHERE unidade_id = 2 LIMIT 1);

INSERT INTO salas (nome, unidade_id, tipo_sala, capacidade)
SELECT 'Sala de Exames ' || generate_series(1, 5), 2, 'EXAME', 2
    WHERE NOT EXISTS (SELECT 1 FROM salas WHERE nome LIKE 'Sala de Exames%' LIMIT 1);

-- Insert sample beds
INSERT INTO leitos (unidade_id, numero, setor, tipo_leito, valor_diaria, equipamentos, capacidade_acompanhantes)
SELECT
    2, -- unidade_id
    'L' || LPAD(gs::TEXT, 3, '0'), -- numero
    CASE
        WHEN gs <= 10 THEN 'UTI Adulto'
        WHEN gs <= 15 THEN 'UTI Pediátrica'
        WHEN gs <= 25 THEN 'Semi-UTI'
        WHEN gs <= 35 THEN 'Enfermaria Clínica'
        WHEN gs <= 45 THEN 'Enfermaria Cirúrgica'
        ELSE 'Apartamentos'
        END, -- setor
    CASE
        WHEN gs <= 15 THEN 'UTI'
        WHEN gs <= 25 THEN 'SEMIUTI'
        WHEN gs <= 45 THEN 'ENFERMARIA'
        ELSE 'PARTICULAR'
        END, -- tipo_leito
    CASE
        WHEN gs <= 15 THEN 1200.00
        WHEN gs <= 25 THEN 600.00
        WHEN gs <= 45 THEN 300.00
        ELSE 800.00
        END, -- valor_diaria
    CASE
        WHEN gs <= 15 THEN 'Monitor Multiparâmetro;Ventilador Mecânico;Bomba de Infusão;Oxímetro'
        WHEN gs <= 25 THEN 'Monitor Cardíaco;Oxímetro;Bomba de Infusão'
        WHEN gs <= 45 THEN 'Cama Hospitalar;Suporte Soro;Oxímetro'
        ELSE 'Cama Hospitalar;TV;Frigobar;Ar Condicionado'
        END, -- equipamentos
    CASE
        WHEN gs <= 25 THEN 1
        ELSE 2
        END -- capacidade_acompanhantes
FROM generate_series(1, 50) AS gs
WHERE NOT EXISTS (SELECT 1 FROM leitos WHERE unidade_id = 2 LIMIT 1);
-- Insert sample supplies
INSERT INTO suprimentos (nome, categoria, codigo, descricao, unidade_medida, quantidade_estoque, quantidade_minima, quantidade_maxima, valor_unitario, fornecedor, data_vencimento, unidade_id, localizacao_estoque)
VALUES
    ('Paracetamol 750mg', 'MEDICAMENTO', 'MED001', 'Analgésico e antitérmico', 'CP', 500, 100, 2000, 0.25, 'Farmácia Brasil', '2025-12-31', 2, 'Farmácia - Prateleira A1'),
    ('Dipirona 500mg', 'MEDICAMENTO', 'MED002', 'Analgésico e antitérmico', 'CP', 300, 50, 1000, 0.30, 'Farmácia Brasil', '2025-11-30', 2, 'Farmácia - Prateleira A2'),
    ('Soro Fisiológico 500ml', 'MEDICAMENTO', 'MED003', 'Solução fisiológica estéril', 'UN', 200, 50, 500, 3.50, 'MedSupply', '2025-08-15', 2, 'Farmácia - Prateleira B1'),
    ('Seringa 10ml', 'MATERIALMEDICO', 'MAT001', 'Seringa descartável estéril', 'UN', 1000, 200, 5000, 1.20, 'MedEquip', NULL, 2, 'Almoxarifado - Setor B'),
    ('Luva Procedimento P', 'MATERIALMEDICO', 'MAT002', 'Luva de procedimento tamanho P', 'UN', 2000, 500, 10000, 0.15, 'ProtectMax', NULL, 2, 'Almoxarifado - Setor A'),
    ('Luva Procedimento M', 'MATERIALMEDICO', 'MAT003', 'Luva de procedimento tamanho M', 'UN', 15, 100, 10000, 0.15, 'ProtectMax', NULL, 2, 'Almoxarifado - Setor A'),
    ('Luva Procedimento G', 'MATERIALMEDICO', 'MAT004', 'Luva de procedimento tamanho G', 'UN', 1500, 300, 8000, 0.15, 'ProtectMax', NULL, 2, 'Almoxarifado - Setor A'),
    ('Gaze Estéril 7.5x7.5', 'MATERIALMEDICO', 'MAT005', 'Gaze estéril para curativos', 'PCT', 50, 20, 200, 2.30, 'SterileMax', '2025-06-30', 2, 'Almoxarifado - Setor C'),
    ('Álcool 70%', 'LIMPEZA', 'LMP001', 'Álcool etílico 70% para assepsia', 'L', 30, 15, 100, 8.50, 'CleanPro', '2026-12-31', 2, 'Limpeza - Estoque Principal'),
    ('Detergente Hospitalar', 'LIMPEZA', 'LMP002', 'Detergente para limpeza hospitalar', 'L', 8, 20, 80, 12.00, 'CleanPro', '2025-03-15', 2, 'Limpeza - Estoque Principal')
    ON CONFLICT (codigo) DO NOTHING;
-- =====================================================
-- 14. MAINTENANCE PROCEDURES
-- =====================================================

-- Procedure to clean old audit logs (keep last 2 years)
CREATE OR REPLACE FUNCTION limpar_audit_logs_antigos()
RETURNS void AS $$
BEGIN
DELETE FROM audit_logs
WHERE timestamp < CURRENT_TIMESTAMP - INTERVAL '2 years';

-- Log the cleanup
INSERT INTO audit_logs (user_id, action, entity_type, entity_id, new_values)
VALUES (0, 'CLEANUP', 'AUDIT_LOGS', 'SYSTEM',
        jsonb_build_object('message', 'Limpeza automática de logs antigos executada'));
END;
$$ LANGUAGE plpgsql;

-- Procedure to update expired supplies
CREATE OR REPLACE FUNCTION atualizar_suprimentos_vencidos()
RETURNS void AS $$
BEGIN
with affected_rows as (
UPDATE suprimentos
SET status = 'VENCIDO', data_atualizacao = CURRENT_TIMESTAMP
WHERE data_vencimento <= CURRENT_DATE
  AND status NOT IN ('VENCIDO', 'DESCARTADO')
    RETURNING *)

-- Log the update
INSERT INTO audit_logs (user_id, action, entity_type, entity_id, new_values)
VALUES (0, 'AUTO_UPDATE', 'SUPRIMENTOS', 'SYSTEM',
    jsonb_build_object('affected_rows', affected_rows, 'message', 'Atualização automática de suprimentos vencidos'));
END;
$$ LANGUAGE plpgsql;

-- Procedure to generate automatic financial report
CREATE OR REPLACE FUNCTION gerar_relatorio_mensal_automatico()
RETURNS void AS $$
DECLARE
current_month VARCHAR(7);
    unit_record RECORD;
BEGIN
    current_month := TO_CHAR(CURRENT_DATE - INTERVAL '1 month', 'YYYY-MM');

FOR unit_record IN SELECT id FROM unidades WHERE ativa = true LOOP
                   INSERT INTO relatorios_financeiros (
    unidade_id, periodo, tipo_relatorio, total_receita, total_despesa,
    lucro_liquido, consultas_realizadas, exames_realizados, internacoes_total,
    sessoes_telemedicina, ticket_medio_consulta, taxa_ocupacao_leitos, gerado_por
)
SELECT
    unit_record.id,
    current_month,
    'MENSAL',
    COALESCE(SUM(CASE WHEN if.tipo = 'RECEITA' THEN if.valor ELSE 0 END), 0),
    COALESCE(SUM(CASE WHEN if.tipo = 'DESPESA' THEN if.valor ELSE 0 END), 0),
    COALESCE(SUM(CASE WHEN if.tipo = 'RECEITA' THEN if.valor ELSE -if.valor END), 0),
    COALESCE(COUNT(CASE WHEN a.tipo_agendamento = 'CONSULTA' AND a.status = 'REALIZADO' THEN 1 END), 0),
    COALESCE(COUNT(CASE WHEN a.tipo_agendamento = 'EXAME' AND a.status = 'REALIZADO' THEN 1 END), 0),
    COALESCE(COUNT(CASE WHEN i.data_alta IS NOT NULL THEN 1 END), 0),
    COALESCE(COUNT(CASE WHEN st.status = 'FINALIZADA' THEN 1 END), 0),
    COALESCE(AVG(CASE WHEN if.tipo = 'RECEITA' AND if.categoria = 'CONSULTA' THEN if.valor END), 0),
    COALESCE((
                 SELECT ROUND(AVG(
                                      CASE WHEN l.status = 'OCUPADO' THEN 100.0 ELSE 0.0 END
                              ), 2)
                 FROM leitos l
                 WHERE l.unidade_id = unit_record.id
             ), 0),
    0 -- system generated
FROM unidades u
         LEFT JOIN itens_financeiros if ON u.id = if.unidade_id
    AND DATE_TRUNC('month', if.data_lancamento) = (current_month || '-01')::DATE
        LEFT JOIN agendamentos a ON u.id = a.unidade_id
    AND DATE_TRUNC('month', a.data_hora) = (current_month || '-01')::DATE
    LEFT JOIN internacoes i ON u.id = i.unidade_id
    AND DATE_TRUNC('month', i.data_internacao) = (current_month || '-01')::DATE
    LEFT JOIN sessoes_telemedicina st ON st.profissional_id IN (
    SELECT id FROM profissionais WHERE unidade_id = u.id
    ) AND DATE_TRUNC('month', st.data_criacao) = (current_month || '-01')::DATE
WHERE u.id = unit_record.id
GROUP BY u.id
ON CONFLICT (unidade_id, periodo) DO NOTHING;
END LOOP;
END;
$$ LANGUAGE plpgsql;

-- =====================================================
-- 15. COMMENTS AND DOCUMENTATION
-- =====================================================

COMMENT ON DATABASE postgres IS 'SGHSS - Sistema de Gestão Hospitalar e de Serviços de Saúde';

COMMENT ON TABLE pacientes IS 'Cadastro de pacientes do sistema hospitalar';
COMMENT ON TABLE profissionais IS 'Cadastro de profissionais de saúde';
COMMENT ON TABLE agendamentos IS 'Sistema de agendamento de consultas, exames e procedimentos';
COMMENT ON TABLE prontuarios IS 'Prontuários médicos dos pacientes';
COMMENT ON TABLE sessoes_telemedicina IS 'Sessões de telemedicina e teleconsultas';
COMMENT ON TABLE leitos IS 'Cadastro e controle de leitos hospitalares';
COMMENT ON TABLE internacoes IS 'Registro de internações hospitalares';
COMMENT ON TABLE suprimentos IS 'Controle de estoque de medicamentos e materiais';
COMMENT ON TABLE audit_logs IS 'Logs de auditoria para compliance LGPD';

-- Performance statistics
COMMENT ON INDEX idx_pacientes_cpf IS 'Índice para busca rápida de pacientes por CPF';
COMMENT ON INDEX idx_agendamentos_data_hora IS 'Índice para otimizar consultas por data/hora';
COMMENT ON INDEX idx_prontuarios_paciente IS 'Índice para histórico médico do paciente';

-- =====================================================
-- END OF SCHEMA
-- =====================================================
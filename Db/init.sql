-- CREATE TABLE pacientes (
--   id SERIAL PRIMARY KEY,
--   nome TEXT NOT NULL,
--   cpf TEXT NOT NULL UNIQUE,
--   data_nascimento DATE NOT NULL,
--   plano_saude TEXT,
--   data_criacao TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
--   data_atualizacao TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
-- );

-- CREATE TABLE prontuarios (
--   id SERIAL PRIMARY KEY,
--   paciente_id INTEGER REFERENCES pacientes(id),
--   profissional_id INTEGER,
--   data timestamp with time zone NOT NULL,
--   conteudo JSONB NOT NULL,
--   data_criacao TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
--   data_atualizacao TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
-- );

-- CREATE TABLE profissionais (
--   id SERIAL PRIMARY KEY,
--   nome TEXT NOT NULL,
--   especialidade TEXT NOT NULL,
--   crm TEXT NOT NULL UNIQUE,
--   data_criacao TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
--   data_atualizacao TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
-- );

-- CREATE TABLE consultas (
--   id SERIAL PRIMARY KEY,
--   paciente_id INTEGER REFERENCES pacientes(id),
--   profissional_id INTEGER REFERENCES profissionais(id),
--   data_hora TIMESTAMP NOT NULL,
--   status TEXT NOT NULL CHECK (status IN ('agendada', 'realizada', 'cancelada')),
--   observacoes TEXT NOT NULL DEFAULT 'Nenhuma observação',
--   data_criacao TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
--   data_atualizacao TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
-- );

-- CREATE TABLE exames (
--   id SERIAL PRIMARY KEY,
--   paciente_id INTEGER REFERENCES pacientes(id),
--   tipo TEXT NOT NULL,
--   data timestamp with time zone NOT NULL,
--   resultado JSONB
-- );

-- CREATE TABLE medicamentos (
--   id SERIAL PRIMARY KEY,
--   paciente_id INTEGER REFERENCES pacientes(id),
--   nome TEXT NOT NULL,
--   dosagem TEXT NOT NULL,
--   frequencia TEXT NOT NULL
-- );

-- CREATE TABLE receitas (
--   id SERIAL PRIMARY KEY,
--   paciente_id INTEGER REFERENCES pacientes(id),
--   profissional_id INTEGER REFERENCES profissionais(id),
--   data_vencimento_validade TIMESTAMP WITH TIME ZONE NOT NULL,  
--   data_criacao TIMESTAMP WITH TIME ZONE NOT NULL,
--   medicamentos JSONB NOT NULL,
--   orientacoes TEXT NOT NULL
-- );
-- CREATE TABLE videochamadas (
--   id SERIAL PRIMARY KEY,
--   paciente_id INTEGER REFERENCES pacientes(id),
--   profissional_id INTEGER REFERENCES profissionais(id),
--   data_hora TIMESTAMP WITH TIME ZONE NOT NULL,
--   link TEXT NOT NULL
-- );
-- CREATE TABLE atendimentos (
--   id SERIAL PRIMARY KEY,
--   paciente_id INTEGER REFERENCES pacientes(id),
--   profissional_id INTEGER REFERENCES profissionais(id),
--   data_hora TIMESTAMP WITH TIME ZONE NOT NULL,
--   tipo TEXT NOT NULL CHECK (tipo IN ('presencial', 'telemedicina')),
--   descricao TEXT
-- );
-- CREATE TABLE historico_medico (
--   id SERIAL PRIMARY KEY,
--   paciente_id INTEGER REFERENCES pacientes(id),
--   condicoes_preexistentes TEXT,
--   alergias TEXT,
--   cirurgias_anteriores TEXT,
--   medicamentos_atuais TEXT,
--   data_criacao TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
--   data_atualizacao TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
-- );
-- Não sei se será necessário, talvez faça até em outro banco de dados

-- CREATE TABLE notificacoes (
--   id SERIAL PRIMARY KEY,
--   paciente_id INTEGER REFERENCES pacientes(id),
--   mensagem TEXT NOT NULL,
--   data_hora TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
--   lida BOOLEAN NOT NULL DEFAULT FALSE
-- );


-- Unidades
CREATE TABLE unidades (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(200) NOT NULL,
    cnpj VARCHAR(14) UNIQUE NOT NULL,
    tipo_unidade VARCHAR(50) NOT NULL,
    endereco TEXT NOT NULL,
    telefone VARCHAR(20),
    email VARCHAR(100),
    responsavel VARCHAR(200),
    ativa BOOLEAN DEFAULT true,
    data_cadastro TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Endereços
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

-- Pacientes
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

-- Especialidades
CREATE TABLE especialidades (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(100) NOT NULL,
    codigo VARCHAR(10) UNIQUE NOT NULL,
    conselho_regulamentador VARCHAR(50)
);

-- Profissionais
CREATE TABLE profissionais (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(200) NOT NULL,
    cpf VARCHAR(11) UNIQUE NOT NULL,
    crm VARCHAR(20),
    tipo_profissional VARCHAR(50) NOT NULL,
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

-- Agendamentos
CREATE TABLE agendamentos (
    id SERIAL PRIMARY KEY,
    paciente_id INTEGER REFERENCES pacientes(id) NOT NULL,
    profissional_id INTEGER REFERENCES profissionais(id) NOT NULL,
    tipo_agendamento VARCHAR(50) NOT NULL,
    data_hora TIMESTAMP NOT NULL,
    duracao INTERVAL NOT NULL DEFAULT '30 minutes',
    status VARCHAR(20) NOT NULL DEFAULT 'AGENDADO',
    observacoes TEXT,
    unidade_id INTEGER REFERENCES unidades(id) NOT NULL,
    sala_id INTEGER,
    valor_consulta DECIMAL(10,2),
    plano_saude_cobertura BOOLEAN DEFAULT false,
    data_cadastro TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    data_atualizacao TIMESTAMP
);

-- Prontuários
CREATE TABLE prontuarios (
    id SERIAL PRIMARY KEY,
    paciente_id INTEGER REFERENCES pacientes(id) NOT NULL,
    profissional_id INTEGER REFERENCES profissionais(id) NOT NULL,
    data_atendimento TIMESTAMP NOT NULL,
    tipo_atendimento VARCHAR(50) NOT NULL,
    queixa_principal TEXT NOT NULL,
    historia_doenca_atual TEXT,
    exame_fisico TEXT,
    cid10 VARCHAR(10),
    observacoes TEXT,
    plano_tratamento TEXT,
    seguimento TEXT,
    unidade_id INTEGER REFERENCES unidades(id) NOT NULL,
    assinado BOOLEAN DEFAULT false,
    assinado_em TIMESTAMP,
    data_cadastro TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    data_atualizacao TIMESTAMP
);

-- Auditoria
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

-- Índices para performance
CREATE INDEX idx_pacientes_cpf ON pacientes(cpf);
CREATE INDEX idx_agendamentos_data ON agendamentos(data_hora);
CREATE INDEX idx_prontuarios_paciente ON prontuarios(paciente_id);
CREATE INDEX idx_audit_timestamp ON audit_logs(timestamp);
CREATE INDEX idx_profissionais_ativo ON profissionais(ativo, unidade_id);
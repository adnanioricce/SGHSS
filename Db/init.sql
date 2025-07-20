CREATE TABLE pacientes (
  id SERIAL PRIMARY KEY,
  nome TEXT NOT NULL,
  cpf TEXT NOT NULL UNIQUE,
  data_nascimento DATE NOT NULL,
  plano_saude TEXT,
  data_criacao TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
  data_atualizacao TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE prontuarios (
  id SERIAL PRIMARY KEY,
  paciente_id INTEGER REFERENCES pacientes(id),
  profissional_id INTEGER,
  data timestamp with time zone NOT NULL,
  conteudo JSONB NOT NULL,
  data_criacao TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
  data_atualizacao TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE profissionais (
  id SERIAL PRIMARY KEY,
  nome TEXT NOT NULL,
  especialidade TEXT NOT NULL,
  crm TEXT NOT NULL UNIQUE,
  data_criacao TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
  data_atualizacao TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE consultas (
  id SERIAL PRIMARY KEY,
  paciente_id INTEGER REFERENCES pacientes(id),
  profissional_id INTEGER REFERENCES profissionais(id),
  data_hora TIMESTAMP NOT NULL,
  status TEXT NOT NULL CHECK (status IN ('agendada', 'realizada', 'cancelada')),
  observacoes TEXT NOT NULL DEFAULT 'Nenhuma observação',
  data_criacao TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
  data_atualizacao TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE exames (
  id SERIAL PRIMARY KEY,
  paciente_id INTEGER REFERENCES pacientes(id),
  tipo TEXT NOT NULL,
  data timestamp with time zone NOT NULL,
  resultado JSONB
);

CREATE TABLE medicamentos (
  id SERIAL PRIMARY KEY,
  paciente_id INTEGER REFERENCES pacientes(id),
  nome TEXT NOT NULL,
  dosagem TEXT NOT NULL,
  frequencia TEXT NOT NULL
);

CREATE TABLE receitas (
  id SERIAL PRIMARY KEY,
  paciente_id INTEGER REFERENCES pacientes(id),
  profissional_id INTEGER REFERENCES profissionais(id),
  data_vencimento_validade TIMESTAMP WITH TIME ZONE NOT NULL,  
  data_criacao TIMESTAMP WITH TIME ZONE NOT NULL,
  medicamentos JSONB NOT NULL,
  orientacoes TEXT NOT NULL
);
CREATE TABLE videochamadas (
  id SERIAL PRIMARY KEY,
  paciente_id INTEGER REFERENCES pacientes(id),
  profissional_id INTEGER REFERENCES profissionais(id),
  data_hora TIMESTAMP WITH TIME ZONE NOT NULL,
  link TEXT NOT NULL
);
CREATE TABLE atendimentos (
  id SERIAL PRIMARY KEY,
  paciente_id INTEGER REFERENCES pacientes(id),
  profissional_id INTEGER REFERENCES profissionais(id),
  data_hora TIMESTAMP WITH TIME ZONE NOT NULL,
  tipo TEXT NOT NULL CHECK (tipo IN ('presencial', 'telemedicina')),
  descricao TEXT
);
CREATE TABLE historico_medico (
  id SERIAL PRIMARY KEY,
  paciente_id INTEGER REFERENCES pacientes(id),
  condicoes_preexistentes TEXT,
  alergias TEXT,
  cirurgias_anteriores TEXT,
  medicamentos_atuais TEXT,
  data_criacao TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
  data_atualizacao TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);
-- Não sei se será necessário, talvez faça até em outro banco de dados

-- CREATE TABLE notificacoes (
--   id SERIAL PRIMARY KEY,
--   paciente_id INTEGER REFERENCES pacientes(id),
--   mensagem TEXT NOT NULL,
--   data_hora TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
--   lida BOOLEAN NOT NULL DEFAULT FALSE
-- );
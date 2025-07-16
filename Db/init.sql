CREATE TABLE pacientes (
  id SERIAL PRIMARY KEY,
  nome TEXT NOT NULL,
  cpf TEXT NOT NULL UNIQUE,
  data_nascimento DATE NOT NULL,
  plano_saude TEXT
);

CREATE TABLE prontuarios (
  id SERIAL PRIMARY KEY,
  paciente_id INTEGER REFERENCES pacientes(id),
  profissional_id INTEGER,
  data TIMESTAMP NOT NULL,
  conteudo JSONB NOT NULL
);


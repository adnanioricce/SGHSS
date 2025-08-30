namespace Domains.Prontuario

open System
open Infrastructure.Database

module Models =
    type TipoAtendimento = 
        | Consulta 
        | Exame 
        | Internacao 
        | Emergencia 
        | Teleconsulta
    
    type Prontuario = {
        Id: int
        PacienteId: int
        ProfissionalId: int
        DataAtendimento: DateTime
        TipoAtendimento: TipoAtendimento
        QueixaPrincipal: string
        HistoriaDoencaAtual: string
        ExameFisico: string option
        Hipoteses: string list
        CID10: string option
        Prescricoes: Prescricao list
        ExamesSolicitados: ExameSolicitado list
        Procedimentos: Procedimento list
        Observacoes: string option
        PlanoTratamento: string option
        Seguimento: string option
        UnidadeId: int
        DataCadastro: DateTime
        DataAtualizacao: DateTime option
        Assinado: bool
        AssinadoEm: DateTime option
        AgendamentoId: int option
    }
    
    and Prescricao = {
        Id: int
        ProntuarioId: int
        Medicamento: string
        Dosagem: string
        Frequencia: string
        Duracao: string
        Orientacoes: string option
        Ativo: bool
        DataCriacao: DateTime
        DataVencimento: DateTime option
    }
    
    and ExameSolicitado = {
        Id: int
        ProntuarioId: int
        TipoExame: string
        Descricao: string
        Urgente: bool
        Observacoes: string option
        DataSolicitacao: DateTime
        Realizado: bool
        DataRealizacao: DateTime option
        Resultado: string option
        ArquivoResultado: string option
        LaboratorioId: int option
    }
    
    and Procedimento = {
        Id: int
        ProntuarioId: int
        Nome: string
        Codigo: string option // TUSS ou similar
        Descricao: string
        DataRealizacao: DateTime
        ProfissionalId: int
        Observacoes: string option
        Valor: decimal option
        Status: string // SOLICITADO, AUTORIZADO, REALIZADO, CANCELADO
    }

    // Input types
    type ProntuarioInput = {
        PacienteId: int
        ProfissionalId: int
        DataAtendimento: DateTime
        TipoAtendimento: TipoAtendimento
        QueixaPrincipal: string
        HistoriaDoencaAtual: string
        ExameFisico: string option
        Hipoteses: string list
        CID10: string option
        Observacoes: string option
        PlanoTratamento: string option
        Seguimento: string option
        UnidadeId: int
        AgendamentoId: int option
    }

    type PrescricaoInput = {
        Medicamento: string
        Dosagem: string
        Frequencia: string
        Duracao: string
        Orientacoes: string option
        DataVencimento: DateTime option
    }

    type ExameSolicitadoInput = {
        TipoExame: string
        Descricao: string
        Urgente: bool
        Observacoes: string option
        LaboratorioId: int option
    }

    type ProcedimentoInput = {
        Nome: string
        Codigo: string option
        Descricao: string
        DataRealizacao: DateTime
        ProfissionalId: int
        Observacoes: string option
        Valor: decimal option
    }

    // Views com relacionamentos
    type ProntuarioDetalhes = {
        Id: int
        Paciente: {| Id: int; Nome: string; CPF: string; DataNascimento: DateTime |}
        Profissional: {| Id: int; Nome: string; CRM: string option; Especialidade: string option |}
        DataAtendimento: DateTime
        TipoAtendimento: TipoAtendimento
        QueixaPrincipal: string
        HistoriaDoencaAtual: string
        ExameFisico: string option
        Hipoteses: string list
        CID10: string option
        Prescricoes: Prescricao list
        ExamesSolicitados: ExameSolicitado list
        Procedimentos: Procedimento list
        Observacoes: string option
        PlanoTratamento: string option
        Seguimento: string option
        Unidade: {| Id: int; Nome: string |}
        DataCadastro: DateTime
        DataAtualizacao: DateTime option
        Assinado: bool
        AssinadoEm: DateTime option
    }

    type HistoricoPaciente = {
        PacienteId: int
        PacienteNome: string
        TotalConsultas: int
        UltimaConsulta: DateTime option
        Prontuarios: ProntuarioDetalhes list
        PrescricoesAtivas: Prescricao list
        ExamesPendentes: ExameSolicitado list
    }

// Prontuario/Repository.fs
module Repository =
    open Npgsql.FSharp
    open Models
    open System    

    // Funções auxiliares para conversão de enums
    let private parseTipoAtendimento (tipo: string) =
        match tipo.ToUpper() with
        | "CONSULTA" -> Consulta
        | "EXAME" -> Exame
        | "INTERNACAO" -> Internacao
        | "EMERGENCIA" -> Emergencia
        | "TELECONSULTA" -> Teleconsulta
        | _ -> Consulta

    let private tipoAtendimentoToString (tipo: TipoAtendimento) =
        match tipo with
        | Consulta -> "CONSULTA"
        | Exame -> "EXAME"
        | Internacao -> "INTERNACAO"
        | Emergencia -> "EMERGENCIA"
        | Teleconsulta -> "TELECONSULTA"

    // Repository functions for Prontuario
    let getAll (pacienteId: int option) (profissionalId: int option) (dataInicio: DateTime option) (dataFim: DateTime option) =
        task {
            let mutable query = """
                SELECT id, paciente_id, profissional_id, data_atendimento, tipo_atendimento,
                       queixa_principal, historia_doenca_atual, exame_fisico, hipoteses, cid10,
                       observacoes, plano_tratamento, seguimento, unidade_id, data_cadastro,
                       data_atualizacao, assinado, assinado_em, agendamento_id
                FROM prontuarios 
                WHERE 1=1
            """
            let mutable parameters = []

            match pacienteId with
            | Some pid ->
                query <- query + " AND paciente_id = @paciente_id"
                parameters <- ("paciente_id", Sql.int pid) :: parameters
            | None -> ()

            match profissionalId with
            | Some prid ->
                query <- query + " AND profissional_id = @profissional_id"
                parameters <- ("profissional_id", Sql.int prid) :: parameters
            | None -> ()

            match dataInicio with
            | Some inicio ->
                query <- query + " AND data_atendimento >= @data_inicio"
                parameters <- ("data_inicio", Sql.timestamp inicio) :: parameters
            | None -> ()

            match dataFim with
            | Some fim ->
                query <- query + " AND data_atendimento <= @data_fim"
                parameters <- ("data_fim", Sql.timestamp fim) :: parameters
            | None -> ()

            query <- query + " ORDER BY data_atendimento DESC"

            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query query
                |> Sql.parameters parameters
                |> Sql.executeAsync (fun read -> 
                    let hipotesesStr = read.stringOrNone "hipoteses" |> Option.defaultValue ""
                    let hipoteses = 
                        if String.IsNullOrWhiteSpace(hipotesesStr) then []
                        else hipotesesStr.Split(';') |> Array.toList |> List.filter (fun h -> not (String.IsNullOrWhiteSpace(h)))
                    
                    {
                        Id = read.int "id"
                        PacienteId = read.int "paciente_id"
                        ProfissionalId = read.int "profissional_id"
                        DataAtendimento = read.dateTime "data_atendimento"
                        TipoAtendimento = parseTipoAtendimento (read.string "tipo_atendimento")
                        QueixaPrincipal = read.string "queixa_principal"
                        HistoriaDoencaAtual = read.string "historia_doenca_atual"
                        ExameFisico = read.stringOrNone "exame_fisico"
                        Hipoteses = hipoteses
                        CID10 = read.stringOrNone "cid10"
                        Prescricoes = [] // Carregado separadamente
                        ExamesSolicitados = [] // Carregado separadamente
                        Procedimentos = [] // Carregado separadamente
                        Observacoes = read.stringOrNone "observacoes"
                        PlanoTratamento = read.stringOrNone "plano_tratamento"
                        Seguimento = read.stringOrNone "seguimento"
                        UnidadeId = read.int "unidade_id"
                        DataCadastro = read.dateTime "data_cadastro"
                        DataAtualizacao = read.dateTimeOrNone "data_atualizacao"
                        Assinado = read.bool "assinado"
                        AssinadoEm = read.dateTimeOrNone "assinado_em"
                        AgendamentoId = read.intOrNone "agendamento_id"
                    })
        }
    let getPrescricoesByProntuario (prontuarioId: int) =
        task {
            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT id, prontuario_id, medicamento, dosagem, frequencia, duracao,
                           orientacoes, ativo, data_criacao, data_vencimento
                    FROM prescricoes 
                    WHERE prontuario_id = @prontuario_id
                    ORDER BY data_criacao DESC
                """
                |> Sql.parameters ["prontuario_id", Sql.int prontuarioId]
                |> Sql.executeAsync (fun read -> {
                    Id = read.int "id"
                    ProntuarioId = read.int "prontuario_id"
                    Medicamento = read.string "medicamento"
                    Dosagem = read.string "dosagem"
                    Frequencia = read.string "frequencia"
                    Duracao = read.string "duracao"
                    Orientacoes = read.stringOrNone "orientacoes"
                    Ativo = read.bool "ativo"
                    DataCriacao = read.dateTime "data_criacao"
                    DataVencimento = read.dateTimeOrNone "data_vencimento"
                })
        }
    let getExamesSolicitadosByProntuario (prontuarioId: int) =
        task {
            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT id, prontuario_id, tipo_exame, descricao, urgente, observacoes,
                           data_solicitacao, realizado, data_realizacao, resultado,
                           arquivo_resultado, laboratorio_id
                    FROM exames_solicitados 
                    WHERE prontuario_id = @prontuario_id
                    ORDER BY data_solicitacao DESC
                """
                |> Sql.parameters ["prontuario_id", Sql.int prontuarioId]
                |> Sql.executeAsync (fun read -> {
                    Id = read.int "id"
                    ProntuarioId = read.int "prontuario_id"
                    TipoExame = read.string "tipo_exame"
                    Descricao = read.string "descricao"
                    Urgente = read.bool "urgente"
                    Observacoes = read.stringOrNone "observacoes"
                    DataSolicitacao = read.dateTime "data_solicitacao"
                    Realizado = read.bool "realizado"
                    DataRealizacao = read.dateTimeOrNone "data_realizacao"
                    Resultado = read.stringOrNone "resultado"
                    ArquivoResultado = read.stringOrNone "arquivo_resultado"
                    LaboratorioId = read.intOrNone "laboratorio_id"
                })
        }
    let getProcedimentosByProntuario (prontuarioId: int) =
        task {
            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT id, prontuario_id, nome, codigo, descricao, data_realizacao,
                           profissional_id, observacoes, valor, status
                    FROM procedimentos 
                    WHERE prontuario_id = @prontuario_id
                    ORDER BY data_realizacao DESC
                """
                |> Sql.parameters ["prontuario_id", Sql.int prontuarioId]
                |> Sql.executeAsync (fun read -> {
                    Id = read.int "id"
                    ProntuarioId = read.int "prontuario_id"
                    Nome = read.string "nome"
                    Codigo = read.stringOrNone "codigo"
                    Descricao = read.string "descricao"
                    DataRealizacao = read.dateTime "data_realizacao"
                    ProfissionalId = read.int "profissional_id"
                    Observacoes = read.stringOrNone "observacoes"
                    Valor = read.decimalOrNone "valor"
                    Status = read.string "status"
                })
        }
    let getById (id: int) =
        task {
            let! prontuario =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT id, paciente_id, profissional_id, data_atendimento, tipo_atendimento,
                           queixa_principal, historia_doenca_atual, exame_fisico, hipoteses, cid10,
                           observacoes, plano_tratamento, seguimento, unidade_id, data_cadastro,
                           data_atualizacao, assinado, assinado_em, agendamento_id
                    FROM prontuarios 
                    WHERE id = @id
                """
                |> Sql.parameters ["id", Sql.int id]
                |> Sql.executeRowAsync (fun read -> 
                    let hipotesesStr = read.stringOrNone "hipoteses" |> Option.defaultValue ""
                    let hipoteses = 
                        if String.IsNullOrWhiteSpace(hipotesesStr) then []
                        else hipotesesStr.Split(';') |> Array.toList |> List.filter (fun h -> not (String.IsNullOrWhiteSpace(h)))
                    
                    {
                        Id = read.int "id"
                        PacienteId = read.int "paciente_id"
                        ProfissionalId = read.int "profissional_id"
                        DataAtendimento = read.dateTime "data_atendimento"
                        TipoAtendimento = parseTipoAtendimento (read.string "tipo_atendimento")
                        QueixaPrincipal = read.string "queixa_principal"
                        HistoriaDoencaAtual = read.string "historia_doenca_atual"
                        ExameFisico = read.stringOrNone "exame_fisico"
                        Hipoteses = hipoteses
                        CID10 = read.stringOrNone "cid10"
                        Prescricoes = []
                        ExamesSolicitados = []
                        Procedimentos = []
                        Observacoes = read.stringOrNone "observacoes"
                        PlanoTratamento = read.stringOrNone "plano_tratamento"
                        Seguimento = read.stringOrNone "seguimento"
                        UnidadeId = read.int "unidade_id"
                        DataCadastro = read.dateTime "data_cadastro"
                        DataAtualizacao = read.dateTimeOrNone "data_atualizacao"
                        Assinado = read.bool "assinado"
                        AssinadoEm = read.dateTimeOrNone "assinado_em"
                        AgendamentoId = read.intOrNone "agendamento_id"
                    })

            // Carregar prescrições
            let! prescricoes = getPrescricoesByProntuario id
            
            // Carregar exames solicitados
            let! exames = getExamesSolicitadosByProntuario id
            
            // Carregar procedimentos
            let! procedimentos = getProcedimentosByProntuario id

            return { prontuario with 
                        Prescricoes = prescricoes
                        ExamesSolicitados = exames
                        Procedimentos = procedimentos }
        }    
    
    

    let insert (input: ProntuarioInput) =
        task {
            let hipotesesStr = String.Join(";", input.Hipoteses)
            
            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    INSERT INTO prontuarios 
                    (paciente_id, profissional_id, data_atendimento, tipo_atendimento,
                     queixa_principal, historia_doenca_atual, exame_fisico, hipoteses, cid10,
                     observacoes, plano_tratamento, seguimento, unidade_id, agendamento_id)
                    VALUES 
                    (@paciente_id, @profissional_id, @data_atendimento, @tipo_atendimento,
                     @queixa_principal, @historia_doenca_atual, @exame_fisico, @hipoteses, @cid10,
                     @observacoes, @plano_tratamento, @seguimento, @unidade_id, @agendamento_id)
                    RETURNING id
                """
                |> Sql.parameters [
                    "paciente_id", Sql.int input.PacienteId
                    "profissional_id", Sql.int input.ProfissionalId
                    "data_atendimento", Sql.timestamp input.DataAtendimento
                    "tipo_atendimento", Sql.string (tipoAtendimentoToString input.TipoAtendimento)
                    "queixa_principal", Sql.string input.QueixaPrincipal
                    "historia_doenca_atual", Sql.string input.HistoriaDoencaAtual
                    "exame_fisico", Sql.stringOrNone input.ExameFisico
                    "hipoteses", Sql.string hipotesesStr
                    "cid10", Sql.stringOrNone input.CID10
                    "observacoes", Sql.stringOrNone input.Observacoes
                    "plano_tratamento", Sql.stringOrNone input.PlanoTratamento
                    "seguimento", Sql.stringOrNone input.Seguimento
                    "unidade_id", Sql.int input.UnidadeId
                    "agendamento_id", Sql.intOrNone input.AgendamentoId
                ]
                |> Sql.executeRowAsync (fun read -> read.int "id")
        }

    let update (id: int) (input: ProntuarioInput) =
        task {
            let hipotesesStr = String.Join(";", input.Hipoteses)
            
            let! rowsAffected =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    UPDATE prontuarios 
                    SET paciente_id = @paciente_id,
                        profissional_id = @profissional_id,
                        data_atendimento = @data_atendimento,
                        tipo_atendimento = @tipo_atendimento,
                        queixa_principal = @queixa_principal,
                        historia_doenca_atual = @historia_doenca_atual,
                        exame_fisico = @exame_fisico,
                        hipoteses = @hipoteses,
                        cid10 = @cid10,
                        observacoes = @observacoes,
                        plano_tratamento = @plano_tratamento,
                        seguimento = @seguimento,
                        unidade_id = @unidade_id,
                        data_atualizacao = CURRENT_TIMESTAMP
                    WHERE id = @id AND assinado = false
                """
                |> Sql.parameters [
                    "id", Sql.int id
                    "paciente_id", Sql.int input.PacienteId
                    "profissional_id", Sql.int input.ProfissionalId
                    "data_atendimento", Sql.timestamp input.DataAtendimento
                    "tipo_atendimento", Sql.string (tipoAtendimentoToString input.TipoAtendimento)
                    "queixa_principal", Sql.string input.QueixaPrincipal
                    "historia_doenca_atual", Sql.string input.HistoriaDoencaAtual
                    "exame_fisico", Sql.stringOrNone input.ExameFisico
                    "hipoteses", Sql.string hipotesesStr
                    "cid10", Sql.stringOrNone input.CID10
                    "observacoes", Sql.stringOrNone input.Observacoes
                    "plano_tratamento", Sql.stringOrNone input.PlanoTratamento
                    "seguimento", Sql.stringOrNone input.Seguimento
                    "unidade_id", Sql.int input.UnidadeId
                ]
                |> Sql.executeNonQueryAsync
            
            return rowsAffected > 0
        }

    let assinarProntuario (id: int) (profissionalId: int) =
        task {
            let! rowsAffected =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    UPDATE prontuarios 
                    SET assinado = true,
                        assinado_em = CURRENT_TIMESTAMP,
                        data_atualizacao = CURRENT_TIMESTAMP
                    WHERE id = @id AND profissional_id = @profissional_id AND assinado = false
                """
                |> Sql.parameters [
                    "id", Sql.int id
                    "profissional_id", Sql.int profissionalId
                ]
                |> Sql.executeNonQueryAsync
            
            return rowsAffected > 0
        }

    // Prescrições
    let insertPrescricao (prontuarioId: int) (input: PrescricaoInput) =
        task {
            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    INSERT INTO prescricoes 
                    (prontuario_id, medicamento, dosagem, frequencia, duracao, orientacoes, data_vencimento)
                    VALUES 
                    (@prontuario_id, @medicamento, @dosagem, @frequencia, @duracao, @orientacoes, @data_vencimento)
                    RETURNING id
                """
                |> Sql.parameters [
                    "prontuario_id", Sql.int prontuarioId
                    "medicamento", Sql.string input.Medicamento
                    "dosagem", Sql.string input.Dosagem
                    "frequencia", Sql.string input.Frequencia
                    "duracao", Sql.string input.Duracao
                    "orientacoes", Sql.stringOrNone input.Orientacoes
                    "data_vencimento", Sql.timestampOrNone input.DataVencimento
                ]
                |> Sql.executeRowAsync (fun read -> read.int "id")
        }

    let inactivatePrescricao (prescricaoId: int) =
        task {
            let! rowsAffected =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    UPDATE prescricoes 
                    SET ativo = false
                    WHERE id = @id
                """
                |> Sql.parameters ["id", Sql.int prescricaoId]
                |> Sql.executeNonQueryAsync
            
            return rowsAffected > 0
        }

    // Exames Solicitados
    let insertExameSolicitado (prontuarioId: int) (input: ExameSolicitadoInput) =
        task {
            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    INSERT INTO exames_solicitados 
                    (prontuario_id, tipo_exame, descricao, urgente, observacoes, laboratorio_id)
                    VALUES 
                    (@prontuario_id, @tipo_exame, @descricao, @urgente, @observacoes, @laboratorio_id)
                    RETURNING id
                """
                |> Sql.parameters [
                    "prontuario_id", Sql.int prontuarioId
                    "tipo_exame", Sql.string input.TipoExame
                    "descricao", Sql.string input.Descricao
                    "urgente", Sql.bool input.Urgente
                    "observacoes", Sql.stringOrNone input.Observacoes
                    "laboratorio_id", Sql.intOrNone input.LaboratorioId
                ]
                |> Sql.executeRowAsync (fun read -> read.int "id")
        }

    let adicionarResultadoExame (exameId: int) (resultado: string) (arquivoUrl: string option) =
        task {
            let! rowsAffected =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    UPDATE exames_solicitados 
                    SET realizado = true,
                        data_realizacao = CURRENT_TIMESTAMP,
                        resultado = @resultado,
                        arquivo_resultado = @arquivo_resultado
                    WHERE id = @id
                """
                |> Sql.parameters [
                    "id", Sql.int exameId
                    "resultado", Sql.string resultado
                    "arquivo_resultado", Sql.stringOrNone arquivoUrl
                ]
                |> Sql.executeNonQueryAsync
            
            return rowsAffected > 0
        }

    // Procedimentos
    let insertProcedimento (prontuarioId: int) (input: ProcedimentoInput) =
        task {
            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    INSERT INTO procedimentos 
                    (prontuario_id, nome, codigo, descricao, data_realizacao, profissional_id, observacoes, valor)
                    VALUES 
                    (@prontuario_id, @nome, @codigo, @descricao, @data_realizacao, @profissional_id, @observacoes, @valor)
                    RETURNING id
                """
                |> Sql.parameters [
                    "prontuario_id", Sql.int prontuarioId
                    "nome", Sql.string input.Nome
                    "codigo", Sql.stringOrNone input.Codigo
                    "descricao", Sql.string input.Descricao
                    "data_realizacao", Sql.timestamp input.DataRealizacao
                    "profissional_id", Sql.int input.ProfissionalId
                    "observacoes", Sql.stringOrNone input.Observacoes
                    "valor", Sql.decimalOrNone input.Valor
                ]
                |> Sql.executeRowAsync (fun read -> read.int "id")
        }

    // Histórico do Paciente
    let getHistoricoPaciente (pacienteId: int) =
        task {
            let! prontuarios = getAll (Some pacienteId) None None None
            
            let! prescricoesAtivas =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT p.id, p.prontuario_id, p.medicamento, p.dosagem, p.frequencia, p.duracao,
                           p.orientacoes, p.ativo, p.data_criacao, p.data_vencimento
                    FROM prescricoes p
                    INNER JOIN prontuarios pr ON p.prontuario_id = pr.id
                    WHERE pr.paciente_id = @paciente_id 
                    AND p.ativo = true 
                    AND (p.data_vencimento IS NULL OR p.data_vencimento > CURRENT_TIMESTAMP)
                    ORDER BY p.data_criacao DESC
                """
                |> Sql.parameters ["paciente_id", Sql.int pacienteId]
                |> Sql.executeAsync (fun read -> {
                    Id = read.int "id"
                    ProntuarioId = read.int "prontuario_id"
                    Medicamento = read.string "medicamento"
                    Dosagem = read.string "dosagem"
                    Frequencia = read.string "frequencia"
                    Duracao = read.string "duracao"
                    Orientacoes = read.stringOrNone "orientacoes"
                    Ativo = read.bool "ativo"
                    DataCriacao = read.dateTime "data_criacao"
                    DataVencimento = read.dateTimeOrNone "data_vencimento"
                })

            let! examesPendentes =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT e.id, e.prontuario_id, e.tipo_exame, e.descricao, e.urgente, e.observacoes,
                           e.data_solicitacao, e.realizado, e.data_realizacao, e.resultado,
                           e.arquivo_resultado, e.laboratorio_id
                    FROM exames_solicitados e
                    INNER JOIN prontuarios pr ON e.prontuario_id = pr.id
                    WHERE pr.paciente_id = @paciente_id 
                    AND e.realizado = false
                    ORDER BY e.data_solicitacao DESC, e.urgente DESC
                """
                |> Sql.parameters ["paciente_id", Sql.int pacienteId]
                |> Sql.executeAsync (fun read -> {
                    Id = read.int "id"
                    ProntuarioId = read.int "prontuario_id"
                    TipoExame = read.string "tipo_exame"
                    Descricao = read.string "descricao"
                    Urgente = read.bool "urgente"
                    Observacoes = read.stringOrNone "observacoes"
                    DataSolicitacao = read.dateTime "data_solicitacao"
                    Realizado = read.bool "realizado"
                    DataRealizacao = read.dateTimeOrNone "data_realizacao"
                    Resultado = read.stringOrNone "resultado"
                    ArquivoResultado = read.stringOrNone "arquivo_resultado"
                    LaboratorioId = read.intOrNone "laboratorio_id"
                })

            let! pacienteInfo =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT nome FROM pacientes WHERE id = @id
                """
                |> Sql.parameters ["id", Sql.int pacienteId]
                |> Sql.executeRowAsync (fun read -> read.string "nome")

            let ultimaConsulta = 
                prontuarios 
                |> List.map (fun p -> p.DataAtendimento)
                |> List.tryHead

            return {
                PacienteId = pacienteId
                PacienteNome = pacienteInfo
                TotalConsultas = prontuarios.Length
                UltimaConsulta = ultimaConsulta
                Prontuarios = [] // Será preenchido no handler se necessário
                PrescricoesAtivas = prescricoesAtivas
                ExamesPendentes = examesPendentes
            }
        }

module Handler =
    open Giraffe
    open Models

    // DTOs
    type ProntuarioResponse = {
        Id: int
        PacienteId: int
        ProfissionalId: int
        DataAtendimento: DateTime
        TipoAtendimento: string
        QueixaPrincipal: string
        HistoriaDoencaAtual: string
        ExameFisico: string option
        Hipoteses: string list
        CID10: string option
        Observacoes: string option
        PlanoTratamento: string option
        Seguimento: string option
        UnidadeId: int
        DataCadastro: DateTime
        DataAtualizacao: DateTime option
        Assinado: bool
        AssinadoEm: DateTime option
        AgendamentoId: int option
        Prescricoes: PrescricaoResponse list
        ExamesSolicitados: ExameSolicitadoResponse list
        Procedimentos: ProcedimentoResponse list
    }

    and PrescricaoResponse = {
        Id: int
        Medicamento: string
        Dosagem: string
        Frequencia: string
        Duracao: string
        Orientacoes: string option
        Ativo: bool
        DataCriacao: DateTime
        DataVencimento: DateTime option
    }

    and ExameSolicitadoResponse = {
        Id: int
        TipoExame: string
        Descricao: string
        Urgente: bool
        Observacoes: string option
        DataSolicitacao: DateTime
        Realizado: bool
        DataRealizacao: DateTime option
        Resultado: string option
        ArquivoResultado: string option
        LaboratorioId: int option
    }

    and ProcedimentoResponse = {
        Id: int
        Nome: string
        Codigo: string option
        Descricao: string
        DataRealizacao: DateTime
        ProfissionalId: int
        Observacoes: string option
        Valor: decimal option
        Status: string
    }

    type ProntuarioInputDto = {
        PacienteId: int
        ProfissionalId: int
        DataAtendimento: DateTime
        TipoAtendimento: string
        QueixaPrincipal: string
        HistoriaDoencaAtual: string
        ExameFisico: string option
        Hipoteses: string list
        CID10: string option
        Observacoes: string option
        PlanoTratamento: string option
        Seguimento: string option
        UnidadeId: int
        AgendamentoId: int option
        Prescricoes: PrescricaoInputDto list
        ExamesSolicitados: ExameSolicitadoInputDto list
        Procedimentos: ProcedimentoInputDto list
    }

    and PrescricaoInputDto = {
        Medicamento: string
        Dosagem: string
        Frequencia: string
        Duracao: string
        Orientacoes: string option
        DataVencimento: DateTime option
    }

    and ExameSolicitadoInputDto = {
        TipoExame: string
        Descricao: string
        Urgente: bool
        Observacoes: string option
        LaboratorioId: int option
    }

    and ProcedimentoInputDto = {
        Nome: string
        Codigo: string option
        Descricao: string
        DataRealizacao: DateTime
        ProfissionalId: int
        Observacoes: string option
        Valor: decimal option
    }

    // Funções auxiliares de conversão
    let private toResponse (prontuario: Prontuario) : ProntuarioResponse =
        let tipoString = 
            match prontuario.TipoAtendimento with
            | Consulta -> "CONSULTA"
            | Exame -> "EXAME"
            | Internacao -> "INTERNACAO"
            | Emergencia -> "EMERGENCIA"
            | Teleconsulta -> "TELECONSULTA"

        {
            Id = prontuario.Id
            PacienteId = prontuario.PacienteId
            ProfissionalId = prontuario.ProfissionalId
            DataAtendimento = prontuario.DataAtendimento
            TipoAtendimento = tipoString
            QueixaPrincipal = prontuario.QueixaPrincipal
            HistoriaDoencaAtual = prontuario.HistoriaDoencaAtual
            ExameFisico = prontuario.ExameFisico
            Hipoteses = prontuario.Hipoteses
            CID10 = prontuario.CID10
            Observacoes = prontuario.Observacoes
            PlanoTratamento = prontuario.PlanoTratamento
            Seguimento = prontuario.Seguimento
            UnidadeId = prontuario.UnidadeId
            DataCadastro = prontuario.DataCadastro
            DataAtualizacao = prontuario.DataAtualizacao
            Assinado = prontuario.Assinado
            AssinadoEm = prontuario.AssinadoEm
            AgendamentoId = prontuario.AgendamentoId
            Prescricoes = prontuario.Prescricoes |> List.map (fun p -> {
                Id = p.Id
                Medicamento = p.Medicamento
                Dosagem = p.Dosagem
                Frequencia = p.Frequencia
                Duracao = p.Duracao
                Orientacoes = p.Orientacoes
                Ativo = p.Ativo
                DataCriacao = p.DataCriacao
                DataVencimento = p.DataVencimento
            })
            ExamesSolicitados = prontuario.ExamesSolicitados |> List.map (fun e -> {
                Id = e.Id
                TipoExame = e.TipoExame
                Descricao = e.Descricao
                Urgente = e.Urgente
                Observacoes = e.Observacoes
                DataSolicitacao = e.DataSolicitacao
                Realizado = e.Realizado
                DataRealizacao = e.DataRealizacao
                Resultado = e.Resultado
                ArquivoResultado = e.ArquivoResultado
                LaboratorioId = e.LaboratorioId
            })
            Procedimentos = prontuario.Procedimentos |> List.map (fun p -> {
                Id = p.Id
                Nome = p.Nome
                Codigo = p.Codigo
                Descricao = p.Descricao
                DataRealizacao = p.DataRealizacao
                ProfissionalId = p.ProfissionalId
                Observacoes = p.Observacoes
                Valor = p.Valor
                Status = p.Status
            })
        }

    let private toDomainInput (dto: ProntuarioInputDto) : ProntuarioInput =
        let tipo = 
            match dto.TipoAtendimento.ToUpper() with
            | "CONSULTA" -> Consulta
            | "EXAME" -> Exame
            | "INTERNACAO" -> Internacao
            | "EMERGENCIA" -> Emergencia
            | "TELECONSULTA" -> Teleconsulta
            | _ -> failwith $"Tipo de atendimento inválido: {dto.TipoAtendimento}"

        {
            PacienteId = dto.PacienteId
            ProfissionalId = dto.ProfissionalId
            DataAtendimento = dto.DataAtendimento
            TipoAtendimento = tipo
            QueixaPrincipal = dto.QueixaPrincipal
            HistoriaDoencaAtual = dto.HistoriaDoencaAtual
            ExameFisico = dto.ExameFisico
            Hipoteses = dto.Hipoteses
            CID10 = dto.CID10
            Observacoes = dto.Observacoes
            PlanoTratamento = dto.PlanoTratamento
            Seguimento = dto.Seguimento
            UnidadeId = dto.UnidadeId
            AgendamentoId = dto.AgendamentoId
        }

    // Validações
    let private validateInput (dto: ProntuarioInputDto) =
        let errors = ResizeArray<string>()
        
        if dto.PacienteId <= 0 then
            errors.Add("Paciente deve ser especificado")
        
        if dto.ProfissionalId <= 0 then
            errors.Add("Profissional deve ser especificado")
        
        if String.IsNullOrWhiteSpace(dto.QueixaPrincipal) then
            errors.Add("Queixa principal é obrigatória")
        
        if String.IsNullOrWhiteSpace(dto.HistoriaDoencaAtual) then
            errors.Add("História da doença atual é obrigatória")
        
        if dto.DataAtendimento > DateTime.Now then
            errors.Add("Data de atendimento não pode ser no futuro")
        
        if dto.UnidadeId <= 0 then
            errors.Add("Unidade deve ser especificada")

        match dto.TipoAtendimento.ToUpper() with
        | "CONSULTA" | "EXAME" | "INTERNACAO" | "EMERGENCIA" | "TELECONSULTA" -> ()
        | _ -> errors.Add("Tipo de atendimento inválido")

        // Validar prescrições
        dto.Prescricoes |> List.iteri (fun i prescricao ->
            if String.IsNullOrWhiteSpace(prescricao.Medicamento) then
                errors.Add($"Prescrição {i+1}: Medicamento é obrigatório")
            if String.IsNullOrWhiteSpace(prescricao.Dosagem) then
                errors.Add($"Prescrição {i+1}: Dosagem é obrigatória")
            if String.IsNullOrWhiteSpace(prescricao.Frequencia) then
                errors.Add($"Prescrição {i+1}: Frequência é obrigatória")
        )

        // Validar exames solicitados
        dto.ExamesSolicitados |> List.iteri (fun i exame ->
            if String.IsNullOrWhiteSpace(exame.TipoExame) then
                errors.Add($"Exame {i+1}: Tipo de exame é obrigatório")
            if String.IsNullOrWhiteSpace(exame.Descricao) then
                errors.Add($"Exame {i+1}: Descrição é obrigatória")
        )

        errors |> Seq.toList

    // Handlers
    let getAllProntuarios : HttpHandler =
        fun next ctx ->
            task {
                try
                    let pacienteId = 
                        match ctx.TryGetQueryStringValue "pacienteId" with
                        | Some idStr -> Int32.TryParse(idStr) |> function | true, id -> Some id | _ -> None
                        | None -> None

                    let profissionalId = 
                        match ctx.TryGetQueryStringValue "profissionalId" with
                        | Some idStr -> Int32.TryParse(idStr) |> function | true, id -> Some id | _ -> None
                        | None -> None

                    let dataInicio = 
                        match ctx.TryGetQueryStringValue "dataInicio" with
                        | Some dateStr -> DateTime.TryParse(dateStr) |> function | true, dt -> Some dt | _ -> None
                        | None -> None

                    let dataFim = 
                        match ctx.TryGetQueryStringValue "dataFim" with
                        | Some dateStr -> DateTime.TryParse(dateStr) |> function | true, dt -> Some dt | _ -> None
                        | None -> None

                    let! prontuarios = Repository.getAll pacienteId profissionalId dataInicio dataFim
                    let response = prontuarios |> List.map toResponse
                    
                    return! json response next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro interno do servidor"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let getProntuarioById (prontuarioId: int) : HttpHandler =
        fun next ctx ->
            task {
                try
                    let! prontuario = Repository.getById prontuarioId
                    let response = toResponse prontuario
                    
                    return! json response next ctx
                with
                | :? System.InvalidOperationException ->
                    let errorResponse = {| error = "Prontuário não encontrado" |}
                    return! (setStatusCode 404 >=> json errorResponse) next ctx
                | ex ->
                    let errorResponse = {| error = "Erro interno do servidor"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let createProntuario : HttpHandler =
        fun next ctx ->
            task {
                try
                    let! inputDto = ctx.BindJsonAsync<ProntuarioInputDto>()
                    
                    let validationErrors = validateInput inputDto
                    if not validationErrors.IsEmpty then
                        let errorResponse = {| errors = validationErrors |}
                        return! (setStatusCode 400 >=> json errorResponse) next ctx
                    else
                        let domainInput = toDomainInput inputDto
                        let! prontuarioId = Repository.insert domainInput
                        
                        // Inserir prescrições
                        for prescricaoDto in inputDto.Prescricoes do
                            let prescricaoInput : PrescricaoInput = {
                                Medicamento = prescricaoDto.Medicamento
                                Dosagem = prescricaoDto.Dosagem
                                Frequencia = prescricaoDto.Frequencia
                                Duracao = prescricaoDto.Duracao
                                Orientacoes = prescricaoDto.Orientacoes
                                DataVencimento = prescricaoDto.DataVencimento
                            }
                            let! _ = Repository.insertPrescricao prontuarioId prescricaoInput
                            ()

                        // Inserir exames solicitados
                        for exameDto in inputDto.ExamesSolicitados do
                            let exameInput : ExameSolicitadoInput = {
                                TipoExame = exameDto.TipoExame
                                Descricao = exameDto.Descricao
                                Urgente = exameDto.Urgente
                                Observacoes = exameDto.Observacoes
                                LaboratorioId = exameDto.LaboratorioId
                            }
                            let! _ = Repository.insertExameSolicitado prontuarioId exameInput
                            ()

                        // Inserir procedimentos
                        for procDto in inputDto.Procedimentos do
                            let procInput : ProcedimentoInput = {
                                Nome = procDto.Nome
                                Codigo = procDto.Codigo
                                Descricao = procDto.Descricao
                                DataRealizacao = procDto.DataRealizacao
                                ProfissionalId = procDto.ProfissionalId
                                Observacoes = procDto.Observacoes
                                Valor = procDto.Valor
                            }
                            let! _ = Repository.insertProcedimento prontuarioId procInput
                            ()

                        let response = {| id = prontuarioId; message = "Prontuário criado com sucesso" |}
                        return! (setStatusCode 201 >=> json response) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao criar prontuário"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let updateProntuario (prontuarioId: int) : HttpHandler =
        fun next ctx ->
            task {
                try                    
                    let! inputDto = ctx.BindJsonAsync<ProntuarioInputDto>()
                    
                    let validationErrors = validateInput inputDto
                    if not validationErrors.IsEmpty then
                        let errorResponse = {| errors = validationErrors |}
                        return! (setStatusCode 400 >=> json errorResponse) next ctx
                    else
                        let domainInput = toDomainInput inputDto
                        let! success = Repository.update prontuarioId domainInput
                        
                        if success then
                            let response = {| message = "Prontuário atualizado com sucesso" |}
                            return! json response next ctx
                        else
                            let errorResponse = {| error = "Prontuário não encontrado ou já assinado (não pode ser editado)" |}
                            return! (setStatusCode 404 >=> json errorResponse) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao atualizar prontuário"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let assinarProntuario (prontuarioId: int) : HttpHandler =
        fun next ctx ->
            task {
                try
                    // TODO: Obter profissionalId do token JWT
                    let profissionalId = 1 // Placeholder - deve vir da autenticação
                    
                    let! success = Repository.assinarProntuario prontuarioId profissionalId
                    
                    if success then
                        let response = {| message = "Prontuário assinado com sucesso" |}
                        return! json response next ctx
                    else
                        let errorResponse = {| error = "Prontuário não encontrado, já assinado ou você não tem permissão" |}
                        return! (setStatusCode 404 >=> json errorResponse) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao assinar prontuário"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let getHistoricoPaciente (pacienteId: int) : HttpHandler =
        fun next ctx ->
            task {
                try
                    let! historico = Repository.getHistoricoPaciente pacienteId
                    return! json historico next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao obter histórico do paciente"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    // Handlers para Prescrições
    let adicionarPrescricao (prontuarioId: int) : HttpHandler =
        fun next ctx ->
            task {
                try
                    let! prescricaoDto = ctx.BindJsonAsync<PrescricaoInputDto>()
                    
                    if String.IsNullOrWhiteSpace(prescricaoDto.Medicamento) ||
                       String.IsNullOrWhiteSpace(prescricaoDto.Dosagem) ||
                       String.IsNullOrWhiteSpace(prescricaoDto.Frequencia) then
                        let errorResponse = {| error = "Medicamento, dosagem e frequência são obrigatórios" |}
                        return! (setStatusCode 400 >=> json errorResponse) next ctx
                    else
                        let prescricaoInput : PrescricaoInput = {
                            Medicamento = prescricaoDto.Medicamento
                            Dosagem = prescricaoDto.Dosagem
                            Frequencia = prescricaoDto.Frequencia
                            Duracao = prescricaoDto.Duracao
                            Orientacoes = prescricaoDto.Orientacoes
                            DataVencimento = prescricaoDto.DataVencimento
                        }
                        
                        let! id = Repository.insertPrescricao prontuarioId prescricaoInput
                        let response = {| id = id; message = "Prescrição adicionada com sucesso" |}
                        
                        return! (setStatusCode 201 >=> json response) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao adicionar prescrição"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let inativarPrescricao (prescricaoId: int) : HttpHandler =
        fun next ctx ->
            task {
                try
                    let! success = Repository.inactivatePrescricao prescricaoId
                    
                    if success then
                        let response = {| message = "Prescrição inativada com sucesso" |}
                        return! json response next ctx
                    else
                        let errorResponse = {| error = "Prescrição não encontrada" |}
                        return! (setStatusCode 404 >=> json errorResponse) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao inativar prescrição"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    // Handlers para Exames
    let adicionarExame (prontuarioId: int) : HttpHandler =
        fun next ctx ->
            task {
                try                    
                    let! exameDto = ctx.BindJsonAsync<ExameSolicitadoInputDto>()
                    
                    if String.IsNullOrWhiteSpace(exameDto.TipoExame) ||
                       String.IsNullOrWhiteSpace(exameDto.Descricao) then
                        let errorResponse = {| error = "Tipo de exame e descrição são obrigatórios" |}
                        return! (setStatusCode 400 >=> json errorResponse) next ctx
                    else
                        let exameInput : ExameSolicitadoInput = {
                            TipoExame = exameDto.TipoExame
                            Descricao = exameDto.Descricao
                            Urgente = exameDto.Urgente
                            Observacoes = exameDto.Observacoes
                            LaboratorioId = exameDto.LaboratorioId
                        }
                        
                        let! id = Repository.insertExameSolicitado prontuarioId exameInput
                        let response = {| id = id; message = "Exame solicitado com sucesso" |}
                        
                        return! (setStatusCode 201 >=> json response) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao solicitar exame"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let adicionarResultadoExame exameId : HttpHandler =
        fun next ctx ->
            task {
                try                    
                    let! resultadoDto = ctx.BindJsonAsync<{| resultado: string; arquivoUrl: string option |}>()
                    
                    if String.IsNullOrWhiteSpace(resultadoDto.resultado) then
                        let errorResponse = {| error = "Resultado é obrigatório" |}
                        return! (setStatusCode 400 >=> json errorResponse) next ctx
                    else
                        let! success = Repository.adicionarResultadoExame exameId resultadoDto.resultado resultadoDto.arquivoUrl
                        
                        if success then
                            let response = {| message = "Resultado do exame adicionado com sucesso" |}
                            return! json response next ctx
                        else
                            let errorResponse = {| error = "Exame não encontrado" |}
                            return! (setStatusCode 404 >=> json errorResponse) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao adicionar resultado"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    // Handlers para Procedimentos
    let adicionarProcedimento prontuarioId : HttpHandler =
        fun next ctx ->
            task {
                try                    
                    let! procDto = ctx.BindJsonAsync<ProcedimentoInputDto>()
                    
                    if String.IsNullOrWhiteSpace(procDto.Nome) ||
                       String.IsNullOrWhiteSpace(procDto.Descricao) then
                        let errorResponse = {| error = "Nome e descrição do procedimento são obrigatórios" |}
                        return! (setStatusCode 400 >=> json errorResponse) next ctx
                    else
                        let procInput : ProcedimentoInput = {
                            Nome = procDto.Nome
                            Codigo = procDto.Codigo
                            Descricao = procDto.Descricao
                            DataRealizacao = procDto.DataRealizacao
                            ProfissionalId = procDto.ProfissionalId
                            Observacoes = procDto.Observacoes
                            Valor = procDto.Valor
                        }
                        
                        let! id = Repository.insertProcedimento prontuarioId procInput
                        let response = {| id = id; message = "Procedimento adicionado com sucesso" |}
                        
                        return! (setStatusCode 201 >=> json response) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao adicionar procedimento"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    // Rotas Prontuario
    let routes : HttpHandler =
        choose [
            GET >=> choose [
                route "" >=> getAllProntuarios
                routef "/%i" getProntuarioById
                routef "/paciente/%i/historico" getHistoricoPaciente
            ]
            POST >=> choose [
                route "" >=> createProntuario
                routef "/%i/assinar" assinarProntuario
                routef "/%i/prescricoes" adicionarPrescricao
                routef "/%i/exames" adicionarExame
                routef "/%i/procedimentos" adicionarProcedimento
            ]
            PUT >=> choose [
                routef "/%i" updateProntuario
                routef "/exames/%i/resultado" adicionarResultadoExame
            ]
            DELETE >=> routef "/prescricoes/%i" inativarPrescricao
        ]
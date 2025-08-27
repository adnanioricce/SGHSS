namespace Domains.Agendamento

open System

module Models =
    type TipoAgendamento = 
        | Consulta 
        | Exame 
        | Cirurgia 
        | Teleconsulta 
        | Retorno
        | Procedimento
    
    type StatusAgendamento = 
        | Agendado 
        | Confirmado 
        | Cancelado 
        | Realizado 
        | Faltou 
        | Reagendado
    
    type Agendamento = {
        Id: int
        PacienteId: int
        ProfissionalId: int
        TipoAgendamento: TipoAgendamento
        DataHora: DateTime
        Duracao: TimeSpan
        Status: StatusAgendamento
        Observacoes: string option
        UnidadeId: int
        SalaId: int option
        ValorConsulta: decimal option
        PlanoSaudeCobertura: bool
        DataCadastro: DateTime
        DataAtualizacao: DateTime option
        CanceladoPor: int option
        MotivoCancel: string option
    }
    
    type AgendamentoInput = {
        PacienteId: int
        ProfissionalId: int
        TipoAgendamento: TipoAgendamento
        DataHora: DateTime
        Duracao: TimeSpan
        Observacoes: string option
        UnidadeId: int
        SalaId: int option
        ValorConsulta: decimal option
        PlanoSaudeCobertura: bool
    }
    
    type AgendamentoDetalhes = {
        Id: int
        Paciente: {| Id: int; Nome: string; CPF: string |}
        Profissional: {| Id: int; Nome: string; CRM: string option; TipoProfissional: string |}
        TipoAgendamento: TipoAgendamento
        DataHora: DateTime
        Duracao: TimeSpan
        Status: StatusAgendamento
        Observacoes: string option
        Unidade: {| Id: int; Nome: string |}
        Sala: {| Id: int; Nome: string |} option
        ValorConsulta: decimal option
        PlanoSaudeCobertura: bool
        DataCadastro: DateTime
        DataAtualizacao: DateTime option
        MotivoCancel: string option
    }

// Agendamento/Repository.fs
module Repository =
    open Npgsql.FSharp
    open Models
    open System    
    open Infrastructure.Database

    // Funções auxiliares para conversão de enums
    let private parseTipoAgendamento (tipo: string) =
        match tipo.ToUpper() with
        | "CONSULTA" -> Consulta
        | "EXAME" -> Exame
        | "CIRURGIA" -> Cirurgia
        | "TELECONSULTA" -> Teleconsulta
        | "RETORNO" -> Retorno
        | "PROCEDIMENTO" -> Procedimento
        | _ -> Consulta // default

    let private parseStatusAgendamento (status: string) =
        match status.ToUpper() with
        | "AGENDADO" -> Agendado
        | "CONFIRMADO" -> Confirmado
        | "CANCELADO" -> Cancelado
        | "REALIZADO" -> Realizado
        | "FALTOU" -> Faltou
        | "REAGENDADO" -> Reagendado
        | _ -> Agendado // default

    let private tipoAgendamentoToString (tipo: TipoAgendamento) =
        match tipo with
        | Consulta -> "CONSULTA"
        | Exame -> "EXAME"
        | Cirurgia -> "CIRURGIA"
        | Teleconsulta -> "TELECONSULTA"
        | Retorno -> "RETORNO"
        | Procedimento -> "PROCEDIMENTO"

    let private statusAgendamentoToString (status: StatusAgendamento) =
        match status with
        | Agendado -> "AGENDADO"
        | Confirmado -> "CONFIRMADO"
        | Cancelado -> "CANCELADO"
        | Realizado -> "REALIZADO"
        | Faltou -> "FALTOU"
        | Reagendado -> "REAGENDADO"

    let getAll (dataInicio: DateTime option) (dataFim: DateTime option) (status: StatusAgendamento option) =
        task {
            let mutable query = """
                SELECT id, paciente_id, profissional_id, tipo_agendamento, data_hora, 
                       duracao, status, observacoes, unidade_id, sala_id, valor_consulta,
                       plano_saude_cobertura, data_cadastro, data_atualizacao, 
                       cancelado_por, motivo_cancel
                FROM agendamentos 
                WHERE 1=1
            """
            let mutable parameters = []

            match dataInicio with
            | Some inicio ->
                query <- query + " AND data_hora >= @data_inicio"
                parameters <- ("data_inicio", Sql.timestamp inicio) :: parameters
            | None -> ()

            match dataFim with
            | Some fim ->
                query <- query + " AND data_hora <= @data_fim"
                parameters <- ("data_fim", Sql.timestamp fim) :: parameters
            | None -> ()

            match status with
            | Some s ->
                query <- query + " AND status = @status"
                parameters <- ("status", Sql.string (statusAgendamentoToString s)) :: parameters
            | None -> ()

            query <- query + " ORDER BY data_hora"

            return!
                DbConnection.getConnectionString ()
                |> Sql.connect
                |> Sql.query query
                |> Sql.parameters parameters
                |> Sql.executeAsync (fun read -> {
                    Id = read.int "id"
                    PacienteId = read.int "paciente_id"
                    ProfissionalId = read.int "profissional_id"
                    TipoAgendamento = parseTipoAgendamento (read.string "tipo_agendamento")
                    DataHora = read.dateTime "data_hora"
                    Duracao = read.double "duracao" |> TimeSpan.FromHours
                    Status = parseStatusAgendamento (read.string "status")
                    Observacoes = read.stringOrNone "observacoes"
                    UnidadeId = read.int "unidade_id"
                    SalaId = read.intOrNone "sala_id"
                    ValorConsulta = read.decimalOrNone "valor_consulta"
                    PlanoSaudeCobertura = read.bool "plano_saude_cobertura"
                    DataCadastro = read.dateTime "data_cadastro"
                    DataAtualizacao = read.dateTimeOrNone "data_atualizacao"
                    CanceladoPor = read.intOrNone "cancelado_por"
                    MotivoCancel = read.stringOrNone "motivo_cancel"
                })
        }

    let getById (id: int) =
        task {
            return!
                DbConnection.getConnectionString ()
                |> Sql.connect
                |> Sql.query """
                    SELECT id, paciente_id, profissional_id, tipo_agendamento, data_hora, 
                           duracao, status, observacoes, unidade_id, sala_id, valor_consulta,
                           plano_saude_cobertura, data_cadastro, data_atualizacao, 
                           cancelado_por, motivo_cancel
                    FROM agendamentos 
                    WHERE id = @id
                """
                |> Sql.parameters ["id", Sql.int id]
                |> Sql.executeRowAsync (fun read -> {
                    Id = read.int "id"
                    PacienteId = read.int "paciente_id"
                    ProfissionalId = read.int "profissional_id"
                    TipoAgendamento = parseTipoAgendamento (read.string "tipo_agendamento")
                    DataHora = read.dateTime "data_hora"
                    Duracao = read.double "duracao" |> TimeSpan.FromHours
                    Status = parseStatusAgendamento (read.string "status")
                    Observacoes = read.stringOrNone "observacoes"
                    UnidadeId = read.int "unidade_id"
                    SalaId = read.intOrNone "sala_id"
                    ValorConsulta = read.decimalOrNone "valor_consulta"
                    PlanoSaudeCobertura = read.bool "plano_saude_cobertura"
                    DataCadastro = read.dateTime "data_cadastro"
                    DataAtualizacao = read.dateTimeOrNone "data_atualizacao"
                    CanceladoPor = read.intOrNone "cancelado_por"
                    MotivoCancel = read.stringOrNone "motivo_cancel"
                })
        }

    let getDetalhes (id: int) =
        task {
            return!
                DbConnection.getConnectionString ()
                |> Sql.connect
                |> Sql.query """
                    SELECT 
                        a.id, a.tipo_agendamento, a.data_hora, a.duracao, a.status, 
                        a.observacoes, a.valor_consulta, a.plano_saude_cobertura,
                        a.data_cadastro, a.data_atualizacao, a.motivo_cancel,
                        p.id as paciente_id, p.nome as paciente_nome, p.cpf as paciente_cpf,
                        pr.id as profissional_id, pr.nome as profissional_nome, 
                        pr.crm as profissional_crm, pr.tipo_profissional,
                        u.id as unidade_id, u.nome as unidade_nome,
                        s.id as sala_id, s.nome as sala_nome
                    FROM agendamentos a
                    INNER JOIN pacientes p ON a.paciente_id = p.id
                    INNER JOIN profissionais pr ON a.profissional_id = pr.id
                    INNER JOIN unidades u ON a.unidade_id = u.id
                    LEFT JOIN salas s ON a.sala_id = s.id
                    WHERE a.id = @id
                """
                |> Sql.parameters ["id", Sql.int id]
                |> Sql.executeRowAsync (fun read -> {
                    Id = read.int "id"
                    Paciente = {|
                        Id = read.int "paciente_id"
                        Nome = read.string "paciente_nome"
                        CPF = read.string "paciente_cpf"
                    |}
                    Profissional = {|
                        Id = read.int "profissional_id"
                        Nome = read.string "profissional_nome"
                        CRM = read.stringOrNone "profissional_crm"
                        TipoProfissional = read.string "tipo_profissional"
                    |}
                    TipoAgendamento = parseTipoAgendamento (read.string "tipo_agendamento")
                    DataHora = read.dateTime "data_hora"
                    Duracao = read.double "duracao" |> TimeSpan.FromHours
                    Status = parseStatusAgendamento (read.string "status")
                    Observacoes = read.stringOrNone "observacoes"
                    Unidade = {|
                        Id = read.int "unidade_id"
                        Nome = read.string "unidade_nome"
                    |}
                    Sala = 
                        match read.intOrNone "sala_id" with
                        | Some salaId -> Some {| Id = salaId; Nome = read.string "sala_nome" |}
                        | None -> None
                    ValorConsulta = read.decimalOrNone "valor_consulta"
                    PlanoSaudeCobertura = read.bool "plano_saude_cobertura"
                    DataCadastro = read.dateTime "data_cadastro"
                    DataAtualizacao = read.dateTimeOrNone "data_atualizacao"
                    MotivoCancel = read.stringOrNone "motivo_cancel"
                })
        }

    let insert (input: AgendamentoInput) =
        task {
            return!
                DbConnection.getConnectionString ()
                |> Sql.connect
                |> Sql.query """
                    INSERT INTO agendamentos 
                    (paciente_id, profissional_id, tipo_agendamento, data_hora, duracao, 
                     observacoes, unidade_id, sala_id, valor_consulta, plano_saude_cobertura)
                    VALUES 
                    (@paciente_id, @profissional_id, @tipo_agendamento, @data_hora, @duracao,
                     @observacoes, @unidade_id, @sala_id, @valor_consulta, @plano_saude_cobertura)
                    RETURNING id
                """
                |> Sql.parameters [
                    "paciente_id", Sql.int input.PacienteId
                    "profissional_id", Sql.int input.ProfissionalId
                    "tipo_agendamento", Sql.string (tipoAgendamentoToString input.TipoAgendamento)
                    "data_hora", Sql.timestamp input.DataHora
                    "duracao", Sql.interval input.Duracao
                    "observacoes", Sql.stringOrNone input.Observacoes
                    "unidade_id", Sql.int input.UnidadeId
                    "sala_id", Sql.intOrNone input.SalaId
                    "valor_consulta", Sql.decimalOrNone input.ValorConsulta
                    "plano_saude_cobertura", Sql.bool input.PlanoSaudeCobertura
                ]
                |> Sql.executeRowAsync (fun read -> read.int "id")
        }

    let updateStatus (id: int) (novoStatus: StatusAgendamento) (usuarioId: int option) (motivo: string option) =
        task {
            let! rowsAffected =
                DbConnection.getConnectionString ()
                |> Sql.connect
                |> Sql.query """
                    UPDATE agendamentos 
                    SET status = @status,
                        data_atualizacao = CURRENT_TIMESTAMP,
                        cancelado_por = @cancelado_por,
                        motivo_cancel = @motivo_cancel
                    WHERE id = @id
                """
                |> Sql.parameters [
                    "id", Sql.int id
                    "status", Sql.string (statusAgendamentoToString novoStatus)
                    "cancelado_por", Sql.intOrNone usuarioId
                    "motivo_cancel", Sql.stringOrNone motivo
                ]
                |> Sql.executeNonQueryAsync
            
            return rowsAffected > 0
        }

    let update (id: int) (input: AgendamentoInput) =
        task {
            let! rowsAffected =
                DbConnection.getConnectionString ()
                |> Sql.connect
                |> Sql.query """
                    UPDATE agendamentos 
                    SET paciente_id = @paciente_id,
                        profissional_id = @profissional_id,
                        tipo_agendamento = @tipo_agendamento,
                        data_hora = @data_hora,
                        duracao = @duracao,
                        observacoes = @observacoes,
                        unidade_id = @unidade_id,
                        sala_id = @sala_id,
                        valor_consulta = @valor_consulta,
                        plano_saude_cobertura = @plano_saude_cobertura,
                        data_atualizacao = CURRENT_TIMESTAMP
                    WHERE id = @id AND status NOT IN ('CANCELADO', 'REALIZADO')
                """
                |> Sql.parameters [
                    "id", Sql.int id
                    "paciente_id", Sql.int input.PacienteId
                    "profissional_id", Sql.int input.ProfissionalId
                    "tipo_agendamento", Sql.string (tipoAgendamentoToString input.TipoAgendamento)
                    "data_hora", Sql.timestamp input.DataHora
                    "duracao", Sql.interval input.Duracao
                    "observacoes", Sql.stringOrNone input.Observacoes
                    "unidade_id", Sql.int input.UnidadeId
                    "sala_id", Sql.intOrNone input.SalaId
                    "valor_consulta", Sql.decimalOrNone input.ValorConsulta
                    "plano_saude_cobertura", Sql.bool input.PlanoSaudeCobertura
                ]
                |> Sql.executeNonQueryAsync
            
            return rowsAffected > 0
        }

    let verificarConflito (profissionalId: int) (dataHora: DateTime) (duracao: TimeSpan) (agendamentoIdExcluir: int option) =
        task {
            let dataFim = dataHora.Add(duracao)
            let mutable query = """
                SELECT COUNT(*) 
                FROM agendamentos 
                WHERE profissional_id = @profissional_id 
                AND status NOT IN ('CANCELADO', 'FALTOU')
                AND (
                    (data_hora <= @data_inicio AND (data_hora + duracao) > @data_inicio) OR
                    (data_hora < @data_fim AND (data_hora + duracao) >= @data_fim) OR
                    (data_hora >= @data_inicio AND data_hora < @data_fim)
                )
            """
            let mutable parameters = [
                "profissional_id", Sql.int profissionalId
                "data_inicio", Sql.timestamp dataHora
                "data_fim", Sql.timestamp dataFim
            ]

            match agendamentoIdExcluir with
            | Some excludeId ->
                query <- query + " AND id != @exclude_id"
                parameters <- ("exclude_id", Sql.int excludeId) :: parameters
            | None -> ()

            let! count =
                DbConnection.getConnectionString ()
                |> Sql.connect
                |> Sql.query query
                |> Sql.parameters parameters
                |> Sql.executeRowAsync (fun read -> read.int "count")
            
            return count > 0
        }

    let getAgendamentosPorPaciente (pacienteId: int) (dataInicio: DateTime option) =
        task {
            let mutable query = """
                SELECT id, paciente_id, profissional_id, tipo_agendamento, data_hora, 
                       duracao, status, observacoes, unidade_id, sala_id, valor_consulta,
                       plano_saude_cobertura, data_cadastro, data_atualizacao, 
                       cancelado_por, motivo_cancel
                FROM agendamentos 
                WHERE paciente_id = @paciente_id
            """
            let mutable parameters = ["paciente_id", Sql.int pacienteId]

            match dataInicio with
            | Some inicio ->
                query <- query + " AND data_hora >= @data_inicio"
                parameters <- ("data_inicio", Sql.timestamp inicio) :: parameters
            | None -> ()

            query <- query + " ORDER BY data_hora DESC"

            return!
                DbConnection.getConnectionString ()
                |> Sql.connect
                |> Sql.query query
                |> Sql.parameters parameters
                |> Sql.executeAsync (fun read -> {
                    Id = read.int "id"
                    PacienteId = read.int "paciente_id"
                    ProfissionalId = read.int "profissional_id"
                    TipoAgendamento = parseTipoAgendamento (read.string "tipo_agendamento")
                    DataHora = read.dateTime "data_hora"
                    Duracao = read.double "duracao" |> TimeSpan.FromHours
                    Status = parseStatusAgendamento (read.string "status")
                    Observacoes = read.stringOrNone "observacoes"
                    UnidadeId = read.int "unidade_id"
                    SalaId = read.intOrNone "sala_id"
                    ValorConsulta = read.decimalOrNone "valor_consulta"
                    PlanoSaudeCobertura = read.bool "plano_saude_cobertura"
                    DataCadastro = read.dateTime "data_cadastro"
                    DataAtualizacao = read.dateTimeOrNone "data_atualizacao"
                    CanceladoPor = read.intOrNone "cancelado_por"
                    MotivoCancel = read.stringOrNone "motivo_cancel"
                })
        }

    let getAgendamentosPorProfissional (profissionalId: int) (data: DateTime) =
        task {
            let inicioDia = data.Date
            let fimDia = inicioDia.AddDays(1.0).AddTicks(-1L)

            return!
                DbConnection.getConnectionString ()
                |> Sql.connect
                |> Sql.query """
                    SELECT id, paciente_id, profissional_id, tipo_agendamento, data_hora, 
                           duracao, status, observacoes, unidade_id, sala_id, valor_consulta,
                           plano_saude_cobertura, data_cadastro, data_atualizacao, 
                           cancelado_por, motivo_cancel
                    FROM agendamentos 
                    WHERE profissional_id = @profissional_id
                    AND data_hora >= @inicio_dia
                    AND data_hora <= @fim_dia
                    AND status NOT IN ('CANCELADO')
                    ORDER BY data_hora
                """
                |> Sql.parameters [
                    "profissional_id", Sql.int profissionalId
                    "inicio_dia", Sql.timestamp inicioDia
                    "fim_dia", Sql.timestamp fimDia
                ]
                |> Sql.executeAsync (fun read -> {
                    Id = read.int "id"
                    PacienteId = read.int "paciente_id"
                    ProfissionalId = read.int "profissional_id"
                    TipoAgendamento = parseTipoAgendamento (read.string "tipo_agendamento")
                    DataHora = read.dateTime "data_hora"
                    Duracao = read.double "duracao" |> TimeSpan.FromHours
                    Status = parseStatusAgendamento (read.string "status")
                    Observacoes = read.stringOrNone "observacoes"
                    UnidadeId = read.int "unidade_id"
                    SalaId = read.intOrNone "sala_id"
                    ValorConsulta = read.decimalOrNone "valor_consulta"
                    PlanoSaudeCobertura = read.bool "plano_saude_cobertura"
                    DataCadastro = read.dateTime "data_cadastro"
                    DataAtualizacao = read.dateTimeOrNone "data_atualizacao"
                    CanceladoPor = read.intOrNone "cancelado_por"
                    MotivoCancel = read.stringOrNone "motivo_cancel"
                })
        }

// Agendamento/Handler.fs
module Handler =
    open Giraffe
    open Microsoft.AspNetCore.Http
    open Models
    open System    

    // DTOs
    type AgendamentoResponse = {
        Id: int
        PacienteId: int
        ProfissionalId: int
        TipoAgendamento: string
        DataHora: DateTime
        Duracao: string
        Status: string
        Observacoes: string option
        UnidadeId: int
        SalaId: int option
        ValorConsulta: decimal option
        PlanoSaudeCobertura: bool
        DataCadastro: DateTime
        DataAtualizacao: DateTime option
        MotivoCancel: string option
    }

    type AgendamentoInputDto = {
        PacienteId: int
        ProfissionalId: int
        TipoAgendamento: string
        DataHora: DateTime
        Duracao: string // em formato "HH:mm"
        Observacoes: string option
        UnidadeId: int
        SalaId: int option
        ValorConsulta: decimal option
        PlanoSaudeCobertura: bool
    }

    type StatusUpdateDto = {
        NovoStatus: string
        Motivo: string option
    }

    // Funções auxiliares
    let private toResponse (agendamento: Agendamento) : AgendamentoResponse =
        {
            Id = agendamento.Id
            PacienteId = agendamento.PacienteId
            ProfissionalId = agendamento.ProfissionalId
            TipoAgendamento = 
                match agendamento.TipoAgendamento with
                | Consulta -> "CONSULTA"
                | Exame -> "EXAME"
                | Cirurgia -> "CIRURGIA"
                | Teleconsulta -> "TELECONSULTA"
                | Retorno -> "RETORNO"
                | Procedimento -> "PROCEDIMENTO"
            DataHora = agendamento.DataHora
            Duracao = agendamento.Duracao.ToString(@"hh\:mm")
            Status = 
                match agendamento.Status with
                | Agendado -> "AGENDADO"
                | Confirmado -> "CONFIRMADO"
                | Cancelado -> "CANCELADO"
                | Realizado -> "REALIZADO"
                | Faltou -> "FALTOU"
                | Reagendado -> "REAGENDADO"
            Observacoes = agendamento.Observacoes
            UnidadeId = agendamento.UnidadeId
            SalaId = agendamento.SalaId
            ValorConsulta = agendamento.ValorConsulta
            PlanoSaudeCobertura = agendamento.PlanoSaudeCobertura
            DataCadastro = agendamento.DataCadastro
            DataAtualizacao = agendamento.DataAtualizacao
            MotivoCancel = agendamento.MotivoCancel
        }

    let private toDomainInput (dto: AgendamentoInputDto) : AgendamentoInput =
        let tipo = 
            match dto.TipoAgendamento.ToUpper() with
            | "CONSULTA" -> Consulta
            | "EXAME" -> Exame
            | "CIRURGIA" -> Cirurgia
            | "TELECONSULTA" -> Teleconsulta
            | "RETORNO" -> Retorno
            | "PROCEDIMENTO" -> Procedimento
            | _ -> failwith $"Tipo de agendamento inválido: {dto.TipoAgendamento}"

        let duracao = 
            match TimeSpan.TryParse(dto.Duracao) with
            | true, ts -> ts
            | false, _ -> failwith $"Formato de duração inválido: {dto.Duracao}. Use HH:mm"

        {
            PacienteId = dto.PacienteId
            ProfissionalId = dto.ProfissionalId
            TipoAgendamento = tipo
            DataHora = dto.DataHora
            Duracao = duracao
            Observacoes = dto.Observacoes
            UnidadeId = dto.UnidadeId
            SalaId = dto.SalaId
            ValorConsulta = dto.ValorConsulta
            PlanoSaudeCobertura = dto.PlanoSaudeCobertura
        }

    // Validações
    let private validateInput (dto: AgendamentoInputDto) =
        let errors = ResizeArray<string>()
        
        if dto.PacienteId <= 0 then
            errors.Add("Paciente deve ser especificado")
        
        if dto.ProfissionalId <= 0 then
            errors.Add("Profissional deve ser especificado")
        
        if dto.DataHora <= DateTime.Now then
            errors.Add("Data/hora deve ser no futuro")
        
        if dto.UnidadeId <= 0 then
            errors.Add("Unidade deve ser especificada")

        match TimeSpan.TryParse(dto.Duracao) with
        | true, ts when ts.TotalMinutes >= 15.0 && ts.TotalHours <= 8.0 -> ()
        | _ -> errors.Add("Duração deve estar entre 15 minutos e 8 horas (formato HH:mm)")

        match dto.TipoAgendamento.ToUpper() with
        | "CONSULTA" | "EXAME" | "CIRURGIA" | "TELECONSULTA" | "RETORNO" | "PROCEDIMENTO" -> ()
        | _ -> errors.Add("Tipo de agendamento inválido")

        errors |> Seq.toList

    // Handlers
    let getAllAgendamentos : HttpHandler =
        fun next ctx ->
            task {
                try
                    let dataInicio = 
                        match ctx.TryGetQueryStringValue "dataInicio" with
                        | Some dateStr -> DateTime.TryParse(dateStr) |> function | true, dt -> Some dt | _ -> None
                        | None -> None

                    let dataFim = 
                        match ctx.TryGetQueryStringValue "dataFim" with
                        | Some dateStr -> DateTime.TryParse(dateStr) |> function | true, dt -> Some dt | _ -> None
                        | None -> None

                    let status = 
                        match ctx.TryGetQueryStringValue "status" with
                        | Some statusStr -> 
                            match statusStr.ToUpper() with
                            | "AGENDADO" -> Some Agendado
                            | "CONFIRMADO" -> Some Confirmado
                            | "CANCELADO" -> Some Cancelado
                            | "REALIZADO" -> Some Realizado
                            | "FALTOU" -> Some Faltou
                            | "REAGENDADO" -> Some Reagendado
                            | _ -> None
                        | None -> None

                    let! agendamentos = Repository.getAll dataInicio dataFim status
                    let response = agendamentos |> List.map toResponse
                    
                    return! json response next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro interno do servidor"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let getAgendamentoById (agendamentoId: int) : HttpHandler =
        fun next ctx ->
            task {
                try
                    // let agendamentoId = 
                    //     let res,value = ctx.Request.RouteValues.TryGetValue("id")
                    //     if res then value :?> string |> int else failwith "ID do agendamento não fornecido"                        
                    
                    let! agendamento = Repository.getById agendamentoId
                    let response = toResponse agendamento
                    
                    return! json response next ctx
                with
                | :? System.InvalidOperationException ->
                    let errorResponse = {| error = "Agendamento não encontrado" |}
                    return! (setStatusCode 404 >=> json errorResponse) next ctx
                | ex ->
                    let errorResponse = {| error = "Erro interno do servidor"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let getAgendamentoDetalhes (agendamentoId: int) : HttpHandler =
        fun next ctx ->
            task {
                try
                    // let agendamentoId = 
                    //     let res,value = ctx.Request.RouteValues.TryGetValue("id")
                    //     if res then value :?> string |> int else failwith "ID do agendamento não fornecido"
                    
                    let! detalhes = Repository.getDetalhes agendamentoId
                    return! json detalhes next ctx
                with
                | :? System.InvalidOperationException ->
                    let errorResponse = {| error = "Agendamento não encontrado" |}
                    return! (setStatusCode 404 >=> json errorResponse) next ctx
                | ex ->
                    let errorResponse = {| error = "Erro interno do servidor"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let createAgendamento : HttpHandler =
        fun next ctx ->
            task {
                try
                    let! inputDto = ctx.BindJsonAsync<AgendamentoInputDto>()
                    
                    let validationErrors = validateInput inputDto
                    if not validationErrors.IsEmpty then
                        let errorResponse = {| errors = validationErrors |}
                        return! (setStatusCode 400 >=> json errorResponse) next ctx
                    else
                        let domainInput = toDomainInput inputDto
                        
                        // Verificar conflito de horário
                        let! temConflito = Repository.verificarConflito 
                                            domainInput.ProfissionalId 
                                            domainInput.DataHora 
                                            domainInput.Duracao 
                                            None
                        
                        if temConflito then
                            let errorResponse = {| error = "Profissional já possui agendamento neste horário" |}
                            return! (setStatusCode 409 >=> json errorResponse) next ctx
                        else
                            let! id = Repository.insert domainInput
                            let response = {| id = id; message = "Agendamento criado com sucesso" |}
                            
                            return! (setStatusCode 201 >=> json response) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao criar agendamento"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let updateAgendamento (agendamentoId) : HttpHandler =
        fun next ctx ->
            task {
                try
                    // let agendamentoId = 
                    //     let res,value = ctx.Request.RouteValues.TryGetValue("id")
                    //     if res then value :?> string |> int else failwith "ID do agendamento não fornecido"
                    let! inputDto = ctx.BindJsonAsync<AgendamentoInputDto>()
                    
                    let validationErrors = validateInput inputDto
                    if not validationErrors.IsEmpty then
                        let errorResponse = {| errors = validationErrors |}
                        return! (setStatusCode 400 >=> json errorResponse) next ctx
                    else
                        let domainInput = toDomainInput inputDto
                        
                        // Verificar conflito de horário (excluindo o próprio agendamento)
                        let! temConflito = Repository.verificarConflito 
                                            domainInput.ProfissionalId 
                                            domainInput.DataHora 
                                            domainInput.Duracao 
                                            (Some agendamentoId)
                        
                        if temConflito then
                            let errorResponse = {| error = "Profissional já possui agendamento neste horário" |}
                            return! (setStatusCode 409 >=> json errorResponse) next ctx
                        else
                            let! success = Repository.update agendamentoId domainInput
                            
                            if success then
                                let response = {| message = "Agendamento atualizado com sucesso" |}
                                return! json response next ctx
                            else
                                let errorResponse = {| error = "Agendamento não encontrado ou não pode ser atualizado (já realizado/cancelado)" |}
                                return! (setStatusCode 404 >=> json errorResponse) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao atualizar agendamento"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let updateStatusAgendamento (agendamentoId) : HttpHandler =
        fun next ctx ->
            task {
                try
                    // let agendamentoId = 
                    //     let res,value = ctx.Request.RouteValues.TryGetValue("id")
                    //     if res then value :?> string |> int else failwith "ID do agendamento não fornecido"
                    let! statusDto = ctx.BindJsonAsync<StatusUpdateDto>()
                    
                    let novoStatus = 
                        match statusDto.NovoStatus.ToUpper() with
                        | "AGENDADO" -> Agendado
                        | "CONFIRMADO" -> Confirmado
                        | "CANCELADO" -> Cancelado
                        | "REALIZADO" -> Realizado
                        | "FALTOU" -> Faltou
                        | "REAGENDADO" -> Reagendado
                        | _ -> failwith $"Status inválido: {statusDto.NovoStatus}"

                    // TODO: Obter userId do token JWT
                    let usuarioId = Some 1 // Placeholder - deve vir da autenticação
                    
                    let! success = Repository.updateStatus agendamentoId novoStatus usuarioId statusDto.Motivo
                    
                    if success then
                        let response = {| message = $"Status alterado para {statusDto.NovoStatus} com sucesso" |}
                        return! json response next ctx
                    else
                        let errorResponse = {| error = "Agendamento não encontrado" |}
                        return! (setStatusCode 404 >=> json errorResponse) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao atualizar status"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let cancelarAgendamento (agendamentoId) : HttpHandler =
        fun next ctx ->
            task {
                try
                    // let agendamentoId = 
                    //     let res,value = ctx.Request.RouteValues.TryGetValue("id")
                    //     if res then value :?> string |> int else failwith "ID do agendamento não fornecido"
                    let! cancelDto = ctx.BindJsonAsync<{| motivo: string |}>()
                    
                    // TODO: Obter userId do token JWT
                    let usuarioId = Some 1 // Placeholder - deve vir da autenticação
                    
                    let! success = Repository.updateStatus agendamentoId Cancelado usuarioId (Some cancelDto.motivo)
                    
                    if success then
                        let response = {| message = "Agendamento cancelado com sucesso" |}
                        return! json response next ctx
                    else
                        let errorResponse = {| error = "Agendamento não encontrado" |}
                        return! (setStatusCode 404 >=> json errorResponse) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao cancelar agendamento"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let confirmarAgendamento (agendamentoId: int) : HttpHandler =
        fun next ctx ->
            task {
                try
                    // let agendamentoId = 
                    //     let res,value = ctx.Request.RouteValues.TryGetValue("id")
                    //     if res then value :?> string |> int else failwith "ID do agendamento não fornecido"
                    
                    // TODO: Obter userId do token JWT
                    let usuarioId = Some 1 // Placeholder
                    
                    let! success = Repository.updateStatus agendamentoId Confirmado usuarioId None
                    
                    if success then
                        let response = {| message = "Agendamento confirmado com sucesso" |}
                        return! json response next ctx
                    else
                        let errorResponse = {| error = "Agendamento não encontrado" |}
                        return! (setStatusCode 404 >=> json errorResponse) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao confirmar agendamento"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let marcarComoRealizado (agendamentoId) : HttpHandler =
        fun next ctx ->
            task {
                try
                    // let agendamentoId = 
                    //     let res,value = ctx.Request.RouteValues.TryGetValue("id")
                    //     if res then value :?> string |> int else failwith "ID do agendamento não fornecido"
                    
                    // TODO: Obter userId do token JWT
                    let usuarioId = Some 1 // Placeholder
                    
                    let! success = Repository.updateStatus agendamentoId Realizado usuarioId None
                    
                    if success then
                        let response = {| message = "Agendamento marcado como realizado" |}
                        return! json response next ctx
                    else
                        let errorResponse = {| error = "Agendamento não encontrado" |}
                        return! (setStatusCode 404 >=> json errorResponse) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao marcar como realizado"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let getAgendamentosPorPaciente (pacienteId: int) : HttpHandler =
        fun next ctx ->
            task {
                try
                    // let pacienteId = 
                    //     let res,value = ctx.Request.RouteValues.TryGetValue("id")
                    //     if res then value :?> string |> int else failwith "ID do agendamento não fornecido"
                    
                    let dataInicio = 
                        match ctx.TryGetQueryStringValue "dataInicio" with
                        | Some dateStr -> DateTime.TryParse(dateStr) |> function | true, dt -> Some dt | _ -> None
                        | None -> None

                    let! agendamentos = Repository.getAgendamentosPorPaciente pacienteId dataInicio
                    let response = agendamentos |> List.map toResponse
                    
                    return! json response next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro interno do servidor"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let getAgendamentosPorProfissional (profissionalId) : HttpHandler =
        fun next ctx ->
            task {
                try
                    // let profissionalId = 
                    //     let res,value = ctx.Request.RouteValues.TryGetValue("id")
                    //     if res then value :?> string |> int else failwith "ID do agendamento não fornecido"
                    
                    let data = 
                        match ctx.TryGetQueryStringValue "data" with
                        | Some dateStr -> 
                            match DateTime.TryParse(dateStr) with
                            | true, dt -> dt
                            | false, _ -> DateTime.Today
                        | None -> DateTime.Today

                    let! agendamentos = Repository.getAgendamentosPorProfissional profissionalId data
                    let response = agendamentos |> List.map toResponse
                    
                    return! json response next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro interno do servidor"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let verificarDisponibilidade (profissionalId) : HttpHandler =
        fun next ctx ->
            task {
                try
                    // let profissionalId = 
                    //     let res,value = ctx.Request.RouteValues.TryGetValue("id")
                    //     if res then value :?> string |> int else failwith "ID do agendamento não fornecido"
                    let dataHora = 
                        ctx.GetQueryStringValue "dataHora"
                        |> Result.bind (fun str -> 
                            match DateTime.TryParse(str) with
                            | true, dt -> Ok dt
                            | false, _ -> Error "Formato de data/hora inválido. Use ISO 8601")
                        |> function
                            | Ok dt -> dt
                            | Error msg -> failwith msg
                    
                    let duracao = 
                        ctx.GetQueryStringValue "duracao"
                        |> Result.bind (fun str -> 
                            match TimeSpan.TryParse(str) with
                            | true, ts -> Ok ts
                            | false, _ -> Error "Formato de duração inválido. Use HH:mm")
                        |> function
                            | Ok ts -> ts
                            | Error msg -> failwith msg                                        
                    
                    let! temConflito = Repository.verificarConflito profissionalId dataHora duracao None
                    
                    let response = {| disponivel = not temConflito; dataHora = dataHora; duracao = duracao.ToString(@"hh\:mm") |}
                    return! json response next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao verificar disponibilidade"; details = ex.Message |}
                    return! (setStatusCode 400 >=> json errorResponse) next ctx
            }

    // Rotas
    let routes : HttpHandler =
        choose [
            GET >=> choose [
                route "" >=> getAllAgendamentos
                routef "/%i" getAgendamentoById
                routef "/%i/detalhes" getAgendamentoDetalhes
                routef "/paciente/%i" getAgendamentosPorPaciente
                routef "/profissional/%i" getAgendamentosPorProfissional
                routef "/profissional/%i/disponibilidade" verificarDisponibilidade
            ]
            POST >=> choose [
                route "" >=> createAgendamento
                routef "/%i/confirmar" confirmarAgendamento
                routef "/%i/realizar" marcarComoRealizado
            ]
            PUT >=> choose [
                routef "/%i" updateAgendamento
                routef "/%i/status" updateStatusAgendamento
            ]
            DELETE >=> routef "/%i" cancelarAgendamento
        ]

//namespace SGHSS.Core.Domain.Paciente

//open System

//module Models =
//    type TipoDocumento = | CPF | RG | CNH | Passaporte
    
//    type Endereco = {
//        Id: int
//        Logradouro: string
//        Numero: string
//        Complemento: string option
//        Bairro: string
//        Cidade: string
//        Estado: string
//        CEP: string
//        Pais: string
//    }
    
//    type ContatoEmergencia = {
//        Nome: string
//        Parentesco: string
//        Telefone: string
//        Email: string option
//    }
    
//    type Paciente = {
//        Id: int
//        Nome: string
//        CPF: string
//        RG: string option
//        DataNascimento: DateTime
//        Sexo: string
//        EstadoCivil: string option
//        Profissao: string option
//        Email: string option
//        Telefone: string
//        TelefoneSecundario: string option
//        Endereco: Endereco
//        ContatoEmergencia: ContatoEmergencia option
//        PlanoSaude: string option
//        NumeroCarteirinha: string option
//        Observacoes: string option
//        DataCadastro: DateTime
//        DataAtualizacao: DateTime option
//        Ativo: bool
//    }
    
//    type PacienteInput = {
//        Nome: string
//        CPF: string
//        RG: string option
//        DataNascimento: DateTime
//        Sexo: string
//        EstadoCivil: string option
//        Profissao: string option
//        Email: string option
//        Telefone: string
//        TelefoneSecundario: string option
//        Endereco: Endereco
//        ContatoEmergencia: ContatoEmergencia option
//        PlanoSaude: string option
//        NumeroCarteirinha: string option
//        Observacoes: string option
//    }
//```

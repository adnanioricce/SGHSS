namespace Domains.Telemedicina

open System
open Infrastructure.Database
open SGHSS.Api
open SGHSS.Api.Logging

module Models =
    type StatusSessao = 
        | Agendada 
        | Iniciada 
        | EmAndamento 
        | Finalizada 
        | Cancelada
        | Falha
    
    type TipoParticipante = 
        | Paciente 
        | Medico 
        | Observador 
        | Acompanhante
    
    type SessaoTelemedicina = {
        Id: int
        AgendamentoId: int
        PacienteId: int
        ProfissionalId: int
        LinkSessao: string
        TokenAcesso: string
        SenhaPaciente: string option
        DataInicio: DateTime option
        DataFim: DateTime option
        Status: StatusSessao
        GravacaoUrl: string option
        GravacaoPermitida: bool
        ObservacoesFinais: string option
        ProntuarioId: int option
        QualidadeConexao: string option
        PlataformaVideo: string // ZOOM, TEAMS, JITSI, etc.
        DataCriacao: DateTime
        DataAtualizacao: DateTime option
        CanceladoPor: int option
        MotivoCancel: string option
    }
    
    type ParticipanteSessao = {
        Id: int
        SessaoId: int
        UsuarioId: int
        TipoParticipante: TipoParticipante
        Nome: string
        Email: string option
        DataEntrada: DateTime option
        DataSaida: DateTime option
        TempoConectado: TimeSpan option
        Ativo: bool
        DispositivoUtilizado: string option
        IpAddress: string option
    }
    
    type ConfiguracaoTelemedicina = {
        Id: int
        ProfissionalId: int
        PlataformaPreferida: string
        PermiteGravacao: bool
        DuracaoMaximaSessao: TimeSpan
        PermiteSalaEspera: bool
        NotificacoesEmail: bool
        NotificacoesSms: bool
        HorarioAtendimentoInicio: TimeSpan
        HorarioAtendimentoFim: TimeSpan
        DiasAtendimento: string // Dias da semana separados por vírgula: "1,2,3,4,5"
        Ativo: bool
        DataCriacao: DateTime
        DataAtualizacao: DateTime option
    }
    
    type HistoricoSessao = {
        Id: int
        SessaoId: int
        Evento: string // SESSAO_CRIADA, PARTICIPANTE_ENTROU, SESSAO_INICIADA, etc.
        Descricao: string
        UsuarioId: int option
        Timestamp: DateTime
        DadosAdicionais: string option // JSON com dados extras
    }

    // Input Types
    type SessaoTelemedicinInput = {
        AgendamentoId: int
        PacienteId: int
        ProfissionalId: int
        GravacaoPermitida: bool
        PlataformaVideo: string
        ObservacoesIniciais: string option
    }
    
    type ConfiguracaoTelemedicinInput = {
        ProfissionalId: int
        PlataformaPreferida: string
        PermiteGravacao: bool
        DuracaoMaximaSessao: TimeSpan
        PermiteSalaEspera: bool
        NotificacoesEmail: bool
        NotificacoesSms: bool
        HorarioAtendimentoInicio: TimeSpan
        HorarioAtendimentoFim: TimeSpan
        DiasAtendimento: int list // Lista de dias da semana (1-7)
    }

    type EntrarSessaoInput = {
        SessaoId: int
        UsuarioId: int
        TipoParticipante: TipoParticipante
        Nome: string
        Email: string option
        DispositivoUtilizado: string option
    }

    // Views detalhadas
    type SessaoDetalhes = {
        Id: int
        Agendamento: {| Id: int; DataHora: DateTime; Duracao: TimeSpan |}
        Paciente: {| Id: int; Nome: string; Email: string option; Telefone: string |}
        Profissional: {| Id: int; Nome: string; CRM: string option; Email: string |}
        LinkSessao: string
        SenhaPaciente: string option
        Status: StatusSessao
        DataInicio: DateTime option
        DataFim: DateTime option
        DuracaoReal: TimeSpan option
        GravacaoUrl: string option
        GravacaoPermitida: bool
        PlataformaVideo: string
        Participantes: ParticipanteSessao list
        QualidadeConexao: string option
        ObservacoesFinais: string option
        DataCriacao: DateTime
        MotivoCancel: string option
    }

    type DashboardTelemedicina = {
        SessoesHoje: int
        SessoesAguardando: int
        SessoesEmAndamento: int
        SessoesFinalizadas: int
        SessoesCanceladas: int
        TempoMedioSessao: TimeSpan option
        ProximasSessoes: SessaoDetalhes list
        ProfissionaisOnline: int
        PacientesAguardando: int
    }

// Telemedicina/Repository.fs
module Repository =
    open Npgsql.FSharp
    open Models
    open System    

    // Funções auxiliares para conversão de enums
    let private parseStatusSessao (status: string) =
        match status.ToUpper() with
        | "AGENDADA" -> Agendada
        | "INICIADA" -> Iniciada
        | "EMANDAMENTO" -> EmAndamento
        | "FINALIZADA" -> Finalizada
        | "CANCELADA" -> Cancelada
        | "FALHA" -> Falha
        | _ -> Agendada

    let private parseTipoParticipante (tipo: string) =
        match tipo.ToUpper() with
        | "PACIENTE" -> Paciente
        | "MEDICO" -> Medico
        | "OBSERVADOR" -> Observador
        | "ACOMPANHANTE" -> Acompanhante
        | _ -> Paciente

    let private statusSessaoToString (status: StatusSessao) =
        match status with
        | Agendada -> "AGENDADA"
        | Iniciada -> "INICIADA"
        | EmAndamento -> "EMANDAMENTO"
        | Finalizada -> "FINALIZADA"
        | Cancelada -> "CANCELADA"
        | Falha -> "FALHA"

    let private tipoParticipanteToString (tipo: TipoParticipante) =
        match tipo with
        | Paciente -> "PACIENTE"
        | Medico -> "MEDICO"
        | Observador -> "OBSERVADOR"
        | Acompanhante -> "ACOMPANHANTE"

    // Funções auxiliares para gerar links e tokens
    let private gerarTokenSessao () =
        let guid = System.Guid.NewGuid().ToString("N")
        guid.Substring(0, 16).ToUpper()

    let private gerarSenhaPaciente () =
        let random = System.Random()
        let numeros = [|0..9|]
        Array.init 6 (fun _ -> numeros.[random.Next(numeros.Length)]) 
        |> Array.map string 
        |> String.concat ""

    let private gerarLinkSessao (sessaoId: int) (token: string) =
        $"https://telemedicina.sghss.com/sessao/{sessaoId}?token={token}"

    // Repository functions for SessaoTelemedicina
    let getAll (profissionalId: int option) (status: StatusSessao option) (data: DateTime option) =
        task {
            let mutable query = """
                SELECT id, agendamento_id, paciente_id, profissional_id, link_sessao, token_acesso,
                       senha_paciente, data_inicio, data_fim, status, gravacao_url, gravacao_permitida,
                       observacoes_finais, prontuario_id, qualidade_conexao, plataforma_video,
                       data_criacao, data_atualizacao, cancelado_por, motivo_cancel
                FROM sessoes_telemedicina 
                WHERE 1=1
            """
            let mutable parameters = []

            match profissionalId with
            | Some pid ->
                query <- query + " AND profissional_id = @profissional_id"
                parameters <- ("profissional_id", Sql.int pid) :: parameters
            | None -> ()

            match status with
            | Some s ->
                query <- query + " AND status = @status"
                parameters <- ("status", Sql.string (statusSessaoToString s)) :: parameters
            | None -> ()

            match data with
            | Some d ->
                query <- query + " AND DATE(data_criacao) = DATE(@data)"
                parameters <- ("data", Sql.timestamp d) :: parameters
            | None -> ()

            query <- query + " ORDER BY data_criacao DESC"

            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query query
                |> Sql.parameters parameters
                |> Sql.executeAsync (fun read -> {
                    Id = read.int "id"
                    AgendamentoId = read.int "agendamento_id"
                    PacienteId = read.int "paciente_id"
                    ProfissionalId = read.int "profissional_id"
                    LinkSessao = read.string "link_sessao"
                    TokenAcesso = read.string "token_acesso"
                    SenhaPaciente = read.stringOrNone "senha_paciente"
                    DataInicio = read.dateTimeOrNone "data_inicio"
                    DataFim = read.dateTimeOrNone "data_fim"
                    Status = parseStatusSessao (read.string "status")
                    GravacaoUrl = read.stringOrNone "gravacao_url"
                    GravacaoPermitida = read.bool "gravacao_permitida"
                    ObservacoesFinais = read.stringOrNone "observacoes_finais"
                    ProntuarioId = read.intOrNone "prontuario_id"
                    QualidadeConexao = read.stringOrNone "qualidade_conexao"
                    PlataformaVideo = read.string "plataforma_video"
                    DataCriacao = read.dateTime "data_criacao"
                    DataAtualizacao = read.dateTimeOrNone "data_atualizacao"
                    CanceladoPor = read.intOrNone "cancelado_por"
                    MotivoCancel = read.stringOrNone "motivo_cancel"
                })
        }

    let getById (id: int) =
        task {
            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT id, agendamento_id, paciente_id, profissional_id, link_sessao, token_acesso,
                           senha_paciente, data_inicio, data_fim, status, gravacao_url, gravacao_permitida,
                           observacoes_finais, prontuario_id, qualidade_conexao, plataforma_video,
                           data_criacao, data_atualizacao, cancelado_por, motivo_cancel
                    FROM sessoes_telemedicina 
                    WHERE id = @id
                """
                |> Sql.parameters ["id", Sql.int id]
                |> Sql.executeRowAsync (fun read -> {
                    Id = read.int "id"
                    AgendamentoId = read.int "agendamento_id"
                    PacienteId = read.int "paciente_id"
                    ProfissionalId = read.int "profissional_id"
                    LinkSessao = read.string "link_sessao"
                    TokenAcesso = read.string "token_acesso"
                    SenhaPaciente = read.stringOrNone "senha_paciente"
                    DataInicio = read.dateTimeOrNone "data_inicio"
                    DataFim = read.dateTimeOrNone "data_fim"
                    Status = parseStatusSessao (read.string "status")
                    GravacaoUrl = read.stringOrNone "gravacao_url"
                    GravacaoPermitida = read.bool "gravacao_permitida"
                    ObservacoesFinais = read.stringOrNone "observacoes_finais"
                    ProntuarioId = read.intOrNone "prontuario_id"
                    QualidadeConexao = read.stringOrNone "qualidade_conexao"
                    PlataformaVideo = read.string "plataforma_video"
                    DataCriacao = read.dateTime "data_criacao"
                    DataAtualizacao = read.dateTimeOrNone "data_atualizacao"
                    CanceladoPor = read.intOrNone "cancelado_por"
                    MotivoCancel = read.stringOrNone "motivo_cancel"
                })
        }

    let getSessaoDetalhes (id: int) =
        task {
            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT 
                        s.id, s.link_sessao, s.senha_paciente, s.status, s.data_inicio, s.data_fim,
                        s.gravacao_url, s.gravacao_permitida, s.plataforma_video, s.qualidade_conexao,
                        s.observacoes_finais, s.data_criacao, s.motivo_cancel,
                        a.id as agendamento_id, a.data_hora, a.duracao,
                        p.id as paciente_id, p.nome as paciente_nome, p.email as paciente_email, p.telefone as paciente_telefone,
                        pr.id as profissional_id, pr.nome as profissional_nome, pr.crm as profissional_crm, pr.email as profissional_email
                    FROM sessoes_telemedicina s
                    INNER JOIN agendamentos a ON s.agendamento_id = a.id
                    INNER JOIN pacientes p ON s.paciente_id = p.id
                    INNER JOIN profissionais pr ON s.profissional_id = pr.id
                    WHERE s.id = @id
                """
                |> Sql.parameters ["id", Sql.int id]
                |> Sql.executeRowAsync (fun read -> 
                    let dataInicio = read.dateTimeOrNone "data_inicio"
                    let dataFim = read.dateTimeOrNone "data_fim"
                    let duracaoReal = 
                        match dataInicio, dataFim with
                        | Some inicio, Some fim -> Some (fim - inicio)
                        | _ -> None

                    {
                        Id = read.int "id"
                        Agendamento = {|
                            Id = read.int "agendamento_id"
                            DataHora = read.dateTime "data_hora"
                            Duracao = read.interval "duracao"
                        |}
                        Paciente = {|
                            Id = read.int "paciente_id"
                            Nome = read.string "paciente_nome"
                            Email = read.stringOrNone "paciente_email"
                            Telefone = read.string "paciente_telefone"
                        |}
                        Profissional = {|
                            Id = read.int "profissional_id"
                            Nome = read.string "profissional_nome"
                            CRM = read.stringOrNone "profissional_crm"
                            Email = read.string "profissional_email"
                        |}
                        LinkSessao = read.string "link_sessao"
                        SenhaPaciente = read.stringOrNone "senha_paciente"
                        Status = parseStatusSessao (read.string "status")
                        DataInicio = dataInicio
                        DataFim = dataFim
                        DuracaoReal = duracaoReal
                        GravacaoUrl = read.stringOrNone "gravacao_url"
                        GravacaoPermitida = read.bool "gravacao_permitida"
                        PlataformaVideo = read.string "plataforma_video"
                        Participantes = [] // Será carregado separadamente
                        QualidadeConexao = read.stringOrNone "qualidade_conexao"
                        ObservacoesFinais = read.stringOrNone "observacoes_finais"
                        DataCriacao = read.dateTime "data_criacao"
                        MotivoCancel = read.stringOrNone "motivo_cancel"
                    })
        }
    let registrarHistorico (sessaoId: int) (evento: string) (descricao: string) (usuarioId: int option) (dadosAdicionais: string option) =
        task {
            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    INSERT INTO historico_sessoes 
                    (sessao_id, evento, descricao, usuario_id, dados_adicionais)
                    VALUES (@sessao_id, @evento, @descricao, @usuario_id, @dados_adicionais)
                """
                |> Sql.parameters [
                    "sessao_id", Sql.int sessaoId
                    "evento", Sql.string evento
                    "descricao", Sql.string descricao
                    "usuario_id", Sql.intOrNone usuarioId
                    "dados_adicionais", Sql.stringOrNone dadosAdicionais
                ]
                |> Sql.executeNonQueryAsync
        }
    let insert (input: SessaoTelemedicinInput) =
        task {
            let token = gerarTokenSessao()
            let senhaPaciente = gerarSenhaPaciente()
            // TODO: Criar uma lógica para gerar uma sessão em alguma solução self hosted.
            let linkSessao = sprintf "http://localhost:58078/api/v1/sessao/%s" (Guid.NewGuid().ToString())
            let! sessaoId =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    INSERT INTO sessoes_telemedicina 
                    (agendamento_id, paciente_id, profissional_id, token_acesso,senha_paciente,link_sessao,
                     gravacao_permitida, plataforma_video, observacoes_finais)
                    VALUES 
                    (@agendamento_id, @paciente_id, @profissional_id, @token_acesso,@senha_paciente, @link_sessao,
                     @gravacao_permitida, @plataforma_video, @observacoes_finais)
                    RETURNING id
                """
                |> Sql.parameters [
                    "agendamento_id", Sql.int input.AgendamentoId
                    "paciente_id", Sql.int input.PacienteId
                    "profissional_id", Sql.int input.ProfissionalId
                    "token_acesso", Sql.string token
                    "senha_paciente", Sql.string senhaPaciente
                    "gravacao_permitida", Sql.bool input.GravacaoPermitida
                    "plataforma_video", Sql.string input.PlataformaVideo
                    "observacoes_finais", Sql.stringOrNone input.ObservacoesIniciais
                    "link_sessao", Sql.string linkSessao
                ]
                |> Sql.executeRowAsync (fun read -> read.int "id")

            // Atualizar com o link da sessão
            let linkSessao = gerarLinkSessao sessaoId token
            let! _ =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    UPDATE sessoes_telemedicina 
                    SET link_sessao = @link_sessao
                    WHERE id = @id
                """
                |> Sql.parameters [
                    "id", Sql.int sessaoId
                    "link_sessao", Sql.string linkSessao
                ]
                |> Sql.executeNonQueryAsync

            // Registrar no histórico
            let! _ = registrarHistorico sessaoId "SESSAO_CRIADA" "Sessão de telemedicina criada" None None

            return sessaoId
        }    

    let updateStatus (id: int) (novoStatus: StatusSessao) (usuarioId: int option) (observacoes: string option) =
        task {
            let mutable updateFields = "status = @status, data_atualizacao = CURRENT_TIMESTAMP"
            let mutable parameters = [
                "id", Sql.int id
                "status", Sql.string (statusSessaoToString novoStatus)
            ]

            match novoStatus with
            | Iniciada ->
                updateFields <- updateFields + ", data_inicio = CURRENT_TIMESTAMP"
            | Finalizada ->
                updateFields <- updateFields + ", data_fim = CURRENT_TIMESTAMP"
            | Cancelada ->
                updateFields <- updateFields + ", cancelado_por = @cancelado_por, motivo_cancel = @motivo_cancel"
                parameters <- ("cancelado_por", Sql.intOrNone usuarioId) :: ("motivo_cancel", Sql.stringOrNone observacoes) :: parameters
            | _ -> ()

            let query = $"UPDATE sessoes_telemedicina SET {updateFields} WHERE id = @id"

            let! rowsAffected =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query query
                |> Sql.parameters parameters
                |> Sql.executeNonQueryAsync
            
            if rowsAffected > 0 then
                let eventoDescricao = $"Status alterado para {statusSessaoToString novoStatus}"
                let! _ = registrarHistorico id "STATUS_ALTERADO" eventoDescricao usuarioId observacoes
                ()

            return rowsAffected > 0
        }

    let adicionarParticipante (input: EntrarSessaoInput) =
        task {
            // Verificar se a sessão existe e está ativa
            let! sessaoExiste =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT COUNT(*) FROM sessoes_telemedicina 
                    WHERE id = @id AND status IN ('AGENDADA', 'INICIADA', 'EMANDAMENTO')
                """
                |> Sql.parameters ["id", Sql.int input.SessaoId]
                |> Sql.executeRowAsync (fun read -> read.int "count" > 0)

            if not sessaoExiste then
                return Error "Sessão não encontrada ou não está disponível para entrada"
            else
                // Verificar se participante já está na sessão
                let! jaParticipando =
                    DbConnection.getConnectionString()
                    |> Sql.connect
                    |> Sql.query """
                        SELECT COUNT(*) FROM participantes_sessao 
                        WHERE sessao_id = @sessao_id AND usuario_id = @usuario_id AND ativo = true
                    """
                    |> Sql.parameters [
                        "sessao_id", Sql.int input.SessaoId
                        "usuario_id", Sql.int input.UsuarioId
                    ]
                    |> Sql.executeRowAsync (fun read -> read.int "count" > 0)

                if jaParticipando then
                    return Error "Usuário já está participando da sessão"
                else
                    let! participanteId =
                        DbConnection.getConnectionString()
                        |> Sql.connect
                        |> Sql.query """
                            INSERT INTO participantes_sessao 
                            (sessao_id, usuario_id, tipo_participante, nome, email, data_entrada, 
                             dispositivo_utilizado, ativo)
                            VALUES 
                            (@sessao_id, @usuario_id, @tipo_participante, @nome, @email, CURRENT_TIMESTAMP,
                             @dispositivo_utilizado, true)
                            RETURNING id
                        """
                        |> Sql.parameters [
                            "sessao_id", Sql.int input.SessaoId
                            "usuario_id", Sql.int input.UsuarioId
                            "tipo_participante", Sql.string (tipoParticipanteToString input.TipoParticipante)
                            "nome", Sql.string input.Nome
                            "email", Sql.stringOrNone input.Email
                            "dispositivo_utilizado", Sql.stringOrNone input.DispositivoUtilizado
                        ]
                        |> Sql.executeRowAsync (fun read -> read.int "id")

                    // Registrar entrada no histórico
                    let descricao = $"Participante {input.Nome} ({tipoParticipanteToString input.TipoParticipante}) entrou na sessão"
                    let! _ = registrarHistorico input.SessaoId "PARTICIPANTE_ENTROU" descricao (Some input.UsuarioId) None

                    // Se for o primeiro participante médico, iniciar sessão
                    if input.TipoParticipante = Medico then
                        let! _ = updateStatus input.SessaoId Iniciada (Some input.UsuarioId) None
                        ()

                    return Ok participanteId
        }

    let removerParticipante (sessaoId: int) (usuarioId: int) =
        task {
            let! rowsAffected =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    UPDATE participantes_sessao 
                    SET ativo = false,
                        data_saida = CURRENT_TIMESTAMP,
                        tempo_conectado = CURRENT_TIMESTAMP - data_entrada
                    WHERE sessao_id = @sessao_id AND usuario_id = @usuario_id AND ativo = true
                """
                |> Sql.parameters [
                    "sessao_id", Sql.int sessaoId
                    "usuario_id", Sql.int usuarioId
                ]
                |> Sql.executeNonQueryAsync

            if rowsAffected > 0 then
                let descricao = "Participante saiu da sessão"
                let! _ = registrarHistorico sessaoId "PARTICIPANTE_SAIU" descricao (Some usuarioId) None
                ()

            return rowsAffected > 0
        }

    let finalizarSessao (id: int) (usuarioId: int) (observacoes: string option) (qualidadeConexao: string option) (prontuarioId: int option) =
        task {
            // Remover todos os participantes ativos
            let! _ =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    UPDATE participantes_sessao 
                    SET ativo = false,
                        data_saida = CURRENT_TIMESTAMP,
                        tempo_conectado = CURRENT_TIMESTAMP - data_entrada
                    WHERE sessao_id = @sessao_id AND ativo = true
                """
                |> Sql.parameters ["sessao_id", Sql.int id]
                |> Sql.executeNonQueryAsync

            // Finalizar sessão
            let! rowsAffected =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    UPDATE sessoes_telemedicina 
                    SET status = 'FINALIZADA',
                        data_fim = CURRENT_TIMESTAMP,
                        data_atualizacao = CURRENT_TIMESTAMP,
                        observacoes_finais = @observacoes_finais,
                        qualidade_conexao = @qualidade_conexao,
                        prontuario_id = @prontuario_id
                    WHERE id = @id
                """
                |> Sql.parameters [
                    "id", Sql.int id
                    "observacoes_finais", Sql.stringOrNone observacoes
                    "qualidade_conexao", Sql.stringOrNone qualidadeConexao
                    "prontuario_id", Sql.intOrNone prontuarioId
                ]
                |> Sql.executeNonQueryAsync

            if rowsAffected > 0 then
                let descricao = "Sessão finalizada"
                let! _ = registrarHistorico id "SESSAO_FINALIZADA" descricao (Some usuarioId) observacoes
                ()

            return rowsAffected > 0
        }

    // Configurações de Telemedicina
    let getConfiguracaoProfissional (profissionalId: int) =
        task {
            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT id, profissional_id, plataforma_preferida, permite_gravacao, duracao_maxima_sessao,
                           permite_sala_espera, notificacoes_email, notificacoes_sms, 
                           horario_atendimento_inicio, horario_atendimento_fim, dias_atendimento,
                           ativo, data_criacao, data_atualizacao
                    FROM configuracoes_telemedicina 
                    WHERE profissional_id = @profissional_id
                """
                |> Sql.parameters ["profissional_id", Sql.int profissionalId]
                |> Sql.executeRowAsync (fun read -> 
                    let diasStr = read.string "dias_atendimento"
                    
                    {
                        Id = read.int "id"
                        ProfissionalId = read.int "profissional_id"
                        PlataformaPreferida = read.string "plataforma_preferida"
                        PermiteGravacao = read.bool "permite_gravacao"
                        DuracaoMaximaSessao = read.interval "duracao_maxima_sessao"
                        PermiteSalaEspera = read.bool "permite_sala_espera"
                        NotificacoesEmail = read.bool "notificacoes_email"
                        NotificacoesSms = read.bool "notificacoes_sms"
                        HorarioAtendimentoInicio = read.interval "horario_atendimento_inicio"
                        HorarioAtendimentoFim = read.interval "horario_atendimento_fim"
                        DiasAtendimento = diasStr
                        Ativo = read.bool "ativo"
                        DataCriacao = read.dateTime "data_criacao"
                        DataAtualizacao = read.dateTimeOrNone "data_atualizacao"
                    })
        }

    let upsertConfiguracaoProfissional (input: ConfiguracaoTelemedicinInput) =
        task {
            let diasStr = String.Join(",", input.DiasAtendimento |> List.map string)
            
            let! exists =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT COUNT(*) FROM configuracoes_telemedicina 
                    WHERE profissional_id = @profissional_id
                """
                |> Sql.parameters ["profissional_id", Sql.int input.ProfissionalId]
                |> Sql.executeRowAsync (fun read -> read.int "count" > 0)

            if exists then
                // Update
                let! rowsAffected =
                    DbConnection.getConnectionString()
                    |> Sql.connect
                    |> Sql.query """
                        UPDATE configuracoes_telemedicina 
                        SET plataforma_preferida = @plataforma_preferida,
                            permite_gravacao = @permite_gravacao,
                            duracao_maxima_sessao = @duracao_maxima_sessao,
                            permite_sala_espera = @permite_sala_espera,
                            notificacoes_email = @notificacoes_email,
                            notificacoes_sms = @notificacoes_sms,
                            horario_atendimento_inicio = @horario_atendimento_inicio,
                            horario_atendimento_fim = @horario_atendimento_fim,
                            dias_atendimento = @dias_atendimento,
                            data_atualizacao = CURRENT_TIMESTAMP
                        WHERE profissional_id = @profissional_id
                    """
                    |> Sql.parameters [
                        "profissional_id", Sql.int input.ProfissionalId
                        "plataforma_preferida", Sql.string input.PlataformaPreferida
                        "permite_gravacao", Sql.bool input.PermiteGravacao
                        "duracao_maxima_sessao", Sql.interval input.DuracaoMaximaSessao
                        "permite_sala_espera", Sql.bool input.PermiteSalaEspera
                        "notificacoes_email", Sql.bool input.NotificacoesEmail
                        "notificacoes_sms", Sql.bool input.NotificacoesSms
                        "horario_atendimento_inicio", Sql.interval input.HorarioAtendimentoInicio
                        "horario_atendimento_fim", Sql.interval input.HorarioAtendimentoFim
                        "dias_atendimento", Sql.string diasStr
                    ]
                    |> Sql.executeNonQueryAsync
                return rowsAffected > 0
            else
                // Insert
                let! id =
                    DbConnection.getConnectionString()
                    |> Sql.connect
                    |> Sql.query """
                        INSERT INTO configuracoes_telemedicina 
                        (profissional_id, plataforma_preferida, permite_gravacao, duracao_maxima_sessao,
                         permite_sala_espera, notificacoes_email, notificacoes_sms, 
                         horario_atendimento_inicio, horario_atendimento_fim, dias_atendimento)
                        VALUES 
                        (@profissional_id, @plataforma_preferida, @permite_gravacao, @duracao_maxima_sessao,
                         @permite_sala_espera, @notificacoes_email, @notificacoes_sms,
                         @horario_atendimento_inicio, @horario_atendimento_fim, @dias_atendimento)
                        RETURNING id
                    """
                    |> Sql.parameters [
                        "profissional_id", Sql.int input.ProfissionalId
                        "plataforma_preferida", Sql.string input.PlataformaPreferida
                        "permite_gravacao", Sql.bool input.PermiteGravacao
                        "duracao_maxima_sessao", Sql.interval input.DuracaoMaximaSessao
                        "permite_sala_espera", Sql.bool input.PermiteSalaEspera
                        "notificacoes_email", Sql.bool input.NotificacoesEmail
                        "notificacoes_sms", Sql.bool input.NotificacoesSms
                        "horario_atendimento_inicio", Sql.interval input.HorarioAtendimentoInicio
                        "horario_atendimento_fim", Sql.interval input.HorarioAtendimentoFim
                        "dias_atendimento", Sql.string diasStr
                    ]
                    |> Sql.executeRowAsync (fun read -> read.int "id")
                return id > 0
        }

    let getDashboard (data: DateTime) =
        task {
            let inicioDia = data.Date
            let fimDia = inicioDia.AddDays(1.0).AddTicks(-1L)

            let! sessoesHoje =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT COUNT(*) FROM sessoes_telemedicina 
                    WHERE DATE(data_criacao) = DATE(@data)
                """
                |> Sql.parameters ["data", Sql.timestamp data]
                |> Sql.executeRowAsync (fun read -> read.int "count")

            let! sessoesAguardando =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT COUNT(*) FROM sessoes_telemedicina 
                    WHERE status = 'AGENDADA'
                    AND DATE(data_criacao) = DATE(@data)
                """
                |> Sql.parameters ["data", Sql.timestamp data]
                |> Sql.executeRowAsync (fun read -> read.int "count")

            let! sessoesEmAndamento =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT COUNT(*) FROM sessoes_telemedicina 
                    WHERE status IN ('INICIADA', 'EMANDAMENTO')
                """
                |> Sql.parameters []
                |> Sql.executeRowAsync (fun read -> read.int "count")

            let! sessoesFinalizadas =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT COUNT(*) FROM sessoes_telemedicina 
                    WHERE status = 'FINALIZADA'
                    AND DATE(data_criacao) = DATE(@data)
                """
                |> Sql.parameters ["data", Sql.timestamp data]
                |> Sql.executeRowAsync (fun read -> read.int "count")

            let! sessoesCanceladas =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT COUNT(*) FROM sessoes_telemedicina 
                    WHERE status = 'CANCELADA'
                    AND DATE(data_criacao) = DATE(@data)
                """
                |> Sql.parameters ["data", Sql.timestamp data]
                |> Sql.executeRowAsync (fun read -> read.int "count")

            let! tempoMedio =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT AVG(EXTRACT(EPOCH FROM (data_fim - data_inicio))) as media_segundos
                    FROM sessoes_telemedicina 
                    WHERE status = 'FINALIZADA'
                    AND data_inicio IS NOT NULL 
                    AND data_fim IS NOT NULL
                    AND DATE(data_criacao) = DATE(@data)
                """
                |> Sql.parameters ["data", Sql.timestamp data]
                |> Sql.executeRowAsync (fun read -> 
                    match read.decimalOrNone "media_segundos" with
                    | Some segundos -> Some (TimeSpan.FromSeconds(float segundos))
                    | None -> None)

            let! profissionaisOnline =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT COUNT(DISTINCT profissional_id) 
                    FROM sessoes_telemedicina 
                    WHERE status IN ('INICIADA', 'EMANDAMENTO')
                """
                |> Sql.parameters []
                |> Sql.executeRowAsync (fun read -> read.int "count")

            let! pacientesAguardando =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT COUNT(DISTINCT paciente_id) 
                    FROM sessoes_telemedicina 
                    WHERE status = 'AGENDADA'
                    AND DATE(data_criacao) = DATE(@data)
                """
                |> Sql.parameters ["data", Sql.timestamp data]
                |> Sql.executeRowAsync (fun read -> read.int "count")

            // Próximas sessões (próximas 4 horas)
            let! proximasSessoes =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT 
                        s.id, s.link_sessao, s.senha_paciente, s.status, s.data_inicio, s.data_fim,
                        s.gravacao_url, s.gravacao_permitida, s.plataforma_video, s.qualidade_conexao,
                        s.observacoes_finais, s.data_criacao, s.motivo_cancel,
                        a.id as agendamento_id, a.data_hora, a.duracao,
                        p.id as paciente_id, p.nome as paciente_nome, p.email as paciente_email, p.telefone as paciente_telefone,
                        pr.id as profissional_id, pr.nome as profissional_nome, pr.crm as profissional_crm, pr.email as profissional_email
                    FROM sessoes_telemedicina s
                    INNER JOIN agendamentos a ON s.agendamento_id = a.id
                    INNER JOIN pacientes p ON s.paciente_id = p.id
                    INNER JOIN profissionais pr ON s.profissional_id = pr.id
                    WHERE s.status IN ('AGENDADA', 'INICIADA')
                    AND a.data_hora BETWEEN CURRENT_TIMESTAMP AND (CURRENT_TIMESTAMP + INTERVAL '4 hours')
                    ORDER BY a.data_hora
                    LIMIT 10
                """
                |> Sql.parameters []
                |> Sql.executeAsync (fun read -> 
                    let dataInicio = read.dateTimeOrNone "data_inicio"
                    let dataFim = read.dateTimeOrNone "data_fim"
                    let duracaoReal = 
                        match dataInicio, dataFim with
                        | Some inicio, Some fim -> Some (fim - inicio)
                        | _ -> None

                    {
                        Id = read.int "id"
                        Agendamento = {|
                            Id = read.int "agendamento_id"
                            DataHora = read.dateTime "data_hora"
                            Duracao = read.interval "duracao"
                        |}
                        Paciente = {|
                            Id = read.int "paciente_id"
                            Nome = read.string "paciente_nome"
                            Email = read.stringOrNone "paciente_email"
                            Telefone = read.string "paciente_telefone"
                        |}
                        Profissional = {|
                            Id = read.int "profissional_id"
                            Nome = read.string "profissional_nome"
                            CRM = read.stringOrNone "profissional_crm"
                            Email = read.string "profissional_email"
                        |}
                        LinkSessao = read.string "link_sessao"
                        SenhaPaciente = read.stringOrNone "senha_paciente"
                        Status = parseStatusSessao (read.string "status")
                        DataInicio = dataInicio
                        DataFim = dataFim
                        DuracaoReal = duracaoReal
                        GravacaoUrl = read.stringOrNone "gravacao_url"
                        GravacaoPermitida = read.bool "gravacao_permitida"
                        PlataformaVideo = read.string "plataforma_video"
                        Participantes = []
                        QualidadeConexao = read.stringOrNone "qualidade_conexao"
                        ObservacoesFinais = read.stringOrNone "observacoes_finais"
                        DataCriacao = read.dateTime "data_criacao"
                        MotivoCancel = read.stringOrNone "motivo_cancel"
                    })

            return {
                SessoesHoje = sessoesHoje
                SessoesAguardando = sessoesAguardando
                SessoesEmAndamento = sessoesEmAndamento
                SessoesFinalizadas = sessoesFinalizadas
                SessoesCanceladas = sessoesCanceladas
                TempoMedioSessao = tempoMedio
                ProximasSessoes = proximasSessoes
                ProfissionaisOnline = profissionaisOnline
                PacientesAguardando = pacientesAguardando
            }
        }

// Telemedicina/Handler.fs
module Handler =
    open Giraffe    
    open Models

    // DTOs
    type SessaoTelemedicinResponse = {
        Id: int
        AgendamentoId: int
        PacienteId: int
        ProfissionalId: int
        LinkSessao: string
        SenhaPaciente: string option
        Status: string
        DataInicio: DateTime option
        DataFim: DateTime option
        DuracaoReal: string option
        GravacaoUrl: string option
        GravacaoPermitida: bool
        PlataformaVideo: string
        QualidadeConexao: string option
        ObservacoesFinais: string option
        DataCriacao: DateTime
        MotivoCancel: string option
    }
    [<CLIMutable>]
    type SessaoTelemedicinInputDto = {
        AgendamentoId: int
        PacienteId: int
        ProfissionalId: int
        GravacaoPermitida: bool
        PlataformaVideo: string
        ObservacoesIniciais: string
    }

    type EntrarSessaoDto = {
        UsuarioId: int
        TipoParticipante: string
        Nome: string
        Email: string option
        DispositivoUtilizado: string option
    }

    type FinalizarSessaoDto = {
        Observacoes: string option
        QualidadeConexao: string option
        ProntuarioId: int option
    }

    type ConfiguracaoTelemedicinDto = {
        ProfissionalId: int
        PlataformaPreferida: string
        PermiteGravacao: bool
        DuracaoMaximaSessaoMinutos: int
        PermiteSalaEspera: bool
        NotificacoesEmail: bool
        NotificacoesSms: bool
        HorarioAtendimentoInicio: string // "HH:mm"
        HorarioAtendimentoFim: string // "HH:mm"
        DiasAtendimento: int list // 1-7 (segunda a domingo)
    }

    // Funções auxiliares de conversão
    let private toResponse (sessao: SessaoTelemedicina) : SessaoTelemedicinResponse =
        let statusString = 
            match sessao.Status with
            | Agendada -> "AGENDADA"
            | Iniciada -> "INICIADA"
            | EmAndamento -> "EMANDAMENTO"
            | Finalizada -> "FINALIZADA"
            | Cancelada -> "CANCELADA"
            | Falha -> "FALHA"

        let duracaoReal = 
            match sessao.DataInicio, sessao.DataFim with
            | Some inicio, Some fim -> 
                let duracao = fim - inicio
                Some (duracao.ToString(@"hh\:mm\:ss"))
            | _ -> None

        {
            Id = sessao.Id
            AgendamentoId = sessao.AgendamentoId
            PacienteId = sessao.PacienteId
            ProfissionalId = sessao.ProfissionalId
            LinkSessao = sessao.LinkSessao
            SenhaPaciente = sessao.SenhaPaciente
            Status = statusString
            DataInicio = sessao.DataInicio
            DataFim = sessao.DataFim
            DuracaoReal = duracaoReal
            GravacaoUrl = sessao.GravacaoUrl
            GravacaoPermitida = sessao.GravacaoPermitida
            PlataformaVideo = sessao.PlataformaVideo
            QualidadeConexao = sessao.QualidadeConexao
            ObservacoesFinais = sessao.ObservacoesFinais
            DataCriacao = sessao.DataCriacao
            MotivoCancel = sessao.MotivoCancel
        }

    let private toDomainInput (dto: SessaoTelemedicinInputDto) : SessaoTelemedicinInput =
        {
            AgendamentoId = dto.AgendamentoId
            PacienteId = dto.PacienteId
            ProfissionalId = dto.ProfissionalId
            GravacaoPermitida = dto.GravacaoPermitida
            PlataformaVideo = dto.PlataformaVideo
            ObservacoesIniciais = dto.ObservacoesIniciais |> Utils.toOptionStrIfNull
        }

    let private toConfigDomainInput (dto: ConfiguracaoTelemedicinDto) : ConfiguracaoTelemedicinInput =
        let duracaoMaxima = TimeSpan.FromMinutes(float dto.DuracaoMaximaSessaoMinutos)
        let horarioInicio = TimeSpan.Parse(dto.HorarioAtendimentoInicio)
        let horarioFim = TimeSpan.Parse(dto.HorarioAtendimentoFim)

        {
            ProfissionalId = dto.ProfissionalId
            PlataformaPreferida = dto.PlataformaPreferida
            PermiteGravacao = dto.PermiteGravacao
            DuracaoMaximaSessao = duracaoMaxima
            PermiteSalaEspera = dto.PermiteSalaEspera
            NotificacoesEmail = dto.NotificacoesEmail
            NotificacoesSms = dto.NotificacoesSms
            HorarioAtendimentoInicio = horarioInicio
            HorarioAtendimentoFim = horarioFim
            DiasAtendimento = dto.DiasAtendimento
        }

    // Validações
    let private validateSessaoInput (dto: SessaoTelemedicinInputDto) =
        let errors = ResizeArray<string>()
        
        if dto.AgendamentoId <= 0 then
            errors.Add("Agendamento deve ser especificado")
        
        if dto.PacienteId <= 0 then
            errors.Add("Paciente deve ser especificado")
        
        if dto.ProfissionalId <= 0 then
            errors.Add("Profissional deve ser especificado")
        
        if String.IsNullOrWhiteSpace(dto.PlataformaVideo) then
            errors.Add("Plataforma de vídeo deve ser especificada")

        match dto.PlataformaVideo.ToUpper() with
        | "ZOOM" | "TEAMS" | "JITSI" | "GOOGLE_MEET" | "WEBEX" -> ()
        | _ -> errors.Add("Plataforma de vídeo inválida")

        errors |> Seq.toList

    let private validateConfigInput (dto: ConfiguracaoTelemedicinDto) =
        let errors = ResizeArray<string>()
        
        if dto.ProfissionalId <= 0 then
            errors.Add("Profissional deve ser especificado")
        
        if String.IsNullOrWhiteSpace(dto.PlataformaPreferida) then
            errors.Add("Plataforma preferida deve ser especificada")
        
        if dto.DuracaoMaximaSessaoMinutos <= 0 || dto.DuracaoMaximaSessaoMinutos > 480 then
            errors.Add("Duração máxima deve estar entre 1 e 480 minutos")

        try
            TimeSpan.Parse(dto.HorarioAtendimentoInicio) |> ignore
        with
        | _ -> errors.Add("Formato de horário de início inválido (use HH:mm)")

        try
            TimeSpan.Parse(dto.HorarioAtendimentoFim) |> ignore
        with
        | _ -> errors.Add("Formato de horário de fim inválido (use HH:mm)")

        if dto.DiasAtendimento.IsEmpty || dto.DiasAtendimento |> List.exists (fun d -> d < 1 || d > 7) then
            errors.Add("Dias de atendimento devem estar entre 1 (segunda) e 7 (domingo)")

        errors |> Seq.toList

    // Handlers
    let getAllSessoes : HttpHandler =
        fun next ctx ->
            task {
                try
                    let profissionalId = 
                        match ctx.TryGetQueryStringValue "profissionalId" with
                        | Some idStr -> Int32.TryParse(idStr) |> function | true, id -> Some id | _ -> None
                        | None -> None

                    let status = 
                        match ctx.TryGetQueryStringValue "status" with
                        | Some statusStr -> 
                            match statusStr.ToUpper() with
                            | "AGENDADA" -> Some Agendada
                            | "INICIADA" -> Some Iniciada
                            | "EMANDAMENTO" -> Some EmAndamento
                            | "FINALIZADA" -> Some Finalizada
                            | "CANCELADA" -> Some Cancelada
                            | "FALHA" -> Some Falha
                            | _ -> None
                        | None -> None

                    let data = 
                        match ctx.TryGetQueryStringValue "data" with
                        | Some dateStr -> DateTime.TryParse(dateStr) |> function | true, dt -> Some dt | _ -> None
                        | None -> None

                    let! sessoes = Repository.getAll profissionalId status data
                    let response = sessoes |> List.map toResponse
                    
                    return! json response next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro interno do servidor"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let getSessaoById sessaoId : HttpHandler =
        fun next ctx ->
            task {
                try
                    let! sessao = Repository.getById sessaoId
                    let response = toResponse sessao
                    
                    return! json response next ctx
                with
                | :? System.InvalidOperationException ->
                    let errorResponse = {| error = "Sessão não encontrada" |}
                    return! (setStatusCode 404 >=> json errorResponse) next ctx
                | ex ->
                    let errorResponse = {| error = "Erro interno do servidor"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let getSessaoDetalhes sessaoId : HttpHandler =
        fun next ctx ->
            task {
                try
                    let! detalhes = Repository.getSessaoDetalhes sessaoId
                    return! json detalhes next ctx
                with
                | :? System.InvalidOperationException ->
                    let errorResponse = {| error = "Sessão não encontrada" |}
                    return! (setStatusCode 404 >=> json errorResponse) next ctx
                | ex ->
                    let errorResponse = {| error = "Erro interno do servidor"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let createSessao : HttpHandler =
        fun next ctx ->
            task {
                try
                    let! inputDto = ctx.BindJsonAsync<SessaoTelemedicinInputDto>()
                    
                    let validationErrors = validateSessaoInput inputDto
                    if not validationErrors.IsEmpty then
                        let errorResponse = {| errors = validationErrors |}
                        return! (setStatusCode 400 >=> json errorResponse) next ctx
                    else
                        let domainInput = toDomainInput inputDto
                        let! id = Repository.insert domainInput
                        Logger.logger.Information("Sessão com Id = {id} foi criada!",id)
                        // Buscar a sessão criada para retornar os dados completos
                        let! sessao = Repository.getById id
                        let response = {| 
                            id = id
                            linkSessao = sessao.LinkSessao
                            senhaPaciente = sessao.SenhaPaciente |> Option.defaultValue ""
                            message = "Sessão de telemedicina criada com sucesso" 
                        |}
                        
                        return! (setStatusCode 201 >=> json response) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao criar sessão"; details = ex.Message |}
                    Logger.logger.Error("Erro interno do servidor:{ex}",ex)
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let entrarSessao sessaoId : HttpHandler =
        fun next ctx ->
            task {
                try
                    let! entradaDto = ctx.BindJsonAsync<EntrarSessaoDto>()
                    
                    let tipoParticipante = 
                        match entradaDto.TipoParticipante.ToUpper() with
                        | "PACIENTE" -> Paciente
                        | "MEDICO" -> Medico
                        | "OBSERVADOR" -> Observador
                        | "ACOMPANHANTE" -> Acompanhante
                        | _ -> failwith $"Tipo de participante inválido: {entradaDto.TipoParticipante}"

                    let entradaInput : EntrarSessaoInput = {
                        SessaoId = sessaoId
                        UsuarioId = entradaDto.UsuarioId
                        TipoParticipante = tipoParticipante
                        Nome = entradaDto.Nome
                        Email = entradaDto.Email
                        DispositivoUtilizado = entradaDto.DispositivoUtilizado
                    }
                    
                    let! result = Repository.adicionarParticipante entradaInput
                    
                    match result with
                    | Ok participanteId ->
                        let response = {| 
                            participanteId = participanteId
                            message = "Participante adicionado à sessão com sucesso" 
                        |}
                        return! (setStatusCode 201 >=> json response) next ctx
                    | Error erro ->
                        let errorResponse = {| error = erro |}
                        return! (setStatusCode 400 >=> json errorResponse) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao entrar na sessão"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let sairSessao (sessaoId,usuarioId) : HttpHandler =
        fun next ctx ->
            task {
                try
                    let! success = Repository.removerParticipante sessaoId usuarioId
                    
                    if success then
                        let response = {| message = "Participante removido da sessão" |}
                        return! json response next ctx
                    else
                        let errorResponse = {| error = "Participante não encontrado na sessão" |}
                        return! (setStatusCode 404 >=> json errorResponse) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao sair da sessão"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let finalizarSessao sessaoId : HttpHandler =
        fun next ctx ->
            task {
                try
                    let! finalizarDto = ctx.BindJsonAsync<FinalizarSessaoDto>()
                    
                    // TODO: Obter usuarioId do token JWT
                    let usuarioId = 1 // Placeholder - deve vir da autenticação
                    
                    let! success = Repository.finalizarSessao sessaoId usuarioId finalizarDto.Observacoes finalizarDto.QualidadeConexao finalizarDto.ProntuarioId
                    
                    if success then
                        let response = {| message = "Sessão finalizada com sucesso" |}
                        return! json response next ctx
                    else
                        let errorResponse = {| error = "Sessão não encontrada" |}
                        return! (setStatusCode 404 >=> json errorResponse) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao finalizar sessão"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let cancelarSessao sessaoId : HttpHandler =
        fun next ctx ->
            task {
                try                    
                    let! cancelDto = ctx.BindJsonAsync<{| motivo: string |}>()
                    
                    // TODO: Obter usuarioId do token JWT
                    let usuarioId = 1 // Placeholder - deve vir da autenticação
                    
                    let! success = Repository.updateStatus sessaoId Cancelada (Some usuarioId) (Some cancelDto.motivo)
                    
                    if success then
                        let response = {| message = "Sessão cancelada com sucesso" |}
                        return! json response next ctx
                    else
                        let errorResponse = {| error = "Sessão não encontrada" |}
                        return! (setStatusCode 404 >=> json errorResponse) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao cancelar sessão"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let getConfiguracoes profissionalId : HttpHandler =
        fun next ctx ->
            task {
                try                    
                    
                    let! config = Repository.getConfiguracaoProfissional profissionalId
                    
                    let diasList = 
                        config.DiasAtendimento.Split(',') 
                        |> Array.choose (fun s -> Int32.TryParse(s.Trim()) |> function | true, i -> Some i | false, _ -> None)
                        |> Array.toList

                    let response = {|
                        id = config.Id
                        profissionalId = config.ProfissionalId
                        plataformaPreferida = config.PlataformaPreferida
                        permiteGravacao = config.PermiteGravacao
                        duracaoMaximaSessaoMinutos = int config.DuracaoMaximaSessao.TotalMinutes
                        permiteSalaEspera = config.PermiteSalaEspera
                        notificacoesEmail = config.NotificacoesEmail
                        notificacoesSms = config.NotificacoesSms
                        horarioAtendimentoInicio = config.HorarioAtendimentoInicio.ToString(@"hh\:mm")
                        horarioAtendimentoFim = config.HorarioAtendimentoFim.ToString(@"hh\:mm")
                        diasAtendimento = diasList
                        ativo = config.Ativo
                        dataCriacao = config.DataCriacao
                        dataAtualizacao = config.DataAtualizacao
                    |}
                    
                    return! json response next ctx
                with
                | :? System.InvalidOperationException ->
                    let errorResponse = {| error = "Configurações não encontradas" |}
                    return! (setStatusCode 404 >=> json errorResponse) next ctx
                | ex ->
                    let errorResponse = {| error = "Erro interno do servidor"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let updateConfiguracoes : HttpHandler =
        fun next ctx ->
            task {
                try
                    let! configDto = ctx.BindJsonAsync<ConfiguracaoTelemedicinDto>()
                    
                    let validationErrors = validateConfigInput configDto
                    if not validationErrors.IsEmpty then
                        let errorResponse = {| errors = validationErrors |}
                        return! (setStatusCode 400 >=> json errorResponse) next ctx
                    else
                        let domainInput = toConfigDomainInput configDto
                        let! success = Repository.upsertConfiguracaoProfissional domainInput
                        
                        if success then
                            let response = {| message = "Configurações atualizadas com sucesso" |}
                            return! json response next ctx
                        else
                            let errorResponse = {| error = "Erro ao atualizar configurações" |}
                            return! (setStatusCode 500 >=> json errorResponse) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao atualizar configurações"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let getDashboard : HttpHandler =
        fun next ctx ->
            task {
                try
                    let data = 
                        match ctx.TryGetQueryStringValue "data" with
                        | Some dateStr -> 
                            match DateTime.TryParse(dateStr) with
                            | true, dt -> dt
                            | false, _ -> DateTime.Today
                        | None -> DateTime.Today

                    let! dashboard = Repository.getDashboard data
                    
                    let response = {|
                        data = data.ToString("yyyy-MM-dd")
                        sessoesHoje = dashboard.SessoesHoje
                        sessoesAguardando = dashboard.SessoesAguardando
                        sessoesEmAndamento = dashboard.SessoesEmAndamento
                        sessoesFinalizadas = dashboard.SessoesFinalizadas
                        sessoesCanceladas = dashboard.SessoesCanceladas
                        tempoMedioSessao = 
                            match dashboard.TempoMedioSessao with
                            | Some tempo -> Some (tempo.ToString(@"hh\:mm\:ss"))
                            | None -> None
                        proximasSessoes = dashboard.ProximasSessoes
                        profissionaisOnline = dashboard.ProfissionaisOnline
                        pacientesAguardando = dashboard.PacientesAguardando
                    |}
                    
                    return! json response next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao obter dashboard"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let getStatusSessao sessaoId : HttpHandler =
        fun next ctx ->
            task {
                try                    
                    
                    let! sessao = Repository.getById sessaoId
                    
                    let statusResponse = {|
                        id = sessao.Id
                        status = match sessao.Status with
                                 | Agendada -> "AGENDADA"
                                 | Iniciada -> "INICIADA"
                                 | EmAndamento -> "EMANDAMENTO"
                                 | Finalizada -> "FINALIZADA"
                                 | Cancelada -> "CANCELADA"
                                 | Falha -> "FALHA"
                        dataInicio = sessao.DataInicio
                        dataFim = sessao.DataFim
                        linkSessao = sessao.LinkSessao
                        plataformaVideo = sessao.PlataformaVideo
                        gravacaoPermitida = sessao.GravacaoPermitida
                        qualidadeConexao = sessao.QualidadeConexao
                    |}
                    
                    return! json statusResponse next ctx
                with
                | :? System.InvalidOperationException ->
                    let errorResponse = {| error = "Sessão não encontrada" |}
                    return! (setStatusCode 404 >=> json errorResponse) next ctx
                | ex ->
                    let errorResponse = {| error = "Erro interno do servidor"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let verificarDisponibilidadeTelemedicina profissionalId : HttpHandler =
        fun next ctx ->
            task {
                try                    
                    let dataHora = 
                        ctx.GetQueryStringValue "dataHora"
                        |> Result.map (fun str -> DateTime.Parse(str))
                        |> Result.defaultValue DateTime.MinValue

                    let diaSemana = int dataHora.DayOfWeek + 1 // Converter para 1-7
                    let horarioConsulta = dataHora.TimeOfDay
                    
                    // Verificar configurações do profissional
                    let! config = Repository.getConfiguracaoProfissional profissionalId
                    
                    let diasPermitidos = 
                        config.DiasAtendimento.Split(',') 
                        |> Array.choose (fun s -> Int32.TryParse(s.Trim()) |> function | true, i -> Some i | false, _ -> None)
                        |> Set.ofArray

                    let disponivel = 
                        config.Ativo &&
                        diasPermitidos.Contains(diaSemana) &&
                        horarioConsulta >= config.HorarioAtendimentoInicio &&
                        horarioConsulta <= config.HorarioAtendimentoFim
                    
                    let motivoIndisponibilidade = 
                        if not config.Ativo then 
                            Some "Profissional não habilitado para telemedicina"
                        elif not (diasPermitidos.Contains(diaSemana)) then 
                            Some "Profissional não atende neste dia da semana"
                        elif horarioConsulta < config.HorarioAtendimentoInicio || horarioConsulta > config.HorarioAtendimentoFim then 
                            let horarioInicio = config.HorarioAtendimentoInicio.ToString("hh:mm")
                            let horarioFim = config.HorarioAtendimentoFim.ToString("hh:mm")
                            Some $"Profissional atende das {horarioInicio} às {horarioFim}"
                        else 
                            None

                    let response = {|
                        disponivel = disponivel
                        dataHora = dataHora
                        profissionalId = profissionalId
                        plataformaPreferida = config.PlataformaPreferida
                        duracaoMaximaSessao = config.DuracaoMaximaSessao.ToString(@"hh\:mm")
                        motivoIndisponibilidade = motivoIndisponibilidade
                    |}
                    
                    return! json response next ctx
                with
                | :? System.InvalidOperationException ->
                    let errorResponse = {| error = "Profissional não possui configurações de telemedicina" |}
                    return! (setStatusCode 404 >=> json errorResponse) next ctx
                | ex ->
                    let errorResponse = {| error = "Erro ao verificar disponibilidade"; details = ex.Message |}
                    return! (setStatusCode 400 >=> json errorResponse) next ctx
            }

    // Rotas Telemedicina
    let routes : HttpHandler =
        choose [
            GET >=> choose [
                route "" >=> getAllSessoes
                routef "/%i" getSessaoById
                routef "/%i/detalhes" getSessaoDetalhes
                routef "/%i/status" getStatusSessao
                route "/dashboard" >=> getDashboard
                routef "/profissional/%i/configuracoes" getConfiguracoes
                routef "/profissional/%i/disponibilidade" verificarDisponibilidadeTelemedicina
            ]
            POST >=> choose [
                route "" >=> createSessao
                routef "/%i/entrar" entrarSessao
                routef "/%i/finalizar" finalizarSessao
            ]
            PUT >=> choose [
                route "/configuracoes" >=> updateConfiguracoes
            ]
            DELETE >=> choose [
                routef "/%i" cancelarSessao
                routef "/%i/sair/%i" sairSessao
            ]
        ]

// Database Schema Addition for Telemedicina
(*
-- Adicionar à migration SQL:

-- Tabela principal de sessões de telemedicina
CREATE TABLE IF NOT EXISTS sessoes_telemedicina (
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
    plataforma_video VARCHAR(50) NOT NULL DEFAULT 'JITSI', -- ZOOM, TEAMS, JITSI, GOOGLE_MEET, WEBEX
    data_criacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    data_atualizacao TIMESTAMP,
    cancelado_por INTEGER REFERENCES profissionais(id),
    motivo_cancel TEXT
);

-- Tabela de participantes das sessões
CREATE TABLE IF NOT EXISTS participantes_sessao (
    id SERIAL PRIMARY KEY,
    sessao_id INTEGER REFERENCES sessoes_telemedicina(id) NOT NULL,
    usuario_id INTEGER NOT NULL, -- Pode ser paciente_id ou profissional_id
    tipo_participante VARCHAR(20) NOT NULL CHECK (tipo_participante IN ('PACIENTE', 'MEDICO', 'OBSERVADOR', 'ACOMPANHANTE')),
    nome VARCHAR(200) NOT NULL,
    email VARCHAR(100),
    data_entrada TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    data_saida TIMESTAMP,
    tempo_conectado INTERVAL,
    ativo BOOLEAN DEFAULT true,
    dispositivo_utilizado VARCHAR(100), -- Mobile, Desktop, Tablet
    ip_address INET
);

-- Tabela de configurações de telemedicina por profissional
CREATE TABLE IF NOT EXISTS configuracoes_telemedicina (
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
    dias_atendimento VARCHAR(20) DEFAULT '1,2,3,4,5', -- 1=segunda, 7=domingo
    ativo BOOLEAN DEFAULT true,
    data_criacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    data_atualizacao TIMESTAMP
);

-- Tabela de histórico de eventos das sessões
CREATE TABLE IF NOT EXISTS historico_sessoes (
    id SERIAL PRIMARY KEY,
    sessao_id INTEGER REFERENCES sessoes_telemedicina(id) NOT NULL,
    evento VARCHAR(50) NOT NULL, -- SESSAO_CRIADA, PARTICIPANTE_ENTROU, SESSAO_INICIADA, etc.
    descricao TEXT NOT NULL,
    usuario_id INTEGER,
    timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    dados_adicionais JSONB -- Para dados extras como qualidade de conexão, erros, etc.
);

-- Índices para performance
CREATE INDEX IF NOT EXISTS idx_sessoes_telemedicina_profissional ON sessoes_telemedicina(profissional_id, data_criacao);
CREATE INDEX IF NOT EXISTS idx_sessoes_telemedicina_paciente ON sessoes_telemedicina(paciente_id, data_criacao);
CREATE INDEX IF NOT EXISTS idx_sessoes_telemedicina_status ON sessoes_telemedicina(status, data_criacao);
CREATE INDEX IF NOT EXISTS idx_sessoes_telemedicina_agendamento ON sessoes_telemedicina(agendamento_id);
CREATE INDEX IF NOT EXISTS idx_participantes_sessao_ativo ON participantes_sessao(sessao_id, ativo);
CREATE INDEX IF NOT EXISTS idx_historico_sessoes_timestamp ON historico_sessoes(sessao_id, timestamp);

-- Constraint para garantir que agendamento seja do tipo TELECONSULTA
ALTER TABLE sessoes_telemedicina 
ADD CONSTRAINT fk_sessoes_teleconsulta 
FOREIGN KEY (agendamento_id) 
REFERENCES agendamentos(id) 
WHERE tipo_agendamento = 'TELECONSULTA';

-- Trigger para atualizar status do agendamento quando sessão for criada
CREATE OR REPLACE FUNCTION update_agendamento_status_telemedicina()
RETURNS TRIGGER AS $
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
$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_update_agendamento_status_telemedicina
    AFTER UPDATE OF status ON sessoes_telemedicina
    FOR EACH ROW
    EXECUTE FUNCTION update_agendamento_status_telemedicina();

-- Inserir configurações padrão para profissionais que permitem telemedicina
INSERT INTO configuracoes_telemedicina (profissional_id, plataforma_preferida)
SELECT id, 'JITSI' 
FROM profissionais 
WHERE permite_telemedicina = true 
AND id NOT IN (SELECT profissional_id FROM configuracoes_telemedicina);
*)

// Integration with other modules
(*
-- Exemplo de como integrar com outros módulos:

// 1. Ao criar um agendamento do tipo TELECONSULTA, criar automaticamente uma sessão
let criarAgendamentoTelemedicina (agendamento: AgendamentoInput) =
    task {
        // Criar agendamento
        let! agendamentoId = AgendamentoRepository.insert agendamento
        
        // Se for teleconsulta, criar sessão
        if agendamento.TipoAgendamento = Teleconsulta then
            let sessaoInput = {
                AgendamentoId = agendamentoId
                PacienteId = agendamento.PacienteId
                ProfissionalId = agendamento.ProfissionalId
                GravacaoPermitida = true // Ou buscar da configuração do profissional
                PlataformaVideo = "JITSI" // Ou buscar da configuração do profissional
                ObservacoesIniciais = agendamento.Observacoes
            }
            let! _ = TelemedicinRepository.insert sessaoInput
            ()
        
        return agendamentoId
    }

// 2. Ao finalizar uma sessão, criar automaticamente um prontuário
let finalizarSessaoComProntuario (sessaoId: int) (dadosProntuario: ProntuarioInput) =
    task {
        // Criar prontuário
        let! prontuarioId = ProntuarioRepository.insert dadosProntuario
        
        // Finalizar sessão vinculando ao prontuário
        let! _ = TelemedicinRepository.finalizarSessao sessaoId 1 None None (Some prontuarioId)
        
        return prontuarioId
    }
*)
//*)// Telemedicina/Models.fs (Updated with missing types)
//namespace SGHSS.Core.Domain.Telemedicina

//open System

//module Models =
//    type StatusSessao = 
//        | Agendada 
//        | Iniciada 
//        | EmAndamento 
//        | Finalizada 
//        | Cancelada
//        | Falha
    
//    type TipoParticipante = 
//        | Paciente 
//        | Medico 
//        | Observador 
//        | Acompanhante
    
//    type SessaoTelemedicina = {
//        Id: int
//        AgendamentoId: int
//        PacienteId: int
//        ProfissionalId: int
//        LinkSessao: string
//        TokenAcesso: string
//        SenhaPaciente: string option
//        DataInicio: DateTime option
//        DataFim: DateTime option
//        Status: StatusSessao
//        GravacaoUrl: string option
//        GravacaoPermitida: bool
//        ObservacoesFinais: string option
//        ProntuarioId: int option
//        QualidadeConexao: string option
//        PlataformaVideo: string // ZOOM, TEAMS, JITSI, etc.
//        DataCriacao: DateTime
//        DataAtualizacao: DateTime option
//        CanceladoPor: int option
//        MotivoCancel: string option
//    }
    
//    type ParticipanteSessao = {
//        Id: int
//        SessaoId: int
//        UsuarioId: int
//        TipoParticipante: TipoParticipante
//        Nome: string
//        Email: string option
//        DataEntrada: DateTime option
//        DataSaida: DateTime option
//        TempoConectado: TimeSpan option
//        Ativo: bool
//        DispositivoUtilizado: string option
//        IpAddress: string option
//    }
    
//    type ConfiguracaoTelemedicina = {
//        Id: int
//        ProfissionalId: int
//        PlataformaPreferida: string
//        PermiteGravacao: bool
//        DuracaoMaximaSessao: TimeSpan
//        PermiteSalaEspera: bool
//        NotificacoesEmail: bool
//        NotificacoesSms: bool
//        HorarioAtendimentoInicio: TimeSpan
//        HorarioAtendimentoFim: TimeSpan
//        DiasAtendimento: string // Dias da semana separados por vírgula: "1,2,3,4,5"
//        Ativo: bool
//        DataCriacao: DateTime
//        DataAtualizacao: DateTime option
//    }
    
//    type HistoricoSessao = {
//        Id: int
//        SessaoId: int
//        Evento: string // SESSAO_CRIADA, PARTICIPANTE_ENTROU, SESSAO_INICIADA, etc.
//        Descricao: string
//        UsuarioId: int option
//        Timestamp: DateTime
//        DadosAdicionais: string option // JSON com dados extras
//    }

//    // Input Types
//    type SessaoTelemedicinInput = {
//        AgendamentoId: int
//        PacienteId: int
//        ProfissionalId: int
//        GravacaoPermitida: bool
//        PlataformaVideo: string
//        ObservacoesIniciais: string option
//    }
    
//    type ConfiguracaoTelemedicinInput = {
//        ProfissionalId: int
//        PlataformaPreferida: string
//        PermiteGravacao: bool
//        DuracaoMaximaSessao: TimeSpan
//        PermiteSalaEspera: bool
//        NotificacoesEmail: bool
//        NotificacoesSms: bool
//        HorarioAtendimentoInicio: TimeSpan
//        HorarioAtendimentoFim: TimeSpan
//        DiasAtendimento: int list // Lista de dias da semana (1-7)
//    }

//    type EntrarSessaoInput = {
//        SessaoId: int
//        UsuarioId: int
//        TipoParticipante: TipoParticipante
//        Nome: string
//        Email: string option
//        DispositivoUtilizado: string option
//    }

//    // Views detalhadas
//    type SessaoDetalhes = {
//        Id: int
//        Agendamento: {| Id: int; DataHora: DateTime; Duracao: TimeSpan |}
//        Paciente: {| Id: int; Nome: string; Email: string option; Telefone: string |}
//        Profissional: {| Id: int; Nome: string; CRM: string option; Email: string |}
//        LinkSessao: string
//        SenhaPaciente: string option
//        Status: StatusSessao
//        DataInicio: DateTime option
//        DataFim: DateTime option
//        DuracaoReal: TimeSpan option
//        GravacaoUrl: string option
//        GravacaoPermitida: bool
//        PlataformaVideo: string
//        Participantes: ParticipanteSessao list
//        QualidadeConexao: string option
//        ObservacoesFinais: string option
//        DataCriacao: DateTime
//        MotivoCancel: string option
//    }

//    type DashboardTelemedicina = {
//        SessoesHoje: int
//        SessoesAguardando: int
//        SessoesEmAndamento: int
//        SessoesFinalizadas: int
//        SessoesCanceladas: int
//        TempoMedioSessao: TimeSpan option
//        ProximasSessoes: SessaoDetalhes list
//        ProfissionaisOnline: int
//        PacientesAguardando: int
//    }

//// Telemedicina/Repository.fs
//module Repository =
//    open Npgsql.FSharp
//    open Models
//    open System

//    let private connectionString = 
//        Environment.GetEnvironmentVariable("CONNECTION_STRING") 
//        ?? "Host=localhost;Username=postgres;Password=senha;Database=sghss"

//    // Funções auxiliares para conversão de enums
//    let private parseStatusSessao (status: string) =
//        match status.ToUpper() with
//        | "AGENDADA" -> Agendada
//        | "INICIADA" -> Iniciada
//        | "EMANDAMENTO" -> EmAndamento
//        | "FINALIZADA" -> Finalizada
//        | "CANCELADA" -> Cancelada
//        | "FALHA" -> Falha
//        | _ -> Agendada

//    let private parseTipoParticipante (tipo: string) =
//        match tipo.ToUpper() with
//        | "PACIENTE" -> Paciente
//        | "MEDICO" -> Medico
//        | "OBSERVADOR" -> Observador
//        | "ACOMPANHANTE" -> Acompanhante
//        | _ -> Paciente

//    let private statusSessaoToString (status: StatusSessao) =
//        match status with
//        | Agendada -> "AGENDADA"
//        | Iniciada -> "INICIADA"
//        | EmAndamento -> "EMANDAMENTO"
//        | Finalizada -> "FINALIZADA"
//        | Cancelada -> "CANCELADA"
//        | Falha -> "FALHA"

//    let private tipoParticipanteToString (tipo: TipoParticipante) =
//        match tipo with
//        | Paciente -> "PACIENTE"
//        | Medico -> "MEDICO"
//        | Observador -> "OBSERVADOR"
//        | Acompanhante -> "ACOMPANHANTE"

//    // Funções auxiliares para gerar links e tokens
//    let private gerarTokenSessao () =
//        let guid = System.Guid.NewGuid().ToString("N")
//        guid.Substring(0, 16).ToUpper()

//    let private gerarSenhaPaciente () =
//        let random = System.Random()
//        let numeros = [|0..9|]
//        Array.init 6 (fun _ -> numeros.[random.Next(numeros.Length)]) 
//        |> Array.map string 
//        |> String.concat ""

//    let private gerarLinkSessao (sessaoId: int) (token: string) =
//        $"https://telemedicina.sghss.com/sessao/{sessaoId}?token={token}"

//    // Repository functions for SessaoTelemedicina
//    let getAll (profissionalId: int option) (status: StatusSessao option) (data: DateTime option) =
//        task {
//            let mutable query = """
//                SELECT id, agendamento_id, paciente_id, profissional_id, link_sessao, token_acesso,
//                       senha_paciente, data_inicio, data_fim, status, gravacao_url, gravacao_permitida,
//                       observacoes_finais, prontuario_id, qualidade_conexao, plataforma_video,
//                       data_criacao, data_atualizacao, cancelado_por, motivo_cancel
//                FROM sessoes_telemedicina 
//                WHERE 1=1
//            """
//            let mutable parameters = []

//            match profissionalId with
//            | Some pid ->
//                query <- query + " AND profissional_id = @profissional_id"
//                parameters <- ("profissional_id", Sql.int pid) :: parameters
//            | None -> ()

//            match status with
//            | Some s ->
//                query <- query + " AND status = @status"
//                parameters <- ("status", Sql.string (statusSessaoToString s)) :: parameters
//            | None -> ()

//            match data with
//            | Some d ->
//                query <- query + " AND DATE(data_criacao) = DATE(@data)"
//                parameters <- ("data", Sql.timestamp d) :: parameters
//            | None -> ()

//            query <- query + " ORDER BY data_criacao DESC"

//            return!
//                connectionString
//                |> Sql.connect
//                |> Sql.query query
//                |> Sql.parameters parameters
//                |> Sql.executeAsync (fun read -> {
//                    Id = read.int "id"
//                    AgendamentoId = read.int "agendamento_id"
//                    PacienteId = read.int "paciente_id"
//                    ProfissionalId = read.int "profissional_id"
//                    LinkSessao = read.string "link_sessao"
//                    TokenAcesso = read.string "token_acesso"
//                    SenhaPaciente = read.stringOrNone "senha_paciente"
//                    DataInicio = read.dateTimeOrNone "data_inicio"
//                    DataFim = read.dateTimeOrNone "data_fim"
//                    Status = parseStatusSessao (read.string "status")
//                    GravacaoUrl = read.stringOrNone "gravacao_url"
//                    GravacaoPermitida = read.bool "gravacao_permitida"
//                    ObservacoesFinais = read.stringOrNone "observacoes_finais"
//                    ProntuarioId = read.intOrNone "prontuario_id"
//                    QualidadeConexao = read.stringOrNone "qualidade_conexao"
//                    PlataformaVideo = read.string "plataforma_video"
//                    DataCriacao = read.dateTime "data_criacao"
//                    DataAtualizacao = read.dateTimeOrNone "data_atualizacao"
//                    CanceladoPor = read.intOrNone "cancelado_por"
//                    MotivoCancel = read.stringOrNone "motivo_cancel"
//                })
//        }

//    let getById (id: int) =
//        task {
//            return!
//                connectionString
//                |> Sql.connect
//                |> Sql.query """
//                    SELECT id, agendamento_id, paciente_id, profissional_id, link_sessao, token_acesso,
//                           senha_paciente, data_inicio, data_fim, status, gravacao_url, gravacao_permitida,
//                           observacoes_finais, prontuario_id, qualidade_conexao, plataforma_video,
//                           data_criacao, data_atualizacao, cancelado_por, motivo_cancel
//                    FROM sessoes_telemedicina 
//                    WHERE id = @id
//                """
//                |> Sql.parameters ["id", Sql.int id]
//                |> Sql.executeRowAsync (fun read -> {
//                    Id = read.int "id"
//                    AgendamentoId = read.int "agendamento_id"
//                    PacienteId = read.int "paciente_id"
//                    ProfissionalId = read.int "profissional_id"
//                    LinkSessao = read.string "link_sessao"
//                    TokenAcesso = read.string "token_acesso"
//                    SenhaPaciente = read.stringOrNone "senha_paciente"
//                    DataInicio = read.dateTimeOrNone "data_inicio"
//                    DataFim = read.dateTimeOrNone "data_fim"
//                    Status = parseStatusSessao (read.string "status")
//                    GravacaoUrl = read.stringOrNone "gravacao_url"
//                    GravacaoPermitida = read.bool "gravacao_permitida"
//                    ObservacoesFinais = read.stringOrNone "observacoes_finais"
//                    ProntuarioId = read.intOrNone "prontuario_id"
//                    QualidadeConexao = read.stringOrNone "qualidade_conexao"
//                    PlataformaVideo = read.string "plataforma_video"
//                    DataCriacao = read.dateTime "data_criacao"
//                    DataAtualizacao = read.dateTimeOrNone "data_atualizacao"
//                    CanceladoPor = read.intOrNone "cancelado_por"
//                    MotivoCancel = read.stringOrNone "motivo_cancel"
//                })
//        }

//    let getSessaoDetalhes (id: int) =
//        task {
//            return!
//                connectionString
//                |> Sql.connect
//                |> Sql.query """
//                    SELECT 
//                        s.id, s.link_sessao, s.senha_paciente, s.status, s.data_inicio, s.data_fim,
//                        s.gravacao_url, s.gravacao_permitida, s.plataforma_video, s.qualidade_conexao,
//                        s.observacoes_finais, s.data_criacao, s.motivo_cancel,
//                        a.id as agendamento_id, a.data_hora, a.duracao,
//                        p.id as paciente_id, p.nome as paciente_nome, p.email as paciente_email, p.telefone as paciente_telefone,
//                        pr.id as profissional_id, pr.nome as profissional_nome, pr.crm as profissional_crm, pr.email as profissional_email
//                    FROM sessoes_telemedicina s
//                    INNER JOIN agendamentos a ON s.agendamento_id = a.id
//                    INNER JOIN pacientes p ON s.paciente_id = p.id
//                    INNER JOIN profissionais pr ON s.profissional_id = pr.id
//                    WHERE s.id = @id
//                """
//                |> Sql.parameters ["id", Sql.int id]
//                |> Sql.executeRowAsync (fun read -> 
//                    let dataInicio = read.dateTimeOrNone "data_inicio"
//                    let dataFim = read.dateTimeOrNone "data_fim"
//                    let duracaoReal = 
//                        match dataInicio, dataFim with
//                        | Some inicio, Some fim -> Some (fim - inicio)
//                        | _ -> None

//                    {
//                        Id = read.int "id"
//                        Agendamento = {|
//                            Id = read.int "agendamento_id"
//                            DataHora = read.dateTime "data_hora"
//                            Duracao = read.interval "duracao"
//                        |}
//                        Paciente = {|
//                            Id = read.int "paciente_id"
//                            Nome = read.string "paciente_nome"
//                            Email = read.stringOrNone "paciente_email"
//                            Telefone = read.string "paciente_telefone"
//                        |}
//                        Profissional = {|
//                            Id = read.int "profissional_id"
//                            Nome = read.string "profissional_nome"
//                            CRM = read.stringOrNone "profissional_crm"
//                            Email = read.string "profissional_email"
//                        |}
//                        LinkSessao = read.string "link_sessao"
//                        SenhaPaciente = read.stringOrNone "senha_paciente"
//                        Status = parseStatusSessao (read.string "status")
//                        DataInicio = dataInicio
//                        DataFim = dataFim
//                        DuracaoReal = duracaoReal
//                        GravacaoUrl = read.stringOrNone "gravacao_url"
//                        GravacaoPermitida = read.bool "gravacao_permitida"
//                        PlataformaVideo = read.string "plataforma_video"
//                        Participantes = [] // Será carregado separadamente
//                        QualidadeConexao = read.stringOrNone "qualidade_conexao"
//                        ObservacoesFinais = read.stringOrNone "observacoes_finais"
//                        DataCriacao = read.dateTime "data_criacao"
//                        MotivoCancel = read.stringOrNone "motivo_cancel"
//                    })
//        }

//    let insert (input: SessaoTelemedicinInput) =
//        task {
//            let token = gerarTokenSessao()
//            let senhaPaciente = gerarSenhaPaciente()
            
//            let! sessaoId =
//                connectionString
//                |> Sql.connect
//                |> Sql.query """
//                    INSERT INTO sessoes_telemedicina 
//                    (agendamento_id, paciente_id, profissional_id, token_acesso, senha_paciente,
//                     gravacao_permitida, plataforma_video, observacoes_finais)
//                    VALUES 
//                    (@agendamento_id, @paciente_id, @profissional_id, @token_acesso, @senha_paciente,
//                     @gravacao_permitida, @plataforma_video, @observacoes_finais)
//                    RETURNING id
//                """
//                |> Sql.parameters [
//                    "agendamento_id", Sql.int input.AgendamentoId
//                    "paciente_id", Sql.int input.PacienteId
//                    "profissional_id", Sql.int input.ProfissionalId
//                    "token_acesso", Sql.string token
//                    "senha_paciente", Sql.string senhaPaciente
//                    "gravacao_permitida", Sql.bool input.GravacaoPermitida
//                    "plataforma_video", Sql.string input.PlataformaVideo
//                    "observacoes_finais", Sql.stringOrNone input.ObservacoesIniciais
//                ]
//                |> Sql.executeRowAsync (fun read -> read.int "id")

//            // Atualizar com o link da sessão
//            let linkSessao = gerarLinkSessao sessaoId token
//            let! _ =
//                connectionString
//                |> Sql.connect
//                |> Sql.query """
//                    UPDATE sessoes_telemedicina 
//                    SET link_sessao = @link_sessao
//                    WHERE id = @id
//                """
//                |> Sql.parameters [
//                    "id", Sql.int sessaoId
//                    "link_sessao", Sql.string linkSessao
//                ]
//                |> Sql.executeNonQueryAsync

//            // Registrar no histórico
//            let! _ = registrarHistorico sessaoId "SESSAO_CRIADA" "Sessão de telemedicina criada" None None

//            return sessaoId
//        }

//    and registrarHistorico (sessaoId: int) (evento: string) (descricao: string) (usuarioId: int option) (dadosAdicionais: string option) =
//        task {
//            return!
//                connectionString
//                |> Sql.connect
//                |> Sql.query """
//                    INSERT INTO historico_sessoes 
//                    (sessao_id, evento, descricao, usuario_id, dados_adicionais)
//                    VALUES (@sessao_id, @evento, @descricao, @usuario_id, @dados_adicionais)
//                """
//                |> Sql.parameters [
//                    "sessao_id", Sql.int sessaoId
//                    "evento", Sql.string evento
//                    "descricao", Sql.string descricao
//                    "usuario_id", Sql.intOrNone usuarioId
//                    "dados_adicionais", Sql.stringOrNone dadosAdicionais
//                ]
//                |> Sql.executeNonQueryAsync
//        }

//    let updateStatus (id: int) (novoStatus: StatusSessao) (usuarioId: int option) (observacoes: string option) =
//        task {
//            let mutable updateFields = "status = @status, data_atualizacao = CURRENT_TIMESTAMP"
//            let mutable parameters = [
//                "id", Sql.int id
//                "status", Sql.string (statusSessaoToString novoStatus)
//            ]

//            match novoStatus with
//            | Iniciada ->
//                updateFields <- updateFields + ", data_inicio = CURRENT_TIMESTAMP"
//            | Finalizada ->
//                updateFields <- updateFields + ", data_fim = CURRENT_TIMESTAMP"
//            | Cancelada ->
//                updateFields <- updateFields + ", cancelado_por = @cancelado_por, motivo_cancel = @motivo_cancel"
//                parameters <- ("cancelado_por", Sql.intOrNone usuarioId) :: ("motivo_cancel", Sql.stringOrNone observacoes) :: parameters
//            | _ -> ()

//            let query = $"UPDATE sessoes_telemedicina SET {updateFields} WHERE id = @id"

//            let! rowsAffected =
//                connectionString
//                |> Sql.connect
//                |> Sql.query query
//                |> Sql.parameters parameters
//                |> Sql.executeNonQueryAsync
            
//            if rowsAffected > 0 then
//                let eventoDescricao = $"Status alterado para {statusSessaoToString novoStatus}"
//                let! _ = registrarHistorico id "STATUS_ALTERADO" eventoDescricao usuarioId observacoes
//                ()

//            return rowsAffected > 0
//        }

//    let adicionarParticipante (input: EntrarSessaoInput) =
//        task {
//            // Verificar se a sessão existe e está ativa
//            let! sessaoExiste =
//                connectionString
//                |> Sql.connect
//                |> Sql.query """
//                    SELECT COUNT(*) FROM sessoes_telemedicina 
//                    WHERE id = @id AND status IN ('AGENDADA', 'INICIADA', 'EMANDAMENTO')
//                """
//                |> Sql.parameters ["id", Sql.int input.SessaoId]
//                |> Sql.executeRowAsync (fun read -> read.int "count" > 0)

//            if not sessaoExiste then
//                return Error "Sessão não encontrada ou não está disponível para entrada"
//            else
//                // Verificar se participante já está na sessão
//                let! jaParticipando =
//                    connectionString
//                    |> Sql.connect
//                    |> Sql.query """
//                        SELECT COUNT(*) FROM participantes_sessao 
//                        WHERE sessao_id = @sessao_id AND usuario_id = @usuario_id AND ativo = true
//                    """
//                    |> Sql.parameters [
//                        "sessao_id", Sql.int input.SessaoId
//                        "usuario_id", Sql.int input.UsuarioId
//                    ]
//                    |> Sql.executeRowAsync (fun read -> read.int "count" > 0)

//                if jaParticipando then
//                    return Error "Usuário já está participando da sessão"
//                else
//                    let! participanteId =
//                        connectionString
//                        |> Sql.connect
//                        |> Sql.query """
//                            INSERT INTO participantes_sessao 
//                            (sessao_id, usuario_id, tipo_participante, nome, email, data_entrada, 
//                             dispositivo_utilizado, ativo)
//                            VALUES 
//                            (@sessao_id, @usuario_id, @tipo_participante, @nome, @email, CURRENT_TIMESTAMP,
//                             @dispositivo_utilizado, true)
//                            RETURNING id
//                        """
//                        |> Sql.parameters [
//                            "sessao_id", Sql.int input.SessaoId
//                            "usuario_id", Sql.int input.UsuarioId
//                            "tipo_participante", Sql.string (tipoParticipanteToString input.TipoParticipante)
//                            "nome", Sql.string input.Nome
//                            "email", Sql.stringOrNone input.Email
//                            "dispositivo_utilizado", Sql.stringOrNone input.DispositivoUtilizado
//                        ]
//                        |> Sql.executeRowAsync (fun read -> read.int "id")

//                    // Registrar entrada no histórico
//                    let descricao = $"Participante {input.Nome} ({tipoParticipanteToString input.TipoParticipante}) entrou na sessão"
//                    let! _ = registrarHistorico input.SessaoId "PARTICIPANTE_ENTROU" descricao (Some input.UsuarioId) None

//                    // Se for o primeiro participante médico, iniciar sessão
//                    if input.TipoParticipante = Medico then
//                        let! _ = updateStatus input.SessaoId Iniciada (Some input.UsuarioId) None
//                        ()

//                    return Ok participanteId
//        }

//    let removerParticipante (sessaoId: int) (usuarioId: int) =
//        task {
//            let! rowsAffected =
//                connectionString
//                |> Sql.connect
//                |> Sql.query """
//                    UPDATE participantes_sessao 
//                    SET ativo = false,
//                        data_saida = CURRENT_TIMESTAMP,
//                        tempo_conectado = CURRENT_TIMESTAMP - data_entrada
//                    WHERE sessao_id = @sessao_id AND usuario_id = @usuario_id AND ativo = true
//                """
//                |> Sql.parameters [
//                    "sessao_id", Sql.int sessaoId
//                    "usuario_id", Sql.int usuarioId
//                ]
//                |> Sql.executeNonQueryAsync

//            if rowsAffected > 0 then
//                let descricao = "Participante saiu da sessão"
//                let! _ = registrarHistorico sessaoId "PARTICIPANTE_SAIU" descricao (Some usuarioId) None
//                ()

//            return rowsAffected > 0
//        }

//    let finalizarSessao (id: int) (usuarioId: int) (observacoes: string option) (qualidadeConexao: string option) (prontuarioId: int option) =
//        task {
//            // Remover todos os participantes ativos
//            let! _ =
//                connectionString
//                |> Sql.connect
//                |> Sql.query """
//                    UPDATE participantes_sessao 
//                    SET ativo = false,
//                        data_saida = CURRENT_TIMESTAMP,
//                        tempo_conectado = CURRENT_TIMESTAMP - data_entrada
//                    WHERE sessao_id = @sessao_id AND ativo = true
//                """
//                |> Sql.parameters ["sessao_id", Sql.int id]
//                |> Sql.executeNonQueryAsync

//            // Finalizar sessão
//            let! rowsAffected =
//                connectionString
//                |> Sql.connect
//                |> Sql.query """
//                    UPDATE sessoes_telemedicina 
//                    SET status = 'FINALIZADA',
//                        data_fim = CURRENT_TIMESTAMP,
//                        data_atualizacao = CURRENT_TIMESTAMP,
//                        observacoes_finais = @observacoes_finais,
//                        qualidade_conexao = @qualidade_conexao,
//                        prontuario_id = @prontuario_id
//                    WHERE id = @id
//                """
//                |> Sql.parameters [
//                    "id", Sql.int id
//                    "observacoes_finais", Sql.stringOrNone observacoes
//                    "qualidade_conexao", Sql.stringOrNone qualidadeConexao
//                    "prontuario_id", Sql.intOrNone prontuarioId
//                ]
//                |> Sql.executeNonQueryAsync

//            if rowsAffected > 0 then
//                let descricao = "Sessão finalizada"
//                let! _ = registrarHistorico id "SESSAO_FINALIZADA" descricao (Some usuarioId) observacoes
//                ()

//            return rowsAffected > 0
//        }

//    // Configurações de Telemedicina
//    let getConfiguracaoProfissional (profissionalId: int) =
//        task {
//            return!
//                connectionString
//                |> Sql.connect
//                |> Sql.query """
//                    SELECT id, profissional_id, plataforma_preferida, permite_gravacao, duracao_maxima_sessao,
//                           permite_sala_espera, notificacoes_email, notificacoes_sms, 
//                           horario_atendimento_inicio, horario_atendimento_fim, dias_atendimento,
//                           ativo, data_criacao, data_atualizacao
//                    FROM configuracoes_telemedicina 
//                    WHERE profissional_id = @profissional_id
//                """
//                |> Sql.parameters ["profissional_id", Sql.int profissionalId]
//                |> Sql.executeRowAsync (fun read -> 
//                    let diasStr = read.string "dias_atendimento"
                    
//                    {
//                        Id = read.int "id"
//                        ProfissionalId = read.int "profissional_id"
//                        PlataformaPreferida = read.string "plataforma_preferida"
//                        PermiteGravacao = read.bool "permite_gravacao"
//                        DuracaoMaximaSessao = read.interval "duracao_maxima_sessao"
//                        PermiteSalaEspera = read.bool "permite_sala_espera"
//                        NotificacoesEmail = read.bool "notificacoes_email"
//                        NotificacoesSms = read.bool "notificacoes_sms"
//                        HorarioAtendimentoInicio = read.interval "horario_atendimento_inicio"
//                        HorarioAtendimentoFim = read.interval "horario_atendimento_fim"
//                        DiasAtendimento = diasStr
//                        Ativo = read.bool "ativo"
//                        DataCriacao = read.dateTime "data_criacao"
//                        DataAtualizacao = read.dateTimeOrNone "data_atualizacao"
//                    })
//        }

//    //let upsertConfiguracaoProfissional (input: Configu
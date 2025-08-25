module Telemedicina

open System

module Models =
    type StatusSessao = 
        | Agendada 
        | Iniciada 
        | EmAndamento 
        | Finalizada 
        | Cancelada
    
    type SessaoTelemedicina = {
        Id: int
        AgendamentoId: int
        PacienteId: int
        ProfissionalId: int
        LinkSessao: string
        TokenAcesso: string
        DataInicio: DateTime option
        DataFim: DateTime option
        Status: StatusSessao
        GravacaoUrl: string option
        ObservacoesFinais: string option
        ProntuarioId: int option
        DataCriacao: DateTime
        DataAtualizacao: DateTime option
    }
    
    type ParticipanteSessao = {
        Id: int
        SessaoId: int
        UsuarioId: int
        TipoParticipante: string // "PACIENTE", "MEDICO", "OBSERVADOR"
        DataEntrada: DateTime option
        DataSaida: DateTime option
        Ativo: bool
    }
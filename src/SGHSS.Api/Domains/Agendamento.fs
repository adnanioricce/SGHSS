namespace Domains.Profissional

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
    }

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

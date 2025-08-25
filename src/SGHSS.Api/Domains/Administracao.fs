module Administracao

open System

module Models =
    type Unidade = {
        Id: int
        Nome: string
        CNPJ: string
        TipoUnidade: string // Hospital, Clínica, Laboratório, HomeCare
        Endereco: string
        Telefone: string
        Email: string
        Responsavel: string
        Ativa: bool
        DataCadastro: DateTime
    }
    
    type Leito = {
        Id: int
        UnidadeId: int
        Numero: string
        Setor: string
        TipoLeito: string // UTI, Semi-UTI, Enfermaria, Particular
        Status: string // LIVRE, OCUPADO, LIMPEZA, MANUTENCAO
        PacienteId: int option
        DataOcupacao: DateTime option
        DataLiberacao: DateTime option
        ObservacoesStatus: string option
    }
    
    type Internacao = {
        Id: int
        PacienteId: int
        LeitoId: int
        MedicoResponsavelId: int
        DataInternacao: DateTime
        DataAltaPrevista: DateTime option
        DataAlta: DateTime option
        TipoInternacao: string
        MotivoInternacao: string
        Diagnostico: string option
        Status: string // INTERNADO, ALTA_MEDICA, ALTA_HOSPITALAR, TRANSFERIDO, OBITO
        ObservacoesAlta: string option
    }
    
    type RelatorioFinanceiro = {
        Id: int
        UnidadeId: int
        Periodo: string
        TotalReceita: decimal
        TotalDespesa: decimal
        LucroLiquido: decimal
        ConsultasRealizadas: int
        ExamesRealizados: int
        InternacoesTotal: int
        DataGeracao: DateTime
    }
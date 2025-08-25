namespace Domains.Profissional

open System

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
    }
    
    and Procedimento = {
        Id: int
        ProntuarioId: int
        Nome: string
        Descricao: string
        DataRealizacao: DateTime
        ProfissionalId: int
        Observacoes: string option
    }
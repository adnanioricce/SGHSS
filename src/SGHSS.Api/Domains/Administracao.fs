namespace Domains.Administracao

open System
open Infrastructure.Database

module Models =
    // Unidades
    type TipoUnidade = 
        | Hospital 
        | Clinica 
        | Laboratorio 
        | HomeCare
        | UPA
        | PostoSaude
    
    type Unidade = {
        Id: int
        Nome: string
        CNPJ: string
        TipoUnidade: TipoUnidade
        Endereco: string
        Telefone: string
        Email: string
        Responsavel: string
        CapacidadeLeitos: int
        Ativa: bool
        DataCadastro: DateTime
        DataAtualizacao: DateTime option
    }
    
    // Leitos
    type TipoLeito = 
        | UTI 
        | SemiUTI 
        | Enfermaria 
        | Particular
        | Isolamento
        | Pediatria
        | Maternidade
    
    type StatusLeito = 
        | Livre 
        | Ocupado 
        | Limpeza 
        | Manutencao
        | Bloqueado
    
    type Leito = {
        Id: int
        UnidadeId: int
        Numero: string
        Setor: string
        TipoLeito: TipoLeito
        Status: StatusLeito
        PacienteId: int option
        DataOcupacao: DateTime option
        DataLiberacao: DateTime option
        ObservacoesStatus: string option
        ValorDiaria: decimal option
        Equipamentos: string list
        CapacidadeAcompanhantes: int
        DataCadastro: DateTime
        DataAtualizacao: DateTime option
    }
    
    // Internações
    type TipoInternacao = 
        | Eletiva 
        | Emergencia 
        | Urgencia
        | Transferencia
        | Cirurgica
    
    type StatusInternacao = 
        | Internado 
        | AltaMedica 
        | AltaHospitalar 
        | Transferido 
        | Obito
        | AltaAdministrativa
    
    type Internacao = {
        Id: int
        PacienteId: int
        LeitoId: int
        MedicoResponsavelId: int
        DataInternacao: DateTime
        DataAltaPrevista: DateTime option
        DataAlta: DateTime option
        TipoInternacao: TipoInternacao
        MotivoInternacao: string
        Diagnostico: string option
        CID10Principal: string option
        CID10Secundarios: string list
        Status: StatusInternacao
        ObservacoesAlta: string option
        ValorTotal: decimal option
        PlanoSaudeCobertura: bool
        NumeroGuia: string option
        UnidadeId: int
        DataCadastro: DateTime
        DataAtualizacao: DateTime option
    }
    
    // Relatórios Financeiros
    type TipoReceita = 
        | Consultas 
        | Exames 
        | Internacoes 
        | Procedimentos
        | Cirurgias
        | Telemedicina
    
    type TipoDespesa = 
        | Medicamentos 
        | Equipamentos 
        | Pessoal 
        | Infraestrutura
        | Suprimentos
        | Terceirizados
    
    type ItemFinanceiro = {
        Id: int
        Descricao: string
        Tipo: string // RECEITA ou DESPESA
        Categoria: string
        Valor: decimal
        Data: DateTime
        UnidadeId: int
        PacienteId: int option
        ProfissionalId: int option
        Observacoes: string option
    }
    
    type RelatorioFinanceiro = {
        Id: int
        UnidadeId: int
        Periodo: string // "2024-01" para mensal, "2024-Q1" para trimestral
        TipoRelatorio: string // MENSAL, TRIMESTRAL, ANUAL
        TotalReceita: decimal
        TotalDespesa: decimal
        LucroLiquido: decimal
        ConsultasRealizadas: int
        ExamesRealizados: int
        InternacoesTotal: int
        SessoesTelemedicina: int
        TicketMedioConsulta: decimal
        TaxaOcupacaoLeitos: decimal
        DataGeracao: DateTime
        GeradoPor: int
        Detalhes: ItemFinanceiro list
    }

    // Suprimentos
    type CategoriaSuprimento = 
        | Medicamento 
        | MaterialMedico 
        | Equipamento
        | Limpeza
        | Alimentacao
        | Administrativo
    
    type StatusSuprimento = 
        | EmEstoque 
        | EstoqueBaixo 
        | EstoqueZerado
        | Vencido
        | Descartado
    
    type Suprimento = {
        Id: int
        Nome: string
        Categoria: CategoriaSuprimento
        Codigo: string option // Código interno ou de barras
        Descricao: string
        UnidadeMedida: string // UN, KG, ML, etc.
        QuantidadeEstoque: decimal
        QuantidadeMinima: decimal
        QuantidadeMaxima: decimal
        ValorUnitario: decimal
        Fornecedor: string option
        DataVencimento: DateTime option
        Status: StatusSuprimento
        UnidadeId: int
        LocalizacaoEstoque: string option
        DataCadastro: DateTime
        DataAtualizacao: DateTime option
    }

    type MovimentacaoEstoque = {
        Id: int
        SuprimentoId: int
        TipoMovimento: string // ENTRADA, SAIDA, AJUSTE, DESCARTE
        Quantidade: decimal
        ValorUnitario: decimal option
        Motivo: string
        ResponsavelId: int
        DataMovimento: DateTime
        Observacoes: string option
        NotaFiscal: string option
    }

    // Input Types
    type UnidadeInput = {
        Nome: string
        CNPJ: string
        TipoUnidade: TipoUnidade
        Endereco: string
        Telefone: string
        Email: string
        Responsavel: string
        CapacidadeLeitos: int
    }

    type LeitoInput = {
        UnidadeId: int
        Numero: string
        Setor: string
        TipoLeito: TipoLeito
        ValorDiaria: decimal option
        Equipamentos: string list
        CapacidadeAcompanhantes: int
    }

    type InternacaoInput = {
        PacienteId: int
        LeitoId: int
        MedicoResponsavelId: int
        DataInternacao: DateTime
        DataAltaPrevista: DateTime option
        TipoInternacao: TipoInternacao
        MotivoInternacao: string
        Diagnostico: string option
        CID10Principal: string option
        CID10Secundarios: string list
        PlanoSaudeCobertura: bool
        NumeroGuia: string option
        UnidadeId: int
    }

    type SuprimentoInput = {
        Nome: string
        Categoria: CategoriaSuprimento
        Codigo: string option
        Descricao: string
        UnidadeMedida: string
        QuantidadeEstoque: decimal
        QuantidadeMinima: decimal
        QuantidadeMaxima: decimal
        ValorUnitario: decimal
        Fornecedor: string option
        DataVencimento: DateTime option
        UnidadeId: int
        LocalizacaoEstoque: string option
    }

    // Views detalhadas
    type LeitoDetalhes = {
        Id: int
        Numero: string
        Setor: string
        TipoLeito: TipoLeito
        Status: StatusLeito
        Unidade: {| Id: int; Nome: string |}
        Paciente: {| Id: int; Nome: string; CPF: string |} option
        DataOcupacao: DateTime option
        DiasOcupado: int option
        ValorDiaria: decimal option
        Equipamentos: string list
        ObservacoesStatus: string option
    }

    type InternacaoDetalhes = {
        Id: int
        Paciente: {| Id: int; Nome: string; CPF: string; DataNascimento: DateTime |}
        Leito: {| Id: int; Numero: string; Setor: string; TipoLeito: string |}
        MedicoResponsavel: {| Id: int; Nome: string; CRM: string option |}
        Unidade: {| Id: int; Nome: string |}
        DataInternacao: DateTime
        DataAltaPrevista: DateTime option
        DataAlta: DateTime option
        DiasInternado: int
        TipoInternacao: TipoInternacao
        Status: StatusInternacao
        MotivoInternacao: string
        Diagnostico: string option
        ValorTotal: decimal option
        PlanoSaudeCobertura: bool
        NumeroGuia: string option
    }

    type DashboardAdministracao = {
        UnidadeId: int option
        Data: DateTime
        // Leitos
        TotalLeitos: int
        LeitosOcupados: int
        LeitosLivres: int
        LeitosManutencao: int
        TaxaOcupacao: decimal
        // Internações
        InternacoesHoje: int
        AltasHoje: int
        InternacoesAtivas: int
        TempoMedioInternacao: decimal
        // Financeiro
        ReceitaHoje: decimal
        DespesaHoje: decimal
        ReceitaMes: decimal
        DespesaMes: decimal
        // Suprimentos
        ItensEstoqueBaixo: int
        ItensVencendo: int
        ValorEstoque: decimal
        // Próximas ações
        AltasPrevistas: InternacaoDetalhes list
        ManutencoesProgramadas: LeitoDetalhes list
    }

// Administracao/Repository.fs

open Models

module Repository =
    open Npgsql.FSharp
    open Models
    open System
   

    // Funções auxiliares para conversão de enums
    let private parseTipoUnidade (tipo: string) =
        match tipo.ToUpper() with
        | "HOSPITAL" -> Hospital
        | "CLINICA" -> Clinica
        | "LABORATORIO" -> Laboratorio
        | "HOMECARE" -> HomeCare
        | "UPA" -> UPA
        | "POSTOSAUDE" -> PostoSaude
        | _ -> Clinica

    let private parseTipoLeito (tipo: string) =
        match tipo.ToUpper() with
        | "UTI" -> UTI
        | "SEMIUTI" -> SemiUTI
        | "ENFERMARIA" -> Enfermaria
        | "PARTICULAR" -> Particular
        | "ISOLAMENTO" -> Isolamento
        | "PEDIATRIA" -> Pediatria
        | "MATERNIDADE" -> Maternidade
        | _ -> Enfermaria

    let private parseStatusLeito (status: string) =
        match status.ToUpper() with
        | "LIVRE" -> Livre
        | "OCUPADO" -> Ocupado
        | "LIMPEZA" -> StatusLeito.Limpeza
        | "MANUTENCAO" -> Manutencao
        | "BLOQUEADO" -> Bloqueado
        | _ -> Livre

    let private parseTipoInternacao (tipo: string) =
        match tipo.ToUpper() with
        | "ELETIVA" -> Eletiva
        | "EMERGENCIA" -> Emergencia
        | "URGENCIA" -> Urgencia
        | "TRANSFERENCIA" -> Transferencia
        | "CIRURGICA" -> Cirurgica
        | _ -> Eletiva

    let private parseStatusInternacao (status: string) =
        match status.ToUpper() with
        | "INTERNADO" -> Internado
        | "ALTAMEDICA" -> AltaMedica
        | "ALTAHOSPITALAR" -> AltaHospitalar
        | "TRANSFERIDO" -> Transferido
        | "OBITO" -> Obito
        | "ALTAADMINISTRATIVA" -> AltaAdministrativa
        | _ -> Internado

    let private parseCategoriaSuprimento (categoria: string) =
        match categoria.ToUpper() with
        | "MEDICAMENTO" -> Medicamento
        | "MATERIALMEDICO" -> MaterialMedico
        | "EQUIPAMENTO" -> Equipamento
        | "LIMPEZA" -> Limpeza
        | "ALIMENTACAO" -> Alimentacao
        | "ADMINISTRATIVO" -> Administrativo
        | _ -> MaterialMedico

    let private parseStatusSuprimento (status: string) =
        match status.ToUpper() with
        | "EMESTOQUE" -> EmEstoque
        | "ESTOQUEBAIXO" -> EstoqueBaixo
        | "ESTOQUEZERADO" -> EstoqueZerado
        | "VENCIDO" -> Vencido
        | "DESCARTADO" -> Descartado
        | _ -> EmEstoque

    // Conversion helpers to string
    let private tipoUnidadeToString (tipo: TipoUnidade) =
        match tipo with
        | Hospital -> "HOSPITAL"
        | Clinica -> "CLINICA"
        | Laboratorio -> "LABORATORIO"
        | HomeCare -> "HOMECARE"
        | UPA -> "UPA"
        | PostoSaude -> "POSTOSAUDE"

    let private tipoLeitoToString (tipo: TipoLeito) =
        match tipo with
        | UTI -> "UTI"
        | SemiUTI -> "SEMIUTI"
        | Enfermaria -> "ENFERMARIA"
        | Particular -> "PARTICULAR"
        | Isolamento -> "ISOLAMENTO"
        | Pediatria -> "PEDIATRIA"
        | Maternidade -> "MATERNIDADE"

    let private statusLeitoToString (status: StatusLeito) =
        match status with
        | StatusLeito.Livre -> "LIVRE"
        | StatusLeito.Ocupado -> "OCUPADO"
        | StatusLeito.Limpeza -> "LIMPEZA"
        | StatusLeito.Manutencao -> "MANUTENCAO"
        | StatusLeito.Bloqueado -> "BLOQUEADO"

    let private tipoInternacaoToString (tipo: TipoInternacao) =
        match tipo with
        | Eletiva -> "ELETIVA"
        | Emergencia -> "EMERGENCIA"
        | Urgencia -> "URGENCIA"
        | Transferencia -> "TRANSFERENCIA"
        | Cirurgica -> "CIRURGICA"

    let private statusInternacaoToString (status: StatusInternacao) =
        match status with
        | Internado -> "INTERNADO"
        | AltaMedica -> "ALTAMEDICA"
        | AltaHospitalar -> "ALTAHOSPITALAR"
        | Transferido -> "TRANSFERIDO"
        | Obito -> "OBITO"
        | AltaAdministrativa -> "ALTAADMINISTRATIVA"

    let private categoriaSuprimentoToString (categoria: CategoriaSuprimento) =
        match categoria with
        | Medicamento -> "MEDICAMENTO"
        | MaterialMedico -> "MATERIALMEDICO"
        | Equipamento -> "EQUIPAMENTO"
        | Limpeza -> "LIMPEZA"
        | Alimentacao -> "ALIMENTACAO"
        | Administrativo -> "ADMINISTRATIVO"

    let private statusSuprimentoToString (status: StatusSuprimento) =
        match status with
        | EmEstoque -> "EMESTOQUE"
        | EstoqueBaixo -> "ESTOQUEBAIXO"
        | EstoqueZerado -> "ESTOQUEZERADO"
        | Vencido -> "VENCIDO"
        | Descartado -> "DESCARTADO"

    // Repository functions for Unidades
    let getAllUnidades (ativa: bool option) =
        task {
            let mutable query = """
                SELECT id, nome, cnpj, tipo_unidade, endereco, telefone, email, 
                       responsavel, capacidade_leitos, ativa, data_cadastro, data_atualizacao
                FROM unidades 
                WHERE 1=1
            """
            let mutable parameters = []

            match ativa with
            | Some true ->
                query <- query + " AND ativa = true"
            | Some false ->
                query <- query + " AND ativa = false"
            | None -> ()

            query <- query + " ORDER BY nome"

            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query query
                |> Sql.parameters parameters
                |> Sql.executeAsync (fun read -> {
                    Id = read.int "id"
                    Nome = read.string "nome"
                    CNPJ = read.string "cnpj"
                    TipoUnidade = parseTipoUnidade (read.string "tipo_unidade")
                    Endereco = read.string "endereco"
                    Telefone = read.string "telefone"
                    Email = read.string "email"
                    Responsavel = read.string "responsavel"
                    CapacidadeLeitos = read.int "capacidade_leitos"
                    Ativa = read.bool "ativa"
                    DataCadastro = read.dateTime "data_cadastro"
                    DataAtualizacao = read.dateTimeOrNone "data_atualizacao"
                })
        }

    let getUnidadeById (id: int) =
        task {
            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT id, nome, cnpj, tipo_unidade, endereco, telefone, email, 
                           responsavel, capacidade_leitos, ativa, data_cadastro, data_atualizacao
                    FROM unidades 
                    WHERE id = @id
                """
                |> Sql.parameters ["id", Sql.int id]
                |> Sql.executeRowAsync (fun read -> {
                    Id = read.int "id"
                    Nome = read.string "nome"
                    CNPJ = read.string "cnpj"
                    TipoUnidade = parseTipoUnidade (read.string "tipo_unidade")
                    Endereco = read.string "endereco"
                    Telefone = read.string "telefone"
                    Email = read.string "email"
                    Responsavel = read.string "responsavel"
                    CapacidadeLeitos = read.int "capacidade_leitos"
                    Ativa = read.bool "ativa"
                    DataCadastro = read.dateTime "data_cadastro"
                    DataAtualizacao = read.dateTimeOrNone "data_atualizacao"
                })
        }

    let insertUnidade (input: UnidadeInput) =
        task {
            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    INSERT INTO unidades 
                    (nome, cnpj, tipo_unidade, endereco, telefone, email, responsavel, capacidade_leitos)
                    VALUES 
                    (@nome, @cnpj, @tipo_unidade, @endereco, @telefone, @email, @responsavel, @capacidade_leitos)
                    RETURNING id
                """
                |> Sql.parameters [
                    "nome", Sql.string input.Nome
                    "cnpj", Sql.string input.CNPJ
                    "tipo_unidade", Sql.string (tipoUnidadeToString input.TipoUnidade)
                    "endereco", Sql.string input.Endereco
                    "telefone", Sql.string input.Telefone
                    "email", Sql.string input.Email
                    "responsavel", Sql.string input.Responsavel
                    "capacidade_leitos", Sql.int input.CapacidadeLeitos
                ]
                |> Sql.executeRowAsync (fun read -> read.int "id")
        }

    let updateUnidade (id: int) (input: UnidadeInput) =
        task {
            let! rowsAffected =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    UPDATE unidades 
                    SET nome = @nome,
                        cnpj = @cnpj,
                        tipo_unidade = @tipo_unidade,
                        endereco = @endereco,
                        telefone = @telefone,
                        email = @email,
                        responsavel = @responsavel,
                        capacidade_leitos = @capacidade_leitos,
                        data_atualizacao = CURRENT_TIMESTAMP
                    WHERE id = @id
                """
                |> Sql.parameters [
                    "id", Sql.int id
                    "nome", Sql.string input.Nome
                    "cnpj", Sql.string input.CNPJ
                    "tipo_unidade", Sql.string (tipoUnidadeToString input.TipoUnidade)
                    "endereco", Sql.string input.Endereco
                    "telefone", Sql.string input.Telefone
                    "email", Sql.string input.Email
                    "responsavel", Sql.string input.Responsavel
                    "capacidade_leitos", Sql.int input.CapacidadeLeitos
                ]
                |> Sql.executeNonQueryAsync
            
            return rowsAffected > 0
        }

    // Repository functions for Leitos
    let getAllLeitos (unidadeId: int option) (status: StatusLeito option) (tipoLeito: TipoLeito option) =
        task {
            let mutable query = """
                SELECT id, unidade_id, numero, setor, tipo_leito, status, paciente_id,
                       data_ocupacao, data_liberacao, observacoes_status, valor_diaria,
                       equipamentos, capacidade_acompanhantes, data_cadastro, data_atualizacao
                FROM leitos 
                WHERE 1=1
            """
            let mutable parameters = []

            match unidadeId with
            | Some uid ->
                query <- query + " AND unidade_id = @unidade_id"
                parameters <- ("unidade_id", Sql.int uid) :: parameters
            | None -> ()

            match status with
            | Some s ->
                query <- query + " AND status = @status"
                parameters <- ("status", Sql.string (statusLeitoToString s)) :: parameters
            | None -> ()

            match tipoLeito with
            | Some t ->
                query <- query + " AND tipo_leito = @tipo_leito"
                parameters <- ("tipo_leito", Sql.string (tipoLeitoToString t)) :: parameters
            | None -> ()

            query <- query + " ORDER BY unidade_id, setor, numero"

            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query query
                |> Sql.parameters parameters
                |> Sql.executeAsync (fun read -> 
                    let equipamentosStr = read.stringOrNone "equipamentos" |> Option.defaultValue ""
                    let equipamentos = 
                        if String.IsNullOrWhiteSpace(equipamentosStr) then []
                        else equipamentosStr.Split(';') |> Array.toList |> List.filter (fun e -> not (String.IsNullOrWhiteSpace(e)))
                    
                    {
                        Id = read.int "id"
                        UnidadeId = read.int "unidade_id"
                        Numero = read.string "numero"
                        Setor = read.string "setor"
                        TipoLeito = parseTipoLeito (read.string "tipo_leito")
                        Status = parseStatusLeito (read.string "status")
                        PacienteId = read.intOrNone "paciente_id"
                        DataOcupacao = read.dateTimeOrNone "data_ocupacao"
                        DataLiberacao = read.dateTimeOrNone "data_liberacao"
                        ObservacoesStatus = read.stringOrNone "observacoes_status"
                        ValorDiaria = read.decimalOrNone "valor_diaria"
                        Equipamentos = equipamentos
                        CapacidadeAcompanhantes = read.int "capacidade_acompanhantes"
                        DataCadastro = read.dateTime "data_cadastro"
                        DataAtualizacao = read.dateTimeOrNone "data_atualizacao"
                    })
        }

    let getLeitoById (id: int) =
        task {
            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT id, unidade_id, numero, setor, tipo_leito, status, paciente_id,
                           data_ocupacao, data_liberacao, observacoes_status, valor_diaria,
                           equipamentos, capacidade_acompanhantes, data_cadastro, data_atualizacao
                    FROM leitos 
                    WHERE id = @id
                """
                |> Sql.parameters ["id", Sql.int id]
                |> Sql.executeRowAsync (fun read -> 
                    let equipamentosStr = read.stringOrNone "equipamentos" |> Option.defaultValue ""
                    let equipamentos = 
                        if String.IsNullOrWhiteSpace(equipamentosStr) then []
                        else equipamentosStr.Split(';') |> Array.toList |> List.filter (fun e -> not (String.IsNullOrWhiteSpace(e)))
                    
                    {
                        Id = read.int "id"
                        UnidadeId = read.int "unidade_id"
                        Numero = read.string "numero"
                        Setor = read.string "setor"
                        TipoLeito = parseTipoLeito (read.string "tipo_leito")
                        Status = parseStatusLeito (read.string "status")
                        PacienteId = read.intOrNone "paciente_id"
                        DataOcupacao = read.dateTimeOrNone "data_ocupacao"
                        DataLiberacao = read.dateTimeOrNone "data_liberacao"
                        ObservacoesStatus = read.stringOrNone "observacoes_status"
                        ValorDiaria = read.decimalOrNone "valor_diaria"
                        Equipamentos = equipamentos
                        CapacidadeAcompanhantes = read.int "capacidade_acompanhantes"
                        DataCadastro = read.dateTime "data_cadastro"
                        DataAtualizacao = read.dateTimeOrNone "data_atualizacao"
                    })
        }

    let getLeitosDetalhes (unidadeId: int option) (status: StatusLeito option) =
        task {
            let mutable query = """
                SELECT 
                    l.id, l.numero, l.setor, l.tipo_leito, l.status, l.data_ocupacao,
                    l.valor_diaria, l.equipamentos, l.observacoes_status,
                    u.id as unidade_id, u.nome as unidade_nome,
                    p.id as paciente_id, p.nome as paciente_nome, p.cpf as paciente_cpf
                FROM leitos l
                INNER JOIN unidades u ON l.unidade_id = u.id
                LEFT JOIN pacientes p ON l.paciente_id = p.id
                WHERE 1=1
            """
            let mutable parameters = []

            match unidadeId with
            | Some uid ->
                query <- query + " AND l.unidade_id = @unidade_id"
                parameters <- ("unidade_id", Sql.int uid) :: parameters
            | None -> ()

            match status with
            | Some s ->
                query <- query + " AND l.status = @status"
                parameters <- ("status", Sql.string (statusLeitoToString s)) :: parameters
            | None -> ()

            query <- query + " ORDER BY u.nome, l.setor, l.numero"

            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query query
                |> Sql.parameters parameters
                |> Sql.executeAsync (fun read -> 
                    let equipamentosStr = read.stringOrNone "equipamentos" |> Option.defaultValue ""
                    let equipamentos = 
                        if String.IsNullOrWhiteSpace(equipamentosStr) then []
                        else equipamentosStr.Split(';') |> Array.toList |> List.filter (fun e -> not (String.IsNullOrWhiteSpace(e)))

                    let diasOcupado = 
                        match read.dateTimeOrNone "data_ocupacao" with
                        | Some dataOcupacao -> Some (int (DateTime.Now - dataOcupacao).TotalDays)
                        | None -> None

                    let paciente = 
                        match read.intOrNone "paciente_id" with
                        | Some pid -> Some {| Id = pid; Nome = read.string "paciente_nome"; CPF = read.string "paciente_cpf" |}
                        | None -> None

                    {
                        Id = read.int "id"
                        Numero = read.string "numero"
                        Setor = read.string "setor"
                        TipoLeito = parseTipoLeito (read.string "tipo_leito")
                        Status = parseStatusLeito (read.string "status")
                        Unidade = {| Id = read.int "unidade_id"; Nome = read.string "unidade_nome" |}
                        Paciente = paciente
                        DataOcupacao = read.dateTimeOrNone "data_ocupacao"
                        DiasOcupado = diasOcupado
                        ValorDiaria = read.decimalOrNone "valor_diaria"
                        Equipamentos = equipamentos
                        ObservacoesStatus = read.stringOrNone "observacoes_status"
                    })
        }

    let insertLeito (input: LeitoInput) =
        task {
            let equipamentosStr = String.Join(";", input.Equipamentos)
            
            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    INSERT INTO leitos 
                    (unidade_id, numero, setor, tipo_leito, valor_diaria, equipamentos, capacidade_acompanhantes)
                    VALUES 
                    (@unidade_id, @numero, @setor, @tipo_leito, @valor_diaria, @equipamentos, @capacidade_acompanhantes)
                    RETURNING id
                """
                |> Sql.parameters [
                    "unidade_id", Sql.int input.UnidadeId
                    "numero", Sql.string input.Numero
                    "setor", Sql.string input.Setor
                    "tipo_leito", Sql.string (tipoLeitoToString input.TipoLeito)
                    "valor_diaria", Sql.decimalOrNone input.ValorDiaria
                    "equipamentos", Sql.string equipamentosStr
                    "capacidade_acompanhantes", Sql.int input.CapacidadeAcompanhantes
                ]
                |> Sql.executeRowAsync (fun read -> read.int "id")
        }

    let updateStatusLeito (id: int) (novoStatus: StatusLeito) (observacoes: string option) =
        task {
            let mutable updateFields = "status = @status, data_atualizacao = CURRENT_TIMESTAMP"
            let mutable parameters = [
                "id", Sql.int id
                "status", Sql.string (statusLeitoToString novoStatus)
                "observacoes", Sql.stringOrNone observacoes
            ]

            match novoStatus with
            | Ocupado ->
                updateFields <- updateFields + ", data_ocupacao = CURRENT_TIMESTAMP, data_liberacao = NULL"
            | Livre ->
                updateFields <- updateFields + ", data_liberacao = CURRENT_TIMESTAMP, paciente_id = NULL"
            | StatusLeito.Limpeza | Manutencao | Bloqueado ->
                updateFields <- updateFields + ", paciente_id = NULL"
            | _ -> ()

            updateFields <- updateFields + ", observacoes_status = @observacoes"

            let query = $"UPDATE leitos SET {updateFields} WHERE id = @id"

            let! rowsAffected =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query query
                |> Sql.parameters parameters
                |> Sql.executeNonQueryAsync
            
            return rowsAffected > 0
        }

    let ocuparLeito (leitoId: int) (pacienteId: int) =
        task {
            let! rowsAffected =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    UPDATE leitos 
                    SET status = 'OCUPADO',
                        paciente_id = @paciente_id,
                        data_ocupacao = CURRENT_TIMESTAMP,
                        data_liberacao = NULL,
                        data_atualizacao = CURRENT_TIMESTAMP
                    WHERE id = @leito_id AND status = 'LIVRE'
                """
                |> Sql.parameters [
                    "leito_id", Sql.int leitoId
                    "paciente_id", Sql.int pacienteId
                ]
                |> Sql.executeNonQueryAsync
            
            return rowsAffected > 0
        }

    // Repository functions for Internações
    let getAllInternacoes (unidadeId: int option) (status: StatusInternacao option) (dataInicio: DateTime option) (dataFim: DateTime option) =
        task {
            let mutable query = """
                SELECT id, paciente_id, leito_id, medico_responsavel_id, data_internacao,
                       data_alta_prevista, data_alta, tipo_internacao, motivo_internacao,
                       diagnostico, cid10_principal, cid10_secundarios, status, observacoes_alta,
                       valor_total, plano_saude_cobertura, numero_guia, unidade_id,
                       data_cadastro, data_atualizacao
                FROM internacoes 
                WHERE 1=1
            """
            let mutable parameters = []

            match unidadeId with
            | Some uid ->
                query <- query + " AND unidade_id = @unidade_id"
                parameters <- ("unidade_id", Sql.int uid) :: parameters
            | None -> ()

            match status with
            | Some s ->
                query <- query + " AND status = @status"
                parameters <- ("status", Sql.string (statusInternacaoToString s)) :: parameters
            | None -> ()

            match dataInicio with
            | Some inicio ->
                query <- query + " AND data_internacao >= @data_inicio"
                parameters <- ("data_inicio", Sql.timestamp inicio) :: parameters
            | None -> ()

            match dataFim with
            | Some fim ->
                query <- query + " AND data_internacao <= @data_fim"
                parameters <- ("data_fim", Sql.timestamp fim) :: parameters
            | None -> ()

            query <- query + " ORDER BY data_internacao DESC"

            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query query
                |> Sql.parameters parameters
                |> Sql.executeAsync (fun read -> 
                    let cid10SecundariosStr = read.stringOrNone "cid10_secundarios" |> Option.defaultValue ""
                    let cid10Secundarios = 
                        if String.IsNullOrWhiteSpace(cid10SecundariosStr) then []
                        else cid10SecundariosStr.Split(';') |> Array.toList |> List.filter (fun c -> not (String.IsNullOrWhiteSpace(c)))
                    
                    {
                        Id = read.int "id"
                        PacienteId = read.int "paciente_id"
                        LeitoId = read.int "leito_id"
                        MedicoResponsavelId = read.int "medico_responsavel_id"
                        DataInternacao = read.dateTime "data_internacao"
                        DataAltaPrevista = read.dateTimeOrNone "data_alta_prevista"
                        DataAlta = read.dateTimeOrNone "data_alta"
                        TipoInternacao = parseTipoInternacao (read.string "tipo_internacao")
                        MotivoInternacao = read.string "motivo_internacao"
                        Diagnostico = read.stringOrNone "diagnostico"
                        CID10Principal = read.stringOrNone "cid10_principal"
                        CID10Secundarios = cid10Secundarios
                        Status = parseStatusInternacao (read.string "status")
                        ObservacoesAlta = read.stringOrNone "observacoes_alta"
                        ValorTotal = read.decimalOrNone "valor_total"
                        PlanoSaudeCobertura = read.bool "plano_saude_cobertura"
                        NumeroGuia = read.stringOrNone "numero_guia"
                        UnidadeId = read.int "unidade_id"
                        DataCadastro = read.dateTime "data_cadastro"
                        DataAtualizacao = read.dateTimeOrNone "data_atualizacao"
                    })
        }

    let getInternacaoById (id: int) =
        task {
            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT id, paciente_id, leito_id, medico_responsavel_id, data_internacao,
                           data_alta_prevista, data_alta, tipo_internacao, motivo_internacao,
                           diagnostico, cid10_principal, cid10_secundarios, status, observacoes_alta,
                           valor_total, plano_saude_cobertura, numero_guia, unidade_id,
                           data_cadastro, data_atualizacao
                    FROM internacoes 
                    WHERE id = @id
                """
                |> Sql.parameters ["id", Sql.int id]
                |> Sql.executeRowAsync (fun read -> 
                    let cid10SecundariosStr = read.stringOrNone "cid10_secundarios" |> Option.defaultValue ""
                    let cid10Secundarios = 
                        if String.IsNullOrWhiteSpace(cid10SecundariosStr) then []
                        else cid10SecundariosStr.Split(';') |> Array.toList |> List.filter (fun c -> not (String.IsNullOrWhiteSpace(c)))
                    
                    {
                        Id = read.int "id"
                        PacienteId = read.int "paciente_id"
                        LeitoId = read.int "leito_id"
                        MedicoResponsavelId = read.int "medico_responsavel_id"
                        DataInternacao = read.dateTime "data_internacao"
                        DataAltaPrevista = read.dateTimeOrNone "data_alta_prevista"
                        DataAlta = read.dateTimeOrNone "data_alta"
                        TipoInternacao = parseTipoInternacao (read.string "tipo_internacao")
                        MotivoInternacao = read.string "motivo_internacao"
                        Diagnostico = read.stringOrNone "diagnostico"
                        CID10Principal = read.stringOrNone "cid10_principal"
                        CID10Secundarios = cid10Secundarios
                        Status = parseStatusInternacao (read.string "status")
                        ObservacoesAlta = read.stringOrNone "observacoes_alta"
                        ValorTotal = read.decimalOrNone "valor_total"
                        PlanoSaudeCobertura = read.bool "plano_saude_cobertura"
                        NumeroGuia = read.stringOrNone "numero_guia"
                        UnidadeId = read.int "unidade_id"
                        DataCadastro = read.dateTime "data_cadastro"
                        DataAtualizacao = read.dateTimeOrNone "data_atualizacao"
                    })
        }

    let getInternacaoDetalhes (id: int) =
        task {
            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT 
                        i.id, i.data_internacao, i.data_alta_prevista, i.data_alta,
                        i.tipo_internacao, i.status, i.motivo_internacao, i.diagnostico,
                        i.valor_total, i.plano_saude_cobertura, i.numero_guia,
                        p.id as paciente_id, p.nome as paciente_nome, p.cpf as paciente_cpf, p.data_nascimento,
                        l.id as leito_id, l.numero as leito_numero, l.setor as leito_setor, l.tipo_leito,
                        pr.id as medico_id, pr.nome as medico_nome, pr.crm as medico_crm,
                        u.id as unidade_id, u.nome as unidade_nome
                    FROM internacoes i
                    INNER JOIN pacientes p ON i.paciente_id = p.id
                    INNER JOIN leitos l ON i.leito_id = l.id
                    INNER JOIN profissionais pr ON i.medico_responsavel_id = pr.id
                    INNER JOIN unidades u ON i.unidade_id = u.id
                    WHERE i.id = @id
                """
                |> Sql.parameters ["id", Sql.int id]
                |> Sql.executeRowAsync (fun read -> 
                    let dataAlta = read.dateTimeOrNone "data_alta"
                    let dataRef = dataAlta |> Option.defaultValue DateTime.Now
                    let diasInternado = int (dataRef - read.dateTime "data_internacao").TotalDays

                    {
                        Id = read.int "id"
                        Paciente = {|
                            Id = read.int "paciente_id"
                            Nome = read.string "paciente_nome"
                            CPF = read.string "paciente_cpf"
                            DataNascimento = read.dateTime "data_nascimento"
                        |}
                        Leito = {|
                            Id = read.int "leito_id"
                            Numero = read.string "leito_numero"
                            Setor = read.string "leito_setor"
                            TipoLeito = read.string "tipo_leito"
                        |}
                        MedicoResponsavel = {|
                            Id = read.int "medico_id"
                            Nome = read.string "medico_nome"
                            CRM = read.stringOrNone "medico_crm"
                        |}
                        Unidade = {|
                            Id = read.int "unidade_id"
                            Nome = read.string "unidade_nome"
                        |}
                        DataInternacao = read.dateTime "data_internacao"
                        DataAltaPrevista = read.dateTimeOrNone "data_alta_prevista"
                        DataAlta = dataAlta
                        DiasInternado = diasInternado
                        TipoInternacao = parseTipoInternacao (read.string "tipo_internacao")
                        Status = parseStatusInternacao (read.string "status")
                        MotivoInternacao = read.string "motivo_internacao"
                        Diagnostico = read.stringOrNone "diagnostico"
                        ValorTotal = read.decimalOrNone "valor_total"
                        PlanoSaudeCobertura = read.bool "plano_saude_cobertura"
                        NumeroGuia = read.stringOrNone "numero_guia"
                    })
        }

    let insertInternacao (input: InternacaoInput) =
        task {
            let cid10SecundariosStr = String.Join(";", input.CID10Secundarios)
            
            // Primeiro, verificar se o leito está livre
            let! leitoLivre =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT status FROM leitos WHERE id = @leito_id
                """
                |> Sql.parameters ["leito_id", Sql.int input.LeitoId]
                |> Sql.executeRowAsync (fun read -> read.string "status" = "LIVRE")

            if not leitoLivre then
                return Error "Leito não está disponível para internação"
            else
                // Inserir internação
                let! internacaoId =
                    DbConnection.getConnectionString()
                    |> Sql.connect
                    |> Sql.query """
                        INSERT INTO internacoes 
                        (paciente_id, leito_id, medico_responsavel_id, data_internacao, data_alta_prevista,
                         tipo_internacao, motivo_internacao, diagnostico, cid10_principal, cid10_secundarios,
                         plano_saude_cobertura, numero_guia, unidade_id)
                        VALUES 
                        (@paciente_id, @leito_id, @medico_responsavel_id, @data_internacao, @data_alta_prevista,
                         @tipo_internacao, @motivo_internacao, @diagnostico, @cid10_principal, @cid10_secundarios,
                         @plano_saude_cobertura, @numero_guia, @unidade_id)
                        RETURNING id
                    """
                    |> Sql.parameters [
                        "paciente_id", Sql.int input.PacienteId
                        "leito_id", Sql.int input.LeitoId
                        "medico_responsavel_id", Sql.int input.MedicoResponsavelId
                        "data_internacao", Sql.timestamp input.DataInternacao
                        "data_alta_prevista", Sql.timestampOrNone input.DataAltaPrevista
                        "tipo_internacao", Sql.string (tipoInternacaoToString input.TipoInternacao)
                        "motivo_internacao", Sql.string input.MotivoInternacao
                        "diagnostico", Sql.stringOrNone input.Diagnostico
                        "cid10_principal", Sql.stringOrNone input.CID10Principal
                        "cid10_secundarios", Sql.string cid10SecundariosStr
                        "plano_saude_cobertura", Sql.bool input.PlanoSaudeCobertura
                        "numero_guia", Sql.stringOrNone input.NumeroGuia
                        "unidade_id", Sql.int input.UnidadeId
                    ]
                    |> Sql.executeRowAsync (fun read -> read.int "id")

                // Ocupar o leito
                let! _ = ocuparLeito input.LeitoId input.PacienteId

                return Ok internacaoId
        }

    let darAltaInternacao (id: int) (tipoAlta: StatusInternacao) (observacoes: string option) (valorTotal: decimal option) =
        task {
            // Buscar dados da internação
            let! internacao = getInternacaoById id

            let! rowsAffected =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    UPDATE internacoes 
                    SET status = @status,
                        data_alta = CURRENT_TIMESTAMP,
                        observacoes_alta = @observacoes_alta,
                        valor_total = @valor_total,
                        data_atualizacao = CURRENT_TIMESTAMP
                    WHERE id = @id AND status = 'INTERNADO'
                """
                |> Sql.parameters [
                    "id", Sql.int id
                    "status", Sql.string (statusInternacaoToString tipoAlta)
                    "observacoes_alta", Sql.stringOrNone observacoes
                    "valor_total", Sql.decimalOrNone valorTotal
                ]
                |> Sql.executeNonQueryAsync

            if rowsAffected > 0 then
                // Liberar o leito
                let! _ = updateStatusLeito internacao.LeitoId StatusLeito.Limpeza (Some "Liberado após alta")
                ()

            return rowsAffected > 0
        }

    // Repository functions for Suprimentos
    let getAllSuprimentos (unidadeId: int option) (categoria: CategoriaSuprimento option) (status: StatusSuprimento option) =
        task {
            let mutable query = """
                SELECT id, nome, categoria, codigo, descricao, unidade_medida, quantidade_estoque,
                       quantidade_minima, quantidade_maxima, valor_unitario, fornecedor, data_vencimento,
                       status, unidade_id, localizacao_estoque, data_cadastro, data_atualizacao
                FROM suprimentos 
                WHERE 1=1
            """
            let mutable parameters = []

            match unidadeId with
            | Some uid ->
                query <- query + " AND unidade_id = @unidade_id"
                parameters <- ("unidade_id", Sql.int uid) :: parameters
            | None -> ()

            match categoria with
            | Some c ->
                query <- query + " AND categoria = @categoria"
                parameters <- ("categoria", Sql.string (categoriaSuprimentoToString c)) :: parameters
            | None -> ()

            match status with
            | Some s ->
                query <- query + " AND status = @status"
                parameters <- ("status", Sql.string (statusSuprimentoToString s)) :: parameters
            | None -> ()

            query <- query + " ORDER BY nome"

            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query query
                |> Sql.parameters parameters
                |> Sql.executeAsync (fun read -> {
                    Id = read.int "id"
                    Nome = read.string "nome"
                    Categoria = parseCategoriaSuprimento (read.string "categoria")
                    Codigo = read.stringOrNone "codigo"
                    Descricao = read.string "descricao"
                    UnidadeMedida = read.string "unidade_medida"
                    QuantidadeEstoque = read.decimal "quantidade_estoque"
                    QuantidadeMinima = read.decimal "quantidade_minima"
                    QuantidadeMaxima = read.decimal "quantidade_maxima"
                    ValorUnitario = read.decimal "valor_unitario"
                    Fornecedor = read.stringOrNone "fornecedor"
                    DataVencimento = read.dateTimeOrNone "data_vencimento"
                    Status = parseStatusSuprimento (read.string "status")
                    UnidadeId = read.int "unidade_id"
                    LocalizacaoEstoque = read.stringOrNone "localizacao_estoque"
                    DataCadastro = read.dateTime "data_cadastro"
                    DataAtualizacao = read.dateTimeOrNone "data_atualizacao"
                })
        }

    let insertSuprimento (input: SuprimentoInput) =
        task {
            // Determinar status inicial baseado na quantidade
            let statusInicial = 
                if input.QuantidadeEstoque <= 0m then EstoqueZerado
                elif input.QuantidadeEstoque <= input.QuantidadeMinima then EstoqueBaixo
                else EmEstoque

            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    INSERT INTO suprimentos 
                    (nome, categoria, codigo, descricao, unidade_medida, quantidade_estoque,
                     quantidade_minima, quantidade_maxima, valor_unitario, fornecedor, data_vencimento,
                     status, unidade_id, localizacao_estoque)
                    VALUES 
                    (@nome, @categoria, @codigo, @descricao, @unidade_medida, @quantidade_estoque,
                     @quantidade_minima, @quantidade_maxima, @valor_unitario, @fornecedor, @data_vencimento,
                     @status, @unidade_id, @localizacao_estoque)
                    RETURNING id
                """
                |> Sql.parameters [
                    "nome", Sql.string input.Nome
                    "categoria", Sql.string (categoriaSuprimentoToString input.Categoria)
                    "codigo", Sql.stringOrNone input.Codigo
                    "descricao", Sql.string input.Descricao
                    "unidade_medida", Sql.string input.UnidadeMedida
                    "quantidade_estoque", Sql.decimal input.QuantidadeEstoque
                    "quantidade_minima", Sql.decimal input.QuantidadeMinima
                    "quantidade_maxima", Sql.decimal input.QuantidadeMaxima
                    "valor_unitario", Sql.decimal input.ValorUnitario
                    "fornecedor", Sql.stringOrNone input.Fornecedor
                    "data_vencimento", Sql.timestampOrNone input.DataVencimento
                    "status", Sql.string (statusSuprimentoToString statusInicial)
                    "unidade_id", Sql.int input.UnidadeId
                    "localizacao_estoque", Sql.stringOrNone input.LocalizacaoEstoque
                ]
                |> Sql.executeRowAsync (fun read -> read.int "id")
        }

    let movimentarEstoque (suprimentoId: int) (tipoMovimento: string) (quantidade: decimal) (motivo: string) (responsavelId: int) (valorUnitario: decimal option) =
        task {
            // Buscar estoque atual
            let! suprimento =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT quantidade_estoque, quantidade_minima, quantidade_maxima 
                    FROM suprimentos WHERE id = @id
                """
                |> Sql.parameters ["id", Sql.int suprimentoId]
                |> Sql.executeRowAsync (fun read -> {|
                    QuantidadeAtual = read.decimal "quantidade_estoque"
                    QuantidadeMinima = read.decimal "quantidade_minima"
                    QuantidadeMaxima = read.decimal "quantidade_maxima"
                |})

            // Calcular nova quantidade
            let novaQuantidade = 
                match tipoMovimento.ToUpper() with
                | "ENTRADA" -> suprimento.QuantidadeAtual + quantidade
                | "SAIDA" -> suprimento.QuantidadeAtual - quantidade
                | "AJUSTE" -> quantidade
                | "DESCARTE" -> suprimento.QuantidadeAtual - quantidade
                | _ -> suprimento.QuantidadeAtual

            if novaQuantidade < 0m then
                return Error "Quantidade insuficiente em estoque"
            else
                // Determinar novo status
                let novoStatus = 
                    if novaQuantidade <= 0m then EstoqueZerado
                    elif novaQuantidade <= suprimento.QuantidadeMinima then EstoqueBaixo
                    else EmEstoque

                // Registrar movimentação
                let! movimentacaoId =
                    DbConnection.getConnectionString()
                    |> Sql.connect
                    |> Sql.query """
                        INSERT INTO movimentacoes_estoque 
                        (suprimento_id, tipo_movimento, quantidade, valor_unitario, motivo, responsavel_id)
                        VALUES 
                        (@suprimento_id, @tipo_movimento, @quantidade, @valor_unitario, @motivo, @responsavel_id)
                        RETURNING id
                    """
                    |> Sql.parameters [
                        "suprimento_id", Sql.int suprimentoId
                        "tipo_movimento", Sql.string tipoMovimento
                        "quantidade", Sql.decimal quantidade
                        "valor_unitario", Sql.decimalOrNone valorUnitario
                        "motivo", Sql.string motivo
                        "responsavel_id", Sql.int responsavelId
                    ]
                    |> Sql.executeRowAsync (fun read -> read.int "id")

                // Atualizar estoque
                let! _ =
                    DbConnection.getConnectionString()
                    |> Sql.connect
                    |> Sql.query """
                        UPDATE suprimentos 
                        SET quantidade_estoque = @nova_quantidade,
                            status = @status,
                            data_atualizacao = CURRENT_TIMESTAMP
                        WHERE id = @id
                    """
                    |> Sql.parameters [
                        "id", Sql.int suprimentoId
                        "nova_quantidade", Sql.decimal novaQuantidade
                        "status", Sql.string (statusSuprimentoToString novoStatus)
                    ]
                    |> Sql.executeNonQueryAsync

                return Ok movimentacaoId
        }

    // Dashboard and Reports
    // let getDashboard (unidadeId: int option) (data: DateTime) =
    //     task {
    //         let inicioDia = data.Date
    //         let fimDia = inicioDia.AddDays(1.0).AddTicks(-1L)
    //         let inicioMes = DateTime(data.Year, data.Month, 1)
    //         let fimMes = inicioMes.AddMonths(1).AddTicks(-1L)
    //
    //         let unidadeFilter = 
    //             match unidadeId with
    //             | Some uid -> $" AND unidade_id = {uid}"
    //             | None -> ""
    //
    //         // Leitos
    //         let! leitoStats =
    //             DbConnection.getConnectionString()
    //             |> Sql.connect
    //             |> Sql.query $"""
    //                 SELECT 
    //                     COUNT(*) as total_leitos,
    //                     COUNT(CASE WHEN status = 'OCUPADO' THEN 1 END) as leitos_ocupados,
    //                     COUNT(CASE WHEN status = 'LIVRE' THEN 1 END) as leitos_livres,
    //                     COUNT(CASE WHEN status = 'MANUTENCAO' THEN 1 END) as leitos_manutencao
    //                 FROM leitos 
    //                 WHERE 1=1 {unidadeFilter}
    //             """
    //             |> Sql.executeRowAsync (fun read -> {|
    //                 TotalLeitos = read.int "total_leitos"
    //                 LeitosOcupados = read.int "leitos_ocupados"
    //                 LeitosLivres = read.int "leitos_livres"
    //                 LeitosManutencao = read.int "leitos_manutencao"
    //             |})
    //
    //         let taxaOcupacao = 
    //             if leitoStats.TotalLeitos > 0 then
    //                 decimal leitoStats.LeitosOcupados / decimal leitoStats.TotalLeitos * 100m
    //             else 0m
    //
    //         // Internações
    //         let! internacaoStats =
    //             DbConnection.getConnectionString()
    //             |> Sql.connect
    //             |> Sql.query $"""
    //                 SELECT 
    //                     COUNT(CASE WHEN DATE(data_internacao) = DATE(@data) THEN 1 END) as internacoes_hoje,
    //                     COUNT(CASE WHEN DATE(data_alta) = DATE(@data) THEN 1 END) as altas_hoje,
    //                     COUNT(CASE WHEN status = 'INTERNADO' THEN 1 END) as internacoes_ativas,
    //                     AVG(CASE WHEN data_alta IS NOT NULL THEN 
    //                         EXTRACT(DAY FROM (data_alta - data_internacao)) 
    //                     END) as tempo_medio_internacao
    //                 FROM internacoes 
    //                 WHERE 1=1 {unidadeFilter}
    //             """
    //             |> Sql.parameters ["data", Sql.timestamp data]
    //             |> Sql.executeRowAsync (fun read -> {|
    //                 InternacoesHoje = read.int "internacoes_hoje"
    //                 AltasHoje = read.int "altas_hoje"
    //                 InternacoesAtivas = read.int "internacoes_ativas"
    //                 TempoMedioInternacao = read.decimalOrNone "tempo_medio_internacao" |> Option.defaultValue 0m
    //             |})
    //         return 
    //     }

            // Financeiro (simulado - seria integrado com sistema financeiro real)
            // let! financeiroStats =
            //     DbConnection.getConnectionString()
            //     |> Sql.connect
            //     |> Sql.query $"""
            //         SELECT 
            //             COALESCE(SUM(CASE WHEN DATE(data_cadastro) = DATE(@data) THEN 100 END), 0) as receita_hoje,
            //             COALESCE(SUM(CASE WHEN DATE(data_cadastro) = DATE(@data) THEN 50 END), 0) as despesa_hoje,
            //             COALESCE(SUM(CASE WHEN data_cadastro >= @inicio_mes AND data_cadastro <= @fim_mes THEN 3000 END), 0) as receita_mes,
            //             COALESCE(SUM(CASE WHEN data_cadastro >= @inicio_mes AND data_cadastro <= @fim_mes THEN 1500// Administracao/Models.fs (Updated and expanded)
module Handler =
    open Giraffe    
    open Models

    // DTOs
    type UnidadeResponse = {
        Id: int
        Nome: string
        CNPJ: string
        TipoUnidade: string
        Endereco: string
        Telefone: string
        Email: string
        Responsavel: string
        CapacidadeLeitos: int
        Ativa: bool
        DataCadastro: DateTime
        DataAtualizacao: DateTime option
    }

    type UnidadeInputDto = {
        Nome: string
        CNPJ: string
        TipoUnidade: string
        Endereco: string
        Telefone: string
        Email: string
        Responsavel: string
        CapacidadeLeitos: int
    }

    type LeitoResponse = {
        Id: int
        UnidadeId: int
        Numero: string
        Setor: string
        TipoLeito: string
        Status: string
        PacienteId: int option
        DataOcupacao: DateTime option
        DiasOcupado: int option
        ValorDiaria: decimal option
        Equipamentos: string list
        CapacidadeAcompanhantes: int
        ObservacoesStatus: string option
    }

    type LeitoInputDto = {
        UnidadeId: int
        Numero: string
        Setor: string
        TipoLeito: string
        ValorDiaria: decimal option
        Equipamentos: string list
        CapacidadeAcompanhantes: int
    }

    type InternacaoResponse = {
        Id: int
        PacienteId: int
        LeitoId: int
        MedicoResponsavelId: int
        DataInternacao: DateTime
        DataAltaPrevista: DateTime option
        DataAlta: DateTime option
        TipoInternacao: string
        Status: string
        MotivoInternacao: string
        Diagnostico: string option
        ValorTotal: decimal option
        PlanoSaudeCobertura: bool
        NumeroGuia: string option
        UnidadeId: int
    }

    type InternacaoInputDto = {
        PacienteId: int
        LeitoId: int
        MedicoResponsavelId: int
        DataInternacao: DateTime
        DataAltaPrevista: DateTime option
        TipoInternacao: string
        MotivoInternacao: string
        Diagnostico: string option
        CID10Principal: string option
        CID10Secundarios: string list
        PlanoSaudeCobertura: bool
        NumeroGuia: string option
        UnidadeId: int
    }

    type AltaInputDto = {
        TipoAlta: string
        Observacoes: string option
        ValorTotal: decimal option
    }

    type SuprimentoResponse = {
        Id: int
        Nome: string
        Categoria: string
        Codigo: string option
        Descricao: string
        UnidadeMedida: string
        QuantidadeEstoque: decimal
        QuantidadeMinima: decimal
        QuantidadeMaxima: decimal
        ValorUnitario: decimal
        Fornecedor: string option
        DataVencimento: DateTime option
        Status: string
        UnidadeId: int
        LocalizacaoEstoque: string option
    }

    type SuprimentoInputDto = {
        Nome: string
        Categoria: string
        Codigo: string option
        Descricao: string
        UnidadeMedida: string
        QuantidadeEstoque: decimal
        QuantidadeMinima: decimal
        QuantidadeMaxima: decimal
        ValorUnitario: decimal
        Fornecedor: string option
        DataVencimento: DateTime option
        UnidadeId: int
        LocalizacaoEstoque: string option
    }

    type MovimentacaoEstoqueDto = {
        TipoMovimento: string // ENTRADA, SAIDA, AJUSTE, DESCARTE
        Quantidade: decimal
        Motivo: string
        ResponsavelId: int
        ValorUnitario: decimal option
        NotaFiscal: string option
    }

    // Funções auxiliares de conversão
    let private toUnidadeResponse (unidade: Unidade) : UnidadeResponse =
        {
            Id = unidade.Id
            Nome = unidade.Nome
            CNPJ = unidade.CNPJ
            TipoUnidade = 
                match unidade.TipoUnidade with
                | Hospital -> "HOSPITAL"
                | Clinica -> "CLINICA"
                | Laboratorio -> "LABORATORIO"
                | HomeCare -> "HOMECARE"
                | UPA -> "UPA"
                | PostoSaude -> "POSTOSAUDE"
            Endereco = unidade.Endereco
            Telefone = unidade.Telefone
            Email = unidade.Email
            Responsavel = unidade.Responsavel
            CapacidadeLeitos = unidade.CapacidadeLeitos
            Ativa = unidade.Ativa
            DataCadastro = unidade.DataCadastro
            DataAtualizacao = unidade.DataAtualizacao
        }

    let private toLeitoResponse (leito: Leito) : LeitoResponse =
        let diasOcupado = 
            match leito.DataOcupacao with
            | Some dataOcupacao -> Some (int (DateTime.Now - dataOcupacao).TotalDays)
            | None -> None

        {
            Id = leito.Id
            UnidadeId = leito.UnidadeId
            Numero = leito.Numero
            Setor = leito.Setor
            TipoLeito = 
                match leito.TipoLeito with
                | UTI -> "UTI"
                | SemiUTI -> "SEMIUTI"
                | Enfermaria -> "ENFERMARIA"
                | Particular -> "PARTICULAR"
                | Isolamento -> "ISOLAMENTO"
                | Pediatria -> "PEDIATRIA"
                | Maternidade -> "MATERNIDADE"
            Status = 
                match leito.Status with
                | Livre -> "LIVRE"
                | Ocupado -> "OCUPADO"
                | StatusLeito.Limpeza -> "LIMPEZA"
                | Manutencao -> "MANUTENCAO"
                | Bloqueado -> "BLOQUEADO"
            PacienteId = leito.PacienteId
            DataOcupacao = leito.DataOcupacao
            DiasOcupado = diasOcupado
            ValorDiaria = leito.ValorDiaria
            Equipamentos = leito.Equipamentos
            CapacidadeAcompanhantes = leito.CapacidadeAcompanhantes
            ObservacoesStatus = leito.ObservacoesStatus
        }

    let private toInternacaoResponse (internacao: Internacao) : InternacaoResponse =
        {
            Id = internacao.Id
            PacienteId = internacao.PacienteId
            LeitoId = internacao.LeitoId
            MedicoResponsavelId = internacao.MedicoResponsavelId
            DataInternacao = internacao.DataInternacao
            DataAltaPrevista = internacao.DataAltaPrevista
            DataAlta = internacao.DataAlta
            TipoInternacao = 
                match internacao.TipoInternacao with
                | Eletiva -> "ELETIVA"
                | Emergencia -> "EMERGENCIA"
                | Urgencia -> "URGENCIA"
                | Transferencia -> "TRANSFERENCIA"
                | Cirurgica -> "CIRURGICA"
            Status = 
                match internacao.Status with
                | Internado -> "INTERNADO"
                | AltaMedica -> "ALTAMEDICA"
                | AltaHospitalar -> "ALTAHOSPITALAR"
                | Transferido -> "TRANSFERIDO"
                | Obito -> "OBITO"
                | AltaAdministrativa -> "ALTAADMINISTRATIVA"
            MotivoInternacao = internacao.MotivoInternacao
            Diagnostico = internacao.Diagnostico
            ValorTotal = internacao.ValorTotal
            PlanoSaudeCobertura = internacao.PlanoSaudeCobertura
            NumeroGuia = internacao.NumeroGuia
            UnidadeId = internacao.UnidadeId
        }

    let private toSuprimentoResponse (suprimento: Suprimento) : SuprimentoResponse =
        {
            Id = suprimento.Id
            Nome = suprimento.Nome
            Categoria = 
                match suprimento.Categoria with
                | Medicamento -> "MEDICAMENTO"
                | MaterialMedico -> "MATERIALMEDICO"
                | Equipamento -> "EQUIPAMENTO"
                | Limpeza -> "LIMPEZA"
                | Alimentacao -> "ALIMENTACAO"
                | Administrativo -> "ADMINISTRATIVO"
            Codigo = suprimento.Codigo
            Descricao = suprimento.Descricao
            UnidadeMedida = suprimento.UnidadeMedida
            QuantidadeEstoque = suprimento.QuantidadeEstoque
            QuantidadeMinima = suprimento.QuantidadeMinima
            QuantidadeMaxima = suprimento.QuantidadeMaxima
            ValorUnitario = suprimento.ValorUnitario
            Fornecedor = suprimento.Fornecedor
            DataVencimento = suprimento.DataVencimento
            Status = 
                match suprimento.Status with
                | EmEstoque -> "EMESTOQUE"
                | EstoqueBaixo -> "ESTOQUEBAIXO"
                | EstoqueZerado -> "ESTOQUEZERADO"
                | Vencido -> "VENCIDO"
                | Descartado -> "DESCARTADO"
            UnidadeId = suprimento.UnidadeId
            LocalizacaoEstoque = suprimento.LocalizacaoEstoque
        }

    // Domain input conversions
    let private toUnidadeDomainInput (dto: UnidadeInputDto) : UnidadeInput =
        let tipo = 
            match dto.TipoUnidade.ToUpper() with
            | "HOSPITAL" -> Hospital
            | "CLINICA" -> Clinica
            | "LABORATORIO" -> Laboratorio
            | "HOMECARE" -> HomeCare
            | "UPA" -> UPA
            | "POSTOSAUDE" -> PostoSaude
            | _ -> failwith $"Tipo de unidade inválido: {dto.TipoUnidade}"

        {
            Nome = dto.Nome
            CNPJ = dto.CNPJ
            TipoUnidade = tipo
            Endereco = dto.Endereco
            Telefone = dto.Telefone
            Email = dto.Email
            Responsavel = dto.Responsavel
            CapacidadeLeitos = dto.CapacidadeLeitos
        }

    let private toLeitoDomainInput (dto: LeitoInputDto) : LeitoInput =
        let tipo = 
            match dto.TipoLeito.ToUpper() with
            | "UTI" -> UTI
            | "SEMIUTI" -> SemiUTI
            | "ENFERMARIA" -> Enfermaria
            | "PARTICULAR" -> Particular
            | "ISOLAMENTO" -> Isolamento
            | "PEDIATRIA" -> Pediatria
            | "MATERNIDADE" -> Maternidade
            | _ -> failwith $"Tipo de leito inválido: {dto.TipoLeito}"

        {
            UnidadeId = dto.UnidadeId
            Numero = dto.Numero
            Setor = dto.Setor
            TipoLeito = tipo
            ValorDiaria = dto.ValorDiaria
            Equipamentos = dto.Equipamentos
            CapacidadeAcompanhantes = dto.CapacidadeAcompanhantes
        }

    let private toInternacaoDomainInput (dto: InternacaoInputDto) : InternacaoInput =
        let tipo = 
            match dto.TipoInternacao.ToUpper() with
            | "ELETIVA" -> Eletiva
            | "EMERGENCIA" -> Emergencia
            | "URGENCIA" -> Urgencia
            | "TRANSFERENCIA" -> Transferencia
            | "CIRURGICA" -> Cirurgica
            | _ -> failwith $"Tipo de internação inválido: {dto.TipoInternacao}"

        {
            PacienteId = dto.PacienteId
            LeitoId = dto.LeitoId
            MedicoResponsavelId = dto.MedicoResponsavelId
            DataInternacao = dto.DataInternacao
            DataAltaPrevista = dto.DataAltaPrevista
            TipoInternacao = tipo
            MotivoInternacao = dto.MotivoInternacao
            Diagnostico = dto.Diagnostico
            CID10Principal = dto.CID10Principal
            CID10Secundarios = dto.CID10Secundarios
            PlanoSaudeCobertura = dto.PlanoSaudeCobertura
            NumeroGuia = dto.NumeroGuia
            UnidadeId = dto.UnidadeId
        }

    let private toSuprimentoDomainInput (dto: SuprimentoInputDto) : SuprimentoInput =
        let categoria = 
            match dto.Categoria.ToUpper() with
            | "MEDICAMENTO" -> Medicamento
            | "MATERIALMEDICO" -> MaterialMedico
            | "EQUIPAMENTO" -> Equipamento
            | "LIMPEZA" -> Limpeza
            | "ALIMENTACAO" -> Alimentacao
            | "ADMINISTRATIVO" -> Administrativo
            | _ -> failwith $"Categoria de suprimento inválida: {dto.Categoria}"

        {
            Nome = dto.Nome
            Categoria = categoria
            Codigo = dto.Codigo
            Descricao = dto.Descricao
            UnidadeMedida = dto.UnidadeMedida
            QuantidadeEstoque = dto.QuantidadeEstoque
            QuantidadeMinima = dto.QuantidadeMinima
            QuantidadeMaxima = dto.QuantidadeMaxima
            ValorUnitario = dto.ValorUnitario
            Fornecedor = dto.Fornecedor
            DataVencimento = dto.DataVencimento
            UnidadeId = dto.UnidadeId
            LocalizacaoEstoque = dto.LocalizacaoEstoque
        }

    // Validações
    let private validateUnidadeInput (dto: UnidadeInputDto) =
        let errors = ResizeArray<string>()
        
        if String.IsNullOrWhiteSpace(dto.Nome) then
            errors.Add("Nome é obrigatório")
        
        if String.IsNullOrWhiteSpace(dto.CNPJ) || dto.CNPJ.Length <> 14 then
            errors.Add("CNPJ deve ter 14 dígitos")
        
        if String.IsNullOrWhiteSpace(dto.Endereco) then
            errors.Add("Endereço é obrigatório")
        
        if String.IsNullOrWhiteSpace(dto.Telefone) then
            errors.Add("Telefone é obrigatório")
        
        if String.IsNullOrWhiteSpace(dto.Email) || not (dto.Email.Contains("@")) then
            errors.Add("Email válido é obrigatório")
        
        if dto.CapacidadeLeitos <= 0 then
            errors.Add("Capacidade de leitos deve ser maior que zero")

        errors |> Seq.toList

    let private validateLeitoInput (dto: LeitoInputDto) =
        let errors = ResizeArray<string>()
        
        if dto.UnidadeId <= 0 then
            errors.Add("Unidade deve ser especificada")
        
        if String.IsNullOrWhiteSpace(dto.Numero) then
            errors.Add("Número do leito é obrigatório")
        
        if String.IsNullOrWhiteSpace(dto.Setor) then
            errors.Add("Setor é obrigatório")
        
        if dto.CapacidadeAcompanhantes < 0 then
            errors.Add("Capacidade de acompanhantes não pode ser negativa")

        errors |> Seq.toList

    let private validateInternacaoInput (dto: InternacaoInputDto) =
        let errors = ResizeArray<string>()
        
        if dto.PacienteId <= 0 then
            errors.Add("Paciente deve ser especificado")
        
        if dto.LeitoId <= 0 then
            errors.Add("Leito deve ser especificado")
        
        if dto.MedicoResponsavelId <= 0 then
            errors.Add("Médico responsável deve ser especificado")
        
        if String.IsNullOrWhiteSpace(dto.MotivoInternacao) then
            errors.Add("Motivo da internação é obrigatório")
        
        if dto.DataInternacao > DateTime.Now then
            errors.Add("Data de internação não pode ser no futuro")
        
        match dto.DataAltaPrevista with
        | Some dataAlta when dataAlta <= dto.DataInternacao ->
            errors.Add("Data de alta prevista deve ser posterior à data de internação")
        | _ -> ()

        errors |> Seq.toList

    // Handlers para Unidades
    let getAllUnidades : HttpHandler =
        fun next ctx ->
            task {
                try
                    let ativa = 
                        match ctx.TryGetQueryStringValue "ativa" with
                        | Some "true" -> Some true
                        | Some "false" -> Some false
                        | _ -> None

                    let! unidades = Repository.getAllUnidades ativa
                    let response = unidades |> List.map toUnidadeResponse
                    
                    return! json response next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro interno do servidor"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let getUnidadeById unidadeId : HttpHandler =
        fun next ctx ->
            task {
                try
                    let! unidade = Repository.getUnidadeById unidadeId
                    let response = toUnidadeResponse unidade
                    
                    return! json response next ctx
                with
                | :? System.InvalidOperationException ->
                    let errorResponse = {| error = "Unidade não encontrada" |}
                    return! (setStatusCode 404 >=> json errorResponse) next ctx
                | ex ->
                    let errorResponse = {| error = "Erro interno do servidor"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let createUnidade : HttpHandler =
        fun next ctx ->
            task {
                try
                    let! inputDto = ctx.BindJsonAsync<UnidadeInputDto>()
                    
                    let validationErrors = validateUnidadeInput inputDto
                    if not validationErrors.IsEmpty then
                        let errorResponse = {| errors = validationErrors |}
                        return! (setStatusCode 400 >=> json errorResponse) next ctx
                    else
                        let domainInput = toUnidadeDomainInput inputDto
                        let! id = Repository.insertUnidade domainInput
                        let response = {| id = id; message = "Unidade criada com sucesso" |}
                        
                        return! (setStatusCode 201 >=> json response) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao criar unidade"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let updateUnidade unidadeId : HttpHandler =
        fun next ctx ->
            task {
                try                    
                    let! inputDto = ctx.BindJsonAsync<UnidadeInputDto>()
                    
                    let validationErrors = validateUnidadeInput inputDto
                    if not validationErrors.IsEmpty then
                        let errorResponse = {| errors = validationErrors |}
                        return! (setStatusCode 400 >=> json errorResponse) next ctx
                    else
                        let domainInput = toUnidadeDomainInput inputDto
                        let! success = Repository.updateUnidade unidadeId domainInput
                        
                        if success then
                            let response = {| message = "Unidade atualizada com sucesso" |}
                            return! json response next ctx
                        else
                            let errorResponse = {| error = "Unidade não encontrada" |}
                            return! (setStatusCode 404 >=> json errorResponse) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao atualizar unidade"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }
    // Handlers para Leitos
    let getAllLeitos : HttpHandler =
        fun next ctx ->
            task {
                try
                    let unidadeId = 
                        match ctx.TryGetQueryStringValue "unidadeId" with
                        | Some idStr -> Int32.TryParse(idStr) |> function | true, id -> Some id | _ -> None
                        | None -> None

                    let status = 
                        match ctx.TryGetQueryStringValue "status" with
                        | Some statusStr -> 
                            match statusStr.ToUpper() with
                            | "LIVRE" -> Some Livre
                            | "OCUPADO" -> Some Ocupado
                            | "LIMPEZA" -> Some StatusLeito.Limpeza
                            | "MANUTENCAO" -> Some Manutencao
                            | "BLOQUEADO" -> Some Bloqueado
                            | _ -> None
                        | None -> None

                    let tipoLeito = 
                        match ctx.TryGetQueryStringValue "tipoLeito" with
                        | Some tipoStr -> 
                            match tipoStr.ToUpper() with
                            | "UTI" -> Some UTI
                            | "SEMIUTI" -> Some SemiUTI
                            | "ENFERMARIA" -> Some Enfermaria
                            | "PARTICULAR" -> Some Particular
                            | "ISOLAMENTO" -> Some Isolamento
                            | "PEDIATRIA" -> Some Pediatria
                            | "MATERNIDADE" -> Some Maternidade
                            | _ -> None
                        | None -> None

                    let! leitos = Repository.getAllLeitos unidadeId status tipoLeito
                    let response = leitos |> List.map toLeitoResponse
                    
                    return! json response next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro interno do servidor"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let getLeitosDetalhes : HttpHandler =
        fun next ctx ->
            task {
                try
                    let unidadeId = 
                        match ctx.TryGetQueryStringValue "unidadeId" with
                        | Some idStr -> Int32.TryParse(idStr) |> function | true, id -> Some id | _ -> None
                        | None -> None

                    let status = 
                        match ctx.TryGetQueryStringValue "status" with
                        | Some statusStr -> 
                            match statusStr.ToUpper() with
                            | "LIVRE" -> Some Livre
                            | "OCUPADO" -> Some Ocupado
                            | "LIMPEZA" -> Some StatusLeito.Limpeza
                            | "MANUTENCAO" -> Some Manutencao
                            | "BLOQUEADO" -> Some Bloqueado
                            | _ -> None
                        | None -> None

                    let! leitosDetalhes = Repository.getLeitosDetalhes unidadeId status
                    return! json leitosDetalhes next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro interno do servidor"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let getLeitoById leitoId : HttpHandler =
        fun next ctx ->
            task {
                try
                    let! leito = Repository.getLeitoById leitoId
                    let response = toLeitoResponse leito
                    
                    return! json response next ctx
                with
                | :? System.InvalidOperationException ->
                    let errorResponse = {| error = "Leito não encontrado" |}
                    return! (setStatusCode 404 >=> json errorResponse) next ctx
                | ex ->
                    let errorResponse = {| error = "Erro interno do servidor"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let createLeito : HttpHandler =
        fun next ctx ->
            task {
                try
                    let! inputDto = ctx.BindJsonAsync<LeitoInputDto>()
                    
                    let validationErrors = validateLeitoInput inputDto
                    if not validationErrors.IsEmpty then
                        let errorResponse = {| errors = validationErrors |}
                        return! (setStatusCode 400 >=> json errorResponse) next ctx
                    else
                        let domainInput = toLeitoDomainInput inputDto
                        let! id = Repository.insertLeito domainInput
                        let response = {| id = id; message = "Leito criado com sucesso" |}
                        
                        return! (setStatusCode 201 >=> json response) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao criar leito"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let updateStatusLeito leitoId : HttpHandler =
        fun next ctx ->
            task {
                try                    
                    let! statusDto = ctx.BindJsonAsync<{| status: string; observacoes: string option |}>()
                    
                    let novoStatus = 
                        match statusDto.status.ToUpper() with
                        | "LIVRE" -> Livre
                        | "OCUPADO" -> Ocupado
                        | "LIMPEZA" -> StatusLeito.Limpeza
                        | "MANUTENCAO" -> Manutencao
                        | "BLOQUEADO" -> Bloqueado
                        | _ -> failwith $"Status de leito inválido: {statusDto.status}"

                    let! success = Repository.updateStatusLeito leitoId novoStatus statusDto.observacoes
                    
                    if success then
                        let response = {| message = $"Status do leito alterado para {statusDto.status}" |}
                        return! json response next ctx
                    else
                        let errorResponse = {| error = "Leito não encontrado" |}
                        return! (setStatusCode 404 >=> json errorResponse) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao atualizar status do leito"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    // Handlers para Internações
    let getAllInternacoes : HttpHandler =
        fun next ctx ->
            task {
                try
                    let unidadeId = 
                        match ctx.TryGetQueryStringValue "unidadeId" with
                        | Some idStr -> Int32.TryParse(idStr) |> function | true, id -> Some id | _ -> None
                        | None -> None

                    let status = 
                        match ctx.TryGetQueryStringValue "status" with
                        | Some statusStr -> 
                            match statusStr.ToUpper() with
                            | "INTERNADO" -> Some Internado
                            | "ALTAMEDICA" -> Some AltaMedica
                            | "ALTAHOSPITALAR" -> Some AltaHospitalar
                            | "TRANSFERIDO" -> Some Transferido
                            | "OBITO" -> Some Obito
                            | "ALTAADMINISTRATIVA" -> Some AltaAdministrativa
                            | _ -> None
                        | None -> None

                    let dataInicio = 
                        match ctx.TryGetQueryStringValue "dataInicio" with
                        | Some dateStr -> DateTime.TryParse(dateStr) |> function | true, dt -> Some dt | _ -> None
                        | None -> None

                    let dataFim = 
                        match ctx.TryGetQueryStringValue "dataFim" with
                        | Some dateStr -> DateTime.TryParse(dateStr) |> function | true, dt -> Some dt | _ -> None
                        | None -> None

                    let! internacoes = Repository.getAllInternacoes unidadeId status dataInicio dataFim
                    let response = internacoes |> List.map toInternacaoResponse
                    
                    return! json response next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro interno do servidor"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let getInternacaoById internacaoId : HttpHandler =
        fun next ctx ->
            task {
                try
                    let! internacao = Repository.getInternacaoById internacaoId
                    let response = toInternacaoResponse internacao
                    
                    return! json response next ctx
                with
                | :? System.InvalidOperationException ->
                    let errorResponse = {| error = "Internação não encontrada" |}
                    return! (setStatusCode 404 >=> json errorResponse) next ctx
                | ex ->
                    let errorResponse = {| error = "Erro interno do servidor"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let getInternacaoDetalhes internacaoId : HttpHandler =
        fun next ctx ->
            task {
                try
                    let! detalhes = Repository.getInternacaoDetalhes internacaoId
                    return! json detalhes next ctx
                with
                | :? System.InvalidOperationException ->
                    let errorResponse = {| error = "Internação não encontrada" |}
                    return! (setStatusCode 404 >=> json errorResponse) next ctx
                | ex ->
                    let errorResponse = {| error = "Erro interno do servidor"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let createInternacao : HttpHandler =
        fun next ctx ->
            task {
                try
                    let! inputDto = ctx.BindJsonAsync<InternacaoInputDto>()
                    
                    let validationErrors = validateInternacaoInput inputDto
                    if not validationErrors.IsEmpty then
                        let errorResponse = {| errors = validationErrors |}
                        return! (setStatusCode 400 >=> json errorResponse) next ctx
                    else
                        let domainInput = toInternacaoDomainInput inputDto
                        let! result = Repository.insertInternacao domainInput
                        
                        match result with
                        | Ok id ->
                            let response = {| id = id; message = "Internação criada com sucesso" |}
                            return! (setStatusCode 201 >=> json response) next ctx
                        | Error erro ->
                            let errorResponse = {| error = erro |}
                            return! (setStatusCode 409 >=> json errorResponse) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao criar internação"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let darAltaInternacao internacaoId : HttpHandler =
        fun next ctx ->
            task {
                try
                    let! altaDto = ctx.BindJsonAsync<AltaInputDto>()
                    
                    let tipoAlta = 
                        match altaDto.TipoAlta.ToUpper() with
                        | "ALTAMEDICA" -> AltaMedica
                        | "ALTAHOSPITALAR" -> AltaHospitalar
                        | "TRANSFERIDO" -> Transferido
                        | "OBITO" -> Obito
                        | "ALTAADMINISTRATIVA" -> AltaAdministrativa
                        | _ -> failwith $"Tipo de alta inválido: {altaDto.TipoAlta}"

                    let! success = Repository.darAltaInternacao internacaoId tipoAlta altaDto.Observacoes altaDto.ValorTotal
                    
                    if success then
                        let response = {| message = "Alta realizada com sucesso" |}
                        return! json response next ctx
                    else
                        let errorResponse = {| error = "Internação não encontrada ou não está ativa" |}
                        return! (setStatusCode 404 >=> json errorResponse) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao dar alta"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    // Handlers para Suprimentos
    let getAllSuprimentos : HttpHandler =
        fun next ctx ->
            task {
                try
                    let unidadeId = 
                        match ctx.TryGetQueryStringValue "unidadeId" with
                        | Some idStr -> Int32.TryParse(idStr) |> function | true, id -> Some id | _ -> None
                        | None -> None

                    let categoria = 
                        match ctx.TryGetQueryStringValue "categoria" with
                        | Some catStr -> 
                            match catStr.ToUpper() with
                            | "MEDICAMENTO" -> Some Medicamento
                            | "MATERIALMEDICO" -> Some MaterialMedico
                            | "EQUIPAMENTO" -> Some Equipamento
                            | "LIMPEZA" -> Some Limpeza
                            | "ALIMENTACAO" -> Some Alimentacao
                            | "ADMINISTRATIVO" -> Some Administrativo
                            | _ -> None
                        | None -> None

                    let status = 
                        match ctx.TryGetQueryStringValue "status" with
                        | Some statusStr -> 
                            match statusStr.ToUpper() with
                            | "EMESTOQUE" -> Some EmEstoque
                            | "ESTOQUEBAIXO" -> Some EstoqueBaixo
                            | "ESTOQUEZERADO" -> Some EstoqueZerado
                            | "VENCIDO" -> Some Vencido
                            | "DESCARTADO" -> Some Descartado
                            | _ -> None
                        | None -> None

                    let! suprimentos = Repository.getAllSuprimentos unidadeId categoria status
                    let response = suprimentos |> List.map toSuprimentoResponse
                    
                    return! json response next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro interno do servidor"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let createSuprimento : HttpHandler =
        fun next ctx ->
            task {
                try
                    let! inputDto = ctx.BindJsonAsync<SuprimentoInputDto>()
                    
                    if String.IsNullOrWhiteSpace(inputDto.Nome) ||
                       String.IsNullOrWhiteSpace(inputDto.Descricao) ||
                       String.IsNullOrWhiteSpace(inputDto.UnidadeMedida) ||
                       inputDto.UnidadeId <= 0 then
                        let errorResponse = {| error = "Nome, descrição, unidade de medida e unidade são obrigatórios" |}
                        return! (setStatusCode 400 >=> json errorResponse) next ctx
                    else
                        let domainInput = toSuprimentoDomainInput inputDto
                        let! id = Repository.insertSuprimento domainInput
                        let response = {| id = id; message = "Suprimento criado com sucesso" |}
                        
                        return! (setStatusCode 201 >=> json response) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao criar suprimento"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let movimentarEstoque suprimentoId : HttpHandler =
        fun next ctx ->
            task {
                try
                    let! movDto = ctx.BindJsonAsync<MovimentacaoEstoqueDto>()
                    
                    if movDto.Quantidade <= 0m then
                        let errorResponse = {| error = "Quantidade deve ser maior que zero" |}
                        return! (setStatusCode 400 >=> json errorResponse) next ctx
                    elif String.IsNullOrWhiteSpace(movDto.Motivo) then
                        let errorResponse = {| error = "Motivo é obrigatório" |}
                        return! (setStatusCode 400 >=> json errorResponse) next ctx
                    else
                        let! result = Repository.movimentarEstoque suprimentoId movDto.TipoMovimento movDto.Quantidade movDto.Motivo movDto.ResponsavelId movDto.ValorUnitario
                        
                        match result with
                        | Ok movimentacaoId ->
                            let response = {| id = movimentacaoId; message = "Movimentação realizada com sucesso" |}
                            return! (setStatusCode 201 >=> json response) next ctx
                        | Error erro ->
                            let errorResponse = {| error = erro |}
                            return! (setStatusCode 400 >=> json errorResponse) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao movimentar estoque"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    // Dashboard e Relatórios
    // let getDashboard : HttpHandler =
    //     fun next ctx ->
    //         task {
    //             try
    //                 let unidadeId = 
    //                     match ctx.TryGetQueryStringValue "unidadeId" with
    //                     | Some idStr -> Int32.TryParse(idStr) |> function | true, id -> Some id | _ -> None
    //                     | None -> None
    //
    //                 let data = 
    //                     match ctx.TryGetQueryStringValue "data" with
    //                     | Some dateStr -> 
    //                         match DateTime.TryParse(dateStr) with
    //                         | true, dt -> dt
    //                         | false, _ -> DateTime.Today
    //                     | None -> DateTime.Today
    //
    //                 let! dashboard = Repository.getDashboard unidadeId data
    //                 
    //                 let response = {|
    //                     unidadeId = dashboard.UnidadeId
    //                     data = dashboard.Data.ToString("yyyy-MM-dd")
    //                     leitos = {|
    //                         total = dashboard.TotalLeitos
    //                         ocupados = dashboard.LeitosOcupados
    //                         livres = dashboard.LeitosLivres
    //                         manutencao = dashboard.LeitosManutencao
    //                         taxaOcupacao = dashboard.TaxaOcupacao
    //                     |}
    //                     internacoes = {|
    //                         hoje = dashboard.InternacoesHoje
    //                         altasHoje = dashboard.AltasHoje
    //                         ativas = dashboard.InternacoesAtivas
    //                         tempoMedioInternacao = dashboard.TempoMedioInternacao
    //                     |}
    //                     financeiro = {|
    //                         receitaHoje = dashboard.ReceitaHoje
    //                         despesaHoje = dashboard.DespesaHoje
    //                         receitaMes = dashboard.ReceitaMes
    //                         despesaMes = dashboard.DespesaMes
    //                     |}
    //                     suprimentos = {|
    //                         itensEstoqueBaixo = dashboard.ItensEstoqueBaixo
    //                         itensVencendo = dashboard.ItensVencendo
    //                         valorEstoque = dashboard.ValorEstoque
    //                     |}
    //                     proximasAcoes = {|
    //                         altasPrevistas = dashboard.AltasPrevistas
    //                         manutencoesProgramadas = dashboard.ManutencoesProgramadas
    //                     |}
    //                 |}
    //                 
    //                 return! json response next ctx
    //             with
    //             | ex ->
    //                 let errorResponse = {| error = "Erro ao obter dashboard"; details = ex.Message |}
    //                 return! (setStatusCode 500 >=> json errorResponse) next ctx
    //         }

    // let gerarRelatorioFinanceiro : HttpHandler =
    //     fun next ctx ->
    //         task {
    //             try
    //                 let! relatorioDto = ctx.BindJsonAsync<{| unidadeId: int; periodo: string |}>()
    //                 
    //                 // TODO: Obter usuarioId do token JWT
    //                 let geradoPor = 1 // Placeholder - deve vir da autenticação
    //                 
    //                 let! relatorioId = Repository.gerarRelatorioFinanceiro relatorioDto.unidadeId relatorioDto.periodo geradoPor
    //                 let response = {| id = relatorioId; message = "Relatório financeiro gerado com sucesso" |}
    //                 
    //                 return! (setStatusCode 201 >=> json response) next ctx
    //             with
    //             | ex ->
    //                 let errorResponse = {| error = "Erro ao gerar relatório"; details = ex.Message |}
    //                 return! (setStatusCode 500 >=> json errorResponse) next ctx
    //         }

    // Rotas
    let routes : HttpHandler =
        choose [
            GET >=> choose [
                // Dashboard
                // route "/dashboard" >=> getDashboard
                
                // Unidades
                route "/unidades" >=> getAllUnidades
                routef "/unidades/%i" getUnidadeById
                
                // Leitos
                route "/leitos" >=> getAllLeitos
                route "/leitos/detalhes" >=> getLeitosDetalhes
                routef "/leitos/%i" getLeitoById
                
                // Internações
                route "/internacoes" >=> getAllInternacoes
                routef "/internacoes/%i" getInternacaoById
                routef "/internacoes/%i/detalhes" getInternacaoDetalhes
                
                // Suprimentos
                route "/suprimentos" >=> getAllSuprimentos
            ]
            POST >=> choose [
                // Unidades
                route "/unidades" >=> createUnidade
                
                // Leitos
                route "/leitos" >=> createLeito
                
                // Internações
                route "/internacoes" >=> createInternacao
                routef "/internacoes/%i/alta" darAltaInternacao
                
                // Suprimentos
                route "/suprimentos" >=> createSuprimento
                routef "/suprimentos/%i/movimentar" movimentarEstoque
                
                // Relatórios
                // route "/relatorios/financeiro" >=> gerarRelatorioFinanceiro
            ]
            PUT >=> choose [
                // Unidades
                routef "/unidades/%i" updateUnidade
                
                // Leitos
                routef "/leitos/%i/status" updateStatusLeito
            ]
        ]
//
//
// -- Adicionar à migration SQL:
//
// -- Tabela expandida de unidades
// ALTER TABLE unidades ADD COLUMN IF NOT EXISTS capacidade_leitos INTEGER DEFAULT 0;
// ALTER TABLE unidades ADD COLUMN IF NOT EXISTS data_atualizacao TIMESTAMP;
//
// -- Tabela de leitos
// CREATE TABLE IF NOT EXISTS leitos (
//     id SERIAL PRIMARY KEY,
//     unidade_id INTEGER REFERENCES unidades(id) NOT NULL,
//     numero VARCHAR(20) NOT NULL,
//     setor VARCHAR(100) NOT NULL,
//     tipo_leito VARCHAR(20) NOT NULL CHECK (tipo_leito IN ('UTI', 'SEMIUTI', 'ENFERMARIA', 'PARTICULAR', 'ISOLAMENTO', 'PEDIATRIA', 'MATERNIDADE')),
//     status VARCHAR(20) NOT NULL DEFAULT 'LIVRE' CHECK (status IN ('LIVRE', 'OCUPADO', 'LIMPEZA', 'MANUTENCAO', 'BLOQUEADO')),
//     paciente_id INTEGER REFERENCES pacientes(id),
//     data_ocupacao TIMESTAMP,
//     data_liberacao TIMESTAMP,
//     observacoes_status TEXT,
//     valor_diaria DECIMAL(10,2),
//     equipamentos TEXT, -- Separados por ;
//     capacidade_acompanhantes INTEGER DEFAULT 0,
//     data_cadastro TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
//     data_atualizacao TIMESTAMP,
//     UNIQUE(unidade_id, numero)
// );
//
// -- Tabela de internações
// CREATE TABLE IF NOT EXISTS internacoes (
//     id SERIAL PRIMARY KEY,
//     paciente_id INTEGER REFERENCES pacientes(id) NOT NULL,
//     leito_id INTEGER REFERENCES leitos(id) NOT NULL,
//     medico_responsavel_id INTEGER REFERENCES profissionais(id) NOT NULL,
//     data_internacao TIMESTAMP NOT NULL,
//     data_alta_prevista TIMESTAMP,
//     data_alta TIMESTAMP,
//     tipo_internacao VARCHAR(20) NOT NULL CHECK (tipo_internacao IN ('ELETIVA', 'EMERGENCIA', 'URGENCIA', 'TRANSFERENCIA', 'CIRURGICA')),
//     motivo_internacao TEXT NOT NULL,
//     diagnostico TEXT,
//     cid10_principal VARCHAR(10),
//     cid10_secundarios TEXT, -- Separados por ;
//     status VARCHAR(20) NOT NULL DEFAULT 'INTERNADO' CHECK (status IN ('INTERNADO', 'ALTAMEDICA', 'ALTAHOSPITALAR', 'TRANSFERIDO', 'OBITO', 'ALTAADMINISTRATIVA')),
//     observacoes_alta TEXT,
//     valor_total DECIMAL(12,2),
//     plano_saude_cobertura BOOLEAN DEFAULT false,
//     numero_guia VARCHAR(50),
//     unidade_id INTEGER REFERENCES unidades(id) NOT NULL,
//     data_cadastro TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
//     data_atualizacao TIMESTAMP
// );
//
// -- Tabela de suprimentos
// CREATE TABLE IF NOT EXISTS suprimentos (
//     id SERIAL PRIMARY KEY,
//     nome VARCHAR(200) NOT NULL,
//     categoria VARCHAR(20) NOT NULL CHECK (categoria IN ('MEDICAMENTO', 'MATERIALMEDICO', 'EQUIPAMENTO', 'LIMPEZA', 'ALIMENTACAO', 'ADMINISTRATIVO')),
//     codigo VARCHAR(50),
//     descricao TEXT NOT NULL,
//     unidade_medida VARCHAR(10) NOT NULL, -- UN, KG, ML, etc.
//     quantidade_estoque DECIMAL(12,3) DEFAULT 0,
//     quantidade_minima DECIMAL(12,3) NOT NULL,
//     quantidade_maxima DECIMAL(12,3) NOT NULL,
//     valor_unitario DECIMAL(10,2) NOT NULL,
//     fornecedor VARCHAR(200),
//     data_vencimento DATE,
//     status VARCHAR(20) NOT NULL DEFAULT 'EMESTOQUE' CHECK (status IN ('EMESTOQUE', 'ESTOQUEBAIXO', 'ESTOQUEZERADO', 'VENCIDO', 'DESCARTADO')),
//     unidade_id INTEGER REFERENCES unidades(id) NOT NULL,
//     localizacao_estoque VARCHAR(100),
//     data_cadastro TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
//     data_atualizacao TIMESTAMP
// );
//
// -- Tabela de movimentações de estoque
// CREATE TABLE IF NOT EXISTS movimentacoes_estoque (
//     id SERIAL PRIMARY KEY,
//     suprimento_id INTEGER REFERENCES suprimentos(id) NOT NULL,
//     tipo_movimento VARCHAR(20) NOT NULL CHECK (tipo_movimento IN ('ENTRADA', 'SAIDA', 'AJUSTE', 'DESCARTE')),
//     quantidade DECIMAL(12,3) NOT NULL,
//     valor_unitario DECIMAL(10,2),
//     motivo TEXT NOT NULL,
//     responsavel_id INTEGER REFERENCES profissionais(id) NOT NULL,
//     data_movimento TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
//     observacoes TEXT,
//     nota_fiscal VARCHAR(50)
// );
//
// -- Tabela de relatórios financeiros
// CREATE TABLE IF NOT EXISTS relatorios_financeiros (
//     id SERIAL PRIMARY KEY,
//     unidade_id INTEGER REFERENCES unidades(id) NOT NULL,
//     periodo VARCHAR(20) NOT NULL, -- "2024-01" para mensal
//     tipo_relatorio VARCHAR(20) NOT NULL CHECK (tipo_relatorio IN ('MENSAL', 'TRIMESTRAL', 'ANUAL')),
//     total_receita DECIMAL(12,2) DEFAULT 0,
//     total_despesa DECIMAL(12,2) DEFAULT 0,
//     lucro_liquido DECIMAL(12,2) DEFAULT 0,
//     consultas_realizadas INTEGER DEFAULT 0,
//     exames_realizados INTEGER DEFAULT 0,
//     internacoes_total INTEGER DEFAULT 0,
//     sessoes_telemedicina INTEGER DEFAULT 0,
//     ticket_medio_consulta DECIMAL(10,2) DEFAULT 0,
//     taxa_ocupacao_leitos DECIMAL(5,2) DEFAULT 0,
//     data_geracao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
//     gerado_por INTEGER REFERENCES profissionais(id) NOT NULL
// );
//
// -- Índices para performance
// CREATE INDEX IF NOT EXISTS idx_leitos_unidade_status ON leitos(unidade_id, status);
// CREATE INDEX IF NOT EXISTS idx_leitos_paciente ON leitos(paciente_id);
// CREATE INDEX IF NOT EXISTS idx_internacoes_paciente ON internacoes(paciente_id, data_internacao);
// CREATE INDEX IF NOT EXISTS idx_internacoes_leito ON internacoes(leito_id);
// CREATE INDEX IF NOT EXISTS idx_internacoes_status ON internacoes(status, unidade_id);
// CREATE INDEX IF NOT EXISTS idx_internacoes_data ON internacoes(data_internacao, data_alta);
// CREATE INDEX IF NOT EXISTS idx_suprimentos_categoria ON suprimentos(categoria, unidade_id);
// CREATE INDEX IF NOT EXISTS idx_suprimentos_status ON suprimentos(status, data_vencimento);
// CREATE INDEX IF NOT EXISTS idx_movimentacoes_suprimento ON movimentacoes_estoque(suprimento_id, data_movimento);
//
// -- Triggers para atualização automática de status
//
// -- Trigger para atualizar status de suprimento baseado na quantidade
// CREATE OR REPLACE FUNCTION update_suprimento_status()
// RETURNS TRIGGER AS $
// BEGIN
//     IF NEW.quantidade_estoque <= 0 THEN
//         NEW.status := 'ESTOQUEZERADO';
//     ELSIF NEW.quantidade_estoque <= NEW.quantidade_minima THEN
//         NEW.status := 'ESTOQUEBAIXO';
//     ELSE
//         NEW.status := 'EMESTOQUE';
//     END IF;
//     
//     -- Verificar vencimento
//     IF NEW.data_vencimento IS NOT NULL AND NEW.data_vencimento <= CURRENT_DATE THEN
//         NEW.status := 'VENCIDO';
//     END IF;
//     
//     NEW.data_atualizacao := CURRENT_TIMESTAMP;
//     RETURN NEW;
// END;
// $ LANGUAGE plpgsql;

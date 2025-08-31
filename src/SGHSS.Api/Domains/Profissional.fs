namespace Domains.Profissional

open System
open Infrastructure.Database
open NSwag
open NSwag.Annotations
open SGHSS.Api.Logging.Logger

module Models =
    type TipoProfissional = 
        | Medico 
        | Enfermeiro 
        | Tecnico 
        | Fisioterapeuta 
        | Psicologo 
        | Nutricionista
        | Farmaceutico
        | Administrativo
    
    type Especialidade = {
        Id: int
        Nome: string
        Codigo: string
        ConselhoRegulamentador: string
    }
    
    type Profissional = {
        Id: int
        Nome: string
        CPF: string
        CRM: string option // ou CRE, COREN, etc.
        Especialidades: Especialidade list
        TipoProfissional: TipoProfissional
        Email: string
        Telefone: string
        DataAdmissao: DateTime
        DataDemissao: DateTime option
        Ativo: bool
        UnidadeId: int
        PermiteTelemedicina: bool
        DataCadastro: DateTime
        DataAtualizacao: DateTime option
    }
    
    type HorarioAtendimento = {
        Id: int
        ProfissionalId: int
        DiaSemana: int // 0-6 (domingo-sábado)
        HoraInicio: TimeSpan
        HoraFim: TimeSpan
        Ativo: bool
    }
    type ProfissionalInput = {
        Nome: string
        CPF: string
        CRM: string option
        TipoProfissional: TipoProfissional
        Email: string
        Telefone: string
        DataAdmissao: DateTime
        UnidadeId: int
        PermiteTelemedicina: bool
    }
module Repository =
    open Npgsql.FSharp
    open Models
    open System    

    // Função auxiliar para mapear TipoProfissional do banco
    let private parseTipoProfissional (tipo: string) =
        match tipo.ToUpper() with
        | "MEDICO" -> Medico
        | "ENFERMEIRO" -> Enfermeiro
        | "TECNICO" -> Tecnico
        | "FISIOTERAPEUTA" -> Fisioterapeuta
        | "PSICOLOGO" -> Psicologo
        | "NUTRICIONISTA" -> Nutricionista
        | "FARMACEUTICO" -> Farmaceutico
        | "ADMINISTRATIVO" -> Administrativo
        | _ -> Administrativo // default

    // Função auxiliar para converter TipoProfissional para string
    let private tipoProfissionalToString (tipo: TipoProfissional) =
        match tipo with
        | Medico -> "MEDICO"
        | Enfermeiro -> "ENFERMEIRO"
        | Tecnico -> "TECNICO"
        | Fisioterapeuta -> "FISIOTERAPEUTA"
        | Psicologo -> "PSICOLOGO"
        | Nutricionista -> "NUTRICIONISTA"
        | Farmaceutico -> "FARMACEUTICO"
        | Administrativo -> "ADMINISTRATIVO"

    let getAll (ativo: bool option) =
        async {
            let query = 
                match ativo with
                | Some true -> 
                    "SELECT id, nome, cpf, crm, tipo_profissional, email, telefone, 
                            data_admissao, data_demissao, ativo, unidade_id, permite_telemedicina,
                            data_cadastro, data_atualizacao 
                     FROM profissionais WHERE ativo = true ORDER BY nome"
                | Some false ->
                    "SELECT id, nome, cpf, crm, tipo_profissional, email, telefone, 
                            data_admissao, data_demissao, ativo, unidade_id, permite_telemedicina,
                            data_cadastro, data_atualizacao 
                     FROM profissionais WHERE ativo = false ORDER BY nome"
                | None ->
                    "SELECT id, nome, cpf, crm, tipo_profissional, email, telefone, 
                            data_admissao, data_demissao, ativo, unidade_id, permite_telemedicina,
                            data_cadastro, data_atualizacao 
                     FROM profissionais ORDER BY nome"

            return!
                DbConnection.getConnectionString ()
                |> Sql.connect
                |> Sql.query query
                |> Sql.executeAsync (fun read -> {
                    Id = read.int "id"
                    Nome = read.string "nome"
                    CPF = read.string "cpf"
                    CRM = read.stringOrNone "crm"
                    Especialidades = [] // Will be loaded separately if needed
                    TipoProfissional = parseTipoProfissional (read.string "tipo_profissional")
                    Email = read.string "email"
                    Telefone = read.string "telefone"
                    DataAdmissao = read.dateTime "data_admissao"
                    DataDemissao = read.dateTimeOrNone "data_demissao"
                    Ativo = read.bool "ativo"
                    UnidadeId = read.int "unidade_id"
                    PermiteTelemedicina = read.bool "permite_telemedicina"
                    DataCadastro = read.dateTime "data_cadastro"
                    DataAtualizacao = read.dateTimeOrNone "data_atualizacao"
                }) |> Async.AwaitTask
        }

    let getById (id: int) =
        task {
            return!
                DbConnection.getConnectionString ()
                |> Sql.connect
                |> Sql.query """
                    SELECT id, nome, cpf, crm, tipo_profissional, email, telefone, 
                           data_admissao, data_demissao, ativo, unidade_id, permite_telemedicina,
                           data_cadastro, data_atualizacao 
                    FROM profissionais 
                    WHERE id = @id
                """
                |> Sql.parameters ["id", Sql.int id]
                |> Sql.executeRowAsync (fun read -> {
                    Id = read.int "id"
                    Nome = read.string "nome"
                    CPF = read.string "cpf"
                    CRM = read.stringOrNone "crm"
                    Especialidades = [] // Load separately
                    TipoProfissional = parseTipoProfissional (read.string "tipo_profissional")
                    Email = read.string "email"
                    Telefone = read.string "telefone"
                    DataAdmissao = read.dateTime "data_admissao"
                    DataDemissao = read.dateTimeOrNone "data_demissao"
                    Ativo = read.bool "ativo"
                    UnidadeId = read.int "unidade_id"
                    PermiteTelemedicina = read.bool "permite_telemedicina"
                    DataCadastro = read.dateTime "data_cadastro"
                    DataAtualizacao = read.dateTimeOrNone "data_atualizacao"
                })
        }

    let insert (input: ProfissionalInput) =
        task {
            return!
                DbConnection.getConnectionString ()
                |> Sql.connect
                |> Sql.query """
                    INSERT INTO profissionais 
                    (nome, cpf, crm, tipo_profissional, email, telefone, data_admissao, 
                     unidade_id, permite_telemedicina, ativo)
                    VALUES 
                    (@nome, @cpf, @crm, @tipo_profissional, @email, @telefone, @data_admissao,
                     @unidade_id, @permite_telemedicina, @ativo)
                    RETURNING id
                """
                |> Sql.parameters [
                    "nome", Sql.string input.Nome
                    "cpf", Sql.string input.CPF
                    "crm", Sql.stringOrNone input.CRM
                    "tipo_profissional", Sql.string (tipoProfissionalToString input.TipoProfissional)
                    "email", Sql.string input.Email
                    "telefone", Sql.string input.Telefone
                    "data_admissao", Sql.timestamp input.DataAdmissao
                    "unidade_id", Sql.int input.UnidadeId
                    "permite_telemedicina", Sql.bool input.PermiteTelemedicina
                    "ativo", Sql.bool true
                ]
                |> Sql.executeRowAsync (fun read -> read.int "id")
        }

    let update (id: int) (input: ProfissionalInput) =
        task {
            let! rowsAffected =
                DbConnection.getConnectionString ()
                |> Sql.connect
                |> Sql.query """
                    UPDATE profissionais 
                    SET nome = @nome,
                        cpf = @cpf,
                        crm = @crm,
                        tipo_profissional = @tipo_profissional,
                        email = @email,
                        telefone = @telefone,
                        data_admissao = @data_admissao,
                        unidade_id = @unidade_id,
                        permite_telemedicina = @permite_telemedicina,
                        data_atualizacao = CURRENT_TIMESTAMP
                    WHERE id = @id AND ativo = true
                """
                |> Sql.parameters [
                    "id", Sql.int id
                    "nome", Sql.string input.Nome
                    "cpf", Sql.string input.CPF
                    "crm", Sql.stringOrNone input.CRM
                    "tipo_profissional", Sql.string (tipoProfissionalToString input.TipoProfissional)
                    "email", Sql.string input.Email
                    "telefone", Sql.string input.Telefone
                    "data_admissao", Sql.timestamp input.DataAdmissao
                    "unidade_id", Sql.int input.UnidadeId
                    "permite_telemedicina", Sql.bool input.PermiteTelemedicina
                ]
                |> Sql.executeNonQueryAsync
            
            return rowsAffected > 0
        }

    let deactivate (id: int) (dataDemissao: DateTime) =
        task {
            let! rowsAffected =
                DbConnection.getConnectionString ()
                |> Sql.connect
                |> Sql.query """
                    UPDATE profissionais 
                    SET ativo = false,
                        data_demissao = @data_demissao,
                        data_atualizacao = CURRENT_TIMESTAMP
                    WHERE id = @id AND ativo = true
                """
                |> Sql.parameters [
                    "id", Sql.int id
                    "data_demissao", Sql.timestamp dataDemissao
                ]
                |> Sql.executeNonQueryAsync
            
            return rowsAffected > 0
        }

    let getByUnidade (unidadeId: int) =
        task {
            return!
                DbConnection.getConnectionString ()
                |> Sql.connect
                |> Sql.query """
                    SELECT id, nome, cpf, crm, tipo_profissional, email, telefone, 
                           data_admissao, data_demissao, ativo, unidade_id, permite_telemedicina,
                           data_cadastro, data_atualizacao 
                    FROM profissionais 
                    WHERE unidade_id = @unidade_id AND ativo = true
                    ORDER BY nome
                """
                |> Sql.parameters ["unidade_id", Sql.int unidadeId]
                |> Sql.executeAsync (fun read -> {
                    Id = read.int "id"
                    Nome = read.string "nome"
                    CPF = read.string "cpf"
                    CRM = read.stringOrNone "crm"
                    Especialidades = []
                    TipoProfissional = parseTipoProfissional (read.string "tipo_profissional")
                    Email = read.string "email"
                    Telefone = read.string "telefone"
                    DataAdmissao = read.dateTime "data_admissao"
                    DataDemissao = read.dateTimeOrNone "data_demissao"
                    Ativo = read.bool "ativo"
                    UnidadeId = read.int "unidade_id"
                    PermiteTelemedicina = read.bool "permite_telemedicina"
                    DataCadastro = read.dateTime "data_cadastro"
                    DataAtualizacao = read.dateTimeOrNone "data_atualizacao"
                })
        }

    let getByTipo (tipoProfissional: TipoProfissional) =
        task {
            return!
                DbConnection.getConnectionString ()
                |> Sql.connect
                |> Sql.query """
                    SELECT id, nome, cpf, crm, tipo_profissional, email, telefone, 
                           data_admissao, data_demissao, ativo, unidade_id, permite_telemedicina,
                           data_cadastro, data_atualizacao 
                    FROM profissionais 
                    WHERE tipo_profissional = @tipo AND ativo = true
                    ORDER BY nome
                """
                |> Sql.parameters ["tipo", Sql.string (tipoProfissionalToString tipoProfissional)]
                |> Sql.executeAsync (fun read -> {
                    Id = read.int "id"
                    Nome = read.string "nome"
                    CPF = read.string "cpf"
                    CRM = read.stringOrNone "crm"
                    Especialidades = []
                    TipoProfissional = parseTipoProfissional (read.string "tipo_profissional")
                    Email = read.string "email"
                    Telefone = read.string "telefone"
                    DataAdmissao = read.dateTime "data_admissao"
                    DataDemissao = read.dateTimeOrNone "data_demissao"
                    Ativo = read.bool "ativo"
                    UnidadeId = read.int "unidade_id"
                    PermiteTelemedicina = read.bool "permite_telemedicina"
                    DataCadastro = read.dateTime "data_cadastro"
                    DataAtualizacao = read.dateTimeOrNone "data_atualizacao"
                })
        }
module Handler =
    open Giraffe
    open Microsoft.AspNetCore.Http
    open Models
    open System
    //open FSharp.Control.Tasks

    // DTOs para input/output
    type ProfissionalResponse = {
        Id: int
        Nome: string
        CPF: string
        CRM: string option
        TipoProfissional: string
        Email: string
        Telefone: string
        DataAdmissao: DateTime
        DataDemissao: DateTime option
        Ativo: bool
        UnidadeId: int
        PermiteTelemedicina: bool
        DataCadastro: DateTime
        DataAtualizacao: DateTime option
    }

    type ProfissionalInputDto = {
        Nome: string
        CPF: string
        CRM: string option
        TipoProfissional: string
        Email: string
        Telefone: string
        DataAdmissao: DateTime
        UnidadeId: int
        PermiteTelemedicina: bool
    }

    // Função auxiliar para converter Domain -> DTO
    let private toResponse (profissional: Profissional) : ProfissionalResponse =
        let tipoString = 
            match profissional.TipoProfissional with
            | Medico -> "MEDICO"
            | Enfermeiro -> "ENFERMEIRO"
            | Tecnico -> "TECNICO"
            | Fisioterapeuta -> "FISIOTERAPEUTA"
            | Psicologo -> "PSICOLOGO"
            | Nutricionista -> "NUTRICIONISTA"
            | Farmaceutico -> "FARMACEUTICO"
            | Administrativo -> "ADMINISTRATIVO"

        {
            Id = profissional.Id
            Nome = profissional.Nome
            CPF = profissional.CPF
            CRM = profissional.CRM
            TipoProfissional = tipoString
            Email = profissional.Email
            Telefone = profissional.Telefone
            DataAdmissao = profissional.DataAdmissao
            DataDemissao = profissional.DataDemissao
            Ativo = profissional.Ativo
            UnidadeId = profissional.UnidadeId
            PermiteTelemedicina = profissional.PermiteTelemedicina
            DataCadastro = profissional.DataCadastro
            DataAtualizacao = profissional.DataAtualizacao
        }

    // Função auxiliar para converter DTO -> Domain
    let private toDomainInput (dto: ProfissionalInputDto) : ProfissionalInput =
        let tipo = 
            match dto.TipoProfissional.ToUpper() with
            | "MEDICO" -> Medico
            | "ENFERMEIRO" -> Enfermeiro
            | "TECNICO" -> Tecnico
            | "FISIOTERAPEUTA" -> Fisioterapeuta
            | "PSICOLOGO" -> Psicologo
            | "NUTRICIONISTA" -> Nutricionista
            | "FARMACEUTICO" -> Farmaceutico
            | "ADMINISTRATIVO" -> Administrativo
            | _ -> failwith $"Tipo de profissional inválido: {dto.TipoProfissional}"

        {
            Nome = dto.Nome
            CPF = dto.CPF
            CRM = dto.CRM
            TipoProfissional = tipo
            Email = dto.Email
            Telefone = dto.Telefone
            DataAdmissao = dto.DataAdmissao
            UnidadeId = dto.UnidadeId
            PermiteTelemedicina = dto.PermiteTelemedicina
        }

    // Validações
    let private validateCPF (cpf: string) =
        // Implementar validação de CPF
        cpf.Length = 11 && cpf |> Seq.forall System.Char.IsDigit

    let private validateEmail (email: string) =
        email.Contains("@") && email.Contains(".")

    let private validateInput (dto: ProfissionalInputDto) =
        let errors = ResizeArray<string>()
        
        if String.IsNullOrWhiteSpace(dto.Nome) then
            errors.Add("Nome é obrigatório")
        
        if String.IsNullOrWhiteSpace(dto.CPF) || not (validateCPF dto.CPF) then
            errors.Add("CPF inválido")
        
        if String.IsNullOrWhiteSpace(dto.Email) || not (validateEmail dto.Email) then
            errors.Add("Email inválido")
        
        if String.IsNullOrWhiteSpace(dto.Telefone) then
            errors.Add("Telefone é obrigatório")
        
        if dto.DataAdmissao > DateTime.Now then
            errors.Add("Data de admissão não pode ser no futuro")
        
        if dto.UnidadeId <= 0 then
            errors.Add("Unidade deve ser especificada")

        errors |> Seq.toList
    
    [<OpenApiOperation("Get All Profissionals", Description = "Lista todos os profissionais")>]
    let getAllProfissionais : HttpHandler =
        fun next ctx ->
            task {
                try
                    // Verificar query parameters
                    let ativo = 
                        match ctx.TryGetQueryStringValue "ativo" with
                        | Some "true" -> Some true
                        | Some "false" -> Some false
                        | _ -> None
                    logger.Information("Recebeu requisição para listar profissionais. paramêtros:{params}",ativo)
                    let! profissionais = Repository.getAll ativo
                    let response = profissionais |> List.map toResponse
                    
                    return! json response next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro interno do servidor"; details = ex.ToString() |}
                    logger.Error("Erro interno do servidor: {ex}",ex)
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }
    [<OpenApiOperation("Get Profissional Por Id", Description = "Consulta um profissional por Id")>]
    let getProfissionalById profissionalId : HttpHandler =
        fun next ctx ->
            task {
                try
                    logger.Information("Recebeu requisição para consultar profissional. paramêtros:{params}",{|ProfissionalId = profissionalId|})
                    let! profissional = Repository.getById profissionalId
                    let response = toResponse profissional
                    
                    return! json response next ctx
                with
                | :? System.InvalidOperationException ->
                    let errorResponse = {| error = "Profissional não encontrado" |}                    
                    return! (setStatusCode 404 >=> json errorResponse) next ctx
                | ex ->
                    let errorResponse = {| error = "Erro interno do servidor"; details = ex.Message |}
                    logger.Error("Erro interno do servidor: {ex}",ex)
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }
    [<OpenApiOperation("Create Profissional", Description = "Salva os dados de um profissional no sistema")>]
    let createProfissional : HttpHandler =
        fun next ctx ->
            task {
                try
                    let! inputDto = ctx.BindJsonAsync<ProfissionalInputDto>()
                    logger.Information("Recebeu requisição para criar profissional. paramêtros:{params}",inputDto)
                    let validationErrors = validateInput inputDto
                    if not validationErrors.IsEmpty then
                        let errorResponse = {| errors = validationErrors |}
                        logger.Information("Recebeu requisição com erros de validação para criar profissional. paramêtros:{params}, erros = {errorResponse}",inputDto,errorResponse)
                        return! (setStatusCode 400 >=> json errorResponse) next ctx
                    else
                        let domainInput = toDomainInput inputDto
                        logger.Information("Requisição para criar profissional foi validada. paramêtros:{params}, domainInput = {domainInput}",inputDto,domainInput)
                        let! id = Repository.insert domainInput
                        let response = {| id = id; message = "Profissional criado com sucesso" |}
                        logger.Information("Profissional criado. paramêtros:{params}, domainInput = {domainInput}",inputDto,domainInput)
                        return! (setStatusCode 201 >=> json response) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao criar profissional"; details = ex.Message |}
                    logger.Error("Erro interno do servidor: {ex}",ex)
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }
    [<OpenApiOperation("Update Profissional", Description = "Atualiza os dados de um profissional")>]
    // [<OpenApiResponse(typeof<>, Description = "Echo result")>]
    let updateProfissional profissionalId : HttpHandler =
        fun next ctx ->
            task {
                try                    
                    let! inputDto = ctx.BindJsonAsync<ProfissionalInputDto>()
                    logger.Information("Recebeu requisição para atualizar profissional. paramêtros:{params}",inputDto)
                    let validationErrors = validateInput inputDto
                    if not validationErrors.IsEmpty then
                        let errorResponse = {| errors = validationErrors |}
                        logger.Information("Recebeu requisição com erros de validação para atualizar profissional. paramêtros:{params}, erros = {errorResponse}",inputDto,errorResponse)
                        return! (setStatusCode 400 >=> json errorResponse) next ctx
                    else
                        let domainInput = toDomainInput inputDto
                        let! success = Repository.update profissionalId domainInput
                        
                        if success then
                            let response = {| message = "Profissional atualizado com sucesso" |}
                            return! json response next ctx
                        else
                            let errorResponse = {| error = "Profissional não encontrado ou inativo" |}
                            return! (setStatusCode 404 >=> json errorResponse) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao atualizar profissional"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }
    [<OpenApiOperation("Desativar Profissional", Description = "Desativa um profissional")>]
    let deactivateProfissional dataDeEmissao profissionalId : HttpHandler =
        fun next ctx ->
            task {
                try                                        
                    let! success = Repository.deactivate profissionalId dataDeEmissao
                    
                    if success then
                        let response = {| message = "Profissional desativado com sucesso" |}
                        return! json response next ctx
                    else
                        let errorResponse = {| error = "Profissional não encontrado ou já inativo" |}
                        return! (setStatusCode 404 >=> json errorResponse) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao desativar profissional"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let getProfissionaisByUnidade unidadeId : HttpHandler =
        fun next ctx ->
            task {
                try
                    let! profissionais = Repository.getByUnidade unidadeId
                    let response = profissionais |> List.map toResponse
                    
                    return! json response next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro interno do servidor"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let getProfissionaisByTipo (tipoString:string) : HttpHandler =
        fun next ctx ->
            task {
                try                    
                    let tipo = 
                        match tipoString.ToUpper() with
                        | "MEDICO" -> Medico
                        | "ENFERMEIRO" -> Enfermeiro
                        | "TECNICO" -> Tecnico
                        | "FISIOTERAPEUTA" -> Fisioterapeuta
                        | "PSICOLOGO" -> Psicologo
                        | "NUTRICIONISTA" -> Nutricionista
                        | "FARMACEUTICO" -> Farmaceutico
                        | "ADMINISTRATIVO" -> Administrativo
                        | _ -> failwith "Tipo inválido"
                    
                    let! profissionais = Repository.getByTipo tipo
                    let response = profissionais |> List.map toResponse
                    
                    return! json response next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Tipo de profissional inválido ou erro interno"; details = ex.Message |}
                    return! (setStatusCode 400 >=> json errorResponse) next ctx
            }

    // Rotas Profissional
    let routes : HttpHandler =
        choose [
            GET >=> choose [
                route "" >=> getAllProfissionais
                routef "/%i" getProfissionalById
                routef "/unidade/%i" getProfissionaisByUnidade
                routef "/tipo/%s" getProfissionaisByTipo
            ]
            POST >=> route "" >=> createProfissional
            PUT >=> routef "/%i" updateProfissional
            DELETE >=> routef "/%i" (fun i -> deactivateProfissional DateTime.Now i)
        ]
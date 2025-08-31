namespace Domains.Paciente

open System
open System.Threading.Tasks
open Infrastructure.Database
open SGHSS.Api.Logging

module Models =
    type TipoDocumento = | CPF | RG | CNH | Passaporte
    
    type Endereco = {
        Id: int
        Logradouro: string
        Numero: string
        Complemento: string option
        Bairro: string
        Cidade: string
        Estado: string
        CEP: string
        Pais: string
    }
    
    type ContatoEmergencia = {
        Id: int
        PacienteId: int
        Nome: string
        Parentesco: string
        Telefone: string
        Email: string option
    }
    
    type Paciente = {
        Id: int
        Nome: string
        CPF: string
        RG: string option
        DataNascimento: DateTime
        Sexo: string
        EstadoCivil: string option
        Profissao: string option
        Email: string option
        Telefone: string
        TelefoneSecundario: string option
        Endereco: Endereco option
        PlanoSaude: string option
        NumeroCarteirinha: string option
        Observacoes: string option
        DataCadastro: DateTime
        DataAtualizacao: DateTime option
        Ativo: bool
        ContatosEmergencia: ContatoEmergencia list
    }
    
    // Input Types
    type EnderecoInput = {
        Logradouro: string
        Numero: string
        Complemento: string option
        Bairro: string
        Cidade: string
        Estado: string
        CEP: string
        Pais: string option
    }
    
    type ContatoEmergenciaInput = {
        Nome: string
        Parentesco: string
        Telefone: string
        Email: string option
    }
    
    type PacienteInput = {
        Nome: string
        CPF: string
        RG: string option
        DataNascimento: DateTime
        Sexo: string
        EstadoCivil: string option
        Profissao: string option
        Email: string option
        Telefone: string
        TelefoneSecundario: string option
        Endereco: EnderecoInput option
        PlanoSaude: string option
        NumeroCarteirinha: string option
        Observacoes: string option
        ContatosEmergencia: ContatoEmergenciaInput list
    }

    // Views for detailed information
    type PacienteDetalhes = {
        Id: int
        Nome: string
        CPF: string
        RG: string option
        DataNascimento: DateTime
        Idade: int
        Sexo: string
        EstadoCivil: string option
        Profissao: string option
        Email: string option
        Telefone: string
        TelefoneSecundario: string option
        Endereco: Endereco option
        PlanoSaude: string option
        NumeroCarteirinha: string option
        Observacoes: string option
        ContatosEmergencia: ContatoEmergencia list
        EstatisticasAtendimento: {|
            TotalConsultas: int
            UltimaConsulta: DateTime option
            ProximaConsulta: DateTime option
            ConsultasAgendadas: int
            PrescricoesAtivas: int
            ExamesPendentes: int
        |}
        DataCadastro: DateTime
        DataAtualizacao: DateTime option
        Ativo: bool
    }

    type HistoricoMedico = {
        PacienteId: int
        PacienteNome: string
        TotalProntuarios: int
        UltimoAtendimento: DateTime option
        DiagnosticosMaisFrequentes: string list
        MedicamentosAtuais: string list
        AlergiasMedicamentosas: string list
        ExamesRecentes: {| Tipo: string; Data: DateTime; Resultado: string |} list
        InternacoesPrevias: int
        ObservacoesCriticas: string list
    }

// Paciente/Repository.fs
module Repository =
    open Npgsql.FSharp
    open Models
    open System   
    
    // Validation functions
    let private validarCPF (cpf: string) =
        // Remove non-numeric characters
        let cpfLimpo = cpf.Replace(".", "").Replace("-", "").Replace(" ", "")
        
        // Check if has 11 digits and is not a known invalid pattern
        if cpfLimpo.Length <> 11 then false
        elif cpfLimpo |> Seq.forall (fun c -> c = cpfLimpo.[0]) then false // All same digits
        else
            // CPF validation algorithm
            let calcularDigito (cpf: string) (peso: int list) =
                let soma = 
                    cpf.ToCharArray()
                    |> Array.take (peso.Length)
                    |> Array.mapi (fun i c -> (int c - int '0') * peso.[i])
                    |> Array.sum
                let resto = soma % 11
                if resto < 2 then 0 else 11 - resto
            
            let peso1 = [10; 9; 8; 7; 6; 5; 4; 3; 2]
            let peso2 = [11; 10; 9; 8; 7; 6; 5; 4; 3; 2]
            
            let digito1 = calcularDigito cpfLimpo peso1
            let digito2 = calcularDigito cpfLimpo peso2
            
            let digitoInformado1 = int cpfLimpo.[9] - int '0'
            let digitoInformado2 = int cpfLimpo.[10] - int '0'
            
            digito1 = digitoInformado1 && digito2 = digitoInformado2

    // Repository functions for Endereco
    let private insertEndereco (endereco: EnderecoInput) =
        task {
            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    INSERT INTO enderecos 
                    (logradouro, numero, complemento, bairro, cidade, estado, cep, pais)
                    VALUES 
                    (@logradouro, @numero, @complemento, @bairro, @cidade, @estado, @cep, @pais)
                    RETURNING id
                """
                |> Sql.parameters [
                    "logradouro", Sql.string endereco.Logradouro
                    "numero", Sql.string endereco.Numero
                    "complemento", Sql.stringOrNone endereco.Complemento
                    "bairro", Sql.string endereco.Bairro
                    "cidade", Sql.string endereco.Cidade
                    "estado", Sql.string endereco.Estado
                    "cep", Sql.string endereco.CEP
                    "pais", Sql.string (endereco.Pais |> Option.defaultValue "Brasil")
                ]
                |> Sql.executeRowAsync (fun read -> read.int "id")
        }

    let private getEnderecoById (id: int) =
        task {
            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT id, logradouro, numero, complemento, bairro, cidade, estado, cep, pais
                    FROM enderecos 
                    WHERE id = @id
                """
                |> Sql.parameters ["id", Sql.int id]
                |> Sql.executeRowAsync (fun read -> {
                    Id = read.int "id"
                    Logradouro = read.string "logradouro"
                    Numero = read.string "numero"
                    Complemento = read.stringOrNone "complemento"
                    Bairro = read.string "bairro"
                    Cidade = read.string "cidade"
                    Estado = read.string "estado"
                    CEP = read.string "cep"
                    Pais = read.string "pais"
                })
        }

    let private updateEndereco (id: int) (endereco: EnderecoInput) =
        task {
            let! rowsAffected =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    UPDATE enderecos 
                    SET logradouro = @logradouro,
                        numero = @numero,
                        complemento = @complemento,
                        bairro = @bairro,
                        cidade = @cidade,
                        estado = @estado,
                        cep = @cep,
                        pais = @pais
                    WHERE id = @id
                """
                |> Sql.parameters [
                    "id", Sql.int id
                    "logradouro", Sql.string endereco.Logradouro
                    "numero", Sql.string endereco.Numero
                    "complemento", Sql.stringOrNone endereco.Complemento
                    "bairro", Sql.string endereco.Bairro
                    "cidade", Sql.string endereco.Cidade
                    "estado", Sql.string endereco.Estado
                    "cep", Sql.string endereco.CEP
                    "pais", Sql.string (endereco.Pais |> Option.defaultValue "Brasil")
                ]
                |> Sql.executeNonQueryAsync
            
            return rowsAffected > 0
        }

    // Repository functions for ContatoEmergencia
    let private getContatosEmergencia (pacienteId: int) =
        task {
            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT id, paciente_id, nome, parentesco, telefone, email
                    FROM contatos_emergencia 
                    WHERE paciente_id = @paciente_id
                    ORDER BY nome
                """
                |> Sql.parameters ["paciente_id", Sql.int pacienteId]
                |> Sql.executeAsync (fun read -> {
                    Id = read.int "id"
                    PacienteId = read.int "paciente_id"
                    Nome = read.string "nome"
                    Parentesco = read.string "parentesco"
                    Telefone = read.string "telefone"
                    Email = read.stringOrNone "email"
                })
        }

    let private insertContatoEmergencia (pacienteId: int) (contato: ContatoEmergenciaInput) =
        task {
            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    INSERT INTO contatos_emergencia 
                    (paciente_id, nome, parentesco, telefone, email)
                    VALUES 
                    (@paciente_id, @nome, @parentesco, @telefone, @email)
                    RETURNING id
                """
                |> Sql.parameters [
                    "paciente_id", Sql.int pacienteId
                    "nome", Sql.string contato.Nome
                    "parentesco", Sql.string contato.Parentesco
                    "telefone", Sql.string contato.Telefone
                    "email", Sql.stringOrNone contato.Email
                ]
                |> Sql.executeRowAsync (fun read -> read.int "id")
        }

    let private deleteContatosEmergencia (pacienteId: int) =
        task {
            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    DELETE FROM contatos_emergencia WHERE paciente_id = @paciente_id
                """
                |> Sql.parameters ["paciente_id", Sql.int pacienteId]
                |> Sql.executeNonQueryAsync
        }

    // Main Repository functions for Paciente
    let getAll (ativo: bool option) (termo: string option) (unidadeId: int option) =
        task {
            let mutable query = """
                SELECT p.id, p.nome, p.cpf, p.rg, p.data_nascimento, p.sexo, 
                       p.estado_civil, p.profissao, p.email, p.telefone, p.telefone_secundario,
                       p.endereco_id, p.plano_saude, p.numero_carteirinha, p.observacoes,
                       p.data_cadastro, p.data_atualizacao, p.ativo
                FROM pacientes p
                WHERE 1=1
            """
            let mutable parameters = []

            match ativo with
            | Some true ->
                query <- query + " AND p.ativo = true"
            | Some false ->
                query <- query + " AND p.ativo = false"
            | None -> ()

            match termo with
            | Some t when not (String.IsNullOrWhiteSpace(t)) ->
                let termoLike = System.String.Format("%{0}%",t)
                query <- query + " AND (p.nome ILIKE @termo OR p.cpf ILIKE @termo)"
                parameters <- ("termo", Sql.string termoLike) :: parameters
            | _ -> ()

            // Filter by unidade through appointments or medical records
            match unidadeId with
            | Some uid ->
                query <- query + """ 
                    AND EXISTS (
                        SELECT 1 FROM agendamentos a 
                        WHERE a.paciente_id = p.id AND a.unidade_id = @unidade_id
                        UNION
                        SELECT 1 FROM prontuarios pr 
                        WHERE pr.paciente_id = p.id AND pr.unidade_id = @unidade_id
                    )
                """
                parameters <- ("unidade_id", Sql.int uid) :: parameters
            | None -> ()

            query <- query + " ORDER BY p.nome"

            let! pacientes =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query query
                |> Sql.parameters parameters
                |> Sql.executeAsync (fun read -> 
                    let enderecoId = read.intOrNone "endereco_id"
                    {
                        Id = read.int "id"
                        Nome = read.string "nome"
                        CPF = read.string "cpf"
                        RG = read.stringOrNone "rg"
                        DataNascimento = read.dateTime "data_nascimento"
                        Sexo = read.string "sexo"
                        EstadoCivil = read.stringOrNone "estado_civil"
                        Profissao = read.stringOrNone "profissao"
                        Email = read.stringOrNone "email"
                        Telefone = read.string "telefone"
                        TelefoneSecundario = read.stringOrNone "telefone_secundario"
                        Endereco = None // Will be loaded separately
                        PlanoSaude = read.stringOrNone "plano_saude"
                        NumeroCarteirinha = read.stringOrNone "numero_carteirinha"
                        Observacoes = read.stringOrNone "observacoes"
                        DataCadastro = read.dateTime "data_cadastro"
                        DataAtualizacao = read.dateTimeOrNone "data_atualizacao"
                        Ativo = read.bool "ativo"
                        ContatosEmergencia = [] // Will be loaded separately
                    })

            // Load addresses and contacts for each patient (if needed - for performance, might want to do this only for detailed views)
            let! pacientesCompletos = 
                pacientes
                |> List.map (fun p -> 
                    task {
                        let! endereco = 
                            match p.Endereco with
                            | None -> 
                                // Try to get endereco_id from the original query result
                                task { return None }
                            | Some _ -> task { return p.Endereco }

                        let! contatos = getContatosEmergencia p.Id
                        
                        return { p with 
                                    Endereco = endereco
                                    ContatosEmergencia = contatos }
                    })
                |> Task.WhenAll

            return pacientesCompletos |> Array.toList
        }

    let getById (id: int) =
        task {
            let! paciente =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT p.id, p.nome, p.cpf, p.rg, p.data_nascimento, p.sexo, 
                           p.estado_civil, p.profissao, p.email, p.telefone, p.telefone_secundario,
                           p.endereco_id, p.plano_saude, p.numero_carteirinha, p.observacoes,
                           p.data_cadastro, p.data_atualizacao, p.ativo
                    FROM pacientes p
                    WHERE p.id = @id
                """
                |> Sql.parameters ["id", Sql.int id]
                |> Sql.executeRowAsync (fun read -> 
                    {
                        Id = read.int "id"
                        Nome = read.string "nome"
                        CPF = read.string "cpf"
                        RG = read.stringOrNone "rg"
                        DataNascimento = read.dateTime "data_nascimento"
                        Sexo = read.string "sexo"
                        EstadoCivil = read.stringOrNone "estado_civil"
                        Profissao = read.stringOrNone "profissao"
                        Email = read.stringOrNone "email"
                        Telefone = read.string "telefone"
                        TelefoneSecundario = read.stringOrNone "telefone_secundario"
                        Endereco = None // Will be loaded separately
                        PlanoSaude = read.stringOrNone "plano_saude"
                        NumeroCarteirinha = read.stringOrNone "numero_carteirinha"
                        Observacoes = read.stringOrNone "observacoes"
                        DataCadastro = read.dateTime "data_cadastro"
                        DataAtualizacao = read.dateTimeOrNone "data_atualizacao"
                        Ativo = read.bool "ativo"
                        ContatosEmergencia = []
                    }, read.intOrNone "endereco_id")

            let (pacienteBase, enderecoId) = paciente

            // Load endereco if exists
            let! endereco = 
                match enderecoId with
                | Some eId -> 
                    task {
                        let! addr = getEnderecoById eId
                        return Some addr
                    }
                | None -> task { return None }

            // Load emergency contacts
            let! contatos = getContatosEmergencia pacienteBase.Id

            return { pacienteBase with 
                        Endereco = endereco
                        ContatosEmergencia = contatos }
        }

    let getPacienteDetalhes (id: int) =
        task {
            // Get basic patient data
            let! paciente = getById id

            // Get medical statistics
            let! estatisticas =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT 
                        COUNT(DISTINCT pr.id) as total_consultas,
                        MAX(pr.data_atendimento) as ultima_consulta,
                        MIN(a.data_hora) FILTER (WHERE a.data_hora > CURRENT_TIMESTAMP AND a.status = 'AGENDADO') as proxima_consulta,
                        COUNT(DISTINCT a.id) FILTER (WHERE a.data_hora > CURRENT_TIMESTAMP AND a.status IN ('AGENDADO', 'CONFIRMADO')) as consultas_agendadas,
                        COUNT(DISTINCT pre.id) FILTER (WHERE pre.ativo = true AND (pre.data_vencimento IS NULL OR pre.data_vencimento > CURRENT_TIMESTAMP)) as prescricoes_ativas,
                        COUNT(DISTINCT ex.id) FILTER (WHERE ex.realizado = false) as exames_pendentes
                    FROM pacientes p
                    LEFT JOIN prontuarios pr ON p.id = pr.paciente_id
                    LEFT JOIN agendamentos a ON p.id = a.paciente_id
                    LEFT JOIN prescricoes pre ON pr.id = pre.prontuario_id
                    LEFT JOIN exames_solicitados ex ON pr.id = ex.prontuario_id
                    WHERE p.id = @id
                    GROUP BY p.id
                """
                |> Sql.parameters ["id", Sql.int id]
                |> Sql.executeRowAsync (fun read -> {|
                    TotalConsultas = read.int "total_consultas"
                    UltimaConsulta = read.dateTimeOrNone "ultima_consulta"
                    ProximaConsulta = read.dateTimeOrNone "proxima_consulta"
                    ConsultasAgendadas = read.int "consultas_agendadas"
                    PrescricoesAtivas = read.int "prescricoes_ativas"
                    ExamesPendentes = read.int "exames_pendentes"
                |})

            let idade = DateTime.Now.Year - paciente.DataNascimento.Year - (if DateTime.Now.DayOfYear < paciente.DataNascimento.DayOfYear then 1 else 0)

            return {
                Id = paciente.Id
                Nome = paciente.Nome
                CPF = paciente.CPF
                RG = paciente.RG
                DataNascimento = paciente.DataNascimento
                Idade = idade
                Sexo = paciente.Sexo
                EstadoCivil = paciente.EstadoCivil
                Profissao = paciente.Profissao
                Email = paciente.Email
                Telefone = paciente.Telefone
                TelefoneSecundario = paciente.TelefoneSecundario
                Endereco = paciente.Endereco
                PlanoSaude = paciente.PlanoSaude
                NumeroCarteirinha = paciente.NumeroCarteirinha
                Observacoes = paciente.Observacoes
                ContatosEmergencia = paciente.ContatosEmergencia
                EstatisticasAtendimento = estatisticas
                DataCadastro = paciente.DataCadastro
                DataAtualizacao = paciente.DataAtualizacao
                Ativo = paciente.Ativo
            }
        }

    let insert (input: PacienteInput) =
        task {
            // Validate CPF
            if not (validarCPF input.CPF) then
                return Error "CPF inválido"
            else
                // Check if CPF already exists
                let! cpfExists =
                    DbConnection.getConnectionString()
                    |> Sql.connect
                    |> Sql.query "SELECT COUNT(*) FROM pacientes WHERE cpf = @cpf"
                    |> Sql.parameters ["cpf", Sql.string input.CPF]
                    |> Sql.executeRowAsync (fun read -> read.int "count" > 0)

                if cpfExists then
                    return Error "CPF já está cadastrado no sistema"
                else
                    // Insert endereco if provided
                    let! enderecoId = 
                        match input.Endereco with
                        | Some endereco -> 
                            task {
                                let! id = insertEndereco endereco
                                return Some id
                            }
                        | None -> task { return None }

                    // Insert patient
                    let! pacienteId =
                        DbConnection.getConnectionString()
                        |> Sql.connect
                        |> Sql.query """
                            INSERT INTO pacientes 
                            (nome, cpf, rg, data_nascimento, sexo, estado_civil, profissao, email, 
                             telefone, telefone_secundario, endereco_id, plano_saude, numero_carteirinha, observacoes)
                            VALUES 
                            (@nome, @cpf, @rg, @data_nascimento, @sexo, @estado_civil, @profissao, @email,
                             @telefone, @telefone_secundario, @endereco_id, @plano_saude, @numero_carteirinha, @observacoes)
                            RETURNING id
                        """
                        |> Sql.parameters [
                            "nome", Sql.string input.Nome
                            "cpf", Sql.string input.CPF
                            "rg", Sql.stringOrNone input.RG
                            "data_nascimento", Sql.timestamp input.DataNascimento
                            "sexo", Sql.string input.Sexo
                            "estado_civil", Sql.stringOrNone input.EstadoCivil
                            "profissao", Sql.stringOrNone input.Profissao
                            "email", Sql.stringOrNone input.Email
                            "telefone", Sql.string input.Telefone
                            "telefone_secundario", Sql.stringOrNone input.TelefoneSecundario
                            "endereco_id", Sql.intOrNone enderecoId
                            "plano_saude", Sql.stringOrNone input.PlanoSaude
                            "numero_carteirinha", Sql.stringOrNone input.NumeroCarteirinha
                            "observacoes", Sql.stringOrNone input.Observacoes
                        ]
                        |> Sql.executeRowAsync (fun read -> read.int "id")

                    // Insert emergency contacts
                    for contato in input.ContatosEmergencia do
                        let! _ = insertContatoEmergencia pacienteId contato
                        ()

                    return Ok pacienteId
        }

    let update (id: int) (input: PacienteInput) =
        task {
            // Validate CPF
            if not (validarCPF input.CPF) then
                return Error "CPF inválido"
            else
                // Check if CPF already exists for another patient
                let! cpfExists =
                    DbConnection.getConnectionString()
                    |> Sql.connect
                    |> Sql.query "SELECT COUNT(*) FROM pacientes WHERE cpf = @cpf AND id != @id"
                    |> Sql.parameters ["cpf", Sql.string input.CPF; "id", Sql.int id]
                    |> Sql.executeRowAsync (fun read -> read.int "count" > 0)

                if cpfExists then
                    return Error "CPF já está cadastrado para outro paciente"
                else
                    // Get current patient data to check if endereco exists
                    let! currentPatient = getById id
                    
                    // Handle endereco update/insert
                    let! enderecoId = 
                        match input.Endereco, currentPatient.Endereco with
                        | Some novoEndereco, Some enderecoExistente ->
                            // Update existing endereco
                            task {
                                let! success = updateEndereco enderecoExistente.Id novoEndereco
                                return if success then Some enderecoExistente.Id else None
                            }
                        | Some novoEndereco, None ->
                            // Insert new endereco
                            task {
                                let! id = insertEndereco novoEndereco
                                return Some id
                            }
                        | None, Some _ ->
                            // Remove endereco reference (keep endereco in DB for audit)
                            task { return None }
                        | None, None ->
                            task { return None }

                    // Update patient
                    let! rowsAffected =
                        DbConnection.getConnectionString()
                        |> Sql.connect
                        |> Sql.query """
                            UPDATE pacientes 
                            SET nome = @nome,
                                cpf = @cpf,
                                rg = @rg,
                                data_nascimento = @data_nascimento,
                                sexo = @sexo,
                                estado_civil = @estado_civil,
                                profissao = @profissao,
                                email = @email,
                                telefone = @telefone,
                                telefone_secundario = @telefone_secundario,
                                endereco_id = @endereco_id,
                                plano_saude = @plano_saude,
                                numero_carteirinha = @numero_carteirinha,
                                observacoes = @observacoes,
                                data_atualizacao = CURRENT_TIMESTAMP
                            WHERE id = @id
                        """
                        |> Sql.parameters [
                            "id", Sql.int id
                            "nome", Sql.string input.Nome
                            "cpf", Sql.string input.CPF
                            "rg", Sql.stringOrNone input.RG
                            "data_nascimento", Sql.timestamp input.DataNascimento
                            "sexo", Sql.string input.Sexo
                            "estado_civil", Sql.stringOrNone input.EstadoCivil
                            "profissao", Sql.stringOrNone input.Profissao
                            "email", Sql.stringOrNone input.Email
                            "telefone", Sql.string input.Telefone
                            "telefone_secundario", Sql.stringOrNone input.TelefoneSecundario
                            "endereco_id", Sql.intOrNone enderecoId
                            "plano_saude", Sql.stringOrNone input.PlanoSaude
                            "numero_carteirinha", Sql.stringOrNone input.NumeroCarteirinha
                            "observacoes", Sql.stringOrNone input.Observacoes
                        ]
                        |> Sql.executeNonQueryAsync

                    if rowsAffected > 0 then
                        // Update emergency contacts - delete all and insert new ones
                        let! _ = deleteContatosEmergencia id
                        
                        for contato in input.ContatosEmergencia do
                            let! _ = insertContatoEmergencia id contato
                            ()

                        return Ok true
                    else
                        return Error "Paciente não encontrado"
        }

    let deactivate (id: int) =
        task {
            let! rowsAffected =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    UPDATE pacientes 
                    SET ativo = false,
                        data_atualizacao = CURRENT_TIMESTAMP
                    WHERE id = @id
                """
                |> Sql.parameters ["id", Sql.int id]
                |> Sql.executeNonQueryAsync
            
            return rowsAffected > 0
        }

    let reactivate (id: int) =
        task {
            let! rowsAffected =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    UPDATE pacientes 
                    SET ativo = true,
                        data_atualizacao = CURRENT_TIMESTAMP
                    WHERE id = @id
                """
                |> Sql.parameters ["id", Sql.int id]
                |> Sql.executeNonQueryAsync
            
            return rowsAffected > 0
        }

    let getByCPF (cpf: string) =
        task {
            try
                let! paciente =
                    DbConnection.getConnectionString()
                    |> Sql.connect
                    |> Sql.query """
                        SELECT p.id, p.nome, p.cpf, p.rg, p.data_nascimento, p.sexo, 
                               p.estado_civil, p.profissao, p.email, p.telefone, p.telefone_secundario,
                               p.endereco_id, p.plano_saude, p.numero_carteirinha, p.observacoes,
                               p.data_cadastro, p.data_atualizacao, p.ativo
                        FROM pacientes p
                        WHERE p.cpf = @cpf AND p.ativo = true
                    """
                    |> Sql.parameters ["cpf", Sql.string cpf]
                    |> Sql.executeRowAsync (fun read -> 
                        {
                            Id = read.int "id"
                            Nome = read.string "nome"
                            CPF = read.string "cpf"
                            RG = read.stringOrNone "rg"
                            DataNascimento = read.dateTime "data_nascimento"
                            Sexo = read.string "sexo"
                            EstadoCivil = read.stringOrNone "estado_civil"
                            Profissao = read.stringOrNone "profissao"
                            Email = read.stringOrNone "email"
                            Telefone = read.string "telefone"
                            TelefoneSecundario = read.stringOrNone "telefone_secundario"
                            Endereco = None
                            PlanoSaude = read.stringOrNone "plano_saude"
                            NumeroCarteirinha = read.stringOrNone "numero_carteirinha"
                            Observacoes = read.stringOrNone "observacoes"
                            DataCadastro = read.dateTime "data_cadastro"
                            DataAtualizacao = read.dateTimeOrNone "data_atualizacao"
                            Ativo = read.bool "ativo"
                            ContatosEmergencia = []
                        }, read.intOrNone "endereco_id")

                let (pacienteBase, enderecoId) = paciente

                // Load endereco if exists
                let! endereco = 
                    match enderecoId with
                    | Some eId -> 
                        task {
                            let! addr = getEnderecoById eId
                            return Some addr
                        }
                    | None -> task { return None }

                // Load emergency contacts
                let! contatos = getContatosEmergencia pacienteBase.Id

                return Some { pacienteBase with 
                                Endereco = endereco
                                ContatosEmergencia = contatos }
            with
            | :? System.InvalidOperationException ->
                return None
        }

    let getHistoricoMedico (id: int) =
        task {
            let! paciente = getById id

            // Get medical history summary
            let! historico =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT 
                        COUNT(DISTINCT pr.id) as total_prontuarios,
                        MAX(pr.data_atendimento) as ultimo_atendimento,
                        STRING_AGG(DISTINCT pr.cid10, ';') FILTER (WHERE pr.cid10 IS NOT NULL) as diagnosticos,
                        STRING_AGG(DISTINCT pre.medicamento, ';') FILTER (WHERE pre.ativo = true) as medicamentos_atuais,
                        COUNT(DISTINCT i.id) as internacoes_previas
                    FROM pacientes p
                    LEFT JOIN prontuarios pr ON p.id = pr.paciente_id
                    LEFT JOIN prescricoes pre ON pr.id = pre.prontuario_id
                    LEFT JOIN internacoes i ON p.id = i.paciente_id
                    WHERE p.id = @id
                    GROUP BY p.id
                """
                |> Sql.parameters ["id", Sql.int id]
                |> Sql.executeRowAsync (fun read -> 
                    let diagnosticosStr = read.stringOrNone "diagnosticos" |> Option.defaultValue ""
                    let medicamentosStr = read.stringOrNone "medicamentos_atuais" |> Option.defaultValue ""
                    
                    {|
                        TotalProntuarios = read.int "total_prontuarios"
                        UltimoAtendimento = read.dateTimeOrNone "ultimo_atendimento"
                        Diagnosticos = if String.IsNullOrWhiteSpace(diagnosticosStr) then [] else diagnosticosStr.Split(';') |> Array.toList |> List.filter (fun d -> not (String.IsNullOrWhiteSpace(d)))
                        MedicamentosAtuais = if String.IsNullOrWhiteSpace(medicamentosStr) then [] else medicamentosStr.Split(';') |> Array.toList |> List.filter (fun m -> not (String.IsNullOrWhiteSpace(m)))
                        InternacoesPrevias = read.int "internacoes_previas"
                    |})

            // Get recent exams
            let! examesRecentes =
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT e.tipo_exame, e.data_realizacao, e.resultado
                    FROM exames_solicitados e
                    INNER JOIN prontuarios pr ON e.prontuario_id = pr.id
                    WHERE pr.paciente_id = @id 
                    AND e.realizado = true 
                    AND e.data_realizacao IS NOT NULL
                    ORDER BY e.data_realizacao DESC
                    LIMIT 10
                """
                |> Sql.parameters ["id", Sql.int id]
                |> Sql.executeAsync (fun read -> {|
                    Tipo = read.string "tipo_exame"
                    Data = read.dateTime "data_realizacao"
                    Resultado = read.stringOrNone "resultado" |> Option.defaultValue "Não informado"
                |})

            return {
                PacienteId = paciente.Id
                PacienteNome = paciente.Nome
                TotalProntuarios = historico.TotalProntuarios
                UltimoAtendimento = historico.UltimoAtendimento
                DiagnosticosMaisFrequentes = historico.Diagnosticos
                MedicamentosAtuais = historico.MedicamentosAtuais
                AlergiasMedicamentosas = [] // Would need separate table for allergies
                ExamesRecentes = examesRecentes
                InternacoesPrevias = historico.InternacoesPrevias
                ObservacoesCriticas = [] // Would need separate table for critical observations
            }
        }

    let search (termo: string) (limite: int option) =
        task {
            let termoLike = System.String.Format("%{0}%",termo)
            let limiteQuery = limite |> Option.defaultValue 50

            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query $"""
                    SELECT p.id, p.nome, p.cpf, p.telefone, p.data_nascimento, p.plano_saude
                    FROM pacientes p
                    WHERE p.ativo = true
                    AND (
                        p.nome ILIKE @termo 
                        OR p.cpf ILIKE @termo
                        OR p.telefone ILIKE @termo
                    )
                    ORDER BY 
                        CASE WHEN p.nome ILIKE @termo THEN 1 ELSE 2 END,
                        p.nome
                    LIMIT {limiteQuery}
                """
                |> Sql.parameters ["termo", Sql.string termoLike]
                |> Sql.executeAsync (fun read -> {|
                    Id = read.int "id"
                    Nome = read.string "nome"
                    CPF = read.string "cpf"
                    Telefone = read.string "telefone"
                    DataNascimento = read.dateTime "data_nascimento"
                    PlanoSaude = read.stringOrNone "plano_saude"
                |})
        }

// Paciente/Handler.fs
module Handler =
    open Giraffe    
    open Models

    // DTOs for API
    type PacienteResponse = {
        Id: int
        Nome: string
        CPF: string
        RG: string option
        DataNascimento: DateTime
        Idade: int
        Sexo: string
        EstadoCivil: string option
        Profissao: string option
        Email: string option
        Telefone: string
        TelefoneSecundario: string option
        Endereco: EnderecoResponse option
        PlanoSaude: string option
        NumeroCarteirinha: string option
        Observacoes: string option
        ContatosEmergencia: ContatoEmergenciaResponse list
        DataCadastro: DateTime
        DataAtualizacao: DateTime option
        Ativo: bool
    }

    and EnderecoResponse = {
        Id: int
        Logradouro: string
        Numero: string
        Complemento: string option
        Bairro: string
        Cidade: string
        Estado: string
        CEP: string
        Pais: string
        EnderecoCompleto: string
    }

    and ContatoEmergenciaResponse = {
        Id: int
        Nome: string
        Parentesco: string
        Telefone: string
        Email: string option
    }
    [<CLIMutable>]
    type EnderecoInputDto = {
        Logradouro: string
        Numero: string
        Complemento: string option
        Bairro: string
        Cidade: string
        Estado: string
        CEP: string
        Pais: string
    }
    [<CLIMutable>]
    type PacienteInputDto = {
        Nome: string
        CPF: string
        RG: string option
        DataNascimento: DateTime
        Sexo: string
        EstadoCivil: string option
        Profissao: string option
        Email: string option
        Telefone: string
        TelefoneSecundario: string option
        Endereco: EnderecoInputDto
        PlanoSaude: string option
        NumeroCarteirinha: string option
        Observacoes: string option
        ContatosEmergencia: ContatoEmergenciaInputDto list
    }
        

    and ContatoEmergenciaInputDto = {
        Nome: string
        Parentesco: string
        Telefone: string
        Email: string option
    }

    type PacienteSearchResponse = {
        Id: int
        Nome: string
        CPF: string
        Telefone: string
        DataNascimento: DateTime
        Idade: int
        PlanoSaude: string option
    }

    // Conversion functions
    let private toEnderecoResponse (endereco: Endereco) : EnderecoResponse =
        let enderecoCompleto = 
            let complemento = endereco.Complemento |> Option.map (fun c -> $", {c}") |> Option.defaultValue ""
            $"{endereco.Logradouro}, {endereco.Numero}{complemento} - {endereco.Bairro}, {endereco.Cidade}/{endereco.Estado} - CEP: {endereco.CEP}"

        {
            Id = endereco.Id
            Logradouro = endereco.Logradouro
            Numero = endereco.Numero
            Complemento = endereco.Complemento
            Bairro = endereco.Bairro
            Cidade = endereco.Cidade
            Estado = endereco.Estado
            CEP = endereco.CEP
            Pais = endereco.Pais
            EnderecoCompleto = enderecoCompleto
        }

    let private toContatoEmergenciaResponse (contato: ContatoEmergencia) : ContatoEmergenciaResponse =
        {
            Id = contato.Id
            Nome = contato.Nome
            Parentesco = contato.Parentesco
            Telefone = contato.Telefone
            Email = contato.Email
        }

    let private toResponse (paciente: Paciente) : PacienteResponse =
        let idade = DateTime.Now.Year - paciente.DataNascimento.Year - (if DateTime.Now.DayOfYear < paciente.DataNascimento.DayOfYear then 1 else 0)
        {
            Id = paciente.Id
            Nome = paciente.Nome
            CPF = paciente.CPF
            RG = paciente.RG
            DataNascimento = paciente.DataNascimento
            Idade = idade
            Sexo = paciente.Sexo
            EstadoCivil = paciente.EstadoCivil
            Profissao = paciente.Profissao
            Email = paciente.Email
            Telefone = paciente.Telefone
            TelefoneSecundario = paciente.TelefoneSecundario
            Endereco = paciente.Endereco |> Option.map toEnderecoResponse
            PlanoSaude = paciente.PlanoSaude
            NumeroCarteirinha = paciente.NumeroCarteirinha
            Observacoes = paciente.Observacoes
            ContatosEmergencia = paciente.ContatosEmergencia |> List.map toContatoEmergenciaResponse
            DataCadastro = paciente.DataCadastro
            DataAtualizacao = paciente.DataAtualizacao
            Ativo = paciente.Ativo
        }

    let private toDomainInput (dto: PacienteInputDto) : PacienteInput =
        {
            Nome = dto.Nome
            CPF = dto.CPF
            RG = dto.RG
            DataNascimento = dto.DataNascimento
            Sexo = dto.Sexo
            EstadoCivil = dto.EstadoCivil
            Profissao = dto.Profissao
            Email = dto.Email
            Telefone = dto.Telefone
            TelefoneSecundario = dto.TelefoneSecundario
            Endereco = Some {
                Logradouro = dto.Endereco.Logradouro
                Numero = dto.Endereco.Numero
                Complemento = dto.Endereco.Complemento 
                Bairro = dto.Endereco.Bairro
                Cidade = dto.Endereco.Cidade
                Estado = dto.Endereco.Estado
                CEP = dto.Endereco.CEP
                Pais = if System.String.IsNullOrWhiteSpace(dto.Endereco.Pais) then None else dto.Endereco.Pais |> Some
            }
            PlanoSaude = dto.PlanoSaude
            NumeroCarteirinha = dto.NumeroCarteirinha
            Observacoes = dto.Observacoes
            ContatosEmergencia = dto.ContatosEmergencia |> List.map (fun c -> {
                Nome = c.Nome
                Parentesco = c.Parentesco
                Telefone = c.Telefone
                Email = c.Email
            })
        }

    // Validation functions
    let private validarCPF (cpf: string) =
        let cpfLimpo = cpf.Replace(".", "").Replace("-", "").Replace(" ", "")
        cpfLimpo.Length = 11 && cpfLimpo |> Seq.forall System.Char.IsDigit

    let private validarEmail (email: string) =
        email.Contains("@") && email.Contains(".") && email.Length >= 5

    let private validarTelefone (telefone: string) =
        let telefoneLimpo = telefone.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", "")
        telefoneLimpo.Length >= 10 && telefoneLimpo |> Seq.forall System.Char.IsDigit

    let private validateInput (dto: PacienteInputDto) =
        let errors = ResizeArray<string>()
        
        if String.IsNullOrWhiteSpace(dto.Nome) || dto.Nome.Length < 2 then
            errors.Add("Nome deve ter pelo menos 2 caracteres")
        
        if String.IsNullOrWhiteSpace(dto.CPF) || not (validarCPF dto.CPF) then
            errors.Add("CPF inválido")
        
        if String.IsNullOrWhiteSpace(dto.Telefone) || not (validarTelefone dto.Telefone) then
            errors.Add("Telefone inválido")
        
        match dto.TelefoneSecundario with
        | Some tel when not (String.IsNullOrWhiteSpace(tel)) && not (validarTelefone tel) ->
            errors.Add("Telefone secundário inválido")
        | _ -> ()

        match dto.Email with
        | Some email when not (String.IsNullOrWhiteSpace(email)) && not (validarEmail email) ->
            errors.Add("Email inválido")
        | _ -> ()

        if dto.DataNascimento > DateTime.Today then
            errors.Add("Data de nascimento não pode ser no futuro")
        
        if dto.DataNascimento < DateTime.Today.AddYears(-120) then
            errors.Add("Data de nascimento não pode ser anterior a 120 anos")

        match dto.Sexo.ToUpper() with
        | "M" | "F" | "O" -> ()
        | _ -> errors.Add("Sexo deve ser M (Masculino), F (Feminino) ou O (Outros)")

        // Validate endereco if provided
        match dto.Endereco |> Some with
        | Some endereco ->
            if String.IsNullOrWhiteSpace(endereco.Logradouro) then
                errors.Add("Logradouro é obrigatório")
            if String.IsNullOrWhiteSpace(endereco.Numero) then
                errors.Add("Número do endereço é obrigatório")
            if String.IsNullOrWhiteSpace(endereco.Bairro) then
                errors.Add("Bairro é obrigatório")
            if String.IsNullOrWhiteSpace(endereco.Cidade) then
                errors.Add("Cidade é obrigatória")
            if String.IsNullOrWhiteSpace(endereco.Estado) || endereco.Estado.Length <> 2 then
                errors.Add("Estado deve ter 2 caracteres (UF)")
            if String.IsNullOrWhiteSpace(endereco.CEP) then
                errors.Add("CEP é obrigatório")
        | None -> ()

        // Validate emergency contacts
        dto.ContatosEmergencia |> List.iteri (fun i contato ->
            if String.IsNullOrWhiteSpace(contato.Nome) then
                errors.Add($"Contato de emergência {i+1}: Nome é obrigatório")
            if String.IsNullOrWhiteSpace(contato.Telefone) || not (validarTelefone contato.Telefone) then
                errors.Add($"Contato de emergência {i+1}: Telefone inválido")
            match contato.Email with
            | Some email when not (String.IsNullOrWhiteSpace(email)) && not (validarEmail email) ->
                errors.Add($"Contato de emergência {i+1}: Email inválido")
            | _ -> ()
        )

        errors |> Seq.toList

    // HTTP Handlers
    let getAllPacientes : HttpHandler =
        fun next ctx ->
            task {
                try
                    let ativo = 
                        match ctx.TryGetQueryStringValue "ativo" with
                        | Some "true" -> Some true
                        | Some "false" -> Some false
                        | _ -> None

                    let termo = ctx.TryGetQueryStringValue "termo"

                    let unidadeId = 
                        match ctx.TryGetQueryStringValue "unidadeId" with
                        | Some idStr -> Int32.TryParse(idStr) |> function | true, id -> Some id | _ -> None
                        | None -> None
                    Logger.logger.Information("Requisição para listar pacientes utilizando paramêtros = {params}: {ex}",{| ativo = ativo;termo = termo; unidadeId = unidadeId; |})
                    let! pacientes = Repository.getAll ativo termo unidadeId
                    let response = pacientes |> List.map toResponse
                    Logger.logger.Information("Resposta da Requisição para listar pacientes: {response}",response)
                    return! json response next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro interno do servidor"; details = ex.Message |}
                    Logger.logger.Error("Erro interno do servidor: {ex}",ex)
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let getPacienteById pacienteId : HttpHandler =
        fun next ctx ->
            task {
                try
                    Logger.logger.Information("Requisição para consultar paciente utilizando paramêtros = {params}: {ex}",{| pacienteId = pacienteId |})
                    let! paciente = Repository.getById pacienteId
                    let response = toResponse paciente
                    Logger.logger.Information("Resposta da Requisição para consultar paciente: {response}",response)
                    return! json response next ctx
                with
                | :? System.InvalidOperationException ->
                    let errorResponse = {| error = "Paciente não encontrado" |}
                    return! (setStatusCode 404 >=> json errorResponse) next ctx
                | ex ->
                    let errorResponse = {| error = "Erro interno do servidor"; details = ex.Message |}
                    Logger.logger.Error("Erro interno do servidor: {ex}",ex)
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let getPacienteDetalhes pacienteId : HttpHandler =
        fun next ctx ->
            task {
                try                                        
                    let! detalhes = Repository.getPacienteDetalhes pacienteId
                    return! json detalhes next ctx
                with
                | :? System.InvalidOperationException ->
                    let errorResponse = {| error = "Paciente não encontrado" |}
                    return! (setStatusCode 404 >=> json errorResponse) next ctx
                | ex ->
                    let errorResponse = {| error = "Erro interno do servidor"; details = ex.Message |}
                    Logger.logger.Error("Erro interno do servidor: {ex}",ex)
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let getPacienteByCPF cpf : HttpHandler =
        fun next ctx ->
            task {
                try
                    let! pacienteOpt = Repository.getByCPF cpf
                    
                    match pacienteOpt with
                    | Some paciente ->
                        let response = toResponse paciente
                        return! json response next ctx
                    | None ->
                        let errorResponse = {| error = "Paciente não encontrado" |}
                        return! (setStatusCode 404 >=> json errorResponse) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro interno do servidor"; details = ex.Message |}
                    Logger.logger.Error("Erro interno do servidor: {ex}",ex)
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let searchPacientes : HttpHandler =
        fun next ctx ->
            task {
                try
                    let termo =
                        ctx.GetQueryStringValue "q"
                        |> Result.defaultValue ""
                    let limite = 
                        match ctx.TryGetQueryStringValue "limite" with
                        | Some limiteStr -> Int32.TryParse(limiteStr) |> function | true, l -> Some l | _ -> None
                        | None -> None

                    if String.IsNullOrWhiteSpace(termo) || termo.Length < 3 then
                        let errorResponse = {| error = "Termo de busca deve ter pelo menos 3 caracteres" |}
                        return! (setStatusCode 400 >=> json errorResponse) next ctx
                    else
                        let! resultados = Repository.search termo limite
                        let response = resultados |> List.map (fun r -> 
                            let idade = DateTime.Now.Year - r.DataNascimento.Year - (if DateTime.Now.DayOfYear < r.DataNascimento.DayOfYear then 1 else 0)
                            {
                                Id = r.Id
                                Nome = r.Nome
                                CPF = r.CPF
                                Telefone = r.Telefone
                                DataNascimento = r.DataNascimento
                                Idade = idade
                                PlanoSaude = r.PlanoSaude
                            })
                        
                        return! json response next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro interno do servidor"; details = ex.Message |}
                    Logger.logger.Error("Erro interno do servidor: {ex}",ex)
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let createPaciente : HttpHandler =
        fun next ctx ->
            task {
                try
                    let! body = ctx.ReadBodyFromRequestAsync()
                    Logger.logger.Information("Corpo da requisição recebida para criação de paciente: {body}",body)
                    // let dto = Newtonsoft.Json.JsonConvert.DeserializeObject<PacienteInputDto>(body,FSharpConverter)
                    // let! inputDto = ctx.BindJsonAsync<PacienteInputDto>()
                    let inputDto = Newtonsoft.Json.JsonConvert.DeserializeObject<PacienteInputDto>(body)
                    Logger.logger.Information("Requisição deserializada: {body}",inputDto)
                    let validationErrors = validateInput inputDto
                    if not validationErrors.IsEmpty then
                        let errorResponse = {| errors = validationErrors |}
                        return! (setStatusCode 400 >=> json errorResponse) next ctx
                    else
                        let domainInput = toDomainInput inputDto
                        let! result = Repository.insert domainInput
                        
                        match result with
                        | Ok id ->
                            let response = {| id = id; message = "Paciente cadastrado com sucesso" |}
                            return! (setStatusCode 201 >=> json response) next ctx
                        | Error erro ->
                            let errorResponse = {| error = erro |}
                            return! (setStatusCode 400 >=> json errorResponse) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao cadastrar paciente"; details = ex.Message |}
                    Logger.logger.Error("Erro interno do servidor: {ex}",ex)
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let updatePaciente pacienteId : HttpHandler =
        fun next ctx ->
            task {
                try
                    let! inputDto = ctx.BindJsonAsync<PacienteInputDto>()
                    
                    let validationErrors = validateInput inputDto
                    if not validationErrors.IsEmpty then
                        let errorResponse = {| errors = validationErrors |}
                        return! (setStatusCode 400 >=> json errorResponse) next ctx
                    else
                        let domainInput = toDomainInput inputDto
                        let! result = Repository.update pacienteId domainInput
                        
                        match result with
                        | Ok _ ->
                            let response = {| message = "Paciente atualizado com sucesso" |}
                            return! json response next ctx
                        | Error erro ->
                            let errorResponse = {| error = erro |}
                            return! (setStatusCode 400 >=> json errorResponse) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao atualizar paciente"; details = ex.Message |}
                    Logger.logger.Error("Erro interno do servidor: {ex}",ex)
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let deactivatePaciente pacienteId : HttpHandler =
        fun next ctx ->
            task {
                try
                    let! success = Repository.deactivate pacienteId
                    
                    if success then
                        let response = {| message = "Paciente desativado com sucesso" |}
                        return! json response next ctx
                    else
                        let errorResponse = {| error = "Paciente não encontrado" |}
                        return! (setStatusCode 404 >=> json errorResponse) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao desativar paciente"; details = ex.Message |}
                    Logger.logger.Error("Erro interno do servidor: {ex}",ex)
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let reactivatePaciente pacienteId : HttpHandler =
        fun next ctx ->
            task {
                try
                    let! success = Repository.reactivate pacienteId
                    
                    if success then
                        let response = {| message = "Paciente reativado com sucesso" |}
                        return! json response next ctx
                    else
                        let errorResponse = {| error = "Paciente não encontrado" |}
                        return! (setStatusCode 404 >=> json errorResponse) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao reativar paciente"; details = ex.Message |}
                    Logger.logger.Error("Erro interno do servidor: {ex}",ex)
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    let getHistoricoMedico pacienteId : HttpHandler =
        fun next ctx ->
            task {
                try                    
                    let! historico = Repository.getHistoricoMedico pacienteId
                    return! json historico next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao obter histórico médico"; details = ex.Message |}
                    Logger.logger.Error("Erro interno do servidor: {ex}",ex)
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    // Rotas Paciente
    let routes : HttpHandler =
        choose [
            GET >=> choose [
                route "" >=> getAllPacientes
                route "/search" >=> searchPacientes
                routef "/%i" getPacienteById
                routef "/%i/detalhes" getPacienteDetalhes
                routef "/%i/historico" getHistoricoMedico
                routef "/cpf/%s" getPacienteByCPF
            ]
            POST >=> choose [
                route "" >=> createPaciente
                routef "/%i/reativar" reactivatePaciente
            ]
            PUT >=> routef "/%i" updatePaciente
            DELETE >=> routef "/%i" deactivatePaciente
        ]
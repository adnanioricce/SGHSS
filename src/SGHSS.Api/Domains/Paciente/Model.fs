namespace SGH.Paciente

module Models =
    type Paciente = {
      Id: int
      Nome: string
      Cpf: string
      DataNascimento: System.DateTime
      PlanoSaude: string option
    }
    type PacienteInput = {
      Nome: string
      Cpf: string
      DataNascimento: System.DateTime
      PlanoSaude: string option
    }
module Repository =
   
    open Npgsql.FSharp
    open Models

    let getAll (connStr: string) =
        task {
            return!
                connectionString connStr
                |> Sql.connect
                |> Sql.query "SELECT id, nome, cpf, data_nascimento, plano_saude FROM pacientes"
                |> Sql.executeAsync (fun read -> {
                    Id = read.int "id"
                    Nome = read.string "nome"
                    Cpf = read.string "cpf"
                    DataNascimento = read.dateTime "data_nascimento"
                    PlanoSaude = read.stringOrNone "plano_saude"
                })
        }
    let insert (connStr: string) (input: PacienteInput) =
        task {
            return!
                connectionString connStr
                |> Sql.connect
                |> Sql.query """
                    INSERT INTO pacientes (nome, cpf, data_nascimento, plano_saude)
                    VALUES (@nome, @cpf, @data_nascimento, @plano_saude)
                    RETURNING id
                """
                |> Sql.parameters [
                    "nome", Sql.string input.Nome
                    "cpf", Sql.string input.Cpf
                    "data_nascimento", Sql.timestamp input.DataNascimento
                    "plano_saude", Sql.option Sql.string input.PlanoSaude
                ]
                |> Sql.executeRowAsync (fun read -> read.int "id")
        }
module Handler =
    

    open Giraffe
    open Microsoft.AspNetCore.Http
    open FSharp.Control.Tasks.V2.ContextInsensitive
    open Repositories.PacienteRepository
    open Models

    let getAllPacientes : HttpHandler =
      fun next ctx ->
        task {
            let! pacientes = getAll "Host=localhost;Username=postgres;Password=senha;Database=sghss"
            return! json pacientes next ctx
        }
    let createPaciente : HttpHandler =
      fun next ctx ->
        task {
            let! input = ctx.BindJsonAsync<PacienteInput>()
            let! id = PacienteRepository.insert "Host=localhost;Username=postgres;Password=senha;Database=sghss" input
            return! json {| id = id |} next ctx
        }

    let routes : HttpHandler =
      choose [
        GET >=> route "/" >=> getAllPacientes
        POST >=> route "/" >=> createPaciente
      ]
    

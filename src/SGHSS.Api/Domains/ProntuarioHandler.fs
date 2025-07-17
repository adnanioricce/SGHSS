namespace SGH.Prontuario

module Models =
    type Prontuario = {
        Id: int
        PacienteId: int
        ProfissionalId: int
        Data: System.DateTime
        Conteudo: Newtonsoft.Json.Linq.JObject
    }
    
    type ProntuarioInput = {
        PacienteId: int
        ProfissionalId: int
        Conteudo: Newtonsoft.Json.Linq.JObject
    }
module Repository =

    let insert (connStr: string) (input: ProntuarioInput) =
        task {
            return!
                connStr
                |> Sql.connect
                |> Sql.query """
                INSERT INTO prontuarios (paciente_id, profissional_id, data, conteudo)
                VALUES (@paciente_id, @profissional_id, NOW(), @conteudo::jsonb)
                RETURNING id
                """            
                |> Sql.parameters [
                    "paciente_id", Sql.int input.PacienteId
                    "profissional_id", Sql.int input.ProfissionalId
                    "conteudo", Sql.string (input.Conteudo.ToString(Newtonsoft.Json.Formatting.None))
                ]
                |> Sql.executeRowAsync (fun read -> read.int "id")
        }
module Handler =
    open Giraffe
    open Microsoft.AspNetCore.Http        
    open Models

    let createProntuario : HttpHandler =
        fun next ctx ->
            task {
                let! input = ctx.BindJsonAsync<ProntuarioInput>()
                let! id = Repository.insert "Host=localhost;Username=postgres;Password=senha;Database=sghss" input
                return! json {| id = id |} next ctx
            }
    let getProntuario : HttpHandler =
        fun next ctx ->
            task {
                let id = ctx.GetRouteValue("id") :?> int
                let! prontuario =
                    "Host=localhost;Username=postgres;Password=senha;Database=sghss"
                    |> Sql.connect
                    |> Sql.query "SELECT id, paciente_id, profissional_id, data, conteudo FROM prontuarios WHERE id = @id"
                    |> Sql.parameters ["id", Sql.int id]
                    |> Sql.executeRowAsync (fun read -> {
                        Id = read.int "id"
                        PacienteId = read.int "paciente_id"
                        ProfissionalId = read.int "profissional_id"
                        Data = read.dateTime "data"
                        Conteudo = Newtonsoft.Json.Linq.JObject.Parse(read.string "conteudo")
                    })
                return! json prontuario next ctx
            }
    let getAllProntuarios : HttpHandler =
        fun next ctx ->
            task {
                let! prontuarios =
                    "Host=localhost;Username=postgres;Password=senha;Database=sghss"
                    |> Sql.connect
                    |> Sql.query "SELECT id, paciente_id, profissional_id, data, conteudo FROM prontuarios"
                    |> Sql.executeAsync (fun read -> {
                        Id = read.int "id"
                        PacienteId = read.int "paciente_id"
                        ProfissionalId = read.int "profissional_id"
                        Data = read.dateTime "data"
                        Conteudo = Newtonsoft.Json.Linq.JObject.Parse(read.string "conteudo")
                    })
                return! json prontuarios next ctx
            }
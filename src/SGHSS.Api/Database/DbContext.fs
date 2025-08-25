namespace Infrastructure.Database

open System
open Npgsql.FSharp

module DbConnection =
    let getConnectionString () = 
        let envVar = Environment.GetEnvironmentVariable("CONNECTION_STRING") 
        if System.String.IsNullOrWhiteSpace(envVar) then "Host=localhost;Username=postgres;Password=senha;Database=sghss" else envVar
    
    let getConnection () = getConnectionString () |> Sql.connect
    
    let executeQuery query parameters =
        task {
            return!
                getConnection()
                |> Sql.query query
                |> Sql.parameters parameters
                |> Sql.executeAsync (fun read -> read)
        }
    
    let executeScalar<'T> query parameters (mapper: RowReader -> 'T) =
        task {
            return!
                getConnection()
                |> Sql.query query
                |> Sql.parameters parameters
                |> Sql.executeRowAsync mapper
        }
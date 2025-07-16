open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Giraffe

[<EntryPoint>]
let main _ =
    let builder = WebApplication.CreateBuilder()
    
    builder.Services.AddGiraffe() |> ignore
    builder.Services.AddSingleton<Data.DbConnectionFactory>() |> ignore

    let app = builder.Build()
    app.UseGiraffe(Routing.routes)
    app.Run()
    0

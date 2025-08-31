namespace SGHSS.Api.Logging

module Logger =
    open Serilog
    //open Microsoft.AspNetCore.Authentication.JwtBearer
    let logger =
        let config =
            new LoggerConfiguration()
            |> (fun cfg -> cfg.WriteTo.Console().MinimumLevel.Debug())
        config.CreateLogger()
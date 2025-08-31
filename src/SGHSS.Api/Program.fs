module SGHSS.Api.App

open System
open System.IO
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.HttpLogging
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Microsoft.Extensions.Configuration
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Newtonsoft.Json
open Newtonsoft.Json.FSharp
open Microsoft.IdentityModel.Tokens
open System.Text
// open Microsoft.OpenApi.Models
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open NSwag
open NSwag.Generation.Processors.Security
open NSwag.Annotations
open Giraffe
open SGHSS.Api.Logging
open Serilog

let webApp =
    choose [
        GET >=> route "/" >=> text "Welcome to SGHSS API"
        //route "/pacientes" >=> Domains.Paciente.Handler.routes
        // Rotas autenticadas
        subRoute "/api/v1" (
            choose [
                // Pacientes
                subRoute "/pacientes" Domains.Paciente.Handler.routes
                
                // Profissionais
                subRoute "/profissionais" Domains.Profissional.Handler.routes
                
                //// Agendamentos
                subRoute "/agendamentos" Domains.Agendamento.Handler.routes
                
                //// Prontuários
                subRoute "/prontuarios" Domains.Prontuario.Handler.routes
                
                //// Telemedicina
                subRoute "/telemedicina" Domains.Telemedicina.Handler.routes
                
                //// Administração
                subRoute "/admin" Domains.Administracao.Handler.routes
                
                //// Relatórios
                //subRoute "/relatorios" Domains.Relatorios.Handler.routes
            ]
        )
        // route "/prontuarios" >=> Domains.Prontuario.Handler.routes
        // route "/prontuarios/{id:int}" >=> Domains.Prontuario.Handler.getProntuario
        setStatusCode 404 >=> text "Not Found"
        setStatusCode 405 >=> text "Method Not Allowed"
        setStatusCode 500 >=> text "Internal Server Error"
        setStatusCode 404 >=> text "Not Found" ]
let configureJwt (services: IServiceCollection) =
    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(fun options ->
                options.TokenValidationParameters <- 
                    TokenValidationParameters(
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = SymmetricSecurityKey(Encoding.UTF8.GetBytes("<PEGUE O VALOR DE UM ARQUIVO DE CONFIG OU DE UMA VARIAVEL DE AMBIENTE>")),
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ClockSkew = TimeSpan.Zero
                    )
            ) |> ignore
let configureSwagger (services: IServiceCollection) =
    services.AddEndpointsApiExplorer() |> ignore
    // NSwag setup
    services.AddOpenApiDocument(fun settings ->
        settings.Title <- "SGHSS API"
        settings.Version <- "v1"
    ) |> ignore
    // services.AddSwaggerGen(fun c ->
    //     c.SwaggerDoc("v1", OpenApiInfo(
    //         Title = "My Giraffe API",
    //         Version = "v1"
    //     ))
    // ) |> ignore
// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex : Exception) (logger : Microsoft.Extensions.Logging.ILogger) =
    Logger.logger.Error(ex, "An unhandled exception has occurred while executing the request.")
    // logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

// ---------------------------------
// Config and Main
// ---------------------------------

let configureCors (configuration:IConfiguration) =
    // let origins = 
    fun (builder : CorsPolicyBuilder) ->        
        let allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>()
        // builder.WithOrigins(allowedOrigins)
        
        builder.WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                |> ignore

let configureApp (app : IApplicationBuilder) =
    let env = app.ApplicationServices.GetService<IWebHostEnvironment>()
    app.UseSerilogRequestLogging() |> ignore
    // app.UseSwagger() |> ignore
    // app.UseSwaggerUI(fun c -> c.SwaggerEndpoint("/swagger/v1/swagger.json", "SGHSS Api v1")) |> ignore
    app.UseOpenApi() |> ignore              // Serves /swagger/v1/swagger.json
    app.UseSwaggerUi() |> ignore           // Serves Swagger UI at /swagger
    // app.UseMiddleware(fun next ->
    //     let func:Func<HttpContext,Task<unit>> = RequestLoggingMiddleware.requestResponseLoggingMiddleware next
    //     func) |> ignore    
    (match env.IsDevelopment() with
    | true  ->
        app.UseDeveloperExceptionPage()
        
    | false ->
        app .UseGiraffeErrorHandler(errorHandler)
            .UseHttpsRedirection())
        .UseCors((configureCors (app.ApplicationServices.GetService<IConfiguration>())))
        .UseStaticFiles()
        .UseGiraffe(webApp)

let configureServices (services : IServiceCollection) =
    services.AddCors()    |> ignore
    services.AddGiraffe() |> ignore
    // .AddControllers().AddNewtonsoftJson(fun opts ->
    //     // Enable F# support
    //     opts.SerializerSettings.Converters.Add(FSharpConverter())
    //     opts.SerializerSettings.NullValueHandling <- NullValueHandling.Ignore) |> ignore
    // services.AddLogging() |> ignore
    services.AddSerilog() |> ignore    
    // services.AddHttpLogging(fun opt ->
    //     opt.CombineLogs <- true
    //     opt.LoggingFields <- HttpLoggingFields.All) |> ignore
    configureJwt services |> ignore
    configureSwagger services |> ignore

let configureLogging (builder : ILoggingBuilder) =
    builder.AddConsole().AddSerilog(Logger.logger) |> ignore

[<EntryPoint>]
let main args =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot     = Path.Combine(contentRoot, "WebRoot")
    Logger.logger.Information("Iniciando aplicação...")
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(
            fun webHostBuilder ->
                webHostBuilder
                    .UseContentRoot(contentRoot)
                    .UseWebRoot(webRoot)
                    .Configure(Action<IApplicationBuilder> configureApp)
                    .ConfigureServices(configureServices)
                    .ConfigureLogging(configureLogging)
                    |> ignore)
        .Build()
        .Run()
    0
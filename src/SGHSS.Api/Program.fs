module SGHSS.Api.App

open System
open System.IO
open System.Threading.Tasks
open Infrastructure.Security
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
open System
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Giraffe
open Infrastructure.Security.AuthHandlers
open Infrastructure.Security.Authorization
open Infrastructure.Security.AuthStartup
open Infrastructure.Security.UserManagementHandlers

let webApp =
    choose [
        GET >=> route "/" >=> text "Welcome to SGHSS API"
        // Public authentication routes
        subRoute "/api/v1/auth" AuthHandlers.routes
        //route "/pacientes" >=> Domains.Paciente.Handler.routes
        // Rotas autenticadas
        subRoute "/api/v1" (
            requireAuth >=> choose [
                // Pacientes
                subRoute "/pacientes" (healthcareProfessional() >=> Domains.Paciente.Handler.routes)
                
                // Profissionais
                subRoute "/profissionais" (internalOnly() >=> Domains.Profissional.Handler.routes)
                
                //// Agendamentos
                subRoute "/agendamentos" (healthcareProfessional() >=> Domains.Agendamento.Handler.routes)
                
                //// Prontuários
                subRoute "/prontuarios" (medicoOrEnfermeiro() >=> Domains.Prontuario.Handler.routes)
                
                //// Telemedicina
                subRoute "/telemedicina" (medicoOnly() >=> Domains.Telemedicina.Handler.routes)
                
                //// Administração
                subRoute "/admin" (adminOnly() >=> Domains.Administracao.Handler.routes)
                
                subRoute "/users" (adminOnly() >=> UserManagementHandlers.routes)
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
        
        builder
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .Build()
            |> ignore

let configureApp (app : IApplicationBuilder) =
    let env = app.ApplicationServices.GetService<IWebHostEnvironment>()
    // app.UseSerilogRequestLogging() |> ignore
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
        
        .UseStaticFiles()
        |> configureAuthMiddleware
        |> (fun app -> app.UseCors((configureCors (app.ApplicationServices.GetService<IConfiguration>()))))
        |> (fun builder -> builder.UseGiraffe(webApp))

let configureServices (services : IServiceCollection) =
    services.AddCors(fun opt ->
        opt.AddPolicy("Default",fun policy ->
            let allowedOrigins = [|"http://localhost:4200";"http://localhost:58078";"http://localhost:5173"|]
        // builder.WithOrigins(allowedOrigins)
        
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
                .Build()
                |> ignore))    |> ignore
    services.AddGiraffe() |> ignore
    // .AddControllers().AddNewtonsoftJson(fun opts ->
    //     // Enable F# support
    //     opts.SerializerSettings.Converters.Add(FSharpConverter())
    //     opts.SerializerSettings.NullValueHandling <- NullValueHandling.Ignore) |> ignore
    services.AddLogging() |> ignore
    // services.AddSerilog() |> ignore    
    // services.AddHttpLogging(fun opt ->
    //     opt.CombineLogs <- true
    //     opt.LoggingFields <- HttpLoggingFields.All) |> ignore
    configureJwtAuthentication services |> ignore
    configureSwagger services |> ignore

let configureLogging (builder : ILoggingBuilder) =
    builder.SetMinimumLevel(LogLevel.Information).AddDebug().AddConsole() |> ignore

[<EntryPoint>]
let main args =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot     = Path.Combine(contentRoot, "WebRoot")
    // quero evitar isso ficar aparecendo no banco de logs no futuro,
    // mesmo que não seja um projeto sério. 
    printfn "%s" (Authentication.hashPassword None "Adm12345!")
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
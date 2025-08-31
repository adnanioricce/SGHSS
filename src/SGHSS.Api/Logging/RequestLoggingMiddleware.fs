namespace SGHSS.Api.Logging

module RequestLoggingMiddleware =
    open System.IO
    open System.Text    
    open Microsoft.AspNetCore.Http
    open Microsoft.Extensions.Logging

    let requestResponseLoggingMiddleware (next: RequestDelegate) (ctx: HttpContext) =
        task {
            let logger = ctx.RequestServices.GetService(typeof<ILoggerFactory>) :?> ILoggerFactory
            let log = logger.CreateLogger("RequestResponseLogger")

            // --- Log Request ---
            ctx.Request.EnableBuffering() // allow re-reading request body
            use reader = new StreamReader(ctx.Request.Body, Encoding.UTF8, true, 1024, true)
            let! requestBody = reader.ReadToEndAsync()
            ctx.Request.Body.Position <- 0L

            log.LogInformation("HTTP {method} {path}\nHeaders: {headers}\nBody: {body}",
                ctx.Request.Method,
                ctx.Request.Path,
                ctx.Request.Headers |> Seq.map (fun kv -> kv.Key + "=" + kv.Value.ToString()) |> String.concat "; ",
                requestBody)

            // --- Capture Response ---
            let originalBodyStream = ctx.Response.Body
            use memStream = new MemoryStream()
            ctx.Response.Body <- memStream

            do! next.Invoke(ctx) // run rest of pipeline

            memStream.Position <- 0L
            use reader2 = new StreamReader(memStream)
            let! responseBody = reader2.ReadToEndAsync()

            log.LogInformation("Response {statusCode}\nBody: {body}",
                ctx.Response.StatusCode,
                responseBody)

            // Reset stream to copy back to actual response
            memStream.Position <- 0L
            do! memStream.CopyToAsync(originalBodyStream)
            ctx.Response.Body <- originalBodyStream
        }



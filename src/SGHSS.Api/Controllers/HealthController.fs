namespace SGHSS.Controllers

open Microsoft.AspNetCore.Mvc

[<ApiController>]
[<Route("api/[controller]")>]
type HealthController () =
    inherit ControllerBase()

    [<HttpGet>]
    member _.Get() = "SGHSS API is running"

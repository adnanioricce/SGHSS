namespace SGHSS.Api

module Utils =
    open System
    let defaultIfNull defaultValue value = if System.String.IsNullOrWhiteSpace(value) then defaultValue else value
    let toOptionStrIfNull value = if System.String.IsNullOrWhiteSpace(value) then None else Some value
    let toOptionIfNull value = if isNull value then None else Some value
    let toOptionIfNullable (value:Nullable<'a>) = if value.HasValue |> not then None else Some value.Value
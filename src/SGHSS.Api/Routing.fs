namespace SGH.Api

module Routing

open Giraffe
open Handlers

let routes : HttpHandler =
  choose [
    subRoute "/api/pacientes" PacienteHandler.routes
    subRoute "/api/profissionais" ProfissionalHandler.routes
    subRoute "/api/prontuarios" ProntuarioHandler.routes
  ]


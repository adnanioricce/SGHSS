let createPaciente : HttpHandler =
  fun next ctx ->
      task {
              let! input = ctx.BindJsonAsync<PacienteInput>()
                      let! id = PacienteRepository.insert "Host=localhost;Username=postgres;Password=senha;Database=sghss" input
                              return! json {| id = id |} next ctx
                                  }

                                  let routes : HttpHandler =
                                    choose [
                                        GET >=> route "/" >=> getAllPacientes
                                            POST >=> route "/" >=> createPaciente
                                              ]
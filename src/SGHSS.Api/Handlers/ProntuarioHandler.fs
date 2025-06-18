let insert (connStr: string) (input: ProntuarioInput) =
    task {
            return!
                        connectionString connStr
                                    |> Sql.connect
                                                |> Sql.query """
                                                                INSERT INTO prontuarios (paciente_id, profissional_id, data, conteudo)
                                                                                VALUES (@paciente_id, @profissional_id, NOW(), @conteudo::jsonb)
                                                                                                RETURNING id
                                                                                                            """
                                                                                                                        |> Sql.parameters [
                                                                                                                                        "paciente_id", Sql.int input.PacienteId
                                                                                                                                                        "profissional_id", Sql.int input.ProfissionalId
                                                                                                                                                                        "conteudo", Sql.string (input.Conteudo.ToString(Newtonsoft.Json.Formatting.None))
                                                                                                                                                                                    ]
                                                                                                                                                                                                |> Sql.executeRowAsync (fun read -> read.int "id")
                                                                                                                                                                                                    }
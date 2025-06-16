namespace SG.Data

module PacienteRepository =
   let insert (connStr: string) (input: PacienteInput) =
       task {
               return!
                           connectionString connStr
                                       |> Sql.connect
                                                   |> Sql.query """
                                                                   INSERT INTO pacientes (nome, cpf, data_nascimento, plano_saude)
                                                                                   VALUES (@nome, @cpf, @data_nascimento, @plano_saude)
                                                                                                   RETURNING id
                                                                                                               """
                                                                                                                           |> Sql.parameters [
                                                                                                                                           "nome", Sql.string input.Nome
                                                                                                                                                           "cpf", Sql.string input.Cpf
                                                                                                                                                                           "data_nascimento", Sql.timestamp input.DataNascimento
                                                                                                                                                                                           "plano_saude", Sql.option Sql.string input.PlanoSaude
                                                                                                                                                                                                       ]
                                                                                                                                                                                                                   |> Sql.executeRowAsync (fun read -> read.int "id")
                                                                                                                                                                                                                       }
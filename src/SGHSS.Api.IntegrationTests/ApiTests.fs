module ApiTests

// =====================================================
// SGHSS API Test Suite
// =====================================================

open Expecto
open System
open System.Net.Http
open System.Text
open Newtonsoft.Json

// Test configuration
let baseUrl = "http://localhost:8000"
let client = new HttpClient()

// Helper functions
let jsonContent (obj: 'T) =
    let json = JsonConvert.SerializeObject(obj)
    new StringContent(json, Encoding.UTF8, "application/json")

let getAsync (endpoint: string) =
    async {
        let! response = client.GetAsync($"{baseUrl}{endpoint}") |> Async.AwaitTask
        let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
        return (response.StatusCode, content)
    }

let postAsync (endpoint: string) (data: 'T) =
    async {
        let content = jsonContent data
        let! response = client.PostAsync($"{baseUrl}{endpoint}", content) |> Async.AwaitTask
        let! responseContent = response.Content.ReadAsStringAsync() |> Async.AwaitTask
        return (response.StatusCode, responseContent)
    }

let putAsync (endpoint: string) (data: 'T) =
    async {
        let content = jsonContent data
        let! response = client.PutAsync($"{baseUrl}{endpoint}", content) |> Async.AwaitTask
        let! responseContent = response.Content.ReadAsStringAsync() |> Async.AwaitTask
        return (response.StatusCode, responseContent)
    }

let deleteAsync (endpoint: string) =
    async {
        let! response = client.DeleteAsync($"{baseUrl}{endpoint}") |> Async.AwaitTask
        let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
        return (response.StatusCode, content)
    }

// Test data models
type TestPaciente = {
    nome: string
    cpf: string
    dataNascimento: DateTime
    sexo: string
    telefone: string
    endereco: {|
        logradouro: string
        numero: string
        bairro: string
        cidade: string
        estado: string
        cep: string
        pais: string
    |}
}

type TestProfissional = {
    nome: string
    cpf: string
    crm: string option
    tipoProfissional: string
    email: string
    telefone: string
    dataAdmissao: DateTime
    unidadeId: int
    permiteTelemedicina: bool
}

type TestAgendamento = {
    pacienteId: int
    profissionalId: int
    tipoAgendamento: string
    dataHora: DateTime
    duracao: string
    unidadeId: int
    planoSaudeCobertura: bool
}

// Test data
let samplePaciente = {
    nome = "João Silva Santos"
    cpf = "12345678901"
    dataNascimento = DateTime(1990, 5, 15)
    sexo = "M"
    telefone = "11999887766"
    endereco = {|
        logradouro = "Rua das Flores"
        numero = "123"
        bairro = "Centro"
        cidade = "São Paulo"
        estado = "SP"
        cep = "01234567"
        pais = "Brasil"
    |}
}

let sampleProfissional = {
    nome = "Dra. Maria Oliveira"
    cpf = "98765432100"
    crm = Some "CRM123456"
    tipoProfissional = "MEDICO"
    email = "maria.oliveira@hospital.com"
    telefone = "11988776655"
    dataAdmissao = DateTime.Now.Date
    unidadeId = 1
    permiteTelemedicina = true
}

// =====================================================
// PATIENT TESTS
// =====================================================
// [<Tests>]
let pacienteTests =
    testList "Paciente API Tests" [
        testAsync "Should create a new patient" {
            let! (statusCode, content) = postAsync "/api/v1/pacientes" samplePaciente
            Expect.equal statusCode System.Net.HttpStatusCode.Created "Should return 201 Created"
            
            let response = JsonConvert.DeserializeAnonymousType(content, {| id = 0; message = "" |})
            Expect.isGreaterThan response.id 0 "Should return valid ID"
        }

        testAsync "Should get all patients" {
            let! (statusCode, content) = getAsync "/api/v1/pacientes"
            Expect.equal statusCode System.Net.HttpStatusCode.OK "Should return 200 OK"
            
            let patients = JsonConvert.DeserializeObject<obj[]>(content)
            Expect.isGreaterThanOrEqual patients.Length 0 "Should return patients array"
        }

        testAsync "Should get patient by ID" {
            // First create a patient
            let! (createStatus, createContent) = postAsync "/api/v1/pacientes" samplePaciente
            let createResponse = JsonConvert.DeserializeAnonymousType(createContent, {| id = 0; message = "" |})
            
            // Then get it by ID
            let! (statusCode, content) = getAsync $"/api/v1/pacientes/{createResponse.id}"
            Expect.equal statusCode System.Net.HttpStatusCode.OK "Should return 200 OK"
            
            let patient = JsonConvert.DeserializeAnonymousType(content, {| id = 0; nome = ""; cpf = "" |})
            Expect.equal patient.nome samplePaciente.nome "Should return correct patient name"
        }

        testAsync "Should validate required fields" {
            let invalidPaciente = { samplePaciente with nome = ""; cpf = "" }
            let! (statusCode, content) = postAsync "/api/v1/pacientes" invalidPaciente
            Expect.equal statusCode System.Net.HttpStatusCode.BadRequest "Should return 400 Bad Request"
        }
    ]

// =====================================================
// PROFESSIONAL TESTS
// =====================================================
// [<Tests>]
let profissionalTests =
    testList "Profissional API Tests" [
        testAsync "Should create a new professional" {
            let! (statusCode, content) = postAsync "/api/v1/profissionais" sampleProfissional
            Expect.equal statusCode System.Net.HttpStatusCode.Created "Should return 201 Created"
            
            let response = JsonConvert.DeserializeAnonymousType(content, {| id = 0; message = "" |})
            Expect.isGreaterThan response.id 0 "Should return valid ID"
        }

        testAsync "Should get all professionals" {
            let! (statusCode, content) = getAsync "/api/v1/profissionais"
            Expect.equal statusCode System.Net.HttpStatusCode.OK "Should return 200 OK"
        }

        testAsync "Should get professionals by type" {
            let! (statusCode, content) = getAsync "/api/v1/profissionais/tipo/MEDICO"
            Expect.equal statusCode System.Net.HttpStatusCode.OK "Should return 200 OK"
        }

        testAsync "Should get active professionals only" {
            let! (statusCode, content) = getAsync "/api/v1/profissionais?ativo=true"
            Expect.equal statusCode System.Net.HttpStatusCode.OK "Should return 200 OK"
        }
    ]

// =====================================================
// APPOINTMENT TESTS
// =====================================================
// [<Tests>]
let agendamentoTests =
    testList "Agendamento API Tests" [
        testAsync "Should prevent scheduling conflicts" {
            // First create patient and professional
            let! (_, pacienteContent) = postAsync "/api/v1/pacientes" samplePaciente
            let pacienteResponse = JsonConvert.DeserializeAnonymousType(pacienteContent, {| id = 0 |})
            
            let! (_, profissionalContent) = postAsync "/api/v1/profissionais" sampleProfissional
            let profissionalResponse = JsonConvert.DeserializeAnonymousType(profissionalContent, {| id = 0 |})
            
            let futureDate = DateTime.Now.AddDays(1).Date.AddHours(10)
            let agendamento1 = {
                pacienteId = pacienteResponse.id
                profissionalId = profissionalResponse.id
                tipoAgendamento = "CONSULTA"
                dataHora = futureDate
                duracao = "00:30"
                unidadeId = 1
                planoSaudeCobertura = false
            }
            
            // Create first appointment
            let! (status1, _) = postAsync "/api/v1/agendamentos" agendamento1
            Expect.equal status1 System.Net.HttpStatusCode.Created "First appointment should be created"
            
            // Try to create conflicting appointment
            let agendamento2 = { agendamento1 with dataHora = futureDate.AddMinutes(15) }
            let! (status2, _) = postAsync "/api/v1/agendamentos" agendamento2
            Expect.equal status2 System.Net.HttpStatusCode.Conflict "Should prevent double booking"
        }

        testAsync "Should check professional availability" {
            let futureDate = DateTime.Now.AddDays(2).Date.AddHours(14)
            let futureDateStr = futureDate.ToString("yyyy-MM-ddTHH:mm:ss")
            let! (statusCode, content) = getAsync $"/api/v1/agendamentos/profissional/1/disponibilidade?dataHora={futureDateStr}&duracao=00:30"
            Expect.equal statusCode System.Net.HttpStatusCode.OK "Should return availability info"
        }
    ]

// =====================================================
// TELEMEDICINE TESTS
// =====================================================
// [<Tests>]
let telemedicinTests =
    testList "Telemedicina API Tests" [
        testAsync "Should create telemedicine session" {
            let sessao = {|
                agendamentoId = 1
                pacienteId = 1
                profissionalId = 1
                gravacaoPermitida = true
                plataformaVideo = "JITSI"
                observacoesIniciais = "Consulta de retorno"
            |}
            
            let! (statusCode, content) = postAsync "/api/v1/telemedicina" sessao
            Expect.equal statusCode System.Net.HttpStatusCode.Created "Should create session"
            
            let response = JsonConvert.DeserializeAnonymousType(content, {| id = 0; linkSessao = ""; senhaPaciente = "" |})
            Expect.isNotEmpty response.linkSessao "Should generate session link"
            Expect.isNotEmpty response.senhaPaciente "Should generate patient password"
        }

        testAsync "Should get telemedicine dashboard" {
            let! (statusCode, content) = getAsync "/api/v1/telemedicina/dashboard"
            Expect.equal statusCode System.Net.HttpStatusCode.OK "Should return dashboard"
        }
    ]

// =====================================================
// ADMINISTRATION TESTS
// =====================================================
// [<Tests>]
let administracaoTests =
    testList "Administração API Tests" [
        testAsync "Should get hospital units" {
            let! (statusCode, content) = getAsync "/api/v1/admin/unidades"
            Expect.equal statusCode System.Net.HttpStatusCode.OK "Should return units"
        }

        testAsync "Should get bed details" {
            let! (statusCode, content) = getAsync "/api/v1/admin/leitos/detalhes"
            Expect.equal statusCode System.Net.HttpStatusCode.OK "Should return bed details"
        }

        testAsync "Should get dashboard" {
            let! (statusCode, content) = getAsync "/api/v1/admin/dashboard"
            Expect.equal statusCode System.Net.HttpStatusCode.OK "Should return admin dashboard"
        }

        testAsync "Should manage bed status" {
            let statusUpdate = {| status = "LIMPEZA"; observacoes = "Limpeza pós alta" |}
            let! (statusCode, content) = putAsync "/api/v1/admin/leitos/1/status" statusUpdate
            Expect.equal statusCode System.Net.HttpStatusCode.OK "Should update bed status"
        }
    ]

// =====================================================
// MEDICAL RECORDS TESTS
// =====================================================
// [<Tests>]
let prontuarioTests =
    testList "Prontuário API Tests" [
        testAsync "Should create medical record with prescriptions" {
            let prontuario = {|
                pacienteId = 1
                profissionalId = 1
                dataAtendimento = DateTime.Now
                tipoAtendimento = "CONSULTA"
                queixaPrincipal = "Dor de cabeça"
                historiaDoencaAtual = "Paciente relata dor de cabeça há 2 dias"
                exameFisico = "Paciente em bom estado geral"
                hipoteses = ["Cefaleia tensional"]
                cid10 = "G44.2"
                unidadeId = 1
                prescricoes = [
                    {|
                        medicamento = "Paracetamol 750mg"
                        dosagem = "1 comprimido"
                        frequencia = "8/8 horas"
                        duracao = "3 dias"
                        orientacoes = "Tomar com água"
                    |}
                ]
                examesSolicitados = []
                procedimentos = []
            |}
            
            let! (statusCode, content) = postAsync "/api/v1/prontuarios" prontuario
            Expect.equal statusCode System.Net.HttpStatusCode.Created "Should create medical record"
        }

        testAsync "Should get patient medical history" {
            let! (statusCode, content) = getAsync "/api/v1/prontuarios/paciente/1/historico"
            Expect.equal statusCode System.Net.HttpStatusCode.OK "Should return patient history"
        }
    ]

// =====================================================
// ALL TESTS COLLECTION
// =====================================================
[<Tests>]
let allTests =
    testList "SGHSS API Integration Tests" [
        pacienteTests
        profissionalTests
        agendamentoTests
        telemedicinTests
        administracaoTests
        prontuarioTests
    ]
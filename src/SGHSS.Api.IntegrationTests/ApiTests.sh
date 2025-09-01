#!/bin/bash

set -e

BASE_URL="http://localhost:8000"
TEST_DIR="$(dirname "$0")/test_data"
mkdir -p $TEST_DIR


RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # sem cor

# Test counter
TESTS_PASSED=0
TESTS_FAILED=0


log_info() {
  echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
  echo -e "${GREEN}[PASS]${NC} $1"
  ((TESTS_PASSED++))
}

log_error() {
  echo -e "${RED}[FAIL]${NC} $1"
  ((TESTS_FAILED++))
}

log_warning() {
  echo -e "${YELLOW}[WARN]${NC} $1"
}


test_api_health() {
  log_info "Testando a saúde da API..."
  if curl -f -s "$BASE_URL" >/dev/null; then
    log_success "API esta rodando"
    return 0
  else
    log_error "API não esta rodando no endereço $BASE_URL"
    return 1
  fi
}


test_patients() {
  log_info "Testando endpoints dos pacientes..."

  
  PATIENT_DATA='{
        "nome": "João Silva Teste",
        "cpf": "12345678901",
        "dataNascimento": "1990-05-15T00:00:00",
        "sexo": "M",
        "telefone": "11999887766",
        "endereco": {
            "logradouro": "Rua de Teste",
            "numero": "123",
            "bairro": "Centro",
            "cidade": "São Paulo",
            "estado": "SP",
            "cep": "01234567",
            "pais": "Brasil"
        }
    }'

  # POST /api/v1/pacientes
  RESPONSE=$(curl -s -w "\n%{http_code}" -X POST \
    -H "Content-Type: application/json" \
    -d "$PATIENT_DATA" \
    "$BASE_URL/api/v1/pacientes")

  HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
  BODY=$(echo "$RESPONSE" | head -n -1)

  if [ "$HTTP_CODE" = "201" ]; then
    PATIENT_ID=$(echo "$BODY" | grep -o '"id":[0-9]*' | cut -d':' -f2)
    echo "$PATIENT_ID" >"$TEST_DIR/patient_id.txt"
    log_success "Paciente criado com ID: $PATIENT_ID"
  else
    log_error "Falha na criação do paciente (HTTP: $HTTP_CODE): $BODY"
    return 1
  fi

  # GET /api/v1/pacientes
  RESPONSE=$(curl -s -w "\n%{http_code}" "$BASE_URL/api/v1/pacientes")
  HTTP_CODE=$(echo "$RESPONSE" | tail -n1)

  if [ "$HTTP_CODE" = "200" ]; then
    log_success "Listar todos os pacientes"
  else
    log_error "Falha ao pegar os pacientes (HTTP: $HTTP_CODE)"
  fi

  # GET /api/v1/pacientes/{id}
  if [ -f "$TEST_DIR/patient_id.txt" ]; then
    PATIENT_ID=$(cat "$TEST_DIR/patient_id.txt")
    RESPONSE=$(curl -s -w "\n%{http_code}" "$BASE_URL/api/v1/pacientes/$PATIENT_ID")
    HTTP_CODE=$(echo "$RESPONSE" | tail -n1)

    if [ "$HTTP_CODE" = "200" ]; then
      log_success "Consulta de paciente por ID: $PATIENT_ID"
    else
      log_error "Falha ao tentar pegar os pacientes por ID (HTTP: $HTTP_CODE)"
    fi
  fi
}

test_professionals() {
  log_info "Testando os endpoints dos profissionais..."
  
  PROFESSIONAL_DATA='{
    "nome": "Dra. Maria Teste",
    "cpf": "98765432100",
    "crm": "CRM123456",
    "tipoProfissional": "MEDICO",
    "email": "maria.teste@hospital.com",
    "telefone": "11988776655",
    "dataAdmissao": "'$(date -I)'T00:00:00",
    "unidadeId": 1,
    "permiteTelemedicina": true
  }'

  # POST /api/v1/profissionais
  RESPONSE=$(curl -s -w "\n%{http_code}" -X POST \
    -H "Content-Type: application/json" \
    -d "$PROFESSIONAL_DATA" \
    "$BASE_URL/api/v1/profissionais")

  HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
  BODY=$(echo "$RESPONSE" | head -n -1)

  if [ "$HTTP_CODE" = "201" ]; then
    PROFESSIONAL_ID=$(echo "$BODY" | grep -o '"id":[0-9]*' | cut -d':' -f2)
    echo "$PROFESSIONAL_ID" >"$TEST_DIR/professional_id.txt"
    log_success "Criou um profissional com ID: $PROFESSIONAL_ID"
  else
    log_error "Falha ao tentar criar profissional (HTTP: $HTTP_CODE): $BODY"
    return 1
  fi

  # GET /api/v1/profissionais
  RESPONSE=$(curl -s -w "\n%{http_code}" "$BASE_URL/api/v1/profissionais")
  HTTP_CODE=$(echo "$RESPONSE" | tail -n1)

  if [ "$HTTP_CODE" = "200" ]; then
    log_success "Listagem de todos os profissionais"
  else
    log_error "Falha ao testar listar profissionais (HTTP: $HTTP_CODE)"
  fi

  # GET /api/v1/profissionais/tipo/MEDICO
  RESPONSE=$(curl -s -w "\n%{http_code}" "$BASE_URL/api/v1/profissionais/tipo/MEDICO")
  HTTP_CODE=$(echo "$RESPONSE" | tail -n1)

  if [ "$HTTP_CODE" = "200" ]; then
    log_success "Listando profissionais por tipo"
  else
    log_error "Falha ao tentar consultar os profissionais por tipo (HTTP: $HTTP_CODE)"
  fi
}

test_appointments() {
  log_info "Testando os endpoints de agendamento..."
  
  if [ ! -f "$TEST_DIR/patient_id.txt" ] || [ ! -f "$TEST_DIR/professional_id.txt" ]; then
    log_warning "Pulando os testes de agendamento - Paciente ou profissional faltando"
    return 0
  fi

  PATIENT_ID=$(cat "$TEST_DIR/patient_id.txt")
  PROFESSIONAL_ID=$(cat "$TEST_DIR/professional_id.txt")
  FUTURE_DATE=$(date -d "+1 day" -I)"T10:00:00"
  
  APPOINTMENT_DATA='{
    "pacienteId": '$PATIENT_ID',
    "profissionalId": '$PROFESSIONAL_ID',
    "tipoAgendamento": "CONSULTA",
    "dataHora": "'$FUTURE_DATE'",
    "duracao": "00:30",
    "unidadeId": 1,
    "planoSaudeCobertura": false
  }'

  # POST /api/v1/agendamentos
  RESPONSE=$(curl -s -w "\n%{http_code}" -X POST \
    -H "Content-Type: application/json" \
    -d "$APPOINTMENT_DATA" \
    "$BASE_URL/api/v1/agendamentos")

  HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
  BODY=$(echo "$RESPONSE" | head -n -1)

  if [ "$HTTP_CODE" = "201" ]; then
    APPOINTMENT_ID=$(echo "$BODY" | grep -o '"id":[0-9]*' | cut -d':' -f2)
    echo "$APPOINTMENT_ID" >"$TEST_DIR/appointment_id.txt"
    log_success "Created appointment with ID: $APPOINTMENT_ID"
  else
    log_error "Failed to create appointment (HTTP: $HTTP_CODE): $BODY"
  fi

  # GET /api/v1/agendamentos
  RESPONSE=$(curl -s -w "\n%{http_code}" "$BASE_URL/api/v1/agendamentos")
  HTTP_CODE=$(echo "$RESPONSE" | tail -n1)

  if [ "$HTTP_CODE" = "200" ]; then
    log_success "Listagem de todos os agendamentos"
  else
    log_error "Falha na consulta dos agendamentos (HTTP: $HTTP_CODE)"
  fi
  
  AVAILABILITY_URL="$BASE_URL/api/v1/agendamentos/profissional/$PROFESSIONAL_ID/disponibilidade?dataHora=$FUTURE_DATE&duracao=00:30"
  RESPONSE=$(curl -s -w "\n%{http_code}" "$AVAILABILITY_URL")
  HTTP_CODE=$(echo "$RESPONSE" | tail -n1)

  if [ "$HTTP_CODE" = "200" ]; then
    log_success "Status da disponibilidade de profissional"
  else
    log_error "Falha na consulta da disponibilidade do profissional (HTTP: $HTTP_CODE)"
  fi
}


test_telemedicine() {
  log_info "Testando os endpoints de Telemedicina..."

  # GET /api/v1/telemedicina/dashboard
  # RESPONSE=$(curl -s -w "\n%{http_code}" "$BASE_URL/api/v1/telemedicina/dashboard")
  # HTTP_CODE=$(echo "$RESPONSE" | tail -n1)

  # if [ "$HTTP_CODE" = "200" ]; then
  #   log_success "Consulta do dashboard de telemedicina"
  # else
  #   log_error "Falha ao consultar dashboard de telemedicina (HTTP: $HTTP_CODE)"
  # fi

  # Test professional configuration
  if [ -f "$TEST_DIR/professional_id.txt" ]; then
    PROFESSIONAL_ID=$(cat "$TEST_DIR/professional_id.txt")

    # Check if professional has telemedicine config
    RESPONSE=$(curl -s -w "\n%{http_code}" "$BASE_URL/api/v1/telemedicina/profissional/$PROFESSIONAL_ID/configuracoes")
    HTTP_CODE=$(echo "$RESPONSE" | tail -n1)

    if [ "$HTTP_CODE" = "200" ] || [ "$HTTP_CODE" = "404" ]; then
      log_success "Verificação da configuração de telemedicina (HTTP: $HTTP_CODE)"
    else
      log_error "Falha na verificação do configuração de telemedicina (HTTP: $HTTP_CODE)"
    fi
  fi
}

test_administration() {
  log_info "Testando endpoints de administração..."

  # GET /api/v1/admin/dashboard
  RESPONSE=$(curl -s -w "\n%{http_code}" "$BASE_URL/api/v1/admin/dashboard")
  HTTP_CODE=$(echo "$RESPONSE" | tail -n1)

  if [ "$HTTP_CODE" = "200" ]; then
    log_success "Consulta do dashboard de Admin"
  else
    log_error "Falha na consulta dos dados do dashboard de admin (HTTP: $HTTP_CODE)"
  fi

  # GET /api/v1/admin/unidades
  RESPONSE=$(curl -s -w "\n%{http_code}" "$BASE_URL/api/v1/admin/unidades")
  HTTP_CODE=$(echo "$RESPONSE" | tail -n1)

  if [ "$HTTP_CODE" = "200" ]; then
    log_success "Listagem de todas as unidades hospitalares"
  else
    log_error "Falha ao listar unidades hospitalares (HTTP: $HTTP_CODE)"
  fi

  # GET /api/v1/admin/leitos/detalhes
  RESPONSE=$(curl -s -w "\n%{http_code}" "$BASE_URL/api/v1/admin/leitos/detalhes")
  HTTP_CODE=$(echo "$RESPONSE" | tail -n1)

  if [ "$HTTP_CODE" = "200" ]; then
    log_success "Listagem dos detalhes dos leitos"
  else
    log_error "Falha ao listar detalhes dos leitos (HTTP: $HTTP_CODE)"
  fi

  # GET /api/v1/admin/suprimentos
  RESPONSE=$(curl -s -w "\n%{http_code}" "$BASE_URL/api/v1/admin/suprimentos")
  HTTP_CODE=$(echo "$RESPONSE" | tail -n1)

  if [ "$HTTP_CODE" = "200" ]; then
    log_success "Listagem de todos os suprimentos"
  else
    log_error "Falha ao listar suprimentos (HTTP: $HTTP_CODE)"
  fi
}

test_medical_records() {
  log_info "Testando endpoints de prontuários médicos..."

  # GET /api/v1/prontuarios
  RESPONSE=$(curl -s -w "\n%{http_code}" "$BASE_URL/api/v1/prontuarios")
  HTTP_CODE=$(echo "$RESPONSE" | tail -n1)

  if [ "$HTTP_CODE" = "200" ]; then
    log_success "Listagem de todos os prontuários médicos"
  else
    log_error "Falha ao listar prontuários médicos (HTTP: $HTTP_CODE)"
  fi
  
  if [ -f "$TEST_DIR/patient_id.txt" ]; then
    PATIENT_ID=$(cat "$TEST_DIR/patient_id.txt")
    RESPONSE=$(curl -s -w "\n%{http_code}" "$BASE_URL/api/v1/prontuarios/paciente/$PATIENT_ID/historico")
    HTTP_CODE=$(echo "$RESPONSE" | tail -n1)

    if [ "$HTTP_CODE" = "200" ]; then
      log_success "Retrieved patient medical history"
    else
      log_error "Failed to get patient history (HTTP: $HTTP_CODE)"
    fi
  fi
}

test_performance() {
  log_info "Executando testes de performance..."
  
  START_TIME=$(date +%s%N)
  curl -s "$BASE_URL/api/v1/admin/dashboard" >/dev/null
  END_TIME=$(date +%s%N)
  DURATION=$((($END_TIME - $START_TIME) / 1000000)) # Convert to milliseconds

  if [ $DURATION -lt 2000 ]; then # Less than 2 seconds
    log_success "Tempo de resposta do dashboard: ${DURATION}ms"
  else
    log_warning "Tempo de resposta do dashboard alto: ${DURATION}ms"
  fi

  
  log_info "Testando múltiplas requisições simultâneas..."
  for i in {1..5}; do
    curl -s "$BASE_URL/api/v1/pacientes" >/dev/null &
  done
  wait
  log_success "Requisições simultâneas completadas"
}

main() {
  log_info "Começando os testes de integração da API..."
  log_info "Testando API na URL: $BASE_URL"
  echo
  
  if ! test_api_health; then
    log_error "API não está acessível. Abortando testes."
    exit 1
  fi

  echo

  
  test_patients
  echo

  test_professionals
  echo

  test_appointments
  echo

  test_telemedicine
  echo

  test_administration
  echo

  test_medical_records
  echo

  test_performance
  echo

  
  TOTAL_TESTS=$((TESTS_PASSED + TESTS_FAILED))
  log_info "Resumo:"
  echo -e "  ${GREEN}Sucesso: $TESTS_PASSED${NC}"
  echo -e "  ${RED}Falha: $TESTS_FAILED${NC}"
  echo -e "  Total:  $TOTAL_TESTS"

  if [ $TESTS_FAILED -eq 0 ]; then
    log_success "Todos os testes passaram! 🎉"
    exit 0
  else
    log_error "$TESTS_FAILED de $TOTAL_TESTS testes falharam."
    exit 1
  fi
}

cleanup() {
  log_info "Limpando dados de teste..."
  rm -rf "$TEST_DIR"
  log_success "Dados de teste limpos!"
}

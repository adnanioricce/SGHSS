#!/bin/bash
# test_sghss_api.sh - Simple API Testing Script

set -e # Exit on any error

BASE_URL="http://localhost:8000"
TEST_DIR="$(dirname "$0")/test_data"
mkdir -p $TEST_DIR

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test counter
TESTS_PASSED=0
TESTS_FAILED=0

# Helper functions
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

# Test if API is running
test_api_health() {
  log_info "Testing API health..."
  if curl -f -s "$BASE_URL" >/dev/null; then
    log_success "API is running"
    return 0
  else
    log_error "API is not running on $BASE_URL"
    return 1
  fi
}

# Test patient endpoints
test_patients() {
  log_info "Testing Patient endpoints..."

  # Create test patient
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
    log_success "Created patient with ID: $PATIENT_ID"
  else
    log_error "Failed to create patient (HTTP: $HTTP_CODE): $BODY"
    return 1
  fi

  # GET /api/v1/pacientes
  RESPONSE=$(curl -s -w "\n%{http_code}" "$BASE_URL/api/v1/pacientes")
  HTTP_CODE=$(echo "$RESPONSE" | tail -n1)

  if [ "$HTTP_CODE" = "200" ]; then
    log_success "Retrieved all patients"
  else
    log_error "Failed to get patients (HTTP: $HTTP_CODE)"
  fi

  # GET /api/v1/pacientes/{id}
  if [ -f "$TEST_DIR/patient_id.txt" ]; then
    PATIENT_ID=$(cat "$TEST_DIR/patient_id.txt")
    RESPONSE=$(curl -s -w "\n%{http_code}" "$BASE_URL/api/v1/pacientes/$PATIENT_ID")
    HTTP_CODE=$(echo "$RESPONSE" | tail -n1)

    if [ "$HTTP_CODE" = "200" ]; then
      log_success "Retrieved patient by ID: $PATIENT_ID"
    else
      log_error "Failed to get patient by ID (HTTP: $HTTP_CODE)"
    fi
  fi
}

# Test professional endpoints
test_professionals() {
  log_info "Testing Professional endpoints..."

  # Create test professional
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
    log_success "Created professional with ID: $PROFESSIONAL_ID"
  else
    log_error "Failed to create professional (HTTP: $HTTP_CODE): $BODY"
    return 1
  fi

  # GET /api/v1/profissionais
  RESPONSE=$(curl -s -w "\n%{http_code}" "$BASE_URL/api/v1/profissionais")
  HTTP_CODE=$(echo "$RESPONSE" | tail -n1)

  if [ "$HTTP_CODE" = "200" ]; then
    log_success "Retrieved all professionals"
  else
    log_error "Failed to get professionals (HTTP: $HTTP_CODE)"
  fi

  # GET /api/v1/profissionais/tipo/MEDICO
  RESPONSE=$(curl -s -w "\n%{http_code}" "$BASE_URL/api/v1/profissionais/tipo/MEDICO")
  HTTP_CODE=$(echo "$RESPONSE" | tail -n1)

  if [ "$HTTP_CODE" = "200" ]; then
    log_success "Retrieved professionals by type"
  else
    log_error "Failed to get professionals by type (HTTP: $HTTP_CODE)"
  fi
}

# Test appointment endpoints
test_appointments() {
  log_info "Testing Appointment endpoints..."

  # Check if we have patient and professional IDs
  if [ ! -f "$TEST_DIR/patient_id.txt" ] || [ ! -f "$TEST_DIR/professional_id.txt" ]; then
    log_warning "Skipping appointment tests - missing patient or professional"
    return 0
  fi

  PATIENT_ID=$(cat "$TEST_DIR/patient_id.txt")
  PROFESSIONAL_ID=$(cat "$TEST_DIR/professional_id.txt")
  FUTURE_DATE=$(date -d "+1 day" -I)"T10:00:00"

  # Create test appointment
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
    log_success "Retrieved all appointments"
  else
    log_error "Failed to get appointments (HTTP: $HTTP_CODE)"
  fi

  # Check availability
  AVAILABILITY_URL="$BASE_URL/api/v1/agendamentos/profissional/$PROFESSIONAL_ID/disponibilidade?dataHora=$FUTURE_DATE&duracao=00:30"
  RESPONSE=$(curl -s -w "\n%{http_code}" "$AVAILABILITY_URL")
  HTTP_CODE=$(echo "$RESPONSE" | tail -n1)

  if [ "$HTTP_CODE" = "200" ]; then
    log_success "Checked professional availability"
  else
    log_error "Failed to check availability (HTTP: $HTTP_CODE)"
  fi
}

# Test telemedicine endpoints
test_telemedicine() {
  log_info "Testing Telemedicine endpoints..."

  # GET /api/v1/telemedicina/dashboard
  RESPONSE=$(curl -s -w "\n%{http_code}" "$BASE_URL/api/v1/telemedicina/dashboard")
  HTTP_CODE=$(echo "$RESPONSE" | tail -n1)

  if [ "$HTTP_CODE" = "200" ]; then
    log_success "Retrieved telemedicine dashboard"
  else
    log_error "Failed to get telemedicine dashboard (HTTP: $HTTP_CODE)"
  fi

  # Test professional configuration
  if [ -f "$TEST_DIR/professional_id.txt" ]; then
    PROFESSIONAL_ID=$(cat "$TEST_DIR/professional_id.txt")

    # Check if professional has telemedicine config
    RESPONSE=$(curl -s -w "\n%{http_code}" "$BASE_URL/api/v1/telemedicina/profissional/$PROFESSIONAL_ID/configuracoes")
    HTTP_CODE=$(echo "$RESPONSE" | tail -n1)

    if [ "$HTTP_CODE" = "200" ] || [ "$HTTP_CODE" = "404" ]; then
      log_success "Checked telemedicine configuration (HTTP: $HTTP_CODE)"
    else
      log_error "Failed to check telemedicine config (HTTP: $HTTP_CODE)"
    fi
  fi
}

# Test administration endpoints
test_administration() {
  log_info "Testing Administration endpoints..."

  # GET /api/v1/admin/dashboard
  RESPONSE=$(curl -s -w "\n%{http_code}" "$BASE_URL/api/v1/admin/dashboard")
  HTTP_CODE=$(echo "$RESPONSE" | tail -n1)

  if [ "$HTTP_CODE" = "200" ]; then
    log_success "Retrieved admin dashboard"
  else
    log_error "Failed to get admin dashboard (HTTP: $HTTP_CODE)"
  fi

  # GET /api/v1/admin/unidades
  RESPONSE=$(curl -s -w "\n%{http_code}" "$BASE_URL/api/v1/admin/unidades")
  HTTP_CODE=$(echo "$RESPONSE" | tail -n1)

  if [ "$HTTP_CODE" = "200" ]; then
    log_success "Retrieved hospital units"
  else
    log_error "Failed to get hospital units (HTTP: $HTTP_CODE)"
  fi

  # GET /api/v1/admin/leitos/detalhes
  RESPONSE=$(curl -s -w "\n%{http_code}" "$BASE_URL/api/v1/admin/leitos/detalhes")
  HTTP_CODE=$(echo "$RESPONSE" | tail -n1)

  if [ "$HTTP_CODE" = "200" ]; then
    log_success "Retrieved bed details"
  else
    log_error "Failed to get bed details (HTTP: $HTTP_CODE)"
  fi

  # GET /api/v1/admin/suprimentos
  RESPONSE=$(curl -s -w "\n%{http_code}" "$BASE_URL/api/v1/admin/suprimentos")
  HTTP_CODE=$(echo "$RESPONSE" | tail -n1)

  if [ "$HTTP_CODE" = "200" ]; then
    log_success "Retrieved supplies"
  else
    log_error "Failed to get supplies (HTTP: $HTTP_CODE)"
  fi
}

# Test medical records endpoints
test_medical_records() {
  log_info "Testing Medical Records endpoints..."

  # GET /api/v1/prontuarios
  RESPONSE=$(curl -s -w "\n%{http_code}" "$BASE_URL/api/v1/prontuarios")
  HTTP_CODE=$(echo "$RESPONSE" | tail -n1)

  if [ "$HTTP_CODE" = "200" ]; then
    log_success "Retrieved medical records"
  else
    log_error "Failed to get medical records (HTTP: $HTTP_CODE)"
  fi

  # Test patient history if we have a patient ID
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

# Performance test
test_performance() {
  log_info "Running basic performance tests..."

  # Test response time for dashboard
  START_TIME=$(date +%s%N)
  curl -s "$BASE_URL/api/v1/admin/dashboard" >/dev/null
  END_TIME=$(date +%s%N)
  DURATION=$((($END_TIME - $START_TIME) / 1000000)) # Convert to milliseconds

  if [ $DURATION -lt 2000 ]; then # Less than 2 seconds
    log_success "Dashboard response time: ${DURATION}ms"
  else
    log_warning "Dashboard response time slow: ${DURATION}ms"
  fi

  # Test concurrent requests
  log_info "Testing concurrent requests..."
  for i in {1..5}; do
    curl -s "$BASE_URL/api/v1/pacientes" >/dev/null &
  done
  wait
  log_success "Completed concurrent request test"
}

# Main test execution
main() {
  log_info "Starting SGHSS API Tests..."
  log_info "Testing API at: $BASE_URL"
  echo

  # Test API health first
  if ! test_api_health; then
    log_error "API health check failed. Make sure the API is running."
    exit 1
  fi

  echo

  # Run all tests
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

  # Test summary
  TOTAL_TESTS=$((TESTS_PASSED + TESTS_FAILED))
  log_info "Test Summary:"
  echo -e "  ${GREEN}Passed: $TESTS_PASSED${NC}"
  echo -e "  ${RED}Failed: $TESTS_FAILED${NC}"
  echo -e "  Total:  $TOTAL_TESTS"

  if [ $TESTS_FAILED -eq 0 ]; then
    log_success "All tests passed! 🎉"
    exit 0
  else
    log_error "Some tests failed. Check the output above."
    exit 1
  fi
}

# Cleanup function
cleanup() {
  log_info "Cleaning up test data..."
  rm -rf "$TEST_DIR"
  log_success "Cleanup completed"
}

# Set trap to cleanup on exit

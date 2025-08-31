namespace Infrastructure.Security

open System
open System.Data.Common
open System.Security.Claims
// adicione os pacotes se necessário
//open Microsoft.AspNetCore.Authentication.JwtBearer
//open Microsoft.IdentityModel.Tokens
open System.Text
open SGHSS.Api
open System
open System.Security.Claims
open System.Text
open System.IdentityModel.Tokens.Jwt
open Microsoft.IdentityModel.Tokens
open BCrypt.Net
open SGHSS.Api.Logging
module Authentication =
    type UserClaims = {
        UserId: int
        Nome: string
        Email: string
        Roles: string list
        UnidadeId: Nullable<int>         
        ProfissionalId: Nullable<int>
        PacienteId: Nullable<int>
    }
    
    type UserRole = 
        | Administrador
        | Medico
        | Enfermeiro
        | Recepcionista
        | Paciente
        | Auditoria
        | Farmacia
        | Laboratorio
    // Login models
    type LoginRequest = {
        Email: string
        Password: string
        RememberMe: bool
    }
    
    type LoginResponse = {
        Token: string
        RefreshToken: string
        ExpiresAt: DateTime
        User: {|
            Id: int
            Nome: string
            Email: string
            Roles: string list
            UnidadeId: int option
        |}
    }
    
    type RefreshTokenRequest = {
        Token: string
        RefreshToken: string
    }
    
    // Password management
    type ChangePasswordRequest = {
        CurrentPassword: string
        NewPassword: string
        ConfirmPassword: string
    }
    
    type ResetPasswordRequest = {
        Email: string
        Token: string
        NewPassword: string
    }
    
    // JWT Configuration
    type JwtConfig = {
        SecretKey: string
        Issuer: string
        Audience: string
        ExpireMinutes: int
        RefreshExpireDays: int
    }
    // Get JWT configuration from environment or defaults
    let getJwtConfig () =
        {
            SecretKey = Environment.GetEnvironmentVariable("JWT_SECRET") |> Utils.defaultIfNull "your-super-secret-jwt-key-that-should-be-at-least-256-bits-long"
            Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") |> Utils.defaultIfNull "SGHSS.Api"
            Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") |> Utils.defaultIfNull "SGHSS.Client"
            ExpireMinutes = 
                match Environment.GetEnvironmentVariable("JWT_EXPIRE_MINUTES") with
                | null -> 60
                | value -> Int32.TryParse(value) |> function | (true, v) -> v | _ -> 60
            RefreshExpireDays =
                match Environment.GetEnvironmentVariable("JWT_REFRESH_DAYS") with
                | null -> 7
                | value -> Int32.TryParse(value) |> function | (true, v) -> v | _ -> 7
        }
    
    // Password hashing functions
    let hashPassword (customSalt:string option) (password: string) =
        let salt = customSalt |> Option.defaultValue (BCrypt.GenerateSalt(12))
        BCrypt.HashPassword(password, salt)
    
    let verifyPassword (password: string) (hash: string) =
        try
            BCrypt.Verify(password, hash)
        with
        | _ -> false
    
    let generateToken (claims: UserClaims) =
        let config = getJwtConfig()
        let securityKey = SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.SecretKey))
        let credentials = SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256)
        
        let jwtClaims =
            [
                Claim(ClaimTypes.NameIdentifier, claims.UserId.ToString())
                Claim(ClaimTypes.Name, claims.Nome)
                Claim(ClaimTypes.Email, claims.Email)
                Claim("unidade_id", claims.UnidadeId |> Utils.toOptionIfNullable |> Option.map string |> Option.defaultValue "")
                Claim("profissional_id", claims.ProfissionalId |> Utils.toOptionIfNullable |> Option.map string |> Option.defaultValue "")
                Claim("paciente_id", claims.PacienteId |> Utils.toOptionIfNullable |> Option.map string |> Option.defaultValue "")
            ] @ (claims.Roles |> List.map (fun role -> Claim(ClaimTypes.Role, role)))
        
        let token = JwtSecurityToken(
            issuer = config.Issuer,
            audience = config.Audience,
            claims = jwtClaims,
            expires = DateTime.UtcNow.AddMinutes(float config.ExpireMinutes),
            signingCredentials = credentials
        )
        
        JwtSecurityTokenHandler().WriteToken(token)
    
    let validateToken (token: string) =        
        try
            let config = getJwtConfig()
            let tokenHandler = JwtSecurityTokenHandler()
            let key = Encoding.UTF8.GetBytes(config.SecretKey)
            
            let validationParameters = TokenValidationParameters(
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = config.Issuer,
                ValidateAudience = true,
                ValidAudience = config.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            )
            
            let principal = tokenHandler.ValidateToken(token, validationParameters, ref null)
            
            let getUserClaim (name:string) =
                let claim = principal.FindFirst(name)
                if claim |> isNull then
                    ""
                else 
                    claim.Value
            
            let getRoles () =
                principal.FindAll(ClaimTypes.Role)
                |> Seq.map (fun c -> c.Value)
                |> List.ofSeq
            
            let getOptionalIntClaim name =
                match getUserClaim name with
                | null | "" -> None
                | value -> Int32.TryParse(value) |> function | (true, v) -> Some v | _ -> None
            
            let userClaims = {
                UserId = Int32.Parse(getUserClaim ClaimTypes.NameIdentifier)
                Nome = getUserClaim ClaimTypes.Name
                Email = getUserClaim ClaimTypes.Email
                Roles = getRoles()
                UnidadeId = getOptionalIntClaim "unidade_id" |> Option.defaultValue -1 |> Nullable
                ProfissionalId = getOptionalIntClaim "profissional_id" |> Option.defaultValue -1 |> Nullable
                PacienteId = getOptionalIntClaim "paciente_id" |> Option.defaultValue -1 |> Nullable
            }
            
            Result.Ok userClaims
        with
        | ex ->
            Logger.logger.Error("Um erro ocorreu ao tentar validar o JWT: {ex}",ex)
            Result.Error ex
    // Role string conversion
    let roleToString = function
        | Administrador -> "ADMINISTRADOR"
        | Medico -> "MEDICO"
        | Enfermeiro -> "ENFERMEIRO"
        | Recepcionista -> "RECEPCIONISTA"
        | Paciente -> "PACIENTE"
        | Auditoria -> "AUDITORIA"
        | Farmacia -> "FARMACIA"
        | Laboratorio -> "LABORATORIO"
    
    let stringToRole = function
        | "ADMINISTRADOR" -> Some Administrador
        | "MEDICO" -> Some Medico
        | "ENFERMEIRO" -> Some Enfermeiro
        | "RECEPCIONISTA" -> Some Recepcionista
        | "PACIENTE" -> Some Paciente
        | "AUDITORIA" -> Some Auditoria
        | "FARMACIA" -> Some Farmacia
        | "LABORATORIO" -> Some Laboratorio
        | _ -> None
    // Permission checking
    let hasPermission (requiredRole: UserRole) (userClaims: UserClaims) =
        let requiredRoleString = roleToString requiredRole
        let adminRoleString = roleToString (UserRole.Administrador)
        userClaims.Roles
        |> List.map (fun role -> role.ToUpper())        
        |> List.exists (fun e -> e = requiredRoleString || e = adminRoleString)
    
    let hasAnyPermission (requiredRoles: UserRole list) (userClaims: UserClaims) =
        requiredRoles |> List.exists (fun role -> hasPermission role userClaims)
    
    let isAdminOrOwner (resourceUserId: int) (userClaims: UserClaims) =
        hasPermission Administrador userClaims || userClaims.UserId = resourceUserId
    
    // Generate refresh token
    let generateRefreshToken () =
        let randomBytes = Array.zeroCreate 64
        use rng = System.Security.Cryptography.RandomNumberGenerator.Create()
        rng.GetBytes(randomBytes)
        Convert.ToBase64String(randomBytes)
    
    // Password strength validation
    let validatePasswordStrength (password: string) =
        let errors = ResizeArray<string>()
        
        if password.Length < 8 then
            errors.Add("Senha deve ter pelo menos 8 caracteres")
        
        if not (password |> Seq.exists Char.IsUpper) then
            errors.Add("Senha deve conter pelo menos uma letra maiúscula")
        
        if not (password |> Seq.exists Char.IsLower) then
            errors.Add("Senha deve conter pelo menos uma letra minúscula")
        
        if not (password |> Seq.exists Char.IsDigit) then
            errors.Add("Senha deve conter pelo menos um número")
        
        if not (password |> Seq.exists (fun c -> "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(c))) then
            errors.Add("Senha deve conter pelo menos um caractere especial")
        
        errors |> List.ofSeq

module UserRepository =
    open Npgsql.FSharp
    open Authentication
    open System
    open Infrastructure.Database    
    type UserAccount = {
        Id: int
        Username: string
        PasswordHash: string
        Email: string
        NomeCompleto: string
        Ativo: bool
        UltimoLogin: DateTimeOffset option
        TentativasLogin: int
        BloqueadoAte: DateTimeOffset option
        ProfissionalId: int option
        PacienteId: int option
        UnidadeId: int option
        DataCadastro: DateTimeOffset
        Roles: string list
    }

    type RefreshTokenData = {
        Id: int
        Token: string
        UserId: int
        ExpiresAt: DateTimeOffset
        CreatedAt: DateTimeOffset
        IsRevoked: bool
    }
    // Create user account
    let createUser (email: string) (password: string) (nomeCompleto: string) (profissionalId: int option) (pacienteId: int option) =
        task {
            let passwordHash = hashPassword None password
            let username = email // Use email as username
            
            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    INSERT INTO usuarios_sistema 
                    (username, password_hash, salt, email, nome_completo, profissional_id, paciente_id)
                    VALUES (@username, @password_hash, @salt, @email, @nome_completo, @profissional_id, @paciente_id)
                    RETURNING id
                """
                |> Sql.parameters [
                    "username", Sql.string username
                    "password_hash", Sql.string passwordHash
                    "salt", Sql.string "bcrypt_handled" // BCrypt handles salt internally
                    "email", Sql.string email
                    "nome_completo", Sql.string nomeCompleto
                    "profissional_id", Sql.intOrNone profissionalId
                    "paciente_id", Sql.intOrNone pacienteId
                ]
                |> Sql.executeRowAsync (fun read -> read.int "id")
        }

    // Get user by email
    let getUserByEmail (email: string) =
        task {
            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT u.id, u.username, u.password_hash, u.email, u.nome_completo, u.ativo,
                           u.ultimo_login, u.tentativas_login, u.bloqueado_ate, u.profissional_id,
                           u.paciente_id, u.unidade_id, u.data_cadastro,
                           COALESCE(string_agg(pa.nome, ','), '') as roles
                    FROM usuarios_sistema u
                    LEFT JOIN usuarios_perfis up ON u.id = up.usuario_id AND up.ativo = true
                    LEFT JOIN perfis_acesso pa ON up.perfil_id = pa.id AND pa.ativo = true
                    WHERE u.email = @email
                    GROUP BY u.id, u.username, u.password_hash, u.email, u.nome_completo, u.ativo,
                             u.ultimo_login, u.tentativas_login, u.bloqueado_ate, u.profissional_id,
                             u.paciente_id, u.unidade_id, u.data_cadastro
                """
                |> Sql.parameters ["email", Sql.string email]
                |> Sql.executeRowAsync (fun read -> 
                    let rolesStr = read.string "roles"
                    let roles = 
                        if String.IsNullOrEmpty(rolesStr) then []
                        else rolesStr.Split(',') |> Array.toList
                    
                    {
                        Id = read.int "id"
                        Username = read.string "username"
                        PasswordHash = read.string "password_hash"
                        Email = read.string "email"
                        NomeCompleto = read.string "nome_completo"
                        Ativo = read.bool "ativo"
                        UltimoLogin = read.datetimeOffsetOrNone "ultimo_login"
                        TentativasLogin = read.int "tentativas_login"
                        BloqueadoAte = read.datetimeOffsetOrNone "bloqueado_ate"
                        ProfissionalId = read.intOrNone "profissional_id"
                        PacienteId = read.intOrNone "paciente_id"
                        UnidadeId = read.intOrNone "unidade_id"
                        DataCadastro = read.dateTime "data_cadastro"
                        Roles = roles
                    })
        }

    // Update login attempt
    let updateLoginAttempt (userId: int) (success: bool) =
        task {
            let query = 
                if success then
                    """
                    UPDATE usuarios_sistema 
                    SET ultimo_login = CURRENT_TIMESTAMP, 
                        tentativas_login = 0,
                        bloqueado_ate = NULL
                    WHERE id = @id
                    """
                else
                    """
                    UPDATE usuarios_sistema 
                    SET tentativas_login = tentativas_login + 1,
                        bloqueado_ate = CASE 
                            WHEN tentativas_login >= 4 THEN CURRENT_TIMESTAMP + INTERVAL '30 minutes'
                            ELSE bloqueado_ate
                        END
                    WHERE id = @id
                    """
            
            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query query
                |> Sql.parameters ["id", Sql.int userId]
                |> Sql.executeNonQueryAsync
        }

    // Store refresh token
    let storeRefreshToken (userId: int) (token: string) (expiresAt: DateTimeOffset) =
        task {
            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    INSERT INTO refresh_tokens (user_id, token, expires_at)
                    VALUES (@user_id, @token, @expires_at)
                    RETURNING id
                """
                |> Sql.parameters [
                    "user_id", Sql.int userId
                    "token", Sql.string token
                    "expires_at", Sql.timestamptz expiresAt
                ]
                |> Sql.executeRowAsync (fun read -> read.int "id")
        }

    // Validate refresh token
    let validateRefreshToken (token: string) =
        task {
            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    SELECT id, user_id, expires_at, is_revoked
                    FROM refresh_tokens 
                    WHERE token = @token
                """
                |> Sql.parameters ["token", Sql.string token]
                |> Sql.executeRowAsync (fun read -> {
                    Id = read.int "id"
                    Token = token
                    UserId = read.int "user_id"
                    ExpiresAt = read.datetimeOffset "expires_at"
                    CreatedAt = DateTimeOffset.UtcNow // Not stored, just for structure
                    IsRevoked = read.bool "is_revoked"
                })
        }

    // Revoke refresh token
    let revokeRefreshToken (token: string) =
        task {
            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    UPDATE refresh_tokens 
                    SET is_revoked = true
                    WHERE token = @token
                """
                |> Sql.parameters ["token", Sql.string token]
                |> Sql.executeNonQueryAsync
        }

    // Change password
    let changePassword (userId: int) (newPasswordHash: string) =
        task {
            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    UPDATE usuarios_sistema 
                    SET password_hash = @password_hash,
                        tentativas_login = 0,
                        bloqueado_ate = NULL
                    WHERE id = @id
                """
                |> Sql.parameters [
                    "id", Sql.int userId
                    "password_hash", Sql.string newPasswordHash
                ]
                |> Sql.executeNonQueryAsync
        }
    let getSalt (userId: int) =
         task {
            return!
                DbConnection.getConnectionString()
                |> Sql.connect
                |> Sql.query """
                    select "salt" from usuarios_sistema where id = @id
                """
                |> Sql.parameters [
                    "id", Sql.int userId
                ]
                |> Sql.executeRowAsync (fun read -> read.string "salt")            
        }
module AuthHandlers =
    // =====================================================
// 3. AUTHENTICATION HANDLERS AND API ENDPOINTS
// =====================================================
    open Giraffe
    open Microsoft.AspNetCore.Http
    open Authentication
    open UserRepository
    open System
    open Infrastructure.Database
    open Npgsql.FSharp

    // Helper to get current user from context
    let getCurrentUser (ctx: HttpContext) =
        match ctx.User.Identity.IsAuthenticated with
        | true ->
            let getUserClaim (name: string) =
                let claim = ctx.User.FindFirst(name)
                if claim |> isNull then
                    ""
                else
                    claim.Value
            let getRoles () =
                ctx.User.FindAll(ClaimTypes.Role)
                |> Seq.map (fun c -> c.Value)
                |> List.ofSeq
            
            let getOptionalIntClaim name =
                match getUserClaim name with
                | null | "" -> None
                | value -> Int32.TryParse(value) |> function | (true, v) -> Some v | _ -> None
            
            Some {
                UserId = Int32.Parse(getUserClaim ClaimTypes.NameIdentifier)
                Nome = getUserClaim ClaimTypes.Name
                Email = getUserClaim ClaimTypes.Email
                Roles = getRoles()
                UnidadeId = getOptionalIntClaim "unidade_id" |> Option.defaultValue -1 |> Nullable
                ProfissionalId = getOptionalIntClaim "profissional_id" |> Option.defaultValue -1 |> Nullable
                PacienteId = getOptionalIntClaim "paciente_id" |> Option.defaultValue -1 |> Nullable
            }
        | false -> None

    // Login handler
    let login : HttpHandler =
        fun next ctx ->
            task {
                try
                    let! loginRequest = ctx.BindJsonAsync<LoginRequest>()
                    
                    // Basic validation
                    if String.IsNullOrWhiteSpace(loginRequest.Email) || String.IsNullOrWhiteSpace(loginRequest.Password) then
                        let errorResponse = {| error = "Email e senha são obrigatórios" |}
                        return! (setStatusCode 400 >=> json errorResponse) next ctx
                    else
                        // Get user from database
                        let! user = getUserByEmail (loginRequest.Email.ToLower())
                        
                        // Check if user exists and account is active
                        if not user.Ativo then
                            let errorResponse = {| error = "Conta desativada" |}
                            return! (setStatusCode 401 >=> json errorResponse) next ctx
                        elif user.BloqueadoAte.IsSome && user.BloqueadoAte.Value > DateTime.UtcNow then
                            let bloqueadoAteStr = user.BloqueadoAte.Value.ToString(":dd/MM/yyyy HH:mm")
                            let errorResponse = {| error = $"Conta bloqueada até {bloqueadoAteStr}" |}
                            return! (setStatusCode 401 >=> json errorResponse) next ctx
                        elif not (verifyPassword loginRequest.Password user.PasswordHash) then
                            // Update failed login attempt
                            let! _ = updateLoginAttempt user.Id false
                            let errorResponse = {| error = "Email ou senha inválidos" |}
                            return! (setStatusCode 401 >=> json errorResponse) next ctx
                        else
                            // Successful login
                            let! _ = updateLoginAttempt user.Id true
                            
                            // Create user claims
                            let userClaims = {
                                UserId = user.Id
                                Nome = user.NomeCompleto
                                Email = user.Email
                                Roles = user.Roles
                                UnidadeId = user.UnidadeId |> Option.defaultValue -1 |> Nullable
                                ProfissionalId = user.ProfissionalId |> Option.defaultValue -1 |> Nullable
                                PacienteId = user.PacienteId |> Option.defaultValue -1 |> Nullable
                            }
                            
                            // Generate tokens
                            let accessToken = generateToken userClaims
                            let refreshToken = generateRefreshToken()
                            
                            let config = getJwtConfig()
                            let expiresAt = DateTime.UtcNow.AddMinutes(float config.ExpireMinutes)
                            let refreshExpiresAt = DateTime.UtcNow.AddDays(float config.RefreshExpireDays)
                            
                            // Store refresh token
                            let! _ = storeRefreshToken user.Id refreshToken refreshExpiresAt
                            
                            let response = {
                                Token = accessToken
                                RefreshToken = refreshToken
                                ExpiresAt = expiresAt
                                User = {|
                                    Id = user.Id
                                    Nome = user.NomeCompleto
                                    Email = user.Email
                                    Roles = user.Roles
                                    UnidadeId = user.UnidadeId
                                |}
                            }
                            
                            return! json response next ctx
                with
                | :? System.InvalidOperationException ->
                    let errorResponse = {| error = "Email ou senha inválidos" |}
                    return! (setStatusCode 401 >=> json errorResponse) next ctx
                | ex ->
                    let errorResponse = {| error = "Erro interno do servidor"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    // Refresh token handler
    let refreshToken : HttpHandler =
        fun next ctx ->
            task {
                try
                    let! request = ctx.BindJsonAsync<RefreshTokenRequest>()
                    
                    // Validate refresh token
                    let! refreshTokenData = validateRefreshToken request.RefreshToken
                    
                    if refreshTokenData.IsRevoked || refreshTokenData.ExpiresAt < DateTime.UtcNow then
                        let errorResponse = {| error = "Refresh token inválido ou expirado" |}
                        return! (setStatusCode 401 >=> json errorResponse) next ctx
                    else
                        // Get user data
                        let! user = 
                            DbConnection.getConnectionString()
                            |> Sql.connect
                            |> Sql.query """
                                SELECT u.id, u.email, u.nome_completo, u.unidade_id, u.profissional_id, u.paciente_id,
                                       COALESCE(string_agg(pa.nome, ','), '') as roles
                                FROM usuarios_sistema u
                                LEFT JOIN usuarios_perfis up ON u.id = up.usuario_id AND up.ativo = true
                                LEFT JOIN perfis_acesso pa ON up.perfil_id = pa.id AND pa.ativo = true
                                WHERE u.id = @id AND u.ativo = true
                                GROUP BY u.id, u.email, u.nome_completo, u.unidade_id, u.profissional_id, u.paciente_id
                            """
                            |> Sql.parameters ["id", Sql.int refreshTokenData.UserId]
                            |> Sql.executeRowAsync (fun read -> 
                                let rolesStr = read.string "roles"
                                let roles = 
                                    if String.IsNullOrEmpty(rolesStr) then []
                                    else rolesStr.Split(',') |> Array.toList
                                
                                {
                                    UserId = read.int "id"
                                    Nome = read.string "nome_completo"
                                    Email = read.string "email"
                                    Roles = roles
                                    UnidadeId = read.intOrNone "unidade_id" |> Option.defaultValue -1 |> Nullable
                                    ProfissionalId = read.intOrNone "profissional_id" |> Option.defaultValue -1 |> Nullable
                                    PacienteId = read.intOrNone "paciente_id" |> Option.defaultValue -1 |> Nullable
                                })
                        
                        // Generate new tokens
                        let newAccessToken = generateToken user
                        let newRefreshToken = generateRefreshToken()
                        
                        let config = getJwtConfig()
                        let expiresAt = DateTime.UtcNow.AddMinutes(float config.ExpireMinutes)
                        let refreshExpiresAt = DateTime.UtcNow.AddDays(float config.RefreshExpireDays)
                        
                        // Revoke old refresh token and store new one
                        let! _ = revokeRefreshToken request.RefreshToken
                        let! _ = storeRefreshToken user.UserId newRefreshToken refreshExpiresAt
                        
                        let response = {
                            Token = newAccessToken
                            RefreshToken = newRefreshToken
                            ExpiresAt = expiresAt
                            User = {|
                                Id = user.UserId
                                Nome = user.Nome
                                Email = user.Email
                                Roles = user.Roles
                                UnidadeId = user.UnidadeId |> Utils.toOptionIfNullable
                            |}
                        }
                        
                        return! json response next ctx
                with
                | :? System.InvalidOperationException ->
                    let errorResponse = {| error = "Refresh token inválido" |}
                    return! (setStatusCode 401 >=> json errorResponse) next ctx
                | ex ->
                    let errorResponse = {| error = "Erro interno do servidor"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    // Logout handler
    let logout : HttpHandler =
        fun next ctx ->
            task {
                try
                    let! request = ctx.BindJsonAsync<{| refreshToken: string |}>()
                    
                    // Revoke refresh token
                    let! _ = revokeRefreshToken request.refreshToken
                    
                    let response = {| message = "Logout realizado com sucesso" |}
                    return! json response next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao fazer logout"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    // Change password handler
    let changePassword : HttpHandler =
        fun next ctx ->
            task {
                try
                    let! request = ctx.BindJsonAsync<ChangePasswordRequest>()
                    
                    match getCurrentUser ctx with
                    | None ->
                        let errorResponse = {| error = "Usuário não autenticado" |}
                        return! (setStatusCode 401 >=> json errorResponse) next ctx
                    | Some userClaims ->
                        // Validate input
                        if request.NewPassword <> request.ConfirmPassword then
                            let errorResponse = {| error = "Nova senha e confirmação não coincidem" |}
                            return! (setStatusCode 400 >=> json errorResponse) next ctx
                        else
                            // Check password strength
                            let passwordErrors = validatePasswordStrength request.NewPassword
                            if not passwordErrors.IsEmpty then
                                let errorResponse = {| errors = passwordErrors |}
                                return! (setStatusCode 400 >=> json errorResponse) next ctx
                            else
                                // Get current user data
                                let! user = getUserByEmail userClaims.Email
                                
                                // Verify current password
                                if not (verifyPassword request.CurrentPassword user.PasswordHash) then
                                    let errorResponse = {| error = "Senha atual inválida" |}
                                    return! (setStatusCode 400 >=> json errorResponse) next ctx
                                else
                                    // Update password
                                    let newPasswordHash = hashPassword None request.NewPassword
                                    let! _ = UserRepository.changePassword userClaims.UserId newPasswordHash
                                    
                                    let response = {| message = "Senha alterada com sucesso" |}
                                    return! json response next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao alterar senha"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    // Get current user profile
    let profile : HttpHandler =
        fun next ctx ->
            task {
                match getCurrentUser ctx with
                | None ->
                    let errorResponse = {| error = "Usuário não autenticado" |}
                    return! (setStatusCode 401 >=> json errorResponse) next ctx
                | Some userClaims ->
                    let response = {|
                        id = userClaims.UserId
                        nome = userClaims.Nome
                        email = userClaims.Email
                        roles = userClaims.Roles
                        unidadeId = userClaims.UnidadeId
                        profissionalId = userClaims.ProfissionalId
                        pacienteId = userClaims.PacienteId
                    |}
                    return! json response next ctx
            }

    // Authentication routes
    let routes : HttpHandler =
        choose [
            POST >=> choose [
                route "/login" >=> login
                route "/refresh" >=> refreshToken
                route "/logout" >=> logout
                route "/change-password" >=> changePassword
            ]
            GET >=> choose [
                route "/profile" >=> profile
            ]
        ]

// =====================================================
// 4. AUTHORIZATION MIDDLEWARE AND HELPERS
// =====================================================

module Authorization =
    open Giraffe
    open Microsoft.AspNetCore.Http
    open Authentication
    open AuthHandlers

    // Authorization middleware
    let requireAuth : HttpHandler =
        fun next ctx ->
            task {
                match getCurrentUser ctx with
                | Some _ -> return! next ctx
                | None ->
                    let errorResponse = {| error = "Token de autenticação necessário" |}
                    return! (setStatusCode 401 >=> json errorResponse) earlyReturn ctx
            }

    let requireRole (role: UserRole) : HttpHandler =
        fun next ctx ->
            task {
                match getCurrentUser ctx with
                | Some userClaims when hasPermission role userClaims -> 
                    return! next ctx
                | Some _ ->
                    let errorResponse = {| error = "Permissão insuficiente" |}
                    return! (setStatusCode 403 >=> json errorResponse) earlyReturn ctx
                | None ->
                    let errorResponse = {| error = "Token de autenticação necessário" |}
                    return! (setStatusCode 401 >=> json errorResponse) earlyReturn ctx
            }

    let requireAnyRole (roles: UserRole list) : HttpHandler =
        fun next ctx ->
            task {
                match getCurrentUser ctx with
                | Some userClaims when hasAnyPermission roles userClaims -> 
                    return! next ctx
                | Some _ ->
                    let errorResponse = {| error = "Permissão insuficiente" |}
                    return! (setStatusCode 403 >=> json errorResponse) earlyReturn ctx
                | None ->
                    let errorResponse = {| error = "Token de autenticação necessário" |}
                    return! (setStatusCode 401 >=> json errorResponse) earlyReturn ctx
            }

    let requireOwnershipOrAdmin (resourceUserId: int) : HttpHandler =
        fun next ctx ->
            task {
                match getCurrentUser ctx with
                | Some userClaims when isAdminOrOwner resourceUserId userClaims -> 
                    return! next ctx
                | Some _ ->
                    let errorResponse = {| error = "Acesso negado. Você só pode acessar seus próprios recursos" |}
                    return! (setStatusCode 403 >=> json errorResponse) earlyReturn ctx
                | None ->
                    let errorResponse = {| error = "Token de autenticação necessário" |}
                    return! (setStatusCode 401 >=> json errorResponse) earlyReturn ctx
            }

    // Helper functions for route protection
    let adminOnly () = requireRole Administrador
    let medicoOnly () = requireRole Medico
    let medicoOrEnfermeiro () = requireAnyRole [Medico; Enfermeiro]
    let healthcareProfessional () = requireAnyRole [Medico; Enfermeiro; Recepcionista]
    let internalOnly () = requireAnyRole [Administrador; Medico; Enfermeiro; Recepcionista; Farmacia; Laboratorio]

// =====================================================
// 5. USER MANAGEMENT HANDLERS (Admin Only)
// =====================================================

module UserManagementHandlers =
    open Giraffe
    open Microsoft.AspNetCore.Http
    open Authentication
    open UserRepository
    open Authorization
    open System
    open Infrastructure.Database
    open Npgsql.FSharp
    type CreateUserRequest = {
        Email: string
        Password: string
        NomeCompleto: string
        ProfissionalId: int option
        PacienteId: int option
        Roles: string list
    }

    type UpdateUserRequest = {
        NomeCompleto: string
        Ativo: bool
        Roles: string list
    }

    // Create user (Admin only)
    let createUser : HttpHandler =
        adminOnly () >=> fun next ctx ->
            task {
                try
                    let! request = ctx.BindJsonAsync<CreateUserRequest>()
                    
                    // Validate input
                    if String.IsNullOrWhiteSpace(request.Email) then
                        let errorResponse = {| error = "Email é obrigatório" |}
                        return! (setStatusCode 400 >=> json errorResponse) next ctx
                    elif String.IsNullOrWhiteSpace(request.Password) then
                        let errorResponse = {| error = "Senha é obrigatória" |}
                        return! (setStatusCode 400 >=> json errorResponse) next ctx
                    else
                        // Check password strength
                        let passwordErrors = validatePasswordStrength request.Password
                        if not passwordErrors.IsEmpty then
                            let errorResponse = {| errors = passwordErrors |}
                            return! (setStatusCode 400 >=> json errorResponse) next ctx
                        else
                            // Check if user already exists
                            try
                                let! existingUser = getUserByEmail (request.Email.ToLower())
                                let errorResponse = {| error = "Email já está em uso" |}
                                return! (setStatusCode 409 >=> json errorResponse) next ctx
                            with
                            | :? System.InvalidOperationException ->
                                // User doesn't exist, create new one
                                let! userId = UserRepository.createUser (request.Email.ToLower()) request.Password request.NomeCompleto request.ProfissionalId request.PacienteId
                                
                                // Assign roles
                                for role in request.Roles do
                                    let! _ =
                                        DbConnection.getConnectionString()
                                        |> Sql.connect
                                        |> Sql.query """
                                            INSERT INTO usuarios_perfis (usuario_id, perfil_id)
                                            SELECT @usuario_id, id FROM perfis_acesso WHERE nome = @role_name
                                        """
                                        |> Sql.parameters [
                                            "usuario_id", Sql.int userId
                                            "role_name", Sql.string role
                                        ]
                                        |> Sql.executeNonQueryAsync
                                    ()
                                
                                let response = {| id = userId; message = "Usuário criado com sucesso" |}
                                return! (setStatusCode 201 >=> json response) next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao criar usuário"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    // List all users (Admin only)
    let getAllUsers : HttpHandler =
        adminOnly () >=> fun next ctx ->
            task {
                try
                    let! users =
                        DbConnection.getConnectionString()
                        |> Sql.connect
                        |> Sql.query """
                            SELECT u.id, u.username, u.email, u.nome_completo, u.ativo,
                                   u.ultimo_login, u.profissional_id, u.paciente_id, u.unidade_id,
                                   COALESCE(string_agg(pa.nome, ','), '') as roles
                            FROM usuarios_sistema u
                            LEFT JOIN usuarios_perfis up ON u.id = up.usuario_id AND up.ativo = true
                            LEFT JOIN perfis_acesso pa ON up.perfil_id = pa.id AND pa.ativo = true
                            GROUP BY u.id, u.username, u.email, u.nome_completo, u.ativo,
                                     u.ultimo_login, u.profissional_id, u.paciente_id, u.unidade_id
                            ORDER BY u.nome_completo
                        """
                        |> Sql.executeAsync (fun read -> 
                            let rolesStr = read.string "roles"
                            let roles = 
                                if String.IsNullOrEmpty(rolesStr) then []
                                else rolesStr.Split(',') |> Array.toList
                            
                            {|
                                id = read.int "id"
                                username = read.string "username"
                                email = read.string "email"
                                nomeCompleto = read.string "nome_completo"
                                ativo = read.bool "ativo"
                                ultimoLogin = read.dateTimeOrNone "ultimo_login"
                                profissionalId = read.intOrNone "profissional_id"
                                pacienteId = read.intOrNone "paciente_id"
                                unidadeId = read.intOrNone "unidade_id"
                                roles = roles
                            |})
                    
                    return! json users next ctx
                with
                | ex ->
                    let errorResponse = {| error = "Erro ao listar usuários"; details = ex.Message |}
                    return! (setStatusCode 500 >=> json errorResponse) next ctx
            }

    // User management routes
    let routes : HttpHandler =
        choose [
            GET >=> route "" >=> getAllUsers
            POST >=> route "" >=> createUser
        ]
// =====================================================
// 6. STARTUP CONFIGURATION
// =====================================================

module AuthStartup =
    open Microsoft.AspNetCore.Builder
    open Microsoft.Extensions.DependencyInjection
    open Microsoft.AspNetCore.Authentication.JwtBearer
    open Microsoft.IdentityModel.Tokens
    open System.Text
    open Authentication

    let configureJwtAuthentication (services: IServiceCollection) =
        let config = getJwtConfig()
        
        services.AddAuthentication(fun options ->
            options.DefaultAuthenticateScheme <- JwtBearerDefaults.AuthenticationScheme
            options.DefaultChallengeScheme <- JwtBearerDefaults.AuthenticationScheme
        ).AddJwtBearer(fun options ->
            options.TokenValidationParameters <- TokenValidationParameters(
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.SecretKey)),
                ValidateIssuer = true,
                ValidIssuer = config.Issuer,
                ValidateAudience = true,
                ValidAudience = config.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            )
            options.Events <- JwtBearerEvents(
                OnAuthenticationFailed = fun context ->
                    if context.Exception.GetType() = typeof<SecurityTokenExpiredException> then
                        context.Response.Headers.Add("Token-Expired", "true")
                    System.Threading.Tasks.Task.CompletedTask
            )
        ) |> ignore
        
        services.AddAuthorization() |> ignore
        services

    let configureAuthMiddleware (app: IApplicationBuilder) =
        app.UseAuthentication()
           .UseAuthorization()
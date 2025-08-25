namespace Infrastructure.Security

open System
open System.Security.Claims
// adicione os pacotes se necessário
//open Microsoft.AspNetCore.Authentication.JwtBearer
//open Microsoft.IdentityModel.Tokens
open System.Text

module Authentication =
    type UserClaims = {
        UserId: int
        Nome: string
        Email: string
        Roles: string list
        UnidadeId: int option
    }
    
    type UserRole = 
        | Administrador
        | Medico
        | Enfermeiro
        | Recepcionista
        | Paciente
        | Auditoria
    
    let generateToken (claims: UserClaims) =
        // JWT token generation logic
        ""
    
    let validateToken (token: string) =
        // Token validation logic
        None
    
    let hasPermission (requiredRole: UserRole) (userClaims: UserClaims) =
        // Permission validation logic
        false
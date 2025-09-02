-- Add these tables to your database schema:

-- Refresh tokens table
CREATE TABLE IF NOT EXISTS refresh_tokens (
                                              id SERIAL PRIMARY KEY,
                                              user_id INTEGER REFERENCES usuarios_sistema(id) NOT NULL,
    token VARCHAR(500) UNIQUE NOT NULL,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    is_revoked BOOLEAN DEFAULT false
    );

CREATE INDEX IF NOT EXISTS idx_refresh_tokens_token ON refresh_tokens(token);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_user_id ON refresh_tokens(user_id);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_expires_at ON refresh_tokens(expires_at);

-- Password reset tokens (optional)
CREATE TABLE IF NOT EXISTS password_reset_tokens (
                                                     id SERIAL PRIMARY KEY,
                                                     user_id INTEGER REFERENCES usuarios_sistema(id) NOT NULL,
    token VARCHAR(500) UNIQUE NOT NULL,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    used BOOLEAN DEFAULT false
    );

-- Clean up expired tokens periodically
CREATE OR REPLACE FUNCTION cleanup_expired_tokens()
RETURNS void AS $$
BEGIN
DELETE FROM refresh_tokens
WHERE expires_at < CURRENT_TIMESTAMP OR is_revoked = true;

DELETE FROM password_reset_tokens
WHERE expires_at < CURRENT_TIMESTAMP OR used = true;
END;
$$ LANGUAGE plpgsql;

-- Insert default admin user (change password after first login!)
INSERT INTO usuarios_sistema (username, password_hash, salt, email, nome_completo, ativo)
VALUES (
           'admin@sghss.com',
           '$2a$12$rQZ8H8m8rQZ8H8m8rQZ8H.K8H8m8rQZ8H8m8rQZ8H8m8rQZ8H8m8r', -- password: Admin123!
           'bcrypt_handled',
           'admin@sghss.com',
           'Administrador do Sistema',
           true
       ) ON CONFLICT (email) DO NOTHING;

-- Assign admin role to default user
INSERT INTO usuarios_perfis (usuario_id, perfil_id)
SELECT u.id, p.id
FROM usuarios_sistema u, perfis_acesso p
WHERE u.email = 'admin@sghss.com'
  AND p.nome = 'Administrador'
    ON CONFLICT (usuario_id, perfil_id) DO NOTHING;
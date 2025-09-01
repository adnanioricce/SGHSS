-- Tabela para salvar o conteúdo do storage (S3, MinIO, etc)
CREATE TABLE IF NOT EXISTS files (
    id SERIAL PRIMARY KEY,
    file_name VARCHAR(255) NOT NULL,
    original_name VARCHAR(255) NOT NULL,
    content_type VARCHAR(100) NOT NULL,
    size BIGINT NOT NULL,
    bucket_name VARCHAR(100) NOT NULL,
    object_name VARCHAR(500) NOT NULL,
    category VARCHAR(50) NOT NULL,
    uploaded_by INTEGER REFERENCES usuarios_sistema(id),
    uploaded_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ,
    url TEXT NOT NULL,
    metadata JSONB,
    UNIQUE(bucket_name, object_name),    
    CONSTRAINT files_size_check CHECK (size > 0)
);

CREATE INDEX IF NOT EXISTS idx_files_category ON files(category) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS idx_files_uploaded_by ON files(uploaded_by) WHERE is_deleted = false;
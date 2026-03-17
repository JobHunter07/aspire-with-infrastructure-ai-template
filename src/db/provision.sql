-- DBGate provisioning example
CREATE TABLE IF NOT EXISTS books (
    id SERIAL PRIMARY KEY,
    title TEXT NOT NULL,
    author TEXT NOT NULL
);

INSERT INTO books (title, author)
VALUES ('Demo Book', 'Template');

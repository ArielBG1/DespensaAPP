-- ============================================================
-- SEED INICIAL para despensa_db
-- Ejecutar DESPUÉS de aplicar las migraciones EF
-- ============================================================

-- Usuarios de prueba (passwords generados con BCrypt work factor 12)
-- admin@despensa.com   → password: Admin1234!
-- cajero@despensa.com  → password: Cajero1234!
INSERT INTO usuarios (nombre, email, password_hash, rol, activo, created_at, updated_at)
VALUES
  ('Administrador', 'admin@despensa.com',
   '$2a$12$KIXbhKH8v6zZbVJmJZjGpOzQoTXcJ1QG4YmX8L3aG2JK5pOvGJMHi',
   'Admin', true, NOW(), NOW()),
  ('Cajero Demo', 'cajero@despensa.com',
   '$2a$12$2Q3k5L8mN7p9R1s4T6u8W.eA0bCdEfGhIjKlMnOpQrStUvWxYzAb',
   'Cajero', true, NOW(), NOW())
ON CONFLICT (email) DO NOTHING;

-- NOTA: Los hashes de arriba son de ejemplo.
-- Para generar hashes reales, usa el endpoint /Auth/GenerarHash?password=TuPassword
-- o ejecuta en C#: BCrypt.Net.BCrypt.HashPassword("TuPassword", 12)

-- Configuraciones base
INSERT INTO configuraciones (clave, valor, descripcion, updated_at)
VALUES
  ('NombreNegocio', 'Despensa App', 'Nombre del negocio', NOW()),
  ('Direccion', 'Av. Ejemplo 123', 'Dirección del negocio', NOW()),
  ('Telefono', '021-123-456', 'Teléfono de contacto', NOW()),
  ('MonedaSimbolo', 'Gs.', 'Símbolo de moneda', NOW()),
  ('MonedaNombre', 'Guaraní', 'Nombre de la moneda', NOW()),
  ('IVAPorcentaje', '10', 'Porcentaje de IVA', NOW()),
  ('StockMinimoAlerta', '5', 'Stock mínimo para alertas', NOW())
ON CONFLICT (clave) DO UPDATE SET valor = EXCLUDED.valor, updated_at = NOW();

-- Categorías de ejemplo
INSERT INTO categorias (nombre, descripcion, activo, created_at)
VALUES
  ('Lácteos', 'Leches, yogures, quesos', true, NOW()),
  ('Panificados', 'Pan, galletitas, facturas', true, NOW()),
  ('Bebidas', 'Aguas, gaseosas, jugos', true, NOW()),
  ('Limpieza', 'Productos de limpieza del hogar', true, NOW()),
  ('Almacén', 'Fideos, arroz, legumbres, aceites', true, NOW()),
  ('Carnes', 'Carnes frescas y fiambres', true, NOW())
ON CONFLICT DO NOTHING;

-- ============================================================
-- SEED INICIAL para despensa_db
-- Ejecutar DESPUÉS de aplicar las migraciones EF
-- ============================================================

-- Los usuarios NO se crean acá: los crea la app al arrancar (Data/SeedData.cs), tomando las
-- contraseñas de las variables de entorno ADMIN_PASSWORD y CAJERO_PASSWORD. Si no están
-- definidas, genera una al azar y la escribe en el log del primer arranque.

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

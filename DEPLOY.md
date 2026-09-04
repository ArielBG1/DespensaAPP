# Deploy gratis: Render + Supabase

La app es ASP.NET Core 10 con PostgreSQL, así que **Vercel no sirve** (solo corre Node/Python/Go).
La combinación gratuita que funciona es:

- **Supabase** → la base de datos PostgreSQL (500 MB gratis)
- **Render** → la app web en Docker (plan Free)

---

## 1. Base de datos en Supabase

1. Entrar a https://supabase.com → *Start your project* (login con GitHub).
2. *New project*:
   - Name: `despensa`
   - Database Password: generar una y **guardarla**.
   - Region: `South America (São Paulo)`.
3. Esperar ~2 minutos a que termine de crearse.
4. Botón **Connect** (arriba) → pestaña *Connection string* → elegir **Session pooler**.
   Copiar algo así:

   ```
   postgresql://postgres.abcdefgh:[YOUR-PASSWORD]@aws-0-sa-east-1.pooler.supabase.com:5432/postgres
   ```

5. Reemplazar `[YOUR-PASSWORD]` por la contraseña del paso 2. Esa URL completa es el `DATABASE_URL`.

> Usar el **Session pooler**, no la conexión directa: la directa es solo IPv6 y Render no la alcanza.

---

## 2. Subir el código a GitHub

1. Crear un repo vacío en https://github.com/new (puede ser **privado**), sin README ni .gitignore.
2. En la carpeta del proyecto:

   ```bash
   git remote add origin https://github.com/TU-USUARIO/despensa-app.git
   git push -u origin main
   ```

---

## 3. Publicar en Render

1. Entrar a https://render.com → login con GitHub.
2. **New +** → **Web Service** → conectar el repo `despensa-app`.
3. Configurar:
   - Language: **Docker** (lo detecta solo por el `Dockerfile`)
   - Region: `Oregon` o la más cercana
   - Instance Type: **Free**
4. En **Environment Variables** agregar:

   | Key               | Value                                      |
   |-------------------|--------------------------------------------|
   | `DATABASE_URL`    | la URL de Supabase del paso 1              |
   | `ADMIN_PASSWORD`  | la contraseña que quieras para el admin    |
   | `CAJERO_PASSWORD` | la contraseña que quieras para el cajero   |

   Las dos últimas son opcionales pero conviene ponerlas: si no están definidas, la app genera
   una contraseña al azar en el primer arranque y la escribe en los logs de Render (pestaña
   *Logs*, buscar "se generó una contraseña al azar"). No hay ninguna contraseña por defecto
   escrita en el código.

5. **Create Web Service**. El primer build tarda ~5 minutos.
6. Queda publicada en `https://despensa-app.onrender.com` (ese link se les pasa a los amigos).

Las migraciones y los datos iniciales (usuarios, categorías, configuración) se aplican solos
en el primer arranque.

### Usuarios de prueba

- `admin@despensa.com` → contraseña de `ADMIN_PASSWORD`
- `cajero@despensa.com` → contraseña de `CAJERO_PASSWORD`

Se crean una sola vez, en el primer arranque con la base vacía. Para cambiarlas después hay que
hacerlo desde la pantalla de Usuarios de la app: cambiar la variable de entorno ya no tiene efecto.

---

## Limitaciones del plan gratuito

- **La app se duerme** tras 15 minutos sin visitas. La primera carga después de dormir tarda
  ~50 segundos. Avisarles a los que testeen para que no crean que está rota.
- **Las fotos de productos se guardan en la base** (tabla `producto_imagenes`), no en disco, así que
  sobreviven a los reinicios y a los deploys. Ocupan lugar en los 500 MB de Supabase: con un
  máximo de 5 MB por foto, conviene subir imágenes chicas.
- **Supabase pausa el proyecto** tras 7 días sin uso; se reactiva con un click desde el dashboard.

## Actualizar la app

Cada `git push` a `main` dispara un deploy nuevo automáticamente.

```bash
git add -A
git commit -m "mi cambio"
git push
```

## Desarrollo local

No cambia nada: sigue usando `ConnectionStrings:DefaultConnection` de `appsettings.json` más la
variable `DESPENSA_DB_PASSWORD`. `DATABASE_URL` solo se usa si está definida.

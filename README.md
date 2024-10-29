# ContactAPI - Bhd

# API de Contactos

Este es un proyecto de API RESTful para la gestión de usuarios en .NET Core con autenticación JWT. La API permite registrar usuarios, iniciar sesión para obtener un token de acceso y consultar datos de usuario, utilizando autenticación y autorización basada en JWT.

## Características

- **Registro de usuarios**: Permite la creación de nuevos usuarios con validación de correo y contraseña.
- **Autenticación JWT**: Protege los endpoints mediante autenticación JWT, generando tokens de acceso que expiran en un minuto.
- **Operaciones CRUD**: Endpoints para registrar, autenticar y listar usuarios.
- **Validación de datos**: Incluye validaciones para correos electrónicos y contraseñas según expresiones regulares configurables.

## Tecnologías Utilizadas

- **.NET Core 6**
- **Entity Framework Core** para la gestión de datos
- **JWT (JSON Web Tokens)** para autenticación y autorización
- **SQL Server** como base de datos

## Endpoints Principales

- **POST /api/User/register**: Registra un nuevo usuario.
- **POST /api/User/login**: Genera un token JWT para un usuario registrado.
- **GET /api/User/{id}**: Obtiene la información de un usuario (requiere autenticación).
- **GET /api/User/all**: Obtiene una lista de todos los usuarios (requiere autenticación).

**Configura la cadena de conexión de la base de datos en appsettings.json

  "ConnectionStrings": {
    "DefaultConnection": "Server=TU_SERVIDOR;Database=Contacts;Trusted_Connection=True;"

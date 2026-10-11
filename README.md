# CXView Backend

Microservicio de Seguridad en .NET 10 con Clean Architecture, JWT RSA, permisos,
perfiles, sesiones revocables, OTP con HMAC, PBKDF2, rate limiting y Dapper.
No se versionan secretos ni claves privadas.

## Inicio local

1. Reemplace `Otp:PepperBase64` mediante user-secrets o variables de entorno.
2. Ejecute `dotnet run --project src/Services/Identity/CXView.Api`.

En Development se genera automáticamente un par RSA local ignorado por Git. En otros
entornos las rutas deben apuntar a claves aprovisionadas externamente.

El proveedor predeterminado es `SqlServer`. `Database:Provider` permite cambiar a `MySql`.
Ejecute en orden los scripts del directorio correspondiente e inyecte las credenciales con
`Database__ConnectionString`. El backend no contiene SQL de tablas: Dapper invoca únicamente
procedimientos almacenados con los mismos nombres y contratos en ambos motores.

Endpoints principales: `/api/v1/auth`, `/api/v1/security/users`,
`/api/v1/security/profiles` y `/api/v1/security/permissions`. Las bajas de usuario son
lógicas para conservar auditoría e integridad referencial.

La cuenta de desarrollo es `admin` / `ChangeMe-123!` y nunca debe habilitarse en producción.

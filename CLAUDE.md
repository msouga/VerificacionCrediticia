# VerificacionCrediticia - Instrucciones de Proyecto

## Arquitectura
- **Backend**: .NET 8 Web API con clean architecture (Core, Infrastructure, API)
- **Frontend**: Angular 19 con Angular Material y Cytoscape.js
- **Base de datos**: Pendiente (actualmente usa mock de Equifax)
- **Puerto API**: 5100
- **Puerto Angular**: 4200

## Estructura
```
src/
  VerificacionCrediticia.API/          # Controllers, Program.cs, DI
  VerificacionCrediticia.Core/         # Entities, DTOs, Enums, Services, Interfaces
  VerificacionCrediticia.Infrastructure/ # Equifax client (real + mock)
  VerificacionCrediticia.Angular/      # Frontend Angular 19
tests/
  VerificacionCrediticia.UnitTests/
  VerificacionCrediticia.IntegrationTests/
```

## Convenciones
- Backend: C# con enums numericos serializados como integers por System.Text.Json
- Frontend: Los enums del backend llegan como numeros (0, 1, 2...), no como strings. Siempre manejar ambos formatos en comparaciones
- Idioma del codigo: espanol para nombres de dominio, ingles para patrones tecnicos
- Sin emojis en codigo ni documentacion

## Ejecucion
- API: `dotnet run --project src/VerificacionCrediticia.API`
- Angular: `cd src/VerificacionCrediticia.Angular && ng serve`
- Debug VS Code: usar launch.json con compound "Full Stack Debug"
- Kill procesos: task "kill-alldebug" en VS Code

## Datos de prueba (Mock)
| DNI | RUC | Descripcion |
|-----|-----|-------------|
| 12345678 | 20123456789 | Caso limpio, score alto |
| 44444444 | 20111111111 | Solicitante moroso |
| 55555555 | 20222222222 | Empresa castigada |
| 66666666 | 20444444444 | Red con socios problematicos (2do nivel) |
| 99999999 | 20555555555 | Cadena de 3 niveles de profundidad |

## Skills obligatorios
Antes de escribir o modificar codigo, SIEMPRE consultar los skills relevantes:
- **angular-enterprise**: Para cualquier cambio en el frontend Angular
- **aspnet-enterprise**: Para controllers, DI, middleware, estructura de API .NET
- **ef-core-patterns**: Para Entity Framework Core, migraciones, repositorios
- **azure-cloud**: Para App Service, Key Vault, CI/CD, configuracion Azure
- **azure-architecture**: Para decisiones de arquitectura y servicios Azure
- **sql-optimization**: Para queries SQL, indices, stored procedures
- **iso27001-security**: Para controles de seguridad, audit logging, proteccion de datos

Invocar el skill correspondiente con la herramienta Skill ANTES de comenzar la implementacion.

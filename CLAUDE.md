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
  VerificacionCrediticia.Infrastructure/ # Equifax, ContentUnderstanding, Reniec (real + mock)
  VerificacionCrediticia.Angular/      # Frontend Angular 19
tests/
  VerificacionCrediticia.UnitTests/
  VerificacionCrediticia.IntegrationTests/
  samples/                             # PDFs de prueba (excluidos de git)
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

## Azure Content Understanding
- Recurso: `cu-vercred-dev` (AIServices, S0) en **westus**, RG: `rg-vercred-cu-dev`
- Endpoint: `https://westus.api.cognitive.microsoft.com/`
- API version: `2025-05-01-preview`
- Modelos: `gpt-41-mini` (gpt-4.1-mini), `text-embedding-3-large`
- Analyzers: `dniperuano` (DNI peruano), `vigenciaPoderes` (Vigencia de Poder)
- Config mock/real: `ContentUnderstanding.UseMock` en `appsettings.Development.json`

## Endpoints de documentos
- `POST /api/documentos/dni` - Procesa DNI (multipart/form-data, respuesta JSON)
- `POST /api/documentos/vigencia-poder` - Procesa Vigencia de Poder (multipart/form-data, respuesta SSE)
  - Eventos SSE: `progress` (texto), `result` (JSON VigenciaPoderDto), `error` (texto)

## Datos de prueba (Mock Evaluacion)
| DNI | RUC | Descripcion |
|-----|-----|-------------|
| 12345678 | 20123456789 | Caso limpio, score alto |
| 44444444 | 20111111111 | Solicitante moroso |
| 55555555 | 20222222222 | Empresa castigada |
| 66666666 | 20444444444 | Red con socios problematicos (2do nivel) |
| 99999999 | 20555555555 | Cadena de 3 niveles de profundidad |

## Datos de prueba (Mock Documentos)
- DNI: cualquier PDF se procesa, archivo con "Aileen" usa datos de Aileen Lei Koo
- Vigencia de Poder: archivo con "TechSolutions" o "20659018901" retorna TECH SOLUTIONS IMPORT SAC con 4 representantes

## Datos de prueba (Equifax UAT)
- DNIs morosos: 41197536, 43109722, 16027546, 04052936, 02858608
- DNIs con info financiera: 08587800, 06906697, 08551803, 07679095, 07725758
- RUCs: 20600857011, 20600782275

## Archivos de muestra (tests/samples/, excluidos de git)
- `DNI_Aileen.pdf`, `DNI_Pablo.pdf`, `DNI_Paola.pdf`
- `Vigencia_Poder_TechSolutions.pdf`
- `Balance_General_TechSolutions.pdf`

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

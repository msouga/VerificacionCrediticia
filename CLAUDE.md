# VerificacionCrediticia - Instrucciones de Proyecto

## Arquitectura
- **Backend**: .NET 9 Web API con clean architecture (Core, Infrastructure, API)
- **Frontend**: Angular 19 con Angular Material y Cytoscape.js
- **Base de datos**: Azure SQL (EF Core 9), migraciones en Infrastructure
- **Storage**: Azure Blob Storage (`stvercreddev`, container `documentos`)
- **Puerto API**: 5100
- **Puerto Angular**: 4200

## Estructura
```
src/
  VerificacionCrediticia.API/          # Controllers, Program.cs, DI
  VerificacionCrediticia.Core/         # Entities, DTOs, Enums, Services, Interfaces
  VerificacionCrediticia.Infrastructure/ # Equifax, ContentUnderstanding, Reniec, Storage, Persistence
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
- API version: `2025-11-01` (GA)
- Modelos: `gpt-41-mini` (gpt-4.1-mini), `text-embedding-3-large`
- Analyzers: `dniperuano`, `vigenciaPoderes`, `balanceGeneral`, `estadoResultados`
- Config mock/real: `ContentUnderstanding.UseMock` en `appsettings.Development.json`

## Endpoints de documentos (individuales, sin expediente)
- `POST /api/documentos/dni` - Procesa DNI (multipart/form-data, respuesta JSON)
- `POST /api/documentos/vigencia-poder` - Procesa Vigencia de Poder (multipart/form-data, respuesta SSE)
- `POST /api/documentos/balance-general` - Procesa Balance General (multipart/form-data, respuesta SSE)
- `POST /api/documentos/estado-resultados` - Procesa Estado de Resultados (multipart/form-data, respuesta SSE)
  - Eventos SSE: `progress` (texto), `result` (JSON), `error` (texto)

## Endpoints de configuracion
- `GET /api/configuracion/tipos-documento` - Listar tipos de documento
- `PUT /api/configuracion/tipos-documento/{id}` - Actualizar tipo de documento
- `GET /api/configuracion/reglas` - Listar reglas de evaluacion (ordenadas por Orden)
- `GET /api/configuracion/reglas/{id}` - Obtener regla
- `POST /api/configuracion/reglas` - Crear regla
- `PUT /api/configuracion/reglas/{id}` - Actualizar regla (valor, peso, operador, resultado, activa, orden)
- `DELETE /api/configuracion/reglas/{id}` - Eliminar regla

## Endpoints de expedientes
- `POST /api/expedientes` - Crear expediente
- `GET /api/expedientes?pagina=1&tamanoPagina=10` - Listar paginado
- `GET /api/expedientes/{id}` - Detalle con documentos y resultado
- `PUT /api/expedientes/{id}` - Actualizar descripcion
- `DELETE /api/expedientes` - Eliminar multiples (body: [ids])
- `POST /api/expedientes/{id}/documentos/{codigoTipo}` - Subir documento a blob (JSON, sin procesamiento)
- `PUT /api/expedientes/{id}/documentos/{docId}` - Reemplazar documento (JSON)
- `POST /api/expedientes/{id}/evaluar` - Evaluar: procesa todos con Content Understanding + reglas (SSE)
- `GET /api/expedientes/tipos-documento` - Lista tipos de documento

## Flujo de expedientes
1. **Subir documentos**: POST va a Azure Blob Storage, estado = Subido (4), respuesta JSON instantanea. Se encola automaticamente para procesamiento background con Content Understanding.
2. **Procesamiento background**: `BackgroundDocumentProcessor` (hosted service) consume la cola `Channel<T>`, procesa hasta 3 documentos en paralelo. Estados: Subido -> Procesando -> Procesado/Error. Recovery al reiniciar (re-encola Subido/Procesando).
3. **Frontend polling**: cada 5s recarga el expediente para ver progreso de procesamiento. Se detiene cuando todos los docs estan Procesado/Error.
4. **Evaluar**: POST SSE. Si todos los docs ya estan Procesados, solo aplica reglas crediticias (instantaneo). Fallback sincrono para docs que no terminaron de procesar.
   - Eventos SSE: `progress` (JSON ProgresoEvaluacionDto con archivo, paso, detalle, documentoActual, totalDocumentos), `result` (JSON ExpedienteDto), `error` (texto)
   - `detalle` contiene mensajes de polling de Content Understanding (ej: "Analizando estructura...", "Extrayendo texto con OCR...")
   - Soporta cancelacion via CancellationToken/AbortController
   - Retry automatico (1 intento) si falla un documento

## Resiliencia HTTP (Content Understanding)
- Polly retry: 429 (TooManyRequests), 503 (ServiceUnavailable), 408 (RequestTimeout)
- Backoff exponencial (2s base) con jitter, max 5 reintentos
- Respeta header `Retry-After` de Azure (delta o fecha absoluta)
- `EnsureSuccessAsync`: lee body de error de Azure AI Services antes de lanzar excepcion (mensajes utiles en vez de genericos)
- Los 500 y otros errores NO se reintentan

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
- `Estado_Resultados_TechSolutions.pdf`

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

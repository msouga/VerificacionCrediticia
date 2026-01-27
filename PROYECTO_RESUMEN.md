# Sistema de Verificación Crediticia - Equifax Perú

## Resumen del Proyecto

Sistema para evaluar solicitudes de crédito analizando la **red de relaciones empresariales** entre personas (DNI) y empresas (RUC), consultando datos de Equifax Perú (Infocorp).

---

## Arquitectura

```
VerificacionCrediticia/
├── src/
│   ├── VerificacionCrediticia.API/              # Web API REST (.NET 8)
│   │   ├── Controllers/
│   │   │   └── VerificacionController.cs        # Endpoints de evaluación
│   │   ├── Extensions/
│   │   │   └── ServiceCollectionExtensions.cs   # Inyección de dependencias
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json         # Config con UseMock=true
│   │   └── Program.cs
│   │
│   ├── VerificacionCrediticia.Core/             # Lógica de Negocio
│   │   ├── Entities/
│   │   │   ├── Persona.cs
│   │   │   ├── Empresa.cs
│   │   │   ├── RelacionSocietaria.cs
│   │   │   ├── DeudaRegistrada.cs
│   │   │   ├── NodoRed.cs
│   │   │   └── Alerta.cs
│   │   ├── Enums/
│   │   │   ├── TipoNodo.cs
│   │   │   ├── EstadoCrediticio.cs
│   │   │   ├── Recomendacion.cs
│   │   │   ├── Severidad.cs
│   │   │   └── TipoAlerta.cs
│   │   ├── DTOs/
│   │   │   ├── SolicitudVerificacionDto.cs
│   │   │   ├── ResultadoExploracionDto.cs
│   │   │   └── ResultadoEvaluacionDto.cs
│   │   ├── Interfaces/
│   │   │   ├── IEquifaxApiClient.cs
│   │   │   ├── IExploradorRedService.cs
│   │   │   ├── IScoringService.cs
│   │   │   └── IVerificacionService.cs
│   │   └── Services/
│   │       ├── ExploradorRedService.cs          # Algoritmo BFS para grafo
│   │       ├── ScoringService.cs                # Cálculo de riesgo
│   │       └── VerificacionService.cs           # Orquestador
│   │
│   ├── VerificacionCrediticia.Infrastructure/   # Integraciones Externas
│   │   ├── Equifax/
│   │   │   ├── EquifaxApiClient.cs              # Cliente real de Equifax
│   │   │   ├── EquifaxApiClientMock.cs          # Cliente mock para pruebas
│   │   │   ├── EquifaxAuthService.cs            # OAuth 2.0
│   │   │   ├── EquifaxSettings.cs               # Configuración
│   │   │   └── Models/
│   │   │       └── EquifaxPersonaResponse.cs
│   │   └── Persistence/
│   │
│   └── VerificacionCrediticia.Web/              # Dashboard Blazor
│       ├── Components/
│       │   ├── Layout/
│       │   │   └── MainLayout.razor
│       │   ├── Pages/
│       │   │   ├── Home.razor                   # Dashboard principal
│       │   │   └── Evaluar.razor                # Formulario de evaluación
│       │   ├── App.razor
│       │   ├── Routes.razor
│       │   └── _Imports.razor
│       ├── Services/
│       │   └── VerificacionApiClient.cs
│       ├── wwwroot/css/app.css
│       └── appsettings.json
│
├── tests/
│   ├── VerificacionCrediticia.UnitTests/
│   │   └── Services/
│   │       └── ScoringServiceTests.cs           # 3 tests
│   └── VerificacionCrediticia.IntegrationTests/
│
├── VerificacionCrediticia.sln
├── global.json
└── .gitignore
```

---

## Flujo de la Aplicación

```
ENTRADA:
├── DNI del solicitante (persona que firma/representa)
└── RUC de la empresa solicitante (a quien se otorga el crédito)

PROCESO (Algoritmo BFS - 2 niveles de profundidad):

Nivel 0:
├── Consultar DNI → ¿En qué empresas (RUCs) es socio?
└── Consultar RUC solicitante → ¿Quiénes son los socios (DNIs)?

Nivel 1:
├── Por cada RUC encontrado → ¿Quiénes son los otros socios?
└── Por cada DNI encontrado → ¿En qué otras empresas participa?

Nivel 2:
└── Repetir el proceso para mapear conexiones más profundas

SALIDA:
├── Grafo de relaciones (DNIs ↔ RUCs)
├── Score crediticio de cada entidad
├── Alertas (deudas, morosidad, problemas legales)
└── Recomendación: APROBAR / REVISAR MANUALMENTE / RECHAZAR
```

---

## Endpoints de la API

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| `POST` | `/api/verificacion/evaluar` | Evalúa solicitud de crédito |
| `GET` | `/api/verificacion/persona/{dni}` | Consulta persona por DNI |
| `GET` | `/api/verificacion/empresa/{ruc}` | Consulta empresa por RUC |

### Ejemplo de Request

```json
POST /api/verificacion/evaluar
{
    "dniSolicitante": "12345678",
    "rucEmpresa": "20123456789",
    "profundidadMaxima": 2,
    "incluirDetalleGrafo": true
}
```

### Ejemplo de Response

```json
{
    "dniSolicitante": "12345678",
    "nombreSolicitante": "Juan Carlos Pérez García",
    "rucEmpresa": "20123456789",
    "razonSocialEmpresa": "DISTRIBUIDORA COMERCIAL DEL NORTE SAC",
    "scoreFinal": 100,
    "recomendacion": 0,
    "recomendacionTexto": "APROBAR",
    "resumen": {
        "totalPersonasAnalizadas": 2,
        "totalEmpresasAnalizadas": 2,
        "personasConProblemas": 0,
        "empresasConProblemas": 0,
        "scorePromedioRed": 700,
        "montoTotalDeudas": 375000,
        "montoTotalDeudasVencidas": 0
    },
    "alertas": [],
    "fechaEvaluacion": "2026-01-20T22:10:25Z"
}
```

---

## Datos de Prueba (Mock)

### Personas

| DNI | Nombre | Score | Estado | Escenario |
|-----|--------|-------|--------|-----------|
| `12345678` | Juan Carlos Pérez García | 750 | Normal | Buen cliente, socio de 2 empresas |
| `87654321` | María Elena López Torres | 680 | Normal | Buena, socia de 2 empresas |
| `11111111` | Roberto Sánchez Medina | 420 | **Moroso** | Deudas vencidas 95 días |
| `22222222` | Ana Patricia Fernández | 580 | CPP | Problemas potenciales |
| `33333333` | Carlos Alberto Mendoza | 290 | **Castigado** | Deuda pérdida |
| `44444444` | Luis Fernando Castro | 710 | Normal | Sin deudas |
| `55555555` | Patricia Rojas Díaz | 620 | Normal | PYME |
| `66666666` | Ricardo Vargas Huaman | 780 | Normal | Caso 2do nivel |
| `77777777` | Eduardo Quispe Ramos | 320 | **Moroso** | Socio problematico |
| `88888888` | Carmen Flores Gutierrez | 250 | **Castigado** | Socia problematica |
| `99999999` | Alberto Ramirez Soto | 720 | Normal | Caso red profunda |
| `10101010` | Fernando Gutierrez Luna | 700 | Normal | Nivel 1 red profunda |
| `20202020` | Gabriela Torres Prado | 520 | CPP | Nivel 2 red profunda |
| `30303030` | Hector Villanueva Cruz | 180 | **Castigado** | Nivel 3 red profunda |
| `40404040` | Isabel Chavez Morales | 350 | **Moroso** | Nivel 3 red profunda |

### Empresas

| RUC | Razón Social | Score | Estado | Escenario |
|-----|--------------|-------|--------|-----------|
| `20123456789` | DISTRIBUIDORA COMERCIAL DEL NORTE SAC | 720 | Normal | Empresa sana |
| `20987654321` | TECNOLOGÍA AVANZADA PERÚ EIRL | 650 | Normal | Empresa normal |
| `20111111111` | IMPORTACIONES GLOBALES SAC | 380 | **Moroso** | Deuda SUNAT + banco |
| `20222222222` | CONSTRUCTORA ANDES SRL | 0 | **Baja** | Empresa quebrada |
| `20333333333` | SERVICIOS LOGÍSTICOS EXPRESS SAC | 690 | Normal | Empresa activa |
| `20444444444` | INVERSIONES ANDINAS SAC | 750 | Normal | Caso 2do nivel |
| `20555555555` | CONSURSUR SAC | 710 | Normal | Caso red profunda (nivel 0) |
| `20666666666` | DISTRIBUIDORA PACIFICO SRL | 680 | Normal | Red profunda (nivel 1) |
| `20777777777` | MINERA ALTIPLANO SAC | 480 | CPP | Red profunda (nivel 2) |

### Escenarios de Prueba

| Escenario | DNI | RUC | Resultado Esperado |
|-----------|-----|-----|-------------------|
| Caso APROBADO | `12345678` | `20123456789` | Score: 100, APROBAR |
| Caso RECHAZADO | `11111111` | `20111111111` | Score: 0, RECHAZAR |
| Caso con red problemática | `87654321` | `20111111111` | Score: 0, RECHAZAR (por asociacion) |
| **Caso relaciones 2do nivel** | `66666666` | `20444444444` | Score reducido, REVISION (socios problematicos) |
| **Caso red profunda (3 niveles)** | `99999999` | `20555555555` | Problemas en niveles 2 y 3 |

#### Detalle del Caso "Relaciones de 2do Nivel"

- **Solicitante** (DNI `66666666` - Ricardo Vargas): Score 780, Normal
- **Empresa** (RUC `20444444444` - INVERSIONES ANDINAS SAC): Score 750, Normal
- **Relaciones problematicas**:
  - Eduardo Quispe (DNI `77777777`): Socio 35%, MOROSO, score 320
  - Carmen Flores (DNI `88888888`): Socia 25%, CASTIGADA, score 250

#### Detalle del Caso "Red Profunda (3 niveles)"

Cadena de relaciones para probar profundidad de analisis:

- **Nivel 0**: Alberto Ramirez (`99999999`, Normal, score 720) + CONSURSUR SAC (`20555555555`, Normal)
- **Nivel 1**: Fernando Gutierrez (`10101010`, Normal, score 700) - socio de CONSURSUR, gerente de DISPACIF (`20666666666`)
- **Nivel 2**: Gabriela Torres (`20202020`, CPP, score 520) - socia de DISPACIF, socia de MINERA ALTIPLANO (`20777777777`, CPP)
- **Nivel 3**: Hector Villanueva (`30303030`, Castigado, score 180) e Isabel Chavez (`40404040`, Morosa, score 350) - socios de MINERA ALTIPLANO

Probar con profundidad 2 para ver hasta nivel 2, o profundidad 3 para ver la cadena completa.

---

## Configuración

### appsettings.Development.json (API)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "Equifax": {
    "BaseUrl": "https://api.sandbox.equifax.com",
    "ClientId": "TU_CLIENT_ID_AQUI",
    "ClientSecret": "TU_CLIENT_SECRET_AQUI",
    "Scope": "https://api.equifax.com/business/credit",
    "TimeoutSeconds": 30,
    "CacheMinutes": 30,
    "UseSandbox": true,
    "UseMock": true
  }
}
```

### Para usar Equifax Real

Cambiar en `appsettings.Development.json`:

```json
{
  "Equifax": {
    "UseMock": false,
    "ClientId": "TU_CLIENT_ID_REAL",
    "ClientSecret": "TU_CLIENT_SECRET_REAL"
  }
}
```

---

## Cómo Ejecutar

### Compilar

```bash
cd "/Users/msouga/Trabajo/RSM/Intcomex/IA Verificacion Clientes/VerificacionCrediticia"
dotnet build
```

### Ejecutar Tests

```bash
dotnet test
```

### Ejecutar API (con mock)

```bash
cd src/VerificacionCrediticia.API
ASPNETCORE_ENVIRONMENT=Development
dotnet run --urls "http://localhost:5100"
```

### Ejecutar Dashboard

```bash
cd src/VerificacionCrediticia.Web
dotnet run --urls "http://localhost:5200"
```

### URLs

- **Dashboard**: http://localhost:5200
- **Nueva Evaluación**: http://localhost:5200/evaluar
- **API Swagger**: http://localhost:5100/swagger

### Detener Servicios

```bash
pkill -f "dotnet.*VerificacionCrediticia"
```

---

## Integración con Equifax Perú

### Portal de Desarrolladores

- **URL**: https://developer.equifax.com/
- **Documentación**: https://developer.equifax.com/documentation

### Autenticación

Equifax usa **OAuth 2.0** (client credentials):

```bash
curl -X POST https://api.sandbox.equifax.com/v2/oauth/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials" \
  -d "client_id=TU_CLIENT_ID" \
  -d "client_secret=TU_CLIENT_SECRET" \
  -d "scope=https://api.equifax.com/business/credit"
```

### Endpoints a Adaptar

Los endpoints en `EquifaxApiClient.cs` son genéricos y deben adaptarse a la API real de Equifax Perú:

- `/v1/credit/person/{dni}` → Consulta de persona
- `/v1/credit/business/{ruc}` → Consulta de empresa
- `/v1/relations/person/{dni}/companies` → Empresas donde es socio
- `/v1/relations/company/{ruc}/partners` → Socios de una empresa

---

## Próximos Pasos: Integración con Azure AI

### Arquitectura Propuesta

```
┌─────────────────────────────────────────────────────────────────┐
│                         AZURE                                    │
│  ┌─────────────┐    ┌──────────────┐    ┌───────────────────┐  │
│  │ Tu API      │───▶│ Azure OpenAI │───▶│ Análisis de       │  │
│  │ (.NET)      │    │ (GPT-4o)     │    │ Patrones + Resumen│  │
│  └─────────────┘    └──────────────┘    └───────────────────┘  │
│         │                                                        │
│         │           ┌──────────────┐    ┌───────────────────┐  │
│         └──────────▶│ Azure ML     │───▶│ Scoring Predictivo│  │
│          (futuro)   │ (cuando haya │    │ (modelo propio)   │  │
│                     │  datos)      │    └───────────────────┘  │
│                     └──────────────┘                            │
└─────────────────────────────────────────────────────────────────┘
```

### Funcionalidades de IA Planificadas

1. **Azure OpenAI Service (GPT-4o)**
   - Análisis inteligente del grafo de relaciones
   - Detección de patrones sospechosos
   - Generación de resúmenes ejecutivos en lenguaje natural
   - No requiere entrenamiento

2. **Azure Machine Learning (Futuro)**
   - Scoring predictivo con modelo propio
   - Requiere datos históricos etiquetados
   - Predicción de probabilidad de mora

### Requisitos para Azure OpenAI

1. Suscripción de Azure
2. Recurso de Azure OpenAI Service
3. Deployment de modelo (GPT-4o recomendado)
4. Endpoint + API Key

### Configuración Futura

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://tu-recurso.openai.azure.com/",
    "ApiKey": "TU_API_KEY",
    "DeploymentName": "gpt-4o",
    "ApiVersion": "2024-02-15-preview"
  }
}
```

---

## Contacto Equifax Perú

- **Teléfono**: (01) 415-0333
- **Horario**: Lunes a Viernes 08:30 - 18:00
- **Portal**: https://soluciones.equifax.com.pe/efx-portal-web/
- **Catálogo de Soluciones**: https://assets.equifax.com/marketing/peru/assets/catalogo_de_soluciones.pdf

---

## Notas Técnicas

- **.NET 8** - Framework principal
- **Blazor Server** - Dashboard interactivo
- **Algoritmo BFS** - Para exploración del grafo de relaciones
- **OAuth 2.0** - Autenticación con Equifax
- **Cache en memoria** - Para evitar consultas repetidas a Equifax

---

*Documento generado el 20 de Enero de 2026*

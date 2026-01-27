# Recursos de Azure para Sistema de Verificación Crediticia

## Resumen del Proyecto

Sistema de evaluación de solicitudes de crédito que analiza la red de relaciones empresariales entre personas (DNI) y empresas (RUC), consultando datos de Equifax Perú.

**Componentes principales:**

- API REST (.NET 8)
- Dashboard Blazor Server
- Integración con Equifax Perú
- Análisis con Inteligencia Artificial (Azure OpenAI)

---

## Recursos de Azure Requeridos

### 1. Infraestructura Base (Aplicación)

| Recurso | Propósito | SKU Recomendado |
|---------|-----------|-----------------|
| Azure App Service | Hospedar la API (.NET 8) | B1/S1 (desarrollo) o P1v3 (producción) |
| Azure App Service | Hospedar el Dashboard Blazor | B1/S1 (desarrollo) o P1v3 (producción) |
| Azure SQL Database | Persistencia de datos | Basic/S0 (desarrollo) o S1+ (producción) |
| Azure Key Vault | Almacenar secretos (API keys Equifax, conexiones) | Standard |
| Azure Application Insights | Monitoreo y diagnóstico | Pay-as-you-go |

### 2. Componentes de Inteligencia Artificial

| Recurso | Propósito | Configuración |
|---------|-----------|---------------|
| Azure OpenAI Service | Análisis inteligente del grafo de relaciones, detección de patrones sospechosos, generación de resúmenes ejecutivos | Deployment de modelo GPT-4o |
| Azure Machine Learning (futuro) | Scoring predictivo con modelo entrenado con datos históricos | Workspace + Compute Instance |

**Funcionalidades de IA incluidas:**

- Análisis inteligente del grafo de relaciones empresariales
- Detección automática de patrones sospechosos
- Generación de resúmenes ejecutivos en lenguaje natural
- Scoring predictivo de riesgo crediticio (futuro)

### 3. Networking y Seguridad (Recomendado para Producción)

| Recurso | Propósito |
|---------|-----------|
| Azure Virtual Network | Aislamiento de red entre componentes |
| Azure API Management | Gateway para la API, control de acceso, rate limiting |
| Azure Front Door o Application Gateway | Balanceo de carga, Web Application Firewall (WAF) |

---

## Configuraciones Recomendadas

### Opción A: Desarrollo / Proof of Concept (POC)

| Recurso | Cantidad | Tier |
|---------|----------|------|
| Azure OpenAI Service | 1 | Standard (con deployment GPT-4o) |
| Azure App Service Plan | 1 | B1 (compartido para ambas apps) |
| Azure App Service | 2 | API + Dashboard Web |
| Azure Key Vault | 1 | Standard |
| Application Insights | 1 | Pay-as-you-go |

**Costo estimado:** $150 - $250 USD/mes

### Opción B: Producción

| Recurso | Cantidad | Tier |
|---------|----------|------|
| Azure OpenAI Service | 1 | Standard (GPT-4o con mayor cuota de tokens) |
| Azure App Service Plan | 1 | P1v3 |
| Azure App Service | 2 | API + Dashboard Web |
| Azure SQL Database | 1 | S1 |
| Azure Key Vault | 1 | Standard |
| Application Insights | 1 | Pay-as-you-go |
| Azure API Management | 1 | Developer o Basic |
| Azure Machine Learning | 1 | Workspace (cuando haya datos históricos) |

**Costo estimado:** $500 - $1,000 USD/mes (varía según volumen de uso de OpenAI)

---

## Requisitos Previos

1. **Suscripción de Azure activa** con permisos de administrador
2. **Solicitar acceso a Azure OpenAI Service** - Requiere aprobación de Microsoft (proceso de 1-5 días hábiles)
3. **Credenciales de Equifax Perú** para ambiente productivo
4. **Dominio personalizado** (opcional) para las aplicaciones web

---

## Proceso de Aprovisionamiento Sugerido

1. Crear Resource Group para el proyecto
2. Solicitar acceso a Azure OpenAI Service
3. Crear Azure Key Vault y almacenar secretos
4. Crear App Service Plan
5. Desplegar Azure App Services (API y Web)
6. Crear recurso Azure OpenAI y deployment de GPT-4o
7. Configurar Application Insights
8. Configurar CI/CD con Azure DevOps o GitHub Actions

---

## Configuración de Azure OpenAI

Una vez aprobado el acceso, se requiere:

```
Endpoint: https://<tu-recurso>.openai.azure.com/
API Key: <generada en Azure Portal>
Deployment Name: gpt-4o
API Version: 2024-02-15-preview
```

---

## Contacto

Para dudas sobre este documento o el proyecto, favor de comunicarse con el equipo de desarrollo.

---

*Documento generado el 21 de Enero de 2026*

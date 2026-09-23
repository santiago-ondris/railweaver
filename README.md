# RailWeaver

**Plan. Build. Simulate. Understand.**

Sandbox de planificación y simulación ferroviaria. Primer mundo: provincia de Córdoba, Argentina.

> Software de simulación, análisis y experimentación. No es un sistema certificado de señalización ni una herramienta para operar o aprobar infraestructura real. Ver [non-goals](docs/vision/non-goals.md).

Estado actual y próximos pasos: [docs/status/project-status.md](docs/status/project-status.md).

## Requisitos

- .NET SDK 10 (ver `global.json`)
- Node.js 20.19+ o 22.12+ (CI usa 24)
- Docker con Docker Compose (PostGIS y obtención offline del DEM)

## Estructura

```text
src/RailWeaver.Core     núcleo: dominio, simulación, planificación (sin dependencias de frameworks)
src/RailWeaver.Api      API HTTP delgada (ASP.NET Core)
src/web                 frontend React + TypeScript + Vite
tests/                  tests .NET (xUnit v3)
docs/                   visión, arquitectura, ADRs, specs, investigación, estado (usable como vault de Obsidian)
compose.yaml            PostgreSQL + PostGIS para desarrollo
```

## Comandos

```bash
dotnet build
dotnet test
docker compose up -d --wait
dotnet run --project src/RailWeaver.Api
npm --prefix src/web install
npm --prefix src/web run dev
```

Para habilitar el relieve, las cotas y los perfiles de Córdoba, descargá y generá
el dataset local (queda fuera de git):

```bash
python3 tools/fetch-elevation.py
```

La primera ejecución también descarga la imagen fijada de GDAL. Los tiles y la
grilla intermedia se eliminan al terminar. Si no vas a regenerar el dataset podés
borrar esa imagen con `docker image rm ghcr.io/osgeo/gdal:ubuntu-small-3.11.4`.
Sin el dataset, el resto de la aplicación funciona normalmente con terreno plano.

Con el relieve instalado, abrí **Corredor** en Herramientas, marcá origen y destino
en el mapa, elegí una pendiente máxima entre 1 y 40 ‰ y pulsá **Generar corredor**.
El panel de detalle muestra la longitud, pendientes, cotas y cortes o rellenos
implícitos; el dock compara el perfil del terreno con la línea de vía. Si la malla
no encuentra un trazado que respete el límite, la herramienta lo informa y permite
ajustar los puntos o el límite. El resultado es preliminar: no incluye túneles,
puentes, curvas ni costos de suelo. Los cuatro botones de pendiente son referencias
a validar, no valores normativos certificados.

Para buscar un recorrido por las vías existentes, abrí **Ruta**, elegí dos estaciones
en el mapa o por nombre y pulsá **Buscar ruta**. La herramienta muestra la trocha,
la distancia, las inversiones de marcha y el perfil del terreno cuando hay DEM.
Podés incluir vías en desuso; las abandonadas nunca se usan. La capa **Diagnóstico
de red** señala posibles cortes de datos, extremos, uniones cerradas y vías sin
trocha. El cálculo sigue la geometría de OpenStreetMap: no estima velocidades ni
tiempos de viaje.

La API escucha en `http://localhost:5080`; el frontend en `http://localhost:5173` y redirige `/api` a la API.

Frontend: `npm --prefix src/web run lint`, `npm --prefix src/web run format:check` y `npm --prefix src/web run build`.

## Documentación

- [Kickoff del proyecto](RailWeaver_Project_Kickoff.md) — cómo entendemos RailWeaver hoy.
- [Visión](docs/vision/product-vision.md) · [Principios](docs/vision/principles.md) · [Non-goals](docs/vision/non-goals.md)
- [Arquitectura](docs/architecture/overview.md) · [ADRs](docs/decisions/README.md)
- [Cómo trabajar con agentes](docs/process/agent-workflow.md)
- [Specs](docs/specs/README.md) · [Investigación](docs/research/README.md)
- [CHANGELOG](CHANGELOG.md)

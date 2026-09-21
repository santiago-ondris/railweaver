# ADR-003 — PostgreSQL + PostGIS

- Status: Accepted
- Date: 2026-09-21

## Context

RailWeaver necesita persistir infraestructura y datos geográficos, y hacer consultas espaciales (intersecciones, distancias, índices espaciales).

## Decision

- Base de datos: **PostgreSQL + PostGIS**.
- Desarrollo local: `compose.yaml` con la imagen oficial `postgis/postgis:17-3.5`. Docker Compose se usa **solo** para servicios requeridos; backend y frontend corren nativamente.
- La base no contiene reglas ferroviarias: guarda datos y resuelve consultas espaciales.
- En v0.1.0 ningún proyecto se conecta todavía a la base; el acceso (driver, ORM o no) se decide cuando exista el primer requisito de persistencia.

## Consequences

- La imagen oficial solo se publica para `linux/amd64`. `compose.yaml` fija `platform: linux/amd64`, por lo que en Apple silicon corre emulada (más lenta, aceptable para desarrollo). Si el rendimiento molesta, evaluar una imagen multi-arch en un ADR.
- PostgreSQL 17 (no 18) para evitar el cambio de layout del volumen de datos introducido en las imágenes 18+ mientras no haya motivo para migrar.
- Credenciales `railweaver/railweaver` solo para desarrollo local.

## Alternatives considered

- **SpatiaLite/SQLite:** más liviano pero menos capacidad espacial y de concurrencia.
- **Archivos (GeoJSON/GeoPackage) sin base:** útil para datasets de entrada, insuficiente para consultas y persistencia del mundo.

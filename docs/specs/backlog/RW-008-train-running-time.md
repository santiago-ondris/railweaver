# RW-008 — Tren V1 y tiempo de recorrido

- Status: Backlog (intención)
- Milestone / release objetivo: M1 — Primer tren → V0.8, `v0.8.0`

## Goal

Definir un tren con material rodante V1 (largo, velocidad máxima, aceleración,
frenado; Kickoff §7) y calcular de forma determinista su perfil de velocidad y
su tiempo de recorrido sobre un camino de la red, con límites de velocidad por
curva. Resultado explicable: dónde y por qué acelera, frena o va limitado.

## Por qué ahora

Separa la física del recorrido del motor de eventos (RW-009): es cálculo puro en
el core, fácil de testear.

## Investigación previa

- Cálculo de tiempo de recorrido (running time calculation) y sus simplificaciones.
- Efecto de la pendiente: si entra ya (V2 parcial) o queda fuera.
- Parámetros de referencia de material rodante presente en Argentina.

## Preguntas abiertas

- ¿El gráfico velocidad-distancia comparte componente con el perfil longitudinal?
- Velocidades en curvas de la red existente: RW-007 solo mide curvas del corredor
  (decisión 1) y entrega `Sections` con `SpeedLimitKmh`. Si el tiempo de recorrido
  se calcula también sobre rutas de la red OSM, hay que decidir cómo estimar sus
  curvas (la geometría de OSM tiene ruido de digitalización) o recorrerlas sin
  límite por curva.

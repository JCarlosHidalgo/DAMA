# 3.3 Diseño del sistema

> **Estado:** ✅ Redactado. Introducción de la sección 3.3 (Diseño del sistema) según
> **IEEE 1016-2009**. **Guía:** [2-diseno-ieee-1016.md](../2-diseno-ieee-1016.md).

---

## Propósito de la sección

Esta sección **describe cómo está construido DAMA** para satisfacer los requisitos especificados en
3.2. Documenta la **Software Design Description (SDD)** de la plataforma: la organización en
microservicios detrás del gateway, los actores y sus interacciones, la estructura de clases del
dominio, el modelo de datos, los flujos dinámicos clave, la interfaz de usuario y el despliegue.
Su función es dejar el diseño **trazable y verificable**: cada decisión de diseño responde a un
requisito de 3.2 y se apoya en artefactos reales del repositorio (init.sql, diagramas generados,
contratos gRPC), de modo que el lector pueda confirmar que lo descrito corresponde al sistema que
existe en `main`.

En conjunto, la sección recorre la arquitectura, los casos de uso, el diagrama de clases, el modelo
de datos y diccionario, los diagramas de secuencia, el diseño de interfaz y el diagrama de
despliegue.

## Convención aplicada — IEEE 1016-2009

**Qué es:** el estándar para la **Software Design Description (SDD)**. Su idea central son los
**design viewpoints** (puntos de vista): el diseño no se documenta como un único bloque, sino desde
varias vistas complementarias —contexto/funcional, arquitectura, estructural, datos,
dinámica/interacción y despliegue—, cada una respondiendo a una preocupación distinta.

**Cómo se aplica en 3.3:** cada sub-sección corresponde a un *viewpoint* de 1016 (3.3.1
arquitectura → vista de arquitectura; 3.3.2 casos de uso → vista funcional; 3.3.3 clases → vista
estructural; 3.3.4 modelo de datos → vista de datos; 3.3.5 secuencia → vista dinámica; 3.3.7
despliegue → vista de despliegue), y 3.3.6 añade la perspectiva de interfaz (UI/UX). Siguiendo la
recomendación del estándar de **diagramar solo lo crítico**, los diagramas no se dibujan a mano:
los estructurales y de componentes se **generan desde el código** con la herramienta de cada stack
(Doxygen para los backends, Compodoc para el frontend, FossFlow para la vista isométrica de
despliegue), y solo casos de uso y secuencia se **autoran en PlantUML**; el documento referencia el
tipo de diagrama y el comando que lo produce, no embebe UML manual.

## Contenido de la sección

### 3.3.1 Arquitectura

Justifica el patrón de **microservicios** detrás de un *api-gateway* nginx con SPA Angular como
cliente, y describe la organización en servicios, sus límites y la vista isométrica del stack.
→ [`3.1-arquitectura.md`](3.1-arquitectura.md)

### 3.3.2 Casos de uso

Documenta la **vista funcional**: qué hace cada actor (Admin, Client, Teacher, Student) con el
sistema, con diagramas autorados en PlantUML.
→ [`3.2-casos-de-uso.md`](3.2-casos-de-uso.md)

### 3.3.3 Diagrama de clases

Presenta la **vista estructural**: las entidades del dominio y sus relaciones, mediante el grafo de
clases/colaboración generado con Doxygen.
→ [`3.3-diagrama-de-clases.md`](3.3-diagrama-de-clases.md)

### 3.3.4 Modelo de datos y diccionario de datos

Describe la **vista de datos**: el esquema de cada base MySQL y el diccionario tabla por tabla,
tomando como fuente de verdad los `init.sql` por servicio.
→ [`3.4-modelo-de-datos-y-diccionario.md`](3.4-modelo-de-datos-y-diccionario.md)

### 3.3.5 Diagramas de secuencia (flujos clave)

Documenta la **vista dinámica** de los flujos críticos (p. ej. toma de asistencia, pago QR,
propagación de eventos), autorados en PlantUML —no se diagrama todo, solo lo crítico.
→ [`3.5-diagramas-de-secuencia.md`](3.5-diagramas-de-secuencia.md)

### 3.3.6 Diseño de interfaz (UI/UX)

Cubre la perspectiva de **interfaz de usuario**: principios de la SPA Angular, navegación y
criterios de usabilidad, enlazando con las características de calidad de 25010.
→ [`3.6-diseno-de-interfaz.md`](3.6-diseno-de-interfaz.md)

### 3.3.7 Diagrama de despliegue

Presenta la **vista de despliegue**: contenedores, servicios, bases de datos y nube, mediante la
vista isométrica `dama-architecture.json` generada con FossFlow.
→ [`3.7-diagrama-de-despliegue.md`](3.7-diagrama-de-despliegue.md)

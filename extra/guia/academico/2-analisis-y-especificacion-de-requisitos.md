# 3.2 Análisis y especificación de requisitos

> **Estado:** ✅ Redactado. Introducción de la sección 3.2 (Análisis y especificación de
> requisitos) según **ISO/IEC/IEEE 29148:2018**. **Guía:**
> [1-requisitos-iso-29148.md](../1-requisitos-iso-29148.md).

---

## Propósito de la sección

Esta sección **establece qué debe hacer DAMA y bajo qué condiciones**, antes de describir cómo se
construye. Constituye la **Especificación de Requisitos de Software (SRS)** de la plataforma:
delimita el alcance del producto, caracteriza a los usuarios que lo operan, enuncia las capacidades
funcionales y las propiedades de calidad exigibles, y cierra demostrando que cada requisito se
remonta a un objetivo del proyecto. Su función es servir de **contrato verificable** —no de una
aspiración— entre lo que se pidió y lo que el código en `main` realmente implementa, de modo que
las secciones de diseño (3.3) y pruebas (3.5) puedan trazarse de vuelta hasta aquí.

En conjunto, la sección recorre la descripción general del sistema, el modelo de roles
multitenant, los requisitos funcionales (RF) y no funcionales (RNF), y la matriz de trazabilidad
que los conecta con los objetivos específicos.

## Convención aplicada — ISO/IEC/IEEE 29148:2018

**Qué es:** el estándar internacional vigente de **ingeniería de requisitos**, sucesor de la
clásica IEEE 830-1998. Define el contenido que debe llevar un SRS —propósito y alcance,
perspectiva del producto, características de los usuarios, requisitos funcionales, no funcionales,
de interfaz externa y restricciones— y los **atributos de calidad** que debe cumplir cada
requisito: *atómico*, *verificable*, *inequívoco*, *trazable* y *necesario*.

**Cómo se aplica en 3.2:** el contenido se organiza siguiendo las subsecciones del SRS de 29148
(la 3.2.1 cubre las subsecciones 1 y 2 —propósito, alcance y perspectiva—; la 3.2.2 la
subsección 3 —características de los usuarios—; la 3.2.3 la subsección 4 —RF—; la 3.2.4 las
subsecciones 5–7 —RNF, interfaces y restricciones—). Los atributos de calidad se materializan
escribiendo cada RF en la forma normativa *"El sistema deberá…"* y anclándolo a su **evidencia**
(`ruta:línea` del endpoint que lo implementa y del caso de prueba que lo verifica); la
**trazabilidad** del estándar se hace explícita en 3.2.5, que cruza cada objetivo específico con
los requisitos que lo satisfacen.

## Contenido de la sección

### 3.2.1 Descripción general del sistema

Enuncia el **propósito** de DAMA, su **alcance y delimitación** por dominio (qué entra y qué queda
fuera) y la **perspectiva del producto** —los sistemas con los que interactúa y el diagrama de
contexto—. Cubre las subsecciones 1 y 2 del SRS.
→ [`2.1-descripcion-general-del-sistema.md`](2.1-descripcion-general-del-sistema.md)

### 3.2.2 Roles de usuario (multitenancy)

Describe el **modelo multitenant** y el catálogo de roles (Admin, Client, Teacher, Student), su
alcance y la política de autorización *default-deny*. Cubre la subsección 3 del SRS (características
de los usuarios).
→ [`2.2-roles-de-usuario.md`](2.2-roles-de-usuario.md)

### 3.2.3 Requisitos funcionales

Lista los **RF** agrupados por dominio en la forma *"El sistema deberá…"*, cada uno trazado a su
endpoint y caso de prueba. Cubre la subsección 4 del SRS.
→ [`2.3-requisitos-funcionales.md`](2.3-requisitos-funcionales.md)

### 3.2.4 Requisitos no funcionales

Enuncia los **RNF** —seguridad, rendimiento, usabilidad, escalabilidad— y las restricciones
tecnológicas, apoyándose en el catálogo OWASP como fuente de los RNF de seguridad. Cubre las
subsecciones 5–7 del SRS.
→ [`2.4-requisitos-no-funcionales.md`](2.4-requisitos-no-funcionales.md)

### 3.2.5 Matriz de trazabilidad

Cruza cada **objetivo específico** del proyecto con los requisitos que lo satisfacen, cerrando el
atributo de *trazabilidad* exigido por 29148 y evidenciando que no hay requisitos huérfanos ni
objetivos sin cubrir.
→ [`2.5-matriz-de-trazabilidad.md`](2.5-matriz-de-trazabilidad.md)

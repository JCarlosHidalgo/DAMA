# Plantilla — Diagramado detallado de un servicio (3.3.3)

> **Qué es esto.** Plantilla reutilizable para el **diagramado detallado de clases** de cada servicio
> del backend (y, con su variante propia, del frontend). Cada archivo de esta carpeta
> (`credentials.md`, `auth.md`, …) se redacta siguiendo exactamente esta estructura. Complementa la
> sección [3.3.3 Diagrama de clases](../3.3-diagrama-de-clases.md), que da la vista general; aquí se
> documenta **clase por clase** apoyándose en los grafos que **Doxygen** genera desde el código.

## Reglas de redacción (obligatorias)

1. **Todo se explica a través de los diagramas.** No se describe la función específica de cada
   método; solo se explican las **relaciones** que muestran los diagramas de jerarquía, herencia y
   colaboración.
2. **Solo se enumeran las clases/interfaces implementadas en el servicio.** Las clases/interfaces
   **externas** (del framework .NET, ASP.NET Core, paquetes NuGet, etc.) **no** se enumeran como
   ítem propio: se **referencian** desde la explicación de la clase implementada que las usa.
3. **Sin abreviaciones** en los nombres de grupo ni en las explicaciones (criterio del proyecto).
4. **Los títulos de figura se escriben para copiarse al documento final**, donde se pegará la imagen
   correspondiente generada por Doxygen. No se dibuja UML a mano en el Markdown.
5. **Trazabilidad:** cuando aporte, enlazar la clase a su requisito (3.2.3/3.2.4) o patrón (3.3.3.3).

> **Cómo generar las imágenes** (mismas que cita esta plantilla):
> ```bash
> cd extra/graphics && docker compose --profile docs run --rm doxygen
> # Salida (gitignored): extra/graphics/out/doxygen/html/  → grafos de jerarquía, herencia y colaboración
> ```

---

## 3.3.3.N Diagramado del servicio «Nombre»

> El índice `N` se asigna al ensamblar el capítulo, en el orden de los servicios (p. ej. Auth = 4,
> CourseManagement = 5, …). El piloto de esta carpeta es **Credentials**.

### a) Jerarquía gráfica

Párrafo introductorio: explicar **cómo se organiza el código del servicio a través de sus
namespaces** y de las **reglas de los patrones estructurales** (segregación de interfaces,
composición por módulos, capa de servicio, etc.). Indicar qué grupos estructurales **existen** y
cuáles **no aplican** (y por qué) en este servicio.

Para **cada grupo estructural** del servicio se incluye un título de figura y, debajo, la **función**
de ese grupo:

> **Figura: Jerarquía gráfica de «descripción del grupo» (Clase1, Clase2, Interfaz1)**

Texto: qué función cumple ese grupo de clases dentro del servicio (rol estructural, no métodos).

*(Repetir un bloque «Figura + función» por cada grupo: datos de entrada/salida, modelo persistente,
acceso a datos, capa de servicio/aplicación, controladores, builders, claims, seguridad, módulos de
composición, etc., según los que el servicio realmente tenga.)*

### b) Diagramas de herencia y colaboración

Lista numerada con **una entrada por clase/interfaz implementada** en el servicio. Formato de cada
entrada:

1. **NombreDeLaClase** — explicación breve del **propósito** de la clase/interfaz (qué rol cumple),
   sin describir sus métodos.

   > **Figura: Diagrama de herencia para NombreDeLaClase**

   - Relación UML con cada clase/interfaz **externa** (o implementada) que aparece en el grafo de
     herencia (p. ej. «implementa la interfaz externa `X`», «hereda de la clase externa `Y`»).

   > **Figura: Diagrama de colaboración para NombreDeLaClase**

   - Cómo se relaciona con cada elemento del grafo de colaboración (p. ej. «recibe por inyección de
     dependencias la interfaz `X`», «construye y devuelve el objeto `Y`», «usa las constantes de
     `Z`»). Una viñeta por relación.

*(Repetir la entrada numerada por cada clase/interfaz implementada. Las externas referenciadas en las
viñetas no llevan entrada propia.)*

### Comandos de demostración

```bash
# Tipos implementados en el servicio (lo que Doxygen diagrama)
find apps/«Servicio»/Backend -name "*.cs" -not -path "*/obj/*" | sort

# Generar los grafos de jerarquía/herencia/colaboración de este servicio
cd extra/graphics && docker compose --profile docs run --rm doxygen
```

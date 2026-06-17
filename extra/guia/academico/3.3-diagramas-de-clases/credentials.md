# 3.3.3.6 Diagramado del servicio Credentials (piloto)

> **Piloto** del diagramado detallado de clases (3.3.3), elegido por ser el servicio con **menos
> clases**. Estructura definida en [`_plantilla.md`](_plantilla.md). Todos los grafos citados los
> genera **Doxygen** desde el código (`UML_LOOK`, `GRAPHICAL_HIERARCHY`, `COLLABORATION_GRAPH`); aquí
> solo se titulan las figuras y se explican las **relaciones** que muestran, sin describir métodos.
>
> **Generar las figuras:** `cd extra/graphics && docker compose --profile docs run --rm doxygen`
> (salida en `extra/graphics/out/doxygen/html/`).

---

## a) Jerarquía gráfica

Credentials es un servicio **sin estado**: refleja al cliente los *claims* del usuario autenticado y
no tiene base de datos. Su código se organiza por **namespaces**, y cada namespace corresponde a un
**rol estructural**:

- `Backend.Claims` — la **abstracción de claims** (lectura tipada del token).
- `Backend.Dtos.Output` — los **datos de salida** de la API.
- `Backend.Services.Abstract` / `Backend.Services.Concrete` — la **capa de servicio** (contrato +
  implementación), siguiendo la **segregación de interfaces**.
- `Backend.Controllers` — el **controlador** de la API REST.
- `Backend.Security` — las **constantes de seguridad** (nombres de claim y roles).
- `Backend.Modules` — la **composición por módulos**: cada módulo registra/configura una porción del
  arranque, descubierta y ordenada por un anfitrión.

Por ser sin estado, **no existen** los grupos «modelo persistente» (entidades de base de datos) ni
«acceso a datos» (objetos de acceso a datos) que sí aparecen en los demás servicios; tampoco hay
*builders*. Esta ausencia es una **decisión de diseño** (Credentials no posee base de datos), no una
omisión. El punto de entrada `Program` usa instrucciones de nivel superior y delega todo el arranque
al anfitrión de módulos, por lo que no aporta jerarquía de clases propia.

A continuación, un título de figura por grupo estructural y la función del grupo:

> **Figura: Jerarquía gráfica de la abstracción de claims (`IClaimContext`, `ClaimContext`, `MissingClaimException`)**

Este grupo expone al resto del servicio los datos de identidad y de academia contenidos en el token,
de forma tipada y con **fallo rápido**: si un claim falta o está malformado, se interrumpe con una
excepción específica en lugar de devolver un valor vacío.

> **Figura: Jerarquía gráfica de los datos de salida (`UserClaimsDto`)**

Define la forma del objeto que la API devuelve al cliente: la proyección, como cadenas de texto, de
la identidad y la academia del usuario.

> **Figura: Jerarquía gráfica de la capa de servicio (`ICredentialsService`, `CredentialsService`)**

Encapsula la única responsabilidad de negocio del servicio —ensamblar el objeto de salida a partir
de los claims— detrás de un contrato (la interfaz) y su implementación, de modo que el controlador
dependa del contrato y no de la implementación.

> **Figura: Jerarquía gráfica del controlador de la API (`CredentialsController`)**

Expone los tres puntos de acceso por rol (cliente, profesor, estudiante) y delega en la capa de
servicio; concentra la autorización por rol en la frontera de la API.

> **Figura: Jerarquía gráfica de las constantes de seguridad (`AuthClaims`, `UserRoles`)**

Centraliza, como constantes, los nombres de los claims del token y los nombres de los roles, para que
ni el controlador ni la abstracción de claims usen cadenas de texto sueltas.

> **Figura: Jerarquía gráfica de la composición de módulos (`IServiceModule`, `IAppModule`, `ModuleHost`, y los módulos `*Module`)**

Implementa el patrón de **arranque modular**: dos contratos (uno para registrar servicios, otro para
configurar la canalización de la aplicación) que cada módulo implementa, y un anfitrión que los
descubre por reflexión y los ejecuta ordenados. Es el grupo más numeroso y el que define el orden de
arranque del servicio.

---

## b) Diagramas de herencia y colaboración

Una entrada por cada clase/interfaz **implementada** en Credentials. Las clases/interfaces externas
(del framework .NET y ASP.NET Core, o de paquetes NuGet) se **referencian** desde las viñetas, sin
entrada propia.

### Abstracción de claims

1. **`IClaimContext`** — contrato que define la lectura tipada de los seis claims (identificador y
   nombre de la academia, zona horaria, identificador y nombre del usuario, rol).

   > **Figura: Diagrama de herencia para `IClaimContext`**
   - Es una interfaz **raíz**: no hereda de ninguna otra interfaz.

   > **Figura: Diagrama de colaboración para `IClaimContext`**
   - Sus propiedades son de tipos de valor del lenguaje (`Guid`, `string`), por lo que el grafo no
     muestra dependencia con otros tipos implementados; es un contrato puro.

2. **`ClaimContext`** — implementación que lee cada claim del usuario autenticado, con memorización
   en la primera lectura y fallo rápido ante ausencia.

   > **Figura: Diagrama de herencia para `ClaimContext`**
   - Implementa la interfaz implementada `IClaimContext`.

   > **Figura: Diagrama de colaboración para `ClaimContext`**
   - Recibe por inyección de dependencias la interfaz externa `IHttpContextAccessor` (de ASP.NET
     Core), desde la cual obtiene los claims del usuario.
   - Usa las constantes de la clase implementada `AuthClaims` para nombrar cada claim que lee.
   - Construye y lanza la clase implementada `MissingClaimException` cuando un claim falta o está
     malformado.

3. **`MissingClaimException`** — excepción específica que señala un claim requerido ausente o
   malformado.

   > **Figura: Diagrama de herencia para `MissingClaimException`**
   - Hereda de la clase externa `System.Exception`.

   > **Figura: Diagrama de colaboración para `MissingClaimException`**
   - La construye y lanza la clase implementada `ClaimContext`.
   - Conserva el nombre del claim afectado como propiedad de texto.

### Datos de salida

4. **`UserClaimsDto`** — objeto de transferencia que la API devuelve: la identidad y la academia del
   usuario proyectadas como cadenas de texto.

   > **Figura: Diagrama de herencia para `UserClaimsDto`**
   - Es una clase **raíz**: no hereda de ninguna otra ni implementa interfaces.

   > **Figura: Diagrama de colaboración para `UserClaimsDto`**
   - La construye la clase implementada `CredentialsService`.
   - La devuelven la interfaz implementada `ICredentialsService` y el controlador implementado
     `CredentialsController`.

### Capa de servicio

5. **`ICredentialsService`** — contrato de la única operación de negocio: obtener los *claims* del
   usuario.

   > **Figura: Diagrama de herencia para `ICredentialsService`**
   - Es una interfaz **raíz**.

   > **Figura: Diagrama de colaboración para `ICredentialsService`**
   - Su operación devuelve la clase implementada `UserClaimsDto`, envuelta en el tipo externo `Task`
     (operación asíncrona).

6. **`CredentialsService`** — implementación que ensambla el objeto de salida a partir de los claims.

   > **Figura: Diagrama de herencia para `CredentialsService`**
   - Implementa la interfaz implementada `ICredentialsService`.

   > **Figura: Diagrama de colaboración para `CredentialsService`**
   - Recibe por inyección de dependencias la interfaz implementada `IClaimContext`, de la que lee los
     claims.
   - Construye y devuelve la clase implementada `UserClaimsDto`.

### Controlador de la API

7. **`CredentialsController`** — expone los tres puntos de acceso por rol y delega en la capa de
   servicio.

   > **Figura: Diagrama de herencia para `CredentialsController`**
   - Hereda de la clase externa `ControllerBase` (de ASP.NET Core MVC).

   > **Figura: Diagrama de colaboración para `CredentialsController`**
   - Recibe por inyección de dependencias la interfaz implementada `ICredentialsService`.
   - Devuelve la clase implementada `UserClaimsDto` envuelta en el tipo externo `ActionResult`.
   - Referencia las constantes de la clase implementada `UserRoles` en sus atributos de
     autorización.

### Constantes de seguridad

8. **`AuthClaims`** — clase estática con los nombres de los claims del token.

   > **Figura: Diagrama de herencia para `AuthClaims`**
   - Clase estática **raíz**: no participa en jerarquía de herencia.

   > **Figura: Diagrama de colaboración para `AuthClaims`**
   - La consumen la clase implementada `ClaimContext` (para leer cada claim) y el módulo implementado
     `JwtAuthenticationModule` (para nombrar el claim de nombre de usuario y el de rol).

9. **`UserRoles`** — clase estática con los nombres de los roles del sistema.

   > **Figura: Diagrama de herencia para `UserRoles`**
   - Clase estática **raíz**.

   > **Figura: Diagrama de colaboración para `UserRoles`**
   - La consume la clase implementada `CredentialsController` en sus atributos de autorización por
     rol.

### Composición de módulos

10. **`IServiceModule`** — contrato de un módulo que **registra servicios** durante el arranque.

    > **Figura: Diagrama de herencia para `IServiceModule`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `IServiceModule`**
    - Su operación de registro recibe los tipos externos `IServiceCollection` e `IConfiguration`.
    - La implementan los módulos de registro del servicio (entradas 13 a 21).

11. **`IAppModule`** — contrato de un módulo que **configura la canalización** de la aplicación.

    > **Figura: Diagrama de herencia para `IAppModule`**
    - Es una interfaz **raíz**.

    > **Figura: Diagrama de colaboración para `IAppModule`**
    - Su operación de configuración recibe el tipo externo `WebApplication`.
    - La implementan los módulos que intervienen en la fase de aplicación (entradas 14, 16, 17, 19,
      20 y 21).

12. **`ModuleHost`** — anfitrión estático que descubre los módulos por reflexión y los ejecuta
    ordenados por su propiedad de orden.

    > **Figura: Diagrama de herencia para `ModuleHost`**
    - Clase estática **raíz**.

    > **Figura: Diagrama de colaboración para `ModuleHost`**
    - Descubre y orquesta las interfaces implementadas `IServiceModule` e `IAppModule`.
    - Recibe los tipos externos `WebApplicationBuilder` (fase de registro) y `WebApplication` (fase
      de configuración).

13. **`SecretsValidationModule`** — módulo que valida, antes que cualquier otro, que el secreto de la
    clave pública exista y sea válido (fallo rápido en el arranque).

    > **Figura: Diagrama de herencia para `SecretsValidationModule`**
    - Implementa la interfaz implementada `IServiceModule`.

    > **Figura: Diagrama de colaboración para `SecretsValidationModule`**
    - Usa la clase auxiliar implementada `SecretsValidation`, que valida la clave con el tipo externo
      `RSA` (de `System.Security.Cryptography`).
    - Lee el secreto del tipo externo `IConfiguration`.

14. **`ForwardedHeadersModule`** — módulo que hace respetar las cabeceras de reenvío del gateway
    (esquema, anfitrión e dirección de origen reales).

    > **Figura: Diagrama de herencia para `ForwardedHeadersModule`**
    - Implementa las interfaces implementadas `IServiceModule` e `IAppModule`.

    > **Figura: Diagrama de colaboración para `ForwardedHeadersModule`**
    - Configura el tipo externo de opciones de cabeceras reenviadas y activa su middleware sobre el
      tipo externo `WebApplication`.

15. **`HttpContextModule`** — módulo que registra el acceso al contexto de la petición.

    > **Figura: Diagrama de herencia para `HttpContextModule`**
    - Implementa la interfaz implementada `IServiceModule`.

    > **Figura: Diagrama de colaboración para `HttpContextModule`**
    - Registra la interfaz externa `IHttpContextAccessor`, de la que depende la clase implementada
      `ClaimContext`.

16. **`AuthorizationModule`** — módulo que define la autorización con política de **denegar por
    defecto** y activa su middleware.

    > **Figura: Diagrama de herencia para `AuthorizationModule`**
    - Implementa las interfaces implementadas `IServiceModule` e `IAppModule`.

    > **Figura: Diagrama de colaboración para `AuthorizationModule`**
    - Construye la política con el tipo externo `AuthorizationPolicyBuilder`, exigiendo usuario
      autenticado como política de respaldo.

17. **`JwtAuthenticationModule`** — módulo que configura la validación del token firmado y activa la
    autenticación.

    > **Figura: Diagrama de herencia para `JwtAuthenticationModule`**
    - Implementa las interfaces implementadas `IServiceModule` e `IAppModule`.

    > **Figura: Diagrama de colaboración para `JwtAuthenticationModule`**
    - Configura los tipos externos de validación de token (parámetros de validación y esquema de
      portador) y carga la clave pública con el tipo externo `RSA`.
    - Referencia las constantes de la clase implementada `AuthClaims` para indicar qué claim aporta el
      nombre de usuario y cuál el rol.

18. **`AutoRegisteredServicesModule`** — módulo que registra automáticamente, por exploración de
    namespaces, los servicios y la abstracción de claims.

    > **Figura: Diagrama de herencia para `AutoRegisteredServicesModule`**
    - Implementa la interfaz implementada `IServiceModule`.

    > **Figura: Diagrama de colaboración para `AutoRegisteredServicesModule`**
    - Usa la biblioteca externa de exploración de ensamblados para registrar las clases implementadas
      de los namespaces `Backend.Services.Concrete` y `Backend.Claims` (es decir, `CredentialsService`
      y `ClaimContext`) contra sus interfaces.

19. **`MvcModule`** — módulo que registra y mapea los controladores.

    > **Figura: Diagrama de herencia para `MvcModule`**
    - Implementa las interfaces implementadas `IServiceModule` e `IAppModule`.

    > **Figura: Diagrama de colaboración para `MvcModule`**
    - Habilita, sobre el tipo externo `WebApplication`, el enrutamiento de la clase implementada
      `CredentialsController`.

20. **`ProblemDetailsModule`** — módulo que normaliza las respuestas de error y activa el manejador de
    excepciones.

    > **Figura: Diagrama de herencia para `ProblemDetailsModule`**
    - Implementa las interfaces implementadas `IServiceModule` e `IAppModule`.

    > **Figura: Diagrama de colaboración para `ProblemDetailsModule`**
    - Registra el servicio externo de detalles de problema y activa su middleware de manejo de
      excepciones sobre el tipo externo `WebApplication`.

21. **`HealthCheckModule`** — módulo que expone la sonda de vivacidad del servicio.

    > **Figura: Diagrama de herencia para `HealthCheckModule`**
    - Implementa las interfaces implementadas `IServiceModule` e `IAppModule`.

    > **Figura: Diagrama de colaboración para `HealthCheckModule`**
    - Registra las comprobaciones de salud y mapea el punto de acceso de vivacidad sobre el tipo
      externo `WebApplication` (Credentials, al no tener dependencias externas, expone solo la sonda
      de vivacidad, no la de disponibilidad profunda).

> **Nota sobre `SecretsValidation`.** Es una clase auxiliar **interna** y estática (no se expone fuera
> del ensamblado) que acompaña a `SecretsValidationModule` (entrada 13); valida claves con el tipo
> externo `RSA`. Doxygen la grafica junto a su módulo en el diagrama de colaboración de la entrada 13.

---

## Comandos de demostración

```bash
# Tipos implementados en Credentials (lo que Doxygen diagrama)
find apps/Credentials/Backend -name "*.cs" -not -path "*/obj/*" | sort

# Relaciones de herencia (qué implementa cada módulo / clase)
grep -rn "class .*:\|interface " apps/Credentials/Backend --include=*.cs | grep -v "/obj/"

# Generar los grafos de jerarquía, herencia y colaboración del servicio
cd extra/graphics && docker compose --profile docs run --rm doxygen
#   salida: extra/graphics/out/doxygen/html/
```

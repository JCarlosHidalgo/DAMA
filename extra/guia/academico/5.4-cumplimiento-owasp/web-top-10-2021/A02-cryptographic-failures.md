# A02:2021 · Fallos Criptográficos (Cryptographic Failures)

> **Estado:** ✅ — RS256 para tokens, PBKDF2 (210k) para contraseñas, AES-256-GCM para secretos de academia, HMAC-SHA256 (comparación en tiempo constante) para webhooks y TLS para el gRPC interno. Sin algoritmos débiles ni secretos en claro.

## Qué exige OWASP
Proteger datos sensibles en reposo y en tránsito con criptografía moderna: hashes de contraseña fuertes y salados, cifrado autenticado para secretos almacenados, firmas/HMAC con comparación en tiempo constante, TLS para todo tráfico, y ausencia de algoritmos obsoletos (MD5, SHA1 para contraseñas, DES, ECB) o claves embebidas en el código.

## Cómo lo cumple DAMA

### Firma de tokens con RS256 (RSA-SHA256)
Los JWT se firman con clave RSA privada; la verificación usa la pública. Nada de secretos simétricos compartidos entre servicios. El firmante construye un `RsaSecurityKey` sobre la clave privada y crea las `SigningCredentials` con el algoritmo `SecurityAlgorithms.RsaSha256` (`apps/Auth/Backend/Security/JwtTokenSigner.cs:26-27`).

Las claves RSA llegan como PEM base64 vía env (`JWT_PRIVATE_KEY_B64` solo en Auth, `JWT_PUBLIC_KEY_B64` en todos) y se validan al arranque (`SecretsValidationModule.cs:12-15`), nunca se incrustan en el código.

### Hashing de contraseñas con PBKDF2-HMAC-SHA256 (210 000 iteraciones)
Salt aleatorio por contraseña, formato versionado del `PasswordHasher<User>` de Identity. El módulo declara la constante de 210 000 iteraciones y la aplica configurando `PasswordHasherOptions.IterationCount` (`apps/Auth/Backend/Modules/PasswordHashingModule.cs:9` y `:15-17`).

### Cifrado autenticado AES-256-GCM para secretos de academia
El `TodotixAppKey` de cada academia (credencial de pago) se almacena cifrado con AES-256-GCM: nonce aleatorio de 12 bytes por operación, tag de autenticación de 16 bytes. El payload persistido es `nonce || tag || ciphertext` en base64. El cifrador fija las constantes de tamaño de nonce (12) y de tag (16), rechaza en construcción toda clave que no tenga 32 bytes, genera un nonce aleatorio por cifrado con `RandomNumberGenerator`, cifra con `AesGcm.Encrypt` y compone el payload concatenando nonce, tag y ciphertext (`apps/Payment/Backend/Security/AppKeyCipher.cs:8-9`, `:15-18`, `:24-37`).

La clave de 32 bytes proviene de env (`TODOTIX_APPKEY`), validada en construcción. Por usar nonce aleatorio por cifrado, esta credencial **no** se puede sembrar como CSV plano (de ahí que no exista injector de credenciales; se configura por un punto de acceso que cifra y hace upsert). Cifrado/descifrado se consumen en `apps/Payment/Backend/Services/Concrete/Todotix/TodotixCredentialService.cs` y `TodotixAppKeyResolver.cs`.

### HMAC-SHA256 con comparación en tiempo constante para webhooks
Las firmas de los callbacks de pago (Todotix) se calculan con HMAC-SHA256 y se verifican con `CryptographicOperations.FixedTimeEquals`, evitando ataques de temporización. La firma se computa con `HMACSHA256.HashData` sobre el payload usando el secreto cargado (`apps/Payment/Backend/Services/Concrete/CallbackSignature.cs:16`); la verificación recalcula la firma esperada (`:27`) y la compara contra la provista en tiempo constante con `FixedTimeEquals` (`:38`).

El secreto (`PAYMENT_CALLBACK_SECRET`) llega por env. La firma de base64url malformada se atrapa y devuelve `false` sin lanzar (`:33-35`).

### Refresh tokens de alta entropía, almacenados como hash
32 bytes de `RandomNumberGenerator`; en la base solo vive su SHA-256, nunca el valor en claro (`apps/Auth/Backend/Security/RefreshTokenGenerator.cs:25`, `:38-42`). El SHA-256 aquí es indexación de un secreto de alta entropía, no hashing de contraseña (donde sí se exige PBKDF2).

### TLS para el gRPC interno entre servicios
El tráfico gRPC servicio-a-servicio está cifrado de extremo a extremo con una CA interna. `bootstrap-tls.sh` genera una CA RSA-2048 y un cert de servidor por backend que expone gRPC (`course-management`, `auth`), firmados con SHA-256 y con el hostname del contenedor como SAN. El script firma cada CSR contra la CA con `openssl x509 -req` usando `-sha256` y la vigencia de `SERVER_DAYS` (`infrastructure/environments/tls-init/bootstrap-tls.sh:55-57`), e itera sobre las entradas `course-management` y `auth` para emitir un cert por host (`:71-73`).

Los clientes (Attendance, Payment) instalan la CA en su trust store al arrancar y validan al servidor por nombre (ver A08). El TLS público (Cloudflare) cubre el borde.

## Flujo de los componentes

| Necesidad | Algoritmo | Dónde |
|---|---|---|
| Firma de access token | RS256 (RSA-SHA256) | `JwtTokenSigner.cs:27` |
| Hash de contraseña | PBKDF2-HMAC-SHA256, 210k iter | `PasswordHashingModule.cs:9` |
| Secreto de academia en reposo | AES-256-GCM (nonce 12B, tag 16B) | `AppKeyCipher.cs:24-30` |
| Firma de webhook | HMAC-SHA256 + FixedTimeEquals | `CallbackSignature.cs:27,38` |
| Refresh token | 32B aleatorios, índice SHA-256 | `RefreshTokenGenerator.cs:25,40` |
| gRPC interno | TLS (CA propia, certs SHA-256) | `bootstrap-tls.sh:55` |

Diagrama FossFlow: rectángulo **"A02 · Cryptographic Failures"** en `extra/graphics/diagrams/owasp-web-top-10.json`, nodos `RS256 JwtTokenSigner`, `PBKDF2 PasswordHasher 210k`, `AES-256-GCM AppKeyCipher`, `HMAC CallbackSignature` y `TLS gRPC interno`.

## Verificación

- Buscar algoritmos débiles en código de producción (debe dar sin coincidencias):

  ```bash
  grep -rn "Sha1\|MD5\|DES\|RijndaelManaged\|ECB" apps/
  ```
- AES-GCM: round-trip Encrypt/Decrypt en la suite de Payment; manipular un byte del ciphertext hace fallar la verificación del tag.

  ```bash
  cd apps/Payment/Test && dotnet test --filter "AppKeyCipher"
  ```
- HMAC: alterar el payload o la firma del callback → `Verify` devuelve `false`.
- TLS: con la CA instalada, los clientes gRPC validan el cert por SAN; quitar la CA del trust store rompe el handshake.

## Notas y brechas conocidas

- Las claves/secretos (`JWT_*`, `TODOTIX_APPKEY`, `PAYMENT_CALLBACK_SECRET`, `SUBSCRIPTION_GRPC_SECRET`) viven en `.env.dev` / `.env.prod` (gitignored); la rotación es manual (runbook `infrastructure/SECRETS.md`). No hay KMS/secret manager externo.
- La CA interna y los certs se regeneran al recrear el volumen `dama-tls`; no hay rotación automática programada (válidos 825 días los de servidor, 3650 la CA).
- `TODOTIX_APPKEY` es además el respaldo global cuando una academia no tiene credencial propia: en ese caso la clave global viaja a Todotix sin la capa AES-GCM por-academia (es la clave de operación, no un secreto en reposo).

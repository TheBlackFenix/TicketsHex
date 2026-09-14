# Renovación de sesión para Angular

`POST /api/auth/login` mantiene el contrato del access token en JSON (`token`, `fechaExpiracion`, `usuario`) y añade `fechaExpiracionRefresh`. El refresh token no aparece en JSON: se entrega en la cookie `ticketshex_refresh`, `HttpOnly`, limitada a `/api/auth`. La cookie dura siete días por defecto y cada uso de refresh la rota sin extender el vencimiento original.

Para renovar:

```http
POST /api/auth/refresh
X-Refresh-Request: 1
```

La petición debe enviarse con `withCredentials: true`. La respuesta tiene la misma forma que el login y fija una cookie nueva. El refresh token anterior y el access token anterior dejan de servir. El interceptor de Angular debe coordinar un solo refresh simultáneo cuando varias peticiones reciban `401`; si la renovación falla, se solicita login de nuevo. Mantenga el access token en memoria si es posible. No intente leer ni almacenar la cookie desde JavaScript.

`POST /api/auth/logout` acepta un access token vigente o, si este expiró, la cookie con la cabecera `X-Refresh-Request: 1` y `withCredentials: true`. Cambio de contraseña y logout revocan la sesión y eliminan la cookie. Cuando `DebeCambiarContrasena` es verdadero, refresh solo emite otro access token restringido; no habilita los endpoints funcionales.

Configuración:

- Mismo origen: `Refresh:CookieSameSite=Lax`; `Cors:AllowedOrigins` puede estar vacío.
- Origen cruzado: `Refresh:CookieSameSite=None`, `Refresh:CookieSecure=true` y cada origen exacto del frontend en `Cors:AllowedOrigins` (por ejemplo `Cors__AllowedOrigins__0=https://front.example.com`). Angular debe usar `withCredentials: true` tanto en login como en refresh y logout.
- Producción debe servir la API sobre HTTPS y mantener `Refresh:CookieSecure=true`.
- Desarrollo local por HTTP usa `Refresh:CookieSecure=false`; para Angular en `http://localhost:4200` configure `Cors__AllowedOrigins__0=http://localhost:4200`. El valor predeterminado no abre CORS a ningún origen.

Antes de desplegar, aplicar el incremental de `20260914_refresh_sesion.sql` del SGBD correspondiente. Las sesiones anteriores no tienen refresh token y requerirán un login nuevo. No se modifican las credenciales guardadas ni se publican secretos en configuración.

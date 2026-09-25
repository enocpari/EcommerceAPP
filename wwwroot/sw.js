/* Service worker de NOVA
 *
 * REGLA DE DISENO INNEGOCIABLE: este service worker NUNCA cachea respuestas que
 * puedan contener datos de usuario. Solo cachea assets estaticos publicos.
 *
 * Especificamente NO cachea:
 *   - ninguna respuesta con cabecera Set-Cookie
 *   - ninguna respuesta cuyo Vary incluya Cookie
 *   - ningun HTML (las paginas de esta app cambian segun el estado de sesion:
 *     el header muestra "Iniciar sesion" o "Cerrar sesion")
 *   - ninguna ruta privada (/Account, /Admin, /Orders, /Reports)
 *   - nada que no sea GET de mismo origen
 *
 * Estrategias:
 *   assets estaticos -> cache-first  (CSS/JS/fuentes/iconos, no dependen de la sesion)
 *   navegaciones     -> network-only, con respuesta offline minima
 */

const VERSION = 'nova-v1';
const CACHE_NAME = 'nova-static-' + VERSION;

/* Assets publicos precacheados en la instalacion. Solo estaticos y publicos:
   ninguno revela nada sobre el usuario. */
const PRECACHE = [
  '/manifest.webmanifest',
  '/css/nodo-tokens.css',
  '/js/site.js',
  '/js/pwa.js',
  '/icons/icon-192.png',
  '/icons/icon-512.png',
  '/icons/icon-32.png',
  '/favicon.ico'
];

/* Prefijosconsidered publicos y estaticos. */
const ASSET_PREFIXES = [
  '/css/', '/js/', '/lib/', '/images/', '/icons/'
];

/* Extensiones que solo pertenecen a assets estaticos.
   'json' esta excluido a proposito: un .json podria ser configuracion de la
   aplicacion y no queremos asumir que es estatico. */
const ASSET_EXTENSIONS = [
  '.css', '.js', '.mjs', '.png', '.jpg', '.jpeg', '.gif', '.svg', '.webp',
  '.avif', '.ico', '.woff', '.woff2', '.ttf', '.otf', '.eot', '.map'
];

/* Rutas que jamas se tocan, ni para cachear ni para servir desde cache. */
const NEVER_TOUCH = [
  '/Account', '/Admin', '/Orders', '/Reports', '/Home/Error'
];

function isPrivatePath(pathname) {
  return NEVER_TOUCH.some(function (p) {
    return pathname === p || pathname.startsWith(p + '/') || pathname.startsWith(p + '?');
  });
}

function isStaticAsset(url) {
  if (ASSET_PREFIXES.some(function (p) { return url.pathname.startsWith(p); })) {
    return true;
  }
  var lower = url.pathname.toLowerCase();
  return ASSET_EXTENSIONS.some(function (ext) { return lower.endsWith(ext); });
}

/* Ultima linea de defensa: aunque la ruta parezca estatica, si la respuesta
   trae cookies o vary por cookie, o es HTML, no se cachea. */
function isCacheableResponse(response) {
  if (!response || !response.ok || response.status !== 200) return false;
  if (response.type === 'opaque') return false;

  var setCookie = response.headers.get('Set-Cookie');
  if (setCookie) return false;

  var vary = response.headers.get('Vary');
  if (vary && vary.toLowerCase().indexOf('cookie') !== -1) return false;

  var contentType = response.headers.get('Content-Type') || '';
  if (contentType.toLowerCase().indexOf('text/html') !== -1) return false;

  return true;
}

self.addEventListener('install', function (event) {
  event.waitUntil(
    caches.open(CACHE_NAME).then(function (cache) {
      // addAll falla entero si un recurso falla; se toleran ausencias.
      return Promise.all(PRECACHE.map(function (url) {
        return cache.add(new Request(url, { cache: 'reload' })).catch(function () { });
      }));
    })
  );
});

self.addEventListener('activate', function (event) {
  event.waitUntil(
    caches.keys().then(function (keys) {
      return Promise.all(keys.map(function (key) {
        if (key !== CACHE_NAME) return caches.delete(key);
      }));
    }).then(function () {
      return self.clients.claim();
    })
  );
});

self.addEventListener('message', function (event) {
  if (event.data && event.data.type === 'SKIP_WAITING') self.skipWaiting();
});

self.addEventListener('fetch', function (event) {
  var request = event.request;

  // Solo GET de mismo origen.
  if (request.method !== 'GET') return;

  var url;
  try {
    url = new URL(request.url);
  } catch (e) {
    return;
  }
  if (url.origin !== self.location.origin) return;

  // Rutas privadas: se deja pasar sin tocar la cache, siempre.
  if (isPrivatePath(url.pathname)) return;

  // Navegaciones: network-only. No se cachea HTML porque su contenido depende
  // del estado de sesion y cachearlo expondría el header de un usuario a otro.
  if (request.mode === 'navigate') {
    event.respondWith(
      fetch(request).catch(function () {
        return new Response(
          '<!doctype html><meta charset="utf-8">' +
          '<title>Sin conexion</title>' +
          '<style>body{font-family:system-ui,sans-serif;background:#fcfcfd;color:#0b1220;' +
          'display:grid;place-items:center;height:100vh;margin:0;text-align:center}' +
          'h1{font-size:20px;margin:0 0 8px}</style>' +
          '<div><h1>Sin conexion</h1><p>Revisa tu red e intenta de nuevo.</p></div>',
          { status: 503, headers: { 'Content-Type': 'text/html; charset=utf-8' } }
        );
      })
    );
    return;
  }

  // Solo assets estaticos publicos llegan al resto de la logica.
  if (!isStaticAsset(url)) return;

  // cache-first
  event.respondWith(
    caches.open(CACHE_NAME).then(function (cache) {
      return cache.match(request).then(function (cached) {
        if (cached) return cached;

        return fetch(request).then(function (response) {
          if (isCacheableResponse(response)) {
            cache.put(request, response.clone());
          }
          return response;
        }).catch(function () {
          return cached || Response.error();
        });
      });
    })
  );
});

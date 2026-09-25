/* Registro del service worker.
 * Se carga solo en produccion: en desarrollo un SW cachea el HTML y el CSS
 * durante el hot reload, lo que hace muy dificil ver los cambios.
 */
(function () {
  'use strict';

  if (!('serviceWorker' in navigator)) return;
  if (window.location.protocol !== 'https:') return;

  window.addEventListener('load', function () {
    navigator.serviceWorker.register('/sw.js', { scope: '/' })
      .then(function (registration) {
        // Pedir recarga solo cuando hay una version nueva esperando, para no
        // interrumpir la navegacion del usuario.
        registration.addEventListener('updatefound', function () {
          var installing = registration.installing;
          if (!installing) return;
          installing.addEventListener('statechange', function () {
            if (installing.state === 'installed' && navigator.serviceWorker.controller) {
              installing.postMessage({ type: 'SKIP_WAITING' });
            }
          });
        });
      })
      .catch(function (err) {
        console.warn('[pwa] no se pudo registrar el service worker:', err);
      });
  });
})();

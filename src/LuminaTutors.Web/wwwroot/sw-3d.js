/**
 * Lumina Tutors — 3D Lab Service Worker
 * Caches locally-hosted Three.js files so subsequent lab entries
 * are served instantly from the browser cache (< 5 ms).
 *
 * Cache strategy: cache-first for the paths below.
 * All other requests pass through to the network untouched.
 */

const CACHE_NAME = 'lumina-3d-v3';

// Same-origin paths served from wwwroot/js/three/
const LOCAL_ASSETS = [
    '/js/three/three.module.js',
    '/js/three/controls/OrbitControls.js',
];

// ── Install: pre-fetch and cache assets ───────────────────────────────────
self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => cache.addAll(LOCAL_ASSETS))
            .then(() => self.skipWaiting())
    );
});

// ── Activate: remove old cache versions ───────────────────────────────────
self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys()
            .then(keys => Promise.all(
                keys.filter(k => k !== CACHE_NAME).map(k => caches.delete(k))
            ))
            .then(() => self.clients.claim())
    );
});

// ── Fetch: cache-first for our Three.js assets ────────────────────────────
self.addEventListener('fetch', event => {
    const url  = new URL(event.request.url);
    const path = url.pathname;

    // Only intercept our local Three.js assets
    if (!LOCAL_ASSETS.includes(path)) return;

    event.respondWith(
        caches.match(event.request).then(cached => {
            if (cached) return cached;

            return fetch(event.request).then(response => {
                if (response.ok) {
                    const clone = response.clone();
                    caches.open(CACHE_NAME).then(c => c.put(event.request, clone));
                }
                return response;
            });
        })
    );
});

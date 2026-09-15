// NODO Storefront & Interactive Components
(function () {
    const STORAGE_KEY = 'nodo_cart_items';
    let cart = [];

    try {
        const stored = localStorage.getItem(STORAGE_KEY);
        if (stored) {
            cart = JSON.parse(stored);
        }
    } catch (e) {
        console.error('Error loading cart from storage', e);
    }

    function saveCart() {
        try {
            localStorage.setItem(STORAGE_KEY, JSON.stringify(cart));
        } catch (e) {
            console.error('Error saving cart to storage', e);
        }
    }

    function formatPrice(amount) {
        return '$' + Number(amount).toLocaleString('es-AR', { minimumFractionDigits: 0, maximumFractionDigits: 0 });
    }

    function openDrawer(id) {
        const d = document.getElementById(id);
        if (d) {
            d.classList.add('open');
            d.setAttribute('aria-hidden', 'false');
            document.body.style.overflow = 'hidden';
        }
    }

    function closeDrawer(id) {
        const d = document.getElementById(id);
        if (d) {
            d.classList.remove('open');
            d.setAttribute('aria-hidden', 'true');
            document.body.style.overflow = '';
        }
    }

    function showToast(msg) {
        let t = document.getElementById('toast');
        if (!t) {
            t = document.createElement('div');
            t.id = 'toast';
            t.className = 'toast';
            document.body.appendChild(t);
        }
        t.textContent = msg;
        t.classList.add('show');
        setTimeout(() => t.classList.remove('show'), 2500);
    }

    function getProductIconSvg(cat) {
        if ((cat || '').toLowerCase().includes('celular')) {
            return '<svg width="34" height="34" viewBox="0 0 44 44" fill="none"><rect x="11" y="3" width="22" height="38" rx="5" stroke="var(--fg)" stroke-width="1.3"/><rect x="15" y="8" width="14" height="2" rx="1" fill="var(--muted)"/><circle cx="22" cy="34" r="1.6" stroke="var(--muted)"/><rect x="14" y="13" width="16" height="16" rx="2" fill="color-mix(in oklch, var(--accent) 10%, transparent)" stroke="var(--border)"/></svg>';
        } else {
            return '<svg width="34" height="34" viewBox="0 0 44 44" fill="none"><path d="M8 18c0-7 6-11 14-11s14 4 14 11v6c0 2-1 3-2.5 4L22 35 10.5 28C9 27 8 26 8 24v-6z" stroke="var(--fg)" stroke-width="1.3"/><path d="M8 22c0 2 1 3 2.5 4L22 33l11.5-7C35 25 36 24 36 22" stroke="var(--muted)" stroke-width="1"/><circle cx="22" cy="19" r="5" fill="color-mix(in oklch, var(--accent) 10%, transparent)" stroke="var(--border)"/></svg>';
        }
    }

    function renderCart() {
        const countElements = document.querySelectorAll('.cart-count-val');
        const totalItems = cart.reduce((sum, item) => sum + (item.qty || 1), 0);

        countElements.forEach(el => {
            el.textContent = totalItems;
            el.style.display = totalItems > 0 ? 'inline-block' : 'none';
        });

        const subEl = document.getElementById('cartSub');
        if (subEl) {
            subEl.textContent = totalItems > 0
                ? `${totalItems} ${totalItems === 1 ? 'producto' : 'productos'} · Envío gratis desde $199.000`
                : '0 productos seleccionados';
        }

        const body = document.getElementById('cartBody');
        const foot = document.getElementById('cartFoot');
        if (!body || !foot) return;

        if (cart.length === 0) {
            body.innerHTML = `
                <div style="text-align:center;padding:48px 16px;">
                    <div style="width:64px;height:64px;border-radius:50%;background:var(--bg);border:1px solid var(--border);display:grid;place-items:center;margin:0 auto 16px;">
                        <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="var(--muted)" stroke-width="1.6"><path d="M6 7h12l-1 9H7L6 7z"/><path d="M9 7V5a3 3 0 0 1 6 0v2"/></svg>
                    </div>
                    <strong style="font-size:16px;font-family:var(--font-display);">Tu carrito está vacío</strong>
                    <p class="meta" style="margin-top:6px;max-width:32ch;margin-inline:auto;">Agrega un celular o unos auriculares seleccionados. Guardamos tus preferencias 48 horas.</p>
                    <a href="/Products" class="btn btn-accent" style="margin-top:20px;" onclick="window.NodoUI.closeDrawer('cartDrawer')">Explorar catálogo →</a>
                </div>
            `;
            foot.innerHTML = '';
            return;
        }

        const totalAmount = cart.reduce((sum, item) => sum + (item.price * item.qty), 0);
        const freeShippingThreshold = 199000;
        const diffShipping = freeShippingThreshold - totalAmount;

        let shippingBanner = '';
        if (diffShipping <= 0) {
            shippingBanner = `
                <div class="cart-free-shipping">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M20 6L9 17l-5-5"/></svg>
                    <span>¡Genial! Tienes <b>Envío Gratis</b> en tu pedido.</span>
                </div>
            `;
        } else {
            const percent = Math.min(100, Math.round((totalAmount / freeShippingThreshold) * 100));
            shippingBanner = `
                <div style="background:var(--bg);border:1px solid var(--border);border-radius:10px;padding:10px 14px;margin-bottom:14px;">
                    <div class="row-between" style="font-size:11.5px;margin-bottom:6px;">
                        <span class="meta">Faltan <b>${formatPrice(diffShipping)}</b> para envío gratis</span>
                        <span class="num" style="font-weight:600">${percent}%</span>
                    </div>
                    <div style="height:5px;border-radius:999px;background:var(--border);overflow:hidden;">
                        <div style="height:100%;width:${percent}%;background:var(--accent);border-radius:999px;"></div>
                    </div>
                </div>
            `;
        }

        body.innerHTML = shippingBanner + cart.map((item) => `
            <div class="cart-item">
                <div class="cart-thumb">
                    ${item.imageUrl ? `<img src="${item.imageUrl}" alt="${item.name}" onerror="this.src='/images/default-product.jpg'" />` : getProductIconSvg(item.category)}
                </div>
                <div class="cart-info">
                    <div class="row-between">
                        <div class="cart-title" title="${item.name}">${item.name}</div>
                        <button class="btn btn-ghost" style="padding:2px 6px;font-size:12px;color:var(--muted);" onclick="window.NodoCart.remove(${item.id})" aria-label="Eliminar">✕</button>
                    </div>
                    <div class="cart-meta">${item.brand || 'NODO'} · ${item.spec || 'Original'}</div>
                    <div class="row-between" style="margin-top:8px;">
                        <div class="cart-qty-ctrl">
                            <button class="qty-btn" onclick="window.NodoCart.updateQty(${item.id}, -1)">−</button>
                            <span class="num" style="font-size:13px;font-weight:600;min-width:18px;text-align:center;">${item.qty}</span>
                            <button class="qty-btn" onclick="window.NodoCart.updateQty(${item.id}, 1)">+</button>
                        </div>
                        <span class="num" style="font-weight:700;font-size:14.5px;">${formatPrice(item.price * item.qty)}</span>
                    </div>
                </div>
            </div>
        `).join('');

        foot.innerHTML = `
            <div class="stack" style="gap:10px;">
                <div class="row-between" style="font-size:13px;color:var(--muted);">
                    <span>Subtotal</span>
                    <span class="num">${formatPrice(totalAmount)}</span>
                </div>
                <div class="row-between" style="font-size:13px;color:var(--muted);">
                    <span>Envío</span>
                    <span>${diffShipping <= 0 ? '<b style="color:#16a34a">GRATIS</b>' : '$6.500'}</span>
                </div>
                <div class="row-between" style="font-size:16px;font-weight:700;padding-top:8px;border-top:1px solid var(--border);">
                    <span>Total estimado</span>
                    <span class="num" style="color:var(--fg);font-size:18px;">${formatPrice(totalAmount + (diffShipping <= 0 ? 0 : 6500))}</span>
                </div>
                <button class="btn btn-accent" style="width:100%;margin-top:6px;padding:13px 18px;font-size:14.5px;" onclick="window.NodoCart.checkout()">
                    Iniciar compra segura →
                </button>
                <div style="text-align:center;font-size:11px;color:var(--muted);font-family:var(--font-mono);margin-top:4px;">
                    12 cuotas fijas · Garantía oficial 12 meses
                </div>
            </div>
        `;
    }

    function addToCart(prod) {
        if (!prod) return;
        if (prod.nodeType || (prod.dataset && prod.dataset.id)) {
            return addFromButton(prod);
        }
        const id = parseInt(prod.id);
        if (isNaN(id) || id <= 0) return;

        let price = prod.price;
        if (typeof price === 'string') {
            price = parseFloat(price.replace(/[^0-9.-]+/g, ''));
        }
        if (isNaN(price)) price = 0;

        const existing = cart.find(x => x.id === id);
        if (existing) {
            existing.qty = (existing.qty || 1) + 1;
        } else {
            cart.push({
                id: id,
                name: prod.name || 'Producto NODO',
                price: price,
                brand: prod.brand || '',
                category: prod.category || '',
                spec: prod.spec || '',
                imageUrl: prod.imageUrl || prod.image || '',
                qty: 1
            });
        }
        saveCart();
        renderCart();
        showToast('✓ ' + (prod.name || 'Producto') + ' agregado al carrito');
        openDrawer('cartDrawer');
    }

    function addFromButton(btn) {
        if (!btn || !btn.dataset) return;
        addToCart({
            id: btn.dataset.id,
            name: btn.dataset.name,
            price: btn.dataset.price,
            brand: btn.dataset.brand,
            category: btn.dataset.category,
            spec: btn.dataset.spec,
            imageUrl: btn.dataset.image
        });
    }

    function updateQty(id, delta) {
        const item = cart.find(x => x.id === id);
        if (!item) return;
        if (delta > 0 && typeof item.availableStock === 'number' && item.qty >= item.availableStock) {
            showToast('Solo hay ' + item.availableStock + ' unidades disponibles de este producto.');
            return;
        }
        item.qty = (item.qty || 1) + delta;
        if (item.qty <= 0) {
            cart = cart.filter(x => x.id !== id);
        }
        saveCart();
        renderCart();
    }

    async function validateCartAsync() {
        if (!cart || cart.length === 0) return { success: true, allInStock: true, items: [] };
        try {
            const resp = await fetch('/Orders/ValidateCart', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(cart)
            });
            if (!resp.ok) return null;
            const data = await resp.json();
            if (data && data.success) {
                let updated = false;
                data.items.forEach(validItem => {
                    const cItem = cart.find(x => x.id === validItem.id);
                    if (cItem) {
                        if (cItem.price !== validItem.price) {
                            cItem.price = validItem.price;
                            updated = true;
                        }
                        cItem.availableStock = validItem.availableStock;
                        cItem.inStock = validItem.inStock;
                    }
                });
                if (updated) {
                    saveCart();
                    renderCart();
                }
                return data;
            }
        } catch (e) {
            console.warn('Could not validate cart online', e);
        }
        return null;
    }

    function removeFromCart(id) {
        cart = cart.filter(x => x.id !== id);
        saveCart();
        renderCart();
        showToast('Producto eliminado del carrito');
    }

    function clearCart() {
        cart = [];
        saveCart();
        renderCart();
    }

    function checkout() {
        if (cart.length === 0) {
            showToast('El carrito está vacío. Agrega un producto primero.');
            return;
        }
        closeDrawer('cartDrawer');
        window.location.href = '/Orders/Checkout';
    }

    function openPdp(prod) {
        const body = document.getElementById('pdpBody');
        const foot = document.getElementById('pdpFoot');
        if (!body || !foot) return;

        const off = prod.wasPrice ? Math.round((1 - prod.price / prod.wasPrice) * 100) : 0;
        body.innerHTML = `
            <div class="stack" style="gap:18px;">
                <div style="aspect-ratio:1.25/1;background:var(--bg);border:1px solid var(--border);border-radius:18px;display:grid;place-items:center;padding:24px;position:relative;">
                    ${prod.badge ? `<span class="badge" style="position:absolute;top:14px;left:14px;background:var(--fg);color:var(--surface);font-family:var(--font-mono);font-size:10px;padding:4px 8px;border-radius:999px;">${prod.badge}</span>` : ''}
                    ${prod.imageUrl ? `<img src="${prod.imageUrl}" alt="${prod.name}" style="max-height:100%;max-width:100%;object-fit:contain" onerror="this.src='/images/default-product.jpg'" />` : getProductIconSvg(prod.category)}
                </div>
                <div>
                    <div class="meta">${prod.brand || 'NODO'} · ${prod.category || 'Dispositivo'}</div>
                    <h2 class="h2" style="margin-top:4px;">${prod.name}</h2>
                    ${prod.spec ? `<div style="font-family:var(--font-mono);font-size:12.5px;color:var(--muted);margin-top:4px;">${prod.spec}</div>` : ''}
                </div>
                <div class="p-price" style="display:flex;align-items:baseline;gap:10px;">
                    <span class="num" style="font-size:24px;font-weight:700;">${formatPrice(prod.price)}</span>
                    ${prod.wasPrice ? `<span class="num" style="font-size:14px;color:var(--muted);text-decoration:line-through;">${formatPrice(prod.wasPrice)}</span><span class="pill">-${off}% OFF</span>` : ''}
                </div>
                <p class="lead" style="font-size:14px;line-height:1.5;">${prod.description || 'Producto original con garantía de fábrica y soporte oficial.'}</p>
                <div class="trustbar" style="margin-top:4px;">
                    <span>◐ Envíos <b>24 h CABA</b></span>
                    <span>◑ Garantía <b>12 meses</b></span>
                    <span>◒ Cuotas <b>Hasta 12 sin interés</b></span>
                </div>
            </div>
        `;

        foot.innerHTML = `
            <div class="row" style="gap:10px;">
                <button class="btn btn-accent" style="flex:1;" onclick='window.NodoCart.add(${JSON.stringify(prod)}); window.NodoUI.closeDrawer("pdpDrawer");'>
                    Agregar al carrito · ${formatPrice(prod.price)}
                </button>
                <a href="/Products/Details/${prod.id}" class="btn btn-secondary">Ver completo →</a>
            </div>
        `;

        openDrawer('pdpDrawer');
    }

    function sendSupport() {
        closeDrawer('soporteDrawer');
        showToast('✓ Mensaje enviado. Te responderemos a la brevedad');
    }

    // Expose to window
    window.NodoUI = {
        openDrawer,
        closeDrawer,
        showToast,
        openPdp,
        sendSupport
    };

    window.NodoCart = {
        add: addToCart,
        addFromButton: addFromButton,
        updateQty: updateQty,
        remove: removeFromCart,
        checkout: checkout,
        clear: clearCart,
        refresh: renderCart,
        validate: validateCartAsync,
        getItems: () => [...cart]
    };

    // Global event delegation for add-to-cart buttons
    document.addEventListener('click', function (e) {
        const btn = e.target.closest('.p-add, [data-add-to-cart]');
        if (btn && btn.dataset && btn.dataset.id) {
            e.preventDefault();
            addFromButton(btn);
        }
    });

    // Close drawers on ESC key
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            document.querySelectorAll('.drawer.open').forEach(d => {
                d.classList.remove('open');
                d.setAttribute('aria-hidden', 'true');
            });
            document.body.style.overflow = '';
        }
    });

    // Hero Showcase Slide Alternator
    function initHeroSlider() {
        const slides = document.querySelectorAll('.hero-slide');
        const dots = document.querySelectorAll('.hero-dot');
        if (!slides.length) return;

        let currentIndex = 0;
        let timer = null;

        function showSlide(index) {
            slides.forEach((slide, i) => {
                if (i === index) {
                    slide.classList.add('active');
                } else {
                    slide.classList.remove('active');
                }
            });
            dots.forEach((dot, i) => {
                if (i === index) {
                    dot.classList.add('active');
                } else {
                    dot.classList.remove('active');
                }
            });
            currentIndex = index;
        }

        function nextSlide() {
            const next = (currentIndex + 1) % slides.length;
            showSlide(next);
        }

        function startTimer() {
            stopTimer();
            timer = setInterval(nextSlide, 4200);
        }

        function stopTimer() {
            if (timer) {
                clearInterval(timer);
                timer = null;
            }
        }

        window.NodoHero = {
            setSlide: function (idx) {
                showSlide(idx);
                startTimer();
            }
        };

        const container = document.querySelector('.hero-showcase');
        if (container) {
            container.addEventListener('mouseenter', stopTimer);
            container.addEventListener('mouseleave', startTimer);
        }

        startTimer();
    }

    // Initialize on DOM ready
    function initApp() {
        renderCart();
        initHeroSlider();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initApp);
    } else {
        initApp();
    }
})();

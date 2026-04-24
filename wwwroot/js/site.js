// ==========================================
// JAZMÍN - site.js
// ==========================================
(function () {
    'use strict';

    // close user dropdown on outside click
    document.addEventListener('click', function (e) {
        document.querySelectorAll('.user-dropdown.open').forEach(function (dd) {
            if (!dd.parentElement.contains(e.target)) {
                dd.classList.remove('open');
                var btn = dd.previousElementSibling;
                if (btn) btn.setAttribute('aria-expanded', 'false');
            }
        });
    });

    // Toast helper
    window.jazminToast = function (msg, type) {
        var el = document.createElement('div');
        el.className = 'toast';
        if (type === 'error') el.style.background = '#C84747';
        el.textContent = msg;
        document.body.appendChild(el);
        setTimeout(function () { el.remove(); }, 2800);
    };

    // AJAX add-to-cart & favorite toggle
    function getAntiForgeryToken(form) {
        var input = form ? form.querySelector('input[name="__RequestVerificationToken"]') : null;
        if (input) return input.value;
        input = document.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : '';
    }

    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (!(form instanceof HTMLFormElement)) return;

        // Add to cart AJAX
        if (form.classList.contains('js-cart-add')) {
            e.preventDefault();
            var data = new FormData(form);
            fetch(form.action, {
                method: 'POST',
                body: data,
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(function (r) { return r.json(); }).then(function (j) {
                if (j.ok) {
                    window.jazminToast('Agregado al carrito ✓');
                    // update badge
                    var badges = document.querySelectorAll('.nav-right .icon-btn[title="Carrito"] .badge');
                    var cartLink = document.querySelector('.nav-right a[title="Carrito"]');
                    if (cartLink) {
                        var badge = cartLink.querySelector('.badge');
                        if (!badge) {
                            badge = document.createElement('span');
                            badge.className = 'badge';
                            cartLink.appendChild(badge);
                        }
                        badge.textContent = j.count;
                    }
                } else {
                    window.jazminToast('No se pudo agregar', 'error');
                }
            }).catch(function () { window.jazminToast('Error de red', 'error'); });
        }

        // Favorite toggle AJAX
        if (form.classList.contains('js-fav-toggle')) {
            e.preventDefault();
            var data2 = new FormData(form);
            fetch(form.action, {
                method: 'POST',
                body: data2,
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(function (r) { return r.json(); }).then(function (j) {
                if (j.ok) {
                    var btn = form.querySelector('.fav-btn');
                    if (btn) btn.classList.toggle('is-fav', j.isFavorite);
                    window.jazminToast(j.isFavorite ? 'Agregado a favoritos ♥' : 'Quitado de favoritos');
                }
            }).catch(function () {
                // fallback: resubmit normally
                form.classList.remove('js-fav-toggle');
                form.submit();
            });
        }
    });

    // Quantity +/- on cart and product detail
    document.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-qty]');
        if (!btn) return;
        var input = btn.parentElement.querySelector('input[type="number"]');
        if (!input) return;
        var v = parseInt(input.value || '1', 10);
        if (btn.dataset.qty === 'plus') v++;
        if (btn.dataset.qty === 'minus') v = Math.max(1, v - 1);
        input.value = v;
        input.dispatchEvent(new Event('change', { bubbles: true }));
    });

    // Gallery thumbs on product detail
    document.querySelectorAll('.pd-thumb').forEach(function (t) {
        t.addEventListener('click', function () {
            var src = t.dataset.src;
            if (!src) return;
            var main = document.querySelector('.pd-main-img img');
            if (main) main.src = src;
            document.querySelectorAll('.pd-thumb').forEach(function (x) { x.classList.remove('active'); });
            t.classList.add('active');
        });
    });

    // Size select
    document.querySelectorAll('.size-options').forEach(function (group) {
        group.addEventListener('click', function (e) {
            var label = e.target.closest('.size-option');
            if (!label) return;
            group.querySelectorAll('.size-option').forEach(function (l) { l.classList.remove('active'); });
            label.classList.add('active');
        });
    });

    // Rating input — visual fill on click
    document.querySelectorAll('.rating-input').forEach(function (group) {
        var inputs = group.querySelectorAll('input[type="radio"]');
        inputs.forEach(function (inp) {
            inp.addEventListener('change', function () {
                var v = parseInt(inp.value, 10);
                group.querySelectorAll('label').forEach(function (lbl) {
                    var lblV = parseInt(lbl.dataset.v, 10);
                    lbl.style.color = lblV <= v ? 'var(--warning)' : '';
                });
            });
        });
    });
})();

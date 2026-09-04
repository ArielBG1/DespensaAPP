/**
 * caja.js — Sistema de Punto de Venta (POS)
 * Depende de: barcode-reader.js
 */
'use strict';

// ════════════════════════════════════════════════════════════════════════════
// ESTADO
// ════════════════════════════════════════════════════════════════════════════

const STORAGE_KEY = 'despensa_caja_v2';
const MONEDA = window.MONEDA || 'Gs.';

let carrito = {
    items: [],          // { productoId, nombre, codigoBarra, precioUnitario, cantidad, descuentoItem, esPorPeso, tipoVenta }
    descuentoGlobal: 0
};

let ui = {
    metodoPago: 'Efectivo',
    productoPendientePeso: null,  // producto esperando ingreso de peso
    itemEditandoIdx: -1,          // índice cuya cantidad se está editando inline
    searchTimer: null,            // debounce timer para búsqueda
    searchResultIdx: -1           // navegación teclado en dropdown
};

// ════════════════════════════════════════════════════════════════════════════
// PERSISTENCIA (sessionStorage)
// ════════════════════════════════════════════════════════════════════════════

function guardarCarrito() {
    try { sessionStorage.setItem(STORAGE_KEY, JSON.stringify(carrito)); } catch {}
}

function cargarCarrito() {
    try {
        const saved = sessionStorage.getItem(STORAGE_KEY);
        if (saved) carrito = JSON.parse(saved);
    } catch {
        carrito = { items: [], descuentoGlobal: 0 };
    }
}

// ════════════════════════════════════════════════════════════════════════════
// CÁLCULOS
// ════════════════════════════════════════════════════════════════════════════

function calcularTotales() {
    let subtotal = 0;
    let descuentoItems = 0;
    for (const item of carrito.items) {
        subtotal += item.precioUnitario * item.cantidad;
        descuentoItems += (item.descuentoItem || 0);
    }
    const descuentoGlobal = carrito.descuentoGlobal || 0;
    const totalDesc = descuentoItems + descuentoGlobal;
    const total = Math.max(0, subtotal - totalDesc);
    return { subtotal, descuentoItems, descuentoGlobal, totalDesc, total };
}

// ════════════════════════════════════════════════════════════════════════════
// MANEJO DEL CARRITO
// ════════════════════════════════════════════════════════════════════════════

function agregarProducto(prod) {
    // prod: { id, nombre, codigoBarra, precioVenta, stockActual, tipoVenta, esPorPeso, unidad }

    if (prod.esPorPeso) {
        abrirModalPeso(prod);
        return;
    }

    const idx = carrito.items.findIndex(i => i.productoId === prod.id && !i.esPorPeso);

    if (idx >= 0) {
        carrito.items[idx].cantidad += 1;
        mostrarFeedback(`${prod.nombre} ×${carrito.items[idx].cantidad}`, 'success');
    } else {
        carrito.items.push({
            productoId: prod.id,
            nombre: prod.nombre,
            codigoBarra: prod.codigoBarra || null,
            precioUnitario: prod.precioVenta,
            cantidad: 1,
            descuentoItem: 0,
            esPorPeso: false,
            tipoVenta: prod.tipoVenta
        });
        mostrarFeedback(`✓ ${prod.nombre} agregado`, 'success');
    }

    guardarCarrito();
    renderCarrito();
    volverFocusCodigo();
}

function agregarItemPeso(productoId, nombre, codigoBarra, precioUnitario, cantidad, tipoVenta) {
    carrito.items.push({
        productoId,
        nombre,
        codigoBarra: codigoBarra || null,
        precioUnitario,
        cantidad,
        descuentoItem: 0,
        esPorPeso: true,
        tipoVenta
    });
    guardarCarrito();
    renderCarrito();
}

function quitarItem(idx) {
    const nombre = carrito.items[idx]?.nombre ?? '';
    carrito.items.splice(idx, 1);
    guardarCarrito();
    renderCarrito();
    mostrarFeedback(`${nombre} eliminado`, 'warning');
}

function cambiarCantidad(idx, nuevaCant) {
    const c = parseFloat(nuevaCant);
    if (isNaN(c) || c <= 0) {
        quitarItem(idx);
        return;
    }
    carrito.items[idx].cantidad = Math.round(c * 1000) / 1000; // 3 decimales
    guardarCarrito();
    renderCarrito();
}

function limpiarCarrito() {
    if (carrito.items.length === 0) return;
    if (!confirm('¿Cancelar la venta actual? Se borrará todo el carrito.')) return;
    carrito = { items: [], descuentoGlobal: 0 };
    guardarCarrito();
    renderCarrito();
    mostrarFeedback('Carrito limpiado', 'warning');
    volverFocusCodigo();
}

// ════════════════════════════════════════════════════════════════════════════
// RENDER DEL CARRITO
// ════════════════════════════════════════════════════════════════════════════

function renderCarrito() {
    const tbody = document.getElementById('carritoBody');
    const totales = calcularTotales();

    if (!carrito.items.length) {
        tbody.innerHTML = `
            <tr id="carritoVacio">
                <td colspan="5" class="text-center py-5 text-muted">
                    <i class="fas fa-shopping-cart fa-3x d-block mb-3 opacity-25"></i>
                    Carrito vacío · Escanee o busque un producto
                </td>
            </tr>`;
    } else {
        tbody.innerHTML = carrito.items.map((item, idx) => {
            const lineTotal = item.precioUnitario * item.cantidad - (item.descuentoItem || 0);
            const cantDisplay = item.esPorPeso
                ? item.cantidad.toFixed(3) + ' ' + unidadLabel(item.tipoVenta)
                : (Number.isInteger(item.cantidad) ? item.cantidad : item.cantidad.toFixed(3));

            return `
            <tr class="align-middle" id="row-${idx}">
                <td style="width:110px">
                    <div class="d-flex align-items-center gap-1">
                        <button class="btn btn-outline-secondary btn-sm px-1 py-0 lh-1"
                                onclick="cambiarCantidad(${idx}, ${item.cantidad - (item.esPorPeso ? 0.1 : 1)})"
                                style="font-size:.85rem;width:22px">−</button>
                        <span class="fw-bold text-center px-1 cant-cell"
                              style="min-width:2.8rem;cursor:pointer;font-size:.9rem"
                              title="Click para editar"
                              onclick="editarCantidad(${idx})">${cantDisplay}</span>
                        <button class="btn btn-outline-secondary btn-sm px-1 py-0 lh-1"
                                onclick="cambiarCantidad(${idx}, ${item.cantidad + (item.esPorPeso ? 0.1 : 1)})"
                                style="font-size:.85rem;width:22px">+</button>
                    </div>
                </td>
                <td>
                    <div class="fw-semibold" style="font-size:.9rem;line-height:1.3">${escHtml(item.nombre)}</div>
                    ${item.codigoBarra
                        ? `<div class="text-muted" style="font-size:.72rem"><i class="fas fa-barcode me-1 opacity-50"></i>${escHtml(item.codigoBarra)}</div>`
                        : ''}
                    ${item.esPorPeso
                        ? `<span class="badge bg-info bg-opacity-15 text-info" style="font-size:.65rem">peso</span>`
                        : ''}
                </td>
                <td class="text-end" style="font-size:.9rem">${MONEDA} ${fmt(item.precioUnitario)}</td>
                <td class="text-end fw-bold" style="font-size:.95rem">${MONEDA} ${fmt(lineTotal)}</td>
                <td class="text-center" style="width:40px">
                    <button class="btn btn-outline-danger btn-sm rounded-circle p-0"
                            style="width:26px;height:26px;font-size:.7rem"
                            onclick="quitarItem(${idx})" title="Quitar">
                        <i class="fas fa-times"></i>
                    </button>
                </td>
            </tr>`;
        }).join('');
    }

    // Totales
    document.getElementById('subtotalDisplay').textContent = MONEDA + ' ' + fmt(totales.subtotal);
    document.getElementById('descuentoDisplay').textContent = '-' + MONEDA + ' ' + fmt(totales.totalDesc);
    document.getElementById('totalDisplay').textContent = MONEDA + ' ' + fmt(totales.total);

    const nItems = carrito.items.reduce((s, i) => s + i.cantidad, 0);
    document.getElementById('nItemsDisplay').textContent =
        nItems % 1 === 0 ? nItems + ' item(s)' : nItems.toFixed(3) + ' item(s)';

    const btnCobrar = document.getElementById('btnCobrar');
    if (btnCobrar) btnCobrar.disabled = carrito.items.length === 0;

    const btnDesc = document.getElementById('btnDescuentoGlobal');
    if (btnDesc) btnDesc.disabled = carrito.items.length === 0;
}

// ─── Edición inline de cantidad ───────────────────────────────────────────

function editarCantidad(idx) {
    const item = carrito.items[idx];
    const row = document.getElementById(`row-${idx}`);
    const cell = row?.querySelector('.cant-cell');
    if (!cell) return;

    const input = document.createElement('input');
    input.type = 'number';
    input.value = item.cantidad;
    input.min = '0.001';
    input.step = item.esPorPeso ? '0.001' : '1';
    input.className = 'form-control form-control-sm text-center p-0 fw-bold';
    input.style.cssText = 'width:3.5rem;font-size:.85rem;';

    cell.replaceWith(input);
    input.select();

    const confirmar = () => {
        const v = parseFloat(input.value);
        cambiarCantidad(idx, isNaN(v) ? 0 : v);
    };

    input.addEventListener('keydown', e => {
        if (e.key === 'Enter') confirmar();
        if (e.key === 'Escape') renderCarrito();
    });
    input.addEventListener('blur', confirmar);
}

// ════════════════════════════════════════════════════════════════════════════
// MODAL PRODUCTO POR PESO
// ════════════════════════════════════════════════════════════════════════════

function abrirModalPeso(prod) {
    ui.productoPendientePeso = prod;

    document.getElementById('pesoProdNombre').textContent = prod.nombre;
    document.getElementById('pesoProdPrecio').textContent = MONEDA + ' ' + fmt(prod.precioVenta);
    document.getElementById('pesoProdUnidad').textContent = '/ ' + (prod.unidad || 'kg');
    document.getElementById('pesoInputCantidad').value = '';
    document.getElementById('pesoTotalCalc').textContent = MONEDA + ' 0';
    document.getElementById('pesoPrecioCalc').textContent = MONEDA + ' ' + fmt(prod.precioVenta);

    bootstrap.Modal.getOrCreateInstance(document.getElementById('modalPeso')).show();
    setTimeout(() => document.getElementById('pesoInputCantidad').focus(), 350);
}

function calcularPesoTotal() {
    const prod = ui.productoPendientePeso;
    if (!prod) return;
    const cant = parseFloat(document.getElementById('pesoInputCantidad').value) || 0;
    const total = prod.precioVenta * cant;
    document.getElementById('pesoTotalCalc').textContent = MONEDA + ' ' + fmt(total);
}

function confirmarPeso() {
    const prod = ui.productoPendientePeso;
    if (!prod) return;
    const cant = parseFloat(document.getElementById('pesoInputCantidad').value);
    if (!cant || cant <= 0) {
        document.getElementById('pesoInputCantidad').classList.add('is-invalid');
        return;
    }
    document.getElementById('pesoInputCantidad').classList.remove('is-invalid');

    agregarItemPeso(prod.id, prod.nombre, prod.codigoBarra, prod.precioVenta, cant, prod.tipoVenta);

    bootstrap.Modal.getInstance(document.getElementById('modalPeso')).hide();
    ui.productoPendientePeso = null;
    mostrarFeedback(`✓ ${prod.nombre} ${cant} ${prod.unidad ?? 'kg'} agregado`, 'success');
    volverFocusCodigo();
}

// ════════════════════════════════════════════════════════════════════════════
// MODAL DESCUENTO GLOBAL
// ════════════════════════════════════════════════════════════════════════════

function abrirModalDescuento() {
    const totales = calcularTotales();
    document.getElementById('descInputMonto').value = carrito.descuentoGlobal || '';
    document.getElementById('descSubtotalRef').textContent = MONEDA + ' ' + fmt(totales.subtotal);
    bootstrap.Modal.getOrCreateInstance(document.getElementById('modalDescuento')).show();
    setTimeout(() => document.getElementById('descInputMonto').focus(), 350);
}

function confirmarDescuento() {
    const monto = parseFloat(document.getElementById('descInputMonto').value) || 0;
    const totales = calcularTotales();
    if (monto < 0 || monto > totales.subtotal) {
        document.getElementById('descInputMonto').classList.add('is-invalid');
        return;
    }
    document.getElementById('descInputMonto').classList.remove('is-invalid');
    carrito.descuentoGlobal = monto;
    guardarCarrito();
    renderCarrito();
    bootstrap.Modal.getInstance(document.getElementById('modalDescuento')).hide();
    mostrarFeedback(`Descuento ${MONEDA} ${fmt(monto)} aplicado`, 'info');
}

// ════════════════════════════════════════════════════════════════════════════
// MODAL DE PAGO
// ════════════════════════════════════════════════════════════════════════════

function abrirModalPago() {
    if (carrito.items.length === 0) return;

    const { total } = calcularTotales();

    document.getElementById('pagoTotalDisplay').textContent = MONEDA + ' ' + fmt(total);
    document.getElementById('pagoMontoRecibido').value = '';
    document.getElementById('pagoVuelto').textContent = MONEDA + ' 0';
    document.getElementById('pagoVueltoRow').style.display = 'none';

    // Reset método
    seleccionarMetodo('Efectivo');

    bootstrap.Modal.getOrCreateInstance(document.getElementById('modalPago')).show();
    setTimeout(() => {
        const montoEl = document.getElementById('pagoMontoRecibido');
        if (!montoEl.disabled) montoEl.focus();
    }, 350);
}

function seleccionarMetodo(metodo) {
    ui.metodoPago = metodo;
    document.querySelectorAll('.metodo-btn').forEach(btn => {
        btn.classList.toggle('active', btn.dataset.metodo === metodo);
    });

    const esEfectivo = metodo === 'Efectivo';
    const montoRow = document.getElementById('pagoMontoRow');
    const vueltoRow = document.getElementById('pagoVueltoRow');

    if (esEfectivo) {
        montoRow.style.display = '';
        document.getElementById('pagoMontoRecibido').disabled = false;
        document.getElementById('pagoMontoRecibido').focus();
    } else {
        montoRow.style.display = 'none';
        vueltoRow.style.display = 'none';
    }
}

function calcularVuelto() {
    const { total } = calcularTotales();
    const recibido = parseFloat(document.getElementById('pagoMontoRecibido').value) || 0;
    const vuelto = Math.max(0, recibido - total);
    document.getElementById('pagoVuelto').textContent = MONEDA + ' ' + fmt(vuelto);
    document.getElementById('pagoVueltoRow').style.display = recibido > 0 ? '' : 'none';
}

async function confirmarVenta() {
    const { total } = calcularTotales();
    const btnConfirmar = document.getElementById('btnConfirmarVenta');

    if (ui.metodoPago === 'Efectivo') {
        const recibido = parseFloat(document.getElementById('pagoMontoRecibido').value) || 0;
        if (recibido < total) {
            document.getElementById('pagoMontoRecibido').classList.add('is-invalid');
            return;
        }
        document.getElementById('pagoMontoRecibido').classList.remove('is-invalid');
    }

    btnConfirmar.disabled = true;
    btnConfirmar.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Procesando...';

    const montoRecibido = ui.metodoPago === 'Efectivo'
        ? (parseFloat(document.getElementById('pagoMontoRecibido').value) || 0)
        : total;

    const payload = {
        items: carrito.items.map(i => ({
            productoId: i.productoId,
            nombre: i.nombre,
            codigoBarra: i.codigoBarra,
            precioUnitario: i.precioUnitario,
            cantidad: i.cantidad,
            descuentoItem: i.descuentoItem || 0,
            esPorPeso: i.esPorPeso,
            tipoVenta: i.tipoVenta
        })),
        metodoPago: ui.metodoPago,
        descuento: carrito.descuentoGlobal || 0,
        montoRecibido,
        observacion: null
    };

    try {
        const resp = await fetch('/Ventas/Procesar', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-XSRF-TOKEN': getCsrfToken()
            },
            body: JSON.stringify(payload)
        });

        const data = await resp.json();

        if (!data.success) {
            mostrarFeedback(data.error || 'Error al procesar la venta.', 'danger');
            btnConfirmar.disabled = false;
            btnConfirmar.innerHTML = '<i class="fas fa-check-circle me-2"></i>Confirmar Venta';
            return;
        }

        // Venta exitosa
        bootstrap.Modal.getInstance(document.getElementById('modalPago')).hide();

        // Limpiar carrito
        carrito = { items: [], descuentoGlobal: 0 };
        guardarCarrito();
        renderCarrito();

        // Redirigir al ticket
        window.location.href = `/Ventas/Ticket/${data.ventaId}`;

    } catch (e) {
        mostrarFeedback('Error de conexión. Intente nuevamente.', 'danger');
        btnConfirmar.disabled = false;
        btnConfirmar.innerHTML = '<i class="fas fa-check-circle me-2"></i>Confirmar Venta';
    }
}

// ════════════════════════════════════════════════════════════════════════════
// BÚSQUEDA POR CÓDIGO DE BARRAS
// ════════════════════════════════════════════════════════════════════════════

async function buscarPorCodigo(codigo) {
    ocultarAlertaCodigo();
    if (!codigo?.trim()) return;

    try {
        const resp = await fetch(`/Ventas/BuscarProducto?codigo=${encodeURIComponent(codigo.trim())}`);
        const data = await resp.json();

        if (!data.encontrado) {
            mostrarAlertaCodigo(`Código «${escHtml(codigo)}» no encontrado.`, 'danger');
            volverFocusCodigo();
            return;
        }

        agregarProducto(data.producto);
    } catch {
        mostrarAlertaCodigo('Error al buscar producto. Verifique la conexión.', 'danger');
    }
}

function mostrarAlertaCodigo(msg, tipo = 'warning') {
    const el = document.getElementById('alertaCodigo');
    el.className = `alert alert-${tipo} py-2 mt-2`;
    el.textContent = msg;
    el.style.display = '';
    clearTimeout(el._timer);
    el._timer = setTimeout(() => ocultarAlertaCodigo(), 4000);
}

function ocultarAlertaCodigo() {
    const el = document.getElementById('alertaCodigo');
    if (el) el.style.display = 'none';
}

// ════════════════════════════════════════════════════════════════════════════
// BÚSQUEDA POR NOMBRE (autocomplete)
// ════════════════════════════════════════════════════════════════════════════

function initBusquedaNombre() {
    const input = document.getElementById('busquedaInput');
    const dropdown = document.getElementById('busquedaDropdown');
    if (!input || !dropdown) return;

    input.addEventListener('input', () => {
        clearTimeout(ui.searchTimer);
        const q = input.value.trim();

        if (q.length < 2) {
            dropdown.style.display = 'none';
            ui.searchResultIdx = -1;
            return;
        }

        ui.searchTimer = setTimeout(() => buscarNombre(q), 280);
    });

    input.addEventListener('keydown', e => {
        const items = dropdown.querySelectorAll('.search-item');
        if (!items.length) return;

        if (e.key === 'ArrowDown') {
            e.preventDefault();
            ui.searchResultIdx = Math.min(ui.searchResultIdx + 1, items.length - 1);
            items.forEach((it, i) => it.classList.toggle('active', i === ui.searchResultIdx));
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            ui.searchResultIdx = Math.max(ui.searchResultIdx - 1, 0);
            items.forEach((it, i) => it.classList.toggle('active', i === ui.searchResultIdx));
        } else if (e.key === 'Enter' && ui.searchResultIdx >= 0) {
            e.preventDefault();
            items[ui.searchResultIdx]?.click();
        } else if (e.key === 'Escape') {
            dropdown.style.display = 'none';
            ui.searchResultIdx = -1;
        }
    });

    document.addEventListener('click', e => {
        if (!input.contains(e.target) && !dropdown.contains(e.target)) {
            dropdown.style.display = 'none';
        }
    });
}

async function buscarNombre(q) {
    const dropdown = document.getElementById('busquedaDropdown');
    try {
        const resp = await fetch(`/Ventas/BuscarNombre?q=${encodeURIComponent(q)}`);
        const lista = await resp.json();

        if (!lista.length) {
            dropdown.innerHTML = `<div class="px-3 py-2 text-muted small">Sin resultados para «${escHtml(q)}»</div>`;
        } else {
            dropdown.innerHTML = lista.map((p, i) => `
                <div class="search-item px-3 py-2 d-flex align-items-center justify-content-between gap-2"
                     style="cursor:pointer;border-bottom:1px solid #f1f5f9;"
                     data-idx="${i}">
                    <div>
                        <div class="fw-semibold" style="font-size:.9rem">${escHtml(p.nombre)}</div>
                        <div class="text-muted" style="font-size:.75rem">
                            ${p.codigoBarra ? `<i class="fas fa-barcode me-1"></i>${escHtml(p.codigoBarra)} · ` : ''}
                            Stock: ${p.stockActual} ${escHtml(p.unidad)}
                        </div>
                    </div>
                    <div class="text-end">
                        <div class="fw-bold text-primary">${MONEDA} ${fmt(p.precioVenta)}</div>
                        ${p.esPorPeso ? '<span class="badge bg-info bg-opacity-15 text-info" style="font-size:.65rem">peso</span>' : ''}
                    </div>
                </div>
            `).join('');

            dropdown.querySelectorAll('.search-item').forEach((el, i) => {
                el.addEventListener('mouseenter', () => {
                    dropdown.querySelectorAll('.search-item').forEach(x => x.classList.remove('active'));
                    el.classList.add('active');
                    ui.searchResultIdx = i;
                });
                el.addEventListener('click', () => {
                    agregarProducto(lista[i]);
                    document.getElementById('busquedaInput').value = '';
                    dropdown.style.display = 'none';
                    ui.searchResultIdx = -1;
                });
            });
        }

        dropdown.style.display = '';
        ui.searchResultIdx = -1;
    } catch {
        dropdown.style.display = 'none';
    }
}

// ════════════════════════════════════════════════════════════════════════════
// UTILIDADES
// ════════════════════════════════════════════════════════════════════════════

function fmt(n) {
    return Math.round(n || 0).toLocaleString('es-PY');
}

function escHtml(str) {
    if (!str) return '';
    return String(str)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');
}

function unidadLabel(tipoVenta) {
    return { Unidad: 'u', Kg: 'kg', Gramo: 'g', Cien_g: '×100g' }[tipoVenta] ?? 'u';
}

function getCsrfToken() {
    return document.querySelector('meta[name="xsrf-token"]')?.content ?? '';
}

function volverFocusCodigo() {
    setTimeout(() => document.getElementById('codigoBarraInput')?.focus(), 150);
}

function mostrarFeedback(msg, tipo = 'success') {
    const container = document.getElementById('toastContainer');
    if (!container) return;

    const id = 'toast-' + Date.now();
    const color = { success: '#198754', danger: '#dc3545', warning: '#f59e0b', info: '#0ea5e9' }[tipo] ?? '#0ea5e9';

    container.insertAdjacentHTML('beforeend', `
        <div id="${id}" class="toast align-items-center text-white border-0 show"
             style="background:${color};min-width:220px;" role="alert">
            <div class="d-flex">
                <div class="toast-body fw-semibold" style="font-size:.88rem">${escHtml(msg)}</div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto"
                        data-bs-dismiss="toast"></button>
            </div>
        </div>
    `);

    setTimeout(() => document.getElementById(id)?.remove(), 3000);
}

// ════════════════════════════════════════════════════════════════════════════
// INIT
// ════════════════════════════════════════════════════════════════════════════

document.addEventListener('DOMContentLoaded', () => {
    // Colapsar sidebar para más espacio
    document.getElementById('sidebar')?.classList.add('collapsed');
    document.getElementById('mainContent')?.classList.add('expanded');

    cargarCarrito();
    renderCarrito();
    initBusquedaNombre();

    // ─── Lector de código de barras ───────────────────────────────────────
    BarcodeReader.init('codigoBarraInput', (codigo) => {
        ocultarAlertaCodigo();
        buscarPorCodigo(codigo);
    });

    // Autofocus
    volverFocusCodigo();

    // ─── Modal Peso: calcular en tiempo real ──────────────────────────────
    document.getElementById('pesoInputCantidad')?.addEventListener('input', calcularPesoTotal);

    document.getElementById('pesoInputCantidad')?.addEventListener('keydown', e => {
        if (e.key === 'Enter') confirmarPeso();
    });

    // ─── Modal Pago: calcular vuelto ──────────────────────────────────────
    document.getElementById('pagoMontoRecibido')?.addEventListener('input', calcularVuelto);

    document.getElementById('pagoMontoRecibido')?.addEventListener('keydown', e => {
        if (e.key === 'Enter') confirmarVenta();
    });

    // ─── Botones método de pago ───────────────────────────────────────────
    document.querySelectorAll('.metodo-btn').forEach(btn => {
        btn.addEventListener('click', () => seleccionarMetodo(btn.dataset.metodo));
    });

    // ─── Restaurar foco al cerrar modales ────────────────────────────────
    ['modalPeso', 'modalPago', 'modalDescuento'].forEach(id => {
        document.getElementById(id)?.addEventListener('hidden.bs.modal', volverFocusCodigo);
    });

    // ─── Hover styles para search items ──────────────────────────────────
    document.addEventListener('mouseover', e => {
        if (e.target.closest?.('.search-item')) {
            e.target.closest('.search-item').style.background = '#f0f9ff';
        }
    });
    document.addEventListener('mouseout', e => {
        if (e.target.closest?.('.search-item')) {
            e.target.closest('.search-item').style.background = '';
        }
    });
});

// Exponer para uso desde HTML inline
window.quitarItem = quitarItem;
window.cambiarCantidad = cambiarCantidad;
window.editarCantidad = editarCantidad;
window.limpiarCarrito = limpiarCarrito;
window.abrirModalPago = abrirModalPago;
window.abrirModalDescuento = abrirModalDescuento;
window.confirmarPeso = confirmarPeso;
window.confirmarDescuento = confirmarDescuento;
window.confirmarVenta = confirmarVenta;
window.seleccionarMetodo = seleccionarMetodo;

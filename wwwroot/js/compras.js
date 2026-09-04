/**
 * compras.js — Módulo de gestión de compras/ingresos
 */

const STORAGE_KEY = 'despensa_compras_v1';

let state = { proveedorId: 0, items: [] };

function fmtMoneda(n) {
    return Math.round(n || 0).toLocaleString('es-PY');
}

function loadState() {
    try {
        const raw = sessionStorage.getItem(STORAGE_KEY);
        if (raw) state = JSON.parse(raw);
    } catch { state = { proveedorId: 0, items: [] }; }
}

function saveState() {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(state));
}

function clearState() {
    sessionStorage.removeItem(STORAGE_KEY);
    state = { proveedorId: 0, items: [] };
}

// ─── Agregar producto ─────────────────────────────────────────────────────────

function agregarProducto(prod) {
    const idx = state.items.findIndex(i => i.productoId === prod.id);
    if (idx >= 0) {
        state.items[idx].cantidad += 1;
    } else {
        state.items.push({
            productoId: prod.id,
            nombre: prod.nombre,
            codigoBarra: prod.codigoBarra,
            precioCompra: prod.precioCompra,
            cantidad: 1,
            tipoVenta: prod.tipoVenta,
            esPorPeso: prod.esPorPeso,
            unidadLabel: prod.unidadLabel
        });
    }
    saveState();
    renderLista();
    mostrarToast(`${prod.nombre} agregado`);
}

// ─── Render tabla ─────────────────────────────────────────────────────────────

function renderLista() {
    const tbody = document.getElementById('listaBody');
    const btnConfirmar = document.getElementById('btnConfirmar');
    const spanTotal = document.getElementById('spanTotal');
    const spanItems = document.getElementById('spanItems');
    const moneda = window.MONEDA ?? 'Gs.';

    if (!tbody) return;

    tbody.innerHTML = '';
    let total = 0;

    state.items.forEach((item, idx) => {
        const subtotal = item.cantidad * item.precioCompra;
        total += subtotal;

        const cantFmt = item.esPorPeso
            ? item.cantidad.toFixed(3)
            : (Number.isInteger(item.cantidad) ? item.cantidad : item.cantidad.toFixed(3));

        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td>
                <div class="fw-semibold" style="font-size:.9rem">${item.nombre}</div>
                ${item.codigoBarra ? `<div class="text-muted" style="font-size:.72rem"><i class="fas fa-barcode me-1"></i>${item.codigoBarra}</div>` : ''}
            </td>
            <td class="text-center">
                <span class="editable-cant" data-idx="${idx}" style="cursor:pointer;border-bottom:1px dashed #999">
                    ${cantFmt} ${item.unidadLabel}
                </span>
            </td>
            <td class="text-end">
                <input type="number" class="form-control form-control-sm text-end precio-input"
                    data-idx="${idx}" value="${Math.round(item.precioCompra)}"
                    min="0" step="1" style="width:90px;display:inline-block;">
            </td>
            <td class="text-end fw-bold">${moneda} ${fmtMoneda(subtotal)}</td>
            <td class="text-center">
                <button class="btn btn-sm btn-outline-danger" onclick="eliminarItem(${idx})">
                    <i class="fas fa-trash"></i>
                </button>
            </td>`;
        tbody.appendChild(tr);
    });

    // Precio editable inline
    tbody.querySelectorAll('.precio-input').forEach(inp => {
        inp.addEventListener('change', e => {
            const i = parseInt(e.target.dataset.idx);
            const val = parseFloat(e.target.value);
            if (!isNaN(val) && val >= 0) {
                state.items[i].precioCompra = val;
                saveState();
                renderLista();
            }
        });
    });

    // Cantidad editable inline
    tbody.querySelectorAll('.editable-cant').forEach(span => {
        span.addEventListener('click', e => editarCantidad(parseInt(e.target.dataset.idx)));
    });

    const totalFmt = moneda + ' ' + fmtMoneda(total);
    if (spanTotal) spanTotal.textContent = totalFmt;
    if (spanItems) spanItems.textContent = state.items.length + ' item(s)';
    if (btnConfirmar) btnConfirmar.disabled = state.items.length === 0;
}

function editarCantidad(idx) {
    const spans = document.querySelectorAll('.editable-cant');
    const span = spans[idx];
    if (!span) return;

    const item = state.items[idx];
    const inp = document.createElement('input');
    inp.type = 'number';
    inp.className = 'form-control form-control-sm text-center';
    inp.style.width = '80px';
    inp.style.display = 'inline-block';
    inp.value = item.cantidad;
    inp.min = item.esPorPeso ? '0.001' : '1';
    inp.step = item.esPorPeso ? '0.001' : '1';

    span.replaceWith(inp);
    inp.focus();
    inp.select();

    const commit = () => {
        const val = parseFloat(inp.value);
        if (!isNaN(val) && val > 0) {
            state.items[idx].cantidad = item.esPorPeso ? val : Math.round(val);
        }
        saveState();
        renderLista();
    };
    inp.addEventListener('blur', commit);
    inp.addEventListener('keydown', e => { if (e.key === 'Enter') inp.blur(); });
}

function eliminarItem(idx) {
    state.items.splice(idx, 1);
    saveState();
    renderLista();
}

// ─── Confirmar compra ─────────────────────────────────────────────────────────

async function confirmarCompra() {
    const provSel = document.getElementById('selectProveedor');
    const proveedorId = provSel ? parseInt(provSel.value) : 0;

    if (!proveedorId) {
        mostrarToast('Seleccione un proveedor', 'danger');
        return;
    }
    if (state.items.length === 0) {
        mostrarToast('Agregue al menos un producto', 'danger');
        return;
    }

    const numRemito = document.getElementById('inputRemito')?.value?.trim() || null;
    const observacion = document.getElementById('inputObservacion')?.value?.trim() || null;

    const btn = document.getElementById('btnConfirmar');
    if (btn) { btn.disabled = true; btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Procesando...'; }

    try {
        const token = document.querySelector('meta[name="xsrf-token"]')?.content ?? '';
        const res = await fetch('/Compras/Procesar', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'X-XSRF-TOKEN': token },
            body: JSON.stringify({
                proveedorId,
                numeroRemito: numRemito,
                observacion,
                items: state.items.map(i => ({
                    productoId: i.productoId,
                    cantidad: i.cantidad,
                    precioUnitario: i.precioCompra
                }))
            })
        });

        const data = await res.json();
        if (res.ok) {
            clearState();
            window.location.href = `/Compras/Detail/${data.compraId}`;
        } else {
            mostrarToast(data.error ?? 'Error al procesar', 'danger');
            if (btn) { btn.disabled = false; btn.innerHTML = '<i class="fas fa-check me-1"></i>Confirmar Compra'; }
        }
    } catch {
        mostrarToast('Error de red. Intente nuevamente.', 'danger');
        if (btn) { btn.disabled = false; btn.innerHTML = '<i class="fas fa-check me-1"></i>Confirmar Compra'; }
    }
}

// ─── Búsqueda por nombre (autocomplete) ──────────────────────────────────────

let debounceTimer;
function buscarNombre(q) {
    clearTimeout(debounceTimer);
    const drop = document.getElementById('busquedaDropdown');
    if (!drop) return;
    if (!q || q.length < 2) { drop.style.display = 'none'; return; }

    debounceTimer = setTimeout(async () => {
        const res = await fetch(`/Compras/BuscarNombre?q=${encodeURIComponent(q)}`);
        const lista = await res.json();
        renderDropdown(lista, drop);
    }, 280);
}

function renderDropdown(lista, drop) {
    drop.innerHTML = '';
    if (!lista.length) { drop.style.display = 'none'; return; }

    lista.forEach((p, i) => {
        const item = document.createElement('div');
        item.className = 'dropdown-item';
        item.style.cursor = 'pointer';
        item.tabIndex = 0;
        item.innerHTML = `<strong>${p.nombre}</strong>
            <span class="text-muted ms-2" style="font-size:.8rem">${p.codigoBarra ?? ''}</span>
            <span class="badge bg-secondary ms-2" style="font-size:.75rem">Compra: ${window.MONEDA ?? 'Gs.'} ${fmtMoneda(p.precioCompra)}</span>`;
        item.addEventListener('click', () => {
            agregarProducto(p);
            drop.style.display = 'none';
            document.getElementById('busquedaInput').value = '';
        });
        drop.appendChild(item);
    });
    drop.style.display = 'block';
}

// ─── Toast ────────────────────────────────────────────────────────────────────

function mostrarToast(msg, tipo = 'success') {
    const container = document.getElementById('toastContainer');
    if (!container) return;

    const id = 'toast_' + Date.now();
    const html = `<div id="${id}" class="toast align-items-center text-bg-${tipo} border-0 mb-2 show" role="alert">
        <div class="d-flex"><div class="toast-body">${msg}</div>
        <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button></div></div>`;
    container.insertAdjacentHTML('beforeend', html);
    setTimeout(() => document.getElementById(id)?.remove(), 3000);
}

// ─── Init ─────────────────────────────────────────────────────────────────────

document.addEventListener('DOMContentLoaded', () => {
    loadState();
    renderLista();

    // Barcode scanner
    if (typeof BarcodeReader !== 'undefined') {
        BarcodeReader.init('codigoBarraInput', async codigo => {
            try {
                const res = await fetch(`/Compras/BuscarProducto?codigo=${encodeURIComponent(codigo)}`);
                if (res.ok) {
                    const prod = await res.json();
                    agregarProducto(prod);
                    document.getElementById('codigoBarraInput').value = '';
                } else {
                    mostrarToast(`Código '${codigo}' no encontrado`, 'warning');
                }
            } catch {
                mostrarToast('Error de búsqueda', 'danger');
            }
        });
        BarcodeReader.enableScan();
    }

    // Búsqueda por nombre
    const busInput = document.getElementById('busquedaInput');
    if (busInput) {
        busInput.addEventListener('input', e => buscarNombre(e.target.value));
        document.addEventListener('click', e => {
            if (!busInput.contains(e.target))
                document.getElementById('busquedaDropdown').style.display = 'none';
        });
    }

    // Sync proveedor from storage
    const provSel = document.getElementById('selectProveedor');
    if (provSel && state.proveedorId) {
        provSel.value = state.proveedorId;
    }
    if (provSel) {
        provSel.addEventListener('change', () => {
            state.proveedorId = parseInt(provSel.value) || 0;
            saveState();
        });
    }
});

/**
 * BarcodeReader — lectura de códigos de barra mediante pistola USB/Bluetooth
 * (o digitación manual + Enter). Las lectoras se comportan como un teclado
 * que tipea muy rápido y termina con Enter; este módulo detecta esa cadencia
 * para distinguir un escaneo de una digitación manual.
 *
 * API pública:
 *   BarcodeReader.init(inputId, onScanCallback)
 *   BarcodeReader.enableScan()
 *   BarcodeReader.disableScan()
 *
 * onScanCallback recibe: (codigo: string, fromScanner: boolean)
 */
const BarcodeReader = (() => {
    'use strict';

    const MS_POR_CARACTER_LECTORA = 50; // por debajo de esto, se asume pistola
    const LARGO_MINIMO = 3;
    const TIMEOUT_ESPERA_MS = 10_000;

    let inputId = null;
    let callback = null;
    let inicioTipeo = 0;
    let inputActual = null;

    function obtenerInput() {
        return document.getElementById(inputId);
    }

    function onKeydown(e) {
        if (e.key.length === 1 && inicioTipeo === 0) {
            inicioTipeo = Date.now();
        }
        if (e.key === 'Enter') {
            e.preventDefault();
            procesar();
        }
    }

    function onPaste(e) {
        e.preventDefault();
        const pegado = ((e.clipboardData || window.clipboardData).getData('text') || '').trim();
        if (pegado.length >= LARGO_MINIMO) {
            inicioTipeo = 0;
            const input = obtenerInput();
            if (input) input.value = '';
            if (callback) callback(pegado, true);
        }
    }

    function procesar() {
        const input = obtenerInput();
        if (!input) return;

        const codigo = input.value.trim();
        if (codigo.length < LARGO_MINIMO) {
            inicioTipeo = 0;
            return;
        }

        const transcurrido = Date.now() - inicioTipeo;
        const esLectora = inicioTipeo > 0 && (transcurrido / codigo.length) < MS_POR_CARACTER_LECTORA;

        inicioTipeo = 0;
        input.value = '';

        if (callback) callback(codigo, esLectora);
    }

    function desconectar() {
        if (!inputActual) return;
        inputActual.removeEventListener('keydown', onKeydown);
        inputActual.removeEventListener('paste', onPaste);
        inputActual = null;
    }

    /**
     * Conecta el módulo a un <input> del formulario.
     * @param {string} idDelInput
     * @param {(codigo: string, fromScanner: boolean) => void} onScanCallback
     */
    function init(idDelInput, onScanCallback) {
        desconectar();
        inputId = idDelInput;
        callback = onScanCallback;

        const input = obtenerInput();
        if (!input) {
            console.warn(`[BarcodeReader] No se encontró el input #${idDelInput}`);
            return;
        }

        input.addEventListener('keydown', onKeydown);
        input.addEventListener('paste', onPaste);
        inputActual = input;
    }

    /** Pone el foco en el campo y muestra un indicador visual de espera. */
    function enableScan() {
        inicioTipeo = 0;
        const input = obtenerInput();
        if (!input) return;

        input.value = '';
        input.focus();
        input.placeholder = '⏳ Esperando código de la lectora...';
        input.classList.add('border-warning', 'border-2', 'fw-semibold');

        setTimeout(disableScan, TIMEOUT_ESPERA_MS);
    }

    /** Quita el indicador visual de espera. */
    function disableScan() {
        inicioTipeo = 0;
        const input = obtenerInput();
        if (!input) return;
        input.placeholder = 'Escanear, tipear o pegar el código...';
        input.classList.remove('border-warning', 'border-2', 'fw-semibold');
    }

    return { init, enableScan, disableScan };
})();

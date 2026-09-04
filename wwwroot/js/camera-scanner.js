/**
 * CameraScanner — escaneo de códigos de barra con la cámara del dispositivo,
 * sin necesitar lectora física. Usa la API nativa BarcodeDetector del
 * navegador (Chrome/Edge/Android). En navegadores sin soporte (Safari/Firefox
 * de escritorio) avisa y sugiere digitar el código manualmente o usar lectora.
 *
 * API pública:
 *   CameraScanner.isSupported()
 *   CameraScanner.open(onDetected)   // onDetected: function(codigo: string)
 *   CameraScanner.close()
 */
const CameraScanner = (() => {
    'use strict';

    const FORMATOS_SOPORTADOS = ['ean_13', 'ean_8', 'upc_a', 'upc_e', 'code_128', 'qr_code'];
    const MODAL_ID = 'cameraScannerModal';

    let detector = null;
    let stream = null;
    let escaneando = false;
    let frameId = null;
    let onDetected = null;

    function isSupported() {
        return 'BarcodeDetector' in window
            && !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia);
    }

    function crearModalSiNoExiste() {
        if (document.getElementById(MODAL_ID)) return;

        const contenedor = document.createElement('div');
        contenedor.innerHTML = `
        <div class="modal fade" id="${MODAL_ID}" tabindex="-1" data-bs-backdrop="static">
          <div class="modal-dialog modal-dialog-centered">
            <div class="modal-content border-0 shadow-lg">
              <div class="modal-header border-0 pb-0">
                <h6 class="modal-title fw-bold">
                  <i class="fas fa-camera me-2 text-primary"></i>Escanear con cámara
                </h6>
                <button type="button" class="btn-close" id="camScanCloseBtn"></button>
              </div>
              <div class="modal-body pt-2">
                <div id="camScanError" class="alert alert-warning py-2 small d-none"></div>
                <div class="ratio ratio-4x3 rounded-3 overflow-hidden bg-dark">
                  <video id="camScanVideo" autoplay playsinline muted></video>
                </div>
                <p class="text-muted small mt-2 mb-0 text-center">
                  Apuntá con la cámara al código de barras del producto.
                </p>
              </div>
            </div>
          </div>
        </div>`;
        document.body.appendChild(contenedor.firstElementChild);

        document.getElementById('camScanCloseBtn').addEventListener('click', close);
        document.getElementById(MODAL_ID).addEventListener('hidden.bs.modal', detenerStream);
    }

    async function open(callback) {
        if (!isSupported()) {
            alert('Este navegador no soporta escaneo por cámara. Usá una lectora física o escribí el código manualmente.');
            return;
        }

        onDetected = callback;
        crearModalSiNoExiste();

        const modalEl = document.getElementById(MODAL_ID);
        const errorEl = document.getElementById('camScanError');
        errorEl.classList.add('d-none');

        bootstrap.Modal.getOrCreateInstance(modalEl).show();

        try {
            detector = detector || new BarcodeDetector({ formats: FORMATOS_SOPORTADOS });
            stream = await navigator.mediaDevices.getUserMedia({
                video: { facingMode: 'environment' }
            });

            const video = document.getElementById('camScanVideo');
            video.srcObject = stream;
            await video.play();

            escaneando = true;
            bucleDeteccion(video);
        } catch (err) {
            errorEl.textContent = 'No se pudo acceder a la cámara: ' + (err.message || err);
            errorEl.classList.remove('d-none');
        }
    }

    async function bucleDeteccion(video) {
        if (!escaneando) return;
        try {
            const resultados = await detector.detect(video);
            if (resultados.length > 0) {
                const codigo = resultados[0].rawValue;
                close();
                if (onDetected) onDetected(codigo);
                return;
            }
        } catch {
            // frame no decodificable: se reintenta en el próximo cuadro
        }
        frameId = requestAnimationFrame(() => bucleDeteccion(video));
    }

    function detenerStream() {
        escaneando = false;
        if (frameId) cancelAnimationFrame(frameId);
        frameId = null;
        if (stream) {
            stream.getTracks().forEach(track => track.stop());
            stream = null;
        }
    }

    function close() {
        detenerStream();
        const modalEl = document.getElementById(MODAL_ID);
        if (modalEl) bootstrap.Modal.getInstance(modalEl)?.hide();
    }

    return { isSupported, open, close };
})();

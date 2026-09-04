/**
 * Ean13Render — dibuja un código EAN-13 como SVG (barras + dígitos),
 * listo para previsualizar en pantalla e imprimir en una etiqueta.
 *
 * API pública:
 *   Ean13Render.renderSvg(containerId, codigo13, opts?)
 *   Ean13Render.buildSvg(codigo13, opts?)
 */
const Ean13Render = (() => {
    'use strict';

    // Patrones de 7 módulos por dígito (izquierda con paridad impar/par, derecha).
    const PATRON_L = {
        '0': '0001101', '1': '0011001', '2': '0010011', '3': '0111101', '4': '0100011',
        '5': '0110001', '6': '0101111', '7': '0111011', '8': '0110111', '9': '0001011'
    };
    const PATRON_G = {
        '0': '0100111', '1': '0110011', '2': '0011011', '3': '0100001', '4': '0011101',
        '5': '0111001', '6': '0000101', '7': '0010001', '8': '0001001', '9': '0010111'
    };
    const PATRON_R = {
        '0': '1110010', '1': '1100110', '2': '1101100', '3': '1000010', '4': '1011100',
        '5': '1001110', '6': '1010000', '7': '1000100', '8': '1001000', '9': '1110100'
    };
    // Secuencia L/G de los 6 dígitos del grupo izquierdo, según el primer dígito (implícito).
    const PARIDAD_POR_PRIMER_DIGITO = {
        '0': 'LLLLLL', '1': 'LLGLGG', '2': 'LLGGLG', '3': 'LLGGGL', '4': 'LGLLGG',
        '5': 'LGGLLG', '6': 'LGGGLL', '7': 'LGLGLG', '8': 'LGLGGL', '9': 'LGGLGL'
    };

    function codigoABits(codigo13) {
        if (!/^\d{13}$/.test(codigo13)) throw new Error('Código EAN-13 inválido');

        const primerDigito = codigo13[0];
        const grupoIzquierdo = codigo13.slice(1, 7);
        const grupoDerecho = codigo13.slice(7, 13);
        const paridad = PARIDAD_POR_PRIMER_DIGITO[primerDigito];

        let bits = '101'; // guarda inicial
        for (let i = 0; i < 6; i++) {
            const patron = paridad[i] === 'L' ? PATRON_L : PATRON_G;
            bits += patron[grupoIzquierdo[i]];
        }
        bits += '01010'; // guarda central
        for (let i = 0; i < 6; i++) {
            bits += PATRON_R[grupoDerecho[i]];
        }
        bits += '101'; // guarda final
        return bits;
    }

    function buildSvg(codigo13, opts = {}) {
        const anchoModulo = opts.moduleWidth || 2;
        const altoBarras = opts.barHeight || 70;
        const bits = codigoABits(codigo13);
        const anchoTotal = bits.length * anchoModulo;
        const altoTotal = altoBarras + 24;

        let x = 0;
        let rects = '';
        for (const bit of bits) {
            if (bit === '1') {
                rects += `<rect x="${x}" y="0" width="${anchoModulo}" height="${altoBarras}" fill="#000"/>`;
            }
            x += anchoModulo;
        }

        return `<svg xmlns="http://www.w3.org/2000/svg" width="${anchoTotal}" height="${altoTotal}" viewBox="0 0 ${anchoTotal} ${altoTotal}">`
            + `<rect x="0" y="0" width="${anchoTotal}" height="${altoTotal}" fill="#fff"/>`
            + rects
            + `<text x="${anchoTotal / 2}" y="${altoBarras + 16}" text-anchor="middle" `
            + `font-family="monospace" font-size="14" letter-spacing="2">${codigo13}</text>`
            + `</svg>`;
    }

    function renderSvg(containerId, codigo13, opts = {}) {
        const svg = buildSvg(codigo13, opts);
        const contenedor = document.getElementById(containerId);
        if (contenedor) contenedor.innerHTML = svg;
        return svg;
    }

    return { renderSvg, buildSvg };
})();

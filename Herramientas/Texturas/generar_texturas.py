"""
Generador de texturas PBR repetibles (sin costuras) para Voidform: Bio-Forge.
=============================================================================

Produce, para cada superficie que usa MaterialesProcedurales.cs, dos imágenes:
  * {superficie}_albedo.png  -> detalle de color en ESCALA DE GRISES (0,55-1,0). Unity lo multiplica por el
                                color del material, así una misma textura sirve para todas las paletas.
  * {superficie}_normal.png  -> mapa de normales en convención OpenGL (+Y arriba, la que usa Unity).

Todas las texturas son periódicas (el borde derecho continúa en el izquierdo y el superior en el inferior),
porque se generan con ruido espectral (FFT) y Voronoi con distancias "envueltas". Así el mosaico que calcula
MaterialesProcedurales según el tamaño del objeto nunca muestra costuras.

Uso:
    python3 Herramientas/Texturas/generar_texturas.py  [carpeta_salida]  [tamaño]
Por defecto escribe en Assets/Resources/Texturas a 512 px.
Requisitos: Python 3 con numpy y Pillow (pip install numpy pillow).
"""

import os
import sys

import numpy as np
from PIL import Image

TAM = 512
RNG = np.random.default_rng(20261121)


# ---------------------------------------------------------------------------
# Primitivas de ruido periódico
# ---------------------------------------------------------------------------

def ruido_espectral(n, beta=2.0, escala_x=1.0, escala_y=1.0, semilla=None):
    """Ruido fractal 1/f^beta periódico. escala_x/escala_y estiran el ruido (vetas, fibras)."""
    rng = np.random.default_rng(semilla) if semilla is not None else RNG
    fx = np.fft.fftfreq(n)[None, :] * escala_x
    fy = np.fft.fftfreq(n)[:, None] * escala_y
    f = np.sqrt(fx * fx + fy * fy)
    f[0, 0] = 1.0
    amplitud = 1.0 / (f ** (beta / 2.0))
    amplitud[0, 0] = 0.0
    fase = rng.uniform(0, 2 * np.pi, (n, n))
    espectro = amplitud * np.exp(1j * fase)
    r = np.real(np.fft.ifft2(espectro))
    return normalizar(r)


def normalizar(a):
    a = a - a.min()
    m = a.max()
    return a / m if m > 0 else a


def voronoi(n, puntos, semilla=None):
    """Devuelve (F1, F2, id) de un Voronoi periódico en el cuadrado unidad."""
    rng = np.random.default_rng(semilla) if semilla is not None else RNG
    p = rng.uniform(0, 1, (puntos, 2))
    ys, xs = np.mgrid[0:n, 0:n] / n
    f1 = np.full((n, n), 9.0)
    f2 = np.full((n, n), 9.0)
    ident = np.zeros((n, n), dtype=np.int32)
    for i, (px, py) in enumerate(p):
        dx = np.abs(xs - px)
        dx = np.minimum(dx, 1 - dx)
        dy = np.abs(ys - py)
        dy = np.minimum(dy, 1 - dy)
        d = np.sqrt(dx * dx + dy * dy)
        menor = d < f1
        f2 = np.where(menor, f1, np.minimum(f2, d))
        ident = np.where(menor, i, ident)
        f1 = np.where(menor, d, f1)
    return f1, f2, ident


def coordenadas(n):
    ys, xs = np.mgrid[0:n, 0:n] / n
    return xs, ys


def suavizar(a, pasos=1):
    for _ in range(pasos):
        a = (a + np.roll(a, 1, 0) + np.roll(a, -1, 0) + np.roll(a, 1, 1) + np.roll(a, -1, 1)) / 5.0
    return a


# ---------------------------------------------------------------------------
# Definición de cada superficie: devuelve (altura 0..1, albedo 0..1, fuerza_normal)
# ---------------------------------------------------------------------------

def sup_acero(n):
    cepillado = ruido_espectral(n, 1.6, escala_x=0.02, escala_y=1.0)
    fino = ruido_espectral(n, 1.2)
    rayas = np.zeros((n, n))
    for _ in range(70):
        y0 = RNG.integers(0, n)
        x0 = RNG.integers(0, n)
        largo = RNG.integers(n // 10, n // 3)
        pendiente = RNG.uniform(-0.25, 0.25)
        xs = (x0 + np.arange(largo)) % n
        yss = (y0 + (np.arange(largo) * pendiente).astype(int)) % n
        rayas[yss, xs] = RNG.uniform(0.4, 1.0)
    rayas = suavizar(rayas, 1)
    altura = 0.6 * cepillado + 0.25 * fino - 0.5 * rayas
    albedo = 0.86 + 0.08 * cepillado + 0.04 * fino + 0.06 * rayas
    return normalizar(altura), albedo, 2.0


def sup_hierro(n):
    base = ruido_espectral(n, 2.2)
    poros = ruido_espectral(n, 0.6)
    oxido = ruido_espectral(n, 3.0)
    manchas = np.clip((oxido - 0.55) * 4, 0, 1)
    altura = 0.6 * base + 0.4 * (poros > 0.72) * poros
    albedo = 0.78 + 0.14 * base - 0.18 * manchas
    return normalizar(altura), albedo, 3.5


def sup_martillado(n):
    f1, _, _ = voronoi(n, 180)
    hoyuelos = 1 - normalizar(f1) ** 0.6
    fino = ruido_espectral(n, 1.4)
    altura = 0.8 * (1 - hoyuelos) + 0.2 * fino
    albedo = 0.9 + 0.06 * fino + 0.04 * hoyuelos
    return normalizar(altura), albedo, 1.6


def sup_metal_placas(n):
    xs, ys = coordenadas(n)
    paneles = 4
    gx = (xs * paneles) % 1.0
    gy = (ys * paneles * 1.5) % 1.0
    borde = np.minimum(np.minimum(gx, 1 - gx), np.minimum(gy, 1 - gy))
    juntas = np.clip(borde * 60, 0, 1)
    remaches = np.zeros((n, n))
    for cx in np.linspace(0.04, 0.96, 2):
        for cy in np.linspace(0.07, 0.93, 2):
            d = np.sqrt(((gx - cx) / 1.0) ** 2 + ((gy - cy) * 1.5) ** 2)
            remaches = np.maximum(remaches, np.clip(1 - d / 0.035, 0, 1))
    desgaste = ruido_espectral(n, 2.0)
    cepillado = ruido_espectral(n, 1.6, escala_x=1.0, escala_y=0.03)
    altura = 0.55 * juntas + 0.35 * np.sqrt(remaches) + 0.1 * cepillado
    albedo = 0.82 + 0.1 * cepillado + 0.06 * desgaste - 0.25 * (1 - juntas) + 0.08 * remaches
    return normalizar(altura), albedo, 4.0


def sup_obsidiana(n):
    f1, f2, _ = voronoi(n, 40)
    fracturas = np.clip((f2 - f1) * 25, 0, 1)
    concoidal = np.sin(normalizar(f1) * 9) * 0.5 + 0.5
    altura = 0.7 * fracturas + 0.1 * concoidal * fracturas + 0.1 * ruido_espectral(n, 2.0)
    albedo = 0.7 + 0.25 * fracturas
    return normalizar(altura), albedo, 2.5


def sup_cristal(n):
    f1, f2, ident = voronoi(n, 28)
    facetas = (ident * 0.6180339) % 1.0
    aristas = np.clip((f2 - f1) * 30, 0, 1)
    altura = 0.5 * facetas + 0.5 * aristas
    albedo = 0.85 + 0.15 * facetas * aristas
    return normalizar(altura), albedo, 1.8


def sup_piedra(n):
    f1, f2, ident = voronoi(n, 22)
    juntas = np.clip((f2 - f1) * 18, 0, 1)
    tono = (ident * 0.37) % 1.0
    rugosidad = ruido_espectral(n, 1.8)
    grano = ruido_espectral(n, 0.8)
    altura = 0.55 * np.sqrt(juntas) + 0.3 * rugosidad + 0.15 * grano
    albedo = 0.68 + 0.12 * tono + 0.12 * rugosidad + 0.08 * grano - 0.2 * (1 - juntas)
    return normalizar(altura), albedo, 4.5


def sup_roca(n):
    base = ruido_espectral(n, 2.4)
    medio = ruido_espectral(n, 1.6)
    grietas = np.clip(1 - np.abs(ruido_espectral(n, 2.6) - 0.5) * 30, 0, 1)
    altura = 0.6 * base + 0.35 * medio - 0.35 * grietas
    albedo = 0.66 + 0.18 * medio + 0.1 * base - 0.2 * grietas
    return normalizar(altura), albedo, 5.0


def sup_coral(n):
    f1, f2, _ = voronoi(n, 260)
    poros = np.clip(f1 * 30, 0, 1)
    ondas = ruido_espectral(n, 2.6)
    altura = 0.65 * poros + 0.35 * ondas
    albedo = 0.75 + 0.2 * poros + 0.05 * ondas
    return normalizar(altura), albedo, 3.5


def sup_corteza(n):
    vetas = ruido_espectral(n, 2.2, escala_x=0.08, escala_y=1.0)
    surcos = np.clip(1 - np.abs(vetas - 0.5) * 6, 0, 1)
    placas = ruido_espectral(n, 1.8, escala_x=0.35, escala_y=1.0)
    musgo = np.clip((ruido_espectral(n, 3.0) - 0.62) * 5, 0, 1)
    altura = 0.5 * placas + 0.2 * vetas - 0.55 * surcos
    albedo = 0.72 + 0.18 * placas - 0.25 * surcos + 0.06 * musgo
    return normalizar(altura), albedo, 6.0


def sup_madera(n):
    xs, ys = coordenadas(n)
    deform = ruido_espectral(n, 2.6, escala_x=0.15, escala_y=1.0)
    anillos = np.sin((xs * 14 + deform * 3.0) * 2 * np.pi) * 0.5 + 0.5
    fibra = ruido_espectral(n, 1.2, escala_x=0.03, escala_y=1.0)
    altura = 0.35 * anillos + 0.65 * fibra
    albedo = 0.72 + 0.16 * anillos + 0.12 * fibra
    return normalizar(altura), albedo, 1.6


def sup_cuero(n):
    f1, f2, _ = voronoi(n, 900)
    grano = np.clip((f2 - f1) * 90, 0, 1)
    arrugas = ruido_espectral(n, 2.4)
    altura = 0.55 * grano + 0.45 * arrugas
    albedo = 0.8 + 0.12 * arrugas + 0.06 * grano
    return normalizar(altura), albedo, 2.6


def sup_tela(n):
    xs, ys = coordenadas(n)
    hilos = 96
    urdimbre = np.sin(xs * hilos * 2 * np.pi) * 0.5 + 0.5
    trama = np.sin(ys * hilos * 2 * np.pi) * 0.5 + 0.5
    cruce = ((np.floor(xs * hilos) + np.floor(ys * hilos)) % 2)
    tejido = np.where(cruce > 0, urdimbre, trama)
    pelusa = ruido_espectral(n, 1.0)
    sucio = ruido_espectral(n, 2.6)
    altura = 0.75 * tejido + 0.25 * pelusa
    albedo = 0.8 + 0.1 * tejido + 0.06 * pelusa - 0.08 * sucio
    return normalizar(altura), albedo, 2.2


def sup_suelo_bosque(n):
    f1, f2, ident = voronoi(n, 120)
    hojas = np.clip((f2 - f1) * 40, 0, 1) * ((ident * 0.73) % 1.0)
    humus = ruido_espectral(n, 2.0)
    ramitas = np.clip(1 - np.abs(ruido_espectral(n, 2.2, 1.0, 0.1) - 0.5) * 40, 0, 1)
    altura = 0.5 * hojas + 0.35 * humus + 0.25 * ramitas
    albedo = 0.6 + 0.25 * hojas + 0.15 * humus
    return normalizar(altura), albedo, 4.0


def sup_suelo_abismos(n):
    xs, ys = coordenadas(n)
    deform = ruido_espectral(n, 2.6)
    ondulas = np.sin((ys * 20 + deform * 2.5) * 2 * np.pi) * 0.5 + 0.5
    arena = ruido_espectral(n, 0.4)
    altura = 0.6 * ondulas + 0.4 * arena
    albedo = 0.75 + 0.12 * ondulas + 0.1 * arena
    return normalizar(altura), albedo, 3.0


def sup_suelo_caldera(n):
    f1, f2, _ = voronoi(n, 34)
    grietas = np.clip((f2 - f1) * 14, 0, 1)
    escoria = ruido_espectral(n, 1.6)
    altura = 0.7 * np.sqrt(grietas) + 0.3 * escoria
    albedo = 0.58 + 0.3 * grietas + 0.12 * escoria
    return normalizar(altura), albedo, 5.0


def sup_suelo_agujas(n):
    grano = ruido_espectral(n, 0.5)
    vetas = ruido_espectral(n, 2.0, escala_x=1.0, escala_y=0.05)
    limaduras = (ruido_espectral(n, 0.2) > 0.82) * 1.0
    altura = 0.5 * grano + 0.35 * vetas + 0.15 * limaduras
    albedo = 0.72 + 0.12 * vetas + 0.16 * limaduras
    return normalizar(altura), albedo, 2.8


def sup_suelo_nexus(n):
    xs, ys = coordenadas(n)
    escala = 6
    # Teselado hexagonal periódico: 6 columnas y 8 filas (número par, para que el desfase de filas alternas
    # también se repita sin costura); los hexágonos quedan apenas achatados.
    qx = xs * escala
    qy = ys * 8
    fila = np.floor(qy)
    desplaz = (fila % 2) * 0.5
    cx = np.floor(qx + desplaz) - desplaz + 0.5
    cy = fila + 0.5
    d = np.sqrt((qx - cx) ** 2 + ((qy - cy) * np.sqrt(3) / 2) ** 2)
    juntas = np.clip((0.5 - d) * 14, 0, 1)
    pulido = ruido_espectral(n, 2.0)
    altura = 0.8 * juntas + 0.2 * pulido
    albedo = 0.82 + 0.1 * pulido - 0.25 * (1 - juntas)
    return normalizar(altura), albedo, 3.0


SUPERFICIES = {
    "acero": sup_acero,
    "hierro": sup_hierro,
    "oro": sup_martillado,
    "laton": sup_martillado,
    "metal_placas": sup_metal_placas,
    "obsidiana": sup_obsidiana,
    "cristal": sup_cristal,
    "piedra": sup_piedra,
    "roca": sup_roca,
    "coral": sup_coral,
    "corteza": sup_corteza,
    "madera": sup_madera,
    "cuero": sup_cuero,
    "tela": sup_tela,
    "suelo_bosque": sup_suelo_bosque,
    "suelo_abismos": sup_suelo_abismos,
    "suelo_caldera": sup_suelo_caldera,
    "suelo_agujas": sup_suelo_agujas,
    "suelo_nexus": sup_suelo_nexus,
}


# ---------------------------------------------------------------------------
# Salida
# ---------------------------------------------------------------------------

def normal_desde_altura(altura, fuerza):
    """Normales OpenGL a partir de la altura, con derivadas periódicas (np.roll) para que no haya costuras."""
    dx = (np.roll(altura, -1, 1) - np.roll(altura, 1, 1)) * 0.5
    dy = (np.roll(altura, -1, 0) - np.roll(altura, 1, 0)) * 0.5
    k = fuerza * altura.shape[0] / 256.0
    nx = -dx * k
    ny = dy * k  # la fila crece hacia abajo y la V de la textura hacia arriba
    nz = np.ones_like(altura)
    largo = np.sqrt(nx * nx + ny * ny + nz * nz)
    n = np.stack([nx / largo, ny / largo, nz / largo], axis=-1)
    return ((n * 0.5 + 0.5) * 255).astype(np.uint8)


def guardar(carpeta, nombre, altura, albedo, fuerza):
    albedo = np.clip(albedo, 0.55, 1.0)
    gris = (albedo * 255).astype(np.uint8)
    Image.fromarray(np.stack([gris] * 3, axis=-1), "RGB").save(os.path.join(carpeta, f"{nombre}_albedo.png"), optimize=True)
    Image.fromarray(normal_desde_altura(suavizar(altura, 1), fuerza), "RGB").save(os.path.join(carpeta, f"{nombre}_normal.png"), optimize=True)


def main():
    raiz = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
    carpeta = sys.argv[1] if len(sys.argv) > 1 else os.path.join(raiz, "Assets", "Resources", "Texturas")
    n = int(sys.argv[2]) if len(sys.argv) > 2 else TAM
    os.makedirs(carpeta, exist_ok=True)
    for nombre, funcion in SUPERFICIES.items():
        altura, albedo, fuerza = funcion(n)
        guardar(carpeta, nombre, altura, albedo, fuerza)
        print(f"  textura {nombre}: albedo medio {np.clip(albedo, 0.55, 1).mean():.2f}")
    print(f"Texturas escritas en {carpeta}")


if __name__ == "__main__":
    main()

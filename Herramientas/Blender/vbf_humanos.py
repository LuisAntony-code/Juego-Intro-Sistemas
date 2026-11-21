"""
Los 5 arquetipos humanos de Voidform: Bio-Forge (1,8 m, mirando a +Z de Unity).

Esqueleto (idéntico a ConstructorModelosPersonajes.ConstruirHumanoPrimitivo, con W = anchura del arquetipo):
    Cadera (0, 0.95, 0) ─ Torso (0, 1.01, 0) ─ Cabeza (0, 1.61, 0)
                         ├ Brazo_I/D (∓0.23W, 1.51, 0) ─ Antebrazo (…, 1.21) ─ Mano (…, 0.93)
                         └ Capa (0, 1.56, -0.15)  [solo Paladín]
    Cadera ─ Muslo_I/D (∓0.11W, 0.93, 0) ─ Pierna (…, 0.48) ─ Pie (…, 0.04)
"""

import math

from vbf_geometria import (V, Escena, caja, cilindro, cono, curva_bezier, elipsoide, loft, miembro, toro, tubo,
                           unir)

LADOS = (("I", -1), ("D", 1))


def esqueleto(esc, w):
    esc.articulacion("Cadera", None, (0, 0.95, 0))
    esc.articulacion("Torso", "Cadera", (0, 1.01, 0))
    esc.articulacion("Cabeza", "Torso", (0, 1.61, 0))
    for l, s in LADOS:
        x = 0.23 * w * s
        esc.articulacion(f"Brazo_{l}", "Torso", (x, 1.51, 0))
        esc.articulacion(f"Antebrazo_{l}", f"Brazo_{l}", (x, 1.21, 0))
        esc.articulacion(f"Mano_{l}", f"Antebrazo_{l}", (x, 0.93, 0))
        xm = 0.11 * w * s
        esc.articulacion(f"Muslo_{l}", "Cadera", (xm, 0.93, 0))
        esc.articulacion(f"Pierna_{l}", f"Muslo_{l}", (xm, 0.48, 0))
        esc.articulacion(f"Pie_{l}", f"Pierna_{l}", (xm, 0.04, 0))


# ---------------------------------------------------------------------------
# Cuerpo base (traje interior) y piezas comunes
# ---------------------------------------------------------------------------

def cuerpo_base(esc, w, m_traje, m_guante, m_bota, ancho_pierna=1.0, m_piernas=None):
    """Anatomía bajo la armadura: proporciones realistas de un adulto de 1,8 m."""
    m_piernas = m_piernas or m_traje
    esc.pieza("Pelvis", elipsoide((0, 0.97, 0), (0.165 * w, 0.11, 0.115)), m_piernas, "Cadera", 'suave')
    esc.pieza("Abdomen", tubo([(0, 1.0, 0), (0, 1.12, 0), (0, 1.25, 0)], [0.145 * w, 0.14 * w, 0.158 * w], 18, (1, 0.7)), m_traje, "Torso", 'suave')
    esc.pieza("Pecho", elipsoide((0, 1.37, 0.0), (0.185 * w, 0.19, 0.125)), m_traje, "Torso", 'suave')
    esc.pieza("Cuello", tubo([(0, 1.5, 0), (0, 1.6, 0.005), (0, 1.67, 0.01)], [0.06, 0.052, 0.05], 14), m_traje, "Cabeza", 'suave')

    for l, s in LADOS:
        x = 0.23 * w * s
        esc.pieza(f"Deltoides_{l}", elipsoide((x - 0.01 * s, 1.485, 0), (0.07, 0.075, 0.07)), m_traje, f"Brazo_{l}", 'suave')
        esc.pieza(f"Humero_{l}", miembro((x, 1.49, 0), (x, 1.22, 0), 0.058, 0.048, abultamiento=0.12), m_traje, f"Brazo_{l}", 'suave')
        esc.pieza(f"Cubito_{l}", miembro((x, 1.21, 0), (x, 0.965, 0.0), 0.05, 0.036, abultamiento=0.15, aplanar=(1, 0.85)), m_traje, f"Antebrazo_{l}", 'suave')
        mano(esc, x, s, m_guante, l)

        xm = 0.11 * w * s
        esc.pieza(f"Femur_{l}", miembro((xm, 0.95, 0), (xm, 0.49, 0.01), 0.088 * ancho_pierna, 0.058 * ancho_pierna, abultamiento=0.1), m_piernas, f"Muslo_{l}", 'suave')
        esc.pieza(f"Tibia_{l}", miembro((xm, 0.48, 0.01), (xm, 0.1, -0.005), 0.058 * ancho_pierna, 0.04 * ancho_pierna, abultamiento=0.14), m_piernas, f"Pierna_{l}", 'suave')
        bota(esc, xm, s, m_bota, l)


def mano(esc, x, s, material, l, grosor=1.0):
    """Mano enguantada semicerrada (empuña el arma a la altura de la palma)."""
    partes = [caja((x, 0.885, 0.0), (0.035 * grosor, 0.085, 0.085))]
    for i in range(4):
        z = -0.03 + i * 0.02
        base = V(x, 0.845, z)
        partes.append(tubo([base, base + V(-0.012 * s, -0.035, 0), base + V(-0.03 * s, -0.045, 0)], [0.0105, 0.0095, 0.008], 8))
    partes.append(tubo([(x - 0.01 * s, 0.88, 0.04), (x - 0.03 * s, 0.86, 0.05), (x - 0.04 * s, 0.84, 0.045)], [0.012, 0.011, 0.009], 8))
    esc.pieza(f"Guante_{l}", unir(*partes), material, f"Mano_{l}", 'suave')


def bota(esc, xm, s, material, l, alta=0.22, robusta=1.0):
    partes = [
        miembro((xm, 0.06, -0.01), (xm, 0.06 + alta, -0.005), 0.05 * robusta, 0.05 * robusta, aplanar=(1, 1.1)),
        caja((xm, 0.04, 0.045), (0.1 * robusta, 0.08, 0.24), conicidad=0.15),
        elipsoide((xm, 0.045, 0.15), (0.05 * robusta, 0.042, 0.06), 16, 8),
    ]
    esc.pieza(f"Bota_{l}", unir(*partes), material, f"Pie_{l}", 'suave')
    esc.pieza(f"Suela_{l}", caja((xm, 0.008, 0.05), (0.104 * robusta, 0.016, 0.27)), esc.material("cuero_suela", (0.06, 0.05, 0.045), 0, 0.9), f"Pie_{l}", 'duro')


def hombrera_placas(esc, x, s, material, l, capas=3, tam=1.0, m_ribete=None, y=1.5):
    """Hombrera de placas superpuestas (lames), cada una algo más pequeña y más baja que la anterior."""
    for i in range(capas):
        r = (0.11 - i * 0.012) * tam
        c = (x + 0.02 * s, y - 0.035 * i, 0)
        esc.pieza(f"Hombrera_{l}_{i}", elipsoide(c, (r, r * 0.75, r * 0.95), 20, 8, recorte_inferior=-0.15), material, f"Brazo_{l}", 'duro')
        if m_ribete is not None and i == 0:
            esc.pieza(f"Ribete_{l}", toro((c[0], c[1] - r * 0.11, 0), r * 0.98, 0.008, escala=(1, 1, 0.95)), m_ribete, f"Brazo_{l}", 'suave')


def brazal(esc, x, material, l, y0=1.17, y1=0.98, r0=0.058, r1=0.047):
    esc.pieza(f"Brazal_{l}", miembro((x, y0, 0), (x, y1, 0), r0, r1, tramos=3, aplanar=(1, 0.9)), material, f"Antebrazo_{l}", 'duro')


def rodillera(esc, xm, material, l, tam=1.0):
    esc.pieza(f"Rodillera_{l}", elipsoide((xm, 0.48, 0.045), (0.065 * tam, 0.07 * tam, 0.05 * tam), 18, 10), material, f"Pierna_{l}", 'duro')


def greba(esc, xm, material, l):
    esc.pieza(f"Greba_{l}", elipsoide((xm, 0.28, 0.025), (0.06, 0.17, 0.052), 18, 10, recorte_inferior=-0.95, recorte_superior=0.95), material, f"Pierna_{l}", 'duro')


# ---------------------------------------------------------------------------
# Arquetipos
# ---------------------------------------------------------------------------

def cazador(esc):
    w = 1.0
    verde = esc.material("tela_verde", (0.17, 0.32, 0.18), 0, 0.85)
    verde_osc = esc.material("tela_verde_oscura", (0.1, 0.2, 0.11), 0, 0.9)
    cuero = esc.material("cuero", (0.28, 0.18, 0.1), 0, 0.6)
    cuero_osc = esc.material("cuero_oscuro", (0.16, 0.1, 0.06), 0, 0.65)
    pantalon = esc.material("tela_parda", (0.3, 0.26, 0.2), 0, 0.9)
    acero = esc.material("acero", (0.82, 0.84, 0.86), 0.9, 0.35)
    madera = esc.material("madera", (0.5, 0.36, 0.22), 0, 0.6)
    pluma = esc.material("tela_pluma", (0.85, 0.82, 0.74), 0, 0.8)
    visor = esc.material("emisivo_hielo", (0.75, 0.9, 1.0), 0, 0.3, emision=2.0)

    esqueleto(esc, w)
    cuerpo_base(esc, w, verde_osc, cuero_osc, cuero, m_piernas=pantalon)

    # Gambesón acolchado: cuerpo verde con costuras horizontales.
    esc.pieza("Gambeson", unir(tubo([(0, 1.0, 0), (0, 1.1, 0), (0, 1.2, 0.0), (0, 1.3, 0.005), (0, 1.4, 0.0), (0, 1.48, -0.005)],
                                    [0.16, 0.158, 0.168, 0.185, 0.19, 0.17], 22, (1, 0.72), tapas_redondas=False),
                                 elipsoide((0, 1.37, 0.01), (0.19, 0.16, 0.13))), verde, "Torso", 'liso')
    esc.pieza("Faldon del gambeson", tubo([(0, 1.03, 0), (0, 0.92, 0), (0, 0.8, 0)], [0.168, 0.2, 0.225], 22, (1, 0.78), tapas_redondas=False), verde, "Cadera", 'liso')
    esc.pieza("Cinturon", unir(toro((0, 1.0, 0), 0.165, 0.02, escala=(1, 1.2, 0.76)), caja((0, 1.0, 0.128), (0.05, 0.045, 0.015))), cuero, "Cadera", 'suave')
    esc.pieza("Bolsas", unir(caja((-0.12, 0.96, 0.08), (0.06, 0.07, 0.04), rot=(0, 30, 0)), caja((0.13, 0.96, 0.06), (0.05, 0.06, 0.04), rot=(0, -40, 0))), cuero_osc, "Cadera", 'duro')

    # Manto corto y capucha.
    esc.pieza("Manto", elipsoide((0, 1.47, -0.01), (0.27, 0.11, 0.2), 24, 10, recorte_inferior=-0.6), verde_osc, "Torso", 'suave')
    esc.pieza("Capucha", unir(elipsoide((0, 1.79, -0.035), (0.128, 0.15, 0.135)),
                              cono((0, 1.84, -0.12), (0, 1.68, -0.27), 0.065)), verde_osc, "Cabeza", 'suave')
    esc.pieza("Rostro", elipsoide((0, 1.765, 0.03), (0.082, 0.105, 0.095)), cuero_osc, "Cabeza", 'suave')
    esc.pieza("Mascara", elipsoide((0, 1.71, 0.05), (0.088, 0.058, 0.085), 18, 8), pantalon, "Cabeza", 'suave')
    esc.pieza("Visor", elipsoide((0, 1.785, 0.1), (0.07, 0.016, 0.03), 16, 6), visor, "Cabeza", 'suave')

    for l, s in LADOS:
        x = 0.23 * w * s
        brazal(esc, x, cuero, l)
        esc.pieza(f"Correas_{l}", unir(*[toro((x, y, 0), 0.055, 0.006, seg=18, seg_menor=6) for y in (1.12, 1.04)]), cuero_osc, f"Antebrazo_{l}", 'plano')
        xm = 0.11 * w * s
        rodillera(esc, xm, cuero, l, 0.9)
    # Hombrera de cuero solo en el hombro del arco (izquierdo).
    hombrera_placas(esc, -0.23, -1, cuero, "I", capas=2, tam=0.85)

    # Carcaj con flechas a la espalda.
    carcaj = [cilindro((0.07, 1.33, -0.17), 0.05, 0.42, seg=16, radio_superior=0.055)]
    esc.pieza("Carcaj", unir(*carcaj), cuero, "Torso", 'suave')
    astas, plumas = [], []
    for i in range(6):
        dx, dz = (i % 3 - 1) * 0.022, (i // 3 - 0.5) * 0.025
        base = V(0.07 + dx, 1.4, -0.17 + dz)
        astas.append(tubo([base, base + V(0, 0.2, 0)], 0.004, 6, tapas_redondas=False))
        plumas.append(caja(base + V(0, 0.17, 0), (0.002, 0.06, 0.022), rot=(0, i * 35, 0)))
    esc.pieza("Flechas", unir(*astas), madera, "Torso", 'plano')
    esc.pieza("Plumas", unir(*plumas), pluma, "Torso", 'plano')
    esc.pieza("Bandolera del carcaj", tubo([(-0.17, 1.47, 0.08), (0.0, 1.3, 0.135), (0.16, 1.1, 0.09), (0.17, 1.05, -0.05)], 0.02, 8, (1, 0.35)), cuero_osc, "Torso", 'suave')
    esc.pieza("Cuchillo", unir(caja((-0.16, 0.92, 0.05), (0.025, 0.14, 0.01), rot=(0, 0, 8))), acero, "Cadera", 'duro')


def paladin(esc):
    w = 1.08
    blanco = esc.material("acero_blanco", (0.88, 0.87, 0.83), 0.85, 0.28)
    gris = esc.material("acero_gris", (0.45, 0.45, 0.47), 0.9, 0.35)
    oro = esc.material("oro", (0.95, 0.75, 0.3), 1.0, 0.25)
    malla = esc.material("hierro_malla", (0.35, 0.35, 0.37), 0.9, 0.5)
    cuero = esc.material("cuero", (0.22, 0.13, 0.07), 0, 0.6)
    capa = esc.material("tela_carmesi", (0.45, 0.06, 0.07), 0, 0.85)
    sol = esc.material("emisivo_oro", (1.0, 0.78, 0.35), 0, 0.3, emision=3.0)
    oscuro = esc.material("cuero_rendija", (0.02, 0.02, 0.02), 0, 0.9)

    esqueleto(esc, w)
    esc.articulacion("Capa", "Torso", (0, 1.56, -0.15))
    cuerpo_base(esc, w, malla, gris, gris)

    # Coraza: peto abombado, espaldar y lamas abdominales.
    esc.pieza("Peto", elipsoide((0, 1.37, 0.015), (0.205 * w, 0.2, 0.15), 28, 14, recorte_inferior=-0.7, recorte_superior=0.85), blanco, "Torso", 'duro')
    esc.pieza("Ribete del peto", toro((0, 1.535, 0.0), 0.16, 0.012, escala=(1.1, 1, 0.85)), oro, "Torso", 'suave')
    esc.pieza("Lamas", unir(*[toro((0, 1.21 - i * 0.055, 0.0), 0.162 - i * 0.004, 0.028, escala=(1.06, 1.0, 0.76), seg=28, seg_menor=8) for i in range(3)]), blanco, "Torso", 'duro')
    esc.pieza("Emblema solar", unir(cilindro((0, 1.4, 0.163), 0.055, 0.012, eje='z', seg=24),
                                    *[caja(V(0, 1.4, 0.165) + V(math.cos(a) * 0.075, math.sin(a) * 0.075, 0), (0.012, 0.03, 0.008), rot=(0, 0, math.degrees(a) - 90))
                                      for a in [i * math.pi / 4 for i in range(8)]]), sol, "Torso", 'duro')
    esc.pieza("Faldar", tubo([(0, 1.02, 0), (0, 0.93, 0), (0, 0.83, 0)], [0.175, 0.205, 0.225], 24, (1.06, 0.8), tapas_redondas=False), blanco, "Cadera", 'liso')
    esc.pieza("Cinturon", unir(toro((0, 1.02, 0), 0.175, 0.018, escala=(1.06, 1.2, 0.8)), caja((0, 1.02, 0.14), (0.06, 0.05, 0.015))), cuero, "Cadera", 'suave')
    esc.pieza("Ribete del faldar", toro((0, 0.83, 0), 0.225, 0.009, escala=(1.06, 1, 0.8)), oro, "Cadera", 'suave')

    # Yelmo cerrado con rendija, respiraderos y cresta dorada.
    esc.pieza("Gola", tubo([(0, 1.5, 0), (0, 1.6, 0)], [0.1, 0.075], 20), blanco, "Cabeza", 'duro')
    esc.pieza("Yelmo", unir(tubo([(0, 1.64, 0), (0, 1.75, 0.005), (0, 1.84, 0.0)], [0.115, 0.12, 0.112], 24, (0.95, 1.05)),
                            elipsoide((0, 1.85, 0), (0.107, 0.075, 0.118), 24, 8, recorte_inferior=0)), blanco, "Cabeza", 'duro')
    esc.pieza("Rendija", caja((0, 1.775, 0.122), (0.15, 0.014, 0.02)), oscuro, "Cabeza", 'plano')
    esc.pieza("Respiraderos", unir(*[caja((0.03 + 0.016 * i, 1.7, 0.115), (0.006, 0.03, 0.02)) for i in range(4)]), oscuro, "Cabeza", 'plano')
    esc.pieza("Cresta", unir(tubo(curva_bezier((0, 1.86, 0.11), (0, 1.97, 0.0), (0, 1.85, -0.12), 10), 0.012, 8, (0.5, 1.4)),
                             caja((0, 1.775, 0.12), (0.012, 0.14, 0.02))), oro, "Cabeza", 'suave')

    for l, s in LADOS:
        x = 0.23 * w * s
        hombrera_placas(esc, x, s, blanco, l, capas=3, tam=1.15, m_ribete=oro)
        brazal(esc, x, blanco, l)
        esc.pieza(f"Codal_{l}", elipsoide((x, 1.21, -0.03), (0.055, 0.055, 0.05), 16, 8), blanco, f"Antebrazo_{l}", 'duro')
        esc.pieza(f"Manopla_{l}", caja((x, 0.9, 0.0), (0.05, 0.07, 0.095), conicidad=-0.1), blanco, f"Mano_{l}", 'duro')
        xm = 0.11 * w * s
        esc.pieza(f"Quijote_{l}", elipsoide((xm, 0.72, 0.03), (0.09, 0.2, 0.08), 18, 10, recorte_inferior=-0.9, recorte_superior=0.9), blanco, f"Muslo_{l}", 'duro')
        rodillera(esc, xm, blanco, l, 1.1)
        greba(esc, xm, blanco, l)
        esc.pieza(f"Escarpe_{l}", unir(*[caja((xm, 0.05 + 0.01 * i, 0.06 + 0.045 * i), (0.105, 0.05, 0.06), rot=(-10 * i, 0, 0)) for i in range(3)]), blanco, f"Pie_{l}", 'duro')

    # Capa larga colgando de los hombros (pivote propio para el balanceo procedural).
    filas = []
    for i in range(9):
        f = i / 8
        y = 1.55 - f * 1.0
        ancho = 0.25 + 0.12 * f
        fila = []
        for k in range(13):
            u = -1 + 2 * k / 12
            z = -0.15 - 0.05 * f - 0.06 * (1 - u * u) - 0.015 * math.sin(u * 6 + f * 3)
            fila.append(V(u * ancho, y, z))
        grosor = [V(p.x, p.y, p.z + 0.012) for p in reversed(fila)]
        filas.append(fila + grosor)
    esc.pieza("Capa clara", loft(filas), capa, "Capa", 'liso')


def bastion(esc):
    w = 1.18
    hierro = esc.material("hierro_oscuro", (0.16, 0.16, 0.18), 0.9, 0.45)
    malla = esc.material("hierro_malla", (0.3, 0.3, 0.32), 0.9, 0.55)
    tela = esc.material("tela_beige", (0.76, 0.72, 0.62), 0, 0.9)
    bronce = esc.material("laton_bronce", (0.45, 0.3, 0.18), 1.0, 0.35)
    cuero = esc.material("cuero", (0.2, 0.12, 0.07), 0, 0.6)
    barba = esc.material("tela_barba", (0.28, 0.2, 0.14), 0, 1.0)
    oscuro = esc.material("cuero_rendija", (0.02, 0.02, 0.02), 0, 0.9)

    esqueleto(esc, w)
    cuerpo_base(esc, w, malla, hierro, hierro, ancho_pierna=1.12)

    esc.pieza("Coraza pesada", unir(elipsoide((0, 1.37, 0.02), (0.215 * w, 0.21, 0.165), 28, 14, recorte_inferior=-0.75),
                                    tubo([(0, 1.05, 0), (0, 1.2, 0.01)], [0.18 * w, 0.2 * w], 24, (1, 0.78))), hierro, "Torso", 'duro')
    esc.pieza("Remaches", unir(*[elipsoide((math.sin(a) * 0.2 * w, 1.3 + 0.12 * math.cos(a * 2), 0.14 * math.cos(a) + 0.02), (0.012, 0.012, 0.012), 8, 4)
                                 for a in [-1.0 + i * 0.25 for i in range(9)]]), bronce, "Torso", 'plano')
    esc.pieza("Gola alta", tubo([(0, 1.48, 0), (0, 1.62, 0.01)], [0.16, 0.1], 24, (1, 0.9)), hierro, "Torso", 'duro')
    esc.pieza("Correa cruzada", tubo([(-0.24, 1.5, 0.05), (0, 1.32, 0.19), (0.22, 1.05, 0.12)], 0.03, 8, (1, 0.3)), cuero, "Torso", 'suave')
    esc.pieza("Tabardo", unir(caja((0, 0.85, 0.17), (0.3, 0.5, 0.02), conicidad=-0.15), caja((0, 0.85, -0.16), (0.32, 0.5, 0.02), conicidad=-0.15)), tela, "Cadera", 'liso')
    esc.pieza("Faldon claro", tubo([(0, 1.03, 0), (0, 0.9, 0), (0, 0.76, 0)], [0.19 * w, 0.23 * w, 0.25 * w], 24, (1, 0.78), tapas_redondas=False), malla, "Cadera", 'liso')
    esc.pieza("Cinturon", unir(toro((0, 1.03, 0), 0.2 * w, 0.025, escala=(1, 1.3, 0.8)), caja((0, 1.03, 0.19), (0.08, 0.07, 0.02))), cuero, "Cadera", 'suave')

    # Yelmo de cubo con visera en T, barba poblada asomando.
    esc.pieza("Yelmo", unir(tubo([(0, 1.63, 0), (0, 1.76, 0), (0, 1.86, 0)], [0.13, 0.135, 0.125], 24, (1, 1.05)),
                            elipsoide((0, 1.865, 0), (0.125, 0.05, 0.13), 24, 6, recorte_inferior=0)), hierro, "Cabeza", 'duro')
    esc.pieza("Visera en T", unir(caja((0, 1.78, 0.134), (0.16, 0.018, 0.02)), caja((0, 1.72, 0.134), (0.035, 0.11, 0.02))), oscuro, "Cabeza", 'plano')
    esc.pieza("Refuerzo del yelmo", unir(toro((0, 1.82, 0), 0.13, 0.012, escala=(1, 1, 1.05)), tubo([(0, 1.88, 0.12), (0, 1.92, 0), (0, 1.88, -0.12)], 0.014, 8)), bronce, "Cabeza", 'suave')
    esc.pieza("Barba", unir(cono((0, 1.67, 0.11), (0, 1.5, 0.17), 0.085, 12, (1.2, 0.7)), elipsoide((0, 1.66, 0.11), (0.1, 0.05, 0.05))), barba, "Cabeza", 'organico',
              desplazamiento=0.01, escala_ruido=0.02)

    for l, s in LADOS:
        x = 0.23 * w * s
        hombrera_placas(esc, x, s, hierro, l, capas=4, tam=1.45, m_ribete=bronce, y=1.53)
        esc.pieza(f"Remaches hombro_{l}", unir(*[elipsoide((x + s * (0.02 + 0.1 * math.cos(a)), 1.6, 0.13 * math.sin(a)), (0.012, 0.012, 0.012), 8, 4)
                                                  for a in [0.3 + i * 0.6 for i in range(5)]]), bronce, f"Brazo_{l}", 'plano')
        brazal(esc, x, hierro, l, r0=0.065, r1=0.055)
        esc.pieza(f"Manopla_{l}", caja((x, 0.9, 0.0), (0.06, 0.08, 0.105)), hierro, f"Mano_{l}", 'duro')
        xm = 0.11 * w * s
        rodillera(esc, xm, hierro, l, 1.3)
        greba(esc, xm, hierro, l)
        bota(esc, xm, s, hierro, l + "_ferrada", alta=0.12, robusta=1.15)


def tirador(esc):
    w = 1.0
    azul = esc.material("tela_azul_marino", (0.12, 0.16, 0.3), 0, 0.8)
    azul_osc = esc.material("tela_azul_oscuro", (0.07, 0.09, 0.17), 0, 0.85)
    cuero = esc.material("cuero", (0.35, 0.22, 0.13), 0, 0.55)
    cuero_osc = esc.material("cuero_oscuro", (0.16, 0.1, 0.06), 0, 0.6)
    laton = esc.material("laton", (0.78, 0.62, 0.3), 1.0, 0.3)
    camisa = esc.material("tela_lino", (0.75, 0.72, 0.66), 0, 0.9)
    lente = esc.material("emisivo_ambar", (1.0, 0.7, 0.3), 0, 0.2, emision=1.5)

    esqueleto(esc, w)
    cuerpo_base(esc, w, azul_osc, cuero_osc, cuero)

    # Abrigo largo con solapas, botones y faldones (tres paneles para que las piernas se muevan).
    esc.pieza("Abrigo", unir(tubo([(0, 1.0, 0), (0, 1.2, 0), (0, 1.42, 0.0)], [0.162, 0.168, 0.19], 22, (1, 0.74)),
                             elipsoide((0, 1.4, 0.0), (0.2, 0.15, 0.14))), azul, "Torso", 'suave')
    esc.pieza("Camisa", caja((0, 1.38, 0.13), (0.07, 0.2, 0.02), conicidad=0.4), camisa, "Torso", 'suave')
    esc.pieza("Solapas", unir(caja((-0.065, 1.36, 0.13), (0.06, 0.24, 0.02), rot=(0, 0, -12)), caja((0.065, 1.36, 0.13), (0.06, 0.24, 0.02), rot=(0, 0, 12))), azul_osc, "Torso", 'duro')
    esc.pieza("Botones", unir(*[elipsoide((sx * 0.085, 1.12 + 0.07 * i, 0.128), (0.011, 0.011, 0.006), 10, 5) for i in range(3) for sx in (-1, 1)]), laton, "Torso", 'liso')
    esc.pieza("Faldones del abrigo", unir(caja((0, 0.73, -0.15), (0.32, 0.56, 0.025), rot=(6, 0, 0), conicidad=-0.25),
                                          caja((-0.17, 0.75, 0.0), (0.025, 0.52, 0.26), rot=(0, 0, -5), conicidad=-0.15),
                                          caja((0.17, 0.75, 0.0), (0.025, 0.52, 0.26), rot=(0, 0, 5), conicidad=-0.15)), azul, "Cadera", 'liso')
    esc.pieza("Cinturon", unir(toro((0, 1.0, 0), 0.165, 0.02, escala=(1, 1.2, 0.76)), caja((0, 1.0, 0.128), (0.05, 0.045, 0.015))), cuero, "Cadera", 'suave')
    esc.pieza("Bandolera", tubo(curva_bezier((-0.2, 1.48, 0.04), (0.0, 1.28, 0.2), (0.17, 1.02, 0.1), 10), 0.022, 8, (1, 0.3)), cuero, "Torso", 'suave')
    cartuchos = []
    for i in range(7):
        f = 0.15 + i * 0.1
        p = curva_bezier((-0.2, 1.48, 0.04), (0.0, 1.28, 0.2), (0.17, 1.02, 0.1), 20)[int(round(f * 20))] + V(0, 0, 0.012)
        cartuchos.append(cilindro(p, 0.008, 0.045, seg=8))
    esc.pieza("Cartuchos", unir(*cartuchos), laton, "Torso", 'plano')

    # Sombrero de ala ancha con gafas de latón y pañuelo.
    esc.pieza("Pasamontanas", elipsoide((0, 1.77, 0.01), (0.088, 0.112, 0.098)), cuero_osc, "Cabeza", 'suave')
    esc.pieza("Panuelo", unir(elipsoide((0, 1.71, 0.04), (0.1, 0.065, 0.085), 18, 8), cono((0, 1.68, -0.05), (0, 1.56, -0.12), 0.05)), azul_osc, "Cabeza", 'suave')
    esc.pieza("Ala del sombrero", cilindro((0, 1.85, 0), 0.215, 0.012, seg=36), cuero_osc, "Cabeza", 'suave')
    esc.pieza("Copa del sombrero", unir(cilindro((0, 1.91, 0), 0.1, 0.11, seg=28, radio_superior=0.088)), cuero_osc, "Cabeza", 'suave')
    esc.pieza("Cinta", cilindro((0, 1.875, 0), 0.102, 0.022, seg=28), cuero, "Cabeza", 'plano')
    esc.pieza("Gafas", unir(toro((-0.04, 1.885, 0.09), 0.026, 0.008, eje='z', seg=18, seg_menor=6), toro((0.04, 1.885, 0.09), 0.026, 0.008, eje='z', seg=18, seg_menor=6)), laton, "Cabeza", 'suave')
    esc.pieza("Lentes", unir(cilindro((-0.04, 1.885, 0.09), 0.022, 0.006, eje='z', seg=16), cilindro((0.04, 1.885, 0.09), 0.022, 0.006, eje='z', seg=16)), lente, "Cabeza", 'plano')
    esc.pieza("Gafas puestas", unir(toro((-0.035, 1.785, 0.09), 0.024, 0.009, eje='z', seg=18, seg_menor=6), toro((0.035, 1.785, 0.09), 0.024, 0.009, eje='z', seg=18, seg_menor=6),
                                    tubo([(-0.09, 1.785, 0.0), (-0.06, 1.785, 0.075)], 0.006, 6), tubo([(0.09, 1.785, 0.0), (0.06, 1.785, 0.075)], 0.006, 6)), laton, "Cabeza", 'suave')
    esc.pieza("Lentes puestas", unir(cilindro((-0.035, 1.785, 0.092), 0.02, 0.008, eje='z', seg=16), cilindro((0.035, 1.785, 0.092), 0.02, 0.008, eje='z', seg=16)), lente, "Cabeza", 'plano')

    for l, s in LADOS:
        x = 0.23 * w * s
        esc.pieza(f"Manga_{l}", miembro((x, 1.5, 0), (x, 1.0, 0), 0.064, 0.056, tramos=5), azul, f"Brazo_{l}", 'suave')
        esc.pieza(f"Puno_{l}", toro((x, 1.0, 0), 0.055, 0.014, seg=20, seg_menor=6), azul_osc, f"Antebrazo_{l}", 'suave')
        esc.pieza(f"Hombrera_{l}", elipsoide((x + 0.015 * s, 1.5, 0), (0.08, 0.04, 0.08), 18, 6, recorte_inferior=-0.2), azul_osc, f"Brazo_{l}", 'suave')
        xm = 0.11 * w * s
        esc.pieza(f"Hebillas_{l}", unir(*[toro((xm, y, 0), 0.053, 0.006, seg=18, seg_menor=6) for y in (0.2, 0.3)]), laton, f"Pie_{l}", 'plano')
    # Mangas largas: el antebrazo sigue al brazo (pieza propia en el antebrazo).
    for l, s in LADOS:
        x = 0.23 * w * s
        esc.pieza(f"Manga baja_{l}", miembro((x, 1.21, 0), (x, 1.0, 0), 0.058, 0.054, tramos=3), azul, f"Antebrazo_{l}", 'suave')


def ciborg(esc):
    w = 1.02
    metal = esc.material("metal_placas", (0.62, 0.64, 0.68), 0.95, 0.3)
    oscuro = esc.material("acero_oscuro", (0.2, 0.2, 0.23), 0.8, 0.45)
    goma = esc.material("cuero_sintetico", (0.08, 0.08, 0.09), 0, 0.7)
    magenta = esc.material("emisivo_magenta", (1.0, 0.25, 0.7), 0, 0.2, emision=4.0)

    esqueleto(esc, w)
    cuerpo_base(esc, w, goma, oscuro, oscuro)

    # Torso segmentado: placas pectorales, costillas expuestas y núcleo magenta.
    esc.pieza("Placas pectorales", unir(elipsoide((-0.085, 1.4, 0.06), (0.1, 0.12, 0.09), 18, 10, recorte_inferior=-0.6),
                                        elipsoide((0.085, 1.4, 0.06), (0.1, 0.12, 0.09), 18, 10, recorte_inferior=-0.6)), metal, "Torso", 'duro')
    esc.pieza("Costillas", unir(*[toro((0, 1.12 + i * 0.045, 0.0), 0.15, 0.011, escala=(1.02, 1, 0.75), seg=24, seg_menor=6) for i in range(4)]), oscuro, "Torso", 'suave')
    esc.pieza("Nucleo magenta", elipsoide((0, 1.4, 0.15), (0.045, 0.045, 0.03), 16, 8), magenta, "Torso", 'suave')
    esc.pieza("Anillo del nucleo", toro((0, 1.4, 0.142), 0.055, 0.012, eje='z', seg=24, seg_menor=8), metal, "Torso", 'suave')
    esc.pieza("Espina", unir(*[caja((0, 1.1 + i * 0.09, -0.14), (0.06, 0.06, 0.05), rot=(20, 0, 0)) for i in range(5)]), metal, "Torso", 'duro')
    esc.pieza("Conductos", unir(tubo(curva_bezier((-0.03, 1.5, -0.12), (-0.12, 1.3, -0.17), (-0.1, 1.05, -0.12), 8), 0.012, 8),
                                tubo(curva_bezier((0.03, 1.5, -0.12), (0.12, 1.3, -0.17), (0.1, 1.05, -0.12), 8), 0.012, 8)), magenta, "Torso", 'plano')
    esc.pieza("Placa pelvica", elipsoide((0, 0.97, 0.03), (0.17, 0.1, 0.12), 20, 8, recorte_inferior=-0.5), metal, "Cadera", 'duro')

    # Cabeza: casco liso con un único lente óptico y antena.
    esc.pieza("Craneo", unir(elipsoide((0, 1.78, 0.0), (0.1, 0.125, 0.115)), elipsoide((0, 1.7, 0.04), (0.085, 0.055, 0.085), 16, 8)), metal, "Cabeza", 'duro')
    esc.pieza("Visor", elipsoide((0, 1.775, 0.06), (0.085, 0.035, 0.07), 20, 8), oscuro, "Cabeza", 'suave')
    esc.pieza("Lente optico", elipsoide((-0.04, 1.78, 0.12), (0.022, 0.022, 0.012), 14, 6), magenta, "Cabeza", 'suave')
    esc.pieza("Antena", unir(tubo([(0.08, 1.82, -0.04), (0.11, 1.98, -0.08)], 0.006, 6), elipsoide((0.11, 1.98, -0.08), (0.012, 0.012, 0.012), 8, 4)), oscuro, "Cabeza", 'suave')

    for l, s in LADOS:
        x = 0.23 * w * s
        esc.pieza(f"Hombro_{l}", elipsoide((x + 0.02 * s, 1.5, 0), (0.09, 0.07, 0.085), 18, 8, recorte_inferior=-0.3), metal, f"Brazo_{l}", 'duro')
        esc.pieza(f"Placa brazo_{l}", elipsoide((x + 0.02 * s, 1.35, 0.0), (0.05, 0.1, 0.055), 14, 8, recorte_inferior=-0.9, recorte_superior=0.9), metal, f"Brazo_{l}", 'duro')
        esc.pieza(f"Placa antebrazo_{l}", elipsoide((x, 1.08, 0.01), (0.055, 0.11, 0.055), 14, 8, recorte_inferior=-0.9, recorte_superior=0.9), metal, f"Antebrazo_{l}", 'duro')
        esc.pieza(f"Linea de energia_{l}", tubo([(x + 0.05 * s, 1.17, 0.0), (x + 0.045 * s, 1.0, 0.0)], 0.007, 6), magenta, f"Antebrazo_{l}", 'plano')
        esc.pieza(f"Piston codo_{l}", cilindro((x - 0.03 * s, 1.21, -0.04), 0.012, 0.16, seg=10), oscuro, f"Antebrazo_{l}", 'suave')
        xm = 0.11 * w * s
        esc.pieza(f"Placa muslo_{l}", elipsoide((xm, 0.72, 0.035), (0.08, 0.19, 0.07), 16, 8, recorte_inferior=-0.9, recorte_superior=0.9), metal, f"Muslo_{l}", 'duro')
        esc.pieza(f"Linea muslo_{l}", tubo([(xm + 0.075 * s, 0.85, 0.0), (xm + 0.065 * s, 0.6, 0.0)], 0.007, 6), magenta, f"Muslo_{l}", 'plano')
        rodillera(esc, xm, metal, l)
        greba(esc, xm, metal, l)
        esc.pieza(f"Piston rodilla_{l}", cilindro((xm, 0.44, -0.06), 0.012, 0.18, seg=10), oscuro, f"Pierna_{l}", 'suave')


ARQUETIPOS = {
    "Cazador": cazador,
    "Paladin": paladin,
    "Bastion": bastion,
    "Tirador": tirador,
    "Ciborg": ciborg,
}


def construir(nombre):
    esc = Escena(f"Humano_{nombre}")
    ARQUETIPOS[nombre](esc)
    return esc

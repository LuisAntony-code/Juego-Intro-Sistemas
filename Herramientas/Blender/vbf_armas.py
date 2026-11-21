"""
Armas modulares de Voidform: Bio-Forge.

Convención (la misma que ConstructorModelosArmas.cs): la EMPUÑADURA está en el origen y el arma apunta hacia +Z
de Unity (hoja, cañón o flecha); +Y es la parte de arriba (mira del rifle). En la mano de un personaje Unity la
gira 90° en X para que siga al antebrazo; en primera persona se coloca tal cual delante de la cámara.
"""

import math

from vbf_geometria import V, Escena, caja, cilindro, cono, curva_bezier, elipsoide, loft, toro, tubo, unir


def hoja(z0, z1, ancho0, ancho1, grosor, curva=0.0, estaciones=10):
    """Hoja con sección romboidal hexagonal (filo a los lados, arista central) que termina en punta."""
    anillos = []
    for i in range(estaciones + 1):
        f = i / estaciones
        z = z0 + (z1 - z0) * f
        w = ancho0 + (ancho1 - ancho0) * f
        if f > 0.82:
            w *= max(0.02, 1 - (f - 0.82) / 0.18)
        t = grosor * (1 - 0.4 * f)
        dy = curva * f * f
        anillos.append([V(w, dy, z), V(0.35 * w, dy + t, z), V(-0.35 * w, dy + t, z), V(-w, dy, z), V(-0.35 * w, dy - t, z), V(0.35 * w, dy - t, z)])
    return loft(anillos)


def empunadura(z0, z1, radio, vueltas=7):
    """Empuñadura forrada de cuero con relieve de las vueltas de la tira."""
    pts, rs = [], []
    n = vueltas * 4
    for i in range(n + 1):
        f = i / n
        pts.append(V(0, 0, z0 + (z1 - z0) * f))
        rs.append(radio * (1 + 0.08 * math.cos(f * vueltas * 2 * math.pi)))
    return tubo(pts, rs, 12)


def arco_compuesto(esc):
    madera = esc.material("madera_laminada", (0.45, 0.3, 0.18), 0, 0.55)
    cuero = esc.material("cuero", (0.25, 0.15, 0.09), 0, 0.6)
    acero = esc.material("acero", (0.75, 0.77, 0.8), 0.9, 0.3)
    cuerda = esc.material("tela_cuerda", (0.85, 0.82, 0.72), 0, 0.8)
    pluma = esc.material("tela_pluma", (0.75, 0.15, 0.1), 0, 0.8)

    # Cuerpo central (riser) con repisa para la flecha y palas recurvadas.
    esc.pieza("Riser", unir(tubo([(0, -0.17, -0.01), (0, -0.06, 0.012), (0, 0.06, 0.012), (0, 0.17, -0.01)], [0.022, 0.026, 0.026, 0.022], 12, (0.8, 1.3)),
                            caja((0.012, 0.075, 0.01), (0.02, 0.02, 0.04))), madera, None, 'suave')
    palas = []
    for s in (-1, 1):
        puntos = curva_bezier((0, 0.16 * s, -0.01), (0, 0.42 * s, -0.04), (0, 0.6 * s, -0.17), 10)
        puntos += [V(0, 0.65 * s, -0.15), V(0, 0.68 * s, -0.1)]
        palas.append(tubo(puntos, [0.021 - 0.0011 * i for i in range(len(puntos))], 10, (1.4, 0.6)))
    esc.pieza("Palas", unir(*palas), madera, None, 'suave')
    esc.pieza("Puntas", unir(elipsoide((0, 0.665, -0.13), (0.012, 0.02, 0.012), 10, 5), elipsoide((0, -0.665, -0.13), (0.012, 0.02, 0.012), 10, 5)), acero, None, 'suave')
    esc.pieza("Empunadura", tubo([(0, -0.06, -0.012), (0, 0.06, -0.012)], 0.027, 14, (0.85, 1)), cuero, None, 'suave')
    esc.pieza("Cuerda", tubo([(0, 0.655, -0.14), (0, 0.0, -0.165), (0, -0.655, -0.14)], 0.0025, 6, tapas_redondas=False), cuerda, None, 'plano')
    # Flecha encordada.
    esc.pieza("Asta", tubo([(0.015, 0.075, -0.165), (0.015, 0.075, 0.5)], 0.004, 8, tapas_redondas=False), madera, None, 'plano')
    esc.pieza("Punta de flecha", cono((0.015, 0.075, 0.5), (0.015, 0.075, 0.58), 0.011, 8, (1, 0.25)), acero, None, 'plano')
    esc.pieza("Plumas", unir(*[caja(V(0.015, 0.075, -0.11) + V(0.009 * math.cos(a), 0.009 * math.sin(a), 0), (0.002, 0.016, 0.07), rot=(0, 0, math.degrees(a)))
                               for a in (0.0, 2.09, 4.19)]), pluma, None, 'plano')


def mandoble(esc):
    acero = esc.material("acero", (0.8, 0.82, 0.85), 0.95, 0.22)
    oro = esc.material("oro", (0.95, 0.75, 0.3), 1.0, 0.25)
    cuero = esc.material("cuero", (0.22, 0.12, 0.07), 0, 0.6)
    runa = esc.material("emisivo_oro", (1.0, 0.8, 0.4), 0, 0.3, emision=2.5)

    esc.pieza("Empunadura", empunadura(-0.11, 0.2, 0.017, 9), cuero, None, 'suave')
    esc.pieza("Pomo", unir(elipsoide((0, 0, -0.14), (0.032, 0.032, 0.028), 16, 8), cono((0, 0, -0.165), (0, 0, -0.19), 0.012, 8)), oro, None, 'suave')
    esc.pieza("Guarda", unir(tubo(curva_bezier((-0.16, 0.0, 0.25), (0, 0.0, 0.21), (0.16, 0.0, 0.25), 8), [0.014, 0.016, 0.018, 0.02, 0.022, 0.02, 0.018, 0.016, 0.014], 10),
                             elipsoide((-0.165, 0, 0.255), (0.02, 0.02, 0.02), 10, 5), elipsoide((0.165, 0, 0.255), (0.02, 0.02, 0.02), 10, 5),
                             elipsoide((0, 0, 0.22), (0.035, 0.022, 0.025), 12, 6)), oro, None, 'suave')
    esc.pieza("Hoja", hoja(0.235, 1.4, 0.03, 0.022, 0.006), acero, None, 'duro')
    esc.pieza("Runas", unir(*[caja((0, 0.0045, 0.32 + i * 0.07), (0.006, 0.002, 0.03)) for i in range(5)]), runa, None, 'plano')


def maza(esc):
    madera = esc.material("madera", (0.4, 0.27, 0.16), 0, 0.6)
    cuero = esc.material("cuero", (0.2, 0.11, 0.06), 0, 0.6)
    hierro = esc.material("hierro", (0.35, 0.35, 0.37), 0.9, 0.4)
    laton = esc.material("laton", (0.75, 0.58, 0.28), 1.0, 0.3)

    esc.pieza("Asta", tubo([(0, 0, -0.14), (0, 0, 0.5)], 0.019, 12), madera, None, 'suave')
    esc.pieza("Empunadura", empunadura(-0.11, 0.12, 0.022, 8), cuero, None, 'suave')
    esc.pieza("Virolas", unir(toro((0, 0, -0.13), 0.022, 0.008, eje='z', seg=16, seg_menor=6), toro((0, 0, 0.14), 0.022, 0.008, eje='z', seg=16, seg_menor=6),
                              toro((0, 0, 0.47), 0.026, 0.01, eje='z', seg=16, seg_menor=6)), laton, None, 'suave')
    cabeza = [tubo([(0, 0, 0.48), (0, 0, 0.7)], [0.04, 0.04], 16)]
    for i in range(7):
        a = 2 * math.pi * i / 7
        d = V(math.cos(a), math.sin(a), 0)
        aleta = caja(d * 0.065 + V(0, 0, 0.59), (0.012, 0.1, 0.2), rot=(0, 0, math.degrees(a) + 90), conicidad=0.0)
        cabeza.append(aleta)
    cabeza.append(cono((0, 0, 0.7), (0, 0, 0.8), 0.03, 10))
    esc.pieza("Cabeza de maza", unir(*cabeza), hierro, None, 'duro')


def escudo(esc):
    acero = esc.material("acero_blanco", (0.82, 0.82, 0.8), 0.9, 0.3)
    oro = esc.material("oro", (0.95, 0.75, 0.3), 1.0, 0.25)
    cuero = esc.material("cuero", (0.2, 0.11, 0.06), 0, 0.6)

    esc.pieza("Escudo", elipsoide((0, 0, 0.06), (0.28, 0.36, 0.045), 40, 20), acero, None, 'suave')
    # Ribete ovalado: el toro se estira antes de orientarlo para seguir el óvalo (0,28 x 0,36 m).
    esc.pieza("Ribete", toro((0, 0, 0.06), 0.285, 0.016, eje='z', seg=48, seg_menor=8, escala=(1.0, 1.0, 1.265)), oro, None, 'suave')
    esc.pieza("Umbo", elipsoide((0, 0, 0.1), (0.07, 0.07, 0.04), 20, 10), oro, None, 'suave')
    esc.pieza("Rayos", unir(*[caja(V(math.cos(a) * 0.15, math.sin(a) * 0.19, 0.1), (0.018, 0.12, 0.01), rot=(0, 0, math.degrees(a) - 90))
                              for a in [i * math.pi / 4 for i in range(8)]]), oro, None, 'duro')
    esc.pieza("Asa", unir(tubo([(0, -0.08, 0.015), (0, -0.08, -0.03), (0, 0.08, -0.03), (0, 0.08, 0.015)], 0.012, 8),
                          caja((0, 0, 0.02), (0.06, 0.25, 0.01))), cuero, None, 'suave')


def rifle_largo(esc):
    madera = esc.material("madera", (0.42, 0.26, 0.14), 0, 0.45)
    acero = esc.material("acero_pavonado", (0.18, 0.19, 0.21), 0.9, 0.35)
    laton = esc.material("laton", (0.78, 0.6, 0.28), 1.0, 0.25)
    lente = esc.material("emisivo_lente", (0.55, 0.8, 1.0), 0, 0.1, emision=1.2)

    esc.pieza("Culata", unir(caja((0, -0.045, -0.3), (0.045, 0.13, 0.3), conicidad=0.15), caja((0, -0.02, -0.09), (0.04, 0.07, 0.16)),
                             caja((0, -0.09, -0.05), (0.035, 0.12, 0.05), rot=(-15, 0, 0))), madera, None, 'duro')
    esc.pieza("Cantonera", caja((0, -0.05, -0.455), (0.047, 0.135, 0.012)), acero, None, 'duro')
    esc.pieza("Guardamanos", caja((0, -0.02, 0.42), (0.042, 0.045, 0.34), conicidad=0.0), madera, None, 'duro')
    esc.pieza("Cajon de mecanismos", unir(caja((0, 0.005, 0.1), (0.05, 0.06, 0.32)), caja((0, -0.045, 0.06), (0.03, 0.06, 0.05))), acero, None, 'duro')
    esc.pieza("Canon", unir(tubo([(0, 0.012, 0.25), (0, 0.012, 0.95)], [0.014, 0.011], 14, tapas_redondas=False),
                            tubo([(0, 0.012, 0.93), (0, 0.012, 0.99)], 0.016, 14)), acero, None, 'suave')
    esc.pieza("Cerrojo", unir(tubo([(0, 0.02, 0.02), (0.05, 0.02, 0.0)], 0.006, 8), elipsoide((0.055, 0.02, 0.0), (0.012, 0.012, 0.012), 10, 5)), acero, None, 'suave')
    esc.pieza("Guardamonte", toro((0, -0.075, 0.05), 0.03, 0.005, eje='x', seg=18, seg_menor=6), laton, None, 'suave')
    esc.pieza("Mira telescopica", unir(tubo([(0, 0.09, -0.04), (0, 0.09, 0.3)], 0.017, 16, tapas_redondas=False),
                                       tubo([(0, 0.09, -0.1), (0, 0.09, -0.04)], [0.024, 0.017], 16, tapas_redondas=False),
                                       tubo([(0, 0.09, 0.3), (0, 0.09, 0.37)], [0.017, 0.026], 16, tapas_redondas=False),
                                       caja((0, 0.06, 0.05), (0.015, 0.04, 0.02)), caja((0, 0.06, 0.22), (0.015, 0.04, 0.02))), laton, None, 'suave')
    esc.pieza("Lentes", unir(cilindro((0, 0.09, 0.371), 0.023, 0.004, eje='z', seg=16), cilindro((0, 0.09, -0.101), 0.021, 0.004, eje='z', seg=16)), lente, None, 'plano')


def arsenal_biomecanico(esc):
    acero = esc.material("acero", (0.7, 0.72, 0.76), 0.95, 0.25)
    oscuro = esc.material("acero_oscuro", (0.15, 0.15, 0.18), 0.85, 0.4)
    filo = esc.material("emisivo_magenta", (1.0, 0.25, 0.7), 0, 0.2, emision=4.0)

    esc.pieza("Funda del antebrazo", unir(caja((0, 0.0, -0.1), (0.1, 0.075, 0.3), conicidad=0.1),
                                          *[caja((0, 0.045, -0.2 + i * 0.07), (0.105, 0.02, 0.05)) for i in range(4)]), oscuro, None, 'duro')
    esc.pieza("Hoja retractil", hoja(0.03, 0.68, 0.035, 0.025, 0.006, curva=0.04), acero, None, 'duro')
    esc.pieza("Filo de energia", tubo([V(0.036 - 0.012 * f, 0.04 * f * f, 0.05 + 0.6 * f) for f in [i / 8 for i in range(9)]], 0.0035, 6), filo, None, 'plano')
    esc.pieza("Conductos", unir(tubo([(0.045, 0.03, -0.22), (0.05, 0.035, 0.0), (0.03, 0.02, 0.05)], 0.008, 6),
                                tubo([(-0.045, 0.03, -0.22), (-0.05, 0.035, 0.0), (-0.03, 0.02, 0.05)], 0.008, 6)), filo, None, 'plano')


def canon_pulso(esc):
    acero = esc.material("acero", (0.68, 0.7, 0.74), 0.95, 0.25)
    oscuro = esc.material("acero_oscuro", (0.15, 0.15, 0.18), 0.85, 0.4)
    energia = esc.material("emisivo_magenta", (1.0, 0.3, 0.75), 0, 0.2, emision=5.0)

    esc.pieza("Cuerpo del canon", unir(tubo([(0, 0, -0.02), (0, 0, 0.26)], [0.05, 0.045], 20), caja((0, -0.04, 0.08), (0.07, 0.04, 0.16))), oscuro, None, 'duro')
    esc.pieza("Bobinas", unir(*[toro((0, 0, 0.04 + i * 0.05), 0.052, 0.01, eje='z', seg=20, seg_menor=6) for i in range(5)]), acero, None, 'suave')
    esc.pieza("Boca", toro((0, 0, 0.27), 0.04, 0.012, eje='z', seg=20, seg_menor=8), energia, None, 'suave')
    esc.pieza("Nucleo", cilindro((0, 0, 0.255), 0.03, 0.01, eje='z', seg=16), energia, None, 'plano')


ARMAS = {
    "ArcoCompuesto": arco_compuesto,
    "Mandoble": mandoble,
    "MazaEscudo": maza,
    "Escudo": escudo,
    "RifleLargo": rifle_largo,
    "ArsenalBiomecanico": arsenal_biomecanico,
    "CanonPulso": canon_pulso,
}


def construir(nombre):
    esc = Escena(f"Arma_{nombre}")
    ARMAS[nombre](esc)
    return esc

"""
Los 5 linajes de guardianes elementales (colosos de ~2,7 m, mirando a +Z de Unity).

Esqueleto (idéntico a ConstructorModelosPersonajes.ConstruirGuardianPrimitivo):
    Cadera (0, 1.15, 0) ─ Torso (0, 1.25, 0) ─ Cabeza (0, 2.27, 0.05)
                         └ Brazo_I/D (∓0.68, 2.13, 0) ─ Antebrazo (…, 1.53) ─ Mano (…, 0.95)
    Cadera ─ Muslo_I/D (∓0.28, 1.10, 0) ─ Pierna (…, 0.55) ─ Pie (…, 0.03)

Postura de coloso: cabeza adelantada y hundida entre los hombros, antebrazos y puños enormes (golpes pesados
telegrafiados) y núcleo luminoso en el pecho, que es el PUNTO DÉBIL del diseño de combate (PuntoDebil.cs).
"""

import math
import random

from vbf_geometria import V, Escena, caja, cilindro, cono, curva_bezier, elipsoide, miembro, toro, tubo, unir

LADOS = (("I", -1), ("D", 1))


def esqueleto(esc):
    esc.articulacion("Cadera", None, (0, 1.15, 0))
    esc.articulacion("Torso", "Cadera", (0, 1.25, 0))
    esc.articulacion("Cabeza", "Torso", (0, 2.27, 0.05))
    for l, s in LADOS:
        esc.articulacion(f"Brazo_{l}", "Torso", (0.68 * s, 2.13, 0))
        esc.articulacion(f"Antebrazo_{l}", f"Brazo_{l}", (0.68 * s, 1.53, 0))
        esc.articulacion(f"Mano_{l}", f"Antebrazo_{l}", (0.68 * s, 0.95, 0))
        esc.articulacion(f"Muslo_{l}", "Cadera", (0.28 * s, 1.10, 0))
        esc.articulacion(f"Pierna_{l}", f"Muslo_{l}", (0.28 * s, 0.55, 0))
        esc.articulacion(f"Pie_{l}", f"Pierna_{l}", (0.28 * s, 0.03, 0))


def cuerpo_coloso(esc, piel, relieve=0.0, escala_relieve=0.25, tipo='CLOUDS', acabado='suave', hombros=1.0):
    """Masa corporal común. 'relieve' desplaza la superficie (corteza, roca, escoria)."""
    def P(nombre, bm, art, mat=None, d=None):
        esc.pieza(nombre, bm, mat or piel, art, acabado, desplazamiento=relieve if d is None else d,
                  escala_ruido=escala_relieve, tipo_ruido=tipo)

    P("Pelvis", elipsoide((0, 1.12, 0), (0.36, 0.2, 0.27), 28, 14), "Cadera")
    P("Abdomen", tubo([(0, 1.22, 0), (0, 1.4, 0.02), (0, 1.58, 0.04)], [0.3, 0.33, 0.4], 28, (1, 0.78)), "Torso")
    P("Pecho", elipsoide((0, 1.86, 0.05), (0.56, 0.42, 0.38), 32, 18), "Torso")
    P("Espalda", elipsoide((0, 2.02, -0.12), (0.5, 0.3, 0.32), 28, 14), "Torso")
    P("Cuello", tubo([(0, 2.1, 0.02), (0, 2.3, 0.1)], [0.2, 0.17], 20), "Cabeza")
    for l, s in LADOS:
        P(f"Hombro_{l}", elipsoide((0.66 * s, 2.13, 0), (0.29 * hombros, 0.25 * hombros, 0.28 * hombros), 28, 14), f"Brazo_{l}")
        P(f"Brazo_{l}", miembro((0.68 * s, 2.05, 0), (0.68 * s, 1.55, 0.0), 0.19, 0.16, 18, abultamiento=0.12), f"Brazo_{l}")
        P(f"Antebrazo_{l}", miembro((0.68 * s, 1.53, 0), (0.69 * s, 1.0, 0.03), 0.17, 0.2, 18, abultamiento=0.1), f"Antebrazo_{l}")
        P(f"Muslo_{l}", miembro((0.28 * s, 1.12, 0), (0.28 * s, 0.57, 0.02), 0.22, 0.17, 18, abultamiento=0.1), f"Muslo_{l}")
        P(f"Pierna_{l}", miembro((0.28 * s, 0.54, 0.02), (0.28 * s, 0.12, -0.01), 0.18, 0.15, 18, abultamiento=0.12), f"Pierna_{l}")


def puno(esc, l, s, material, acabado='suave', relieve=0.0, escala=0.12):
    """Puño colosal con nudillos marcados (zona del golpe básico)."""
    x = 0.68 * s
    partes = [elipsoide((x, 0.84, 0.06), (0.2, 0.19, 0.22), 24, 12)]
    for i in range(4):
        partes.append(elipsoide((x - 0.03 * s, 0.7, -0.07 + i * 0.08), (0.07, 0.065, 0.055), 12, 6))
    partes.append(elipsoide((x - 0.15 * s, 0.82, 0.18), (0.07, 0.1, 0.07), 12, 6))
    esc.pieza(f"Puno_{l}", unir(*partes), material, f"Mano_{l}", acabado, desplazamiento=relieve, escala_ruido=escala)


def pie_coloso(esc, l, s, material, acabado='suave', relieve=0.0, escala=0.12, dedos=3):
    x = 0.28 * s
    partes = [elipsoide((x, 0.1, 0.04), (0.2, 0.12, 0.26), 24, 10)]
    for i in range(dedos):
        a = (i - (dedos - 1) / 2) * 0.35
        partes.append(elipsoide((x + math.sin(a) * 0.12, 0.07, 0.25 + math.cos(a) * 0.05), (0.065, 0.06, 0.09), 12, 6))
    esc.pieza(f"Pie_{l}", unir(*partes), material, f"Pie_{l}", acabado, desplazamiento=relieve, escala_ruido=escala)


def nucleo(esc, material, pos=(0, 1.88, 0.42), radio=0.15, anillo=None):
    esc.pieza("Nucleo del pecho", elipsoide(pos, (radio, radio, radio * 0.6), 24, 12), material, "Torso", 'suave')
    if anillo is not None:
        esc.pieza("Engaste del nucleo", toro((pos[0], pos[1], pos[2] - 0.02), radio * 1.15, radio * 0.22, eje='z', seg=28, seg_menor=8), anillo, "Torso", 'suave')


def ojos(esc, material, separacion=0.085, y=2.5, z=0.3, r=0.035):
    esc.pieza("Ojos", unir(elipsoide((-separacion, y, z), (r, r * 0.7, r * 0.6), 12, 6), elipsoide((separacion, y, z), (r, r * 0.7, r * 0.6), 12, 6)), material, "Cabeza", 'suave')


def vetas(esc, nombre, articulacion, material, trazos, radio=0.014):
    """Líneas luminosas (savia, magma, corriente) que siguen la superficie."""
    esc.pieza(nombre, unir(*[tubo(curva_bezier(a, b, c, 8), radio, 6) for a, b, c in trazos]), material, articulacion, 'suave')


# ---------------------------------------------------------------------------
# Linajes
# ---------------------------------------------------------------------------

def arbol(esc):
    """Coloso Silvestre: tronco vivo con corteza profunda, cornamenta de ramas, musgo y savia luminosa."""
    random.seed(11)
    corteza = esc.material("corteza", (0.3, 0.21, 0.13), 0, 0.85)
    corteza_osc = esc.material("corteza_oscura", (0.18, 0.12, 0.08), 0, 0.9)
    musgo = esc.material("tela_musgo", (0.22, 0.38, 0.12), 0, 0.95)
    hojas = esc.material("tela_hojas", (0.2, 0.45, 0.16), 0, 0.7)
    savia = esc.material("emisivo_verde", (0.45, 1.0, 0.35), 0, 0.3, emision=4.0)

    esqueleto(esc)
    cuerpo_coloso(esc, corteza, relieve=0.05, escala_relieve=0.12)
    esc.pieza("Cabeza", unir(elipsoide((0, 2.45, 0.14), (0.2, 0.22, 0.23), 24, 12), elipsoide((0, 2.36, 0.26), (0.13, 0.09, 0.1), 16, 8)),
              corteza_osc, "Cabeza", 'suave', desplazamiento=0.03, escala_ruido=0.08)
    ojos(esc, savia, y=2.48, z=0.34)
    nucleo(esc, savia, radio=0.14)
    vetas(esc, "Savia del pecho", "Torso", savia, [
        ((0, 1.88, 0.45), (-0.2, 1.7, 0.42), (-0.32, 1.45, 0.3)),
        ((0, 1.88, 0.45), (0.25, 1.75, 0.42), (0.4, 1.55, 0.28)),
        ((0, 1.88, 0.45), (0.05, 2.05, 0.4), (-0.15, 2.18, 0.3)),
    ], 0.016)

    # Cornamenta: dos ramas principales con brotes y racimos de hojas.
    ramas, racimos = [], []
    for s in (-1, 1):
        base = V(0.1 * s, 2.6, 0.08)
        medio = V(0.38 * s, 2.95, -0.05)
        punta = V(0.55 * s, 3.25, -0.2)
        ramas.append(tubo(curva_bezier(base, medio, punta, 10), [0.07 - 0.006 * i for i in range(11)], 10))
        for f, largo in ((0.45, 0.28), (0.7, 0.22)):
            p = curva_bezier(base, medio, punta, 10)[int(f * 10)]
            fin = p + V(0.1 * s, 0.22, 0.12 if f < 0.6 else -0.1)
            ramas.append(tubo([p, p.lerp(fin, 0.5) + V(0, 0.03, 0), fin], [0.035, 0.025, 0.012], 8))
            racimos.append(elipsoide(fin + V(0, 0.05, 0), (0.16, 0.08, 0.14), 14, 7))
        racimos.append(elipsoide(punta + V(0, 0.06, 0), (0.18, 0.09, 0.16), 14, 7))
    esc.pieza("Cuernos rama", unir(*ramas), corteza_osc, "Cabeza", 'suave', desplazamiento=0.015, escala_ruido=0.05)
    esc.pieza("Corona de hojas", unir(*racimos), hojas, "Cabeza", 'suave', desplazamiento=0.05, escala_ruido=0.05)

    # Musgo sobre hombros y espalda; raíces que brotan de los pies.
    esc.pieza("Musgo", unir(elipsoide((0, 2.18, -0.15), (0.45, 0.14, 0.3), 20, 8), elipsoide((-0.15, 1.55, -0.35), (0.25, 0.2, 0.08), 16, 8)),
              musgo, "Torso", 'suave', desplazamiento=0.04, escala_ruido=0.04)
    for l, s in LADOS:
        esc.pieza(f"Musgo hombro_{l}", elipsoide((0.66 * s, 2.32, -0.02), (0.24, 0.1, 0.22), 18, 8), musgo, f"Brazo_{l}", 'suave', desplazamiento=0.04, escala_ruido=0.04)
        vetas(esc, f"Savia del brazo_{l}", f"Antebrazo_{l}", savia, [((0.84 * s, 1.5, 0.05), (0.87 * s, 1.25, 0.1), (0.84 * s, 1.02, 0.12))])
        # Mano de raíces: palma nudosa y dedos-raíz.
        x = 0.68 * s
        dedos = [elipsoide((x, 0.86, 0.04), (0.19, 0.17, 0.2), 22, 10)]
        for i in range(4):
            a = -0.5 + i * 0.33
            ini = V(x, 0.78, 0.04 + a * 0.2)
            dedos.append(tubo(curva_bezier(ini, ini + V(-0.05 * s, -0.18, a * 0.08), ini + V(-0.14 * s, -0.26, a * 0.1), 6), [0.05, 0.045, 0.04, 0.035, 0.028, 0.02, 0.012], 8))
        esc.pieza(f"Mano de raices_{l}", unir(*dedos), corteza_osc, f"Mano_{l}", 'suave', desplazamiento=0.02, escala_ruido=0.05)
        pie_coloso(esc, l, s, corteza_osc, relieve=0.03, escala=0.08)
        raices = []
        for i in range(4):
            a = (i / 4) * 2 * math.pi + 0.3
            ini = V(0.28 * s + math.cos(a) * 0.15, 0.12, 0.04 + math.sin(a) * 0.18)
            fin = V(0.28 * s + math.cos(a) * 0.42, 0.02, 0.04 + math.sin(a) * 0.45)
            raices.append(tubo(curva_bezier(ini, ini.lerp(fin, 0.5) + V(0, 0.06, 0), fin, 6), [0.06, 0.05, 0.04, 0.03, 0.022, 0.014, 0.006], 8))
        esc.pieza(f"Raices_{l}", unir(*raices), corteza, f"Pie_{l}", 'suave', desplazamiento=0.015, escala_ruido=0.05)


def agua(esc):
    """Leviatán de Marea: coral y nácar, núcleo de agua viva, aletas y una cascada invertida."""
    coral = esc.material("coral", (0.52, 0.72, 0.76), 0, 0.55)
    nacar = esc.material("cristal_nacar", (0.85, 0.9, 0.95), 0.3, 0.15)
    profundo = esc.material("coral_profundo", (0.16, 0.32, 0.42), 0, 0.5)
    agua_viva = esc.material("emisivo_cian", (0.3, 0.9, 1.0), 0, 0.1, emision=4.0)

    esqueleto(esc)
    cuerpo_coloso(esc, coral, relieve=0.012, escala_relieve=0.1)
    esc.pieza("Cabeza", unir(elipsoide((0, 2.45, 0.14), (0.19, 0.2, 0.25), 24, 12), elipsoide((0, 2.38, 0.3), (0.12, 0.07, 0.1), 16, 8)),
              profundo, "Cabeza", 'suave')
    ojos(esc, agua_viva, y=2.47, z=0.35)
    nucleo(esc, agua_viva, radio=0.16, anillo=nacar)
    # Cresta de aletas en la cabeza y "cascada invertida" que sube desde la nuca.
    aletas = []
    for i in range(5):
        z = 0.25 - i * 0.1
        aletas.append(cono((0, 2.6 - i * 0.01, z), (0, 2.85 - i * 0.04, z - 0.18), 0.09 - i * 0.008, 10, (0.2, 1)))
    esc.pieza("Cresta de aletas", unir(*aletas), nacar, "Cabeza", 'suave')
    esc.pieza("Cascada invertida", tubo(curva_bezier((0, 2.4, -0.1), (0, 2.85, -0.35), (0, 3.2, -0.15), 10),
                                        [0.12, 0.13, 0.13, 0.12, 0.11, 0.1, 0.08, 0.06, 0.045, 0.03, 0.015], 14), agua_viva, "Cabeza", 'suave')
    # Placas de nácar en el pecho y caparazones espirales en los hombros.
    esc.pieza("Placas de nacar", unir(*[elipsoide((x, y, 0.36), (0.16, 0.12, 0.06), 16, 8) for x, y in ((-0.26, 2.0), (0.26, 2.0), (-0.3, 1.72), (0.3, 1.72))]),
              nacar, "Torso", 'suave')
    esc.pieza("Anillo de nacar", toro((0, 1.62, 0.02), 0.45, 0.035, escala=(1, 1, 0.85)), nacar, "Torso", 'suave')
    vetas(esc, "Corrientes", "Torso", agua_viva, [
        ((0, 1.72, 0.44), (-0.25, 1.5, 0.35), (-0.3, 1.3, 0.25)),
        ((0, 1.72, 0.44), (0.25, 1.5, 0.35), (0.3, 1.3, 0.25)),
    ], 0.014)
    for l, s in LADOS:
        concha = tubo([V(0.66 * s + math.cos(a) * 0.22 * (1 - a / 12) * s, 2.18 + a * 0.012, math.sin(a) * 0.22 * (1 - a / 12)) for a in [i * 0.8 for i in range(12)]],
                      [0.16 * (1 - i / 13) for i in range(12)], 14)
        esc.pieza(f"Caparazon_{l}", concha, nacar, f"Brazo_{l}", 'suave')
        esc.pieza(f"Aleta del antebrazo_{l}", cono((0.86 * s, 1.45, -0.05), (1.02 * s, 1.1, -0.2), 0.16, 10, (0.15, 1)), profundo, f"Antebrazo_{l}", 'suave')
        puno(esc, l, s, profundo)
        pie_coloso(esc, l, s, profundo, dedos=3)


def tierra(esc):
    """Gólem de Chimenea (Tierra-Forja): roca, bandas de hierro remachadas, chimenea humeante y núcleo incandescente."""
    piedra = esc.material("piedra", (0.42, 0.38, 0.34), 0, 0.9)
    roca = esc.material("roca_oscura", (0.25, 0.23, 0.21), 0, 0.95)
    hierro = esc.material("hierro", (0.28, 0.27, 0.27), 0.9, 0.5)
    laton = esc.material("laton", (0.6, 0.45, 0.25), 1.0, 0.4)
    brasa = esc.material("emisivo_naranja", (1.0, 0.45, 0.12), 0, 0.3, emision=5.0)

    esqueleto(esc)
    cuerpo_coloso(esc, piedra, relieve=0.08, escala_relieve=0.18, tipo='VORONOI', hombros=1.15)
    esc.pieza("Cabeza", unir(caja((0, 2.42, 0.14), (0.36, 0.3, 0.34)), caja((0, 2.32, 0.28), (0.28, 0.12, 0.12))), roca, "Cabeza", 'suave',
              desplazamiento=0.04, escala_ruido=0.1, tipo_ruido='VORONOI')
    esc.pieza("Visera de hierro", caja((0, 2.5, 0.31), (0.38, 0.06, 0.06)), hierro, "Cabeza", 'duro')
    ojos(esc, brasa, separacion=0.09, y=2.45, z=0.32, r=0.03)
    # Núcleo incandescente rodeado de una rejilla de hierro.
    nucleo(esc, brasa, radio=0.17, anillo=hierro)
    esc.pieza("Rejilla del nucleo", unir(*[caja((x, 1.88, 0.47), (0.025, 0.36, 0.03)) for x in (-0.09, 0.0, 0.09)]), hierro, "Torso", 'duro')
    esc.pieza("Grietas de brasa", unir(*[tubo(curva_bezier(a, b, c, 8), 0.018, 6) for a, b, c in (
        ((-0.15, 1.75, 0.43), (-0.3, 1.6, 0.38), (-0.35, 1.4, 0.3)),
        ((0.15, 1.7, 0.43), (0.3, 1.55, 0.36), (0.32, 1.35, 0.28)))]), brasa, "Torso", 'suave')
    # Bandas de hierro remachadas.
    bandas = [toro((0, 1.5, 0.02), 0.36, 0.04, escala=(1, 1.4, 0.8)), toro((0, 2.12, -0.05), 0.52, 0.045, escala=(1, 1.3, 0.75))]
    esc.pieza("Bandas de hierro", unir(*bandas), hierro, "Torso", 'duro')
    remaches = []
    for i in range(10):
        a = -1.2 + i * 0.27
        remaches.append(elipsoide((math.sin(a) * 0.37, 1.5, math.cos(a) * 0.3 + 0.02), (0.025, 0.025, 0.025), 8, 4))
    esc.pieza("Remaches", unir(*remaches), laton, "Torso", 'suave')
    # Chimenea sobre el hombro izquierdo con boca incandescente.
    esc.pieza("Chimenea", unir(cilindro((-0.62, 2.55, -0.25), 0.13, 0.7, seg=20), toro((-0.62, 2.88, -0.25), 0.14, 0.035),
                               toro((-0.62, 2.35, -0.25), 0.14, 0.03)), hierro, "Brazo_I", 'duro')
    esc.pieza("Llama de chimenea", cilindro((-0.62, 2.9, -0.25), 0.1, 0.03, seg=20), brasa, "Brazo_I", 'suave')
    for l, s in LADOS:
        esc.pieza(f"Brazalete de hierro_{l}", toro((0.69 * s, 1.2, 0.02), 0.21, 0.04, seg=24, seg_menor=8), hierro, f"Antebrazo_{l}", 'duro')
        puno(esc, l, s, roca, 'suave', relieve=0.04, escala=0.1)
        pie_coloso(esc, l, s, roca, 'suave', relieve=0.03, escala=0.1, dedos=2)
    esc.pieza("Nucleo incandescente", elipsoide((0.69, 1.25, 0.2), (0.08, 0.08, 0.05), 12, 6), brasa, "Antebrazo_D", 'suave')


def fuego(esc):
    """Ígneo de la Caldera: obsidiana fracturada, grietas de magma, melena de llamas y cuernos curvos."""
    random.seed(7)
    obsidiana = esc.material("obsidiana", (0.06, 0.05, 0.06), 0.2, 0.15)
    escoria = esc.material("roca_escoria", (0.18, 0.1, 0.08), 0, 0.9)
    magma = esc.material("emisivo_magma", (1.0, 0.38, 0.08), 0, 0.3, emision=6.0)
    llama = esc.material("emisivo_llama", (1.0, 0.62, 0.15), 0, 0.3, emision=5.0)

    esqueleto(esc)
    cuerpo_coloso(esc, obsidiana, relieve=0.04, escala_relieve=0.15, tipo='VORONOI')
    esc.pieza("Cabeza", unir(elipsoide((0, 2.45, 0.14), (0.19, 0.2, 0.23), 24, 12), elipsoide((0, 2.34, 0.28), (0.12, 0.08, 0.1), 16, 8)),
              obsidiana, "Cabeza", 'suave', desplazamiento=0.02, escala_ruido=0.08, tipo_ruido='VORONOI')
    ojos(esc, llama, y=2.48, z=0.33)
    nucleo(esc, magma, radio=0.15)
    esc.pieza("Cuernos", unir(*[tubo(curva_bezier((0.15 * s, 2.58, 0.1), (0.4 * s, 2.75, 0.05), (0.42 * s, 2.95, -0.2), 10),
                                     [0.07 - 0.0065 * i for i in range(11)], 10) for s in (-1, 1)]), escoria, "Cabeza", 'suave')
    # Melena de llamas: púas emisivas hacia arriba y atrás.
    puas = []
    for i in range(14):
        a = -1.3 + i * 0.2
        base = V(math.sin(a) * 0.15, 2.55 + 0.04 * math.cos(a), -0.02 - 0.08 * abs(math.cos(a)))
        punta = base + V(math.sin(a) * 0.15, 0.3 + random.uniform(0, 0.2), -0.25 - random.uniform(0, 0.15))
        puas.append(cono(base, punta, 0.07, 10))
    esc.pieza("Melena de llamas", unir(*puas), llama, "Cabeza", 'suave')
    # Grietas de magma por el cuerpo.
    trazos = []
    for _ in range(9):
        x0 = random.uniform(-0.4, 0.4)
        y0 = random.uniform(1.3, 2.1)
        z0 = 0.33 + 0.05 * (1 - abs(x0))
        trazos.append(((x0, y0, z0), (x0 + random.uniform(-0.12, 0.12), y0 - 0.12, z0 + 0.02), (x0 + random.uniform(-0.2, 0.2), y0 - 0.25, z0 - 0.03)))
    vetas(esc, "Grieta de magma", "Torso", magma, trazos, 0.018)
    for l, s in LADOS:
        hombro = [cono((0.66 * s + 0.1 * math.cos(a) * s, 2.3, 0.1 * math.sin(a)), (0.75 * s + 0.2 * math.cos(a) * s, 2.65, 0.18 * math.sin(a) - 0.1), 0.06, 8)
                  for a in (-0.8, 0.0, 0.8)]
        esc.pieza(f"Llamas del hombro_{l}", unir(*hombro), llama, f"Brazo_{l}", 'suave')
        vetas(esc, f"Magma del brazo_{l}", f"Antebrazo_{l}", magma, [((0.85 * s, 1.48, 0.06), (0.88 * s, 1.25, 0.1), (0.86 * s, 1.0, 0.1))], 0.016)
        vetas(esc, f"Magma de la pierna_{l}", f"Pierna_{l}", magma, [((0.4 * s, 0.5, 0.12), (0.43 * s, 0.32, 0.14), (0.4 * s, 0.15, 0.12))], 0.014)
        puno(esc, l, s, escoria, relieve=0.03, escala=0.08)
        pie_coloso(esc, l, s, escoria, relieve=0.03, escala=0.08)


def metal(esc):
    """Centinela de Aleación: placas pulidas segmentadas, anillos de resonancia flotantes y visor único."""
    placas = esc.material("metal_placas", (0.62, 0.66, 0.72), 0.95, 0.25)
    acero = esc.material("acero_oscuro", (0.22, 0.24, 0.28), 0.9, 0.4)
    resonancia = esc.material("emisivo_azul", (0.45, 0.7, 1.0), 0, 0.2, emision=4.0)

    esqueleto(esc)
    cuerpo_coloso(esc, acero, acabado='suave')
    # Armadura de placas sobre la masa interior.
    esc.pieza("Coraza", unir(elipsoide((0, 1.9, 0.1), (0.52, 0.36, 0.36), 32, 16, recorte_inferior=-0.6),
                             *[toro((0, 1.3 + i * 0.1, 0.03), 0.34 + i * 0.015, 0.04, escala=(1, 1, 0.8)) for i in range(3)]), placas, "Torso", 'duro')
    esc.pieza("Cabeza", unir(elipsoide((0, 2.46, 0.12), (0.18, 0.22, 0.22), 24, 12), caja((0, 2.32, 0.24), (0.22, 0.1, 0.14))), placas, "Cabeza", 'duro')
    esc.pieza("Lente", caja((0, 2.48, 0.33), (0.3, 0.05, 0.04)), resonancia, "Cabeza", 'duro')
    nucleo(esc, resonancia, radio=0.13, anillo=placas)
    esc.pieza("Anillo de resonancia", unir(toro((0, 2.2, 0.0), 0.85, 0.022, seg=48, seg_menor=8), toro((0, 1.55, 0.0), 0.7, 0.02, seg=48, seg_menor=8)),
              resonancia, "Torso", 'suave')
    for l, s in LADOS:
        # Hombrera flotante separada del cuerpo (levitación magnética).
        esc.pieza(f"Hombrera flotante_{l}", elipsoide((0.78 * s, 2.32, 0), (0.3, 0.16, 0.3), 24, 10, recorte_inferior=-0.2), placas, f"Brazo_{l}", 'duro')
        esc.pieza(f"Placa del brazo_{l}", elipsoide((0.7 * s, 1.8, 0.02), (0.2, 0.24, 0.2), 20, 10, recorte_inferior=-0.9, recorte_superior=0.9), placas, f"Brazo_{l}", 'duro')
        esc.pieza(f"Placa del antebrazo_{l}", unir(*[toro((0.69 * s, 1.45 - i * 0.14, 0.015), 0.19 + i * 0.01, 0.035, seg=24, seg_menor=8) for i in range(4)]),
                  placas, f"Antebrazo_{l}", 'duro')
        esc.pieza(f"Placa del muslo_{l}", elipsoide((0.28 * s, 0.86, 0.06), (0.22, 0.26, 0.2), 20, 10, recorte_inferior=-0.9, recorte_superior=0.9), placas, f"Muslo_{l}", 'duro')
        esc.pieza(f"Greba_{l}", elipsoide((0.28 * s, 0.33, 0.05), (0.18, 0.22, 0.17), 20, 10, recorte_inferior=-0.9, recorte_superior=0.9), placas, f"Pierna_{l}", 'duro')
        esc.pieza(f"Linea de resonancia_{l}", tubo([(0.89 * s, 1.5, 0.0), (0.9 * s, 1.0, 0.05)], 0.012, 6), resonancia, f"Antebrazo_{l}", 'suave')
        puno(esc, l, s, placas, 'duro')
        pie_coloso(esc, l, s, placas, 'duro', dedos=2)


LINAJES = {
    "Arbol": arbol,
    "Agua": agua,
    "Tierra": tierra,
    "Fuego": fuego,
    "Metal": metal,
}


def construir(nombre):
    esc = Escena(f"Guardian_{nombre}")
    LINAJES[nombre](esc)
    return esc

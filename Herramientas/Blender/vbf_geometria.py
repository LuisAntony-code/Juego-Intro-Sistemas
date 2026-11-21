"""
Utilidades de geometría para los generadores de modelos de Voidform: Bio-Forge (Blender 4.x, bpy + bmesh).

CONVENCIÓN DE COORDENADAS
-------------------------
Todo se escribe en COORDENADAS DE UNITY (X derecha del personaje, Y arriba, Z adelante, metros) y se
convierte al guardar cada malla a coordenadas de Blender (Z arriba, el personaje mira hacia -Y):

    Blender = (-x_unity, -z_unity, y_unity)

Se exporta a FBX con los ejes estándar para Unity (axis_forward='-Z', axis_up='Y'), de modo que los números de
este script coinciden con los de ConstructorModelosPersonajes.cs y ConstructorModelosArmas.cs. Además cada modelo
lleva dos marcadores (Marca_Frente y Marca_Arriba): Unity alinea el modelo con ellos y reconstruye las
articulaciones con ejes limpios, así que funciona aunque el importador añada rotaciones de conversión.

ARTICULACIONES
--------------
Cada articulación es un objeto vacío (Empty) con el nombre exacto que busca EsqueletoProcedural.AsignarPorNombre
(Cadera, Torso, Cabeza, Brazo_I, Antebrazo_I, Mano_I, ...). Las mallas cuelgan de su articulación con el origen
en la articulación, así el AnimadorProcedural las rota alrededor del punto correcto (hombro, codo, rodilla...).

MATERIALES
----------
Se llaman "VBF_<superficie>[_variante]" (VBF_acero, VBF_corteza_oscura, VBF_emisivo_verde...). Unity los
reconoce por el nombre (MaterialesProcedurales.AplicarMaterialesPorNombre) y les aplica metalicidad, suavidad y
las texturas PBR de Assets/Resources/Texturas.
"""

import math

import bmesh
import bpy
from mathutils import Matrix, Vector

# Matriz de conversión Unity -> Blender (reflexión: invierte el orden de las caras, por eso se recalculan normales).
U2B = Matrix(((-1, 0, 0), (0, 0, -1), (0, 1, 0)))


def V(x, y=None, z=None):
    if y is None:
        x, y, z = x
    return Vector((x, y, z))


# ---------------------------------------------------------------------------
# Escena y materiales
# ---------------------------------------------------------------------------

class Escena:
    """Contexto de construcción de UN modelo (se reinicia la escena de Blender para cada uno)."""

    def __init__(self, nombre_raiz):
        bpy.ops.wm.read_factory_settings(use_empty=True)
        self.materiales = {}
        self.texturas = {}
        self.articulaciones = {}
        self.posiciones = {}
        self.coleccion = bpy.context.scene.collection
        self.raiz = self.vacio(nombre_raiz, None, V(0, 0, 0))
        # Marcadores de orientación: Unity los usa para alinear el modelo (frente = +Z, arriba = +Y) y reconstruir
        # las articulaciones con ejes limpios, sea cual sea la conversión de ejes del FBX.
        self.vacio("Marca_Frente", self.raiz, V(0, 0, 1))
        self.vacio("Marca_Arriba", self.raiz, V(0, 1, 0))

    # ---- articulaciones ----
    def vacio(self, nombre, padre, pos_unity):
        ob = bpy.data.objects.new(nombre, None)
        ob.empty_display_type = 'SPHERE'
        ob.empty_display_size = 0.04
        self.coleccion.objects.link(ob)
        pos_b = U2B @ pos_unity
        if padre is not None:
            ob.parent = padre
            ob.location = pos_b - (U2B @ self.posiciones[padre.name])
        else:
            ob.location = pos_b
        self.posiciones[nombre] = pos_unity.copy()
        return ob

    def articulacion(self, nombre, padre_nombre, pos_unity):
        padre = self.articulaciones.get(padre_nombre, self.raiz) if padre_nombre else self.raiz
        ob = self.vacio(nombre, padre, V(pos_unity))
        self.articulaciones[nombre] = ob
        return ob

    # ---- materiales ----
    def material(self, nombre, color, metal=0.0, rugosidad=0.5, emision=0.0, alfa=1.0):
        if nombre in self.materiales:
            return self.materiales[nombre]
        m = bpy.data.materials.new("VBF_" + nombre)
        m.use_nodes = True
        bsdf = m.node_tree.nodes.get("Principled BSDF")
        rgba = (color[0], color[1], color[2], 1.0)
        m.diffuse_color = rgba
        m.metallic = metal
        m.roughness = rugosidad
        if bsdf is not None:
            bsdf.inputs["Base Color"].default_value = rgba
            bsdf.inputs["Metallic"].default_value = metal
            bsdf.inputs["Roughness"].default_value = rugosidad
            if emision > 0:
                entrada = bsdf.inputs.get("Emission Color") or bsdf.inputs.get("Emission")
                if entrada is not None:
                    entrada.default_value = rgba
                if "Emission Strength" in bsdf.inputs:
                    bsdf.inputs["Emission Strength"].default_value = emision
            if alfa < 1.0 and "Alpha" in bsdf.inputs:
                bsdf.inputs["Alpha"].default_value = alfa
        self.materiales[nombre] = m
        return m

    def textura_ruido(self, tipo='CLOUDS', escala=0.25, profundidad=2):
        clave = (tipo, escala, profundidad)
        if clave in self.texturas:
            return self.texturas[clave]
        t = bpy.data.textures.new(f"ruido_{tipo}_{escala}", tipo)
        if hasattr(t, "noise_scale"):
            t.noise_scale = escala
        if hasattr(t, "noise_depth"):
            t.noise_depth = profundidad
        self.texturas[clave] = t
        return t

    # ---- piezas ----
    def pieza(self, nombre, bm, material, articulacion, acabado='suave', desplazamiento=0.0, escala_ruido=0.2,
              tipo_ruido='CLOUDS', subdivision=None):
        """
        Crea un objeto de malla a partir de un bmesh en coordenadas de Unity (mundo), colgado de una articulación.
        acabado: 'suave' (subdivisión 1), 'organico' (subdivisión 2), 'duro' (biselado), 'liso' (sombreado suave sin
                 modificadores: faldones y capas, que la subdivisión encogería) y 'plano' (facetado, sin modificadores).
        desplazamiento: relieve procedural en metros (corteza, roca, coral...).
        """
        padre = self.articulaciones.get(articulacion, self.raiz) if articulacion else self.raiz
        origen = self.posiciones[padre.name]
        # Unity mundo -> Unity local (respecto a la articulación) -> Blender.
        bmesh.ops.translate(bm, verts=bm.verts, vec=-origen)
        bm.transform(U2B.to_4x4())
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        me = bpy.data.meshes.new(nombre)
        bm.to_mesh(me)
        bm.free()
        for p in me.polygons:
            p.use_smooth = acabado != 'plano'
        if hasattr(me, "use_auto_smooth"):
            me.use_auto_smooth = True
            me.auto_smooth_angle = math.radians(50 if acabado == 'duro' else 80)
        me.materials.append(material)
        ob = bpy.data.objects.new(nombre, me)
        self.coleccion.objects.link(ob)
        ob.parent = padre

        if acabado == 'duro':
            b = ob.modifiers.new("Bisel", 'BEVEL')
            b.width = 0.006
            b.segments = 3
            b.limit_method = 'ANGLE'
            b.angle_limit = math.radians(35)
        niveles = subdivision if subdivision is not None else {'suave': 1, 'organico': 2}.get(acabado, 0)
        if niveles > 0:
            s = ob.modifiers.new("Subdivision", 'SUBSURF')
            s.levels = niveles
            s.render_levels = niveles
        if desplazamiento > 0:
            d = ob.modifiers.new("Relieve", 'DISPLACE')
            d.texture = self.textura_ruido(tipo_ruido, escala_ruido)
            d.strength = desplazamiento
            d.mid_level = 0.5
            d.texture_coords = 'LOCAL'
        return ob


# ---------------------------------------------------------------------------
# Primitivas (coordenadas de Unity). Todas devuelven un bmesh nuevo.
# ---------------------------------------------------------------------------

def _base_perpendicular(t):
    ref = Vector((0, 1, 0)) if abs(t.y) < 0.95 else Vector((1, 0, 0))
    u = t.cross(ref).normalized()
    v = t.cross(u).normalized()
    return u, v


def loft(anillos, tapa_inicio=True, tapa_fin=True, centro_inicio=None, centro_fin=None):
    """Une anillos (listas de Vector con el mismo número de puntos) en una superficie cerrada."""
    bm = bmesh.new()
    vs = [[bm.verts.new(p) for p in a] for a in anillos]
    n = len(anillos[0])
    for a, b in zip(vs, vs[1:]):
        for i in range(n):
            j = (i + 1) % n
            bm.faces.new((a[i], a[j], b[j], b[i]))
    if tapa_inicio:
        c = bm.verts.new(centro_inicio if centro_inicio is not None else sum(anillos[0], Vector()) / n)
        for i in range(n):
            bm.faces.new((vs[0][(i + 1) % n], vs[0][i], c))
    if tapa_fin:
        c = bm.verts.new(centro_fin if centro_fin is not None else sum(anillos[-1], Vector()) / n)
        for i in range(n):
            bm.faces.new((vs[-1][i], vs[-1][(i + 1) % n], c))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm


def tubo(puntos, radios, seg=14, aplanar=(1.0, 1.0), tapas_redondas=True, giro=0.0):
    """
    Tubo a lo largo de una polilínea con radio variable (miembros, cuernos, ramas, palas de arco).
    aplanar=(a, b) escala la sección (elipse); tapas_redondas añade semiesferas en los extremos.
    """
    puntos = [V(p) for p in puntos]
    if not isinstance(radios, (list, tuple)):
        radios = [radios] * len(puntos)
    anillos = []
    u_prev = None
    n = len(puntos)

    def tangente(i):
        if i == 0:
            return (puntos[1] - puntos[0]).normalized()
        if i == n - 1:
            return (puntos[-1] - puntos[-2]).normalized()
        return (puntos[i + 1] - puntos[i - 1]).normalized()

    def anillo(c, t, r, u, v):
        pts = []
        for k in range(seg):
            a = 2 * math.pi * k / seg + giro
            pts.append(c + u * (math.cos(a) * r * aplanar[0]) + v * (math.sin(a) * r * aplanar[1]))
        return pts

    marcos = []
    for i in range(n):
        t = tangente(i)
        if u_prev is None:
            u, v = _base_perpendicular(t)
        else:
            # Transporte paralelo: evita que la sección se retuerza en curvas.
            u = (u_prev - t * u_prev.dot(t)).normalized()
            v = t.cross(u).normalized()
        u_prev = u
        marcos.append((t, u, v))

    if tapas_redondas:
        t, u, v = marcos[0]
        for k in (3, 2, 1):
            ang = math.pi / 2 * k / 4
            anillos.append(anillo(puntos[0] - t * radios[0] * math.cos(ang), t, radios[0] * math.sin(ang), u, v))
    for i in range(n):
        t, u, v = marcos[i]
        anillos.append(anillo(puntos[i], t, radios[i], u, v))
    if tapas_redondas:
        t, u, v = marcos[-1]
        for k in (1, 2, 3):
            ang = math.pi / 2 * k / 4
            anillos.append(anillo(puntos[-1] + t * radios[-1] * math.cos(ang), t, radios[-1] * math.sin(ang), u, v))
    return loft(anillos)


def miembro(p0, p1, r0, r1, seg=14, aplanar=(1.0, 1.0), tramos=4, abultamiento=0.0):
    """Segmento anatómico entre dos articulaciones; 'abultamiento' engorda el centro (músculo)."""
    p0, p1 = V(p0), V(p1)
    pts, rs = [], []
    for i in range(tramos + 1):
        f = i / tramos
        pts.append(p0.lerp(p1, f))
        rs.append((r0 + (r1 - r0) * f) * (1 + abultamiento * math.sin(math.pi * f)))
    return tubo(pts, rs, seg, aplanar)


def elipsoide(c, radios, seg=24, anillos=14, recorte_inferior=-1.0, recorte_superior=1.0):
    """Elipsoide; los recortes (de -1 a 1 en Y local) permiten cúpulas y casquetes."""
    c = V(c)
    rx, ry, rz = radios
    lat_min = math.asin(max(-1.0, min(1.0, recorte_inferior)))
    lat_max = math.asin(max(-1.0, min(1.0, recorte_superior)))
    filas = []
    for i in range(anillos + 1):
        lat = lat_min + (lat_max - lat_min) * i / anillos
        fila = []
        for k in range(seg):
            lon = 2 * math.pi * k / seg
            fila.append(c + Vector((math.cos(lat) * math.cos(lon) * rx, math.sin(lat) * ry, math.cos(lat) * math.sin(lon) * rz)))
        filas.append(fila)
    # Los anillos de los polos son un único punto: se quitan y el polo pasa a ser el centro de la tapa.
    centro_inf = centro_sup = None
    if recorte_inferior <= -0.999:
        filas = filas[1:]
        centro_inf = c + Vector((0, -ry, 0))
    if recorte_superior >= 0.999:
        filas = filas[:-1]
        centro_sup = c + Vector((0, ry, 0))
    return loft(filas, centro_inicio=centro_inf, centro_fin=centro_sup)


def caja(c, tam, rot=(0.0, 0.0, 0.0), conicidad=0.0):
    """Caja (tam = tamaño total). conicidad estrecha la cara superior (placas, botas, peto)."""
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        f = 1.0 - conicidad * (v.co.y + 0.5)
        v.co = Vector((v.co.x * tam[0] * f, v.co.y * tam[1], v.co.z * tam[2] * (1 - conicidad * 0.3 * (v.co.y + 0.5))))
    R = (Matrix.Rotation(math.radians(rot[1]), 3, 'Y') @ Matrix.Rotation(math.radians(rot[0]), 3, 'X')
         @ Matrix.Rotation(math.radians(rot[2]), 3, 'Z'))
    bm.transform(R.to_4x4())
    bmesh.ops.translate(bm, verts=bm.verts, vec=V(c))
    return bm


def cilindro(c, radio, alto, eje='y', seg=24, radio_superior=None):
    rs = radio if radio_superior is None else radio_superior
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=seg, radius1=radio, radius2=rs, depth=alto)
    # create_cone crea a lo largo de Z: se orienta al eje pedido.
    if eje == 'y':
        bm.transform(Matrix.Rotation(math.radians(-90), 4, 'X'))
    elif eje == 'x':
        bm.transform(Matrix.Rotation(math.radians(90), 4, 'Y'))
    bmesh.ops.translate(bm, verts=bm.verts, vec=V(c))
    return bm


def toro(c, radio_mayor, radio_menor, eje='y', seg=32, seg_menor=10, escala=(1.0, 1.0, 1.0)):
    c = V(c)
    filas = []
    for i in range(seg):
        a = 2 * math.pi * i / seg
        centro = Vector((math.cos(a) * radio_mayor, 0, math.sin(a) * radio_mayor))
        radial = Vector((math.cos(a), 0, math.sin(a)))
        fila = []
        for k in range(seg_menor):
            b = 2 * math.pi * k / seg_menor
            fila.append(centro + radial * math.cos(b) * radio_menor + Vector((0, math.sin(b) * radio_menor, 0)))
        filas.append(fila)
    bm = bmesh.new()
    vs = [[bm.verts.new(p) for p in f] for f in filas]
    for i in range(seg):
        a, b = vs[i], vs[(i + 1) % seg]
        for k in range(seg_menor):
            j = (k + 1) % seg_menor
            bm.faces.new((a[k], b[k], b[j], a[j]))
    for v in bm.verts:
        v.co = Vector((v.co.x * escala[0], v.co.y * escala[1], v.co.z * escala[2]))
    if eje == 'z':
        bm.transform(Matrix.Rotation(math.radians(90), 4, 'X'))
    elif eje == 'x':
        bm.transform(Matrix.Rotation(math.radians(90), 4, 'Z'))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bmesh.ops.translate(bm, verts=bm.verts, vec=c)
    return bm


def cono(base, punta, radio, seg=12, aplanar=(1.0, 1.0)):
    """Púa o cuerno recto (base redondeada, punta afilada)."""
    base, punta = V(base), V(punta)
    pts = [base.lerp(punta, f) for f in (0, 0.35, 0.7, 0.92, 1.0)]
    rs = [radio, radio * 0.72, radio * 0.4, radio * 0.14, radio * 0.02]
    return tubo(pts, rs, seg, aplanar, tapas_redondas=False)


def unir(*bms):
    """Fusiona varios bmesh en uno (mismo material, menos objetos y llamadas de dibujo en Unity)."""
    destino = bmesh.new()
    for bm in bms:
        me = bpy.data.meshes.new("_tmp")
        bm.to_mesh(me)
        destino.from_mesh(me)
        bpy.data.meshes.remove(me)
        bm.free()
    return destino


def curva_bezier(p0, p1, p2, pasos=8):
    """Puntos de una curva cuadrática (cuernos, ramas, palas del arco)."""
    p0, p1, p2 = V(p0), V(p1), V(p2)
    return [(1 - t) ** 2 * p0 + 2 * (1 - t) * t * p1 + t * t * p2 for t in [i / pasos for i in range(pasos + 1)]]

"""
Generador de modelos 3D de Voidform: Bio-Forge para Blender 4.x (funciona sin interfaz).

Crea y exporta a FBX, listos para Unity:
  * 5 arquetipos humanos   -> Assets/Resources/Modelos/Humanos/{Cazador,Paladin,Bastion,Tirador,Ciborg}.fbx
  * 5 guardianes           -> Assets/Resources/Modelos/Guardianes/{Arbol,Agua,Tierra,Fuego,Metal}.fbx
  * 5 armas + 2 accesorios -> Assets/Resources/Modelos/Armas/{ArcoCompuesto,Mandoble,MazaEscudo,RifleLargo,
                                                               ArsenalBiomecanico,Escudo,CanonPulso}.fbx
y, opcionalmente, una imagen de vista previa de cada uno renderizada con Cycles.

USO (desde la raíz del repositorio):
    blender -b --python Herramientas/Blender/generar_modelos_voidform.py
    blender -b --python Herramientas/Blender/generar_modelos_voidform.py -- --previas Docs/Imagenes/Modelos
    blender -b --python Herramientas/Blender/generar_modelos_voidform.py -- --solo Paladin --previas /tmp/previas
Opciones:
    --previas CARPETA   renderiza una vista previa PNG de cada modelo en CARPETA (Cycles; usa la GPU si la hay)
    --muestras N        muestras de Cycles por píxel de las previas (por defecto 48)
    --solo NOMBRE       genera solo ese modelo (puede repetirse)
    --salida CARPETA    carpeta Assets/Resources/Modelos alternativa

En un PC con GPU NVIDIA (p. ej. RTX 5070 Ti) las previas usan OptiX/CUDA automáticamente.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402

import vbf_armas  # noqa: E402
import vbf_guardianes  # noqa: E402
import vbf_humanos  # noqa: E402

RAIZ_REPO = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
CARPETA_TEXTURAS = os.path.join(RAIZ_REPO, "Assets", "Resources", "Texturas")
SUPERFICIES = ["metal_placas", "obsidiana", "cristal", "corteza", "coral", "madera", "cuero", "tela", "acero", "hierro",
               "laton", "oro", "piedra", "roca"]


def argumentos():
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    opciones = {"previas": None, "muestras": 48, "solo": [], "salida": os.path.join(RAIZ_REPO, "Assets", "Resources", "Modelos")}
    i = 0
    while i < len(args):
        a = args[i]
        if a == "--previas":
            opciones["previas"] = os.path.abspath(args[i + 1]); i += 1
        elif a == "--muestras":
            opciones["muestras"] = int(args[i + 1]); i += 1
        elif a == "--solo":
            opciones["solo"].append(args[i + 1]); i += 1
        elif a == "--salida":
            opciones["salida"] = os.path.abspath(args[i + 1]); i += 1
        i += 1
    return opciones


# ---------------------------------------------------------------------------
# Exportación
# ---------------------------------------------------------------------------

def exportar_fbx(ruta):
    os.makedirs(os.path.dirname(ruta), exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=ruta,
        use_selection=False,
        object_types={'EMPTY', 'MESH'},
        use_mesh_modifiers=True,
        mesh_smooth_type='FACE',
        add_leaf_bones=False,
        bake_space_transform=False,
        axis_forward='-Z',
        axis_up='Y',
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        use_custom_props=False,
        path_mode='STRIP',
        embed_textures=False,
    )


def contar_triangulos():
    total = 0
    dg = bpy.context.evaluated_depsgraph_get()
    for ob in bpy.context.scene.objects:
        if ob.type != 'MESH':
            continue
        me = ob.evaluated_get(dg).to_mesh()
        me.calc_loop_triangles()
        total += len(me.loop_triangles)
        ob.evaluated_get(dg).to_mesh_clear()
    return total


# ---------------------------------------------------------------------------
# Vista previa (Cycles)
# ---------------------------------------------------------------------------

def superficie_de(nombre_material):
    n = nombre_material.lower()
    for s in SUPERFICIES:
        if s in n:
            return s
    return None


def texturizar_para_previa(escala=2.0):
    """Añade a los materiales las mismas texturas PBR que usará Unity (proyección en caja), solo para la previa."""
    for m in bpy.data.materials:
        sup = superficie_de(m.name)
        if sup is None or "emisivo" in m.name.lower() or not m.use_nodes:
            continue
        ruta_a = os.path.join(CARPETA_TEXTURAS, f"{sup}_albedo.png")
        ruta_n = os.path.join(CARPETA_TEXTURAS, f"{sup}_normal.png")
        if not os.path.exists(ruta_a):
            continue
        nodos = m.node_tree.nodes
        enlaces = m.node_tree.links
        bsdf = nodos.get("Principled BSDF")
        coord = nodos.new("ShaderNodeTexCoord")
        mapeo = nodos.new("ShaderNodeMapping")
        mapeo.inputs["Scale"].default_value = (escala, escala, escala)
        enlaces.new(coord.outputs["Object"], mapeo.inputs["Vector"])
        img_a = nodos.new("ShaderNodeTexImage")
        img_a.image = bpy.data.images.load(ruta_a, check_existing=True)
        img_a.projection = 'BOX'
        img_a.projection_blend = 0.25
        enlaces.new(mapeo.outputs["Vector"], img_a.inputs["Vector"])
        mezcla = nodos.new("ShaderNodeMix")
        mezcla.data_type = 'RGBA'
        mezcla.blend_type = 'MULTIPLY'
        mezcla.inputs[0].default_value = 1.0
        mezcla.inputs[6].default_value = bsdf.inputs["Base Color"].default_value
        enlaces.new(img_a.outputs["Color"], mezcla.inputs[7])
        enlaces.new(mezcla.outputs[2], bsdf.inputs["Base Color"])
        if os.path.exists(ruta_n):
            img_n = nodos.new("ShaderNodeTexImage")
            img_n.image = bpy.data.images.load(ruta_n, check_existing=True)
            img_n.image.colorspace_settings.name = 'Non-Color'
            img_n.projection = 'BOX'
            img_n.projection_blend = 0.25
            enlaces.new(mapeo.outputs["Vector"], img_n.inputs["Vector"])
            mapa = nodos.new("ShaderNodeNormalMap")
            mapa.inputs["Strength"].default_value = 0.8
            enlaces.new(img_n.outputs["Color"], mapa.inputs["Color"])
            enlaces.new(mapa.outputs["Normal"], bsdf.inputs["Normal"])


def limites_escena():
    dg = bpy.context.evaluated_depsgraph_get()
    mn = Vector((1e9, 1e9, 1e9))
    mx = Vector((-1e9, -1e9, -1e9))
    for ob in bpy.context.scene.objects:
        if ob.type != 'MESH':
            continue
        ev = ob.evaluated_get(dg)
        for esquina in ev.bound_box:
            p = ob.matrix_world @ Vector(esquina)
            mn = Vector((min(mn.x, p.x), min(mn.y, p.y), min(mn.z, p.z)))
            mx = Vector((max(mx.x, p.x), max(mx.y, p.y), max(mx.z, p.z)))
    return mn, mx


def configurar_gpu(escena):
    try:
        prefs = bpy.context.preferences.addons["cycles"].preferences
        for tipo in ("OPTIX", "CUDA", "HIP", "METAL", "ONEAPI"):
            try:
                prefs.compute_device_type = tipo
                prefs.get_devices()
                if any(d.type == tipo for d in prefs.devices):
                    for d in prefs.devices:
                        d.use = True
                    escena.cycles.device = 'GPU'
                    return tipo
            except TypeError:
                continue
    except Exception:  # noqa: BLE001
        pass
    escena.cycles.device = 'CPU'
    return "CPU"


def renderizar_previa(ruta, muestras, arma=False):
    texturizar_para_previa(4.0 if arma else 2.0)
    escena = bpy.context.scene
    mn, mx = limites_escena()
    centro = (mn + mx) / 2
    alto = max(mx.z - mn.z, mx.x - mn.x, mx.y - mn.y) if arma else max(mx.z - mn.z, (mx.x - mn.x) * 0.75, (mx.y - mn.y) * 0.75)

    # Suelo oscuro para recibir sombras.
    bpy.ops.mesh.primitive_circle_add(vertices=64, radius=alto * 2.5, fill_type='NGON', location=(centro.x, centro.y, mn.z - (0.25 * alto if arma else 0)))
    suelo = bpy.context.active_object
    ms = bpy.data.materials.new("previa_suelo")
    ms.use_nodes = True
    ms.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.06, 0.065, 0.075, 1)
    ms.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.6
    suelo.data.materials.append(ms)

    # Iluminación de estudio de tres puntos.
    def luz(nombre, tipo, energia, pos, tam, color=(1, 1, 1)):
        datos = bpy.data.lights.new(nombre, tipo)
        datos.energy = energia
        datos.color = color
        if tipo == 'AREA':
            datos.size = tam
        ob = bpy.data.objects.new(nombre, datos)
        escena.collection.objects.link(ob)
        ob.location = pos
        direccion = centro - Vector(pos)
        ob.rotation_euler = direccion.to_track_quat('-Z', 'Y').to_euler()
        return ob

    k = alto
    luz("Clave", 'AREA', 160 * k * k, (centro.x - 1.6 * k, centro.y - 2.0 * k, centro.z + 1.4 * k), 1.5 * k, (1.0, 0.96, 0.9))
    luz("Relleno", 'AREA', 45 * k * k, (centro.x + 2.2 * k, centro.y - 1.2 * k, centro.z + 0.3 * k), 2.0 * k, (0.8, 0.88, 1.0))
    luz("Contra", 'AREA', 140 * k * k, (centro.x + 0.6 * k, centro.y + 2.2 * k, centro.z + 1.6 * k), 1.0 * k, (0.9, 0.95, 1.0))
    mundo = bpy.data.worlds.new("previa")
    mundo.use_nodes = True
    mundo.node_tree.nodes["Background"].inputs["Color"].default_value = (0.02, 0.022, 0.028, 1)
    escena.world = mundo

    cam_datos = bpy.data.cameras.new("Camara")
    cam_datos.lens = 85
    cam_datos.sensor_fit = 'VERTICAL'
    cam_datos.sensor_height = 24
    cam = bpy.data.objects.new("Camara", cam_datos)
    escena.collection.objects.link(cam)
    if arma:
        # Las armas apuntan a +Z de Unity (-Y de Blender): se ven de perfil, desde su lado derecho.
        distancia = alto * 0.55 / 0.1405
        cam.location = (centro.x - 0.95 * distancia, centro.y - 0.3 * distancia, centro.z + 0.25 * distancia)
    else:
        distancia = alto * 0.62 / 0.1405 + (mx.y - mn.y)
        cam.location = (centro.x - 0.45 * distancia, centro.y - 0.9 * distancia, centro.z + 0.12 * distancia)
    cam.rotation_euler = (centro - cam.location).to_track_quat('-Z', 'Y').to_euler()
    escena.camera = cam

    escena.render.engine = 'CYCLES'
    dispositivo = configurar_gpu(escena)
    escena.cycles.samples = muestras
    escena.cycles.use_denoising = True
    escena.render.resolution_x = 600 if arma else 480
    escena.render.resolution_y = 480 if arma else 640
    escena.render.film_transparent = False
    escena.view_settings.view_transform = 'AgX' if 'AgX' in [v.identifier for v in escena.view_settings.bl_rna.properties['view_transform'].enum_items] else 'Filmic'
    escena.render.filepath = ruta
    try:
        bpy.ops.render.render(write_still=True)
    except RuntimeError:
        escena.cycles.use_denoising = False
        bpy.ops.render.render(write_still=True)
    return dispositivo


# ---------------------------------------------------------------------------
# Programa principal
# ---------------------------------------------------------------------------

def main():
    op = argumentos()
    trabajos = []
    for nombre in vbf_humanos.ARQUETIPOS:
        trabajos.append((nombre, "Humanos", lambda n=nombre: vbf_humanos.construir(n), False))
    for nombre in vbf_guardianes.LINAJES:
        trabajos.append((nombre, "Guardianes", lambda n=nombre: vbf_guardianes.construir(n), False))
    for nombre in vbf_armas.ARMAS:
        trabajos.append((nombre, "Armas", lambda n=nombre: vbf_armas.construir(n), True))

    for nombre, carpeta, constructor, es_arma in trabajos:
        if op["solo"] and nombre not in op["solo"]:
            continue
        constructor()
        ruta = os.path.join(op["salida"], carpeta, f"{nombre}.fbx")
        exportar_fbx(ruta)
        tris = contar_triangulos()
        print(f"[Voidform] {carpeta}/{nombre}.fbx  ->  {tris:,} triángulos")
        if op["previas"]:
            os.makedirs(op["previas"], exist_ok=True)
            disp = renderizar_previa(os.path.join(op["previas"], f"{carpeta}_{nombre}.png"), op["muestras"], es_arma)
            print(f"[Voidform]   previa renderizada ({disp})")


if __name__ == "__main__":
    main()

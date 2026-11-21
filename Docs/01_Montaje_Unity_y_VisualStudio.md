# 01 · Montaje completo en Unity y Visual Studio

Esta guía explica, de lo más simple a lo más complejo, cómo poner en marcha **Voidform: Bio-Forge** en Unity, cómo trabajar el código en Visual Studio y cómo comprobar que cada sistema funciona.

---

## Nivel 1 — Lo mínimo para jugar

### 1.1 Requisitos

| Herramienta | Versión recomendada | Notas |
|---|---|---|
| Unity Hub | Última | Para instalar y abrir el editor. |
| Unity Editor | **2022.3 LTS** o **Unity 6 (6000.x)** | Hace falta 2021.3.18 o superior: el código usa `FindObjectsByType` y `FindFirstObjectByType`. |
| Visual Studio | **2022 Community** (o superior) | Con la carga de trabajo **"Desarrollo de juegos con Unity"**. |
| Paquetes de Unity | `com.unity.ugui` (Unity UI) | Viene instalado por defecto en las plantillas 3D. No hace falta TextMeshPro. |

### 1.2 Crear el proyecto y añadir los scripts

1. Abre **Unity Hub → New project → plantilla "3D (Built-In Render Pipeline)"** o **"3D (URP)"** (las dos funcionan). Ponle el nombre `VoidformBioForge`.
2. Cierra Unity.
3. Copia las carpetas `Assets/Scripts` y `Assets/Resources` de este repositorio dentro de la carpeta `Assets` del proyecto nuevo. La estructura queda así: `VoidformBioForge/Assets/Scripts/Nucleo/...` y `VoidformBioForge/Assets/Resources/Modelos/...`. `Resources` contiene los modelos 3D (FBX) y las texturas PBR; si no la copias, el juego funciona igual pero con modelos de primitivas (Doc 10).
   - **Alternativa con Git:** clona el repositorio en una carpeta aparte y copia `Assets/Scripts`, `Assets/Resources`, `Herramientas` y `Docs`. También puedes copiar el `.gitignore` de este repositorio a la raíz del proyecto y versionarlo desde ahí.
4. Abre el proyecto con Unity Hub. Unity compila los scripts. La consola no debe mostrar errores.

### 1.3 Configurar la entrada (obligatorio en Unity 6)

El juego usa el **Input Manager clásico** (`Input.GetAxis`, `Input.GetKeyDown`).

- Ve a **Edit → Project Settings → Player → Other Settings → Active Input Handling**.
- Selecciona **"Both"** (o **"Input Manager (Old)"**). Unity pedirá reiniciar.
- En Unity 2022.3 con la plantilla 3D suele estar ya en "Input Manager (Old)".

> Si el valor es "Input System Package (New)", el juego lanza `InvalidOperationException` al leer el teclado.

### 1.4 Pulsar Play

Tienes dos caminos y los dos funcionan:

- **Arranque automático (lo más rápido):** abre cualquier escena, incluida la `SampleScene` que crea Unity, y pulsa **Play**. `ArranqueAutomatico.cs` detecta que falta el director del juego y lo crea solo.
- **Escena dedicada (recomendado para compilar):** menú superior **Voidform → 1. Crear escena jugable**. Se crea `Assets/Escenas/VoidformBioForge.unity` con un GameObject que lleva `GestorFlujoPartida`, y la escena se añade a *Build Settings*. Ábrela y pulsa **Play**.

Al arrancar aparecen el menú principal y, de fondo, un bioma generado con la cámara orbitando la Fuente de Energía Viva.

---

## Nivel 2 — Trabajar en Visual Studio y probar cada sistema

### 2.1 Conectar Visual Studio con Unity

1. En Unity: **Edit → Preferences → External Tools → External Script Editor → Visual Studio 2022**.
2. En la misma ventana pulsa **"Regenerate project files"**.
3. Haz doble clic en cualquier script desde Unity: se abre la solución `VoidformBioForge.sln` en Visual Studio con IntelliSense completo.
4. **Depuración:** en Visual Studio pulsa **"Asociar a Unity"** (o F5 con el depurador de Unity), pon puntos de interrupción (por ejemplo en `GestorModo4vs4.RutinaResolverRondaConVentana`) y pulsa Play en Unity.

### 2.2 Recorrido de prueba por todo el juego

Usa esta lista para validar que todo funciona. Cada línea indica qué hacer y qué tiene que pasar.

| # | Qué hacer | Resultado esperado |
|---|---|---|
| 1 | **Jugar → Destructores → Conquista** | Personalización con 5 trajes y 5 armas, con su descripción y resistencias. |
| 2 | Elegir traje y arma → **Continuar → Con bots → Iniciar** | Arena del bioma, vista en 1.ª persona con arma visible, HUD con reloj **5:00**, 5 bases A–E e integridad del ecosistema. |
| 3 | Disparar (clic), recargar (R), cambiar de arma (1/2) | La munición baja, aparece "RECARGANDO", hay trazadores, impactos, retroceso y marcador de impacto (×). |
| 4 | Pulsar E y Q | Se activan las habilidades del traje y del arma. Las ranuras muestran el enfriamiento. |
| 5 | Dejar que la vida baje del 25% | Alerta parpadeante **"¡TRAJE CRÍTICO · RIESGO DE EXPLOSIÓN!"**. |
| 6 | Morir | El traje **explota** (destello, humo, escombros, onda y sacudida), sale el aviso "¡TU TRAJE HA EXPLOTADO!" y una cuenta atrás **"Reapareciendo en 3..."**. |
| 7 | Quedarse en una base sin enemigos | La barra de la base se llena y la base cambia al color de tu bando. El registro anuncia "Base X dominada". |
| 8 | Esperar a 0:00 o dominar las 5 bases | Victoria inmediata (5 bases) o resultado por mayoría o desempate. Luego aparece la pantalla de resultados. |
| 9 | **Cuidadores → 4vs4 → Con bots** | Vista en 3.ª persona, barra de maná y 4 ranuras (Clic, E, R, Q). |
| 10 | Lanzar habilidades hasta vaciar el maná | Aviso **"Maná insuficiente para X: necesita N (tienes M)"**. La habilidad no se lanza. |
| 11 | Morir en 4vs4 | **MODO ESPECTADOR**: la cámara sigue a un compañero vivo; con clic o Tab cambias de compañero. |
| 12 | Terminar 3 rondas | Banner por ronda, marcador y, en resultados, **copas antes → después**, rango, liga y ascenso o descenso. |
| 13 | **Modo Historia → nivel 1** como Destructor | Briefing con perspectiva, escuadrón de 4 (tú + 3 bots) contra 3 guardianes de Madera (Coloso Silvestre, Guardián de Corteza y Centinela de las Esporas) junto al Árbol Madre. Barras de ecosistema y de energía extraída. |
| 14 | Ganar el nivel 1 | El nivel 2 queda desbloqueado y aparece el botón "Siguiente nivel". |
| 15 | **Modo Historia** como Cuidador | Tú (Coloso Silvestre) y un aliado IA (Guardián de Corteza) contra 4 humanos bot. Si caes y tu aliado vive, lo espectas. Si ganáis, se desbloquea el siguiente nivel. |
| 16 | Niveles 5 y 6 como Cuidador | Juegas con un Custodio del Nexus (Eco Prismático o Aethernum). La tecla **F** rota entre Madera, Agua, Fuego y Metal. En el Nexus la gravedad es reducida (saltos más altos) y hay auroras. En el nivel 6, como humano, te enfrentas a 5 guardianes. |
| 17 | Esc durante la partida | Pausa: Continuar, Reiniciar o Abandonar. |
| 18 | **Clasificación** en el menú | Copas, rango, liga, racha, escudo y la fórmula. Incluye un botón para borrar el progreso. |

### 2.3 Ajustar valores sin tocar código

- Menú **Voidform → 2. Crear assets de datos**: genera `Assets/Datos/Trajes`, `Armas` y `Guardianes` como ScriptableObjects que se pueden editar en el Inspector.
- Los gestores (`GestorConquista5Bases`, `GestorModo4vs4`, `GestorModoHistoria`, `SistemaClasificacionYCopas`, `GestorEquilibrioEcosistema`) exponen sus reglas (duraciones, ventanas, copas, ritmos) con `[SerializeField]`.
- Selecciona el GameObject `GestorFlujoPartida` para cambiar la **configuración inicial**, el tiempo antes de los resultados o el fondo animado.

---

## Nivel 3 — Compilación, render, red real y resolución de problemas

### 3.1 Compilar el ejecutable

1. **File → Build Settings**: comprueba que `Assets/Escenas/VoidformBioForge.unity` está marcada (el menú *Crear escena jugable* lo hace solo).
2. **Shaders creados por código:** el juego usa `Shader.Find`. Para que los shaders entren en el ejecutable, ve a **Project Settings → Graphics → Always Included Shaders** y añade:
   - Built-in: `Standard`, `Particles/Standard Unlit`, `Sprites/Default`.
   - URP: `Universal Render Pipeline/Lit`, `Universal Render Pipeline/Particles/Unlit`.
   - Otra opción es crear en `Assets/Resources` un material con cada shader, de modo que Unity los incluya.
3. **Build And Run** (Windows, macOS o Linux).

### 3.2 Canal de render

`MaterialesProcedurales.cs` detecta solo el canal de render (`GraphicsSettings.currentRenderPipeline`):

- **Built-in:** usa `Standard` y `Particles/Standard Unlit`.
- **URP:** usa `Universal Render Pipeline/Lit` y `.../Particles/Unlit`. Para que la emisión (runas, lava, cristales) brille, activa **Post-processing → Bloom** en el *Volume* global de la escena.
- **HDRP:** se intenta `HDRP/Lit`. Funciona, pero la iluminación procedural está calibrada para Built-in y URP.

### 3.3 Usar el nuevo Input System (opcional)

1. Instala `com.unity.inputsystem`.
2. Sustituye en `EntradaJugadorLocal.Update()` las llamadas a `Input.*` por `Keyboard.current` y `Mouse.current`. La salida sigue siendo la misma `EntradaPersonaje`, así que no hay que tocar nada más.
3. En `FabricaInterfaz.AsegurarSistemaDeEventos()` cambia `StandaloneInputModule` por `InputSystemUIInputModule`.
4. Cambia también `Input.GetKeyDown(KeyCode.Escape)` en `GestorInterfazJuego.Update()` y `Input.GetMouseButtonDown` en `CamaraEspectador.Update()`.

### 3.4 Conectar multijugador real

Las salas funcionan con `ProveedorRedLocalSimulado`, que **simula** el servidor: genera códigos, simula la latencia y cubre con bots los puestos que ocuparían personas. Para tener red real:

1. Instala **Netcode for GameObjects**, **Unity Lobby** y **Unity Relay** (o Photon Fusion o Mirror).
2. Crea `ProveedorRedUnityLobby : IProveedorRed` e implementa:
   - `CrearSala` → `LobbyService.Instance.CreateLobbyAsync(...)` + `RelayService.CreateAllocationAsync(...)`. Devuelve el `LobbyCode` como código de sala.
   - `UnirsePorCodigo` → `JoinLobbyByCodeAsync(codigo)` + `JoinAllocationAsync(...)`.
   - `BuscarSalaPublica` → `QuickJoinLobbyAsync(...)` filtrando por modo.
   - `SalirDeSala` → abandonar el lobby y apagar el `NetworkManager`.
3. Regístralo al arrancar: `GestorFlujoPartida.Instancia.Salas.EstablecerProveedorRed(new ProveedorRedUnityLobby());`.
4. **Autoridad de servidor:** la lógica de reglas (`GestorConquista5Bases`, `GestorModo4vs4`, `SistemaSaludYTraje.RecibirDano`) debe ejecutarse solo en el host. Los clientes envían su `EntradaPersonaje` y el servidor la aplica. Esto es posible porque movimiento, armas y guardianes ya leen la intención desde `EntradaPersonaje`, tanto si viene de un humano como de un bot.

### 3.5 Problemas frecuentes

| Síntoma | Causa | Solución |
|---|---|---|
| `InvalidOperationException: You are trying to read Input using the UnityEngine.Input class` | Active Input Handling = "Input System Package (New)". | Ponerlo en **"Both"** (sección 1.3). |
| Objetos de color magenta | El shader no se encuentra (canal de render o compilación). | Revisa 3.1 y 3.2. En el editor, comprueba que el canal configurado coincide con el que tiene el proyecto. |
| No se ve texto en la interfaz | Fuente integrada no disponible (Unity anterior a 2022.2). | El código prueba `LegacyRuntime.ttf`, después `Arial.ttf` y después las fuentes del sistema. Actualiza a 2022.3 LTS. |
| Los botones no responden | No hay EventSystem o hay dos. | El juego crea uno si no existe. Si tu escena ya tiene otro con el nuevo Input System, aplica la sección 3.3. |
| La cámara no gira | El cursor no está capturado en la ventana *Game*. | Haz clic dentro de la ventana *Game* después de pulsar Play. |
| Iluminación duplicada | Luz direccional propia en la escena. | El generador de biomas desactiva las demás luces direccionales. Si quieres usar la tuya, quita el sol en `GeneradorBiomas.ConfigurarIluminacionYNiebla`. |
| El juego arranca en escenas que no son suyas | `ArranqueAutomatico.Habilitado = true`. | Cámbialo a `false` en `Flujo/ArranqueAutomatico.cs`. |

### 3.6 Sustituir el arte provisional por arte final

- **Personajes:** crea un prefab con el modelo final, añade `ModeloVisualPersonaje` y registra sus `Renderer` en tres capas: 0 = primaria, 1 = secundaria, 2 = emisiva (runas). Después cambia `ConstructorModelosPersonajes.ConstruirDestructor/ConstruirGuardian` para que instancie tu prefab. El resto de sistemas no cambia.
- **Explosión del traje:** asigna tu prefab de VFX en `SistemaSaludYTraje → efectoVisualExplosionTraje`.
- **Biomas:** sustituye las primitivas de `GeneradorBiomas.CrearArbolAncestral / CrearRocaDeHumedal / CrearPilarDeRoca / CrearAgujaDeObsidiana` por `Instantiate` de tus prefabs. Para que se marchiten con el ecosistema, sigue llamando a `RegistrarDegradable`.
- **Escenas hechas a mano:** puedes colocar tú mismo `ZonaCapturaBase` (5) y personajes y añadir `GestorConquista5Bases` o `GestorModo4vs4`. Si no se les asignan referencias, los gestores buscan solos las bases y las entidades de la escena. En ese caso pon `ArranqueAutomatico.Habilitado = false`, para que no se cree además el director con su menú.

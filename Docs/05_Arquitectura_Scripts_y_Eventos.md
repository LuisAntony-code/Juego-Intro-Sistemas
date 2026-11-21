# 05 · Arquitectura, scripts y eventos

## Nivel 1 — Mapa de carpetas y scripts

Todo el código está en el espacio de nombres `VoidformBioForge` (las herramientas de editor, en `VoidformBioForge.Herramientas`).

| Carpeta | Script | Responsabilidad |
|---|---|---|
| `Nucleo/` | `TiposYDatosJuego.cs` | Enumeraciones (bando, elemento, modo, sala, daño, trajes, armas, estados, bases, ligas, biomas, resultados, flujo), nombres en español y paleta `PaletaVoidform` |
| | `ConfiguracionPartida.cs` | Elecciones del jugador (`ConfiguracionPartida`) y resultado final (`ResultadoPartida`) |
| | `BusEventosJuego.cs` | Bus global: mensajes, bajas, explosiones de traje, inicio y fin de partida, cambios de pantalla |
| | `GuardadoProgreso.cs` | PlayerPrefs: niveles desbloqueados, copas, racha, escudo e integridad por nivel |
| `Entrada/` | `EntradaPersonaje.cs` | Intenciones del fotograma (mover, mirar, disparar, habilidades) |
| | `EntradaJugadorLocal.cs` | Teclado y ratón → `EntradaPersonaje` |
| `Combate/` | `ControladorMovimientoAssaultCube.cs` | Movimiento ágil, salto con coyote y búfer, cámara, empujes, enraizado, retroceso y apuntado de los bots |
| | `SistemaSaludYTraje.cs` | Vida, blindaje, resistencias, **explosión del traje**, muerte, reaparición y registro estático de entidades |
| | `SistemaEstadosAlterados.cs` | Ralentizado, Enraizado, Quemadura y Empujado, con inmunidades |
| | `SistemaArmasYTrajeDestructor.cs` | Arsenal, disparo por rayo, recarga, dispersión, retroceso y habilidades de traje y arma |
| | `DatosArma.cs`, `DatosTraje.cs` | ScriptableObjects con el catálogo oficial (`CrearPorDefecto`) |
| | `InformacionDano.cs` | Estructura de un golpe (cantidad, tipo, atacante, origen) |
| | `ModeloArmaPrimeraPersona.cs` | Arma visible en 1.ª persona con retroceso, balanceo y animación de recarga |
| `Guardianes/` | `DatosGuardianElemental.cs` | Fichas de Sylvaran, Nerivale, Korvhal, Ignara y Aethernum |
| | `ControladorGuardianElemental.cs` | Golpe básico, 3 habilidades, **maná**, enfriamientos, afinidad, jefe de nivel y rotación de elementos |
| | `MuroDeRocaElemental.cs` | Muro temporal de Korvhal |
| `IA/` | `ControladorBotIA.cs` | Bots/mods de ambos bandos que escriben en `EntradaPersonaje` |
| `Modos/` | `ZonaCapturaBase.cs` | Una base: captura, disputa, dominio y visual |
| | `GestorConquista5Bases.cs` | 5 bases y 5 minutos: reaparición, victoria inmediata, mayoría y desempates |
| | `GestorModo4vs4.cs` | 3 rondas, espectador, muerte simultánea, tiempo límite y empates |
| | `CamaraEspectador.cs` | Cámara que sigue a los compañeros vivos |
| `Progresion/` | `GestorModoHistoria.cs` | 5 niveles, composición por perspectiva, victoria del guardián, avance y desenlace |
| | `DatosNarrativaHistoria.cs` | Prólogo, guiones de los 5 niveles por bando y desenlaces |
| | `GestorSalasYEmparejamiento.cs` | Salas (crear, bots, amigos, pública), `IProveedorRed` y proveedor local simulado |
| | `SistemaClasificacionYCopas.cs` | Copas, rango, liga, racha y escudo |
| `Entornos/` | `DatosBioma.cs` | Arte, luz, niebla y peligro de cada bioma |
| | `GeneradorBiomas.cs` | Arena procedural, aparición de equipos, bases, coberturas, peligros y degradación |
| | `ZonaAmbientalBioma.cs` | Peligro que afecta distinto a cada facción |
| | `FuenteEnergiaViva.cs` | Núcleo central: extracción, disputa y visual según la integridad |
| | `GestorEquilibrioEcosistema.cs` | Integridad del ecosistema y energía extraída |
| `Modelos/` | `ConstructorModelosPersonajes.cs` | Modelos provisionales de humanos y guardianes + `ModeloVisualPersonaje` |
| `VFX/` | `MaterialesProcedurales.cs` | Materiales compatibles con Built-in, URP y HDRP |
| | `FabricaEfectosVisuales.cs` | Todos los VFX + `AutodestruirTrasTiempo`, `LuzEfimera`, `LineaDesvanecida`, `AnilloOndaExpansivo` y `SacudidaCamara` |
| `Interfaz/` | `FabricaInterfaz.cs` | Lienzo, paneles redondeados, textos, botones, barras y campos |
| | `GestorInterfazJuego.cs` | Menús, salas, briefing, pausa y resultados |
| | `GestorInterfazJuego.HUD.cs` | HUD completo (clase parcial) |
| | `EtiquetaNombre.cs` | Nombre y vida flotantes sobre cada personaje |
| `Flujo/` | `GestorFlujoPartida.cs` | Director: navegación, construcción de partidas, resultados y limpieza |
| | `ContextoPartida.cs` | Referencias de la partida en curso para la interfaz |
| | `OrbitaCamaraMenu.cs` | Cámara orbital del fondo del menú |
| | `ArranqueAutomatico.cs` | Crea el director al pulsar Play si falta |
| `Editor/` | `MenuVoidformEditor.cs` | Menú "Voidform": crear escena, crear assets de datos y borrar progreso |

---

## Nivel 2 — Cómo se construye una partida

```
Interfaz (botón Iniciar)
   └─► GestorFlujoPartida.IniciarPartida()
         1. DestruirPartida() + CancelarNivel() de Historia + destruir el fondo del menú
         2. GestorEquilibrioEcosistema (nuevo)
         3. GeneradorBiomas.Generar(bioma) ──► DescripcionArena (apariciones, 5 bases, Fuente)
         4. Según el modo:
              Conquista ─► 5 × ZonaCapturaBase + 4 Destructores + 4 Cuidadores + GestorConquista5Bases
              4vs4      ─► 4 + 4 + GestorModo4vs4
              Historia  ─► GestorModoHistoria.IniciarNivel() + 4 humanos + 1 guardián (jugador o jefe IA)
         5. Cámara: 1.ª persona (Destructor) o 3.ª persona (Guardián) + CamaraEspectador
         6. AlPartidaConstruida(ContextoPartida) ─► GestorInterfazJuego.VincularPartida()
         7. IrA(EnPartida) ─► cursor capturado, entrada desbloqueada
```

**Cada personaje** se ensambla así: `CharacterController` + `EntradaPersonaje` + modelo + `SistemaSaludYTraje` + `ControladorMovimientoAssaultCube` + `SistemaEstadosAlterados` + (`SistemaArmasYTrajeDestructor` **o** `ControladorGuardianElemental`) + (`EntradaJugadorLocal` **o** `ControladorBotIA`) + `EtiquetaNombre`.

**Orden de ejecución:** `GestorFlujoPartida` (−300) → `EntradaPersonaje` (−200, solo limpia en `LateUpdate`) → `EntradaJugadorLocal` y `ControladorBotIA` (−100, escriben intenciones) → movimiento, armas y guardianes (0, leen intenciones) → `EntradaPersonaje.LateUpdate` limpia los pulsos de un fotograma.

---

## Nivel 3 — Eventos y desacoplamiento

### 3.1 Quién emite y quién escucha

| Emisor | Evento | Oyentes |
|---|---|---|
| `SistemaSaludYTraje` | `AlCambiarSaludYBlindaje`, `AlCambiarAlertaTrajeCritico`, `AlRecibirDano`, `AlExplotarTrajeHumano`, `AlMorir`, `AlRevivir` | HUD; `GestorConquista5Bases` (reaparición); `GestorModo4vs4` y `GestorModoHistoria` (fin de ronda o nivel); `SistemaArmas` y `ControladorGuardian` (recargar al revivir) |
| `SistemaArmasYTrajeDestructor` | `AlCambiarMunicion`, `AlCambiarArma`, `AlCambiarEnfriamientoTraje/Arma`, `AlActivarHabilidad`, `AlConfirmarImpacto`, `AlDisparar` | HUD; `ModeloArmaPrimeraPersona` |
| `ControladorGuardianElemental` | `AlCambiarMana`, `AlCambiarEnfriamiento`, `AlCambiarElemento`, `AlFaltarMana`, `AlLanzarHabilidadElemental` | HUD |
| `SistemaEstadosAlterados` | `AlCambiarEstados` | HUD |
| `ZonaCapturaBase` | `AlCambiarEstadoBase`, `AlCambiarProgreso` | `GestorConquista5Bases`; HUD |
| `GestorConquista5Bases` | `AlActualizarReloj5Minutos`, `AlActualizarConteoBases`, `AlProgramarReaparicion`, `AlFinalizarConResultado` | HUD; `GestorFlujoPartida` (resultados) |
| `GestorModo4vs4` | `AlActualizarMarcadorRondas`, `AlActualizarRelojRonda`, `AlActualizarVivos`, `AlCambiarModoEspectador`, `AlIniciarRonda`, `AlFinalizarRonda`, `AlFinalizarPartida4vs4` | HUD; `GestorFlujoPartida` (espectador, copas y resultados) |
| `GestorModoHistoria` | `AlCambiarObjetivo`, `AlCambiarModoEspectador`, `AlFinalizarNivel`, `AlCompletarHistoria` | HUD; `GestorFlujoPartida` |
| `GestorEquilibrioEcosistema` | `AlCambiarIntegridad`, `AlCambiarEnergiaExtraida`, `AlColapsarEcosistema` | HUD; `GeneradorBiomas` (degradación); `GestorModoHistoria` (victoria por extracción). Nadie escucha todavía `AlColapsarEcosistema`: queda para sistemas futuros, y el colapso ya se anuncia en el registro de combate a través de `BusEventosJuego`. |
| `FuenteEnergiaViva` | `AlCambiarEstadoExtraccion` | HUD |
| `GestorSalasYEmparejamiento` | `AlCambiarEstadoSala`, `AlActualizarJugadores`, `AlConfigurarSala` | Interfaz (pantalla de sala) |
| `SistemaClasificacionYCopas` | `AlActualizarClasificacion`, `AlRegistrarCambio` | Interfaz (clasificación, HUD, resultados) |
| `GestorFlujoPartida` | `AlCambiarEstado`, `AlPartidaConstruida`, `AlPartidaDestruida`, `AlResultadosListos` | Interfaz |
| `BusEventosJuego` | `AlMensajeSistema`, `AlRegistrarBaja`, `AlExplotarTraje`, `AlIniciarPartida`, `AlFinalizarPartida`, `AlCambiarEstadoFlujo` | Registro de combate del HUD; sistemas futuros (logros, analíticas, audio) |

### 3.2 Reglas de limpieza (evitar suscripciones fantasma)

- Cada `VincularPartida` guarda una acción de desuscripción por cada evento, y `DesvincularPartida` las ejecuta todas.
- Los estados estáticos (`BusEventosJuego`, registro de entidades y de bases, bloqueos de daño y de entrada, cachés de VFX e interfaz) se reinician con `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`. Así funcionan aunque el editor tenga desactivada la recarga de dominio.
- `DestruirPartida` separa la cámara **antes** de destruir al jugador, cancela la corrutina de resultados y el nivel de Historia en curso, y desbloquea el daño global.

### 3.3 Cómo ampliar el juego

| Quiero... | Pasos |
|---|---|
| Un traje nuevo | Añadir un valor a `TipoTraje`, un `case` en `DatosTraje.CrearPorDefecto` y otro en `SistemaArmasYTrajeDestructor.IntentarActivarHabilidadTraje`. Después, cambiar el bucle de 5 trajes de `GestorInterfazJuego.ConstruirPersonalizacion` y el `% 5` de `GestorFlujoPartida.CrearEquiposVersus` (variedad de los bots). |
| Un arma nueva | Añadir un valor a `TipoArma`, un `case` en `DatosArma.CrearPorDefecto` y otro en `IntentarActivarHabilidadArma`, y añadirla al vector `armas` de `ConstruirPersonalizacion`. |
| Un elemento o guardián nuevo | Añadir un valor a `TipoElemento`, su ficha en `DatosGuardianElemental` y los `case` en las 3 habilidades de `ControladorGuardianElemental`. |
| Un sexto nivel | Subir `totalNiveles` y los `Clamp(…, 1, 5)`, añadir un `BiomaPlaneta` y su `DatosBioma`, y añadir un `case 6` en `DatosNarrativaHistoria.ObtenerNivel`. |
| Un modo nuevo | Crear `GestorModoX` siguiendo el patrón: registrar participantes → evaluar → evento `AlFinalizar...(ResultadoEnfrentamiento, string)`. Construirlo en `GestorFlujoPartida.IniciarPartida` y vincular su HUD en `VincularPartida`. |
| Red real | Implementar `IProveedorRed` (ver `01_...md`, sección 3.4). |

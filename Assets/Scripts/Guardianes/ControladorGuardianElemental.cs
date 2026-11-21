using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Controla a los personajes elementales de los Cuidadores (sin transformación previa).
    /// Inspirado en la contundencia elemental de un guardián tanque de la naturaleza, con diseño
    /// propio por bioma/elemento. En modos versus consume maná/energía para evitar el spam.
    /// -----------------------------------------------------------------
    /// Nivel 1: elemento, estadísticas por elemento y ataque básico cuerpo a cuerpo.
    /// Nivel 2: tres habilidades con costo de maná, regeneración, enfriamientos y aviso de
    ///          "maná insuficiente" (si no hay maná, la habilidad NO se lanza y no entra en enfriamiento).
    /// Nivel 3: estados alterados por elemento (raíces, corrientes, sismos, quemaduras), muros de
    ///          roca, curación de aliados, afinidad con el bioma, escalado de jefe en Historia,
    ///          rotación de elementos del Guardián del Núcleo y restauración del ecosistema.
    /// </summary>
    [RequireComponent(typeof(EntradaPersonaje))]
    public class ControladorGuardianElemental : MonoBehaviour
    {
        public const int SlotAtaqueBasico = 0;
        public const int SlotHabilidad1 = 1;
        public const int SlotHabilidad2 = 2;
        public const int SlotDefinitiva = 3;

        [Header("Elemento del Guardián Según el Nivel o Selección")]
        [SerializeField] private TipoElemento elementoActual = TipoElemento.Arbol;

        [Header("Sistema de Maná / Energía (Modos Versus)")]
        [SerializeField] private bool usarManaEnModoVersus = true;
        [SerializeField] private float manaMaximo = 100f;
        [SerializeField] private float regeneracionManaPorSegundo = 12f;
        [SerializeField] private float costoManaHabilidad1 = 25f;
        [SerializeField] private float costoManaHabilidad2 = 35f;
        [SerializeField] private float costoManaDefinitiva = 65f;

        [Header("Enfriamientos y Poder de Habilidades")]
        [SerializeField] private float enfriamientoHabilidad1 = 4f;
        [SerializeField] private float enfriamientoHabilidad2 = 7f;
        [SerializeField] private float enfriamientoDefinitiva = 16f;
        [SerializeField] private float radioEfecto = 6.5f;
        [SerializeField] private float danoBaseHabilidad = 30f;

        [Header("Reglas Avanzadas")]
        [Tooltip("Guardián del Núcleo (nivel 5): puede rotar entre los 4 elementos con la tecla F.")]
        [SerializeField] private bool puedeRotarElementos = false;
        [SerializeField] private float enfriamientoRotacionElemento = 8f;
        [SerializeField] private float multiplicadorAfinidadDano = 1.15f;
        [SerializeField] private float multiplicadorAfinidadMana = 1.25f;

        private DatosGuardianElemental datosActuales;
        private float manaActual;
        private readonly float[] enfriamientosRestantes = new float[4];
        private readonly float[] enfriamientosTotales = new float[4];
        private float enfriamientoRotacionRestante;
        private bool afinidadBiomaActiva;
        private bool esJefeHistoria;
        private float multiplicadorVidaJefe = 1f;
        private float multiplicadorDanoJefe = 1f;
        private RolGuardian rolActual = RolGuardian.Vanguardia;
        private GameObject auraActual;

        private EntradaPersonaje entrada;
        private SistemaSaludYTraje salud;
        private ControladorMovimientoAssaultCube movimiento;

        public TipoElemento ElementoActual => elementoActual;
        public RolGuardian Rol => rolActual;
        public float ManaActual => manaActual;
        public float ManaMaximo => manaMaximo;
        public bool UsaMana => usarManaEnModoVersus;
        public float RadioEfecto => radioEfecto;
        public bool PuedeRotarElementos => puedeRotarElementos;
        public DatosGuardianElemental Datos => datosActuales;
        public float AlcanceAtaqueBasico => datosActuales != null ? datosActuales.alcanceAtaqueBasico : 3f;

        public event Action<TipoElemento> AlCambiarElemento;
        public event Action<float, float> AlCambiarMana;
        public event Action<string> AlLanzarHabilidadElemental;
        public event Action<string, float, float> AlFaltarMana;
        public event Action<int, float, float> AlCambiarEnfriamiento;

        private void Awake()
        {
            entrada = GetComponent<EntradaPersonaje>();
            salud = GetComponent<SistemaSaludYTraje>();
            movimiento = GetComponent<ControladorMovimientoAssaultCube>();
            manaActual = manaMaximo;
            ConfigurarElementoParaNivel(elementoActual);
        }

        private void OnEnable()
        {
            if (salud != null) salud.AlRevivir += ManejarReaparicion;
        }

        private void OnDisable()
        {
            if (salud != null) salud.AlRevivir -= ManejarReaparicion;
        }

        private void Update()
        {
            RegenerarMana();
            ActualizarEnfriamientos();

            if (salud != null && salud.EstaMuerto) return;
            if (!entrada.EntradaHabilitada) return;

            // El orden es fijo y determinista: si se pulsan varias en el mismo fotograma,
            // cada una comprueba el maná que dejó la anterior (caso límite de spam).
            if (entrada.AtaquePrincipalMantenido || entrada.AtaquePrincipalPresionado) LanzarAtaqueBasico();
            if (entrada.Habilidad1Presionada) LanzarHabilidadElemental1();
            if (entrada.Habilidad2Presionada) LanzarHabilidadElemental2();
            if (entrada.HabilidadEspecialPresionada) LanzarHabilidadDefinitivaElemental();
            if (entrada.CambiarElementoPresionado) IntentarRotarElemento();
        }

        // ================== Configuración ==================

        public void EstablecerModoVersus(bool activo)
        {
            usarManaEnModoVersus = activo;
            manaActual = manaMaximo;
            AlCambiarMana?.Invoke(manaActual, manaMaximo);
        }

        public void EstablecerAfinidadBioma(bool activa)
        {
            afinidadBiomaActiva = activa;
        }

        public void EstablecerRotacionElementos(bool activa)
        {
            puedeRotarElementos = activa;
        }

        /// <summary>Convierte al guardián en jefe de nivel (Modo Historia, 4 humanos contra 1).</summary>
        public void EstablecerEscaladoJefe(float multiplicadorVida, float multiplicadorDano)
        {
            esJefeHistoria = true;
            multiplicadorVidaJefe = Mathf.Max(1f, multiplicadorVida);
            multiplicadorDanoJefe = Mathf.Max(0.5f, multiplicadorDano);
            AplicarElemento(elementoActual, true, false);
        }

        public void ConfigurarElementoParaNivel(TipoElemento nuevoElemento)
        {
            AplicarElemento(nuevoElemento, true, false);
        }

        /// <summary>Configura elemento y variante del linaje (Vanguardia, Control o Artillería).</summary>
        public void ConfigurarGuardian(TipoElemento nuevoElemento, RolGuardian rol)
        {
            rolActual = rol;
            AplicarElemento(nuevoElemento, true, false);
        }

        private void AplicarElemento(TipoElemento nuevoElemento, bool reiniciarMana, bool conservarPorcentajeVida)
        {
            float porcentajeVidaPrevio = salud != null ? salud.PorcentajeVida : 1f;
            elementoActual = nuevoElemento;
            datosActuales = puedeRotarElementos
                ? DatosGuardianElemental.CrearNexus(nuevoElemento, rolActual)
                : DatosGuardianElemental.CrearVariante(nuevoElemento, rolActual);

            danoBaseHabilidad = datosActuales.danoBaseHabilidad;
            radioEfecto = datosActuales.radioEfecto;

            enfriamientosTotales[SlotAtaqueBasico] = datosActuales.enfriamientoAtaqueBasico;
            enfriamientosTotales[SlotHabilidad1] = enfriamientoHabilidad1;
            enfriamientosTotales[SlotHabilidad2] = enfriamientoHabilidad2;
            enfriamientosTotales[SlotDefinitiva] = enfriamientoDefinitiva;

            if (salud != null)
            {
                salud.ConfigurarBandoYEstadisticas(BandoJuego.Cuidador,
                    datosActuales.vidaMaxima * multiplicadorVidaJefe,
                    datosActuales.blindajeMaximo * (esJefeHistoria ? 1.5f : 1f));

                if (conservarPorcentajeVida)
                {
                    // La rotación NO es una curación gratuita: se conserva el porcentaje de vida previo.
                    salud.EstablecerPorcentajeVida(porcentajeVidaPrevio);
                }
            }

            movimiento?.EstablecerMultiplicadorTraje(datosActuales.multiplicadorVelocidad);

            if (reiniciarMana) manaActual = manaMaximo;

            ActualizarApariencia();
            AlCambiarElemento?.Invoke(elementoActual);
            AlCambiarMana?.Invoke(manaActual, manaMaximo);
            for (int i = 0; i < 4; i++) AlCambiarEnfriamiento?.Invoke(i, enfriamientosRestantes[i], enfriamientosTotales[i]);
        }

        private void ActualizarApariencia()
        {
            ModeloVisualPersonaje modelo = GetComponentInChildren<ModeloVisualPersonaje>();
            if (modelo != null)
            {
                modelo.AplicarPaleta(datosActuales.colorPrimario, datosActuales.colorSecundario);
            }

            if (auraActual != null) Destroy(auraActual);
            auraActual = FabricaEfectosVisuales.CrearAuraElemental(transform, elementoActual);
            salud?.RefrescarCacheVisual();
        }

        private void ManejarReaparicion(SistemaSaludYTraje _)
        {
            manaActual = manaMaximo;
            for (int i = 0; i < 4; i++) enfriamientosRestantes[i] = 0f;
            AlCambiarMana?.Invoke(manaActual, manaMaximo);
        }

        // ================== Nivel 2: maná y enfriamientos ==================

        private void RegenerarMana()
        {
            if (!usarManaEnModoVersus) return;
            if (manaActual >= manaMaximo) return;
            if (salud != null && salud.EstaMuerto) return;
            float regen = regeneracionManaPorSegundo * (afinidadBiomaActiva ? multiplicadorAfinidadMana : 1f);
            manaActual = Mathf.Min(manaMaximo, manaActual + regen * Time.deltaTime);
            AlCambiarMana?.Invoke(manaActual, manaMaximo);
        }

        private void ActualizarEnfriamientos()
        {
            for (int i = 0; i < enfriamientosRestantes.Length; i++)
            {
                if (enfriamientosRestantes[i] > 0f)
                {
                    enfriamientosRestantes[i] = Mathf.Max(0f, enfriamientosRestantes[i] - Time.deltaTime);
                    AlCambiarEnfriamiento?.Invoke(i, enfriamientosRestantes[i], enfriamientosTotales[i]);
                }
            }
            if (enfriamientoRotacionRestante > 0f) enfriamientoRotacionRestante -= Time.deltaTime;
        }

        public float ObtenerEnfriamientoRestante(int slot) => enfriamientosRestantes[Mathf.Clamp(slot, 0, 3)];
        public float ObtenerEnfriamientoTotal(int slot) => enfriamientosTotales[Mathf.Clamp(slot, 0, 3)];

        public float ObtenerCostoMana(int slot)
        {
            switch (slot)
            {
                case SlotHabilidad1: return costoManaHabilidad1;
                case SlotHabilidad2: return costoManaHabilidad2;
                case SlotDefinitiva: return costoManaDefinitiva;
                default: return 0f;
            }
        }

        /// <summary>Consulta sin consumir (la usa la IA para decidir).</summary>
        public bool PuedeLanzar(int slot)
        {
            if (enfriamientosRestantes[slot] > 0f) return false;
            return !usarManaEnModoVersus || manaActual >= ObtenerCostoMana(slot);
        }

        private bool PuedeConsumirManaYEnfriamiento(int slot, string nombreHabilidad)
        {
            if (enfriamientosRestantes[slot] > 0f) return false;
            float costo = ObtenerCostoMana(slot);

            if (usarManaEnModoVersus && manaActual < costo)
            {
                AlFaltarMana?.Invoke(nombreHabilidad, costo, manaActual);
                return false;
            }

            if (usarManaEnModoVersus && costo > 0f)
            {
                manaActual = Mathf.Max(0f, manaActual - costo);
                AlCambiarMana?.Invoke(manaActual, manaMaximo);
            }

            enfriamientosRestantes[slot] = enfriamientosTotales[slot];
            AlCambiarEnfriamiento?.Invoke(slot, enfriamientosRestantes[slot], enfriamientosTotales[slot]);
            return true;
        }

        private float DanoEfectivo(float dano)
        {
            float afinidad = afinidadBiomaActiva ? multiplicadorAfinidadDano : 1f;
            return dano * afinidad * multiplicadorDanoJefe;
        }

        // ================== Nivel 1: ataque básico ==================

        public void LanzarAtaqueBasico()
        {
            if (!PuedeConsumirManaYEnfriamiento(SlotAtaqueBasico, datosActuales.nombreAtaqueBasico)) return;

            float alcance = datosActuales.alcanceAtaqueBasico;
            List<SistemaSaludYTraje> objetivos = ObtenerEnemigosEnCono(alcance + 0.8f, 65f);
            foreach (SistemaSaludYTraje objetivo in objetivos)
            {
                AplicarDanoElemental(objetivo, DanoEfectivo(datosActuales.danoAtaqueBasico), datosActuales.nombreAtaqueBasico);
                if (elementoActual == TipoElemento.Fuego)
                {
                    AplicarQuemadura(objetivo, 4f, 1.5f, datosActuales.nombreAtaqueBasico);
                }
            }

            Vector3 frente = transform.position + transform.forward * (alcance * 0.6f) + Vector3.up;
            FabricaEfectosVisuales.CrearEfectoElemental(elementoActual, frente, 0.9f);
        }

        // ================== Nivel 2-3: habilidades ==================

        public void LanzarHabilidadElemental1()
        {
            if (!PuedeConsumirManaYEnfriamiento(SlotHabilidad1, datosActuales.nombreHabilidad1)) return;

            switch (elementoActual)
            {
                case TipoElemento.Arbol:
                    foreach (SistemaSaludYTraje o in ObtenerEnemigosEnRadio(radioEfecto))
                    {
                        AplicarDanoElemental(o, DanoEfectivo(danoBaseHabilidad), datosActuales.nombreHabilidad1);
                        o.GetComponent<SistemaEstadosAlterados>()?.AplicarEnraizado(1.8f);
                    }
                    break;

                case TipoElemento.Agua:
                    salud?.Curar(28f);
                    foreach (SistemaSaludYTraje o in ObtenerEnemigosEnRadio(radioEfecto * 0.7f))
                    {
                        AplicarDanoElemental(o, DanoEfectivo(danoBaseHabilidad * 0.6f), datosActuales.nombreHabilidad1);
                    }
                    GestorEquilibrioEcosistema.Instancia?.RegistrarRestauracion(1.5f, datosActuales.nombreHabilidad1);
                    break;

                case TipoElemento.Tierra:
                    foreach (SistemaSaludYTraje o in ObtenerEnemigosEnRadio(radioEfecto * 0.85f))
                    {
                        AplicarDanoElemental(o, DanoEfectivo(danoBaseHabilidad * 1.15f), datosActuales.nombreHabilidad1);
                        SistemaEstadosAlterados est = o.GetComponent<SistemaEstadosAlterados>();
                        est?.AplicarRalentizacion(0.7f, 1.5f);
                        est?.AplicarEmpuje(Vector3.up * 6f);
                    }
                    break;

                case TipoElemento.Fuego:
                    foreach (SistemaSaludYTraje o in ObtenerEnemigosEnCono(radioEfecto * 1.4f, 70f))
                    {
                        AplicarDanoElemental(o, DanoEfectivo(danoBaseHabilidad * 1.3f), datosActuales.nombreHabilidad1);
                        AplicarQuemadura(o, 8f, 3f, datosActuales.nombreHabilidad1);
                    }
                    break;

                case TipoElemento.Metal:
                    // Pulso Magnético: atrae hacia el guardián (empuje negativo), daña y ralentiza.
                    foreach (SistemaSaludYTraje o in ObtenerEnemigosEnRadio(radioEfecto))
                    {
                        AplicarDanoElemental(o, DanoEfectivo(danoBaseHabilidad), datosActuales.nombreHabilidad1);
                        SistemaEstadosAlterados est = o.GetComponent<SistemaEstadosAlterados>();
                        est?.AplicarEmpuje(-DireccionHacia(o) * 9f + Vector3.up * 1.5f);
                        est?.AplicarRalentizacion(0.75f, 1.5f);
                    }
                    break;
            }

            FabricaEfectosVisuales.CrearEfectoElemental(elementoActual, transform.position, radioEfecto);
            AlLanzarHabilidadElemental?.Invoke($"{UtilidadesTipos.NombreElemento(elementoActual)}: {datosActuales.nombreHabilidad1}");
        }

        public void LanzarHabilidadElemental2()
        {
            if (!PuedeConsumirManaYEnfriamiento(SlotHabilidad2, datosActuales.nombreHabilidad2)) return;

            switch (elementoActual)
            {
                case TipoElemento.Arbol:
                    salud?.Curar(32f);
                    CurarAliadosCercanos(12f, 20f);
                    GestorEquilibrioEcosistema.Instancia?.RegistrarRestauracion(2f, datosActuales.nombreHabilidad2);
                    break;

                case TipoElemento.Agua:
                    foreach (SistemaSaludYTraje o in ObtenerEnemigosEnRadio(radioEfecto))
                    {
                        AplicarDanoElemental(o, DanoEfectivo(danoBaseHabilidad), datosActuales.nombreHabilidad2);
                        SistemaEstadosAlterados est = o.GetComponent<SistemaEstadosAlterados>();
                        est?.AplicarEmpuje(DireccionHacia(o) * 14f + Vector3.up * 2f);
                        est?.AplicarRalentizacion(0.6f, 2.5f);
                    }
                    break;

                case TipoElemento.Tierra:
                    salud?.Curar(22f);
                    salud?.AnadirBlindajeTemporal(25f);
                    foreach (SistemaSaludYTraje o in ObtenerEnemigosEnRadio(radioEfecto))
                    {
                        AplicarDanoElemental(o, DanoEfectivo(danoBaseHabilidad), datosActuales.nombreHabilidad2);
                    }
                    MuroDeRocaElemental.Crear(transform.position + transform.forward * 3.5f, transform.rotation, 6f);
                    break;

                case TipoElemento.Fuego:
                    foreach (SistemaSaludYTraje o in ObtenerEnemigosEnRadio(radioEfecto * 1.1f))
                    {
                        AplicarDanoElemental(o, DanoEfectivo(danoBaseHabilidad * 1.4f), datosActuales.nombreHabilidad2);
                        AplicarQuemadura(o, 10f, 3f, datosActuales.nombreHabilidad2);
                    }
                    break;

                case TipoElemento.Metal:
                    // Égida de Aleación: placas flotantes (+35 de blindaje) y descarga eléctrica corta.
                    salud?.AnadirBlindajeTemporal(35f);
                    foreach (SistemaSaludYTraje o in ObtenerEnemigosEnRadio(radioEfecto * 0.6f))
                    {
                        AplicarDanoElemental(o, DanoEfectivo(danoBaseHabilidad * 0.7f), datosActuales.nombreHabilidad2);
                    }
                    break;
            }

            FabricaEfectosVisuales.CrearEfectoElemental(elementoActual, transform.position, radioEfecto * 1.1f);
            AlLanzarHabilidadElemental?.Invoke($"{UtilidadesTipos.NombreElemento(elementoActual)}: {datosActuales.nombreHabilidad2}");
        }

        public void LanzarHabilidadDefinitivaElemental()
        {
            if (!PuedeConsumirManaYEnfriamiento(SlotDefinitiva, datosActuales.nombreDefinitiva)) return;

            float radio = radioEfecto * 1.5f;
            float dano = DanoEfectivo(danoBaseHabilidad * 1.8f);

            foreach (SistemaSaludYTraje o in ObtenerEnemigosEnRadio(radio))
            {
                AplicarDanoElemental(o, dano, datosActuales.nombreDefinitiva);
                SistemaEstadosAlterados est = o.GetComponent<SistemaEstadosAlterados>();
                switch (elementoActual)
                {
                    case TipoElemento.Arbol:
                        est?.AplicarEnraizado(2.5f);
                        break;
                    case TipoElemento.Agua:
                        est?.AplicarRalentizacion(0.4f, 3.5f);
                        est?.AplicarEmpuje(DireccionHacia(o) * 10f);
                        break;
                    case TipoElemento.Tierra:
                        est?.AplicarEmpuje(DireccionHacia(o) * 8f + Vector3.up * 9f);
                        est?.AplicarRalentizacion(0.5f, 2f);
                        break;
                    case TipoElemento.Fuego:
                        AplicarQuemadura(o, 12f, 4f, datosActuales.nombreDefinitiva);
                        est?.AplicarEmpuje(DireccionHacia(o) * 8f + Vector3.up * 3f);
                        break;
                    case TipoElemento.Metal:
                        est?.AplicarEnraizado(1.2f);
                        est?.AplicarRalentizacion(0.6f, 2.5f);
                        break;
                }
            }

            if (elementoActual == TipoElemento.Arbol || elementoActual == TipoElemento.Agua)
            {
                salud?.Curar(40f);
                GestorEquilibrioEcosistema.Instancia?.RegistrarRestauracion(3f, datosActuales.nombreDefinitiva);
            }

            FabricaEfectosVisuales.CrearEfectoElemental(elementoActual, transform.position, radio);
            FabricaEfectosVisuales.CrearAnilloOnda(transform.position + Vector3.up * 0.2f, radio, PaletaVoidform.ColorElemento(elementoActual));
            AlLanzarHabilidadElemental?.Invoke($"Definitiva — {datosActuales.nombreDefinitiva} ({UtilidadesTipos.NombreElemento(elementoActual)})");
        }

        /// <summary>Nexus (niveles 5 y 6): el guardián cambia al siguiente elemento conservando su porcentaje de vida.</summary>
        public void IntentarRotarElemento()
        {
            if (!puedeRotarElementos || enfriamientoRotacionRestante > 0f) return;
            enfriamientoRotacionRestante = enfriamientoRotacionElemento;
            // El Nexus rota entre los cuatro elementos primarios: Madera → Agua → Fuego → Metal → Madera.
            int indice = Array.IndexOf(UtilidadesTipos.ElementosPrimarios, elementoActual);
            TipoElemento siguiente = UtilidadesTipos.ElementosPrimarios[(indice + 1) % UtilidadesTipos.ElementosPrimarios.Length];
            AplicarElemento(siguiente, false, true);
            FabricaEfectosVisuales.CrearAnilloOnda(transform.position + Vector3.up * 0.2f, 4f, PaletaVoidform.ColorElemento(siguiente));
            AlLanzarHabilidadElemental?.Invoke($"{datosActuales.nombreGuardian} adopta la forma de {UtilidadesTipos.NombreElemento(siguiente)}");
        }

        // ================== Utilidades de área ==================

        private void AplicarDanoElemental(SistemaSaludYTraje objetivo, float dano, string origen)
        {
            InformacionDano info = InformacionDano.Crear(dano, UtilidadesTipos.ElementoATipoDano(elementoActual), salud, origen);
            objetivo.RecibirDano(info);
        }

        private void AplicarQuemadura(SistemaSaludYTraje objetivo, float danoPorSegundo, float duracion, string origen)
        {
            SistemaEstadosAlterados est = objetivo.GetComponent<SistemaEstadosAlterados>();
            if (est == null) return;
            InformacionDano fuente = InformacionDano.Crear(0f, TipoDano.Fuego, salud, origen);
            est.AplicarQuemadura(danoPorSegundo * multiplicadorDanoJefe, duracion, fuente);
        }

        private Vector3 DireccionHacia(SistemaSaludYTraje objetivo)
        {
            Vector3 d = objetivo.transform.position - transform.position;
            d.y = 0f;
            return d.sqrMagnitude < 0.001f ? transform.forward : d.normalized;
        }

        private void CurarAliadosCercanos(float radio, float cantidad)
        {
            foreach (SistemaSaludYTraje entidad in SistemaSaludYTraje.Entidades)
            {
                if (entidad == null || entidad == salud || entidad.EstaMuerto) continue;
                if (entidad.Bando != BandoJuego.Cuidador) continue;
                if (Vector3.Distance(entidad.transform.position, transform.position) <= radio)
                {
                    entidad.Curar(cantidad);
                }
            }
        }

        private List<SistemaSaludYTraje> ObtenerEnemigosEnRadio(float radio)
        {
            List<SistemaSaludYTraje> lista = new List<SistemaSaludYTraje>();
            foreach (SistemaSaludYTraje entidad in SistemaSaludYTraje.Entidades)
            {
                if (entidad == null || entidad == salud || entidad.EstaMuerto) continue;
                if (entidad.Bando == BandoJuego.Cuidador) continue;
                if (Vector3.Distance(entidad.transform.position, transform.position) <= radio)
                {
                    lista.Add(entidad);
                }
            }
            return lista;
        }

        private List<SistemaSaludYTraje> ObtenerEnemigosEnCono(float alcance, float anguloMedio)
        {
            List<SistemaSaludYTraje> lista = new List<SistemaSaludYTraje>();
            foreach (SistemaSaludYTraje entidad in ObtenerEnemigosEnRadio(alcance))
            {
                Vector3 direccion = entidad.transform.position - transform.position;
                direccion.y = 0f;
                if (direccion.sqrMagnitude < 0.5f || Vector3.Angle(transform.forward, direccion) <= anguloMedio)
                {
                    lista.Add(entidad);
                }
            }
            return lista;
        }

        /// <summary>Reenvía el estado completo (la interfaz lo llama al suscribirse).</summary>
        public void ForzarNotificacionCompleta()
        {
            AlCambiarElemento?.Invoke(elementoActual);
            AlCambiarMana?.Invoke(manaActual, manaMaximo);
            for (int i = 0; i < 4; i++) AlCambiarEnfriamiento?.Invoke(i, enfriamientosRestantes[i], enfriamientosTotales[i]);
        }
    }
}

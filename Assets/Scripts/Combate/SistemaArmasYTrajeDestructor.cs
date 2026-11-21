using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Reinterpreta la dinámica ágil de armas de arena shooter estilo AssaultCube (cadencia,
    /// cargador, reserva, recarga, retroceso, dispersión y alcance) y añade las habilidades
    /// especializadas del traje (tecla E) y del arma (tecla Q) de los Destructores humanos.
    /// -----------------------------------------------------------------
    /// Nivel 1: disparo inmediato por rayo (hitscan) con daño y alcance.
    /// Nivel 2: cargador, reserva, recarga manual/automática, cadencia, armas automáticas y
    ///          semiautomáticas, cambio entre arma principal y pistola, enfriamientos de habilidades.
    /// Nivel 3: dispersión dinámica (movimiento/aire), retroceso, caída de daño, perdigones,
    ///          balas perforantes, disparos potenciados y la "Descarga de Extracción", que cura
    ///          al humano a costa de la integridad del ecosistema (problemática social jugable).
    /// </summary>
    [RequireComponent(typeof(EntradaPersonaje))]
    public class SistemaArmasYTrajeDestructor : MonoBehaviour
    {
        private class EstadoArma
        {
            public DatosArma Datos;
            public int BalasEnCargador;
            public int MunicionReserva;
        }

        [Header("Arsenal (si se dejan vacíos se crean los oficiales en tiempo de ejecución)")]
        [SerializeField] private DatosArma datosArmaPrincipal;
        [SerializeField] private DatosArma datosArmaSecundaria;
        [SerializeField] private Transform puntoDisparo;
        [SerializeField] private LayerMask mascaraImpacto = ~0;

        [Header("Habilidad del Traje (valores de las habilidades)")]
        [SerializeField] private float multiplicadorImpulsoTraje = 1.8f;
        [SerializeField] private float duracionImpulsoTraje = 0.45f;
        [SerializeField] private float radioSierraPoda = 4.5f;
        [SerializeField] private float danoSierraPoda = 20f;

        private readonly EstadoArma[] arsenal = new EstadoArma[2];
        private int indiceArmaActiva;
        private bool estaRecargando;
        private Coroutine rutinaRecargaActual;
        private float proximoDisparoDisponible;
        private float dispersionActual;
        private float enfriamientoRestanteTraje;
        private float enfriamientoRestanteArma;

        private float multiplicadorSiguienteDisparo = 1f;
        private bool siguienteDisparoPerforante;
        private bool siguienteDisparoConmocionador;
        private float rafagaSupresoraHasta = -1f;

        private EntradaPersonaje entrada;
        private ControladorMovimientoAssaultCube movimiento;
        private SistemaSaludYTraje salud;
        private SistemaEstadosAlterados estados;

        public DatosArma ArmaActiva => arsenal[indiceArmaActiva]?.Datos;
        public int BalasEnCargador => arsenal[indiceArmaActiva]?.BalasEnCargador ?? 0;
        public int TamanoCargador => ArmaActiva != null ? ArmaActiva.tamanoCargador : 0;
        public int MunicionReserva => arsenal[indiceArmaActiva]?.MunicionReserva ?? 0;
        public bool EstaRecargando => estaRecargando;
        public float DispersionActual => dispersionActual;
        public float EnfriamientoRestanteTraje => enfriamientoRestanteTraje;
        public float EnfriamientoRestanteArma => enfriamientoRestanteArma;
        public DatosTraje Traje => salud != null ? salud.Traje : null;

        public event Action<int, int, int, bool> AlCambiarMunicion;
        public event Action<DatosArma> AlCambiarArma;
        public event Action<float, float> AlCambiarEnfriamientoTraje;
        public event Action<float, float> AlCambiarEnfriamientoArma;
        public event Action<string> AlActivarHabilidad;
        public event Action<bool> AlConfirmarImpacto;
        public event Action AlDisparar;

        private void Awake()
        {
            entrada = GetComponent<EntradaPersonaje>();
            movimiento = GetComponent<ControladorMovimientoAssaultCube>();
            salud = GetComponent<SistemaSaludYTraje>();
            estados = GetComponent<SistemaEstadosAlterados>();
            ConfigurarArsenal(datosArmaPrincipal, datosArmaSecundaria);
        }

        private void OnEnable()
        {
            if (salud != null) salud.AlRevivir += ManejarReaparicion;
        }

        private void OnDisable()
        {
            if (salud != null) salud.AlRevivir -= ManejarReaparicion;
        }

        /// <summary>Equipa un arma principal y una secundaria (por defecto, la Pistola Auxiliar).</summary>
        public void ConfigurarArsenal(DatosArma principal, DatosArma secundaria)
        {
            datosArmaPrincipal = principal != null ? principal : DatosArma.CrearPorDefecto(TipoArma.RifleExtractor);
            datosArmaSecundaria = secundaria != null ? secundaria : DatosArma.CrearPorDefecto(TipoArma.PistolaAuxiliar);

            arsenal[0] = CrearEstadoArma(datosArmaPrincipal);
            arsenal[1] = CrearEstadoArma(datosArmaSecundaria);
            indiceArmaActiva = 0;
            CancelarRecarga();
            dispersionActual = ArmaActiva.dispersionBase;
            NotificarArmaYMunicion();
        }

        private static EstadoArma CrearEstadoArma(DatosArma datos)
        {
            return new EstadoArma
            {
                Datos = datos,
                BalasEnCargador = datos.tamanoCargador,
                MunicionReserva = datos.municionReservaMaxima
            };
        }

        private void ManejarReaparicion(SistemaSaludYTraje _)
        {
            foreach (EstadoArma arma in arsenal)
            {
                if (arma == null) continue;
                arma.BalasEnCargador = arma.Datos.tamanoCargador;
                arma.MunicionReserva = arma.Datos.municionReservaMaxima;
            }
            CancelarRecarga();
            indiceArmaActiva = 0;
            multiplicadorSiguienteDisparo = 1f;
            siguienteDisparoPerforante = false;
            siguienteDisparoConmocionador = false;
            NotificarArmaYMunicion();
        }

        private void Update()
        {
            ActualizarEnfriamientos();
            RecuperarDispersion();

            if (salud != null && salud.EstaMuerto) return;
            if (!entrada.EntradaHabilitada) return;

            if (entrada.CambioArmaSolicitado >= 0)
            {
                CambiarArma(entrada.CambioArmaSolicitado);
            }

            if (entrada.Habilidad1Presionada) IntentarActivarHabilidadTraje();
            if (entrada.HabilidadEspecialPresionada) IntentarActivarHabilidadArma();

            if (estaRecargando) return;

            DatosArma arma = ArmaActiva;
            bool quiereDisparar = arma.esAutomatica ? entrada.AtaquePrincipalMantenido : entrada.AtaquePrincipalPresionado;
            if (quiereDisparar && Time.time >= proximoDisparoDisponible)
            {
                IntentarDisparar();
            }

            if (entrada.RecargarPresionado && BalasEnCargador < TamanoCargador && MunicionReserva > 0)
            {
                IniciarRecarga();
            }
        }

        // ================== Nivel 2: enfriamientos ==================

        private void ActualizarEnfriamientos()
        {
            float totalTraje = Traje != null ? Traje.enfriamientoHabilidad : 6f;
            if (enfriamientoRestanteTraje > 0f)
            {
                enfriamientoRestanteTraje = Mathf.Max(0f, enfriamientoRestanteTraje - Time.deltaTime);
                AlCambiarEnfriamientoTraje?.Invoke(enfriamientoRestanteTraje, totalTraje);
            }

            float totalArma = ArmaActiva != null ? ArmaActiva.enfriamientoHabilidad : 10f;
            if (enfriamientoRestanteArma > 0f)
            {
                enfriamientoRestanteArma = Mathf.Max(0f, enfriamientoRestanteArma - Time.deltaTime);
                AlCambiarEnfriamientoArma?.Invoke(enfriamientoRestanteArma, totalArma);
            }
        }

        private void RecuperarDispersion()
        {
            DatosArma arma = ArmaActiva;
            if (arma == null) return;
            dispersionActual = Mathf.MoveTowards(dispersionActual, arma.dispersionBase, arma.recuperacionDispersionPorSegundo * Time.deltaTime);
        }

        // ================== Nivel 1-3: disparo ==================

        public void IntentarDisparar()
        {
            EstadoArma estado = arsenal[indiceArmaActiva];
            DatosArma arma = estado.Datos;

            if (estado.BalasEnCargador <= 0)
            {
                if (estado.MunicionReserva > 0) IniciarRecarga();
                else ManejarSinMunicion();
                return;
            }

            float intervalo = arma.IntervaloEntreDisparos;
            if (Time.time < rafagaSupresoraHasta) intervalo *= 0.5f;
            proximoDisparoDisponible = Time.time + intervalo;
            estado.BalasEnCargador--;

            Transform origen = ObtenerOrigenDisparo();
            float dispersionEfectiva = CalcularDispersionEfectiva(arma);
            float multiplicadorDano = multiplicadorSiguienteDisparo;
            bool perforante = siguienteDisparoPerforante;
            bool conmocionador = siguienteDisparoConmocionador;
            multiplicadorSiguienteDisparo = 1f;
            siguienteDisparoPerforante = false;
            siguienteDisparoConmocionador = false;

            bool huboImpacto = false;
            bool huboBaja = false;
            Vector3 boca = origen.position + origen.forward * 0.6f + origen.right * 0.18f - origen.up * 0.15f;

            for (int p = 0; p < Mathf.Max(1, arma.perdigonesPorDisparo); p++)
            {
                Vector3 direccion = AplicarDispersion(origen.forward, dispersionEfectiva);
                Vector3 puntoFinal = origen.position + direccion * arma.alcanceMaximo;

                if (perforante)
                {
                    RaycastHit[] impactos = Physics.RaycastAll(origen.position, direccion, arma.alcanceMaximo, mascaraImpacto, QueryTriggerInteraction.Ignore);
                    Array.Sort(impactos, (a, b) => a.distance.CompareTo(b.distance));
                    int atravesados = 0;
                    foreach (RaycastHit impacto in impactos)
                    {
                        SistemaSaludYTraje objetivo = impacto.collider.GetComponentInParent<SistemaSaludYTraje>();
                        if (objetivo == salud) continue;
                        puntoFinal = impacto.point;
                        if (objetivo == null)
                        {
                            FabricaEfectosVisuales.CrearImpacto(impacto.point, impacto.normal, arma.colorTrazador);
                            break; // La geometría detiene incluso a la bala perforante.
                        }
                        ProcesarImpactoEnEntidad(objetivo, arma, impacto, multiplicadorDano, conmocionador, ref huboImpacto, ref huboBaja);
                        atravesados++;
                        if (atravesados >= 3) break;
                    }
                }
                else if (Physics.Raycast(origen.position, direccion, out RaycastHit impacto, arma.alcanceMaximo, mascaraImpacto, QueryTriggerInteraction.Ignore))
                {
                    puntoFinal = impacto.point;
                    SistemaSaludYTraje objetivo = impacto.collider.GetComponentInParent<SistemaSaludYTraje>();
                    if (objetivo != null && objetivo != salud)
                    {
                        ProcesarImpactoEnEntidad(objetivo, arma, impacto, multiplicadorDano, conmocionador, ref huboImpacto, ref huboBaja);
                    }
                    else
                    {
                        FabricaEfectosVisuales.CrearImpacto(impacto.point, impacto.normal, arma.colorTrazador);
                    }
                }

                FabricaEfectosVisuales.CrearTrazador(boca, puntoFinal, arma.colorTrazador);
            }

            FabricaEfectosVisuales.CrearDestelloDisparo(boca, origen.forward, arma.colorTrazador);

            // Retroceso y apertura del cono de dispersión.
            dispersionActual = Mathf.Min(arma.dispersionMaxima, dispersionActual + arma.dispersionPorDisparo);
            movimiento?.AplicarRetroceso(arma.retrocesoVertical * UnityEngine.Random.Range(0.8f, 1.2f));

            AlDisparar?.Invoke();
            if (huboImpacto) AlConfirmarImpacto?.Invoke(huboBaja);
            NotificarMunicion();

            if (estado.BalasEnCargador <= 0)
            {
                if (estado.MunicionReserva > 0) IniciarRecarga();
                else ManejarSinMunicion();
            }
        }

        private void ProcesarImpactoEnEntidad(SistemaSaludYTraje objetivo, DatosArma arma, RaycastHit impacto,
            float multiplicadorDano, bool conmocionador, ref bool huboImpacto, ref bool huboBaja)
        {
            if (objetivo.EstaMuerto) return;
            bool esEnemigo = salud == null || objetivo.Bando != salud.Bando;

            float dano = arma.CalcularDanoPorDistancia(impacto.distance) * multiplicadorDano;
            InformacionDano info = InformacionDano.Crear(dano, TipoDano.Cinetico, salud, arma.nombreArma);
            info.Punto = impacto.point;
            objetivo.RecibirDano(info);

            FabricaEfectosVisuales.CrearImpacto(impacto.point, impacto.normal, esEnemigo ? PaletaVoidform.Alerta : arma.colorTrazador);

            if (conmocionador && esEnemigo)
            {
                SistemaEstadosAlterados estadosObjetivo = objetivo.GetComponent<SistemaEstadosAlterados>();
                if (estadosObjetivo != null)
                {
                    Vector3 empuje = (objetivo.transform.position - transform.position);
                    empuje.y = 0f;
                    estadosObjetivo.AplicarEmpuje(empuje.normalized * 10f + Vector3.up * 2f);
                    estadosObjetivo.AplicarRalentizacion(0.55f, 2f);
                }
            }

            if (esEnemigo)
            {
                huboImpacto = true;
                if (objetivo.EstaMuerto) huboBaja = true;
            }
        }

        private Transform ObtenerOrigenDisparo()
        {
            if (puntoDisparo != null) return puntoDisparo;
            if (movimiento != null && movimiento.PivoteCamara != null) return movimiento.PivoteCamara;
            return transform;
        }

        private float CalcularDispersionEfectiva(DatosArma arma)
        {
            float factor = 1f;
            if (movimiento != null)
            {
                factor *= Mathf.Lerp(1f, arma.multiplicadorDispersionMovimiento, movimiento.FactorMovimiento);
                if (!movimiento.EnSuelo) factor *= arma.multiplicadorDispersionAire;
            }
            return Mathf.Min(arma.dispersionMaxima * 2f, dispersionActual * factor);
        }

        private static Vector3 AplicarDispersion(Vector3 direccion, float grados)
        {
            if (grados <= 0.001f) return direccion;
            Vector2 aleatorio = UnityEngine.Random.insideUnitCircle * grados;
            Quaternion desviacion = Quaternion.Euler(aleatorio.y, aleatorio.x, 0f);
            return (Quaternion.LookRotation(direccion) * desviacion) * Vector3.forward;
        }

        private void ManejarSinMunicion()
        {
            int otra = indiceArmaActiva == 0 ? 1 : 0;
            if (arsenal[otra] != null && (arsenal[otra].BalasEnCargador > 0 || arsenal[otra].MunicionReserva > 0))
            {
                if (salud != null && salud.EsJugadorLocal)
                {
                    BusEventosJuego.PublicarMensaje($"Sin munición en {ArmaActiva.nombreArma}: cambiando de arma.", TipoMensajeSistema.Advertencia);
                }
                CambiarArma(otra);
            }
        }

        // ================== Nivel 2: recarga y cambio de arma ==================

        private void IniciarRecarga()
        {
            if (estaRecargando) return;
            rutinaRecargaActual = StartCoroutine(RutinaRecargar());
        }

        private void CancelarRecarga()
        {
            if (rutinaRecargaActual != null) StopCoroutine(rutinaRecargaActual);
            rutinaRecargaActual = null;
            estaRecargando = false;
        }

        private IEnumerator RutinaRecargar()
        {
            estaRecargando = true;
            NotificarMunicion();
            yield return new WaitForSeconds(ArmaActiva.tiempoRecarga);

            EstadoArma estado = arsenal[indiceArmaActiva];
            int necesarias = estado.Datos.tamanoCargador - estado.BalasEnCargador;
            int transferidas = Mathf.Min(necesarias, estado.MunicionReserva);
            estado.BalasEnCargador += transferidas;
            estado.MunicionReserva -= transferidas;

            estaRecargando = false;
            rutinaRecargaActual = null;
            NotificarMunicion();
        }

        public void CambiarArma(int indice)
        {
            indice = Mathf.Clamp(indice, 0, 1);
            if (indice == indiceArmaActiva || arsenal[indice] == null) return;
            CancelarRecarga();
            indiceArmaActiva = indice;
            dispersionActual = ArmaActiva.dispersionBase;
            proximoDisparoDisponible = Time.time + 0.25f; // Tiempo de desenfundar.
            NotificarArmaYMunicion();
        }

        // ================== Nivel 2-3: habilidades ==================

        public void IntentarActivarHabilidadTraje()
        {
            if (enfriamientoRestanteTraje > 0f) return;
            DatosTraje traje = Traje != null ? Traje : DatosTraje.CrearPorDefecto(TipoTraje.CineticoAsalto);

            enfriamientoRestanteTraje = traje.enfriamientoHabilidad;
            AlCambiarEnfriamientoTraje?.Invoke(enfriamientoRestanteTraje, traje.enfriamientoHabilidad);

            switch (traje.tipo)
            {
                case TipoTraje.CineticoAsalto:
                    if (movimiento != null) StartCoroutine(RutinaImpulsoTraje());
                    break;

                case TipoTraje.TermicoIgnifugo:
                    estados?.EstablecerInmunidad(TipoEstadoAlterado.Quemadura, 3f);
                    break;

                case TipoTraje.HidrostaticoAnclado:
                    estados?.EstablecerInmunidad(TipoEstadoAlterado.Ralentizado, 3f);
                    salud?.AnadirBlindajeTemporal(20f);
                    break;

                case TipoTraje.SismicoBlindado:
                    salud?.RestaurarBlindaje(35f);
                    break;

                case TipoTraje.PodadorBiotecnico:
                    estados?.Limpiar(TipoEstadoAlterado.Enraizado);
                    EjecutarSierraDePoda();
                    break;
            }

            FabricaEfectosVisuales.CrearAnilloOnda(transform.position + Vector3.up * 0.1f, 2.5f, traje.colorTraje);
            AlActivarHabilidad?.Invoke($"{traje.nombreTraje}: {traje.nombreHabilidad}");
        }

        private IEnumerator RutinaImpulsoTraje()
        {
            movimiento.EstablecerMultiplicadorVelocidad(multiplicadorImpulsoTraje);
            yield return new WaitForSeconds(duracionImpulsoTraje);
            movimiento.EstablecerMultiplicadorVelocidad(1f);
        }

        private void EjecutarSierraDePoda()
        {
            HashSet<SistemaSaludYTraje> alcanzados = new HashSet<SistemaSaludYTraje>();
            foreach (Collider col in Physics.OverlapSphere(transform.position, radioSierraPoda, ~0, QueryTriggerInteraction.Ignore))
            {
                SistemaSaludYTraje objetivo = col.GetComponentInParent<SistemaSaludYTraje>();
                if (objetivo == null || objetivo == salud || objetivo.EstaMuerto || objetivo.Bando == BandoJuego.Destructor) continue;
                if (!alcanzados.Add(objetivo)) continue;

                float dano = danoSierraPoda;
                ControladorGuardianElemental guardian = objetivo.GetComponent<ControladorGuardianElemental>();
                if (guardian != null && guardian.ElementoActual == TipoElemento.Arbol) dano *= 1.5f;
                objetivo.RecibirDano(InformacionDano.Crear(dano, TipoDano.Cinetico, salud, "Sierra de Poda"));
            }
        }

        public void IntentarActivarHabilidadArma()
        {
            if (enfriamientoRestanteArma > 0f) return;
            DatosArma arma = ArmaActiva;
            enfriamientoRestanteArma = arma.enfriamientoHabilidad;
            AlCambiarEnfriamientoArma?.Invoke(enfriamientoRestanteArma, arma.enfriamientoHabilidad);

            switch (arma.tipo)
            {
                case TipoArma.PistolaAuxiliar:
                    multiplicadorSiguienteDisparo = 3f;
                    break;

                case TipoArma.SubfusilRafaga:
                    rafagaSupresoraHasta = Time.time + 3f;
                    break;

                case TipoArma.RifleExtractor:
                    CancelarRecarga();
                    arsenal[indiceArmaActiva].BalasEnCargador = arma.tamanoCargador;
                    salud?.Curar(15f);
                    GestorEquilibrioEcosistema.Instancia?.RegistrarExtraccion(3f, "Descarga de Extracción");
                    FabricaEfectosVisuales.CrearEfectoElemental(TipoElemento.Tierra, transform.position, 1.5f);
                    break;

                case TipoArma.EscopetaDispersora:
                    siguienteDisparoConmocionador = true;
                    break;

                case TipoArma.RiflePrecision:
                    multiplicadorSiguienteDisparo = 2f;
                    siguienteDisparoPerforante = true;
                    break;
            }

            AlActivarHabilidad?.Invoke($"{arma.nombreArma}: {arma.nombreHabilidad}");
            NotificarMunicion();
        }

        // ================== Notificaciones a la interfaz ==================

        private void NotificarMunicion()
        {
            AlCambiarMunicion?.Invoke(BalasEnCargador, TamanoCargador, MunicionReserva, estaRecargando);
        }

        private void NotificarArmaYMunicion()
        {
            AlCambiarArma?.Invoke(ArmaActiva);
            NotificarMunicion();
        }

        /// <summary>Reenvía el estado completo (la interfaz lo llama al suscribirse).</summary>
        public void ForzarNotificacionCompleta()
        {
            NotificarArmaYMunicion();
            AlCambiarEnfriamientoTraje?.Invoke(enfriamientoRestanteTraje, Traje != null ? Traje.enfriamientoHabilidad : 6f);
            AlCambiarEnfriamientoArma?.Invoke(enfriamientoRestanteArma, ArmaActiva != null ? ArmaActiva.enfriamientoHabilidad : 10f);
        }
    }
}

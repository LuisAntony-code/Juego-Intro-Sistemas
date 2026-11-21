using System.Collections.Generic;
using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>Objetivo táctico general que recibe cada bot según el modo de juego.</summary>
    public enum ObjetivoBot
    {
        CazarEnemigos,
        CapturarBases,
        ExtraerFuente,
        DefenderFuente
    }

    /// <summary>
    /// Inteligencia artificial de los bots/mods que rellenan equipos (Destructores) o que actúan como
    /// guardianes/jefes. Escribe en EntradaPersonaje exactamente igual que un jugador humano, así que
    /// usa las mismas reglas de movimiento, armas, maná y enfriamientos (no hace trampas).
    /// -----------------------------------------------------------------
    /// Nivel 1: buscar al enemigo vivo más cercano y moverse hacia él.
    /// Nivel 2: percepción con línea de visión, tiempo de reacción, error de puntería según la
    ///          dificultad, disparo, recarga automática y ataque cuerpo a cuerpo de guardianes.
    /// Nivel 3: objetivos por modo (capturar bases, extraer o defender la fuente), esquiva lateral,
    ///          evasión de obstáculos y desatasco, uso táctico de habilidades respetando el maná
    ///          y, para Aethernum, rotación al elemento que más daño hace a los trajes enemigos.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(EntradaPersonaje))]
    [RequireComponent(typeof(ControladorMovimientoAssaultCube))]
    [RequireComponent(typeof(SistemaSaludYTraje))]
    public class ControladorBotIA : MonoBehaviour
    {
        [Header("1. Percepción")]
        [SerializeField] private float radioDeteccion = 48f;
        [SerializeField] private float intervaloReevaluacion = 0.3f;

        [Header("2. Dificultad (0 = fácil, 1 = experto)")]
        [SerializeField] [Range(0f, 1f)] private float dificultad = 0.5f;

        [Header("3. Objetivo Táctico")]
        [SerializeField] private ObjetivoBot objetivo = ObjetivoBot.CazarEnemigos;

        private EntradaPersonaje entrada;
        private ControladorMovimientoAssaultCube movimiento;
        private SistemaSaludYTraje salud;
        private SistemaArmasYTrajeDestructor armas;
        private ControladorGuardianElemental guardian;
        private SistemaEstadosAlterados estados;

        private SistemaSaludYTraje enemigoActual;
        private bool enemigoVisible;
        private float momentoAdquisicion;
        private float proximaReevaluacion;
        private Vector3 destinoActual;
        private Vector3 puntoFuente;
        private ZonaCapturaBase baseObjetivo;
        private float faseEsquiva;
        private float tiempoAtascado;
        private float desvioHasta;
        private Vector3 direccionDesvio;
        private float proximaHabilidad;
        private float proximaRotacionElemento;
        private Vector3 errorPunteriaActual;
        private float proximoCambioError;

        public ObjetivoBot Objetivo => objetivo;

        private float TiempoReaccion => Mathf.Lerp(0.65f, 0.15f, dificultad);
        private float ErrorPunteriaGrados => Mathf.Lerp(5f, 0.8f, dificultad);
        private float VelocidadGiro => Mathf.Lerp(220f, 620f, dificultad);

        private void Awake()
        {
            entrada = GetComponent<EntradaPersonaje>();
            movimiento = GetComponent<ControladorMovimientoAssaultCube>();
            salud = GetComponent<SistemaSaludYTraje>();
            armas = GetComponent<SistemaArmasYTrajeDestructor>();
            guardian = GetComponent<ControladorGuardianElemental>();
            estados = GetComponent<SistemaEstadosAlterados>();
            faseEsquiva = Random.Range(0f, 10f);
        }

        public void Configurar(ObjetivoBot nuevoObjetivo, float nuevaDificultad, Vector3 posicionFuente)
        {
            objetivo = nuevoObjetivo;
            dificultad = Mathf.Clamp01(nuevaDificultad);
            puntoFuente = posicionFuente;
        }

        private void Update()
        {
            if (salud.EstaMuerto || !entrada.EntradaHabilitada)
            {
                entrada.LimpiarTodo();
                enemigoActual = null;
                return;
            }

            if (armas == null) armas = GetComponent<SistemaArmasYTrajeDestructor>();
            if (guardian == null) guardian = GetComponent<ControladorGuardianElemental>();

            if (Time.time >= proximaReevaluacion)
            {
                proximaReevaluacion = Time.time + intervaloReevaluacion;
                ReevaluarEnemigo();
                destinoActual = CalcularDestino();
            }

            ActualizarErrorPunteria();
            ProcesarMovimiento();
            ProcesarCombate();
        }

        // =====================================================================
        // Nivel 1-2: percepción
        // =====================================================================

        private void ReevaluarEnemigo()
        {
            SistemaSaludYTraje mejor = null;
            float mejorPuntuacion = float.MaxValue;
            bool mejorVisible = false;

            foreach (SistemaSaludYTraje entidad in SistemaSaludYTraje.Entidades)
            {
                if (entidad == null || entidad.EstaMuerto || entidad.Bando == salud.Bando) continue;
                float distancia = Vector3.Distance(transform.position, entidad.transform.position);
                if (distancia > radioDeteccion * 1.5f) continue;

                bool visible = distancia <= radioDeteccion && TieneLineaDeVision(entidad);
                // Se prefiere a los visibles; entre ellos, al más cercano. Los guardianes que defienden
                // la fuente priorizan a quien está extrayendo.
                float puntuacion = distancia + (visible ? 0f : 40f);
                if (objetivo == ObjetivoBot.DefenderFuente && Vector3.Distance(entidad.transform.position, puntoFuente) < 9f) puntuacion -= 25f;

                if (puntuacion < mejorPuntuacion)
                {
                    mejorPuntuacion = puntuacion;
                    mejor = entidad;
                    mejorVisible = visible;
                }
            }

            if (mejor != enemigoActual || (mejorVisible && !enemigoVisible))
            {
                momentoAdquisicion = Time.time;
            }
            enemigoActual = mejor;
            enemigoVisible = mejorVisible;
        }

        private bool TieneLineaDeVision(SistemaSaludYTraje objetivoVision)
        {
            Vector3 origen = movimiento.PivoteCamara != null ? movimiento.PivoteCamara.position : transform.position + Vector3.up * 1.6f;
            Vector3 destino = objetivoVision.transform.position + Vector3.up * 1.2f;
            Vector3 direccion = destino - origen;
            if (Physics.Raycast(origen, direccion.normalized, out RaycastHit impacto, direccion.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                SistemaSaludYTraje golpeado = impacto.collider.GetComponentInParent<SistemaSaludYTraje>();
                return golpeado == objetivoVision;
            }
            return true;
        }

        private Vector3 PuntoApuntado(SistemaSaludYTraje objetivoApuntado)
        {
            float altura = objetivoApuntado.GetComponent<ControladorGuardianElemental>() != null ? 1.7f : 1.25f;
            return objetivoApuntado.transform.position + Vector3.up * altura;
        }

        private void ActualizarErrorPunteria()
        {
            if (Time.time < proximoCambioError) return;
            proximoCambioError = Time.time + Random.Range(0.25f, 0.6f);
            errorPunteriaActual = Random.insideUnitSphere * Mathf.Tan(ErrorPunteriaGrados * Mathf.Deg2Rad);
        }

        // =====================================================================
        // Nivel 3: objetivos por modo
        // =====================================================================

        private Vector3 CalcularDestino()
        {
            bool esGuardian = guardian != null;

            if (enemigoActual != null && enemigoVisible)
            {
                float distancia = Vector3.Distance(transform.position, enemigoActual.transform.position);
                if (objetivo == ObjetivoBot.CazarEnemigos || objetivo == ObjetivoBot.DefenderFuente || distancia < 16f || esGuardian)
                {
                    if (esGuardian) return enemigoActual.transform.position;
                    // Un humano mantiene distancia media (ritmo de arena shooter: no se lanza a quemarropa).
                    Vector3 alejamiento = (transform.position - enemigoActual.transform.position);
                    alejamiento.y = 0f;
                    float distanciaIdeal = armas != null && armas.ArmaActiva != null && armas.ArmaActiva.tipo == TipoArma.EscopetaDispersora ? 6f : 14f;
                    return enemigoActual.transform.position + alejamiento.normalized * distanciaIdeal;
                }
            }

            switch (objetivo)
            {
                case ObjetivoBot.CapturarBases:
                    baseObjetivo = ElegirBaseObjetivo();
                    if (baseObjetivo != null)
                    {
                        // Cada bot elige un punto distinto dentro de la base para no amontonarse.
                        Vector2 desplazamiento = new Vector2(Mathf.Sin(faseEsquiva), Mathf.Cos(faseEsquiva)) * 3f;
                        return baseObjetivo.transform.position + new Vector3(desplazamiento.x, 0f, desplazamiento.y);
                    }
                    break;

                case ObjetivoBot.ExtraerFuente:
                    return puntoFuente + new Vector3(Mathf.Sin(faseEsquiva) * 3.5f, 0f, Mathf.Cos(faseEsquiva) * 3.5f);

                case ObjetivoBot.DefenderFuente:
                    if (enemigoActual != null) return enemigoActual.transform.position;
                    return puntoFuente;
            }

            if (enemigoActual != null) return enemigoActual.transform.position;
            return puntoFuente;
        }

        private ZonaCapturaBase ElegirBaseObjetivo()
        {
            EstadoControlBase propia = salud.Bando == BandoJuego.Destructor ? EstadoControlBase.DominadaDestructores : EstadoControlBase.DominadaCuidadores;
            ZonaCapturaBase mejor = null;
            float mejorDistancia = float.MaxValue;

            foreach (ZonaCapturaBase b in ZonaCapturaBase.BasesActivas)
            {
                if (b == null || b.EstadoActual == propia) continue;
                // Pequeño sesgo por bot para repartir al equipo entre varias bases.
                float d = Vector3.Distance(transform.position, b.transform.position) + Mathf.Abs(Mathf.Sin(faseEsquiva + b.IndiceBase)) * 18f;
                if (d < mejorDistancia)
                {
                    mejorDistancia = d;
                    mejor = b;
                }
            }

            if (mejor == null && ZonaCapturaBase.BasesActivas.Count > 0)
            {
                // Todas son nuestras: defender la que esté en disputa o la central.
                foreach (ZonaCapturaBase b in ZonaCapturaBase.BasesActivas)
                {
                    if (b != null && b.SiendoDisputada) return b;
                }
                return ZonaCapturaBase.BasesActivas[Mathf.Abs((int)faseEsquiva) % ZonaCapturaBase.BasesActivas.Count];
            }
            return mejor;
        }

        // =====================================================================
        // Nivel 2-3: movimiento con evasión de obstáculos
        // =====================================================================

        private void ProcesarMovimiento()
        {
            Vector3 haciaDestino = destinoActual - transform.position;
            haciaDestino.y = 0f;
            float distancia = haciaDestino.magnitude;
            Vector3 direccionMundo = distancia > 1.2f ? haciaDestino / distancia : Vector3.zero;

            // Esquiva lateral mientras combate (más intensa con mayor dificultad).
            if (enemigoActual != null && enemigoVisible)
            {
                float esquiva = Mathf.Sin(Time.time * 1.7f + faseEsquiva) * Mathf.Lerp(0.3f, 0.85f, dificultad);
                direccionMundo += transform.right * esquiva;
                if (direccionMundo.sqrMagnitude > 1f) direccionMundo.Normalize();
            }

            if (Time.time < desvioHasta)
            {
                direccionMundo = direccionDesvio;
            }
            else if (direccionMundo.sqrMagnitude > 0.01f)
            {
                direccionMundo = EvitarObstaculos(direccionMundo);
            }

            // Detección de atasco: quiere moverse pero apenas avanza.
            if (direccionMundo.sqrMagnitude > 0.25f && movimiento.VelocidadActual < 1f && !movimiento.EstaEnraizado)
            {
                tiempoAtascado += Time.deltaTime;
                if (tiempoAtascado > 1.1f)
                {
                    tiempoAtascado = 0f;
                    desvioHasta = Time.time + 1f;
                    direccionDesvio = Quaternion.Euler(0f, Random.Range(0, 2) == 0 ? 100f : -100f, 0f) * direccionMundo;
                    entrada.SaltoPresionado = true;
                }
            }
            else
            {
                tiempoAtascado = 0f;
            }

            Vector3 local = transform.InverseTransformDirection(direccionMundo);
            entrada.Movimiento = Vector2.ClampMagnitude(new Vector2(local.x, local.z), 1f);

            // Orientación: al enemigo si está visible, si no hacia donde camina.
            if (enemigoActual != null && enemigoVisible)
            {
                Vector3 punto = PuntoApuntado(enemigoActual);
                float distanciaObjetivo = Vector3.Distance(transform.position, punto);
                movimiento.MirarHaciaPunto(punto + errorPunteriaActual * distanciaObjetivo, VelocidadGiro);
            }
            else if (direccionMundo.sqrMagnitude > 0.01f)
            {
                movimiento.MirarHaciaPunto(transform.position + direccionMundo * 10f + Vector3.up * 1.5f, VelocidadGiro * 0.6f);
            }
        }

        private Vector3 EvitarObstaculos(Vector3 direccion)
        {
            Vector3 origenBajo = transform.position + Vector3.up * 0.6f;
            Vector3 origenAlto = transform.position + Vector3.up * 1.7f;
            const float distanciaSondeo = 2.6f;

            if (!Physics.SphereCast(origenBajo, 0.35f, direccion, out RaycastHit choque, distanciaSondeo, ~0, QueryTriggerInteraction.Ignore)
                || choque.collider.GetComponentInParent<SistemaSaludYTraje>() != null)
            {
                return direccion;
            }

            // Obstáculo bajo (tronco caído, roca): saltar por encima.
            if (!Physics.Raycast(origenAlto, direccion, distanciaSondeo, ~0, QueryTriggerInteraction.Ignore))
            {
                entrada.SaltoPresionado = true;
                return direccion;
            }

            float[] angulos = { 45f, -45f, 90f, -90f, 135f, -135f };
            foreach (float angulo in angulos)
            {
                Vector3 alternativa = Quaternion.Euler(0f, angulo, 0f) * direccion;
                if (!Physics.SphereCast(origenBajo, 0.35f, alternativa, out _, distanciaSondeo, ~0, QueryTriggerInteraction.Ignore))
                {
                    return alternativa;
                }
            }
            return -direccion;
        }

        // =====================================================================
        // Nivel 2-3: combate y habilidades
        // =====================================================================

        private void ProcesarCombate()
        {
            entrada.AtaquePrincipalMantenido = false;

            bool puedeReaccionar = enemigoActual != null && enemigoVisible && Time.time - momentoAdquisicion >= TiempoReaccion;
            if (!puedeReaccionar)
            {
                UsarHabilidadesDefensivas();
                return;
            }

            float distancia = Vector3.Distance(transform.position, enemigoActual.transform.position);
            Vector3 adelante = movimiento.PivoteCamara != null ? movimiento.PivoteCamara.forward : transform.forward;
            float angulo = Vector3.Angle(adelante, PuntoApuntado(enemigoActual) - (movimiento.PivoteCamara != null ? movimiento.PivoteCamara.position : transform.position));

            if (armas != null)
            {
                DatosArma arma = armas.ArmaActiva;
                bool enAlcance = arma == null || distancia <= arma.alcanceMaximo * 0.9f;
                if (enAlcance && angulo < 6f + ErrorPunteriaGrados)
                {
                    entrada.AtaquePrincipalMantenido = true;
                    entrada.AtaquePrincipalPresionado = true;
                }
                UsarHabilidadesDestructor(distancia);
            }
            else if (guardian != null)
            {
                if (distancia <= guardian.AlcanceAtaqueBasico + 1.2f && angulo < 50f)
                {
                    entrada.AtaquePrincipalPresionado = true;
                }
                UsarHabilidadesGuardian();
            }
        }

        private void UsarHabilidadesDefensivas()
        {
            if (Time.time < proximaHabilidad) return;
            if (armas != null) UsarHabilidadesDestructor(999f);
            else if (guardian != null && salud.PorcentajeVida < 0.5f && guardian.PuedeLanzar(ControladorGuardianElemental.SlotHabilidad2)
                     && (guardian.ElementoActual == TipoElemento.Arbol || guardian.ElementoActual == TipoElemento.Tierra))
            {
                entrada.Habilidad2Presionada = true;
                proximaHabilidad = Time.time + 1f;
            }
        }

        private void UsarHabilidadesDestructor(float distanciaEnemigo)
        {
            if (Time.time < proximaHabilidad) return;
            DatosTraje traje = salud.Traje;
            bool usarTraje = false;

            if (traje != null && armas.EnfriamientoRestanteTraje <= 0f)
            {
                switch (traje.tipo)
                {
                    case TipoTraje.CineticoAsalto: usarTraje = distanciaEnemigo > 20f && distanciaEnemigo < 200f; break;
                    case TipoTraje.TermicoIgnifugo: usarTraje = estados != null && estados.TieneEstado(TipoEstadoAlterado.Quemadura); break;
                    case TipoTraje.HidrostaticoAnclado: usarTraje = estados != null && estados.TieneEstado(TipoEstadoAlterado.Ralentizado) || salud.PorcentajeVida < 0.5f; break;
                    case TipoTraje.SismicoBlindado: usarTraje = salud.BlindajeActual < salud.BlindajeMaximo * 0.4f; break;
                    case TipoTraje.PodadorBiotecnico: usarTraje = (estados != null && estados.TieneEstado(TipoEstadoAlterado.Enraizado)) || distanciaEnemigo < 4.5f; break;
                }
            }

            if (usarTraje)
            {
                entrada.Habilidad1Presionada = true;
                proximaHabilidad = Time.time + 0.8f;
                return;
            }

            if (armas.EnfriamientoRestanteArma <= 0f && distanciaEnemigo < 60f)
            {
                bool rifleCurativo = armas.ArmaActiva != null && armas.ArmaActiva.tipo == TipoArma.RifleExtractor;
                if (!rifleCurativo || salud.PorcentajeVida < 0.6f || armas.BalasEnCargador < 5)
                {
                    entrada.HabilidadEspecialPresionada = true;
                    proximaHabilidad = Time.time + 0.8f;
                }
            }
        }

        private void UsarHabilidadesGuardian()
        {
            if (Time.time < proximaHabilidad) return;

            int enemigosCerca = 0;
            int enemigosAmplio = 0;
            foreach (SistemaSaludYTraje e in SistemaSaludYTraje.Entidades)
            {
                if (e == null || e.EstaMuerto || e.Bando == salud.Bando) continue;
                float d = Vector3.Distance(transform.position, e.transform.position);
                if (d <= guardian.RadioEfecto * 0.85f) enemigosCerca++;
                if (d <= guardian.RadioEfecto * 1.4f) enemigosAmplio++;
            }

            if (guardian.PuedeRotarElementos && Time.time >= proximaRotacionElemento)
            {
                proximaRotacionElemento = Time.time + Random.Range(10f, 16f);
                TipoElemento ideal = ElementoMasEficaz();
                if (ideal != guardian.ElementoActual)
                {
                    entrada.CambiarElementoPresionado = true;
                    proximaHabilidad = Time.time + 0.5f;
                    return;
                }
            }

            if (enemigosAmplio >= 2 && guardian.PuedeLanzar(ControladorGuardianElemental.SlotDefinitiva))
            {
                entrada.HabilidadEspecialPresionada = true;
            }
            else if (enemigosCerca >= 1 && guardian.PuedeLanzar(ControladorGuardianElemental.SlotHabilidad1))
            {
                entrada.Habilidad1Presionada = true;
            }
            else if (guardian.PuedeLanzar(ControladorGuardianElemental.SlotHabilidad2))
            {
                bool curativo = guardian.ElementoActual == TipoElemento.Arbol;
                if ((curativo && salud.PorcentajeVida < 0.7f) || (!curativo && enemigosCerca >= 1))
                {
                    entrada.Habilidad2Presionada = true;
                }
            }
            proximaHabilidad = Time.time + Mathf.Lerp(1.2f, 0.45f, dificultad);
        }

        /// <summary>Elige el elemento que más daño hace a los trajes enemigos presentes.</summary>
        private TipoElemento ElementoMasEficaz()
        {
            float[] puntuacion = new float[4];
            foreach (SistemaSaludYTraje e in SistemaSaludYTraje.Entidades)
            {
                if (e == null || e.EstaMuerto || e.Bando == salud.Bando || e.Traje == null) continue;
                for (int i = 0; i < 4; i++)
                {
                    puntuacion[i] += e.Traje.ObtenerMultiplicador(UtilidadesTipos.ElementoATipoDano((TipoElemento)i));
                }
            }

            int mejor = (int)guardian.ElementoActual;
            for (int i = 0; i < 4; i++)
            {
                if (puntuacion[i] > puntuacion[mejor] + 0.01f) mejor = i;
            }
            // La rotación avanza de uno en uno: si el ideal no es el siguiente, se pulsa igual y se
            // acerca en la próxima evaluación (el ciclo completo siempre llega al ideal).
            return (TipoElemento)mejor;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, radioDeteccion);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, destinoActual);
        }
    }
}

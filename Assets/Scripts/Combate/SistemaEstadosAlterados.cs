using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Aplica y hace caducar los estados alterados de un personaje:
    /// Ralentizado (Agua/Tierra), Enraizado (Árbol), Quemadura (Fuego) y Empujado (Agua/Tierra/explosiones).
    /// -----------------------------------------------------------------
    /// Nivel 1: cada estado tiene una duración y se elimina al expirar.
    /// Nivel 2: la ralentización más fuerte manda; la quemadura hace daño por segundo;
    ///          el empuje se transmite al controlador de movimiento.
    /// Nivel 3: inmunidades temporales (habilidades de traje), reducción de duración según el traje,
    ///          inmunidad total al empuje del Traje Hidrostático y evento de cambio para el HUD.
    /// </summary>
    public class SistemaEstadosAlterados : MonoBehaviour
    {
        private class EstadoActivo
        {
            public float FinTiempo;
            public float Intensidad;
            public InformacionDano Fuente;
        }

        private readonly Dictionary<TipoEstadoAlterado, EstadoActivo> estados = new Dictionary<TipoEstadoAlterado, EstadoActivo>();
        private readonly Dictionary<TipoEstadoAlterado, float> inmunidades = new Dictionary<TipoEstadoAlterado, float>();

        private ControladorMovimientoAssaultCube movimiento;
        private SistemaSaludYTraje salud;
        private float acumuladorQuemadura;
        private string ultimaDescripcion = "";

        /// <summary>Texto con los estados activos, por ejemplo "Enraizado 1,2 s · Quemadura 2,0 s".</summary>
        public event Action<string> AlCambiarEstados;

        public bool TieneEstado(TipoEstadoAlterado tipo) => estados.ContainsKey(tipo);

        private void Awake()
        {
            movimiento = GetComponent<ControladorMovimientoAssaultCube>();
            salud = GetComponent<SistemaSaludYTraje>();
        }

        private void Update()
        {
            if (estados.Count == 0)
            {
                PublicarSiCambio();
                return;
            }

            if (salud != null && salud.EstaMuerto)
            {
                LimpiarTodos();
                return;
            }

            // Quemadura: daño por segundo acumulado y aplicado en "ticks" de 0,25 s.
            if (estados.TryGetValue(TipoEstadoAlterado.Quemadura, out EstadoActivo quemadura))
            {
                acumuladorQuemadura += Time.deltaTime;
                if (acumuladorQuemadura >= 0.25f)
                {
                    InformacionDano tick = quemadura.Fuente;
                    tick.Cantidad = quemadura.Intensidad * acumuladorQuemadura;
                    // El tipo de daño lo define la fuente: Fuego en las quemaduras de los guardianes,
                    // Cinético en el sangrado de las cuchillas biomecánicas.
                    if (!quemadura.Fuente.TieneAtacante && quemadura.Fuente.Tipo == TipoDano.Cinetico) tick.Tipo = TipoDano.Fuego;
                    tick.Origen = string.IsNullOrEmpty(quemadura.Fuente.Origen) ? "Quemadura" : quemadura.Fuente.Origen;
                    acumuladorQuemadura = 0f;
                    salud?.RecibirDano(tick);
                }
            }

            List<TipoEstadoAlterado> expirados = null;
            foreach (KeyValuePair<TipoEstadoAlterado, EstadoActivo> par in estados)
            {
                if (Time.time >= par.Value.FinTiempo)
                {
                    if (expirados == null) expirados = new List<TipoEstadoAlterado>();
                    expirados.Add(par.Key);
                }
            }

            if (expirados != null)
            {
                foreach (TipoEstadoAlterado tipo in expirados) estados.Remove(tipo);
                AplicarEfectosAlMovimiento();
            }

            PublicarSiCambio();
        }

        // ================== API de aplicación ==================

        public void AplicarRalentizacion(float factorVelocidad, float duracion)
        {
            // factorVelocidad 0.6 => se mueve al 60%. La intensidad guardada es la reducción (0.4).
            float reduccion = Mathf.Clamp01(1f - factorVelocidad);
            if (!IntentarAplicar(TipoEstadoAlterado.Ralentizado, duracion, reduccion, default)) return;
            AplicarEfectosAlMovimiento();
        }

        public void AplicarEnraizado(float duracion)
        {
            if (!IntentarAplicar(TipoEstadoAlterado.Enraizado, duracion, 1f, default)) return;
            AplicarEfectosAlMovimiento();
        }

        public void AplicarQuemadura(float danoPorSegundo, float duracion, InformacionDano fuente)
        {
            IntentarAplicar(TipoEstadoAlterado.Quemadura, duracion, danoPorSegundo, fuente);
        }

        public void AplicarEmpuje(Vector3 impulso)
        {
            if (EstaInmune(TipoEstadoAlterado.Empujado)) return;
            if (salud != null && salud.Arquetipo != null && salud.Arquetipo.inmuneEmpuje) return;
            if (salud != null && salud.EsInvulnerable) return;
            movimiento?.AplicarImpulso(impulso);
        }

        public void EstablecerInmunidad(TipoEstadoAlterado tipo, float duracion)
        {
            inmunidades[tipo] = Mathf.Max(ObtenerFinInmunidad(tipo), Time.time + duracion);
            Limpiar(tipo);
        }

        public void Limpiar(TipoEstadoAlterado tipo)
        {
            if (estados.Remove(tipo))
            {
                AplicarEfectosAlMovimiento();
                PublicarSiCambio();
            }
        }

        public void LimpiarTodos()
        {
            estados.Clear();
            acumuladorQuemadura = 0f;
            AplicarEfectosAlMovimiento();
            PublicarSiCambio();
        }

        // ================== Lógica interna ==================

        private bool IntentarAplicar(TipoEstadoAlterado tipo, float duracion, float intensidad, InformacionDano fuente)
        {
            if (duracion <= 0f) return false;
            if (EstaInmune(tipo)) return false;
            if (salud != null && (salud.EstaMuerto || salud.EsInvulnerable)) return false;

            float reduccion = salud != null && salud.Arquetipo != null ? salud.Arquetipo.reduccionDuracionEstados : 0f;
            float duracionFinal = duracion * (1f - reduccion);
            float fin = Time.time + duracionFinal;

            if (estados.TryGetValue(tipo, out EstadoActivo existente))
            {
                // Se conserva lo más fuerte y lo más largo (no se acumulan sin límite).
                existente.FinTiempo = Mathf.Max(existente.FinTiempo, fin);
                existente.Intensidad = Mathf.Max(existente.Intensidad, intensidad);
                if (fuente.TieneAtacante) existente.Fuente = fuente;
            }
            else
            {
                estados[tipo] = new EstadoActivo { FinTiempo = fin, Intensidad = intensidad, Fuente = fuente };
            }

            PublicarSiCambio();
            return true;
        }

        private bool EstaInmune(TipoEstadoAlterado tipo)
        {
            return Time.time < ObtenerFinInmunidad(tipo);
        }

        private float ObtenerFinInmunidad(TipoEstadoAlterado tipo)
        {
            return inmunidades.TryGetValue(tipo, out float fin) ? fin : -1f;
        }

        private void AplicarEfectosAlMovimiento()
        {
            if (movimiento == null) return;
            float reduccion = estados.TryGetValue(TipoEstadoAlterado.Ralentizado, out EstadoActivo r) ? r.Intensidad : 0f;
            movimiento.EstablecerMultiplicadorEstados(1f - reduccion);
            movimiento.EstablecerEnraizado(estados.ContainsKey(TipoEstadoAlterado.Enraizado));
        }

        private void PublicarSiCambio()
        {
            string descripcion = "";
            foreach (KeyValuePair<TipoEstadoAlterado, EstadoActivo> par in estados)
            {
                float restante = Mathf.Max(0f, par.Value.FinTiempo - Time.time);
                if (descripcion.Length > 0) descripcion += " · ";
                descripcion += $"{UtilidadesTipos.NombreEstadoAlterado(par.Key)} {restante:0.0} s";
            }

            if (descripcion != ultimaDescripcion)
            {
                ultimaDescripcion = descripcion;
                AlCambiarEstados?.Invoke(descripcion);
            }
        }
    }
}

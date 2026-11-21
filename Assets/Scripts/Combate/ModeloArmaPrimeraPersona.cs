using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>
    /// Arma visible en primera persona para el humano local, con animación propia (vista estilo arena shooter).
    /// Nivel 1: el arma modular y los guanteletes delante de la cámara.
    /// Nivel 2: balanceo al caminar, retroceso al disparar, tensado del arco, recarga y bloqueo.
    /// Nivel 3: tajos y mazazos en tres tiempos (anticipación, impacto y seguimiento) sincronizados con los eventos
    ///          del SistemaCombateArquetipo, de modo que lo que se ve coincide con el momento en que el golpe hace daño.
    /// </summary>
    public class ModeloArmaPrimeraPersona : MonoBehaviour
    {
        private SistemaCombateArquetipo combate;
        private ControladorMovimientoAssaultCube movimiento;
        private Transform pivoteDerecho;
        private Transform pivoteIzquierdo;
        private Vector3 reposoDerecho = new Vector3(0.3f, -0.3f, 0.55f);
        private Vector3 reposoIzquierdo = new Vector3(-0.3f, -0.32f, 0.5f);
        private float retroceso;
        private float faseBalanceo;
        private TipoAccionCombate accion;
        private float inicioAccion = -10f;
        private float duracionAccion = 0.4f;

        public static ModeloArmaPrimeraPersona Crear(Transform camara, SistemaCombateArquetipo sistemaCombate, ControladorMovimientoAssaultCube mov)
        {
            GameObject go = new GameObject("Arma en Primera Persona");
            go.transform.SetParent(camara, false);
            ModeloArmaPrimeraPersona m = go.AddComponent<ModeloArmaPrimeraPersona>();
            m.combate = sistemaCombate;
            m.movimiento = mov;
            m.Construir();
            sistemaCombate.AlAccion += m.ManejarAccion;
            return m;
        }

        private void OnDestroy()
        {
            if (combate != null) combate.AlAccion -= ManejarAccion;
        }

        private void Construir()
        {
            pivoteDerecho = new GameObject("Mano derecha").transform;
            pivoteDerecho.SetParent(transform, false);
            pivoteIzquierdo = new GameObject("Mano izquierda").transform;
            pivoteIzquierdo.SetParent(transform, false);

            DatosArmaModular arma = combate.Arma;
            Color guante = combate.Arquetipo != null ? combate.Arquetipo.colorSecundario : new Color(0.3f, 0.2f, 0.12f);
            CrearGuante(pivoteDerecho, guante);
            CrearGuante(pivoteIzquierdo, guante);

            switch (arma.tipo)
            {
                case TipoArmaModular.ArcoCompuesto:
                    reposoIzquierdo = new Vector3(-0.12f, -0.18f, 0.6f);
                    reposoDerecho = new Vector3(0.18f, -0.26f, 0.45f);
                    ConstructorModelosArmas.Construir(arma.tipo, pivoteIzquierdo, true);
                    break;
                case TipoArmaModular.RifleLargo:
                    reposoDerecho = new Vector3(0.22f, -0.24f, 0.45f);
                    reposoIzquierdo = new Vector3(0.12f, -0.26f, 0.8f);
                    ConstructorModelosArmas.Construir(arma.tipo, pivoteDerecho, true);
                    break;
                case TipoArmaModular.MazaEscudo:
                    ConstructorModelosArmas.Construir(arma.tipo, pivoteDerecho, true);
                    GameObject escudo = ConstructorModelosArmas.ConstruirEscudo(pivoteIzquierdo, true);
                    escudo.transform.localPosition = new Vector3(0.05f, 0.05f, 0.1f);
                    break;
                case TipoArmaModular.ArsenalBiomecanico:
                    ConstructorModelosArmas.Construir(arma.tipo, pivoteDerecho, true);
                    ConstructorModelosArmas.ConstruirCanonPulso(pivoteIzquierdo, true);
                    break;
                default:
                    reposoDerecho = new Vector3(0.28f, -0.3f, 0.5f);
                    ConstructorModelosArmas.Construir(arma.tipo, pivoteDerecho, true).transform.localRotation = Quaternion.Euler(-35f, -10f, 0f);
                    break;
            }
            pivoteDerecho.localPosition = reposoDerecho;
            pivoteIzquierdo.localPosition = reposoIzquierdo;
        }

        private static void CrearGuante(Transform padre, Color color)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = "Guantelete";
            Object.DestroyImmediate(g.GetComponent<Collider>());
            g.transform.SetParent(padre, false);
            g.transform.localPosition = new Vector3(0f, -0.02f, -0.06f);
            g.transform.localScale = new Vector3(0.08f, 0.09f, 0.16f);
            Renderer r = g.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            MaterialesProcedurales.AplicarMaterialRealista(r, color, Color.black, "cuero");
        }

        private void ManejarAccion(TipoAccionCombate tipo)
        {
            accion = tipo;
            inicioAccion = Time.time;
            duracionAccion = tipo == TipoAccionCombate.Disparo || tipo == TipoAccionCombate.SoltarFlecha ? 0.2f
                : tipo == TipoAccionCombate.Recarga ? (combate.Arma.tiempoRecarga > 0f ? combate.Arma.tiempoRecarga : 1f)
                : Mathf.Max(0.25f, combate.Arma.intervalo * 1.1f);
            if (tipo == TipoAccionCombate.Disparo || tipo == TipoAccionCombate.SoltarFlecha) retroceso = 1f;
        }

        private void LateUpdate()
        {
            if (pivoteDerecho == null || combate == null) return;
            float dt = Time.deltaTime;
            retroceso = Mathf.MoveTowards(retroceso, 0f, dt * 6f);
            float velocidad = movimiento != null ? movimiento.FactorMovimiento : 0f;
            faseBalanceo += dt * Mathf.Lerp(1.5f, 9f, velocidad);
            Vector3 balanceo = new Vector3(Mathf.Sin(faseBalanceo) * 0.012f, Mathf.Abs(Mathf.Cos(faseBalanceo)) * 0.012f, 0f) * (0.3f + velocidad);

            Vector3 posD = reposoDerecho + balanceo + new Vector3(0f, 0f, -retroceso * 0.07f);
            Vector3 posI = reposoIzquierdo + balanceo;
            Quaternion rotD = Quaternion.Euler(-retroceso * 7f, 0f, 0f);
            Quaternion rotI = Quaternion.identity;

            float t = (Time.time - inicioAccion) / duracionAccion;
            if (t >= 0f && t < 1f)
            {
                float impacto = combate.Arma.momentoImpacto;
                float s = t < impacto ? -Mathf.SmoothStep(0f, 1f, t / impacto) * 0.6f : Mathf.SmoothStep(-0.6f, 1f, (t - impacto) / 0.25f);
                float salida = t > impacto + 0.25f ? 1f - (t - impacto - 0.25f) / Mathf.Max(0.01f, 1f - impacto - 0.25f) : 1f;
                switch (accion)
                {
                    case TipoAccionCombate.GolpeHorizontal:
                        rotD = Quaternion.Euler(0f, -70f * s * salida, -20f * salida);
                        posD += new Vector3(-0.15f * s * salida, 0.05f, 0.05f * salida);
                        break;
                    case TipoAccionCombate.GolpeDiagonal:
                        rotD = Quaternion.Euler(60f * s * salida, -40f * s * salida, 25f * salida);
                        posD += new Vector3(-0.1f * s * salida, 0.08f * (1f - s), 0f);
                        break;
                    case TipoAccionCombate.Estocada:
                        posD += new Vector3(-0.08f, 0.05f, 0.3f * Mathf.Max(0f, s) * salida - 0.1f * Mathf.Max(0f, -s));
                        break;
                    case TipoAccionCombate.GolpeDescendente:
                        rotD = Quaternion.Euler(Mathf.Lerp(-60f, 70f, (s + 0.6f) / 1.6f) * salida, 0f, 0f);
                        break;
                    case TipoAccionCombate.Recarga:
                        posD += new Vector3(0f, -0.18f * Mathf.Sin(t * Mathf.PI), 0f);
                        rotD = Quaternion.Euler(25f * Mathf.Sin(t * Mathf.PI), 0f, 20f * Mathf.Sin(t * Mathf.PI));
                        break;
                    case TipoAccionCombate.HabilidadCorta:
                    case TipoAccionCombate.HabilidadMedia:
                    case TipoAccionCombate.HabilidadLarga:
                        posI += new Vector3(0.1f, 0.1f, 0.15f) * Mathf.Sin(t * Mathf.PI);
                        break;
                }
            }

            if (combate.CargandoArco)
            {
                // Tensar: la mano derecha retrocede hacia la mejilla y el arco tiembla en carga máxima.
                float carga = combate.CargaArco;
                posD += new Vector3(-0.05f, 0.08f, -0.25f * carga);
                posI += Random.insideUnitSphere * (carga >= 1f ? 0.002f : 0f);
            }
            if (combate.Bloqueando)
            {
                posI += new Vector3(0.18f, 0.12f, 0.05f);
                posD += new Vector3(-0.1f, 0.12f, 0f);
                rotD *= Quaternion.Euler(-20f, -30f, -60f);
            }
            if (combate.Apuntando && combate.Arma.tipo == TipoArmaModular.RifleLargo)
            {
                posD = new Vector3(0f, -0.17f, 0.4f) + balanceo * 0.3f;
            }

            float suave = 1f - Mathf.Exp(-22f * dt);
            pivoteDerecho.localPosition = Vector3.Lerp(pivoteDerecho.localPosition, posD, suave);
            pivoteDerecho.localRotation = Quaternion.Slerp(pivoteDerecho.localRotation, rotD, suave);
            pivoteIzquierdo.localPosition = Vector3.Lerp(pivoteIzquierdo.localPosition, posI, suave);
            pivoteIzquierdo.localRotation = Quaternion.Slerp(pivoteIzquierdo.localRotation, rotI, suave);
        }
    }
}

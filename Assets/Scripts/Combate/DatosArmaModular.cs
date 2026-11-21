using UnityEngine;

namespace VoidformBioForge
{
    /// <summary>Forma en que el arma aplica su ataque básico.</summary>
    public enum ModoAtaqueArma
    {
        CuerpoACuerpo,
        Proyectil,
        Rayo
    }

    /// <summary>
    /// Arma modular combinable con cualquiera de los 5 arquetipos.
    /// -----------------------------------------------------------------
    /// Nivel 1: identidad, daño, alcance y modo (cuerpo a cuerpo, proyectil o rayo).
    /// Nivel 2: combo, cadencia, munición, carga, bloqueo y precisión.
    /// Nivel 3: 3 habilidades del arma (corta E, media Q, larga F) con sus enfriamientos.
    /// </summary>
    [CreateAssetMenu(fileName = "NuevaArmaModular", menuName = "Voidform/Datos de Arma Modular")]
    public class DatosArmaModular : ScriptableObject
    {
        [Header("1. Identidad")]
        public TipoArmaModular tipo = TipoArmaModular.Mandoble;
        public string nombre = "Mandoble / Espada Larga";
        [TextArea(2, 5)] public string descripcion = "";
        public ModoAtaqueArma modo = ModoAtaqueArma.CuerpoACuerpo;
        public Color colorEnergia = new Color(1f, 0.85f, 0.5f);

        [Header("2. Ataque básico")]
        public float dano = 30f;
        public float alcance = 2.6f;
        [Tooltip("Ángulo del cono de impacto cuerpo a cuerpo (grados a cada lado).")]
        public float anguloGolpe = 70f;
        [Tooltip("Segundos entre ataques (o duración de cada golpe del combo).")]
        public float intervalo = 0.45f;
        [Tooltip("Golpes del combo cuerpo a cuerpo (el último hace ×1,35).")]
        public int golpesCombo = 3;
        [Tooltip("Momento del golpe dentro del intervalo (anticipación).")]
        [Range(0.05f, 0.9f)] public float momentoImpacto = 0.35f;
        public float impactoAguante = 15f;

        [Header("3. Munición, carga y bloqueo")]
        public int cargador = 0;
        public int reservaMaxima = 0;
        public float tiempoRecarga = 0f;
        [Tooltip("Tiempo de carga completa (arco). 0 = sin carga.")]
        public float tiempoCarga = 0f;
        public float velocidadProyectil = 60f;
        public float gravedadProyectil = 5.9f;
        [Range(0f, 0.9f)] public float bloqueoFrontal = 0f;
        public float dispersion = 0.4f;
        public float dispersionMovimiento = 3f;
        public float zoomMira = 1f;

        [Header("4. Habilidades del arma (E corta · Q media · F larga)")]
        public FichaHabilidad habilidadCorta;
        public FichaHabilidad habilidadMedia;
        public FichaHabilidad habilidadLarga;

        public bool EsCuerpoACuerpo => modo == ModoAtaqueArma.CuerpoACuerpo;
        public bool UsaMunicion => cargador > 0;

        public FichaHabilidad ObtenerHabilidad(int indice)
        {
            switch (indice)
            {
                case 0: return habilidadCorta;
                case 1: return habilidadMedia;
                default: return habilidadLarga;
            }
        }

        /// <summary>Distancia a la que la IA prefiere combatir con esta arma.</summary>
        public float DistanciaIdealIA
        {
            get
            {
                switch (tipo)
                {
                    case TipoArmaModular.ArcoCompuesto: return 18f;
                    case TipoArmaModular.RifleLargo: return 24f;
                    case TipoArmaModular.ArsenalBiomecanico: return 2f;
                    default: return 2f;
                }
            }
        }

        public static DatosArmaModular CrearPorDefecto(TipoArmaModular tipo)
        {
            DatosArmaModular d = CreateInstance<DatosArmaModular>();
            d.tipo = tipo;
            switch (tipo)
            {
                case TipoArmaModular.ArcoCompuesto:
                    d.nombre = "Arco Compuesto";
                    d.descripcion = "Mantén el clic para tensar (0,8 s) y suelta para disparar. La flecha cae con la gravedad. Clic derecho: apuntar.";
                    d.modo = ModoAtaqueArma.Proyectil;
                    d.colorEnergia = new Color(0.6f, 1f, 0.75f);
                    d.dano = 45f; d.alcance = 70f; d.intervalo = 0.35f; d.golpesCombo = 1;
                    d.cargador = 20; d.reservaMaxima = 40; d.tiempoRecarga = 0f;
                    d.tiempoCarga = 0.8f; d.velocidadProyectil = 70f; d.gravedadProyectil = 5.9f;
                    d.dispersion = 0.2f; d.dispersionMovimiento = 1.5f; d.zoomMira = 1.6f; d.impactoAguante = 20f;
                    d.habilidadCorta = new FichaHabilidad("Disparo a quemarropa y retroceso", "Disparo de 3 m (40 de daño y empuje) que te impulsa 4 m hacia atrás.", 8f);
                    d.habilidadMedia = new FichaHabilidad("Flecha de amarre", "Flecha con cuerda (20 m): amarra al guardián (−50% de velocidad, 3 s) o te impulsa hacia la superficie.", 12f);
                    d.habilidadLarga = new FichaHabilidad("Tiro de núcleo", "Carga 1,5 s y dispara una flecha recta que atraviesa 3 objetivos (70 de daño, ×2,5 en el punto débil).", 18f);
                    break;

                case TipoArmaModular.Mandoble:
                    d.nombre = "Mandoble / Espada Larga";
                    d.descripcion = "Combo de 3 tajos (horizontal, diagonal y estocada). Clic derecho: bloquear (−60% de daño frontal).";
                    d.modo = ModoAtaqueArma.CuerpoACuerpo;
                    d.colorEnergia = new Color(1f, 0.85f, 0.45f);
                    d.dano = 30f; d.alcance = 2.8f; d.anguloGolpe = 70f; d.intervalo = 0.45f; d.golpesCombo = 3; d.momentoImpacto = 0.35f;
                    d.bloqueoFrontal = 0.6f; d.impactoAguante = 25f;
                    d.habilidadCorta = new FichaHabilidad("Barrido ascendente", "Tajo vertical de 3 m: 45 de daño y lanza al aire.", 7f);
                    d.habilidadMedia = new FichaHabilidad("Onda de filo", "Lanza una onda de energía de 4 m de ancho que recorre 15 m (35 de daño).", 11f);
                    d.habilidadLarga = new FichaHabilidad("Estocada de impulso", "Embestida de 12 m: 50 de daño, empuje y gran daño de aguante.", 16f);
                    break;

                case TipoArmaModular.MazaEscudo:
                    d.nombre = "Maza con Púas y Escudo";
                    d.descripcion = "Combo de 2 mazazos pesados. Clic derecho: cubrirse con el escudo (−75% de daño frontal).";
                    d.modo = ModoAtaqueArma.CuerpoACuerpo;
                    d.colorEnergia = new Color(0.85f, 0.6f, 0.35f);
                    d.dano = 34f; d.alcance = 2.4f; d.anguloGolpe = 60f; d.intervalo = 0.55f; d.golpesCombo = 2; d.momentoImpacto = 0.45f;
                    d.bloqueoFrontal = 0.75f; d.impactoAguante = 35f;
                    d.habilidadCorta = new FichaHabilidad("Golpe de yunque", "Mazazo al suelo de 3,5 m: 40 de daño y aturde 0,8 s (cancela habilidades en carga).", 8f);
                    d.habilidadMedia = new FichaHabilidad("Lanzamiento con cadena", "Lanza la cabeza de la maza (12 m) y arrastra al objetivo hacia ti.", 12f);
                    d.habilidadLarga = new FichaHabilidad("Postura de bastión", "Planta el escudo como cobertura de 2 × 1,8 m durante 6 s que bloquea todo el daño frontal.", 20f);
                    break;

                case TipoArmaModular.RifleLargo:
                    d.nombre = "Rifle Largo con Mira";
                    d.descripcion = "60 de daño por disparo, 6 balas por cargador. Clic derecho: mira ×4 (muy impreciso en movimiento).";
                    d.modo = ModoAtaqueArma.Rayo;
                    d.colorEnergia = new Color(1f, 0.8f, 0.45f);
                    d.dano = 60f; d.alcance = 220f; d.intervalo = 1f; d.golpesCombo = 1;
                    d.cargador = 6; d.reservaMaxima = 36; d.tiempoRecarga = 2.2f;
                    d.dispersion = 0.15f; d.dispersionMovimiento = 6f; d.zoomMira = 4f; d.impactoAguante = 25f;
                    d.habilidadCorta = new FichaHabilidad("Disparo de dispersión", "Cartucho de perdigones: cono de 25° y 6 m (8 × 10 de daño) con empuje.", 7f);
                    d.habilidadMedia = new FichaHabilidad("Tiro a articulación", "El siguiente disparo ralentiza al 60% durante 3 s y cancela la habilidad que el guardián esté cargando.", 12f);
                    d.habilidadLarga = new FichaHabilidad("Calibre perforante", "El siguiente disparo hace 120 de daño y atraviesa 3 objetivos.", 18f);
                    break;

                default:
                    d.nombre = "Arsenal Biomecánico Acoplado";
                    d.descripcion = "Cuchillas de antebrazo: combo de 4 golpes muy rápidos. Clic derecho: cañón de pulso (se sobrecalienta; R para ventilar).";
                    d.modo = ModoAtaqueArma.CuerpoACuerpo;
                    d.colorEnergia = new Color(1f, 0.3f, 0.75f);
                    d.dano = 22f; d.alcance = 2.3f; d.anguloGolpe = 65f; d.intervalo = 0.26f; d.golpesCombo = 4; d.momentoImpacto = 0.4f;
                    d.cargador = 24; d.reservaMaxima = 0; d.tiempoRecarga = 1.2f;
                    d.dispersion = 1.2f; d.dispersionMovimiento = 2f; d.impactoAguante = 10f;
                    d.habilidadCorta = new FichaHabilidad("Cuchillas de antebrazo", "Tijera de las dos cuchillas (2,5 m): 50 de daño y sangrado de 6/s durante 3 s.", 6f);
                    d.habilidadMedia = new FichaHabilidad("Cable arpón", "Arpón de 18 m: te impulsa hacia la superficie o atrae 4 m al enemigo.", 10f);
                    d.habilidadLarga = new FichaHabilidad("Pulso de riel", "Carga 1 s y dispara un rayo de 150 m (90 de daño) que anula la levitación de los guardianes de Metal.", 18f);
                    break;
            }
            d.name = d.nombre;
            return d;
        }
    }
}

using System.Collections;
using UnityEngine;

public class Enemigo : MonoBehaviour, IGolpeable
{
    public Transform player;

    [Header("Seguimiento")]
    public float rangoPlayer = 7f;
    public float limiteSeguimiento = 10f;
    public float distanciaMinima = 2f;
    public float velocidad = 3f;

    [Header("Patrulla")]
    public float anchoPatrulla = 3f;

    [Header("Combate Medieval")]
    public bool haceDano = false;
    public bool tieneEscudoFrontal = false;
    public float radioContacto = 1.0f;
    public GameObject vfxDerrota;
    public Animator animator;

    [Header("Animacion y Hechizos")]
    public bool esMago = false;
    public float intervaloHechizo = 2.8f;
    public float rangoDisparoMagico = 16f;
    public float velocidadHechizo = 9.5f;

    [Header("Caballero")]
    public bool esCaballero = false;
    public float rangoEspadazo = 2.8f;
    public float pausaEspadazo = 2.2f;

    [Header("Rana Medieval")]
    public bool esRana = false;
    public float alturaSaltoRana = 1.6f;
    public float pausaSaltoRana = 0.65f;

    [Header("Sonidos")]
    public AudioClip sonidoDisparado;
    public AudioClip sonidoSaltoRana;
    public AudioClip sonidoHechizo;
    public AudioClip sonidoEspadazo;
    public Material materialLlama;
    public Material materialNucleo;

    private Vector3 puntoInicio;
    private Vector3 puntoIzquierdo;
    private Vector3 puntoDerecho;
    private Vector3 destinoPatrulla;
    private bool muerto = false;
    private string paramVelocidad;
    private Transform visualHijo;
    private Vector3 posicionVisualBase;
    private Vector3 escalaVisualBase = Vector3.one;
    private float proximoHechizo = 0f;
    private float ultimoHechizo = -10f;
    private float proximoEspadazo = 0f;
    private bool ranaEnSalto = false;
    private float tiempoFaseRana = 0f;
    private Vector3 inicioSaltoRana;
    private Vector3 finSaltoRana;
    private float baseYRana;
    private const float DuracionSaltoRana = 0.55f;
    private Transform brazoMago;
    private Quaternion rotBaseBrazo;

    private void Start()
    {
        puntoInicio = transform.position;
        baseYRana = puntoInicio.y;
        //Patrulla de lado a lado del camino aunque este curvo
        puntoIzquierdo = puntoInicio - transform.right * anchoPatrulla;
        puntoDerecho = puntoInicio + transform.right * anchoPatrulla;
        destinoPatrulla = puntoDerecho;

        if (transform.childCount > 0)
        {
            visualHijo = transform.GetChild(0);
            posicionVisualBase = visualHijo.localPosition;
            escalaVisualBase = visualHijo.localScale;
        }

        if (esMago && visualHijo != null)
        {
            brazoMago = BuscarHueso(visualHijo, "RightArm");
            if (brazoMago != null) rotBaseBrazo = brazoMago.localRotation;
        }
        proximoHechizo = Time.time + Random.Range(0.8f, intervaloHechizo);

        if (player == null)
        {
            GameObject objJugador = GameObject.FindGameObjectWithTag("Player");
            if (objJugador != null) player = objJugador.transform;
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Float && (p.name == "Velocidad" || p.name == "Speed"))
                {
                    paramVelocidad = p.name;
                    break;
                }
            }
        }
    }

    private void Update()
    {
        if (muerto) return;
        if (GestorJuego.Instancia != null && (GestorJuego.Instancia.juegoPausado || GestorJuego.Instancia.juegoTerminado))
        {
            return;
        }

        float velocidadMovimiento = 0f;

        if (esRana && !esMago)
        {
            velocidadMovimiento = ActualizarRana();
        }
        else if (player != null)
        {
            Vector3 destino = player.position;
            //Persigue solo en el plano, sin subir ni bajar
            destino.y = transform.position.y;

            float distancia = Vector3.Distance(transform.position, destino);
            float lejosDelInicio = Vector3.Distance(puntoInicio, destino);

            if (distancia <= rangoPlayer && lejosDelInicio <= limiteSeguimiento)
            {
                RotarHacia(destino);
                if (distancia > distanciaMinima)
                {
                    transform.position = Vector3.MoveTowards(transform.position, destino, velocidad * Time.deltaTime);
                    velocidadMovimiento = velocidad;
                }
            }
            else if (esMago && distancia <= rangoDisparoMagico)
            {
                RotarHacia(destino);
            }
            else
            {
                Patrullar();
                velocidadMovimiento = velocidad;
            }

            if (esMago && distancia <= rangoDisparoMagico && Time.time >= proximoHechizo)
            {
                LanzarHechizo(player.position + Vector3.up * 0.9f);
                proximoHechizo = Time.time + intervaloHechizo;
            }
        }
        else
        {
            Patrullar();
            velocidadMovimiento = velocidad;
        }

        if (visualHijo != null)
        {
            if (esMago)
            {
                visualHijo.localPosition = posicionVisualBase + Vector3.up * (Mathf.Sin(Time.time * 3.2f) * 0.1f);
                //Se echa hacia atras y luego lanza el hechizo hacia adelante
                float fase = Mathf.Clamp01((Time.time - ultimoHechizo) / 0.6f);
                float inclinacion = fase < 1f ? Mathf.Sin(fase * Mathf.PI * 2f) * -18f : 0f;
                visualHijo.localRotation = Quaternion.Euler(inclinacion, 0f, Mathf.Sin(Time.time * 2f) * 4f);
                if (brazoMago != null)
                {
                    //El baston va hacia atras y luego hacia adelante al lanzar el hechizo
                    float giroBrazo = fase < 1f ? Mathf.Sin(fase * Mathf.PI * 2f) * 40f : Mathf.Sin(Time.time * 2.2f) * 6f;
                    brazoMago.localRotation = rotBaseBrazo * Quaternion.AngleAxis(giroBrazo, Vector3.right);
                }
            }
            else if (tieneEscudoFrontal)
            {
                float paso = velocidadMovimiento > 0.05f ? Mathf.Abs(Mathf.Sin(Time.time * 9f)) * 0.05f : 0f;
                visualHijo.localPosition = posicionVisualBase + Vector3.up * paso;
            }
            visualHijo.localScale = Vector3.Lerp(visualHijo.localScale, escalaVisualBase, 10f * Time.deltaTime);
        }

        if (animator != null && !string.IsNullOrEmpty(paramVelocidad))
        {
            animator.SetFloat(paramVelocidad, velocidadMovimiento);
        }

        if (esCaballero && animator != null && player != null && Time.time >= proximoEspadazo)
        {
            Vector3 jugadorPlano = new Vector3(player.position.x, transform.position.y, player.position.z);
            if (Vector3.Distance(transform.position, jugadorPlano) <= rangoEspadazo)
            {
                animator.SetTrigger("Atacar");
                ReproducirClip(sonidoEspadazo, 0.8f);
                proximoEspadazo = Time.time + pausaEspadazo;
            }
        }

        VerificarContactoLateral();
    }

private void LanzarHechizo(Vector3 objetivo)
    {
        ultimoHechizo = Time.time;
        Vector3 origen = transform.position + transform.forward * 0.8f + Vector3.up * 1.1f;
        Vector3 direccion = (objetivo - origen).normalized;

        GameObject orbe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orbe.name = "HechizoEnemigo";
        orbe.transform.position = origen;
        orbe.transform.localScale = Vector3.one * 0.32f;
        orbe.GetComponent<SphereCollider>().isTrigger = true;
        if (materialNucleo != null) orbe.GetComponent<Renderer>().sharedMaterial = materialNucleo;

        //Halo y llamas que se quedan atras mientras avanza
        if (materialLlama != null)
        {
            CrearLlamas(orbe.transform, "Halo", 40f, 0.25f, 2.2f, 0f, new Color(1f, 0.55f, 0.1f, 0.9f), ParticleSystemSimulationSpace.Local);
            CrearLlamas(orbe.transform, "Estela", 70f, 0.45f, 1.4f, 0.6f, new Color(1f, 0.35f, 0.05f, 0.8f), ParticleSystemSimulationSpace.World);
        }

        Light luz = orbe.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.55f, 0.15f);
        luz.range = 4f;
        luz.intensity = 3f;

        ProyectilWumpa proyectil = orbe.AddComponent<ProyectilWumpa>();
        proyectil.tiempoVida = 3.2f;
        proyectil.InicializarHechizo(direccion, velocidadHechizo, gameObject);

        if (visualHijo != null)
        {
            visualHijo.localScale = escalaVisualBase * 1.22f;
        }
        ReproducirClip(sonidoHechizo, 0.8f);
    }

private void CrearLlamas(Transform padre, string nombre, float cantidad, float vida, float tamano, float velocidadSalida, Color color, ParticleSystemSimulationSpace espacio)
    {
        GameObject obj = new GameObject(nombre);
        obj.transform.SetParent(padre, false);
        ParticleSystem ps = obj.AddComponent<ParticleSystem>();
        ps.Stop();
        var main = ps.main;
        main.startLifetime = vida;
        main.startSpeed = velocidadSalida;
        main.startSize = new ParticleSystem.MinMaxCurve(tamano * 0.5f, tamano);
        main.startColor = color;
        main.simulationSpace = espacio;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        var emision = ps.emission;
        emision.rateOverTime = cantidad;
        var forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Sphere;
        forma.radius = 0.4f;
        var tamanoVida = ps.sizeOverLifetime;
        tamanoVida.enabled = true;
        tamanoVida.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.1f));
        var colorVida = ps.colorOverLifetime;
        colorVida.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.9f, 0.4f), 0f), new GradientColorKey(color, 0.4f), new GradientColorKey(new Color(0.4f, 0.05f, 0f), 1f) },
                  new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorVida.color = g;
        obj.GetComponent<ParticleSystemRenderer>().sharedMaterial = materialLlama;
        ps.Play();
    }


    public void NotificarBloqueoEscudo()
    {
        if (visualHijo != null)
        {
            visualHijo.localScale = Vector3.Scale(escalaVisualBase, new Vector3(1.12f, 0.92f, 1.12f));
        }
    }

    private float ActualizarRana()
    {
        if (!ranaEnSalto)
        {
            tiempoFaseRana += Time.deltaTime;
            Vector3 objetivoMirar = destinoPatrulla;

            if (player != null)
            {
                Vector3 posJugadorPlano = new Vector3(player.position.x, baseYRana, player.position.z);
                float distJugador = Vector3.Distance(new Vector3(transform.position.x, baseYRana, transform.position.z), posJugadorPlano);
                float lejosInicio = Vector3.Distance(puntoInicio, posJugadorPlano);
                if (distJugador <= rangoPlayer && lejosInicio <= limiteSeguimiento)
                {
                    objetivoMirar = posJugadorPlano;
                }
            }

            RotarHacia(objetivoMirar);
            if (tiempoFaseRana < pausaSaltoRana) return 0f;

            Vector3 origenPlano = new Vector3(transform.position.x, baseYRana, transform.position.z);
            Vector3 delta = objetivoMirar - origenPlano;
            delta.y = 0f;
            float distancia = delta.magnitude;
            float paso = Mathf.Min(distancia, Mathf.Max(2.0f, velocidad * 0.85f));
            inicioSaltoRana = origenPlano;
            finSaltoRana = delta.sqrMagnitude > 0.001f ? origenPlano + delta.normalized * paso : origenPlano;

            if (objetivoMirar == destinoPatrulla && Vector3.Distance(finSaltoRana, destinoPatrulla) < 0.25f)
            {
                destinoPatrulla = destinoPatrulla == puntoDerecho ? puntoIzquierdo : puntoDerecho;
            }

            ranaEnSalto = true;
            tiempoFaseRana = 0f;
            if (player != null && Vector3.Distance(transform.position, player.position) < 14f)
            {
                ReproducirClip(sonidoSaltoRana, 0.7f);
            }
            if (visualHijo != null)
            {
                visualHijo.localScale = Vector3.Scale(escalaVisualBase, new Vector3(0.84f, 1.24f, 0.84f));
            }
            return velocidad;
        }

        tiempoFaseRana += Time.deltaTime;
        float t = Mathf.Clamp01(tiempoFaseRana / DuracionSaltoRana);
        Vector3 pos = Vector3.Lerp(inicioSaltoRana, finSaltoRana, t);
        pos.y = baseYRana + 4f * alturaSaltoRana * t * (1f - t);
        transform.position = pos;
        RotarHacia(finSaltoRana);

        if (t >= 1f)
        {
            transform.position = finSaltoRana;
            ranaEnSalto = false;
            tiempoFaseRana = 0f;
            if (visualHijo != null)
            {
                visualHijo.localScale = Vector3.Scale(escalaVisualBase, new Vector3(1.24f, 0.74f, 1.24f));
            }
        }

        return velocidad;
    }

    private void Patrullar()
    {
        RotarHacia(destinoPatrulla);
        transform.position = Vector3.MoveTowards(transform.position, destinoPatrulla, velocidad * Time.deltaTime);

        if (Vector3.Distance(transform.position, destinoPatrulla) < 0.1f)
        {
            destinoPatrulla = destinoPatrulla == puntoDerecho ? puntoIzquierdo : puntoDerecho;
        }
    }

    private void RotarHacia(Vector3 objetivo)
    {
        Vector3 dir = objetivo - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion rotObjetivo = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotObjetivo, 8f * Time.deltaTime);
    }

    private void VerificarContactoLateral()
    {
        if (!haceDano || muerto || player == null) return;

        float distXZ = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(player.position.x, player.position.z));
        if (distXZ >= radioContacto) return;

        //Si le cae encima cuenta como salto y no hace dano
        if (player.position.y >= transform.position.y + 0.9f || player.position.y < transform.position.y - 1.5f) return;

        AtaqueGiro giro = player.GetComponent<AtaqueGiro>();
        if (giro != null && giro.EstaGirando && (!tieneEscudoFrontal || PuedeRecibirGiroDesde(player.position))) return;

        player.GetComponent<MovoJugadorTPV>()?.RecibirDano();
    }

    public bool PuedeRecibirGiroDesde(Vector3 posicionAtacante)
    {
        if (!tieneEscudoFrontal) return true;

        Vector3 dirHaciaAtacante = posicionAtacante - transform.position;
        dirHaciaAtacante.y = 0f;
        if (dirHaciaAtacante.sqrMagnitude < 0.001f) return true;

        return Vector3.Dot(transform.forward, dirHaciaAtacante.normalized) <= 0.25f;
    }

public void Golpear()
    {
        if (muerto) return;
        muerto = true;

        if (vfxDerrota != null)
        {
            Instantiate(vfxDerrota, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        }

        GestorJuego.Instancia?.ReproducirSFX("hit");
        ReproducirClip(sonidoDisparado, 0.9f);
        StartCoroutine(SalirDisparado());
    }

private IEnumerator SalirDisparado()
    {
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;
        if (animator != null) animator.enabled = false;

        Vector3 dir = player != null ? transform.position - player.position : -transform.forward;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) dir = -transform.forward;

        //Sale en linea recta, un poco hacia arriba, sin girar
        Vector3 vel = (dir.normalized + Vector3.up * 0.35f).normalized * 72f;
        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            transform.position += vel * Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }


private void ReproducirClip(AudioClip clip, float volumen)
    {
        if (clip == null) return;
        GameObject obj = new GameObject("Sonido_" + clip.name);
        AudioSource fuente = obj.AddComponent<AudioSource>();
        fuente.spatialBlend = 0f;
        fuente.volume = volumen;
        fuente.PlayOneShot(clip);
        Destroy(obj, clip.length + 0.1f);
    }



    private Transform BuscarHueso(Transform raiz, string nombre)
    {
        foreach (Transform t in raiz.GetComponentsInChildren<Transform>())
        {
            if (t.name == nombre) return t;
        }
        return null;
    }
}

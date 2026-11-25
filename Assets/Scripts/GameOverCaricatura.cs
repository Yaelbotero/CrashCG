using System.Collections;
using UnityEngine;
using UnityEngine.UI;

//Sin vidas: Crash cae de espaldas, la pantalla se cierra en un circulo como en las caricaturas
//y un portal del piso se lo traga hasta el selector de niveles
public class GameOverCaricatura : MonoBehaviour
{
    public AudioClip sonidoDerrota;
    public AudioClip vozCortex;
    public AudioClip sonidoPortal;
    public Sprite agujeroIris;
    public Material materialPortal;
    public float tamanoPortal = 2.6f;
    //Radio del circulo alrededor de Crash, como fraccion del alto de la pantalla
    public float radioCirculo = 0.17f;

    private AudioSource fuente;
    private Camera camara;
    private Transform objetivo;
    private RectTransform iris;
    private Transform portal;
    private float radio;

    public void Iniciar()
    {
        StartCoroutine(Secuencia());
    }

    void Update()
    {
        if (iris != null && objetivo != null && camara != null)
        {
            iris.position = PuntoEnPantalla(objetivo.position + Vector3.up * 0.4f);
            iris.sizeDelta = Vector2.one * radio * 2f;
        }
        if (portal != null) portal.Rotate(0f, 0f, -220f * Time.deltaTime, Space.Self);
    }

    private IEnumerator Secuencia()
    {
        Time.timeScale = 1f;
        if (GestorJuego.Instancia != null) GestorJuego.Instancia.DetenerMusica();
        fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;

        MovoJugadorTPV jugador = FindFirstObjectByType<MovoJugadorTPV>();
        Camara control = FindFirstObjectByType<Camara>();
        camara = control != null ? control.GetComponent<Camera>() : Camera.main;
        objetivo = jugador.transform;
        Animator animador = jugador.animator;

        //Si cayo a la lava o al vacio, vuelve al punto de reaparicion para caer sobre el piso
        if (!Physics.Raycast(objetivo.position + Vector3.up * 0.5f, Vector3.down, 2.5f, jugador.piso, QueryTriggerInteraction.Ignore))
        {
            jugador.Teletransportar(jugador.PuntoReaparicion);
        }

        //Se tira de espaldas y se queda mirando hacia arriba
        animador.updateMode = AnimatorUpdateMode.Normal;
        animador.speed = 1f;
        animador.CrossFadeInFixedTime("Falling Back Death", 0.1f);
        StartCoroutine(QuedarseTendido(animador));
        if (sonidoDerrota != null) fuente.PlayOneShot(sonidoDerrota);

        CrearIris();
        float radioInicial = new Vector2(Screen.width, Screen.height).magnitude;
        radio = radioInicial;
        yield return new WaitForSeconds(1f);
        float radioFinal = radioCirculo * Screen.height;
        yield return Animar(1.6f, t => radio = Mathf.Lerp(radioInicial, radioFinal, Mathf.SmoothStep(0f, 1f, t)));

        //Cortex se burla en off cuando termina el trombon
        float resto = sonidoDerrota != null ? sonidoDerrota.length - 2.6f : 0.5f;
        yield return new WaitForSeconds(Mathf.Max(0.3f, resto));
        if (vozCortex != null)
        {
            fuente.PlayOneShot(vozCortex);
            yield return new WaitForSeconds(vozCortex.length + 0.2f);
        }

        //El portal se abre debajo de Crash
        Vector3 suelo = objetivo.position;
        RaycastHit golpe;
        if (Physics.Raycast(objetivo.position + Vector3.up, Vector3.down, out golpe, 4f, jugador.piso, QueryTriggerInteraction.Ignore)) suelo = golpe.point;
        portal = CrearPortal(suelo + Vector3.up * 0.04f);
        if (sonidoPortal != null) fuente.PlayOneShot(sonidoPortal);
        yield return Animar(0.5f, t => portal.localScale = Vector3.one * tamanoPortal * Mathf.SmoothStep(0f, 1f, t));

        //Lo absorbe girando, estirandose y encogiendose
        CharacterController controlador = jugador.GetComponent<CharacterController>();
        if (controlador != null) controlador.enabled = false;
        Transform modelo = animador.transform;
        Vector3 escala = modelo.localScale;
        Vector3 inicio = objetivo.position;
        Quaternion giro = objetivo.rotation;
        yield return Animar(1.4f, t =>
        {
            float s = t * t;
            objetivo.position = inicio + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.4f - s * 1.2f);
            objetivo.rotation = giro * Quaternion.Euler(0f, s * 1080f, 0f);
            modelo.localScale = Vector3.Scale(escala, new Vector3(1f - s, 1f + Mathf.Sin(t * Mathf.PI) * 0.5f - s, 1f - s));
        });
        modelo.gameObject.SetActive(false);

        yield return Animar(0.5f, t =>
        {
            portal.localScale = Vector3.one * tamanoPortal * (1f - t);
            radio = radioFinal * (1f - t);
        });
        radio = 0f;
        yield return new WaitForSeconds(0.5f);

        if (GestorJuego.Instancia != null) GestorJuego.Instancia.IrAlMenu();
    }

    //Congela la pose al final de la caida para que no se levante
    private IEnumerator QuedarseTendido(Animator animador)
    {
        float limite = Time.time + 4f;
        while (Time.time < limite)
        {
            AnimatorStateInfo info = animador.GetCurrentAnimatorStateInfo(0);
            if (info.IsName("Falling Back Death") && !animador.IsInTransition(0) && info.normalizedTime >= 0.97f) break;
            yield return null;
        }
        animador.speed = 0f;
    }

    //El lienzo retro muestra la imagen 4:3 con barras, asi que el punto se ubica dentro de ese recuadro
    private Vector2 PuntoEnPantalla(Vector3 mundo)
    {
        Vector3 vp = camara.WorldToViewportPoint(mundo);
        Rect area = new Rect(0f, 0f, Screen.width, Screen.height);
        FiltroRetro retro = camara.GetComponent<FiltroRetro>();
        if (retro != null)
        {
            float aspecto = (float)retro.anchoPixeles / retro.altoPixeles;
            if ((float)Screen.width / Screen.height > aspecto)
            {
                float ancho = Screen.height * aspecto;
                area = new Rect((Screen.width - ancho) / 2f, 0f, ancho, Screen.height);
            }
            else
            {
                float alto = Screen.width / aspecto;
                area = new Rect(0f, (Screen.height - alto) / 2f, Screen.width, alto);
            }
        }
        return new Vector2(area.x + vp.x * area.width, area.y + vp.y * area.height);
    }

    private void CrearIris()
    {
        Canvas lienzo = new GameObject("Lienzo_GameOver").AddComponent<Canvas>();
        lienzo.renderMode = RenderMode.ScreenSpaceOverlay;
        lienzo.sortingOrder = 500;

        Image agujero = new GameObject("Iris").AddComponent<Image>();
        agujero.transform.SetParent(lienzo.transform, false);
        agujero.sprite = agujeroIris;
        agujero.raycastTarget = false;
        iris = agujero.rectTransform;
        iris.anchorMin = iris.anchorMax = Vector2.zero;
        iris.pivot = new Vector2(0.5f, 0.5f);

        //Cuatro paneles negros cubren todo lo que queda fuera del circulo
        Panel(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(20000f, 20000f));
        Panel(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20000f, 20000f));
        Panel(new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, 20000f));
        Panel(new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 20000f));
    }

    private void Panel(Vector2 anclaMin, Vector2 anclaMax, Vector2 pivote, Vector2 tamano)
    {
        Image negro = new GameObject("Negro").AddComponent<Image>();
        negro.transform.SetParent(iris, false);
        negro.color = Color.black;
        negro.raycastTarget = false;
        RectTransform rt = negro.rectTransform;
        rt.anchorMin = anclaMin;
        rt.anchorMax = anclaMax;
        rt.pivot = pivote;
        rt.sizeDelta = tamano;
        rt.anchoredPosition = Vector2.zero;
    }

    private Transform CrearPortal(Vector3 posicion)
    {
        GameObject disco = GameObject.CreatePrimitive(PrimitiveType.Quad);
        disco.name = "Portal_Piso";
        Destroy(disco.GetComponent<Collider>());
        disco.GetComponent<Renderer>().sharedMaterial = materialPortal;
        disco.transform.position = posicion;
        disco.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        disco.transform.localScale = Vector3.zero;
        Light luz = new GameObject("Luz_Portal").AddComponent<Light>();
        luz.transform.SetParent(disco.transform, false);
        luz.transform.position = posicion + Vector3.up * 1.2f;
        luz.type = LightType.Point;
        luz.color = new Color(0.4f, 0.7f, 1f);
        luz.range = 6f;
        luz.intensity = 3f;
        return disco.transform;
    }

    private IEnumerator Animar(float duracion, System.Action<float> paso)
    {
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / duracion);
            paso(t);
            yield return null;
        }
    }
}

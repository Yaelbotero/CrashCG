using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//Final del juego: Cortex derrotado habla, Crash lo golpea con el giro y lo manda a volar
public class FinalCortex : MonoBehaviour
{
    [System.Serializable]
    public class LineaDialogo
    {
        public string texto;
        public AudioClip voz;
        //Si tiene imagen se muestra la caja dibujada en vez del texto
        public Sprite imagen;
    }

    public Transform crash;
    public Animator animCrash;
    public Transform cortex;
    public Transform camara;
    public AudioSource fuenteVoz;
    public AudioSource fuenteEfectos;
    public GameObject cajaDialogo;
    public Text textoDialogo;
    public Image imagenDialogo;
    public Image fundido;
    public LineaDialogo[] lineas;
    public LineaDialogo grito;
    public AudioClip sonidoGiro;
    public AudioClip sonidoGolpe;
    public AudioClip sonidoEstrella;
    public AudioClip sonidoVictoria;
    public Transform estrella;
    //Cartel de agradecimiento con el boton para volver al menu
    public Image pantallaGracias;
    public GameObject botonMenu;
    public string escenaSiguiente = "MenuPrincipal";

    private readonly float[] muestras = new float[256];
    private Vector3 escalaCortex;
    private Vector3 escalaCrash;
    private Vector3 posCortex;
    private Vector3 posCamara;
    private bool cortexFlotando = true;
    private bool saliendo = false;
    private bool volverAlMenu = false;

    void Start()
    {
        escalaCortex = cortex.localScale;
        escalaCrash = crash.localScale;
        posCortex = cortex.position;
        posCamara = camara.position;
        crash.localScale = Vector3.zero;
        cajaDialogo.SetActive(false);
        if (imagenDialogo != null) imagenDialogo.gameObject.SetActive(false);
        if (pantallaGracias != null) pantallaGracias.gameObject.SetActive(false);
        if (botonMenu != null) botonMenu.SetActive(false);
        if (estrella != null) estrella.gameObject.SetActive(false);
        fundido.color = Color.black;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;
        AudioListener.volume = PlayerPrefs.GetFloat("volumen", 1f);
        //Sin el script de movimiento hay que decirle al Animator que Crash esta en el piso
        animCrash.SetBool("EnPiso", true);
        animCrash.SetFloat("Velocidad", 0f);
        animCrash.SetFloat("MultiplicadorVel", 1f);
        StartCoroutine(Secuencia());
    }

    void Update()
    {
        if (cortexFlotando)
        {
            //Flota y se estira con el volumen de la voz para que parezca que habla
            float volumen = VolumenVoz();
            cortex.position = posCortex + new Vector3(Mathf.Sin(Time.time * 1.3f) * 0.08f, Mathf.Sin(Time.time * 2.1f) * 0.12f, 0f);
            cortex.localScale = Vector3.Scale(escalaCortex, new Vector3(1f - volumen * 0.25f, 1f + volumen * 0.5f, 1f));
            cortex.rotation = Quaternion.Euler(Mathf.Sin(Time.time * 1.7f) * 6f - volumen * 20f, -140f + Mathf.Sin(Time.time * 0.9f) * 10f, Mathf.Sin(Time.time * 1.1f) * 5f);
        }

        if (!saliendo && Input.GetKeyDown(KeyCode.Escape))
        {
            StopAllCoroutines();
            StartCoroutine(Salir());
        }
    }

    private IEnumerator Secuencia()
    {
        yield return Animar(1f, t => fundido.color = new Color(0f, 0f, 0f, 1f - t));

        //Crash llega del portal dando vueltas
        if (sonidoEstrella != null) fuenteEfectos.PlayOneShot(sonidoEstrella);
        Quaternion giroFinal = crash.rotation;
        yield return Animar(0.7f, t =>
        {
            crash.localScale = escalaCrash * Mathf.SmoothStep(0f, 1f, t);
            crash.rotation = giroFinal * Quaternion.Euler(0f, (1f - t) * 720f, 0f);
        });
        animCrash.CrossFadeInFixedTime("Idle", 0.1f);
        yield return new WaitForSeconds(0.6f);

        foreach (LineaDialogo linea in lineas) yield return Decir(linea);
        cajaDialogo.SetActive(false);
        if (imagenDialogo != null) imagenDialogo.gameObject.SetActive(false);

        //Crash corre hacia Cortex y lo golpea con el giro
        Vector3 inicio = crash.position;
        Vector3 golpe = new Vector3(posCortex.x - 0.9f, inicio.y, posCortex.z);
        animCrash.SetFloat("Velocidad", 6.5f);
        animCrash.CrossFadeInFixedTime("Running", 0.1f);
        yield return Animar(0.45f, t => crash.position = Vector3.Lerp(inicio, golpe, t));
        animCrash.SetFloat("Velocidad", 0f);

        animCrash.SetBool("Girando", true);
        animCrash.CrossFadeInFixedTime("Spinning", 0.05f);
        if (sonidoGiro != null) fuenteEfectos.PlayOneShot(sonidoGiro);
        yield return Animar(0.35f, t => crash.rotation = giroFinal * Quaternion.Euler(0f, t * 720f, 0f));

        if (sonidoGolpe != null) fuenteEfectos.PlayOneShot(sonidoGolpe);
        StartCoroutine(Temblor(0.25f, 0.4f));
        yield return Volando();
        animCrash.SetBool("Girando", false);

        //Salto de celebracion mirando a la camara
        crash.rotation = Quaternion.LookRotation(posCamara - crash.position, Vector3.up);
        crash.rotation = Quaternion.Euler(0f, crash.eulerAngles.y, 0f);
        if (sonidoVictoria != null) fuenteEfectos.PlayOneShot(sonidoVictoria);
        animCrash.SetBool("EnPiso", false);
        animCrash.CrossFadeInFixedTime("Jump", 0.1f);
        Vector3 suelo = crash.position;
        yield return Animar(0.7f, t => crash.position = suelo + Vector3.up * Mathf.Sin(t * Mathf.PI) * 1.1f);
        animCrash.SetBool("EnPiso", true);
        animCrash.CrossFadeInFixedTime("Idle", 0.15f);
        yield return new WaitForSeconds(2.5f);

        yield return Salir();
    }

    private IEnumerator Volando()
    {
        //Sale disparado hacia el fondo girando y se pierde como una estrella
        cortexFlotando = false;
        if (grito != null && grito.voz != null) fuenteVoz.PlayOneShot(grito.voz);
        Vector3 desde = cortex.position;
        Vector3 hasta = desde + new Vector3(14f, 9f, 55f);
        yield return Animar(2f, t =>
        {
            float s = 1f - Mathf.Pow(1f - t, 2f);
            cortex.position = Vector3.Lerp(desde, hasta, s) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 2f;
            cortex.rotation = Quaternion.Euler(t * 900f, t * 1300f, t * 500f);
            cortex.localScale = escalaCortex * (1f - t * 0.85f);
        });
        cortex.gameObject.SetActive(false);

        if (estrella != null)
        {
            estrella.position = hasta;
            estrella.gameObject.SetActive(true);
            if (sonidoEstrella != null) fuenteEfectos.PlayOneShot(sonidoEstrella);
            Vector3 escalaEstrella = estrella.localScale;
            yield return Animar(0.8f, t =>
            {
                estrella.localScale = escalaEstrella * Mathf.Sin(t * Mathf.PI) * 2.5f;
                estrella.rotation = Quaternion.LookRotation(estrella.position - camara.position) * Quaternion.Euler(0f, 0f, t * 180f);
            });
            estrella.gameObject.SetActive(false);
        }
    }

    private IEnumerator Decir(LineaDialogo linea)
    {
        if (linea.imagen != null && imagenDialogo != null)
        {
            cajaDialogo.SetActive(false);
            imagenDialogo.sprite = linea.imagen;
            imagenDialogo.gameObject.SetActive(true);
            if (linea.voz != null) fuenteVoz.PlayOneShot(linea.voz);
            yield return new WaitForSeconds((linea.voz != null ? linea.voz.length : 2.5f) + 0.5f);
            yield break;
        }

        cajaDialogo.SetActive(true);
        textoDialogo.text = "";
        float duracion = linea.voz != null ? linea.voz.length : 2.5f;
        if (linea.voz != null) fuenteVoz.PlayOneShot(linea.voz);

        //El texto aparece letra por letra mientras habla
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            int letras = Mathf.Min(linea.texto.Length, Mathf.CeilToInt(linea.texto.Length * t / (duracion * 0.75f)));
            textoDialogo.text = linea.texto.Substring(0, letras);
            yield return null;
        }
        textoDialogo.text = linea.texto;
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator Temblor(float fuerza, float duracion)
    {
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            camara.position = posCamara + Random.insideUnitSphere * fuerza * (1f - t / duracion);
            yield return null;
        }
        camara.position = posCamara;
    }

    private float VolumenVoz()
    {
        if (fuenteVoz == null || !fuenteVoz.isPlaying) return 0f;
        fuenteVoz.GetOutputData(muestras, 0);
        float suma = 0f;
        foreach (float m in muestras) suma += m * m;
        return Mathf.Clamp01(Mathf.Sqrt(suma / muestras.Length) * 6f);
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

    private IEnumerator Salir()
    {
        saliendo = true;
        cajaDialogo.SetActive(false);
        if (imagenDialogo != null) imagenDialogo.gameObject.SetActive(false);
        Color inicio = fundido.color;
        yield return Animar(0.8f, t => fundido.color = Color.Lerp(inicio, Color.black, t));

        if (pantallaGracias != null)
        {
            pantallaGracias.gameObject.SetActive(true);
            yield return Animar(0.6f, t => pantallaGracias.color = new Color(1f, 1f, 1f, t));
            if (botonMenu != null) botonMenu.SetActive(true);
            Cursor.visible = true;
            //Espera a que el jugador oprima el boton o Enter
            while (!volverAlMenu && !Input.GetKeyDown(KeyCode.Return)) yield return null;
        }

        Cursor.visible = true;
        SceneManager.LoadScene(escenaSiguiente);
    }

    public void IrAlMenu()
    {
        volverAlMenu = true;
    }
}

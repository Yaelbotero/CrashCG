using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IntroCortex : MonoBehaviour
{
    [System.Serializable]
    public class LineaDialogo
    {
        public string texto;
        public AudioClip voz;
        //Si tiene imagen se muestra la caja dibujada en vez del texto
        public Sprite imagen;
    }

    public Transform cortex;
    public Transform portal;
    public Transform[] capasPortal;
    public AudioSource fuenteVoz;
    public GameObject cajaDialogo;
    public Text textoDialogo;
    public Image imagenDialogo;
    public Image fundido;
    public LineaDialogo[] lineas;
    public GameObject panelMenu;
    public GameObject textoSaltar;
    //Puntos donde Cortex se queda flotando mientras habla
    public Vector3[] puntosHablar = { new Vector3(0.6f, 0.1f, 0f), new Vector3(-0.7f, 0.3f, 0.4f), new Vector3(0.2f, -0.1f, -0.6f) };
    public string escenaSiguiente = "SelectorNiveles";
    public string escenaRespaldo = "Gee Wiz";

    private readonly float[] muestras = new float[256];
    private Vector3 posBase;
    private Vector3 escalaCortex;
    private Vector3 escalaPortal;
    private float giroExtra = 0f;
    private bool flotando = false;
    private bool saliendo = false;
    private bool enIntro = false;

void Start()
    {
        escalaCortex = cortex.localScale;
        escalaPortal = portal.localScale;
        portal.localScale = Vector3.zero;
        cortex.localScale = Vector3.zero;
        cajaDialogo.SetActive(false);
        if (imagenDialogo != null) imagenDialogo.gameObject.SetActive(false);
        panelMenu.SetActive(false);
        if (textoSaltar != null) textoSaltar.SetActive(false);
        fundido.color = Color.black;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        AudioListener.volume = PlayerPrefs.GetFloat("volumen", 1f);
        StartCoroutine(AbrirPortal());
    }

    void Update()
    {
        //Cada capa del remolino gira a su propia velocidad y en sentido contrario a la anterior
        for (int i = 0; i < capasPortal.Length; i++)
        {
            float sentido = i % 2 == 0 ? 1f : -1f;
            capasPortal[i].Rotate(0f, 0f, sentido * (35f + 30f * i) * Time.deltaTime, Space.Self);
        }

        if (flotando)
        {
            //Flota y se estira un poco con el volumen de la voz para que parezca que habla
            float volumen = VolumenVoz();
            cortex.position = posBase + new Vector3(Mathf.Sin(Time.time * 1.3f) * 0.08f, Mathf.Sin(Time.time * 2.1f) * 0.12f, 0f);
            cortex.localScale = Vector3.Scale(escalaCortex, new Vector3(1f - volumen * 0.25f, 1f + volumen * 0.5f, 1f));
            cortex.rotation = Quaternion.Euler(Mathf.Sin(Time.time * 1.7f) * 6f - volumen * 20f, 180f + giroExtra + Mathf.Sin(Time.time * 0.9f) * 12f, Mathf.Sin(Time.time * 1.1f) * 5f);
        }

        if (enIntro && !saliendo && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape)))
        {
            StopAllCoroutines();
            StartCoroutine(Salir());
        }
    }

    private float VolumenVoz()
    {
        if (fuenteVoz == null || !fuenteVoz.isPlaying) return 0f;
        fuenteVoz.GetOutputData(muestras, 0);
        float suma = 0f;
        foreach (float m in muestras) suma += m * m;
        return Mathf.Clamp01(Mathf.Sqrt(suma / muestras.Length) * 6f);
    }

private IEnumerator AbrirPortal()
    {
        //El portal se abre mientras la pantalla aclara y queda de fondo del menu
        yield return Animar(1.4f, t =>
        {
            fundido.color = new Color(0f, 0f, 0f, 1f - t);
            float rebote = 1f + Mathf.Sin(t * Mathf.PI) * 0.15f;
            portal.localScale = escalaPortal * Mathf.SmoothStep(0f, 1f, t) * rebote;
        });
        panelMenu.SetActive(true);
    }

    public void Jugar()
    {
        if (enIntro) return;
        panelMenu.SetActive(false);
        enIntro = true;
        if (textoSaltar != null) textoSaltar.SetActive(true);
        StartCoroutine(Secuencia());
    }





    public void SalirDelJuego()
    {
        PlayerPrefs.Save();
        Application.Quit();
    }

    private IEnumerator Secuencia()
    {
        //Cortex sale desde el fondo del remolino girando
        Vector3 fondo = portal.position + Vector3.forward * 6f;
        yield return Volar(fondo, puntosHablar[0], 2.2f, 2f, true);

        for (int i = 0; i < lineas.Length; i++)
        {
            if (i > 0) yield return Volar(posBase, puntosHablar[i % puntosHablar.Length], 1.1f, 1f, false);
            yield return Decir(lineas[i]);
        }

        //Regresa al portal dando vueltas y el portal se cierra
        cajaDialogo.SetActive(false);
        if (imagenDialogo != null) imagenDialogo.gameObject.SetActive(false);
        yield return Volar(posBase, fondo, 1.4f, 3f, false);
        yield return Animar(0.8f, t => portal.localScale = escalaPortal * (1f - Mathf.SmoothStep(0f, 1f, t)));
        yield return Salir();
    }

    private IEnumerator Volar(Vector3 desde, Vector3 hasta, float duracion, float vueltas, bool creciendo)
    {
        flotando = false;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duracion;
            float s = Mathf.SmoothStep(0f, 1f, t);
            //El arco hace que no vuele en linea recta
            Vector3 arco = new Vector3(Mathf.Sin(s * Mathf.PI) * 1.4f, Mathf.Sin(s * Mathf.PI * 2f) * 0.5f, 0f);
            cortex.position = Vector3.Lerp(desde, hasta, s) + arco;
            cortex.rotation = Quaternion.Euler(Mathf.Sin(s * Mathf.PI) * 20f, 180f + s * 360f * vueltas, Mathf.Sin(s * Mathf.PI) * -25f);
            if (creciendo) cortex.localScale = escalaCortex * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 1.5f));
            yield return null;
        }
        cortex.localScale = escalaCortex;
        posBase = hasta;
        giroExtra = 0f;
        flotando = true;
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
        yield return Animar(0.7f, t =>
        {
            fundido.color = Color.Lerp(inicio, Color.black, t);
            fuenteVoz.volume = 1f - t;
        });

        string destino = Application.CanStreamedLevelBeLoaded(escenaSiguiente) ? escenaSiguiente : escenaRespaldo;
        SceneManager.LoadScene(destino);
    }
}

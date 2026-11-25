using UnityEngine;
using UnityEngine.SceneManagement;

public class PortalMeta : MonoBehaviour
{
    public TextMesh textoProgreso3D;
    public GameObject vfxTeletransporte;
    //Si tiene escena, el portal lleva ahi en vez de volver al selector
    public string escenaFinal = "";
    public float esperaEscenaFinal = 1.2f;

    //Pared invisible alineada con la cara del portal, en metros. Cruzarla cuenta como entrar aunque no se toque el portal
    public float alturaZona = 30f;
    public float grosorZona = 3f;
    public float anchoPared = 40f;

    private bool activado = false;
    private Transform jugador;
    private float ladoInicial;
    private float ladoPrevio;

    private void Awake()
    {
        BoxCollider zona = GetComponent<BoxCollider>();
        if (zona == null) return;
        zona.isTrigger = true;
        Vector3 escala = transform.lossyScale;
        float sy = Mathf.Max(0.0001f, Mathf.Abs(escala.y));
        float sz = Mathf.Max(0.0001f, Mathf.Abs(escala.z));
        float sx = Mathf.Max(0.0001f, Mathf.Abs(escala.x));
        Vector3 tam = zona.size;
        Vector3 centro = zona.center;
        float alto = Mathf.Max(tam.y, alturaZona / sy);
        //Crece hacia arriba manteniendo la base donde estaba
        centro.y += (alto - tam.y) / 2f;
        tam.y = alto;
        tam.z = Mathf.Max(tam.z, grosorZona / sz);
        tam.x = Mathf.Max(tam.x, anchoPared / sx);
        zona.size = tam;
        zona.center = centro;
    }

    private void Start()
    {
        GameObject j = GameObject.FindWithTag("Player");
        if (j == null) return;
        jugador = j.transform;
        ladoInicial = Mathf.Sign(Lado());
        ladoPrevio = ladoInicial;
    }

    private void Update()
    {
        if (textoProgreso3D != null && GestorJuego.Instancia != null)
        {
            string estadoGema = GestorJuego.Instancia.gemaObtenida ? "OK" : "Pendiente";
            textoProgreso3D.text = $"Cajas: {GestorJuego.Instancia.cajasRotas}/{GestorJuego.Instancia.cajasTotales}\nGema: {estadoGema}";
        }

        //Revisa si Crash paso al otro lado del plano del portal, por si va tan rapido que el trigger no lo detecta
        if (activado || jugador == null || !jugador.gameObject.activeInHierarchy) return;
        float lado = Lado();
        bool cruzo = Mathf.Sign(ladoPrevio) == ladoInicial && Mathf.Sign(lado) != ladoInicial;
        ladoPrevio = lado;
        if (cruzo && DentroDeLaPared()) Activar(jugador.gameObject);
    }

    private void LateUpdate()
    {
        //El cartel siempre mira a la camara para que no se lea al reves
        Camera cam = Camera.main;
        if (textoProgreso3D != null && cam != null)
        {
            Transform t = textoProgreso3D.transform;
            t.rotation = Quaternion.LookRotation(t.position - cam.transform.position, Vector3.up);
        }
    }

    private float Lado()
    {
        return Vector3.Dot(jugador.position - transform.position, transform.forward);
    }

    private bool DentroDeLaPared()
    {
        Vector3 d = jugador.position - transform.position;
        return Mathf.Abs(Vector3.Dot(d, transform.right)) <= anchoPared / 2f && d.y > -6f && d.y < alturaZona;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) Activar(other.gameObject);
    }

    private void Activar(GameObject crash)
    {
        if (activado) return;
        activado = true;

        if (vfxTeletransporte != null)
        {
            Instantiate(vfxTeletransporte, transform.position, Quaternion.identity);
        }

        if (!string.IsNullOrEmpty(escenaFinal) && Application.CanStreamedLevelBeLoaded(escenaFinal))
        {
            if (GestorJuego.Instancia != null)
            {
                GestorJuego.Instancia.DetenerRelojMeta();
                GestorJuego.Instancia.juegoTerminado = true;
                GestorJuego.Instancia.ReproducirSFX("checkpoint");
            }
            crash.SetActive(false);
            Invoke(nameof(CargarEscenaFinal), esperaEscenaFinal);
            return;
        }

        if (GestorJuego.Instancia != null)
        {
            GestorJuego.Instancia.DetenerRelojMeta();
            GestorJuego.Instancia.CompletarNivel();
        }
    }

    private void CargarEscenaFinal()
    {
        SceneManager.LoadScene(escenaFinal);
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;

public class PortalMeta : MonoBehaviour
{
    public TextMesh textoProgreso3D;
    public GameObject vfxTeletransporte;
    //Si tiene escena, el portal lleva ahi en vez de mostrar el panel de nivel completado
    public string escenaFinal = "";
    public float esperaEscenaFinal = 1.2f;

    private bool activado = false;

    private void Update()
    {
        if (textoProgreso3D != null && GestorJuego.Instancia != null)
        {
            string estadoGema = GestorJuego.Instancia.gemaObtenida ? "OK" : "Pendiente";
            textoProgreso3D.text = $"Cajas: {GestorJuego.Instancia.cajasRotas}/{GestorJuego.Instancia.cajasTotales}\nGema: {estadoGema}";
        }
    }

    //Al tocar el portal se pausa el juego y sale el panel de nivel completado
    private void OnTriggerEnter(Collider other)
    {
        if (activado || !other.CompareTag("Player")) return;
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
            other.gameObject.SetActive(false);
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

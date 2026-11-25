using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SelectorNiveles : MonoBehaviour
{
    [System.Serializable]
    public class Nivel
    {
        public string nombre;
        public string escena;
        public Transform plataforma;
        public Renderer anillo;
        public Transform cristal;
        public Color color = Color.cyan;
    }

    public Nivel[] niveles;
    public Transform crash;
    public Animator animCrash;
    public Text textoNombre;
    public Text textoEstado;
    public Image fundido;
    public float velocidadCrash = 4f;
    public float alturaCristal = 2.35f;
    public string escenaMenu = "MenuPrincipal";

    private int indice = -1;
    private bool ocupado;
    private Vector3 posInicio;

    void Start()
    {
        posInicio = crash.position;
        animCrash.SetBool("EnPiso", true);
        MostrarNivel(-1);
        StartCoroutine(Fundir(1f, 0f, 0.8f));
    }

    void Update()
    {
        AnimarPlataformas();
        if (ocupado) return;

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            StartCoroutine(IrA(Mathf.Min(indice + 1, niveles.Length - 1)));
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            StartCoroutine(IrA(indice < 0 ? 0 : Mathf.Max(indice - 1, 0)));
        else if (indice >= 0 && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)))
            StartCoroutine(Entrar());
        else if (Input.GetKeyDown(KeyCode.Escape))
            StartCoroutine(Salir(escenaMenu));
    }

    private void AnimarPlataformas()
    {
        for (int i = 0; i < niveles.Length; i++)
        {
            Nivel n = niveles[i];
            bool activo = i == indice;
            float brillo = activo ? 2.5f + Mathf.Sin(Time.time * 6f) * 1f : 0.6f;
            n.anillo.material.SetColor("_EmissionColor", n.color * brillo);
            n.cristal.Rotate(0f, (activo ? 160f : 40f) * Time.deltaTime, 0f, Space.World);
            Vector3 p = n.cristal.localPosition;
            p.y = alturaCristal + Mathf.Sin(Time.time * 2f + i) * 0.12f;
            n.cristal.localPosition = p;
        }
    }

    private IEnumerator IrA(int nuevo)
    {
        if (nuevo == indice) yield break;
        ocupado = true;
        MostrarNivel(-1);
        Vector3 destino = niveles[nuevo].plataforma.position;
        destino.y = posInicio.y;
        animCrash.SetFloat("Velocidad", velocidadCrash);
        while (Vector3.Distance(crash.position, destino) > 0.02f)
        {
            Vector3 dir = destino - crash.position;
            crash.rotation = Quaternion.Slerp(crash.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 12f);
            crash.position = Vector3.MoveTowards(crash.position, destino, velocidadCrash * Time.deltaTime);
            yield return null;
        }
        animCrash.SetFloat("Velocidad", 0f);
        Quaternion deFrente = Quaternion.Euler(0f, 180f, 0f);
        while (Quaternion.Angle(crash.rotation, deFrente) > 1f)
        {
            crash.rotation = Quaternion.RotateTowards(crash.rotation, deFrente, 540f * Time.deltaTime);
            yield return null;
        }
        indice = nuevo;
        MostrarNivel(indice);
        ocupado = false;
    }

    private void MostrarNivel(int i)
    {
        textoNombre.text = i < 0 ? "ELIGE UN NIVEL" : niveles[i].nombre;
        textoNombre.color = i < 0 ? Color.white : Color.Lerp(niveles[i].color, Color.white, 0.4f);
        if (i < 0) textoEstado.text = "";
        else textoEstado.text = Disponible(niveles[i]) ? "Enter para entrar" : "Próximamente";
    }

    private bool Disponible(Nivel n)
    {
        return !string.IsNullOrEmpty(n.escena) && Application.CanStreamedLevelBeLoaded(n.escena);
    }

    private IEnumerator Entrar()
    {
        Nivel n = niveles[indice];
        if (!Disponible(n))
        {
            ocupado = true;
            textoEstado.text = "Este nivel aún no está disponible";
            yield return new WaitForSeconds(1.2f);
            MostrarNivel(indice);
            ocupado = false;
            yield break;
        }
        ocupado = true;
        animCrash.SetBool("EnPiso", false);
        animCrash.SetTrigger("Saltar");
        Vector3 origen = crash.position;
        float t = 0f;
        //Crash sube girando hacia el cristal y se encoge
        while (t < 1f)
        {
            t += Time.deltaTime / 0.9f;
            crash.position = origen + Vector3.up * (t * t * 1.6f);
            crash.Rotate(0f, 900f * Time.deltaTime, 0f);
            crash.localScale = Vector3.one * 1.1f * (1f - t * 0.9f);
            yield return null;
        }
        yield return Salir(n.escena);
    }

    private IEnumerator Salir(string escena)
    {
        ocupado = true;
        yield return Fundir(0f, 1f, 0.6f);
        SceneManager.LoadScene(escena);
    }

    private IEnumerator Fundir(float desde, float hasta, float duracion)
    {
        fundido.gameObject.SetActive(true);
        for (float t = 0f; t < 1f; t += Time.deltaTime / duracion)
        {
            fundido.color = new Color(0f, 0f, 0f, Mathf.Lerp(desde, hasta, t));
            yield return null;
        }
        fundido.color = new Color(0f, 0f, 0f, hasta);
        if (hasta <= 0f) fundido.gameObject.SetActive(false);
    }
}

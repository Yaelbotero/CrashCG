using UnityEngine;

//T-Rex que persigue a Crash por la ruta de la cueva y cruza los huecos de lava de un salto
public class PerseguidorTrex : MonoBehaviour
{
    public Transform ruta;
    public Transform jugador;
    public Animator animator;
    public Camara camara;
    public AudioClip rugido;
    //Inicio y fin de cada hueco, medidos en metros sobre la ruta
    public Vector2[] huecos;

    public float velocidad = 5.8f;
    public float ventaja = 14f;
    public float distanciaMaxima = 30f;
    //Distancia del pivote a la punta del hocico, ya escalada
    public float alcanceHocico = 6f;
    public float esperaInicial = 2.4f;
    public float margenEspera = 6f;
    public float margenAterrizaje = 6.5f;
    public float alturaSalto = 6f;
    public float duracionSalto = 1.4f;
    public float duracionRugido = 1.5f;
    //Velocidad a la que la animacion Run no patina, ya multiplicada por la escala
    public float velocidadAnimCarrera = 8f;
    public float temblorPaso = 0.12f;
    public float temblorAterrizaje = 0.7f;
    public float radioRompeCajas = 3f;

    private enum Estado { Intro, Corre, Espera, Salta, Ruge }
    private Estado estado;
    private float tiempoEstado;
    private float progreso;
    private float inicioSalto;
    private float finSalto;
    private float ultimoProgresoJugador;
    private int ultimoPaso = -1;
    private Vector3[] puntos;
    private float[] recorrido;
    private MovoJugadorTPV movimiento;
    private AudioSource fuente;

    void Start()
    {
        puntos = new Vector3[ruta.childCount];
        recorrido = new float[ruta.childCount];
        for (int i = 0; i < puntos.Length; i++)
        {
            puntos[i] = ruta.GetChild(i).position;
            recorrido[i] = i == 0 ? 0f : recorrido[i - 1] + Vector3.Distance(puntos[i - 1], puntos[i]);
        }

        movimiento = jugador.GetComponent<MovoJugadorTPV>();
        fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;

        ultimoProgresoJugador = Progreso(jugador.position);
        Reiniciar(ultimoProgresoJugador);
    }

    void Update()
    {
        if (GestorJuego.Instancia != null && (GestorJuego.Instancia.juegoPausado || GestorJuego.Instancia.juegoTerminado)) return;

        float pj = Progreso(jugador.position);
        //Crash reaparecio en un checkpoint: el dinosaurio vuelve a quedar detras
        if (pj < ultimoProgresoJugador - 5f) Reiniciar(pj);
        ultimoProgresoJugador = pj;

        tiempoEstado += Time.deltaTime;
        float altura = 0f;

        switch (estado)
        {
            case Estado.Intro:
                if (tiempoEstado >= esperaInicial) Cambiar(Estado.Corre, "Run");
                break;

            case Estado.Corre:
                float atraso = pj - progreso - ventaja;
                progreso += (velocidad + Mathf.Max(0f, atraso) * 0.3f) * Time.deltaTime;
                progreso = Mathf.Max(progreso, pj - distanciaMaxima);

                int h = HuecoDelante();
                if (h >= 0 && progreso >= huecos[h].x - margenEspera)
                {
                    progreso = huecos[h].x - margenEspera;
                    Cambiar(Estado.Espera, "Idle");
                }
                else
                {
                    ContarPasos();
                    RomperCajas();
                    if (progreso + alcanceHocico >= pj) Morder();
                }
                break;

            case Estado.Espera:
                int hueco = HuecoDelante();
                if (hueco < 0) { Cambiar(Estado.Corre, "Run"); break; }
                //Espera a que Crash cruce la lava y salta cuando ya hay espacio para caer
                if (pj > huecos[hueco].y + margenAterrizaje + 2f)
                {
                    inicioSalto = progreso;
                    finSalto = huecos[hueco].y + margenAterrizaje;
                    Cambiar(Estado.Salta, "Run");
                }
                else if (progreso + alcanceHocico >= pj) Morder();
                break;

            case Estado.Salta:
                float t = Mathf.Clamp01(tiempoEstado / duracionSalto);
                progreso = Mathf.Lerp(inicioSalto, finSalto, t);
                altura = 4f * alturaSalto * t * (1f - t);
                if (t >= 1f) Aterrizar();
                break;

            case Estado.Ruge:
                if (tiempoEstado >= duracionRugido) Cambiar(Estado.Corre, "Run");
                break;
        }

        animator.speed = estado == Estado.Corre ? velocidad / velocidadAnimCarrera : 1f;
        Colocar(altura);
    }

    private void Reiniciar(float pj)
    {
        progreso = Mathf.Max(0f, pj - ventaja);
        Cambiar(Estado.Intro, "Roar");
        Rugir(temblorAterrizaje * 0.6f);
        Colocar(0f);
    }

    private void Cambiar(Estado nuevo, string animacion)
    {
        estado = nuevo;
        tiempoEstado = 0f;
        ultimoPaso = -1;
        animator.CrossFadeInFixedTime(animacion, 0.2f);
    }

    private void Aterrizar()
    {
        Cambiar(Estado.Ruge, "Roar");
        //Arranca el rugido cuando ya tiene la boca abierta para que coincida con el sonido
        animator.CrossFade("Roar", 0.1f, 0, 0.3f);
        Rugir(temblorAterrizaje);
    }

    private void Rugir(float fuerza)
    {
        if (rugido != null) fuente.PlayOneShot(rugido);
        if (camara != null) camara.Temblar(fuerza, 1.1f);
    }

    private void Morder()
    {
        Cambiar(Estado.Ruge, "Bite");
        movimiento.RecibirDano();
    }

    private int HuecoDelante()
    {
        for (int i = 0; i < huecos.Length; i++)
        {
            if (progreso < huecos[i].y + margenAterrizaje - 0.5f) return i;
        }
        return -1;
    }

    //Cada pisada sacude la pantalla, mas fuerte entre mas cerca este de la camara
    private void ContarPasos()
    {
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        if (!info.IsName("Run")) return;
        int paso = Mathf.FloorToInt(info.normalizedTime * 2f);
        if (paso == ultimoPaso) return;
        ultimoPaso = paso;
        if (camara == null) return;
        float cercania = Mathf.Clamp01(1f - Vector3.Distance(camara.transform.position, transform.position) / 45f);
        camara.Temblar(temblorPaso * (0.4f + cercania), 0.18f);
    }

    private void RomperCajas()
    {
        Vector3 centro = transform.position + transform.forward * 3f + Vector3.up;
        foreach (Collider c in Physics.OverlapSphere(centro, radioRompeCajas))
        {
            Caja caja = c.GetComponentInParent<Caja>();
            if (caja == null || caja.YaActivada || caja.tipo == Caja.TipoCaja.Checkpoint) continue;
            if (caja.tipo == Caja.TipoCaja.TNT || caja.tipo == Caja.TipoCaja.Nitro) caja.ExplotarSinDanoJugador();
            else caja.Golpear();
        }
    }

    private void Colocar(float altura)
    {
        Vector3 dir;
        Vector3 pos = PuntoEn(progreso, out dir);
        transform.position = pos + Vector3.up * altura;
        transform.rotation = Quaternion.LookRotation(dir);
    }

    private float Progreso(Vector3 posicion)
    {
        int cercano = 0;
        float mejor = float.MaxValue;
        for (int i = 0; i < puntos.Length; i++)
        {
            float d = (puntos[i] - posicion).sqrMagnitude;
            if (d < mejor)
            {
                mejor = d;
                cercano = i;
            }
        }
        //Se proyecta sobre el tramo para que el avance no vaya a saltos de punto en punto
        int a = Mathf.Max(0, cercano - 1);
        int b = Mathf.Min(puntos.Length - 1, cercano + 1);
        Vector3 tramo = puntos[b] - puntos[a];
        float t = tramo.sqrMagnitude > 0.0001f ? Mathf.Clamp01(Vector3.Dot(posicion - puntos[a], tramo) / tramo.sqrMagnitude) : 0f;
        return Mathf.Lerp(recorrido[a], recorrido[b], t);
    }

    private Vector3 PuntoEn(float distancia, out Vector3 direccion)
    {
        distancia = Mathf.Clamp(distancia, 0f, recorrido[recorrido.Length - 1]);
        int i = 1;
        while (i < recorrido.Length - 1 && recorrido[i] < distancia) i++;
        float t = Mathf.InverseLerp(recorrido[i - 1], recorrido[i], distancia);
        direccion = puntos[i] - puntos[i - 1];
        direccion.y = 0f;
        if (direccion.sqrMagnitude < 0.0001f) direccion = Vector3.forward;
        return Vector3.Lerp(puntos[i - 1], puntos[i], t);
    }
}

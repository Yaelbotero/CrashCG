using UnityEngine;

//Manada de cabras que corre detras de Crash siguiendo la ruta del nivel
public class PerseguidorManada : MonoBehaviour
{
    public Transform ruta;
    public Transform jugador;
    public Transform[] cabras;
    public float velocidad = 5.2f;
    public float aceleracion = 0.025f;
    public float ventaja = 14f;
    public float distanciaMaxima = 22f;
    public float distanciaAtrapa = 1.4f;
    public float inicioPersecucion = 6f;
    public float anchoManada = 2.2f;

    private Vector3[] puntos;
    private float[] recorrido;
    private Vector3[] desfases;
    private Animator[] animadores;
    private float progresoManada;
    private float velocidadActual;
    private float ultimoProgreso;
    private bool activa;
    private MovoJugadorTPV movimiento;

    void Start()
    {
        puntos = new Vector3[ruta.childCount];
        recorrido = new float[ruta.childCount];
        for (int i = 0; i < puntos.Length; i++)
        {
            puntos[i] = ruta.GetChild(i).position;
            recorrido[i] = i == 0 ? 0f : recorrido[i - 1] + Vector3.Distance(puntos[i - 1], puntos[i]);
        }

        desfases = new Vector3[cabras.Length];
        animadores = new Animator[cabras.Length];
        for (int i = 0; i < cabras.Length; i++)
        {
            desfases[i] = new Vector3(Random.Range(-anchoManada, anchoManada), Random.Range(0f, 6.28f), Random.Range(0f, 3.5f));
            animadores[i] = cabras[i].GetComponentInChildren<Animator>();
        }

        movimiento = jugador.GetComponent<MovoJugadorTPV>();
        ultimoProgreso = Progreso(jugador.position);
        progresoManada = ultimoProgreso - ventaja;
        Colocar();
    }

    void Update()
    {
        if (GestorJuego.Instancia != null && (GestorJuego.Instancia.juegoPausado || GestorJuego.Instancia.juegoTerminado)) return;

        float progresoJugador = Progreso(jugador.position);

        //Si Crash reaparece mas atras, la manada vuelve a quedar detras de el
        if (progresoJugador < ultimoProgreso - 5f)
        {
            activa = false;
        }
        ultimoProgreso = progresoJugador;

        if (!activa)
        {
            progresoManada = progresoJugador - ventaja;
            velocidadActual = velocidad;
            activa = progresoJugador > inicioPersecucion && progresoJugador < recorrido[recorrido.Length - 1] - 8f;
        }
        else
        {
            velocidadActual += aceleracion * Time.deltaTime;
            progresoManada += velocidadActual * Time.deltaTime;
            progresoManada = Mathf.Max(progresoManada, progresoJugador - distanciaMaxima);

            if (progresoManada >= progresoJugador - distanciaAtrapa)
            {
                movimiento.RecibirDano();
                progresoManada = progresoJugador - ventaja;
                velocidadActual = velocidad;
            }
        }

        Colocar();
    }

    private void Colocar()
    {
        for (int i = 0; i < cabras.Length; i++)
        {
            Vector3 dir;
            Vector3 pos = PuntoEn(progresoManada - desfases[i].z, out dir);
            Vector3 lado = Vector3.Cross(Vector3.up, dir).normalized;
            //Cada cabra va dando saltitos a su propio ritmo
            float salto = activa ? Mathf.Abs(Mathf.Sin(Time.time * 9f + desfases[i].y)) * 0.25f : 0f;
            cabras[i].position = pos + lado * desfases[i].x + Vector3.up * salto;
            cabras[i].rotation = Quaternion.LookRotation(dir);
            if (animadores[i] != null) animadores[i].SetFloat("Velocidad", activa ? velocidadActual : 0f);
        }
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
        return recorrido[cercano];
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

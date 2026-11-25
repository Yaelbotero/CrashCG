using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camara : MonoBehaviour
{
    //Rotaciones en X y Y
    public float rotX = 25f;
    public float rotY = 0f;

    //Nuestro objeto a seguir
    public Transform target;

    //Distancia de la camara a nuestro objeto a seguir
    public float distanceTarget = 3.0f;
    public Vector3 offsetMira = Vector3.zero;
    public float suavizado = 0f;

    //Puntos del camino curvo, la camara gira siguiendo el rumbo del mas cercano
    public Transform rutaCamara;
    public float suavizadoRuta = 3f;
    //Toma el rumbo de un punto que queda atras de Crash para que la curva se vea venir antes de girar
    public int puntosRetraso = 2;
    //Giro lateral de la vista 2.5D del bonus
    public float giroVistaBonus = 90f;
    //Objetos que se esconden en el bonus porque quedan entre la camara y Crash
    public string prefijoOcultoBonus = "Torreon_Bonus";
    private float rumboRuta = 0f;

    private float rotXBase;
    private float rotYBase;
    private float distBase;
    private float distanciaActual;
    private bool vistaBonus = false;
    private bool enTransicionVista = false;
    private Vector3 velSuavizado = Vector3.zero;
    private Vector3 posSinTemblor;
    private float temblorFuerza;
    private float temblorDuracion;
    private float temblorRestante;
    private readonly Dictionary<Renderer, UnityEngine.Rendering.ShadowCastingMode> tapables = new Dictionary<Renderer, UnityEngine.Rendering.ShadowCastingMode>();

    void Start()
    {
        //Bloqueamos la posicion del mouse y lo desaparecemos de la pantalla
        Cursor.lockState = CursorLockMode.Locked;
        rotXBase = rotX;
        rotYBase = rotY;
        distBase = distanceTarget;
        distanciaActual = distanceTarget;
        posSinTemblor = transform.position;
    }

    //Sacude la camara; un temblor fuerte no lo tapa uno mas debil
    public void Temblar(float fuerza, float duracion)
    {
        float actual = temblorRestante > 0f ? temblorFuerza * temblorRestante / temblorDuracion : 0f;
        if (fuerza < actual) return;
        temblorFuerza = fuerza;
        temblorDuracion = Mathf.Max(0.01f, duracion);
        temblorRestante = temblorDuracion;
    }

    public void ConfigurarVistaBonus(bool activa)
    {
        vistaBonus = activa;
        enTransicionVista = true;
        PrepararTapables(activa);

        if (suavizado <= 0.001f)
        {
            rotX = activa ? 10f : rotXBase;
            rotY = activa ? giroVistaBonus : rotYBase;
            distanceTarget = activa ? 10.5f : distBase;
            distanciaActual = distanceTarget;
            enTransicionVista = false;
        }
    }

    public void OrientarHaciaAdelante()
    {
        vistaBonus = false;
        enTransicionVista = false;
        rotX = 16f;
        rotY = 0f;
        if (distBase > 0f)
        {
            distanceTarget = distBase;
            distanciaActual = distBase;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        if (enTransicionVista)
        {
            float objRotX = vistaBonus ? 10f : rotXBase;
            float objRotY = vistaBonus ? giroVistaBonus : rotYBase;
            float objDist = vistaBonus ? 10.5f : distBase;
            float t = Mathf.Clamp01(Time.deltaTime / Mathf.Max(0.05f, suavizado));

            rotX = Mathf.Lerp(rotX, objRotX, t);
            rotY = Mathf.Lerp(rotY, objRotY, t);
            distanciaActual = Mathf.Lerp(distanciaActual, objDist, t);
            distanceTarget = distanciaActual;

            if (!vistaBonus &&
                Mathf.Abs(rotX - objRotX) < 0.05f &&
                Mathf.Abs(rotY - objRotY) < 0.05f &&
                Mathf.Abs(distanciaActual - objDist) < 0.05f)
            {
                rotX = objRotX;
                rotY = objRotY;
                distanciaActual = objDist;
                distanceTarget = objDist;
                enTransicionVista = false;
            }
        }
        else
        {
            distanciaActual = distanceTarget;
        }

        ActualizarRumboRuta();
        HandleRotation();
        HandlePosition();
        AplicarTemblor();
    }

    void ActualizarRumboRuta()
    {
        if (rutaCamara == null || rutaCamara.childCount == 0) return;

        int indiceCercano = 0;
        float mejor = float.MaxValue;
        for (int i = 0; i < rutaCamara.childCount; i++)
        {
            float distancia = (rutaCamara.GetChild(i).position - target.position).sqrMagnitude;
            if (distancia < mejor)
            {
                mejor = distancia;
                indiceCercano = i;
            }
        }
        Transform cercano = rutaCamara.GetChild(Mathf.Max(0, indiceCercano - puntosRetraso));
        rumboRuta = Mathf.LerpAngle(rumboRuta, cercano.eulerAngles.y, suavizadoRuta * Time.deltaTime);
    }

    void HandleRotation()
    {
        transform.rotation = Quaternion.Euler(rotX, rotY + rumboRuta, 0);
    }

    void HandlePosition()
    {
        Vector3 puntoMira = target.position + offsetMira;
        Vector3 posDeseada = puntoMira - transform.forward * distanciaActual;
        if (suavizado > 0.001f)
        {
            posSinTemblor = Vector3.SmoothDamp(posSinTemblor, posDeseada, ref velSuavizado, suavizado);
        }
        else
        {
            posSinTemblor = posDeseada;
        }
        transform.position = posSinTemblor;
    }

    void AplicarTemblor()
    {
        if (temblorRestante <= 0f) return;
        temblorRestante -= Time.deltaTime;
        float f = temblorFuerza * Mathf.Clamp01(temblorRestante / temblorDuracion);
        transform.position += Random.insideUnitSphere * f;
        transform.rotation *= Quaternion.Euler(0f, 0f, Random.Range(-1f, 1f) * f * 4f);
    }

    //En el bonus se esconden los torreones que tapan a Crash, pero siguen dando sombra
    void PrepararTapables(bool activa)
    {
        foreach (var par in tapables)
        {
            if (par.Key != null) par.Key.shadowCastingMode = par.Value;
        }
        tapables.Clear();
        if (!activa || string.IsNullOrEmpty(prefijoOcultoBonus)) return;

        foreach (MeshRenderer r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            Transform t = r.transform;
            while (t != null && !t.name.StartsWith(prefijoOcultoBonus)) t = t.parent;
            if (t == null) continue;
            tapables[r] = r.shadowCastingMode;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }
    }
}

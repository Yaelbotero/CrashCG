using UnityEngine;

//Mueve la textura para que la lava parezca fluir y hace latir su brillo
public class DesplazarTextura : MonoBehaviour
{
    public Vector2 velocidad = new Vector2(0.015f, 0.04f);
    public Light luz;
    public float pulso = 0.35f;

    private Material material;
    private Vector2 desfase;
    private float intensidadBase;

    void Start()
    {
        material = GetComponent<Renderer>().material;
        if (luz != null) intensidadBase = luz.intensity;
    }

    void Update()
    {
        desfase += velocidad * Time.deltaTime;
        material.SetTextureOffset("_BaseMap", desfase);
        if (luz != null) luz.intensity = intensidadBase * (1f + Mathf.Sin(Time.time * 2.3f + transform.position.z) * pulso);
    }
}

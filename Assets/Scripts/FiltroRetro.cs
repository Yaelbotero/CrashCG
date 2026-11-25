using UnityEngine;
using UnityEngine.UI;

//La camara dibuja en una textura de baja resolucion 4:3 como la PS1 y se muestra sin suavizado con barras negras a los lados
[RequireComponent(typeof(Camera))]
public class FiltroRetro : MonoBehaviour
{
    public int anchoPixeles = 320;
    public int altoPixeles = 240;

    private Camera camara;
    private RenderTexture textura;
    private RawImage imagen;
    private int anchoPantalla;
    private int altoPantalla;

    void Start()
    {
        camara = GetComponent<Camera>();

        //Camara que solo limpia la pantalla para que el lienzo tenga donde dibujarse
        Camera pantalla = new GameObject("Camara_Pantalla").AddComponent<Camera>();
        pantalla.cullingMask = 0;
        pantalla.clearFlags = CameraClearFlags.SolidColor;
        pantalla.backgroundColor = Color.black;
        pantalla.depth = camara.depth - 10f;

        Canvas lienzo = new GameObject("Lienzo_Retro").AddComponent<Canvas>();
        lienzo.renderMode = RenderMode.ScreenSpaceOverlay;
        lienzo.sortingOrder = -100;

        imagen = new GameObject("Imagen_Retro").AddComponent<RawImage>();
        imagen.transform.SetParent(lienzo.transform, false);
        imagen.raycastTarget = false;
        RectTransform rt = imagen.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        AspectRatioFitter ajuste = imagen.gameObject.AddComponent<AspectRatioFitter>();
        ajuste.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        ajuste.aspectRatio = (float)anchoPixeles / altoPixeles;

        CrearTextura();
    }

    void Update()
    {
        if (Screen.width != anchoPantalla || Screen.height != altoPantalla)
        {
            CrearTextura();
        }
    }

    private void CrearTextura()
    {
        anchoPantalla = Screen.width;
        altoPantalla = Screen.height;

        if (textura != null)
        {
            camara.targetTexture = null;
            textura.Release();
        }

        textura = new RenderTexture(anchoPixeles, altoPixeles, 24);
        textura.filterMode = FilterMode.Point;
        camara.targetTexture = textura;
        imagen.texture = textura;
    }

    void OnDestroy()
    {
        if (camara != null) camara.targetTexture = null;
        if (textura != null) textura.Release();
    }
}

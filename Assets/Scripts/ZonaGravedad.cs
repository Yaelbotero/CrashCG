using UnityEngine;

public class ZonaGravedad : MonoBehaviour
{
    public Vector3 direccionGravedad = Vector3.down;
    public float magnitudGravedad = 9.5f;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        other.GetComponent<MovoJugadorTPV>()?.CambiarGravedad(direccionGravedad, magnitudGravedad);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        other.GetComponent<MovoJugadorTPV>()?.RestaurarGravedad();
    }
}

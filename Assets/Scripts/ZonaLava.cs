using UnityEngine;

//Si Crash cae a la lava pierde una vida, tenga o no mascara
public class ZonaLava : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        MovoJugadorTPV jugador = other.GetComponent<MovoJugadorTPV>();
        if (jugador != null) jugador.Caer();
    }
}

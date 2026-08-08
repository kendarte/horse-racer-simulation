using UnityEngine;

public class SimpleAudioPlayer : MonoBehaviour
{
    [Header("Configuración de Audio")]
    [Tooltip("Arrastre aquí el AudioSource que contiene el sonido de ambiente.")]
    public AudioSource AudioAReproducir;

    void Awake()
    {
        if (AudioAReproducir != null)
        {
            // Obliga a que arranque desde el milisegundo cero exacto al iniciar la escena
            AudioAReproducir.time = 0f;
            AudioAReproducir.Play();
        }
    }
}
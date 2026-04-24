using UnityEngine;

public class SoundBank : MonoBehaviour
{
    public static SoundBank Instance;

    [Header("Sonidos de impacto")]
    public AudioClip impactoPared;
    public AudioClip impactoObstaculo;

    // AudioSource en 2D — suena sin importar la distancia en la escena
    AudioSource _source;

    void Awake()
    {
        Instance = this;
        _source = gameObject.AddComponent<AudioSource>();
        _source.spatialBlend = 0f; // 2D — sin atenuacion por distancia
        _source.playOnAwake  = false;
    }

    public void PlayPared()
    {
        if (impactoPared != null)
            _source.PlayOneShot(impactoPared, 1f);
    }

    public void PlayObstaculo()
    {
        if (impactoObstaculo != null)
            _source.PlayOneShot(impactoObstaculo, 1f);
    }
}

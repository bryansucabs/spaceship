using UnityEngine;
using Photon.Pun;

public class NetworkSetup : MonoBehaviourPun
{
    [Header("Componentes a desactivar en naves rivales")]
    public MonoBehaviour[] scriptsLocales; // Arrastra aquí scripts como AccelHUD o EngineAudio
    public GameObject camaraDeNave; // Arrastra aquí el objeto de la cámara hija de tu nave

    void Start()
    {
        // Si esta nave NO es la mía, apago todo lo que me estorba
        if (!photonView.IsMine)
        {
            if (camaraDeNave != null)
            {
                camaraDeNave.SetActive(false);
                
                if (camaraDeNave.TryGetComponent(out AudioListener listener))
                {
                    listener.enabled = false;
                }
            }

            // Desactiva scripts de UI o de audio local
            foreach (MonoBehaviour script in scriptsLocales)
            {
                if (script != null)
                {
                    script.enabled = false;
                }
            }
        }
    }
}
using UnityEngine;
using Photon.Pun;

// Este script debe estar en la RAÍZ del prefab Overlord (mismo GameObject que el PhotonView).
// PUN2 busca métodos [PunRPC] usando GetComponents<MonoBehaviour>() en el ROOT solamente.
// OverlordHUD vive en el hijo "Camera", por eso sus [PunRPC] nunca eran encontrados.
public class OverlordRPCBridge : MonoBehaviourPun
{
    OverlordHUD _hud;

    void Awake()
    {
        // true = incluir objetos inactivos (NetworkSetup desactiva el hijo Camera
        // en clientes no-Overlord, pero igual necesitamos la referencia para los RPCs)
        _hud = GetComponentInChildren<OverlordHUD>(true);
    }

    [PunRPC]
    void RPC_IniciarSabotaje(float duracion) => _hud?.AplicarEfectosSabotaje(duracion);

    [PunRPC]
    void RPC_IniciarFreno(float duracion) => _hud?.AplicarEfectosFreno(duracion);

    [PunRPC]
    void RPC_RomperPasadizo(string nombre) => _hud?.AplicarRotacionPasadizo(nombre);
}

using Opsive.UltimateCharacterController.Game;
using Opsive.UltimateCharacterController.Utility;
using UnityEngine;

public enum eTeleportTypes
{
    World,
    Building,
    Space,

}

public enum eTeleportTarget
{
    None,
    Won_Lobby_DigitalTraining,
    Won_Lobby_SmallBusiness,
    Won_Lobby_StaffCounse,
    WB_Planet_WorldMap,
    Won_Room_SmallBusiness,
    Won_Room_SmallMeeting,
    Won_Room_StaffCounse,
    GwangTong_2Way,
    GwangTong_3Way,
}

[RequireComponent(typeof(BoxCollider))]
public class TeleportTrigger : MonoBehaviour
{

    [SerializeField] private eTeleportTypes teleportType;
    public eTeleportTypes TeleportType { get { return teleportType; } }

    [SerializeField] private eTeleportTarget telepoartTarget = eTeleportTarget.None;
    public eTeleportTarget TelepoartTarget { get { return telepoartTarget; } }

    [SerializeField] private bool isPlanetToBuilding = false;

    [SerializeField] protected LayerMask m_LayerMask = 1 << LayerManager.Character;


    private bool m_IgnoreCharacterEnter;
    public bool IgnoreCharacterEnter { set { m_IgnoreCharacterEnter = value; } }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.name.Contains("RoomUMA")) return;
        if (!other.gameObject.name.Contains("ME")) return;
        if (!MathUtility.InLayerMask(other.gameObject.layer, m_LayerMask) || m_IgnoreCharacterEnter) return;

#if MOIM_ENABLE_LOGS
        MoimDebug.Log("Player On Trigger Enter");
#endif

        TeleportController teleportController = TeleportController.Instance;
        if(teleportController != null)
        {

            if (gameObject.name.ToLower().Contains("gwangtong"))
                //반대쪽으로 이동 
                teleportController.ShowWarpPopup(teleportType, telepoartTarget, other);
            else if(teleportType == eTeleportTypes.World)
                teleportController.ShowWarpPopup(teleportType, other, gameObject.name);
            else if(teleportType == eTeleportTypes.Building)
                teleportController.ShowTeleportPopup(TelepoartTarget, isPlanetToBuilding);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.name.Contains("RoomUMA")) return;
        if (!other.gameObject.name.Contains("ME")) return;
        if (!MathUtility.InLayerMask(other.gameObject.layer, m_LayerMask)) return;

#if MOIM_ENABLE_LOGS
        MoimDebug.Log("Player On Trigger Exit");
#endif

        TeleportController teleportController = TeleportController.Instance;
        if (teleportController != null)
        {
            teleportController.CloseWarpPopup();
        }

            m_IgnoreCharacterEnter = false;
    }

    
}

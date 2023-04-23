using I2.Loc;
using Opsive.UltimateCharacterController.Character;
using System;
using System.Collections;
using System.Collections.Generic;
using TFVR.Core;
using UnityEngine;

//정의
//Warp : 맵내 이동
//Teleport : 다른 빌딩(씬)으로 이동
public class TeleportController : MonoBehaviour
{
    #region Singleton

    private static TeleportController s_instance;

    public static TeleportController Instance
    {
        get
        {
            if (s_instance == null)
            {
                // Search for existing instance.
                s_instance = (TeleportController)FindObjectOfType(typeof(TeleportController));

                // Create new instance if one doesn't already exist.
                if (s_instance == null)
                {
                    // Need to create a new GameObject to attach the singleton to.
                    var singletonObject = new GameObject();
                    s_instance = singletonObject.AddComponent<TeleportController>();
                    singletonObject.name = typeof(TeleportController).ToString() + " (Singleton)";
                }
            }
            return s_instance;
        }
    }

    #endregion Singleton

    public List<Transform> GwangTong2WayList;
    public List<Transform> GwangTong3WayList;
    private UltimateCharacterLocomotion characterLocomotion;

    public string targetName;
    private bool isJoinning;
    private string m_DestinationName;
    private Vector3 m_DestinationPosition;
    private Vector3 m_DestinationRotation;
    private Transform m_DestinationTransform;

    private TeleportData m_SelectedTeleportData;
    private Collider m_SelectedCollider;
    private bool m_SnapAnimator;

    [SerializeField] List<TeleportData> m_LobbyData;//로컬에 저장된 로비 데이터

    [SerializeField] List<TeleportTrigger> teleportPositionList;//로컬에 저장된 로비 데이터

    public Canvas_Teleport canvas_Teleport;

    public eTeleportTarget eTeleportTarget;

    public int pullCount = 10;
    private string nextKey = string.Empty;

    public XRWebAPI.RoomInfo selectedRoomInfo;

    private Dictionary<string, BuildingLobbyId> buildingLobbyIdDic = new Dictionary<string, BuildingLobbyId>();
    
    public class BuildingLobbyId
    {
        public string pid;
        public string bid;
        public int roomType;


        public BuildingLobbyId(string pid, string bid, int roomType)
        {
            this.pid = pid;
            this.bid = bid;
            this.roomType = roomType;
        }
    }


    private void Awake()
    {
        if (s_instance != null)
        {
#if MOIM_ENABLE_LOGS
            MoimDebug.Log("Teleport Controller instance is not null");
#endif
            Destroy(gameObject);
            return;
        }

    }

    private void OnEnable()
    {
        buildingLobbyIdDic.Clear();
    }



    public void ShowWarpPopup(eTeleportTypes teleportType, Collider collider, string name)
    {
        if (teleportType != eTeleportTypes.World) return;

        m_SelectedCollider = collider;

        if (canvas_Teleport == null)
        {
            GetCanvas();
        }

        if (canvas_Teleport != null)
        {
            canvas_Teleport.gameObject.SetActive(true);
          
            foreach (var item in teleportPositionList)
            {
                if (item.TeleportType != teleportType) continue;
                if (item.TeleportType == eTeleportTypes.World && item.gameObject.name == name) continue;

                TeleportData teleportData = new TeleportData();
                teleportData.potalTransform = item.transform;
                teleportData.targetName = item.name;
                teleportData.teleportType = item.TeleportType;

                canvas_Teleport.CreateItem(teleportData);
            }
        }
    }

    public void ShowWarpPopup(eTeleportTypes teleportType, eTeleportTarget telepoartTarget, Collider other)
    {
        TF_PopupMessage.Show(ScriptLocalization.Popup_Teleport, ScriptLocalization.Common_OK, () => OnButtonWarp(teleportType, telepoartTarget, other), string.Empty, ScriptLocalization.Common_Cancle);
    }

    //버튼으로 워프 팝업 활성화 
    public void ShowWarpPopup()
    {
        m_SelectedCollider = null;

        if (canvas_Teleport == null)
        {
            GetCanvas();
        }

        if (canvas_Teleport != null)
        {
            canvas_Teleport.gameObject.SetActive(true);

            foreach (var item in teleportPositionList)
            {
                if (item.TeleportType != eTeleportTypes.World) continue;

                TeleportData teleportData = new TeleportData();
                teleportData.potalTransform = item.transform;
                teleportData.targetName = item.name;
                teleportData.teleportType = item.TeleportType;

                canvas_Teleport.CreateItem(teleportData);
            }
        }
    }

    public void ShowSpacePopup()
    {
        m_SelectedCollider = null;

        if (canvas_Teleport == null)
        {
            TF_RoomUserMenu roomUserMenu = TF_RoomUserMenu.Instance;
            if (roomUserMenu != null)
            {
                canvas_Teleport = roomUserMenu.canvas_Teleport;
                if (canvas_Teleport == null)    
                {
#if MOIM_ENABLE_LOGS
                    MoimDebug.Log("Teleport Canvas 없음");
#endif
                    return;
                }
            }
        }

        if (canvas_Teleport != null)
        {
            canvas_Teleport.gameObject.SetActive(true);



#if MOIM_ENABLE_LOGS
            MoimDebug.Log("Build ID : " + Me.room.eid);
#endif
            #region OnLine
            //if (teleportPositionList.Count <= 0) return;
            //            XRWebAPI.GetAllList(Me.room.eid, pullCount, nextKey, MoimType.Space, (error, data) =>
            //            {
            //#if MOIM_ENABLE_LOGS
            //                MoimDebug.Log("Get All List Error Code " + error.code);
            //#endif

            //                if (error.code == 0)
            //                {
            //                    if (data.response.results != null)
            //                    {
            //#if MOIM_ENABLE_LOGS
            //                        MoimDebug.Log("Get AllList Data Length : " + data.response.results.Length);
            //#endif

            //                        for (int i = 0; i < data.response.results.Length; i++)
            //                        {
            //#if MOIM_ENABLE_LOGS
            //                            MoimDebug.Log($"Add Dic Data : {data.response.results[i].id} / {data.response.results[i].title}");
            //#endif
            //                            TeleportData teleportData = new TeleportData(data.response.results[i].id, data.response.results[i].title);

            //                            canvas_Teleport.CreateItem(teleportData);
            //                        }
            //                    }
            //                    else
            //                    {
            //                        MoimDebug.Log($"<color=Yellow>오픈 이벤트 없음</color>");
            //                    }

            //                    if (data.response.hasNext && !string.IsNullOrEmpty(data.response.next))
            //                    {
            //                        nextKey = data.response.next;
            //                    }
            //                    else
            //                    {
            //                        nextKey = string.Empty;
            //                    }

            //                }
            //                else
            //                {
            //                    ShowErrorPopup(error.code);
            //                }
            //            });
            #endregion

            foreach (var item in teleportPositionList)
            {
                if (item.TeleportType != eTeleportTypes.Space) continue;

                //building Id 


                TeleportData teleportData = new TeleportData();
                teleportData.potalTransform = item.transform;
                teleportData.targetName = item.name;
                teleportData.teleportType = item.TeleportType;
                teleportData.telepoartTarget = item.TelepoartTarget;

                canvas_Teleport.CreateItem(teleportData);

            }
        }
    }

    public void CloseWarpPopup()
    {
        if (canvas_Teleport == null)
        {
            TF_RoomUserMenu roomUserMenu = TF_RoomUserMenu.Instance;
            if (roomUserMenu != null)
            {
                canvas_Teleport = roomUserMenu.canvas_Teleport;
                if (canvas_Teleport == null)
                {
                    return;
                }
            }
        }

        canvas_Teleport.OnButtonCancelBtn();
    }

    TF_PopupMessage popup;

    public void ShowTeleportPopup(eTeleportTarget telepoartTarget, bool isPlanetToBuilding = false)
    {
        if(popup != null)
        {
            popup.OnPressCancle();
            return;
        }
        eTeleportTarget = telepoartTarget;
        
        if (Me.moimType == MoimType.Planet)
        {
            if (buildingLobbyIdDic.Count <= 0)
                GetBuildingLobbyData();
        }

        string subMSG = LocalizationManager.GetTranslation(telepoartTarget.ToString());
        popup = TF_PopupMessage.Show(ScriptLocalization.Popup_Teleport, ScriptLocalization.Common_OK, () =>
        {
            if (Me.moimType == MoimType.Planet)
                MoveBuilding(telepoartTarget);
            else if (Me.moimType == MoimType.Building)
                LeaveBuilding(telepoartTarget);
            else if (Me.moimType == MoimType.Event)
                ReturnLobby(telepoartTarget);
        }, subMSG, ScriptLocalization.Common_Cancle);


    }

    

    public void SetTeleportData(TeleportData obj)
    {
        m_SelectedTeleportData = obj;
        if (obj.teleportType == eTeleportTypes.Building)
        {
        }
        else if (obj.teleportType == eTeleportTypes.World)
        {
            //로컬이동
            m_DestinationName = obj.targetName;
            //if(m_SelectedCollider != null)
            //{
            //    m_DestinationPosition = obj.PotalPosition;
            //    m_DestinationRotation = obj.PotalRotation;
            //}
            //else 
            //{
                m_DestinationTransform = obj.potalTransform;
            //}

        }

    }

    //Building으로 이동
    private void MoveBuilding(eTeleportTarget eTeleportTarget, bool creating = false)
    {
        if (eTeleportTarget == eTeleportTarget.None) return;

#if MOIM_ENABLE_LOGS
        MoimDebug.Log("다음으로 텔레포트 : " + eTeleportTarget.ToString());
#endif

        Stage selectedStage = GameSettings.FindBuildingLobby((int)eTeleportTarget);

        if (GameSettings.CheckNotUseStage(selectedStage.sceneName))
        {
#if MOIM_ENABLE_LOGS
            MoimDebug.Log("This is Not Use Scene");
#endif
            TF_PopupMessage.Show(ScriptLocalization.Need_Upadate_Title, ScriptLocalization.Common_OK, null, ScriptLocalization.Need_Upadate_Content);
            return;
        }

        if(Me.moimType == MoimType.Planet)
        {
            if (!buildingLobbyIdDic.ContainsKey(selectedStage.buildingPermenantId))
            {
#if MOIM_ENABLE_LOGS
                MoimDebug.Log("Not Contains Key");
#endif
                return;
            }
        }
        
        MoimDebug.Log("//////////////////////////");

        if (Me.lastRoom != null)
        {
            Debug.Log("집에 가즈아 !!" + Me.lastRoom.roomid +"//" + Me.lastPlanetId + "//" + Me.moimType);
        }
        MoimDebug.Log("//////////////////////////");



        RoomType roomType = Me.moimType == MoimType.Planet ? (RoomType)buildingLobbyIdDic[selectedStage.buildingPermenantId].roomType : Me.lastRoom.roomtype;
        string id = Me.moimType == MoimType.Planet ? buildingLobbyIdDic[selectedStage.buildingPermenantId].bid : Me.lastRoom.roomid;//빌딩 ID
        string pid = Me.moimType == MoimType.Planet ? buildingLobbyIdDic[selectedStage.buildingPermenantId].pid : Me.lastPlanetId;
        string password = string.Empty;
        int linkType = 0;
        int userType = NetEventJoin.stdUserType;
        string observerPassword = string.Empty;
        int channelId = Me.room.channelId;



        Me.room.LeaveRoom(() =>
        {
            if (TF_Manager.Instance != null)
            {
                if (Me.moimType == MoimType.Planet)
                    Me.moimType = MoimType.Building;

                TF_Manager.Instance.ExitRoom(null, false, false, false);
                TF_Manager.Instance.OnJoinBuildingLobby(roomType, id, pid, selectedStage, password, linkType, userType, observerPassword, channelId);
            }
        }, MoimType.Planet);


        #region OffLine
        /*
        //TF_Manager.Instance.ExitRoom(null, false, false, false);

        Me.room.stage = selectedStage;

        TF_Manager manager = TF_Manager.Instance;
        if (manager == null) return;


        if (manager.CurrentState == TF_Manager.State.InRoom || (manager.CurrentState == TF_Manager.State.InBuildingLobby && (eTeleportTarget == eTeleportTarget.Won_Lobby_DigitalTraining || eTeleportTarget == eTeleportTarget.Won_Lobby_SmallBusiness || eTeleportTarget == eTeleportTarget.Won_Lobby_StaffCounse))) // world -> space
        {
            manager.EnterBuildingLobby(eTeleportTarget.ToString(), () =>
            {
                int index = UnityEngine.Random.Range(0, selectedStage.spawnPos.Length);

                if (isPlanetToBuilding)
                {
                    if (selectedStage != null)
                    {

                        Me.SpawnPos = selectedStage.spawnPos[index];
                        Me.SpawnRot = selectedStage.spawnRot[index];
                    }
                    else
                    {
                        Me.SpawnPos = Vector3.zero;
                        Me.SpawnRot = Quaternion.identity;
                    }
                }
                else
                {
                    Me.SpawnPos = selectedStage.spawnPos[index];
                    Me.SpawnRot = selectedStage.spawnRot[index];
                }

            });
        }
        else // space -> world
        {
            manager.EnterRoom(false, () =>
            {
                int index = UnityEngine.Random.Range(0, selectedStage.spawnPos.Length);

                Me.SpawnPos = selectedStage.spawnPos[index];
                Me.SpawnRot = selectedStage.spawnRot[index];
            });
        }
        */
        #endregion
    }

    private void LeaveBuilding(eTeleportTarget telepoartTarget)
    {
        Stage selectedStage = GameSettings.FindPlanets(telepoartTarget.ToString());

        if (GameSettings.CheckNotUseStage(selectedStage.sceneName))
        {
#if MOIM_ENABLE_LOGS
            MoimDebug.Log("This is Not Use Scene");
#endif
            TF_PopupMessage.Show(ScriptLocalization.Need_Upadate_Title, ScriptLocalization.Common_OK, null, ScriptLocalization.Need_Upadate_Content);
            return;
        }

        if (Me.lastRoom == null) return;

        RoomType roomType = Me.lastRoom.roomtype;
        string eid = Me.lastPlanetId;
        string password = string.Empty;
        int linkType = 0;
        int userType = NetEventJoin.stdUserType;
        string observerPassword = string.Empty;
        int channelId = Me.room.channelId;

        if (TF_Network.isConnected && Me.room != null)
        {
            Me.moimType = MoimType.Planet;
            Me.room.LeaveRoom(() =>
            {
                if (TF_Manager.Instance != null)
                {
                    TF_Manager.Instance.ExitRoom(null, false, false, false);
                    TF_Manager.Instance.OnJoinPlanetEvent(roomType, eid, password, linkType, userType, observerPassword, false, channelId, () =>
                    {
                        int index = UnityEngine.Random.Range(0, selectedStage.spawnPos.Length);
                        Me.SpawnPos = selectedStage.spawnPos[index];
                        Me.SpawnRot = selectedStage.spawnRot[index];
                    });
                }
            }, MoimType.Building);
        }
    }

    private void ReturnLobby(eTeleportTarget eTeleportTarget, bool creating = false)
    {
        if (eTeleportTarget == eTeleportTarget.None) return;

#if MOIM_ENABLE_LOGS
        MoimDebug.Log("다음으로 텔레포트 : " + eTeleportTarget.ToString());
#endif

        Stage selectedStage = GameSettings.FindBuildingLobby((int)eTeleportTarget);

        if (GameSettings.CheckNotUseStage(selectedStage.sceneName))
        {
#if MOIM_ENABLE_LOGS
            MoimDebug.Log("This is Not Use Scene");
#endif
            TF_PopupMessage.Show(ScriptLocalization.Need_Upadate_Title, ScriptLocalization.Common_OK, null, ScriptLocalization.Need_Upadate_Content);
            return;
        }

        Me.moimType = MoimType.Building;


        Me.room.stage = selectedStage;

        TF_Manager manager = TF_Manager.Instance;
        if (manager == null) return;


        if (manager.CurrentState == TF_Manager.State.InRoom || Me.moimType == MoimType.Event && (eTeleportTarget == eTeleportTarget.Won_Lobby_DigitalTraining || eTeleportTarget == eTeleportTarget.Won_Lobby_SmallBusiness || eTeleportTarget == eTeleportTarget.Won_Lobby_StaffCounse)) // world -> space
        {
            manager.EnterBuildingLobby(eTeleportTarget.ToString(), () =>
            {
                int index = UnityEngine.Random.Range(0, selectedStage.spawnPos.Length);
                if (selectedStage != null)
                {

                    Me.SpawnPos = selectedStage.spawnPos[index];
                    Me.SpawnRot = selectedStage.spawnRot[index];
                }
                else
                {
                    Me.SpawnPos = Vector3.zero;
                    Me.SpawnRot = Quaternion.identity;
                }
            });
        }
        else // space -> world
        {
            manager.EnterRoom(false, () =>
            {
                int index = UnityEngine.Random.Range(0, selectedStage.spawnPos.Length);

                Me.SpawnPos = selectedStage.spawnPos[index];
                Me.SpawnRot = selectedStage.spawnRot[index];
            });
        }

    }


    //온라인 
    public void MoveEvent(XRWebAPI.AckSelectEventInfo.Response eventInfo)
    {
#if MOIM_ENABLE_LOGS
        MoimDebug.Log("Event로 이동");
#endif
        Stage selectedStage = GameSettings.FindStage(eventInfo.stage.id);
        if (GameSettings.CheckNotUseStage(selectedStage.sceneName))
        {
#if MOIM_ENABLE_LOGS
            MoimDebug.Log("This is Not Use Scene");
#endif
            TF_PopupMessage.Show(ScriptLocalization.Need_Upadate_Title, ScriptLocalization.Common_OK, null, ScriptLocalization.Need_Upadate_Content);
            return;
        }

#if MOIM_ENABLE_LOGS
        MoimDebug.Log("선택된 Stage ID : " + selectedStage.id);
#endif 

        if (Me.moimType == MoimType.Space) // space -> Event
        {
#if MOIM_ENABLE_LOGS
            MoimDebug.Log("Me.moimType = " + Me.moimType);
#endif

            string password = string.Empty;
            int linkType = 0;
            int userType = NetEventJoin.stdUserType;
            string observerPassword = string.Empty;
            int channelId = Me.room.channelId;

            Me.moimType = MoimType.Event;

            Me.room.LeaveRoom(() =>
            {
                if (TF_Manager.Instance != null)
                {
                    TF_Manager.Instance.ExitRoom(null, false, false, false);
                    TF_Manager.Instance.OnJoinPlanetEvent((RoomType)eventInfo.type, eventInfo.eid, password, linkType, userType, observerPassword, false, channelId);
                }
            });
        }
    }

    //dummy
    public void MoveEvent()
    {
#if MOIM_ENABLE_LOGS
        MoimDebug.Log("Event로 이동");
#endif
        Stage selectedStage = GameSettings.FindStage(eTeleportTarget.ToString());
        if (GameSettings.CheckNotUseStage(selectedStage.sceneName))
        {
#if MOIM_ENABLE_LOGS
            MoimDebug.Log("This is Not Use Scene");
#endif
            TF_PopupMessage.Show(ScriptLocalization.Need_Upadate_Title, ScriptLocalization.Common_OK, null, ScriptLocalization.Need_Upadate_Content);
            return;
        }

#if MOIM_ENABLE_LOGS
        MoimDebug.Log("선택된 Stage ID : " + selectedStage.id);
#endif 

        if (Me.moimType == MoimType.Building) // building -> Event
        {
#if MOIM_ENABLE_LOGS
            MoimDebug.Log("Me.moimType = " + Me.moimType);
#endif
            Me.room.stage = selectedStage;
            Me.moimType = MoimType.Event;

            TF_Manager.Instance.EnterBuildingLobby(selectedStage.sceneName);
        }
    }


    public void OnButtonWarp()
    {
        if (m_SelectedTeleportData == null)
        {
            return;
        }

        //월드 이동
        if (m_SelectedTeleportData.teleportType != eTeleportTypes.World) return;

        characterLocomotion = null;

        if (m_SelectedCollider == null)
        {
            TF_CharacterMove tF_CharacterMove = TF_CharacterMove.Instance;
            if (tF_CharacterMove != null)
            {
#if MOIM_ENABLE_LOGS
                MoimDebug.Log("이건 뭐다?! : " + tF_CharacterMove.gameObject.name);
#endif
                characterLocomotion = tF_CharacterMove.gameObject.GetComponent<UltimateCharacterLocomotion>();
            }
        }
        else
            characterLocomotion = m_SelectedCollider.GetComponentInParent<UltimateCharacterLocomotion>();

        if (characterLocomotion != null)
        {
            //목표에 있는 포탈의 캐릭터 무시 활성화
            //destination에 있는 포탈을 찾아야됨 
            Transform destination = transform.Find(m_DestinationName);
            if (destination == null) return;

            var destinationTeleporter = destination.GetComponent<TeleportTrigger>();
            if (destinationTeleporter != null)
            {
                destinationTeleporter.IgnoreCharacterEnter = true;
            }

            //if(m_SelectedCollider != null)
            //    characterLocomotion.SetPositionAndRotation(m_DestinationPosition, Quaternion.Euler(m_DestinationRotation), m_SnapAnimator, false);
            //else
            characterLocomotion.SetPositionAndRotation(m_DestinationTransform.position, m_DestinationTransform.rotation, m_SnapAnimator, false);

            //TF_CharacterMove Send
            TF_CharacterMove characterMove = new TF_CharacterMove();
            if (m_SelectedCollider != null)
                characterMove = m_SelectedCollider.gameObject.GetComponent<TF_CharacterMove>();
            else
                characterMove = characterLocomotion.gameObject.GetComponent<TF_CharacterMove>();

            if (characterMove != null)
                characterMove.ForceSync("warp");
        }
    }

    public void OnButtonWarp(eTeleportTypes teleportType, eTeleportTarget telepoartTarget, Collider other)
    {
        //월드 이동
        if (teleportType != eTeleportTypes.World) return;
        if (telepoartTarget == eTeleportTarget.None) return;

        Transform destination;

        UltimateCharacterLocomotion characterLocomotion = other.GetComponentInParent<UltimateCharacterLocomotion>();

        if ((characterLocomotion != null))
        {
            if (telepoartTarget == eTeleportTarget.GwangTong_3Way)
            {
                int randomIdx = UnityEngine.Random.Range(0, GwangTong3WayList.Count);

                destination = GwangTong3WayList[randomIdx];
            }
            else if (telepoartTarget == eTeleportTarget.GwangTong_2Way)
            {
                int randomIdx = UnityEngine.Random.Range(0, GwangTong2WayList.Count);

                destination = GwangTong2WayList[randomIdx];
            }
            else
                return;

            //목표에 있는 포탈의 캐릭터 무시 활성화
            //destination에 있는 포탈을 찾아야됨 

            if (destination == null) return;

            var destinationTeleporter = destination.GetComponent<TeleportTrigger>();
            if (destinationTeleporter != null)
            {
                destinationTeleporter.IgnoreCharacterEnter = true;
            }

            characterLocomotion.SetPositionAndRotation(destination.position, destination.rotation, m_SnapAnimator, false);

            //TF_CharacterMove Send
            TF_CharacterMove characterMove = characterLocomotion.gameObject.GetComponent<TF_CharacterMove>();

            if (characterMove != null)
                characterMove.ForceSync("warp");
        }
    }

    private void GetBuildingLobbyData()
    {
        string planetID = Me.room.roomid;
        Me.lastPlanetId = Me.room.roomid;

#if MOIM_ENABLE_LOGS
        MoimDebug.Log("Planet ID : " + planetID);
#endif
        buildingLobbyIdDic.Clear();

        XRWebAPI.GetAllList(planetID, pullCount, nextKey, MoimType.Building, (error, data) =>
        {
#if MOIM_ENABLE_LOGS
            MoimDebug.Log("Get AllList error code : " + error.code);
#endif
            if (error.code == 0)
            {
                if (data.response.results != null)
                {
#if MOIM_ENABLE_LOGS
                    MoimDebug.Log("Get AllList Data Length : " + data.response.results.Length);
#endif

                    for (int i = 0; i < data.response.results.Length; i++)
                    {
#if MOIM_ENABLE_LOGS
                        MoimDebug.Log($"Add Dic Data : {data.response.results[i].permenantId} / {data.response.results[i].id}");
#endif
                        BuildingLobbyId buildingLobbyId = new BuildingLobbyId(data.response.results[i].pid, data.response.results[i].id, data.response.results[i].type );

                        if (!buildingLobbyIdDic.ContainsKey(data.response.results[i].permenantId))
                            buildingLobbyIdDic.Add(data.response.results[i].permenantId, buildingLobbyId);
                    }

                    if (data.response.hasNext && !string.IsNullOrEmpty(data.response.next))
                    {
                        nextKey = data.response.next;
                    }
                    else
                    {
                        nextKey = string.Empty;
                    }
                }
                else
                {
                    MoimDebug.Log($"<color=Yellow>오픈 이벤트 없음</color>");
                }
            }
            else
            {
                ShowErrorPopup(error.code);
            }
        });
    }

    public void ShowErrorPopup(int error)
    {
        string ok = ScriptLocalization.Common_OK;
        switch (error)
        {
            case 1:
                TF_PopupMessage.Show(ScriptLocalization.EventJoin_Error_AlreadyEvent01, ok, null);
                break;
            case 103:
                TF_PopupMessage.Show(ScriptLocalization.Error_Value, ok, null);
                break;
            case 105:
                TF_PopupMessage.Show(ScriptLocalization.EventJoin_Error_ProgramError, ok, null);
                break;
            case 106:
                TF_PopupMessage.Show(ScriptLocalization.EventJoin_Error_NotUserInfo, ok, null);
                break;
            case 115:
                TF_PopupMessage.Show(ScriptLocalization.Common_NotPermission, ok, null);
                break;
            case 119:
                TF_PopupMessage.Show(ScriptLocalization.EventJoin_Error_NotEventInfo119, ok, null);
                break;
            case 120:
                TF_PopupMessage.Show(ScriptLocalization.SecretRoom_Inconsistency, ok, null);
                break;
            case 1003:
                TF_PopupMessage.Show(ScriptLocalization.EventJoin_Error_ServerError, ok, null);
                break;
            case 1007:
                TF_PopupMessage.Show(ScriptLocalization.EventJoin_Error_AlreadyEvent1007, ok, null);
                break;
            case 1010:
                TF_PopupMessage.Show(ScriptLocalization.EventJoin_Error_AlreadyCreatEvent, ok, null);
                break;
            case 1011:
                TF_PopupMessage.Show(ScriptLocalization.EventJoin_Error_ErrorEventSetting, ok, null);
                break;
            case 1013:
                TF_PopupMessage.Show(ScriptLocalization.EventJoin_Error_ExpireEvent, ok, null);
                break;
            case 3001:
                TF_PopupMessage.Show(ScriptLocalization.EventJoin_Error_AlreadyEvent3001, ok, null);
                break;
            case 3002:
                TF_PopupMessage.Show(ScriptLocalization.EventJoin_Error_NotEventInfo3002, ok, null);
                break;
            case 3003:
                TF_PopupMessage.Show(ScriptLocalization.EventJoin_Error_ErrorCreateEvent, ok, null);
                break;
            case 3004:
                TF_PopupMessage.Show(ScriptLocalization.EventJoin_Error_PullPosition3004, ok, null);
                break;
            case 3005:
                TF_PopupMessage.Show(ScriptLocalization.EventJoin_Error_NotAuthority3005, ok, null);
                break;
            case 3011:
                TF_PopupMessage.Show(ScriptLocalization.SecretRoom_PleasePASS, ok, null);
                break;
            case 3012:
                TF_PopupMessage.Show(ScriptLocalization.SecretRoom_Inconsistency, ok, null);
                break;
            case 3013:
                TF_PopupMessage.Show(ScriptLocalization.SecretRoom_KickOff, ok, null);
                break;
            case 3014:
                TF_PopupMessage.Show(ScriptLocalization.EventJoin_Error_NotStart3014, ok, null);
                break;
            case 3016:
                TF_PopupMessage.Show(ScriptLocalization.EventJoin_Error_NotStart3016, ok, null);
                break;
            case 10002:
                TF_PopupMessage.Show(ScriptLocalization.EventJoin_Error_DBUpdateError10002, ok, null);
                break;
            case 10004:
                TF_PopupMessage.Show(ScriptLocalization.EventJoin_Error_NotExistEventInServer10004, ok, null);
                break;
            default:
                TF_PopupMessage.Show("Error Occurred", ok, null, $"error : {error}");
                break;
        }
    }

    internal void CloseEventInfoPopup()
    {
        if (canvas_Teleport == null)
            GetCanvas();

        if (canvas_Teleport != null)
            canvas_Teleport.CloseEventInfoPopup();
    }

    private void GetCanvas()
    {
        if (canvas_Teleport == null)
        {
            TF_RoomUserMenu roomUserMenu = TF_RoomUserMenu.Instance;
            if (roomUserMenu != null)
            {
                canvas_Teleport = roomUserMenu.canvas_Teleport;
                if (canvas_Teleport == null)
                {
                    return;
                }
            }
        }
    }
}






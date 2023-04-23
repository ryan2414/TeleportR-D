using Mopsicus.InfiniteScroll;
using System;
using System.Collections.Generic;
using System.Linq;
using TFVR.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Canvas_Teleport : MonoBehaviour, IPopupOption, IPointerClickHandler
{
    [SerializeField] private GameObject scrollItemPrefab;
    [SerializeField] private Transform contentsPosition;
    [SerializeField] private Button okBtn;

    [SerializeField] private GameObject page1;
    [SerializeField] private GameObject page2;

    [Header("Page 2")]
    [SerializeField] private InfiniteScroll infiniteScroll;
    [SerializeField] private TextMeshProUGUI spaceTitle;
    [SerializeField] private TextMeshProUGUI selectedDate;
    [SerializeField] private Button nextDateBtn;
    [SerializeField] private Button prevDateBtn;
    [SerializeField] private Button reservationBtn;

    [SerializeField] GameObject panelEventInfoPrefab_Mobile;
    [SerializeField] GameObject panelEventInfoPrefab_PC;
    GameObject panelEventInfo;

    private List<GameObject> m_LobbyList = new List<GameObject>();
    TF_Manager manager;

    List<float> heights = new List<float>();
    List<DummyEventData> dummyDatas = new List<DummyEventData>();

    TeleportController teleportController;
    TeleportData selectedTeleportData;

    eTeleportTarget tmpTeleportTarget;

    public int pullCount = 10;
    private string nextKey = string.Empty;


    private void Awake()
    {
        TeleportController.Instance.canvas_Teleport = this;
    }

    private void OnEnable()
    {
        TF_Screen screen = TF_Screen.Instance;
        if (screen != null)
            TF_Screen.popupItemStack.Push(this);

        if(page1 != null)
        {
            page1.SetActive(true);
            ShowPage2(false);
        }

        if (panelEventInfo != null)
            panelEventInfo.SetActive(false);

        selectedTeleportData = null;
        if(okBtn != null) okBtn.interactable = false;

        if(TF_CameraRig.Instance != null)
            TF_CameraRig.Instance.SetCameraControl(false);
    }

    private void OnDisable()
    {
        if(TF_CameraRig.Instance != null)
            TF_CameraRig.Instance.SetCameraControl(true);
    }

    public void OnButtonCancelBtn()
    {
        ChannelPopupActive(false);
    }

    public void OnButtonOKBtn()
    {
        if(Me.moimType == MoimType.Planet)
        {
            TeleportController teleportController = TeleportController.Instance;
            if(teleportController != null)
                teleportController.OnButtonWarp();
            ChannelPopupActive(false);
        }
        else if (Me.moimType == MoimType.Building)
        {
            if(page1 != null)
            {
                page1.SetActive(false);
                ShowPage2(true);
            }
        }

    }

    public void CreateItem(TeleportData item)
    {
        GameObject _scrollItem = Instantiate(scrollItemPrefab, contentsPosition);
     
        _scrollItem.GetComponent<MultyChannelObj>().SettingChannel(item, SetSelectedTeleportData);
        m_LobbyList.Add(_scrollItem);
    }

    private void SetSelectedTeleportData(TeleportData teleportData)
    {
        //teleportData가 null 이면 버튼 비활성화 
        if(teleportData != null)
        {
            if (Me.moimType == MoimType.Planet)
            {
                if (teleportController == null)
                    teleportController = TeleportController.Instance;

                if(teleportController != null)
                    teleportController.SetTeleportData(teleportData);
            }
            else if (Me.moimType == MoimType.Building)//Set Page 2 Data
                    selectedTeleportData = teleportData;

        }

        if (okBtn != null)
        {
            if (teleportData == null)
                okBtn.interactable = false;
            else
                okBtn.interactable = true;
        }
    }

    private void ChannelPopupActive(bool isShow)
    {
        foreach (var item in m_LobbyList)
        {
            Destroy(item);
        }

        gameObject.SetActive(isShow);
    }

    public void CloseEventInfoPopup()
    {
        ShowPage2(true);
    }

    private void ShowPage2(bool isPage2)
    {
        if (page2 == null || selectedTeleportData == null) return;

        page2.SetActive(isPage2);

        if(isPage2 == true)
        {
            infiniteScroll.OnFill += OnFill;
            infiniteScroll.OnHeight += OnHeight;

            //선택한 room 이름
            if(!string.IsNullOrEmpty( selectedTeleportData.title))//api 연결
                spaceTitle.text = selectedTeleportData.title;
            else
            {
                spaceTitle.text = selectedTeleportData.localizedTargetName;
                tmpTeleportTarget = selectedTeleportData.telepoartTarget;
            }
            
            dateTimeNow = DateTime.Now;
            selectedDate.text = dateTimeNow.ToString("yyyy.MM.dd");

            nextDateBtn.onClick.AddListener(OnButtonNextDate);
            prevDateBtn.onClick.AddListener(OnButtonPrevDate);

            GetEventInfo();
        }
        else
        {
            infiniteScroll.OnFill -= OnFill;
            infiniteScroll.OnHeight -= OnHeight;

            nextDateBtn.onClick.RemoveListener(OnButtonNextDate);
            prevDateBtn.onClick.RemoveListener(OnButtonPrevDate);
        }
    }

    
    DateTime dateTimeNow;
    private void OnButtonPrevDate()
    {
        MoimDebug.Log("이전 날짜");
        dateTimeNow = dateTimeNow.AddDays(-1);
        selectedDate.text = dateTimeNow.ToString("yyyy.MM.dd");
        //Todo: 해당 날짜의 List 불러오기
        GetEventInfo();
    }

    private void OnButtonNextDate()
    {
        MoimDebug.Log("다음 날짜");
        dateTimeNow = dateTimeNow.AddDays(1);
        selectedDate.text = dateTimeNow.ToString("yyyy.MM.dd");
        //Todo: 해당 날짜의 List 불러오기
        GetEventInfo();
    }

    private float OnHeight(int index)
    {
        if (index >= heights.Count || index < 0)
            return 0f;
        return heights[index];
    }

    private void OnFill(int index, GameObject item)
    {
        if (string.IsNullOrEmpty(selectedTeleportData.id))
        {
            if (index >= dummyDatas.Count || index < 0)
                return;
            DummyEventData dummyEventData = dummyDatas[index];

            SpaceInfoItem spaceInfoItem = item.GetComponent<SpaceInfoItem>();
            spaceInfoItem.SetData(dummyEventData.title, dummyEventData.isPrivate, dummyEventData.startTime, dummyEventData.endTime, dummyEventData.teleportTarget, ShowEventInfo);
        }
        else
        {
            if (index >= roomInfos.Count || index < 0) return;

            XRWebAPI.RoomInfo roomInfo = roomInfos[index];

            SpaceInfoItem spaceInfoItem = item.GetComponent<SpaceInfoItem>();
            spaceInfoItem.SetData(roomInfo, ShowEventInfo);

        }


    }

    private void ShowEventInfo(XRWebAPI.RoomInfo roomInfo)
    {
#if MOIM_ENABLE_LOGS
        MoimDebug.Log("Show Event Info");
#endif
        page2.SetActive(false);

        if (panelEventInfo == null)
        {
            if (GameSettings.targetDevice == TargetDevice.Mobile)
                panelEventInfo = Instantiate(panelEventInfoPrefab_Mobile, this.transform);
            else if (GameSettings.targetDevice == TargetDevice.Desktop)
                panelEventInfo = Instantiate(panelEventInfoPrefab_PC, this.transform);

            //UI 사이즈 조절
            RectTransform rectTransform = panelEventInfo.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(888 - Screen.width, 1000 - Screen.height);
        }

        if (teleportController == null)
            teleportController = TeleportController.Instance;
        if(teleportController != null)
            teleportController.selectedRoomInfo = roomInfo;

        Me.moimType = MoimType.Space;

        TF_EventInfo eventInfo = panelEventInfo.GetComponent<TF_EventInfo>();
        bool isOwners = roomInfo.owner.uid == Me.uid ? true : false;
        int userType = NetEventJoin.stdUserType;

        eventInfo.Open(roomInfo.eid, isOwners, false, userType);

        panelEventInfo.SetActive(true);
    }

    private void ShowEventInfo(string title, bool arg2, eTeleportTarget teleportTarget)
    {
        if(panelEventInfo == null)
        {
            if (GameSettings.targetDevice == TargetDevice.Mobile)
                panelEventInfo = Instantiate(panelEventInfoPrefab_Mobile, this.transform);
            else if (GameSettings.targetDevice == TargetDevice.Desktop)
                panelEventInfo = Instantiate(panelEventInfoPrefab_PC, this.transform);

            if (teleportController == null)
                teleportController = TeleportController.Instance;

            if(teleportController != null)
                teleportController.eTeleportTarget = teleportTarget;
        }

        TF_EventInfo eventInfo = panelEventInfo.GetComponent<TF_EventInfo>();
        eventInfo.SettingInfo(title);

        panelEventInfo.SetActive(true);
    }

    private List< XRWebAPI.RoomInfo> roomInfos = new List<XRWebAPI.RoomInfo>();

    //이벤트 정보 가져오기
    private void GetEventInfo()
    {
        heights.Clear();
        roomInfos.Clear();

        if (string.IsNullOrEmpty(selectedTeleportData.id))
        {
            dummyDatas.Clear();

            //EventInfo 가져오기
            for (int i = 0; i < 3; i++)
            {
                int rnd = UnityEngine.Random.Range(0, 2);
                bool isPrivat = rnd == 0 ? true : false;

                DummyEventData dummyEventData = new DummyEventData($"세션 {i}", isPrivat, "09:00", "11:00", tmpTeleportTarget);
                dummyDatas.Add(dummyEventData);


                heights.Add(infiniteScroll.Prefab.GetComponent<RectTransform>().rect.height);
            }

            infiniteScroll.InitData(dummyDatas.Count);//아이템 리스트 초기화
        }//Dummy Data
        else
        {
            XRWebAPI.GetAllList(selectedTeleportData.id, pullCount, nextKey, MoimType.Event, (error, data) =>
            {
#if MOIM_ENABLE_LOGS
                MoimDebug.Log("Get All List Error Code : " + error.code);
#endif
                if (error.code == 0)
                {
#if MOIM_ENABLE_LOGS
                    MoimDebug.Log("Get All List Data Length : " + data.response.results.Length);
#endif
                    roomInfos = data.response.results.ToList();

                    for (int i = 0; i < data.response.results.Length; i++)
                    {
                        heights.Add(infiniteScroll.Prefab.GetComponent<RectTransform>().rect.height);
                    }

                    infiniteScroll.InitData(roomInfos.Count);//아이템 리스트 초기화

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
                    if (teleportController == null)
                        teleportController = TeleportController.Instance;

                    if (teleportController != null)
                        teleportController.ShowErrorPopup(error.code);
                }
            });
        }


    }

    public void ClosePopup()
    {
        OnButtonCancelBtn();
        if(Me.moimType == MoimType.Space) 
            Me.moimType = MoimType.Building;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.selectedObject == null)
            SetSelectedTeleportData(null);
    }
}

public class DummyEventData
{
    public string title;
    public bool isPrivate;
    public string startTime;
    public string endTime;
    public eTeleportTarget teleportTarget;

    public DummyEventData(string _title, bool _isPrivate, string _startTime, string _endTime, eTeleportTarget eTeleportTarget)
    {
        title = _title;
        isPrivate = _isPrivate;
        startTime = _startTime;
        endTime = _endTime;
        teleportTarget = eTeleportTarget;
    }
}
using I2.Loc;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpaceInfoItem : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI eventTitleText;
    [SerializeField] TextMeshProUGUI privateText;
    [SerializeField] TextMeshProUGUI timeTableText;
    [SerializeField] Button viewInfoBtn;

    public static Action<string, bool, eTeleportTarget> onClick;
    public static Action<XRWebAPI.RoomInfo> onClickBtn;
    XRWebAPI.RoomInfo roomInfo;
    string eventTitle;
    bool isPublic;
    eTeleportTarget teleportTarget;

    private void OnEnable()
    {
        viewInfoBtn.onClick.AddListener(OnClick);
    }

    private void OnDisable()
    {
        viewInfoBtn?.onClick.RemoveListener(OnClick);
    }

    public void SetData(XRWebAPI.RoomInfo roomInfo, Action<XRWebAPI.RoomInfo> callback)
    {
        this.roomInfo = roomInfo;

        eventTitleText.text =  roomInfo.title;
        privateText.text = roomInfo.isPublic != true ? ScriptLocalization.Session_Private : ScriptLocalization.Session_Public;
        //Todo : serverTime 변환 필요
       
        string startTime = ConvertTime(roomInfo.startTime);
        string endTime = ConvertTime(roomInfo.endTime); 

        string time = string.Format("{0} ~ {1}", startTime, endTime);
        MoimDebug.Log(time);
        timeTableText.text = time;

        onClickBtn = callback;
    }

    public void SetData(string _eventTitle, bool _isPublic, string startTime, string endTime, eTeleportTarget teleportTarget, Action<string, bool, eTeleportTarget> callback)
    {
        this.teleportTarget = teleportTarget;

        eventTitleText.text = eventTitle = _eventTitle;
        isPublic = _isPublic;
        privateText.text = _isPublic != true ? ScriptLocalization.Session_Private : ScriptLocalization.Session_Public;

        string time = string.Format("{0} ~ {1}", startTime, endTime);

        timeTableText.text = time;

        onClick = callback;
    }

    private void OnClick()
    {
        if (roomInfo != null)
            onClickBtn?.Invoke(roomInfo);
        else
            onClick?.Invoke(eventTitle, isPublic, teleportTarget);
    }

    private string ConvertTime(long Time)
    {
        DateTime dateTime = ServerTime.ToDateTime(roomInfo.startTime);
        string time = $"{dateTime.Hour} : {dateTime.Minute}";

        return time;
    }
}

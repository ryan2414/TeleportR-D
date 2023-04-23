using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 텔레포트시 사용할 데이터
/// </summary>
public class TeleportData
{
    public Transform potalTransform;
    public string targetName;
    public eTeleportTypes teleportType;
    public eTeleportTarget telepoartTarget;
    public string localizedTargetName;//로컬라이징 된 타겟 이름

    public string id;
    public string title;

    public TeleportData() { }

    public TeleportData(string id, string title)
    {
        this.id = id;
        this.title = title;
    }

}

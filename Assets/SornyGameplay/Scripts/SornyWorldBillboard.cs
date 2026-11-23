using UnityEngine;
namespace DungeonDLC.Sorny
{
 public sealed class SornyWorldBillboard:MonoBehaviour
 {void LateUpdate(){var cam=Camera.main;if(!cam)return;var d=transform.position-cam.transform.position;d.y=0;if(d.sqrMagnitude>.01f)transform.rotation=Quaternion.LookRotation(d);}}
}

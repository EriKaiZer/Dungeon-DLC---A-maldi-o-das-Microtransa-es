using UnityEngine;
namespace DungeonDLC.MegaPack
{
    // Visual only: one static sprite. No combat or enemy AI.
    public sealed class DDLCBillboard : MonoBehaviour
    {
        public Camera targetCamera;
        void LateUpdate()
        {
            var cam = targetCamera ? targetCamera : Camera.main;
            if (!cam) return;
            var direction = transform.position - cam.transform.position;
            direction.y = 0;
            if (direction.sqrMagnitude > .001f)
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }
}

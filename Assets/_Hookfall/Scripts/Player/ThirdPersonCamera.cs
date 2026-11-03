// Hookfall - mouse-orbit follow camera whose field of view widens with speed (GDD 4.1).
// Placeholder until Cinemachine is set up in Week 9.
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton.
// Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hookfall.Player
{
    [RequireComponent(typeof(Camera))]
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Rigidbody targetBody;

        [Header("Orbit")]
        [SerializeField] private float distance = 8f;
        [SerializeField] private float height = 2f;
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private float minPitch = -40f;
        [SerializeField] private float maxPitch = 75f;

        [Header("Speed feel")]
        [SerializeField] private float baseFov = 70f;
        [SerializeField] private float maxFov = 90f;
        [SerializeField] private float speedForMaxFov = 35f;

        private Camera cam;
        private float yaw;
        private float pitch = 15f;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            cam.fieldOfView = baseFov;
            if (target != null) yaw = target.eulerAngles.y;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Mouse mouse = Mouse.current;
            if (mouse != null && Time.timeScale > 0f)
            {
                Vector2 delta = mouse.delta.ReadValue();
                yaw += delta.x * mouseSensitivity;
                pitch = Mathf.Clamp(pitch - delta.y * mouseSensitivity, minPitch, maxPitch);
            }

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 focus = target.position + Vector3.up * height;
            transform.SetPositionAndRotation(focus - rotation * Vector3.forward * distance, rotation);

            if (targetBody != null)
            {
                float t = Mathf.Clamp01(targetBody.linearVelocity.magnitude / speedForMaxFov);
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, Mathf.Lerp(baseFov, maxFov, t), Time.unscaledDeltaTime * 5f);
            }
        }
    }
}

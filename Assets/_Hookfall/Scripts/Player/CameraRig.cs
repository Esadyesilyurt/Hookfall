// Hookfall - drives the Cinemachine orbit camera from the mouse and widens the field of view with speed (GDD 4.1).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07 (FOV kick added the same day for game feel). Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Core;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hookfall.Player
{
    [RequireComponent(typeof(CinemachineCamera), typeof(CinemachineOrbitalFollow))]
    public class CameraRig : MonoBehaviour
    {
        [SerializeField] private Rigidbody targetBody;

        [Header("Mouse")]
        [Tooltip("Degrees per mouse count at sensitivity 1 (the Settings slider scales it).")]
        [SerializeField] private float degreesPerCount = 0.12f;

        [Header("Speed feel")]
        [SerializeField] private float baseFov = 70f;
        [SerializeField] private float maxFov = 90f;
        [SerializeField] private float speedForMaxFov = 35f;
        [SerializeField] private float fovLerpSpeed = 5f;

        [Tooltip("How fast a FOV kick (Zip, landing) settles back, in degrees per second.")]
        [SerializeField] private float kickRecovery = 40f;

        private CinemachineCamera vcam;
        private float kick;

        /// <summary>A short field-of-view punch for game feel: positive widens (speed), negative narrows (impact).</summary>
        public void Kick(float degrees) => kick = Mathf.Clamp(kick + degrees, -15f, 25f);
        private CinemachineOrbitalFollow orbit;

        private void Awake()
        {
            vcam = GetComponent<CinemachineCamera>();
            orbit = GetComponent<CinemachineOrbitalFollow>();
        }

        private void LateUpdate()
        {
            Mouse mouse = Mouse.current;
            if (mouse != null && Time.timeScale > 0f)
            {
                Vector2 delta = mouse.delta.ReadValue() * (degreesPerCount * GameSettings.MouseSensitivity);
                orbit.HorizontalAxis.Value += delta.x;
                orbit.VerticalAxis.Value = Mathf.Clamp(orbit.VerticalAxis.Value - delta.y,
                    orbit.VerticalAxis.Range.x, orbit.VerticalAxis.Range.y);
            }

            if (targetBody == null) return;
            float t = GameSettings.SpeedEffects ? Mathf.Clamp01(targetBody.linearVelocity.magnitude / speedForMaxFov) : 0f;
            kick = Mathf.MoveTowards(kick, 0f, kickRecovery * Time.unscaledDeltaTime);
            LensSettings lens = vcam.Lens;
            float target = Mathf.Lerp(baseFov, maxFov, t) + (GameSettings.SpeedEffects ? kick : 0f);
            lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, target, Time.unscaledDeltaTime * fovLerpSpeed * 2f);
            vcam.Lens = lens;
        }
    }
}

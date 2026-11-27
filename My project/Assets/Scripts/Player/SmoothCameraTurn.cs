using System;
using System.Collections;
using UnityEngine;

namespace ProjectRac.Player
{
    // Smoothly rotates the player body yaw and camera pitch toward a world target (e.g. ambush/encounter focus).
    public static class SmoothCameraTurn
    {
        public static Coroutine TurnToTarget(
            MonoBehaviour runner,
            Transform player,
            Vector3 worldTargetPos,
            float duration,
            float targetPitchOffset,
            Action onComplete)
        {
            return runner.StartCoroutine(TurnRoutine(player, worldTargetPos, duration, targetPitchOffset, onComplete));
        }

        private static IEnumerator TurnRoutine(
            Transform player,
            Vector3 worldTargetPos,
            float duration,
            float targetPitchOffset,
            Action onComplete)
        {
            if (player == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            var input = player.GetComponent<PlayerInputHandler>();
            var fps = player.GetComponent<FirstPersonController>();
            var camLook = player.GetComponentInChildren<FirstPersonCameraLook>(true);
            var cam = player.GetComponentInChildren<Camera>(true);

            bool hadInput = input != null && input.enabled;
            bool hadFps = fps != null && fps.enabled;
            bool hadLook = camLook != null && camLook.enabled;

            if (input != null) { input.ClearBufferedInputs(); input.enabled = false; }
            if (fps != null) fps.enabled = false;
            if (camLook != null) camLook.enabled = false;

            Quaternion startRot = player.rotation;
            Vector3 toTarget = worldTargetPos - player.position;
            toTarget.y = 0f;
            Quaternion endRot = toTarget.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(toTarget.normalized)
                : startRot;

            float startPitch = 0f;
            if (cam != null)
            {
                startPitch = cam.transform.localEulerAngles.x;
                if (startPitch > 180f) startPitch -= 360f;
            }

            float safeDuration = Mathf.Max(0.05f, duration);
            float elapsed = 0f;
            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(elapsed / safeDuration);
                float eased = u * u * (3f - 2f * u); // SmoothStep

                player.rotation = Quaternion.Slerp(startRot, endRot, eased);
                float pitch = Mathf.Lerp(startPitch, targetPitchOffset, eased);
                if (camLook != null) camLook.SetPitch(pitch);
                else if (cam != null) cam.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

                yield return null;
            }

            player.rotation = endRot;
            if (camLook != null) camLook.SetPitch(targetPitchOffset);
            else if (cam != null) cam.transform.localRotation = Quaternion.Euler(targetPitchOffset, 0f, 0f);

            // Restore only if onComplete does not immediately take over control (e.g. VisualNovelDialogueUI).
            onComplete?.Invoke();

            if (!UI.VisualNovelDialogueUI.IsActive)
            {
                if (hadInput && input != null) { input.ClearBufferedInputs(); input.enabled = true; }
                if (hadFps && fps != null) fps.enabled = true;
                if (hadLook && camLook != null) camLook.enabled = true;
            }
        }
    }
}

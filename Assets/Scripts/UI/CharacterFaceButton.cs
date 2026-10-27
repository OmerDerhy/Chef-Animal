using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MergesChef.UI
{
    /// <summary>
    /// A sprite in a species' idle AnimationClip, at the time it starts showing. Baked once at edit
    /// time from that species' real Face prefab (Assets/Characters/<Species>(0).prefab) so this button
    /// never needs Animator/AnimatorController access at runtime - those are Editor-only APIs.
    /// </summary>
    [System.Serializable]
    public struct FaceKeyframe
    {
        public Sprite sprite;
        public float time;
    }

    [System.Serializable]
    public struct SpeciesFace
    {
        [Tooltip("Matches the leading word of the spawned staff's name, e.g. 'Panda' for 'Panda Staff'.")]
        public string speciesId;
        public FaceKeyframe[] keyframes;
        [Tooltip("The idle clip's loop length - its last keyframe's time.")]
        public float loopDuration;
    }

    /// <summary>
    /// HUD button that shows the idle "Face" animation (Assets/Characters/&lt;Species&gt;(0).prefab,
    /// the Face-script character used elsewhere in the project) for whichever species the player
    /// currently owns as staff - never a hardcoded species. Reproduces that species' actual idle
    /// clip timing (each one holds its own pace, not a flattened uniform swap). The button itself is
    /// inert by design - onClick has no listeners.
    /// </summary>
    public class CharacterFaceButton : MonoBehaviour
    {
        public Image portraitImage;

        [Tooltip("Baked idle-face data per species, populated from each species' own AnimationClip.")]
        public List<SpeciesFace> faces = new List<SpeciesFace>();

        private FaceKeyframe[] activeKeyframes;
        private float activeLoopDuration;

        private void Start()
        {
            if (portraitImage == null) portraitImage = GetComponent<Image>();
            StartCoroutine(WaitForStaffAndBind());
        }

        private IEnumerator WaitForStaffAndBind()
        {
            // StaffManager spawns its roster in its own Start(); Unity doesn't guarantee Start() order
            // across different components, so poll briefly instead of racing it.
            float timeout = 3f;
            while (timeout > 0f)
            {
                var mgr = MergesChef.Characters.StaffManager.Instance;
                if (mgr != null && mgr.SpawnedStaff.Count > 0)
                {
                    BindToSpecies(SpeciesIdFromStaffName(mgr.SpawnedStaff[0].gameObject.name));
                    yield break;
                }
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            Debug.LogWarning("[CharacterFaceButton] No spawned staff found after 3s - face left blank.");
        }

        /// <summary>"Panda Staff(Clone)" -> "Panda". Staff prefabs are always named "&lt;Species&gt; Staff".</summary>
        private string SpeciesIdFromStaffName(string staffGameObjectName)
        {
            string cleaned = staffGameObjectName.Replace("(Clone)", "").Trim();
            int staffIndex = cleaned.IndexOf(" Staff");
            return staffIndex >= 0 ? cleaned.Substring(0, staffIndex) : cleaned;
        }

        private void BindToSpecies(string speciesId)
        {
            foreach (var face in faces)
            {
                if (face.speciesId != speciesId) continue;

                activeKeyframes = face.keyframes;
                activeLoopDuration = face.loopDuration;
                if (activeKeyframes != null && activeKeyframes.Length > 0 && portraitImage != null)
                {
                    portraitImage.sprite = activeKeyframes[0].sprite;
                }
                return;
            }

            Debug.LogWarning($"[CharacterFaceButton] No face data baked for species '{speciesId}'.");
        }

        private void Update()
        {
            if (activeKeyframes == null || activeKeyframes.Length == 0 || activeLoopDuration <= 0f || portraitImage == null)
            {
                return;
            }

            float t = Time.unscaledTime % activeLoopDuration;

            Sprite current = activeKeyframes[0].sprite;
            for (int i = 0; i < activeKeyframes.Length; i++)
            {
                if (activeKeyframes[i].time <= t) current = activeKeyframes[i].sprite;
                else break;
            }

            if (portraitImage.sprite != current) portraitImage.sprite = current;
        }
    }
}

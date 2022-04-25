using System.Collections;
using System.Reflection;
using MonoMod.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vasi;
using Logger = Modding.Logger;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Pale_Prince
{
    internal class PrinceFinder : MonoBehaviour
    {
        private static readonly FastReflectionDelegate _updateDelegate =
            typeof(BossStatue)
                .GetMethod("UpdateDetails", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetFastDelegate();

        private void Start()
        {
            USceneManager.activeSceneChanged += SceneChanged;
        }

        private void SceneChanged(Scene arg0, Scene arg1) => StartCoroutine(SceneChangeRoutine(arg0.name, arg1.name));

        private IEnumerator SceneChangeRoutine(string prev, string next)
        {
            if (next == "GG_Workshop") yield return SetStatue();
            if (next != "GG_Hollow_Knight") yield break;
            if (!PalePrince._settings.BossDoor) yield break;

            StartCoroutine(AddComponent());
        }

        private static IEnumerator SetStatue()
        {
            yield return null;

            GameObject statue = GameObject.Find("GG_Statue_HollowKnight");

            var scene = ScriptableObject.CreateInstance<BossScene>();
            scene.sceneName = "GG_Hollow_Knight";

            var bs = statue.GetComponent<BossStatue>();
            bs.dreamBossScene = scene;
            bs.dreamStatueStatePD = "statueStatePure";

            bs.SetPlaquesVisible(bs.StatueState.isUnlocked && bs.StatueState.hasBeenSeen);

            var details = new BossStatue.BossUIDetails();
            details.nameKey = details.nameSheet = "Pale_Name";
            details.descriptionKey = details.descriptionSheet = "Pale_Desc";
            bs.dreamBossDetails = details;

            GameObject @switch = statue.Child("dream_version_switch");
            @switch.SetActive(true);
            @switch.transform.position = new Vector3(185.1f, 36.5f, 0.4f);

            GameObject burst = @switch.Child("lit_pieces/Burst Pt");
            burst.transform.position = new Vector3(183.7f, 36.3f, 0.4f);

            GameObject glow = @switch.Child("lit_pieces/Base Glow");
            glow.transform.position = new Vector3(183.7f, 36.3f, 0.4f);

            glow.GetComponent<tk2dSprite>().color = Color.white;

            var fader = glow.GetComponent<ColorFader>();
            fader.upColour = Color.white;
            fader.downColour = Color.white;

            var toggle = statue.GetComponentInChildren<BossStatueDreamToggle>(true);

            toggle.SetState(true);

            Modding.ReflectionHelper.SetField
            (
                toggle,
                "colorFaders",
                toggle.litPieces.GetComponentsInChildren<ColorFader>(true)
            );

            toggle.SetOwner(bs);

            yield return new WaitWhile(() => bs.bossUIControlFSM == null);

            _updateDelegate(bs);
        }

        private static IEnumerator AddComponent()
        {
            yield return null;

            GameObject.Find("HK Prime").AddComponent<Prince>();
        }

        private void OnDestroy()
        {
            USceneManager.activeSceneChanged -= SceneChanged;
        }

        public static void Log(object o)
        {
            Logger.Log($"[{Assembly.GetExecutingAssembly().GetName().Name}]: " + o);
        }
    }
}
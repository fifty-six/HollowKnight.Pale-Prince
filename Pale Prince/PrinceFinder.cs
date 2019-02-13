using System.Collections;
using System.Reflection;
using HutongGames.PlayMaker.Actions;
using ModCommon;
using ModCommon.Util;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = Modding.Logger;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Pale_Prince
{
    internal class PrinceFinder : MonoBehaviour
    {
        private void Start()
        {
            USceneManager.activeSceneChanged += SceneChanged;
        }

        private void SceneChanged(Scene arg0, Scene arg1)
        {
            if (arg1.name == "GG_Workshop") SetStatue();
            if (arg1.name != "GG_Hollow_Knight") return;
            if (arg0.name != "GG_Workshop") return;

            StartCoroutine(AddComponent());
        }
        
        private static void SetStatue()
        {
            GameObject statue = GameObject.Find("GG_Statue_HollowKnight");
            
            var scene = ScriptableObject.CreateInstance<BossScene>();
            scene.sceneName = "GG_Hollow_Knight";
            
            var bs = statue.GetComponent<BossStatue>();
            bs.dreamBossScene = scene;
            bs.dreamStatueStatePD = "statueStatePure";

            var details = new BossStatue.BossUIDetails();
            details.nameKey = details.nameSheet = "Pale_Name";
            details.descriptionKey = details.descriptionSheet = "Pale_Desc";
            bs.dreamBossDetails = details;
            
            GameObject @switch = statue.FindGameObjectInChildren("dream_version_switch");
            @switch.SetActive(true);
            @switch.transform.position = new Vector3(185.1f, 36.5f, 0.4f);
            
            GameObject burst = @switch.FindGameObjectInChildren("Burst Pt");
            burst.transform.position = new Vector3(183.7f, 36.3f, 0.4f);

            GameObject glow = @switch.FindGameObjectInChildren("Base Glow");
            glow.transform.position = new Vector3(183.7f, 36.3f, 0.4f);
            
            glow.GetComponent<tk2dSprite>().color = Color.white;

            var fader = glow.GetComponent<ColorFader>();
            fader.upColour = Color.black;
            fader.downColour = Color.white;
            
            var toggle = statue.GetComponentInChildren<BossStatueDreamToggle>();
            toggle.SetOwner(bs);
            toggle.SetState(true);
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
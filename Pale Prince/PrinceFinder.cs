using System.Collections;
using System.Reflection;
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
            if (arg1.name != "GG_Hollow_Knight") return;
            if (arg0.name != "GG_Workshop") return;

            StartCoroutine(AddComponent());
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
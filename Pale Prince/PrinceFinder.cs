using System.Reflection;
using UnityEngine;
using Logger = Modding.Logger;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Pale_Prince
{
    internal class PrinceFinder : MonoBehaviour
    {
        private GameObject _prince;

        private void Start()
        {
            Log("Added PrinceFinder Behaviour");
        }

        private void Update()
        {
            if (_prince != null) return;
            _prince = GameObject.Find("HK Prime");
            if (_prince == null) return;
            _prince.AddComponent<Prince>();
        }

        public static void Log(object o)
        {
            Logger.Log($"[{Assembly.GetExecutingAssembly().GetName().Name}]: " + o);
        }
    }
}
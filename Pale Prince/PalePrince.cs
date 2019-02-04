using Modding;
using JetBrains.Annotations;
using UnityEngine.SceneManagement;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using UObject = UnityEngine.Object;

namespace Pale_Prince
{
    [UsedImplicitly]
    public class PalePrince : Mod, ITogglableMod
    {
        private string _lastScene;

        public PalePrince()
        {
            typeof(Mod).GetField("Name").SetValue(this, "Pale Prince");
        }

        public override void Initialize()
        {
            ModHooks.Instance.AfterSavegameLoadHook += SaveGame;
            ModHooks.Instance.NewGameHook += AddComponent;
            ModHooks.Instance.LanguageGetHook += OnLangGet;
            USceneManager.activeSceneChanged += SceneChanged;
        }

        private void SceneChanged(Scene arg0, Scene arg1)
        {
            _lastScene = arg0.name;
        }

        private string OnLangGet(string key, string sheettitle)
        {
            switch (key)
            {
                case "NAME_HK_PRIME":
                case "HK_PRIME_MAIN" when _lastScene == "GG_Workshop":
                    return "Pale Prince";
                case "GG_S_HK":
                    return "Suffer.";
                default:
                    return Language.Language.GetInternal(key, sheettitle);
            }
        }

        private static void SaveGame(SaveGameData data) => AddComponent();

        private static void AddComponent()
        {
            GameManager.instance.gameObject.AddComponent<PrinceFinder>();
        }

        public void Unload()
        {
            ModHooks.Instance.AfterSavegameLoadHook -= SaveGame;
            ModHooks.Instance.NewGameHook -= AddComponent;
            ModHooks.Instance.LanguageGetHook -= OnLangGet;

            var finder = GameManager.instance.gameObject.GetComponent<PrinceFinder>();

            if (finder != null)
                UObject.Destroy(finder);

        }
    }
}
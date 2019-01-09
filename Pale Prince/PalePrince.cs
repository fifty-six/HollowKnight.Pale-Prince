using Modding;
using JetBrains.Annotations;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Pale_Prince
{
    [UsedImplicitly]
    public class PalePrince : Mod, ITogglableMod
    {
        public PalePrince()
        {
            typeof(Mod).GetField("Name").SetValue(this, "Pale Prince");
        }

        public override void Initialize()
        {
            ModHooks.Instance.AfterSavegameLoadHook += SaveGame;
            ModHooks.Instance.NewGameHook += AddComponent;
            ModHooks.Instance.LanguageGetHook += OnLangGet;
        }

        private static string OnLangGet(string key, string sheettitle)
        {
            switch (key)
            {
                case "NAME_HK_PRIME":
                case "HK_PRIME_MAIN":
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
        }
    }
}
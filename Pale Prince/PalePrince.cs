using System;
using System.Diagnostics;
using System.Reflection;
using Modding;
using JetBrains.Annotations;
using ModCommon;
using MonoMod.RuntimeDetour;
using UnityEngine.SceneManagement;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using UObject = UnityEngine.Object;

namespace Pale_Prince
{
    [UsedImplicitly]
    public class PalePrince : Mod<SaveSettings>, ITogglableMod
    {
        [PublicAPI]
        public static PalePrince Instance { get; private set; }
        
        public override string GetVersion()
        {
            return Assembly.GetAssembly(typeof(PalePrince)).GetName().Version.ToString();
        }

        private string _lastScene;

        private Hook _pdGet;
        private Hook _pdSet;

        public PalePrince() 
        {
            typeof(Mod).GetField("Name").SetValue(this, "Pale Prince");
        }

        public override void Initialize()
        {
            Instance = this;
            
            ModHooks.Instance.BeforeSavegameSaveHook += BeforeSaveGameSave;
            ModHooks.Instance.AfterSavegameLoadHook += SaveGame;
            ModHooks.Instance.SavegameSaveHook += SaveGameSave;
            ModHooks.Instance.NewGameHook += AddComponent;
            ModHooks.Instance.LanguageGetHook += OnLangGet;
            USceneManager.activeSceneChanged += SceneChanged;

            _pdGet = new Hook
            (
                typeof(PlayerData).GetMethod("GetVariable")?.MakeGenericMethod(typeof(BossStatue.Completion)),
                typeof(PalePrince).GetMethod(nameof(GetVariableHook)),
                this
            );

            _pdSet = new Hook
            (
                typeof(PlayerData).GetMethod("SetVariable")?.MakeGenericMethod(typeof(BossStatue.Completion)),
                typeof(PalePrince).GetMethod(nameof(SetVariableHook)),
                this
            );
        }

        [UsedImplicitly]
        public void SetVariableHook(Action<PlayerData, string, BossStatue.Completion> orig, PlayerData pd, string key, BossStatue.Completion val)
        {
            switch (key)
            {
                case "statueStatePure":
                    Settings.Completion = val;
                    break;
                default:
                    orig(pd, key, val);
                    break;
            }
        }

        [UsedImplicitly]
        public BossStatue.Completion GetVariableHook(Func<PlayerData, string, BossStatue.Completion> orig, PlayerData pd, string key)
        {
            return key == "statueStatePure" ? Settings.Completion : orig(pd, key);
        }

        private void SceneChanged(Scene arg0, Scene arg1)
        {
            _lastScene = arg0.name;
        }

        private string OnLangGet(string key, string sheettitle)
        {
            switch (key)
            {
                case "Pale_Name":
                case "HK_PRIME_MAIN" when _lastScene == "GG_Workshop" && PlayerData.instance.statueStateHollowKnight.usingAltVersion:
                    return "Pale Prince";
                case "Pale_Desc":
                    return "Suffer.";
                default:
                    return Language.Language.GetInternal(key, sheettitle);
            }
        }

        private void BeforeSaveGameSave(SaveGameData data)
        {
            Settings.AltStatue = PlayerData.instance.statueStateHollowKnight.usingAltVersion;
            
            PlayerData.instance.statueStateHollowKnight.usingAltVersion = false;
        }

        private void SaveGame(SaveGameData data)
        {
            SaveGameSave();
            AddComponent();
        }
        
        private void SaveGameSave(int id = 0)
        {
            PlayerData.instance.statueStateHollowKnight.usingAltVersion = Settings.AltStatue;
        }

        private static void AddComponent()
        {
            GameManager.instance.gameObject.AddComponent<PrinceFinder>();
        }

        public void Unload()
        {
            ModHooks.Instance.BeforeSavegameSaveHook -= BeforeSaveGameSave;
            ModHooks.Instance.AfterSavegameLoadHook -= SaveGame;
            ModHooks.Instance.SavegameSaveHook -= SaveGameSave;
            ModHooks.Instance.NewGameHook -= AddComponent;
            ModHooks.Instance.LanguageGetHook -= OnLangGet;
            USceneManager.activeSceneChanged -= SceneChanged;

            _pdGet?.Dispose();
            _pdSet?.Dispose();

            var finder = GameManager.instance.gameObject.GetComponent<PrinceFinder>();

            if (finder != null)
                UObject.Destroy(finder);
        }
    }
}
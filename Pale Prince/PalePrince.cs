using System;
using System.Reflection;
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
        public override ModSettings SaveSettings
        {
            get => _settings;
            set => _settings = value as SaveSettings ?? _settings;
        }

        private SaveSettings _settings = new SaveSettings();

        [PublicAPI]
        public static PalePrince Instance { get; private set; }
        
        public override string GetVersion()
        {
            return Assembly.GetAssembly(typeof(PalePrince)).GetName().Version.ToString();
        }

        private string _lastScene;

        public PalePrince() : base("Pale Prince") {}

        public override void Initialize()
        {
            Instance = this;
            
            Unload();
            
            ModHooks.Instance.BeforeSavegameSaveHook += BeforeSaveGameSave;
            ModHooks.Instance.AfterSavegameLoadHook += SaveGame;
            ModHooks.Instance.SavegameSaveHook += SaveGameSave;
            ModHooks.Instance.NewGameHook += AddComponent;
            ModHooks.Instance.LanguageGetHook += OnLangGet;
            ModHooks.Instance.SetPlayerVariableHook += SetVariableHook;
            ModHooks.Instance.GetPlayerVariableHook += GetVariableHook;
            USceneManager.activeSceneChanged += SceneChanged;
        }

        private object SetVariableHook(Type t, string key, object obj)
        {
            if (key == "statueStatePure")
                _settings.Completion = (BossStatue.Completion) obj;
            return obj;
        }

        private object GetVariableHook(Type t, string key, object orig)
        {
            return key == "statueStatePure"
                ? _settings.Completion
                : orig;
        }

        private void SceneChanged(Scene arg0, Scene arg1)
        {
            // API Issue.
            if (arg1.name == Constants.MENU_SCENE)
            {
                _settings = new SaveSettings();
            }
            
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
            _settings.AltStatue = PlayerData.instance.statueStateHollowKnight.usingAltVersion;
            
            PlayerData.instance.statueStateHollowKnight.usingAltVersion = false;
        }

        private void SaveGame(SaveGameData data)
        {
            SaveGameSave();
            AddComponent();
        }
        
        private void SaveGameSave(int id = 0)
        {
            PlayerData.instance.statueStateHollowKnight.usingAltVersion = _settings.AltStatue;
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
            ModHooks.Instance.SetPlayerVariableHook -= SetVariableHook;
            USceneManager.activeSceneChanged -= SceneChanged;

            var finder = GameManager.instance.gameObject.GetComponent<PrinceFinder>();

            if (finder != null)
                UObject.Destroy(finder);
        }
    }
}
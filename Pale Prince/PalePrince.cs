using System;
using Modding;
using JetBrains.Annotations;
using UnityEngine.SceneManagement;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using UObject = UnityEngine.Object;
using System.Collections.Generic;
using UnityEngine.UI;

namespace Pale_Prince
{
    [UsedImplicitly]
    public class PalePrince : Mod, ITogglableMod, ILocalSettings<SaveSettings>, IMenuMod
    {
        [PublicAPI]
        public static PalePrince Instance { get; private set; }

        public static SaveSettings Settings { get; private set; }

        public void OnLoadLocal(SaveSettings s) => Settings = s;

        public SaveSettings OnSaveLocal() => Settings;

        public override string GetVersion() => Vasi.VersionUtil.GetVersion<PalePrince>();

        private string _lastScene;

        public PalePrince() : base("Pale Prince") { }

        public override void Initialize()
        {
            Instance = this;

            Unload();

            ModHooks.BeforeSavegameSaveHook += BeforeSaveGameSave;
            ModHooks.AfterSavegameLoadHook += SaveGame;
            ModHooks.SavegameSaveHook += SaveGameSave;
            ModHooks.NewGameHook += AddComponent;
            ModHooks.LanguageGetHook += OnLangGet;
            ModHooks.SetPlayerVariableHook += SetVariableHook;
            ModHooks.GetPlayerVariableHook += GetVariableHook;
            USceneManager.activeSceneChanged += SceneChanged;
        }


        private object SetVariableHook(Type t, string key, object obj)
        {
            if (key != "statueStatePure")
                return obj;

            var completion = (BossStatue.Completion) obj;

            Settings.Completion = completion;

            // Set alt false so if the mod is uninstalled then it doesn't break the mod.
            completion.usingAltVersion = false;

            return completion;
        }

        private object GetVariableHook(Type type, string name, object orig)
        {
            return name == "statueStatePure" ? Settings.Completion : orig;
        }

        private void SceneChanged(Scene arg0, Scene arg1)
        {
            // API Issue.
            if (arg1.name == Constants.MENU_SCENE)
            {
                Settings = new SaveSettings();
            }

            _lastScene = arg0.name;
        }

        private string OnLangGet(string key, string sheettitle, string orig)
        {
            switch (key)
            {
                case "Pale_Name":
                case "HK_PRIME_MAIN" when _lastScene == "GG_Workshop" && PlayerData.instance.statueStateHollowKnight.usingAltVersion:
                    return "Pale Prince";
                case "Pale_Desc":
                    return "Suffer.";
                default:
                    return orig;
            }
        }

        private static void BeforeSaveGameSave(SaveGameData data)
        {
            Settings.AltStatue = PlayerData.instance.statueStateHollowKnight.usingAltVersion;

            PlayerData.instance.statueStateHollowKnight.usingAltVersion = false;
        }

        private void SaveGame(SaveGameData data)
        {
            SaveGameSave();
            AddComponent();
            RefreshMenu();
        }

        private static void SaveGameSave(int id = 0)
        {
            PlayerData.instance.statueStateHollowKnight.usingAltVersion = Settings.AltStatue;
        }

        private static void AddComponent()
        {
            GameManager.instance.gameObject.AddComponent<PrinceFinder>();
        }

        public bool ToggleButtonInsideMenu => true;

        public List<IMenuMod.MenuEntry> GetMenuData(IMenuMod.MenuEntry? menu)
        {
            return new List<IMenuMod.MenuEntry>
            {
                new()
                {
                    Name = "Enter Pantheon",
                    Description = "Choose if Pale Prince is enabled in P4 and P5",
                    Values = new[] { Language.Language.Get("MOH_ON", "MainMenu"), Language.Language.Get("MOH_OFF", "MainMenu") },
                    Saver = ChooseEnter,
                    Loader = () => Settings.BossDoor ? 0 : 1
                }
            };
        }

        private static void ChooseEnter(int i) => Settings.BossDoor = i == 0;

        public void Unload()
        {
            ModHooks.BeforeSavegameSaveHook -= BeforeSaveGameSave;
            ModHooks.AfterSavegameLoadHook -= SaveGame;
            ModHooks.SavegameSaveHook -= SaveGameSave;
            ModHooks.NewGameHook -= AddComponent;
            ModHooks.LanguageGetHook -= OnLangGet;
            ModHooks.SetPlayerVariableHook -= SetVariableHook;
            USceneManager.activeSceneChanged -= SceneChanged;

            var finder = GameManager.instance.gameObject.GetComponent<PrinceFinder>();

            if (finder != null)
                UObject.Destroy(finder);
        }

        private void RefreshMenu()
        {
            MenuScreen menu = ModHooks.BuiltModMenuScreens[this];
            
            foreach (var option in menu.gameObject.GetComponentsInChildren<MenuOptionHorizontal>())
            {
                option.menuSetting.RefreshValueFromGameSettings();
            }
        }
    }
}
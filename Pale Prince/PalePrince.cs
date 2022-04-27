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
    public class PalePrince : Mod, ITogglableMod, ILocalSettings<SaveSettings>,IMenuMod
    {
        [PublicAPI]
        public static PalePrince Instance { get; private set; }

        public static SaveSettings _settings;

        public void OnLoadLocal(SaveSettings s) => _settings = s;
        
        public SaveSettings OnSaveLocal() => _settings;
        
        public override string GetVersion() => Vasi.VersionUtil.GetVersion<PalePrince>();

        private string _lastScene;

        public PalePrince() : base("Pale Prince") {}

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
            
            _settings.Completion = completion;

            // Set alt false so if the mod is uninstalled then it doesn't break the mod.
            completion.usingAltVersion = false;

            return completion;
        }
        
        private object GetVariableHook(Type type, string name, object orig)
        {
            return name == "statueStatePure" ? _settings.Completion : orig;
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

        private void BeforeSaveGameSave(SaveGameData data)
        {
            _settings.AltStatue = PlayerData.instance.statueStateHollowKnight.usingAltVersion;
            
            PlayerData.instance.statueStateHollowKnight.usingAltVersion = false;
        }

        private void SaveGame(SaveGameData data)
        {
            SaveGameSave();
            AddComponent();
            RefreshMenu();
        }
        
        private void SaveGameSave(int id = 0)
        {
            PlayerData.instance.statueStateHollowKnight.usingAltVersion = _settings.AltStatue;
        }

        private static void AddComponent()
        {
            GameManager.instance.gameObject.AddComponent<PrinceFinder>();
        }
        public bool ToggleButtonInsideMenu =>true;
        public List<IMenuMod.MenuEntry> GetMenuData(IMenuMod.MenuEntry? menu)
        {
            List<IMenuMod.MenuEntry> menus = new();
            menus.Add(
                new()
                {
                    Name = "Enter Pantheon",
                    Description = "Choose if Pale Prince enable in p4 and p5(choose Pale Prince Statue in hog)",
                    Values = new string[] { Language.Language.Get("MOH_ON", "MainMenu"), Language.Language.Get("MOH_OFF", "MainMenu") },
                    Saver = i => ChooseEnter(i),
                    Loader = () => _settings.BossDoor ? 0 : 1
                }
                );
            return menus;
        }
        private void ChooseEnter(int i)
        {
            if (i == 0)
            {
                _settings.BossDoor = true;
            }
            else
            {
                _settings.BossDoor = false;
            }
        }
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
        public void RefreshMenu()
        {
            MenuScreen menu = ModHooks.BuiltModMenuScreens[this];
            foreach(var option in menu.gameObject.GetComponentsInChildren<MenuOptionHorizontal>())
            {
                option.menuSetting.RefreshValueFromGameSettings();
            }
        }
    }
}
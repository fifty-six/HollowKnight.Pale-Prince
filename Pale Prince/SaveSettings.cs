using System;
using Modding;
using UnityEngine;

namespace Pale_Prince
{
    [Serializable]
    public class SaveSettings : ModSettings, ISerializationCallbackReceiver
    {
        public BossStatue.Completion Completion = new BossStatue.Completion
        {
            isUnlocked = true
        };

        public bool AltStatue
        {
            get => GetBool();
            set => SetBool(value);
        }
        
        public void OnBeforeSerialize()
        {
            StringValues["Completion"] = JsonUtility.ToJson(Completion);
        }

        public void OnAfterDeserialize()
        {
            StringValues.TryGetValue("Completion", out string @out);

            if (string.IsNullOrEmpty(@out)) return;

            Completion = JsonUtility.FromJson<BossStatue.Completion>(@out);
        }
    }
}
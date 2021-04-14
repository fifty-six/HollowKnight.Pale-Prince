using System;
using Modding;

namespace Pale_Prince
{
    [Serializable]
    public class SaveSettings : ModSettings
    {
        public BossStatue.Completion Completion = new()
        {
            isUnlocked = true
        };

        public bool AltStatue;
    }
}
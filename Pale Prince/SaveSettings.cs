using System;

namespace Pale_Prince
{
    [Serializable]
    public class SaveSettings
    {
        public BossStatue.Completion Completion = new()
        {
            isUnlocked = true
        };

        public bool AltStatue;
        public bool BossDoor = false;
    }
}
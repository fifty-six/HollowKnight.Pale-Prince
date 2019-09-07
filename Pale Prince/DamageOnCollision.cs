using GlobalEnums;
using UnityEngine;

namespace Pale_Prince 
{
    internal class DamageOnCollision : MonoBehaviour
    {
        public int Damage { private get; set; } = 1;

        private void OnParticleCollision(GameObject other)
        {
            if (other != HeroController.instance.gameObject) return;
            HeroController.instance.TakeDamage(gameObject, CollisionSide.other, Damage, 1);
        }
    }
}
using UnityEngine;

namespace Pale_Prince
{
    internal class Replay2dtkAnimation : MonoBehaviour
    {
        public int Frame { private get; set; } = 15;
        
        private tk2dSpriteAnimator _anim;

        private void OnEnable()
        {
            if (_anim == null)
                _anim = GetComponent<tk2dSpriteAnimator>();
            _anim.PlayFromFrame(Frame);
        }

        private void Update()
        {
            if (_anim.Playing)
                return;
            _anim.PlayFromFrame(Frame);
        }
    }
}
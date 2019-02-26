using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ModCommon;
using UnityEngine;
using Random = System.Random;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Pale_Prince
{
    internal class Prince : MonoBehaviour
    {
        private const int HP = 3000;
        private HealthManager _hm;

        private tk2dSpriteAnimator _anim;

        private PlayMakerFSM _control;
        private PlayMakerFSM _stuns;

        private readonly Random _rand = new Random();

        private readonly Dictionary<string, float> _fpsDict = new Dictionary<string, float>
        {
            ["Dash"] = 40,
        };

        private ParticleSystem _trail;

        private GameObject HeavyShot
        {
            get
            {
                if (_heavyShot != null) return _heavyShot;

                _heavyShot = Instantiate(BlackShot);

                _heavyShot.GetComponent<Rigidbody2D>().gravityScale = 1f;

                return _heavyShot;
            }
        }

        private GameObject _heavyShot;

        private GameObject HeavyShotGlow
        {
            get
            {
                if (_heavyShotGlow != null) return _heavyShotGlow;

                _heavyShotGlow = Instantiate(BlackShotGlow);

                _heavyShotGlow.GetComponent<Rigidbody2D>().gravityScale = 1f;

                return _heavyShotGlow;
            }
        }

        private GameObject _heavyShotGlow;

        private GameObject SilentLongLifetime
        {
            get
            {
                if (_silentLongLifetime != null) return _silentLongLifetime;

                _silentLongLifetime = Instantiate(SilentShot);

                _silentLongLifetime.GetComponent<AutoRecycleSelf>().timeToWait *= 2f;

                return _silentLongLifetime;
            }
        }

        private GameObject _silentLongLifetime;

        private GameObject SilentShot
        {
            get
            {
                if (_silentShot != null) return _silentShot;

                _silentShot = Instantiate(_control.GetAction<FlingObjectsFromGlobalPoolTime>("SmallShot LowHigh").gameObject.Value);

                Destroy(_silentShot.GetComponent<AudioSource>());

                return _silentShot;
            }
        }

        private GameObject _silentShot;

        private GameObject BlackShot
        {
            get
            {
                if (_blackShot != null) return _blackShot;

                _blackShot = Instantiate(SilentShot);

                _blackShot.GetComponent<tk2dSprite>().color = Color.black;

                ParticleSystem.MainModule main = _blackShot.GetComponentInChildren<ParticleSystem>(true).main;

                main.startColor = Color.black;

                var psrend = _blackShot.GetComponentInChildren<ParticleSystemRenderer>();

                psrend.material = new Material(psrend.material)
                {
                    color = Color.black,
                    // Particles/Multiply adds up to black, Particles/Additive adds up to white.
                    shader = Shader.Find("Particles/Multiply")
                };

                // Disabling this won't work for some reason, so we just set it transparent.
                _blackShot.FindGameObjectInChildren("Beam").GetComponent<tk2dSprite>().color = new Color(0, 0, 0, 0);

                _blackShot.FindGameObjectInChildren("Glow").GetComponent<tk2dSprite>().color = Color.black;

                return _blackShot;
            }
        }

        private GameObject _blackShot;

        private GameObject BlackShotGlow
        {
            get
            {
                if (_blackShotGlow != null) return _blackShotGlow;

                _blackShotGlow = Instantiate(BlackShot);

                GameObject glow = _blackShotGlow.FindGameObjectInChildren("Glow");

                glow.GetComponent<tk2dSprite>().color = Color.white;
                Destroy(glow.GetComponent<DeactivateAfter2dtkAnimation>());
                glow.AddComponent<Replay2dtkAnimation>();

                return _blackShotGlow;
            }
        }

        private GameObject _blackShotGlow;

        private void Awake()
        {
            _hm = gameObject.GetComponent<HealthManager>();
            _control = gameObject.LocateMyFSM("Control");
            _stuns = gameObject.LocateMyFSM("Stun Control");
            _anim = gameObject.GetComponent<tk2dSpriteAnimator>();
        }

        private IEnumerator Start()
        {
            yield return null;

            while (HeroController.instance == null)
                yield return null;

            if (!PlayerData.instance.statueStateHollowKnight.usingAltVersion) yield break;

            Destroy(_stuns);

            _control.Fsm.GetFsmFloat("Idle Time Min").Value = 0f;
            _control.Fsm.GetFsmFloat("Idle Time Max").Value = 0.01f;

            _control.GetAction<FloatMultiply>("Slash1 Recover").multiplyBy = 5 / 6f;

            HeroController.instance.AddMPChargeSpa(999);

            _trail = AddTrail(gameObject, 1.8f);

            _hm.hp = HP;

            #if DEBUG
            _control.Fsm.GetFsmInt("Half HP").Value = HP;
            _control.Fsm.GetFsmInt("Quarter HP").Value = HP;
            _hm.hp /= 3;
            #else
            _control.Fsm.GetFsmInt("Half HP").Value = HP    * 2 / 3;
            _control.Fsm.GetFsmInt("Quarter HP").Value = HP * 1 / 3;
            #endif

            for (int i = 0; i < 3; i++)
            {
                _control.RemoveAction<IntOperator>("Set Phase HP");
            }

            _control.InsertMethod("Tele Out", 0, () => { _trail.Pause(); });

            foreach (string state in new string[] {"Tele In", "Tele Cancel"})
            {
                _control.InsertMethod(state, 0, () => { _trail.Play(); });
            }

            CreateArcs();

            CreateHighLow();

            // Proj is an Action because I don't want to invoke a property get until later.
            Action ProjectileSpawner(Func<GameObject> proj, float speed)
            {
                return () =>
                {
                    Quaternion angle = Quaternion.Euler(Vector3.zero);
                    Vector3 pos = transform.position;
                    float x = speed * Math.Sign(transform.localScale.x);

                    for (float i = 0; i <= 3; i += 1.5f)
                    {
                        Instantiate(proj?.Invoke(), pos.SetY(pos.y - i), angle)
                            .GetComponent<Rigidbody2D>()
                            .velocity = new Vector2(x, 0);
                    }
                };
            }

            _control.InsertMethod("Slash1", 0, ProjectileSpawner(() => SilentShot, 30f));

            _control.InsertMethod("Tendril Start", 0, ProjectileSpawner(() => BlackShot, 40f));

            AddAlternatePlumes();

            _control.InsertCoroutine("SmallShot Antic", 0, ShotTeleInBurst, false);

            _control.GetAction<Wait>("Tendril Burst").time.Value /= 1.5f;

            _control.GetAction<Wait>("Focus Charge").time.Value /= 1.25f;

            AddChestShot();

            AddDashTele();

            foreach (KeyValuePair<string, float> i in _fpsDict)
            {
                _anim.GetClipByName(i.Key).fps = i.Value;
            }

            #if DEBUG
            foreach (FsmState state in _control.FsmStates)
            {
                _control.InsertMethod(state.Name, 0, () => Log($"Start: {state.Name}"));
                _control.InsertMethod(state.Name, state.Actions.Length, () => Log($"End: {state.Name}"));
            }
            
            foreach (tk2dSpriteAnimationClip clip in _anim.Library.clips)
            {
                Log($"{clip.name}: {clip.fps}");
            }
            #endif

            Log("Done.");
        }

        private void AddDashTele()
        {
            ParticleSystem.MainModule main = _trail.main;
            var psr = _trail.GetComponent<ParticleSystemRenderer>();

            bool tele = false;

            IEnumerator TeleOut()
            {
                tele = false;

                // After Tele can transition here if the Tele fails so I want to reset the trail regardless.
                if (main.startColor.color == Color.black)
                {
                    psr.material.shader = Shader.Find("Particles/Additive");
                    main.startColor = Color.white;
                    _trail.Play();
                }

                if (_hm.hp           > HP * 2 / 3) yield break;
                if (_rand.Next(0, 2) == 0) yield break;

                tele = true;

                psr.material.shader = Shader.Find("Particles/Multiply");
                main.startColor = Color.black;

                yield return new WaitForSeconds(.20f);

                _anim.Stop();

                _trail.Pause();

                _control.SetState("Tele Out Dash");
            }

            _control.CreateState("Dash Wall");

            // ReSharper disable once ImplicitlyCapturedClosure
            void ConditionalEvent()
            {
                if (tele)
                {
                    _control.SetState("Tele Out Dash");
                }
            }

            _control.AddMethod("Dash Wall", ConditionalEvent);

            _control.ChangeTransition("Dash", "WALL", "Dash Wall");

            _control.AddTransition("Dash Wall", FsmEvent.Finished, "Dash Recover");

            _control.AddCoroutine("Dash", TeleOut);

            _control.CopyState("Tele Out", "Tele Out Dash");
            _control.CopyState("TelePos Slash", "TelePos DashOut");
            _control.CopyState("Tele In", "Tele In Dash");

            _control.AddTransition("Dash", "TELE", "Tele Out Dash");

            _control.ChangeTransition("Tele Out Dash", "FINISHED", "TelePos DashOut");
            _control.ChangeTransition("TelePos DashOut", "FINISHED", "Tele In Dash");
            _control.ChangeTransition("Tele In Dash", "FINISHED", "Dash Continue");

            _control.GetAction<SetStringValue>("TelePos DashOut").stringValue = "DASH";

            _control.CreateState("Dash Continue");

            IEnumerator ResumeDash()
            {
                psr.material.shader = Shader.Find("Particles/Additive");
                main.startColor = Color.white;

                _trail.Play();

                transform.localScale = transform.localScale.SetX
                (
                    Math.Abs(transform.localScale.x) *
                    Math.Sign(HeroController.instance.transform.position.x - transform.position.x)
                );

                gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(60 * Math.Sign(transform.localScale.x), 0);

                _anim.Play("Dash");

                yield return new WaitForSeconds(.15f);

                _anim.Stop();
            }

            _control.InsertCoroutine("Dash Continue", 0, ResumeDash);

            _control.AddTransition("Dash Continue", FsmEvent.Finished, "Dash Recover");
        }

        private void AddChestShot()
        {
            string[] states =
            {
                "ChestShot Antic",
                "ChestShot Rise",
                "ChestShot",
                "ChestShot Pause",
                "ChestShot Fall",
                "ChestShot Recover"
            };

            for (int i = 0; i < states.Length; i++)
            {
                string state = states[i];

                _control.CreateState(state);

                _control.AddTransition(state, FsmEvent.Finished, i + 1 < states.Length
                                           ? states[i + 1]
                                           : "Idle Stance"
                );
            }

            IEnumerator ChestShotAntic()
            {
                gameObject.GetComponent<Rigidbody2D>().isKinematic = false;

                _anim.Play("ChestShot Antic");

                yield return new WaitForSeconds(_anim.GetClipByName("ChestShot Antic").Duration);
            }

            _control.InsertCoroutine("ChestShot Antic", 0, ChestShotAntic);

            IEnumerator ChestShotRise()
            {
                _anim.Play("ChestShot");
                gameObject.GetComponent<Rigidbody2D>().gravityScale = 0f;
                gameObject.FindGameObjectInChildren("Idle").SetActive(false);
                gameObject.FindGameObjectInChildren("ChestShot").SetActive(true);
                GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");

                var rb = gameObject.GetComponent<Rigidbody2D>();

                float y = transform.position.y + 4;

                _trail.Pause();

                rb.velocity = transform.up * 4.20f;

                while (transform.position.y < y)
                {
                    yield return null;
                }

                rb.velocity = Vector3.zero;

                _trail.Play();

                yield return new WaitForSeconds(0.85f);
            }

            _control.InsertCoroutine("ChestShot Rise", 0, ChestShotRise);

            IEnumerator ChestShot()
            {
                GameCameras.instance.cameraShakeFSM.Fsm.GetFsmBool("RumblingMed").Value = true;

                Quaternion down = Quaternion.Euler(Vector3.down);
                Quaternion right = Quaternion.Euler(Vector3.right);

                for (int _ = 0; _ < 6; _++)
                {
                    for (float i = 29.3f; i <= 61.7f; i += 4.6f)
                    {
                        Instantiate(SilentLongLifetime, new Vector3(i, 20), down)
                            .GetComponent<Rigidbody2D>()
                            .velocity = new Vector2(0, -10);
                    }

                    for (float i = 6.4f; i <= 20f; i += 4f)
                    {
                        Instantiate(SilentLongLifetime, new Vector3(29.3f, i), right)
                            .GetComponent<Rigidbody2D>()
                            .velocity = new Vector2(10, 0);
                    }

                    yield return new WaitForSeconds(.75f);
                }
            }

            _control.InsertCoroutine("ChestShot", 0, ChestShot);

            _control.InsertMethod("ChestShot Pause", 0, () => { GameCameras.instance.cameraShakeFSM.Fsm.GetFsmBool("RumblingMed").Value = false; });

            _control.AddAction("ChestShot Pause", new Wait
            {
                time = 0.75f,
                finishEvent = FsmEvent.Finished
            });

            IEnumerator ChestShotFall()
            {
                _anim.Play("Jump");
                gameObject.GetComponent<Rigidbody2D>().gravityScale = _control.Fsm.GetFsmFloat("Gravity Scale").Value;

                // Floor level
                while (transform.position.y >= 9.5)
                {
                    yield return null;
                }
            }

            _control.InsertCoroutine("ChestShot Fall", 0, ChestShotFall);

            IEnumerator ChestShotRecover()
            {
                _anim.Play("FallToStun");
                GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
                gameObject.FindGameObjectInChildren("Stun").SetActive(true);

                yield return new WaitForSeconds(2);

                _anim.Play("StunToIdle");
                gameObject.FindGameObjectInChildren("Stun").SetActive(false);
                gameObject.FindGameObjectInChildren("Idle").SetActive(true);

                yield return new WaitForSeconds(_anim.GetClipByName("StunToIdle").Duration);
            }

            _control.InsertCoroutine("ChestShot Recover", 0, ChestShotRecover);

            _control.GetAction<SendRandomEventV3>("Choice P3").AddToSendRandomEventV3("ChestShot Antic", .2f, 1, 5);
        }

        private void CreateHighLow()
        {
            foreach (string state in new string[] {"L Wave", "R Wave"})
            {
                _control.GetAction<SetFloatValue>(state, 1).floatValue = (state.StartsWith("R") ? 1 : -1) * 180;
                _control.GetAction<SetFloatValue>(state, 6).floatValue = (state.StartsWith("R") ? -1 : 1) * 180;
                _control.RemoveAction(state, 3);
            }

            _control.GetAction<Wait>("SmallShot LowHigh").time = _control.GetAction<Wait>("SmallShot HighLow").time = .7f;

            var highlow = _control.GetAction<FlingObjectsFromGlobalPoolTime>("SmallShot HighLow");
            var lowhigh = _control.GetAction<FlingObjectsFromGlobalPoolTime>("SmallShot LowHigh");
            highlow.gameObject = lowhigh.gameObject;
            highlow.frequency = lowhigh.frequency;
            highlow.speedMax = highlow.speedMin = lowhigh.speedMax;
            highlow.originVariationY.Value *= -1;

            _control.RemoveAction("SmallShot HighLow", 0);

            _control.InsertAction("SmallShot HighLow", _control.GetAction("SmallShot LowHigh", 0), 0);
        }

        private void CreateArcs()
        {
            string[] states =
            {
                "L Wave",
                "R Wave",
                "SmallShot HighLow",
                "SmallShot Recover",
                "SmallShot Start",
                "SmallShot Antic",
                "SmallShot Dir",
                "Smallshot Distance",
                "SmallShot LowHigh"
            };

            foreach (string state in states)
            {
                string CloneName(string orig) => "Arc " + orig.Replace("SmallShot ", "");

                FsmState clone = _control.CopyState(state, CloneName(state));

                #if DEBUG
                Log("Created state " + clone.Name);
                #endif

                foreach (FsmTransition trans in clone.Transitions)
                {
                    if (!states.Contains(trans.ToState)) continue;
                    _control.ChangeTransition(clone.Name, trans.EventName, CloneName(trans.ToState));
                }
            }

            foreach (string state in new string[] {"Arc L Wave", "Arc R Wave"})
            {
                _control.RemoveAction<SendEvent>(state);
                _control.RemoveAction(state, 2);
            }

            var arcLowHighLAngle = _control.GetAction<RandomFloat>("Arc L Wave");

            arcLowHighLAngle.min = 240;
            arcLowHighLAngle.max = 260;

            _control.GetAction<Wait>("Arc LowHigh").time.Value /= 1.6f;

            var arcHighLowLAngle = _control.GetAction<RandomFloat>("Arc L Wave", 3);

            arcHighLowLAngle.min = 135;
            arcHighLowLAngle.max = 150;

            _control.GetAction<FloatAdd>("Arc HighLow").add = -18;

            _control.GetAction<Wait>("Arc HighLow").time = 1.1f;

            // Each arc type has its own antic
            _control.InsertMethod("Arc Antic", 0, () =>
            {
                #if DEBUG
                Log
                (
                #endif
                _control.Fsm.GetFsmFloat("Chooser").Value = _rand.Next(0, 101)
                    #if DEBUG
                )
                #endif
                ;

                _control.GetAction<Tk2dPlayAnimationWithEvents>("Arc Antic").clipName = _control.Fsm.GetFsmFloat("Chooser").Value <= 50f
                    ? "DartShoot Antic"
                    : "SmallShot Antic";
            });

            // Tendril-esque antic
            _control.GetAction<Tk2dPlayAnimation>("Arc Start").clipName = "SmallShot";
            _control.AddAction("Arc Start", _control.GetAction<ActivateGameObject>("Tendril Burst"));
            _control.AddAction("Arc Recover", _control.GetAction<ActivateGameObject>("Tendril Recover"));

            string[] choices = {"Choice P3", "Choice P2"};

            foreach (string state in choices)
            {
                _control.GetAction<SendRandomEventV3>(state)
                        .AddToSendRandomEventV3
                        (
                            "Arc Antic",
                            .2f,
                            1,
                            5
                        );
            }

            var lowHighArc = _control.GetAction<FlingObjectsFromGlobalPoolTime>("Arc LowHigh");
            var highLowArc = _control.GetAction<FlingObjectsFromGlobalPoolTime>("Arc HighLow");

            lowHighArc.frequency = highLowArc.frequency = highLowArc.frequency.Value / 3f;

            _control.RemoveAction<FlingObjectsFromGlobalPoolTime>("Arc LowHigh");
            _control.RemoveAction<FlingObjectsFromGlobalPoolTime>("Arc HighLow");

            void CloneFling(FlingObjectsFromGlobalPoolTime orig, string state)
            {
                _control.AddAction(state, new FlingObjectsFromGlobalPoolTimeInstantiate
                {
                    gameObject = () => _control.Fsm.GetFsmFloat("Chooser").Value <= 50 ? HeavyShotGlow : HeavyShot,
                    spawnPoint = orig.spawnPoint,
                    position = orig.position,
                    frequency = orig.frequency.Value,
                    spawnMax = orig.spawnMax,
                    spawnMin = orig.spawnMax,
                    speedMin = orig.speedMin,
                    speedMax = orig.speedMax,
                    angleMax = orig.angleMax,
                    angleMin = orig.angleMin,
                    originVariationY = orig.originVariationY,
                    originVariationX = orig.originVariationX
                });
            }

            CloneFling(lowHighArc, "Arc LowHigh");
            CloneFling(highLowArc, "Arc HighLow");

            _control.GetAction<Wait>("Arc LowHigh").time = _control.GetAction<Wait>("Arc HighLow").time;
        }

        private void AddAlternatePlumes()
        {
            _control.CreateBool("Falling");

            _control.AddMethod("Stomp Land", () =>
            {
                _control.Fsm.GetFsmBool("Falling").Value = _rand.Next(0, 2) == 0;
            });

            void GenAlternatePlume()
            {
                GameObject go = Instantiate(_control.Fsm.GetFsmGameObject("Plume").Value);
                go.transform.Rotate(180, 0, 0);
                go.transform.position = new Vector3(go.transform.position.x + 1.85f, 22);

                if (!_control.Fsm.GetFsmBool("Falling").Value) return;

                Destroy(_control.Fsm.GetFsmGameObject("Plume").Value);

                go.GetComponent<tk2dSprite>().color = new Color(.675f, .678f, .686f);

                go.GetOrAddComponent<Rigidbody2D>().gravityScale = .7f;
            }

            _control.InsertMethod("Plume Gen", 5, GenAlternatePlume);
            _control.InsertMethod("Plume Gen", 3, GenAlternatePlume);
        }

        private IEnumerator ShotTeleInBurst()
        {
            const float speedMod = 1.6f;

            GameObject blast = Instantiate(_control.Fsm.GetFsmGameObject("Focus Blast").Value, transform);
            GameObject collider = Instantiate(_control.Fsm.GetFsmGameObject("Focus Hit").Value, transform);
            GameObject lines = Instantiate(_control.Fsm.GetFsmGameObject("Lines Anim").Value, transform);
            var anim = blast.GetComponent<Animator>();

            lines.SetActive(true);
            yield return new WaitForSeconds(.2f);
            Destroy(lines);

            blast.SetActive(true);

            anim.speed *= .4f;
            yield return new WaitForSeconds(.2f * .4f);
            anim.speed *= 1 / .4f;

            anim.speed *= speedMod;
            yield return new WaitForSeconds(.8f * (1 / speedMod));

            collider.SetActive(true);
            yield return new WaitForSeconds(_anim.GetClipByName("Focus Burst").Duration);

            Destroy(collider);
            Destroy(blast);
        }

        private static ParticleSystem AddTrail(GameObject go, float offset = 0, Color? c = null)
        {
            var trail = go.AddComponent<ParticleSystem>();
            var rend = trail.GetComponent<ParticleSystemRenderer>();

            rend.material = rend.trailMaterial = new Material(Shader.Find("Particles/Additive"))
            {
                mainTexture = Resources.FindObjectsOfTypeAll<Texture>().FirstOrDefault(x => x.name == "Default-Particle"),
                color = c ?? Color.white
            };

            ParticleSystem.MainModule main = trail.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f;
            main.startLifetime = .8f;
            main.startColor = c ?? Color.white;
            main.maxParticles *= 2;

            ParticleSystem.ShapeModule shapeModule = trail.shape;
            shapeModule.shapeType = ParticleSystemShapeType.Sphere;
            shapeModule.radius *= 1.4f;
            shapeModule.radiusSpeed = 0.01f;
            Vector3 pos = shapeModule.position;
            pos.y -= offset;
            shapeModule.position = pos;

            ParticleSystem.EmissionModule emission = trail.emission;
            emission.rateOverTime = 0f;
            emission.rateOverDistance = 30f;

            ParticleSystem.CollisionModule collision = trail.collision;
            collision.type = ParticleSystemCollisionType.World;
            collision.sendCollisionMessages = true;
            collision.mode = ParticleSystemCollisionMode.Collision2D;
            collision.enabled = true;
            collision.quality = ParticleSystemCollisionQuality.High;
            collision.maxCollisionShapes = 256;
            collision.dampenMultiplier = 0;
            collision.radiusScale = .3f;
            collision.collidesWith = 1 << 9;

            go.AddComponent<DamageOnCollision>().Damage = 2;

            return trail;
        }

        private static void Log(object s) => PrinceFinder.Log(s);
    }
}
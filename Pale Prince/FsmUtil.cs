using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using JetBrains.Annotations;
using Logger = Modding.Logger;

// Taken and modified from https://github.com/KayDeeTee/HK-NGG/blob/master/src/FsmUtil.cs

namespace Pale_Prince
{
    internal static class FsmUtil
    {
        // ReSharper disable once InconsistentNaming
        private static readonly FieldInfo FsmStringParamsFi = typeof(ActionData).GetField("fsmStringParams", BindingFlags.NonPublic | BindingFlags.Instance);

        [PublicAPI]
        public static void RemoveAction(this PlayMakerFSM fsm, string stateName, int index)
        {
            FsmState t = fsm.GetState(stateName);

            FsmStateAction[] actions = t.Actions;

            FsmStateAction action = fsm.GetAction(stateName, index);
            actions = actions.Where(x => x != action).ToArray();
            Log(action.GetType().ToString());

            t.Actions = actions;
        }

        [PublicAPI]
        public static void RemoveAction<T>(this PlayMakerFSM fsm, string stateName) where T : FsmStateAction
        {
            FsmState t = fsm.GetState(stateName);

            FsmStateAction[] actions = t.Actions;

            FsmStateAction action = fsm.GetAction<T>(stateName);
            actions = actions.Where(x => x != action).ToArray();
            Log(action.GetType().ToString());

            t.Actions = actions;
        }

        [PublicAPI]
        public static void RemoveAnim(this PlayMakerFSM fsm, string stateName, int index)
        {
            var anim = fsm.GetAction<Tk2dPlayAnimationWithEvents>(stateName, index);
            var @event = new FsmEvent(anim.animationCompleteEvent ?? anim.animationTriggerEvent);
            fsm.RemoveAction(stateName, index);
            fsm.InsertAction(stateName, new NextFrameEvent
            {
                sendEvent = @event,
                Active = true,
                Enabled = true
            }, index);
        }

        [PublicAPI]
        public static FsmState GetState(this PlayMakerFSM fsm, string stateName)
        {
            return fsm.FsmStates.FirstOrDefault(t => t.Name == stateName);
        }

        [PublicAPI]
        public static FsmState CopyState(this PlayMakerFSM fsm, string stateName, string newState)
        {
            var state = new FsmState(fsm.GetState(stateName)) {Name = newState};

            List<FsmState> fsmStates = fsm.FsmStates.ToList();
            fsmStates.Add(state);
            fsm.Fsm.States = fsmStates.ToArray();

            return state;
        }

        [PublicAPI]
        public static FsmStateAction GetAction(this PlayMakerFSM fsm, string stateName, int index)
        {
            return fsm.GetState(stateName).Actions[index];
        }

        [PublicAPI]
        public static T GetAction<T>(this PlayMakerFSM fsm, string stateName, int index) where T : FsmStateAction
        {
            return GetAction(fsm, stateName, index) as T;
        }

        [PublicAPI]
        public static T GetAction<T>(this PlayMakerFSM fsm, string stateName) where T : FsmStateAction
        {
            return fsm.GetState(stateName).Actions.FirstOrDefault(x => x is T) as T;
        }

        [PublicAPI]
        public static void AddAction(this PlayMakerFSM fsm, string stateName, FsmStateAction action)
        {
            FsmState t = fsm.GetState(stateName);

            FsmStateAction[] actions = t.Actions;

            Array.Resize(ref actions, actions.Length + 1);
            actions[actions.Length - 1] = action;

            t.Actions = actions;
        }

        [PublicAPI]
        public static void InsertAction(this PlayMakerFSM fsm, string stateName, FsmStateAction action, int index)
        {
            FsmState t = fsm.GetState(stateName);

            List<FsmStateAction> actions = t.Actions.ToList();

            actions.Insert(index, action);

            t.Actions = actions.ToArray();

            action.Init(t);
        }

        [PublicAPI]
        public static void InsertAction(this PlayMakerFSM fsm, string state, int ind, FsmStateAction action)
        {
            InsertAction(fsm, state, action, ind);
        }

        [PublicAPI]
        public static void ChangeTransition(this PlayMakerFSM fsm, string stateName, string eventName, string toState)
        {
            FsmState t = fsm.GetState(stateName);

            foreach (FsmTransition trans in t.Transitions)
            {
                if (trans.EventName == eventName)
                {
                    trans.ToState = toState;
                }
            }
        }
        
        [PublicAPI]
        public static void AddTransition(this PlayMakerFSM fsm, string stateName, FsmEvent @event, string toState)
        {
            FsmState t = fsm.GetState(stateName);

            List<FsmTransition> transitions = t.Transitions.ToList();
            transitions.Add(new FsmTransition
            {
                FsmEvent = @event,
                ToState = toState
            });
            t.Transitions = transitions.ToArray();
        }

        [PublicAPI]
        public static void AddTransition(this PlayMakerFSM fsm, string stateName, string eventName, string toState)
        {
            FsmState t = fsm.GetState(stateName);

            List<FsmTransition> transitions = t.Transitions.ToList();
            transitions.Add(new FsmTransition
            {
                FsmEvent = new FsmEvent(eventName),
                ToState = toState
            });
            t.Transitions = transitions.ToArray();
        }

        [PublicAPI]
        public static void RemoveTransitions
        (
            this PlayMakerFSM   fsm,
            IEnumerable<string> states,
            IEnumerable<string> transitions
        )
        {
            IEnumerable<string> enumerable = states as string[] ?? states.ToArray();

            foreach (FsmState t in fsm.FsmStates)
            {
                if (!enumerable.Contains(t.Name)) continue;

                t.Transitions = t.Transitions.Where(trans => !transitions.Contains(trans.ToState)).ToArray();
            }
        }

        [PublicAPI]
        public static void RemoveTransition(this PlayMakerFSM fsm, string stateName, string transition)
        {
            FsmState t = fsm.GetState(stateName);

            t.Transitions = t.Transitions.Where(trans => transition != trans.ToState).ToArray();
        }

        [PublicAPI]
        public static void ReplaceStringVariable(this PlayMakerFSM fsm, List<string> states, Dictionary<string, string> dict)
        {
            foreach (FsmState t in fsm.FsmStates)
            {
                bool found = false;
                if (!states.Contains(t.Name)) continue;
                foreach (FsmString str in (List<FsmString>) FsmStringParamsFi.GetValue(t.ActionData))
                {
                    List<FsmString> val = new List<FsmString>();
                    if (dict.ContainsKey(str.Value))
                    {
                        val.Add(dict[str.Value]);
                        found = true;
                    }
                    else
                    {
                        val.Add(str);
                    }

                    if (val.Count > 0)
                    {
                        FsmStringParamsFi.SetValue(t.ActionData, val);
                    }
                }

                if (found)
                {
                    t.LoadActions();
                }
            }
        }

        [PublicAPI]
        public static void ReplaceStringVariable(this PlayMakerFSM fsm, string state, Dictionary<string, string> dict)
        {
            foreach (FsmState t in fsm.FsmStates)
            {
                bool found = false;
                if (t.Name != state && state != "") continue;
                foreach (FsmString str in (List<FsmString>) FsmStringParamsFi.GetValue(t.ActionData))
                {
                    List<FsmString> val = new List<FsmString>();
                    if (dict.ContainsKey(str.Value))
                    {
                        val.Add(dict[str.Value]);
                        found = true;
                    }
                    else
                    {
                        val.Add(str);
                    }

                    if (val.Count > 0)
                    {
                        FsmStringParamsFi.SetValue(t.ActionData, val);
                    }
                }

                if (found)
                {
                    t.LoadActions();
                }
            }
        }

        [PublicAPI]
        public static void ReplaceStringVariable(this PlayMakerFSM fsm, string state, string src, string dst)
        {
            Log("Replacing FSM Strings");
            foreach (FsmState t in fsm.FsmStates)
            {
                bool found = false;
                if (t.Name != state && state != "") continue;
                Log($"Found FsmState with name \"{t.Name}\" ");
                foreach (FsmString str in (List<FsmString>) FsmStringParamsFi.GetValue(t.ActionData))
                {
                    List<FsmString> val = new List<FsmString>();
                    Log($"Found FsmString with value \"{str}\" ");
                    if (str.Value.Contains(src))
                    {
                        val.Add(dst);
                        found = true;
                        Log($"Found FsmString with value \"{str}\", changing to \"{dst}\" ");
                    }
                    else
                    {
                        val.Add(str);
                    }

                    if (val.Count > 0)
                    {
                        FsmStringParamsFi.SetValue(t.ActionData, val);
                    }
                }

                if (found)
                {
                    t.LoadActions();
                }
            }
        }
        
        [PublicAPI]
        public static void AddCoroutine(this PlayMakerFSM fsm, string stateName, Func<IEnumerator> method)
        {
            fsm.InsertCoroutine(stateName, fsm.GetState(stateName).Actions.Length, method);
        }
        
        [PublicAPI]
        public static void AddMethod(this PlayMakerFSM fsm, string stateName, Action method)
        {
            fsm.InsertMethod(stateName, fsm.GetState(stateName).Actions.Length, method);
        }

        [PublicAPI]
        public static void InsertMethod(this PlayMakerFSM fsm, string stateName, int index, Action method)
        {
            InsertAction(fsm, stateName, new InvokeMethod(method), index);
        }

        [PublicAPI]
        public static void InsertCoroutine(this PlayMakerFSM fsm, string stateName, int index, Func<IEnumerator> coro, bool wait = true)
        {
            InsertAction(fsm, stateName, new InvokeCoroutine(coro, wait), index);
        }

        [PublicAPI]
        public static FsmInt CreateInt(this PlayMakerFSM fsm, string intName)
        {
            var @new = new FsmInt(intName);
            List<FsmInt> intVars = fsm.FsmVariables.IntVariables.ToList();
            intVars.Add(@new);
            fsm.Fsm.Variables.IntVariables = intVars.ToArray();
            return @new;
        }
        
        [PublicAPI]
        public static FsmBool CreateBool(this PlayMakerFSM fsm, string boolName)
        {
            var @new = new FsmBool(boolName);
            List<FsmBool> boolVars = fsm.FsmVariables.BoolVariables.ToList();
            boolVars.Add(@new);
            fsm.Fsm.Variables.BoolVariables = boolVars.ToArray();
            return @new;
        }

        [PublicAPI]
        public static void AddToSendRandomEventV3
        (
            this SendRandomEventV3 sre,
            string                 toState,
            float                  weight,
            int                    eventMaxAmount,
            int                    missedMaxAmount,
            [CanBeNull] string     eventName = null
        )
        {
            var fsm = sre.Fsm.Owner as PlayMakerFSM;
            string state = sre.State.Name;
            eventName = eventName ?? toState.Split(' ').First();

            List<FsmEvent> events = sre.events.ToList();
            List<FsmFloat> weights = sre.weights.ToList();
            List<FsmInt> trackingInts = sre.trackingInts.ToList();
            List<FsmInt> eventMax = sre.eventMax.ToList();
            List<FsmInt> trackingIntsMissed = sre.trackingIntsMissed.ToList();
            List<FsmInt> missedMax = sre.missedMax.ToList();

            fsm.AddTransition(state, eventName, toState);

            events.Add(fsm.GetState(state).Transitions.Single(x => x.FsmEvent.Name == eventName).FsmEvent);
            weights.Add(weight);
            trackingInts.Add(fsm.CreateInt($"Ct {eventName}"));
            eventMax.Add(eventMaxAmount);
            trackingIntsMissed.Add(fsm.CreateInt($"Ms {eventName}"));
            missedMax.Add(missedMaxAmount);

            sre.events = events.ToArray();
            sre.weights = weights.ToArray();
            sre.trackingInts = trackingInts.ToArray();
            sre.eventMax = eventMax.ToArray();
            sre.trackingIntsMissed = trackingIntsMissed.ToArray();
            sre.missedMax = missedMax.ToArray();
        }

        [PublicAPI]
        public static FsmState CreateState(this PlayMakerFSM fsm, string stateName)
        {
            var state = new FsmState(fsm.Fsm) {Name = stateName};

            List<FsmState> fsmStates = fsm.FsmStates.ToList();
            fsmStates.Add(state);
            fsm.Fsm.States = fsmStates.ToArray();

            return state;
        }

        private static void Log(string str)
        {
            Logger.Log("[FSM UTIL]: " + str);
        }
    }

    ///////////////////////
    // Method Invocation //
    ///////////////////////

    public class InvokeMethod : FsmStateAction
    {
        private readonly Action _action;

        public InvokeMethod(Action a)
        {
            _action = a;
        }

        public override void OnEnter()
        {
            _action?.Invoke();
            Finish();
        }
    }

    public class InvokeCoroutine : FsmStateAction
    {
        private readonly Func<IEnumerator> _coro;
        private readonly bool _wait;

        public InvokeCoroutine(Func<IEnumerator> f, bool wait)
        {
            _coro = f;
            _wait = wait;
        }

        private IEnumerator Coroutine()
        {
            yield return _coro?.Invoke();
            Finish();
        }

        public override void OnEnter()
        {
            Fsm.Owner.StartCoroutine(_wait ? Coroutine() : _coro?.Invoke());
            if (!_wait) Finish();
        }
    }
}
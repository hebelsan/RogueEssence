﻿using System.Collections.Generic;
using RogueElements;
using RogueEssence.Data;
using System;

namespace RogueEssence.Dungeon
{
    public enum BattleActionType
    {
        None = -1,
        Skill,
        Item,
        Throw,
        Trap
    }

    public class BattleContext : UserTargetGameContext, IActionContext
    {
        public const int DEFAULT_ATTACK_SLOT = -1;
        public const int FAKE_ATTACK_SLOT = -2;
        public const int EQUIP_ITEM_SLOT = -1;
        public const int FLOOR_ITEM_SLOT = -2;
        public const int NO_ITEM_SLOT = -3;
        public const int FORCED_SLOT = -3;

        /// <summary>
        /// the tile of the user before it started a strike (used for tipper effects)
        /// wrapped; it can't go out of bounds anyway
        /// </summary>
        public Loc StrikeStartTile { get; set; }
        /// <summary>
        /// the tile of the user JUST AFTER it started a strike (used for updating position)
        /// wrapped; it can't go out of bounds anyway
        /// </summary>
        public Loc StrikeEndTile { get; set; }
        /// <summary>
        /// the direcion of the user before it started a strike (used for multistrike confusion)
        /// unwrapped
        /// </summary>
        public Dir8 StartDir { get; set; }
        /// <summary>
        /// the origin tile for the explosion
        /// unwrapped
        /// </summary>
        public Loc ExplosionTile { get; set; }
        /// <summary>
        ///  the location of the tile being targeted
        /// unwrapped
        /// </summary>
        public Loc TargetTile { get; set; }
        /// <summary>
        /// all tiles in which a strike's hitbox ended (used for item landing)
        /// unwrapped
        /// </summary>
        public List<Loc> StrikeLandTiles { get; set; }


        public BattleActionType ActionType { get; set; }
        public int UsageSlot;
        public int StrikesMade;//current strikes
        public int Strikes;//total strikes
        public CombatAction HitboxAction { get; set; }
        public ExplosionData Explosion { get; set; }
        public BattleData Data { get; set; }
        public InvItem Item;//the item that is used, and most likely dropped
        public string SkillUsedUp;//the skill whose last charge was used up
        public AbortStatus TurnCancel;

        private bool actionSilent;
        private string actionMsg;

        public bool Hit;
        public int RangeMod;

        public StateCollection<ContextState> GlobalContextStates;


        public BattleContext(BattleActionType actionType) : base()
        {
            TurnCancel = new AbortStatus();
            this.ActionType = actionType;
            UsageSlot = BattleContext.DEFAULT_ATTACK_SLOT;
            SkillUsedUp = "";
            StrikeLandTiles = new List<Loc>();
            actionMsg = "";
            GlobalContextStates = new StateCollection<ContextState>();
        }

        public BattleContext(BattleContext other, bool copyGlobal) : base(other)
        {
            TurnCancel = other.TurnCancel;
            if (copyGlobal)
                GlobalContextStates = other.GlobalContextStates.Clone();
            else
                GlobalContextStates = other.GlobalContextStates;
            StrikeStartTile = other.StrikeStartTile;
            StrikeEndTile = other.StrikeEndTile;
            StartDir = other.StartDir;
            ExplosionTile = other.ExplosionTile;
            TargetTile = other.TargetTile;
            if (copyGlobal)
            {
                StrikeLandTiles = new List<Loc>();
                StrikeLandTiles.AddRange(other.StrikeLandTiles);
            }
            else
                StrikeLandTiles = other.StrikeLandTiles;
            ActionType = other.ActionType;
            UsageSlot = other.UsageSlot;
            StrikesMade = other.StrikesMade;
            Strikes = other.Strikes;
            HitboxAction = other.HitboxAction.Clone();
            Explosion = new ExplosionData(other.Explosion);
            Data = new BattleData(other.Data);
            Item = new InvItem(other.Item);
            SkillUsedUp = other.SkillUsedUp;
            actionMsg = other.actionMsg;
            actionSilent = other.actionSilent;
            Hit = other.Hit;
            RangeMod = other.RangeMod;
        }

        public void SetActionMsg(string msg, bool silent = false)
        {
            actionMsg = msg;
            actionSilent = silent;
        }

        public void PrintActionMsg()
        {
            if (!String.IsNullOrEmpty(actionMsg))
                DungeonScene.Instance.LogMsg(actionMsg, actionSilent, false);
        }

        public int GetContextStateInt<T>(bool global, int defaultVal) where T : ContextIntState
        {
            return GetContextStateInt(typeof(T), global, defaultVal);
        }
        public int GetContextStateInt(Type type, bool global, int defaultVal)
        {
            ContextIntState countState = global ? (ContextIntState)GlobalContextStates.GetWithDefault(type) : (ContextIntState)ContextStates.GetWithDefault(type);
            if (countState == null)
                return defaultVal;
            else
                return countState.Count;
        }


        public void AddContextStateInt<T>(bool global, int addedVal) where T : ContextIntState
        {
            AddContextStateInt(typeof(T), global, addedVal);
        }

        public void AddContextStateInt(Type type, bool global, int addedVal)
        {
            ContextIntState countState = global ? (ContextIntState)GlobalContextStates.GetWithDefault(type) : (ContextIntState)ContextStates.GetWithDefault(type);
            if (countState == null)
            {
                ContextIntState newCount = (ContextIntState)Activator.CreateInstance(type);
                newCount.Count = addedVal;
                if (global)
                    GlobalContextStates.Set(newCount);
                else
                    ContextStates.Set(newCount);
            }
            else
                countState.Count += addedVal;
        }

        public Multiplier GetContextStateMult<T>() where T : ContextMultState
        {
            return GetContextStateMult<T>(false, new Multiplier(1, 1));
        }
        public Multiplier GetContextStateMult<T>(bool global, Multiplier defaultVal) where T : ContextMultState
        {
            return GetContextStateMult(typeof(T), global, defaultVal);
        }

        public Multiplier GetContextStateMult(Type type, bool global, Multiplier defaultVal)
        {
            ContextMultState countState = global ? (ContextMultState)GlobalContextStates.GetWithDefault(type) : (ContextMultState)ContextStates.GetWithDefault(type);
            if (countState == null)
                return defaultVal;
            else
                return countState.Mult;
        }

        public void AddContextStateMult<T>(bool global, int numerator, int denominator) where T : ContextMultState
        {
            AddContextStateMult(typeof(T), global, numerator, denominator);
        }

        public void AddContextStateMult(Type type, bool global, int numerator, int denominator)
        {
            ContextMultState multState = global ? (ContextMultState)GlobalContextStates.GetWithDefault(type) : (ContextMultState)ContextStates.GetWithDefault(type);
            if (multState == null)
            {
                ContextMultState newMult = (ContextMultState)Activator.CreateInstance(type);
                newMult.Mult = new Multiplier(numerator, denominator);
                if (global)
                    GlobalContextStates.Set(newMult);
                else
                    ContextStates.Set(newMult);
            }
            else
                multState.Mult.AddMultiplier(numerator, denominator);
        }



        public IEnumerator<YieldInstruction> TargetTileWithExplosion(Loc target)
        {
            BattleContext context = new BattleContext(this, false);

            context.ExplosionTile = target;

            //pre-explosion check
            yield return CoroutineManager.Instance.StartCoroutine(DungeonScene.Instance.BeforeExplosion(context));

            //release explosion
            yield return CoroutineManager.Instance.StartCoroutine(context.Explosion.ReleaseExplosion(context.ExplosionTile, context.User, context.ProcessHitLoc, context.ProcessHitTile));
        }

        public IEnumerator<YieldInstruction> ProcessHitLoc(Loc loc)
        {
            BattleContext actionContext = new BattleContext(this, false);

            Character charTarget = ZoneManager.Instance.CurrentMap.GetCharAtLoc(loc);
            actionContext.TargetTile = loc;
            if (charTarget != null && DungeonScene.Instance.IsTargeted(actionContext.User, charTarget, actionContext.Explosion.TargetAlignments))
            {
                actionContext.Target = charTarget;
                yield return CoroutineManager.Instance.StartCoroutine(DungeonScene.Instance.HitTarget(actionContext, charTarget));//hit the character
            }
            //do thing to tile
            yield return CoroutineManager.Instance.StartCoroutine(actionContext.User.HitTile(actionContext));
        }

        public IEnumerator<YieldInstruction> ProcessHitTile(Loc loc)
        {
            BattleContext actionContext = new BattleContext(this, false);
            actionContext.TargetTile = loc;

            //do thing to tile
            yield return CoroutineManager.Instance.StartCoroutine(actionContext.User.HitTile(actionContext));
        }
    }
}

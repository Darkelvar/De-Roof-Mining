using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
namespace drM
{
    public class DRMMModSettings : ModSettings
    {
        public bool RemoveThickRoofs;
        public override void ExposeData()
        {
            Scribe_Values.Look(ref RemoveThickRoofs,"RemoveThickRoofs");
            base.ExposeData();
        }
    }
    public class DRMMod : Mod
    {
        DRMMModSettings settings;
        public DRMMod(ModContentPack content) : base(content)
        {
            this.settings=GetSettings<DRMMModSettings>();
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            //listingStandard.CheckboxLabeled("Turn removing overhead mountain on/off",ref settings.RemoveThickRoofs,"Remove Overhead Mountains");
            listingStandard.CheckboxLabeled(Translator.Translate("drM.setting1"),ref settings.RemoveThickRoofs,Translator.Translate("drM.setting2"));
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }
        public override string SettingsCategory()
        {
            string toreturn2 = Translator.Translate("drM.settingsmodname");
            //return "De-Roof Mining";
            return toreturn2;
        }
    }
    [DefOf]public static class DesignationDefOf2
    {//seems like for some reason the code is able to treat DesignationDefOf2 just like it would treat the base DesignationDefOf in functions despite them not being related by subclassing, which means you can freely add your custom designations
        static DesignationDefOf2()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DesignationDefOf2));
        }
        public static DesignationDef drm_SmartMine;
    }
    public class WorkGiver_SmartMiner : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode
        {
            get
            {
                return PathEndMode.Touch;
            }
        }
        public override Danger MaxPathDanger(Pawn pawn)
        {
            return Danger.Deadly;
        }
        public static void ResetStaticData()
        {
            WorkGiver_SmartMiner.NoPathTrans = "NoPath".Translate();
        }
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            foreach(Designation designation in pawn.Map.designationManager.SpawnedDesignationsOfDef(DesignationDefOf2.drm_SmartMine))
            {
                bool flag=false;
                for(int i = 0; i < 8; i++)
                {
                    IntVec3 c = designation.target.Cell+GenAdj.AdjacentCells[i];
                    if (c.InBounds(pawn.Map) && c.Walkable(pawn.Map))
                    {
                        flag=true;
                        break;
                    }
                }
                if (flag)
                {
                    Mineable firstMineable = designation.target.Cell.GetFirstMineable(pawn.Map);
                    if (firstMineable != null)
                    {
                        yield return firstMineable;
                    }
                }
            }
            IEnumerator<Designation> enumerator = null;
            yield break;
            yield break;
        }
        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            return !pawn.Map.designationManager.AnySpawnedDesignationOfDef(DesignationDefOf2.drm_SmartMine);
        }
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!pawn.CanReserve(t, 1, -1, null, forced))
            {
                return null;
            }
            if (!t.def.mineable)
            {
                return null;
            }
            if (pawn.Map.designationManager.DesignationAt(t.Position, DesignationDefOf2.drm_SmartMine) == null)
            {
                return null;
            }
            /*if (!new HistoryEvent(HistoryEventDefOf.Mined, pawn.Named(HistoryEventArgsNames.Doer)).Notify_PawnAboutToDo_Job())
            {
                return null;
            }*/
            bool flag=false;
            for (int i = 0; i < 8; i++)
            {
                IntVec3 intVec=t.Position+GenAdj.AdjacentCells[i];
                if (intVec.InBounds(pawn.Map) && intVec.Standable(pawn.Map) && ReachabilityImmediate.CanReachImmediate(intVec, t, pawn.Map, PathEndMode.Touch, pawn))
                {
                    flag=true;
                    break;
                }
            }
            if (!flag)
            {
                for (int j = 0; j < 8; j++)
                {
                    IntVec3 intVec2 = t.Position+GenAdj.AdjacentCells[j];
                    if (intVec2.InBounds(t.Map) && ReachabilityImmediate.CanReachImmediate(intVec2, t, pawn.Map, PathEndMode.Touch, pawn) && intVec2.Walkable(t.Map) && !intVec2.Standable(t.Map))
                    {
                        Thing thing=null;
                        List<Thing> thingList=intVec2.GetThingList(t.Map);
                        for (int k = 0; k < thingList.Count; k++)
                        {
                            if (thingList[k].def.designateHaulable && thingList[k].def.passability == Traversability.PassThroughOnly)
                            {
                                thing=thingList[k];
                                break;
                            }
                        }
                        if (thing != null)
                        {
                            Job job=HaulAIUtility.HaulAsideJobFor(pawn,thing);
                            if (job != null)
                            {
                                return job;
                            }
                            JobFailReason.Is(WorkGiver_SmartMiner.NoPathTrans,null);
                            return null;
                        }
                    }
                }
                JobFailReason.Is(WorkGiver_SmartMiner.NoPathTrans,null);
                return null;
            }            
          return JobMaker.MakeJob(JobDefOf2.drm_SmartMine, t, 20000, true);               
        }
        private static string NoPathTrans;
        private const int MiningJobTicks=20000;
    }
    [DefOf]public static class JobDefOf2
    {
        static JobDefOf2()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(JobDefOf2));
        }
        public static JobDef drm_SmartMine;
    }
    public class Designator_SmartMine : Designator_Mine
    {
        public override int DraggableDimensions
        {
            get
            {
                return 2;
            }
        }
        public override bool DragDrawMeasurements
        {
            get
            {
                return true;
            }
        }
        protected override DesignationDef Designation
        {
            get
            {
                return DesignationDefOf2.drm_SmartMine;
            }
        }
        public Designator_SmartMine()
        {
            //this.defaultLabel="De-roof Mine".Translate("drM.defaultLabel");
            this.defaultLabel=Translator.Translate("drM.defaultLabel");
            this.icon=ContentFinder<Texture2D>.Get("UI/Designators/Mine",true);
            //this.defaultDesc="Designate areas of rock to be mined out, removing the roof beforehand.".Translate("drM.defaultDesc");
            this.defaultDesc=Translator.Translate("drM.defaultDesc");
            this.useMouseIcon=true;
            this.soundDragSustain=SoundDefOf.Designate_DragStandard;
            this.soundDragChanged=SoundDefOf.Designate_DragStandard_Changed;
            this.soundSucceeded=SoundDefOf.Designate_Mine;
            this.tutorTag="De-roof Mine";
        }
        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            if (!c.InBounds(base.Map))
            {
                return false;
            }
            if (base.Map.designationManager.DesignationAt(c, this.Designation) != null)
            {
                return AcceptanceReport.WasRejected;
            }
            if (c.Fogged(base.Map))
            {
                return true;
            }
            Mineable firstMineable=c.GetFirstMineable(base.Map);
            //string toreturn= "Must designate impassable mineable rocks.".Translate("drM.toreturn");
            string toreturn = Translator.Translate("drM.toreturn");
            if (firstMineable == null)
            {
                return toreturn;
            }
            AcceptanceReport result = this.CanDesignateThing(firstMineable);
            if (!result.Accepted)
            {
                return result;
            }
            return AcceptanceReport.WasAccepted;
        }
        public override AcceptanceReport CanDesignateThing(Thing t)
        {
            if (!t.def.mineable)
            {
                return false;
            }
            if (base.Map.designationManager.DesignationAt(t.Position, this.Designation) != null)
            {
                return AcceptanceReport.WasRejected;
            }
            return true;
        }
        public override void DesignateSingleCell(IntVec3 loc)
        {
            base.Map.designationManager.AddDesignation(new Designation(loc,this.Designation));
            base.Map.designationManager.TryRemoveDesignation(loc, DesignationDefOf.SmoothWall);
            base.Map.designationManager.TryRemoveDesignation(loc, DesignationDefOf.Mine);
            if (DebugSettings.godMode)
            {
                Mineable firstMineable=loc.GetFirstMineable(base.Map);
                if (firstMineable != null)
                {
                    firstMineable.DestroyMined(null);
                }
            }
        }
        public override void DesignateThing(Thing t)
        {
            this.DesignateSingleCell(t.Position);
        }
        protected override void FinalizeDesignationSucceeded()
        {
            base.FinalizeDesignationSucceeded();
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Mining,KnowledgeAmount.SpecificInteraction);
        }
        public override void SelectedUpdate()
        {
            GenUI.RenderMouseoverBracket();
        }
        public override void RenderHighlight(List<IntVec3> dragCells)
        {
            DesignatorUtility.RenderHighlightOverSelectableCells(this,dragCells);
        }
    }
    public class JobDriver_SmartMine : JobDriver
    {
        private Thing MineTarget
        {
            get
            {
                return this.job.GetTarget(TargetIndex.A).Thing;
            }
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.MineTarget, this.job, 1, -1, null, errorOnFailed);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Reserve.Reserve(TargetIndex.A, 1, -1, null).FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnDespawnedNullOrForbidden(TargetIndex.A);
            Toil doWork = new Toil();
            doWork.initAction = delegate ()
            {
                //if there's no roof, just treat this part of job as done
                if (base.Map.roofGrid.RoofAt(Cell) == null)
                {
                    this.ReadyForNextToil();
                    return;
                }
                else
                {
                    this.workLeft = 25f;
                }                
            };
            doWork.tickAction = delegate ()
            {
                float num = doWork.actor.GetStatValue(StatDefOf.ConstructionSpeed, true) * 1.7f;
                this.workLeft -= num;
                if (this.workLeft <= 0f)
                {
                    removedRoofs.Clear();                    
                    //if Remove Thick Roofs setting is on just remove the roof, otherwise only remove non-thick roofs
                    if (LoadedModManager.GetMod<DRMMod>().GetSettings<DRMMModSettings>().RemoveThickRoofs)
                    {
                        base.Map.roofGrid.SetRoof(Cell, null);
                        removedRoofs.Add(Cell);
                        RoofCollapseCellsFinder.CheckCollapseFlyingRoofs(removedRoofs, base.Map, true, false);
                        removedRoofs.Clear();
                    }
                    else
                    {
                        if (!base.Map.roofGrid.RoofAt(Cell).isThickRoof)
                        {
                            base.Map.roofGrid.SetRoof(Cell, null);
                            removedRoofs.Add(Cell);
                            RoofCollapseCellsFinder.CheckCollapseFlyingRoofs(removedRoofs, base.Map, true, false);
                            removedRoofs.Clear();
                        }
                    }
                    this.ReadyForNextToil();
                    return;
                }
            };
            doWork.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            //doWork.PlaySoundAtStart(SoundDefOf.Roof_Start); - sounds were getting annoying fast, removed
            //doWork.PlaySoundAtEnd(SoundDefOf.Roof_Finish); - sounds were getting annoying fast, removed
            doWork.WithEffect(EffecterDefOf.RoofWork, TargetIndex.A);
            doWork.WithProgressBar(TargetIndex.A, () => 1f - this.workLeft / 65f, false, -0.5f);
            doWork.defaultCompleteMode = ToilCompleteMode.Never;
            doWork.activeSkill = (() => SkillDefOf.Construction);
            yield return doWork;
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnDespawnedNullOrForbidden(TargetIndex.A);
            Toil mine = new Toil();
            mine.tickAction = delegate ()
            {
                Pawn actor = mine.actor;
                Thing mineTarget = this.MineTarget;
                if (this.ticksToPickHit < -100)
                {
                    this.ResetTicksToPickHit();
                }
                if (actor.skills != null && (mineTarget.Faction != actor.Faction || actor.Faction == null))
                {
                    actor.skills.Learn(SkillDefOf.Mining, 0.07f, false);
                }
                this.ticksToPickHit--;
                if (this.ticksToPickHit <= 0)
                {
                    IntVec3 position = mineTarget.Position;
                    if (this.effecter == null)
                    {
                        this.effecter = EffecterDefOf.Mine.Spawn();
                    }
                    this.effecter.Trigger(actor, mineTarget);
                    int num = mineTarget.def.building.isNaturalRock ? 80 : 40;
                    Mineable mineable = mineTarget as Mineable;
                    if (mineable == null || mineTarget.HitPoints > num)
                    {
                        DamageInfo dinfo = new DamageInfo(DamageDefOf.Mining, (float)num, 0f, -1f, mine.actor, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null);
                        mineTarget.TakeDamage(dinfo);
                    }
                    else
                    {
                        mineable.Notify_TookMiningDamage(mineTarget.HitPoints, mine.actor);
                        mineable.HitPoints = 0;
                        mineable.DestroyMined(actor);
                    }
                    if (mineTarget.Destroyed)
                    {
                        actor.Map.mineStrikeManager.CheckStruckOre(position, mineTarget.def, actor);
                        actor.records.Increment(RecordDefOf.CellsMined);
                        if (this.pawn.Faction != Faction.OfPlayer)
                        {
                            List<Thing> thingList = position.GetThingList(this.Map);
                            for (int i = 0; i < thingList.Count; i++)
                            {
                                thingList[i].SetForbidden(true, false);
                            }
                        }
                        if (this.pawn.Faction == Faction.OfPlayer && MineStrikeManager.MineableIsVeryValuable(mineTarget.def))
                        {
                            TaleRecorder.RecordTale(TaleDefOf.MinedValuable, new object[]
                            {
                                this.pawn,
                                mineTarget.def.building.mineableThing
                            });
                        }
                        if (this.pawn.Faction == Faction.OfPlayer && MineStrikeManager.MineableIsValuable(mineTarget.def) && !this.pawn.Map.IsPlayerHome)
                        {
                            TaleRecorder.RecordTale(TaleDefOf.CaravanRemoteMining, new object[]
                            {
                                this.pawn,
                                mineTarget.def.building.mineableThing
                            });
                        }
                        this.ReadyForNextToil();
                        return;
                    }
                    this.ResetTicksToPickHit();
                }
            };
            mine.defaultCompleteMode = ToilCompleteMode.Never;
            mine.WithProgressBar(TargetIndex.A, () => 1f - (float)this.MineTarget.HitPoints / (float)this.MineTarget.MaxHitPoints, false, -0.5f);
            mine.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            mine.activeSkill = (() => SkillDefOf.Mining);
            yield return mine;
            yield break;
        }
        protected IntVec3 Cell
        {
            get
            {
                return this.job.GetTarget(TargetIndex.A).Cell;
            }
        }
        protected bool DoWorkFailOn()
        {
            return !Cell.Roofed(base.Map);
        }
        private void ResetTicksToPickHit()
        {
            float num = this.pawn.GetStatValue(StatDefOf.MiningSpeed, true);
            if (num < 0.6f && this.pawn.Faction != Faction.OfPlayer)
            {
                num = 0.6f;
            }
            this.ticksToPickHit = (int)Math.Round((double)(100f / num));
        }
        private int ticksToPickHit = -1000;
        private Effecter effecter;
        protected PathEndMode PathEndMode { get { return PathEndMode.ClosestTouch; } }
        private float workLeft;
        private static List<IntVec3> removedRoofs = new List<IntVec3>();
    }
}
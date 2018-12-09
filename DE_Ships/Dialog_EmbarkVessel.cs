// Decompiled with JetBrains decompiler
// Type: RimWorld.Dialog_EmbarkVessel
// Assembly: Assembly-CSharp, Version=1.0.6901.14209, Culture=neutral, PublicKeyToken=null
// MVID: 46D57962-12D3-482C-B2BD-C50A13375FDD
// Assembly location: C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\Assembly-CSharp.dll

using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;
/*
namespace DE_Ships
{
    //differences between this and base Rimworld.Dialog_FromCaravan: changed name, uses VesselTicksPerMoveUtility insteaed of CaravanTicksPerMoveUtility, added caravan instance variable indicating 
    public class Dialog_EmbarkVessel : Window
    {
        private static List<TabRecord> tabsList = new List<TabRecord>();
        private static List<Thing> tmpPackingSpots = new List<Thing>();
        private float lastMassFlashTime = -9999f;
        private int startingTile = -1;
        private int destinationTile = -1;
        private bool massUsageDirty = true;
        private bool massCapacityDirty = true;
        private bool tilesPerDayDirty = true;
        private bool daysWorthOfFoodDirty = true;
        private bool foragedFoodPerDayDirty = true;
        private bool visibilityDirty = true;
        private bool ticksToArriveDirty = true;
        private readonly Vector2 BottomButtonSize = new Vector2(160f, 40f);
        private Map map;
        private bool reform;
        private Action onClosed;
        private bool canChooseRoute;
        private bool mapAboutToBeRemoved;
        public bool choosingRoute;
        private bool thisWindowInstanceEverOpened;
        public List<TransferableOneWay> transferables;
        private TransferableOneWayWidget pawnsTransfer;
        private TransferableOneWayWidget itemsTransfer;
        private Dialog_EmbarkVessel.Tab tab;
        private float cachedMassUsage;
        private float cachedMassCapacity;
        private string cachedMassCapacityExplanation;
        private float cachedTilesPerDay;
        private string cachedTilesPerDayExplanation;
        private Pair<float, float> cachedDaysWorthOfFood;
        private Pair<ThingDef, float> cachedForagedFoodPerDay;
        private string cachedForagedFoodPerDayExplanation;
        private float cachedVisibility;
        private string cachedVisibilityExplanation;
        private int cachedTicksToArrive;
        private const float TitleRectHeight = 35f;
        private const float BottomAreaHeight = 55f;
        private const float MaxDaysWorthOfFoodToShowWarningDialog = 5f;

        public Dialog_EmbarkVessel(Map map, bool reform = false, Action onClosed = null, bool mapAboutToBeRemoved = false)
        {
            this.map = map;
            this.reform = reform;
            this.onClosed = onClosed;
            this.mapAboutToBeRemoved = mapAboutToBeRemoved;
            this.canChooseRoute = !reform || !map.retainedCaravanData.HasDestinationTile;
            this.closeOnAccept = !reform;
            this.closeOnCancel = !reform;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
        }

        public int CurrentTile
        {
            get
            {
                return this.map.Tile;
            }
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(1024f, (float)UI.screenHeight);
            }
        }

        protected override float Margin
        {
            get
            {
                return 0.0f;
            }
        }

        private bool AutoStripSpawnedCorpses
        {
            get
            {
                return this.reform;
            }
        }

        private bool ListPlayerPawnsInventorySeparately
        {
            get
            {
                return this.reform;
            }
        }

        private BiomeDef Biome
        {
            get
            {
                return this.map.Biome;
            }
        }

        private bool MustChooseRoute
        {
            get
            {
                if (!this.canChooseRoute)
                    return false;
                if (this.reform)
                    return this.map.Parent is Settlement;
                return true;
            }
        }

        private bool ShowCancelButton
        {
            get
            {
                if (!this.mapAboutToBeRemoved)
                    return true;
                bool flag = false;
                for (int index = 0; index < this.transferables.Count; ++index)
                {
                    Pawn anyThing = this.transferables[index].AnyThing as Pawn;
                    if (anyThing != null && anyThing.IsColonist && !anyThing.Downed)
                    {
                        flag = true;
                        break;
                    }
                }
                return !flag;
            }
        }

        private IgnorePawnsInventoryMode IgnoreInventoryMode
        {
            get
            {
                return this.ListPlayerPawnsInventorySeparately ? IgnorePawnsInventoryMode.IgnoreIfAssignedToUnloadOrPlayerPawn : IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload;
            }
        }

        public float MassUsage
        {
            get
            {
                if (this.massUsageDirty)
                {
                    this.massUsageDirty = false;
                    this.cachedMassUsage = CollectionsMassCalculator.MassUsageTransferables(this.transferables, this.IgnoreInventoryMode, false, this.AutoStripSpawnedCorpses);
                }
                return this.cachedMassUsage;
            }
        }

        public float MassCapacity
        {
            get
            {
                if (this.massCapacityDirty)
                {
                    this.massCapacityDirty = false;
                    StringBuilder explanation = new StringBuilder();
                    this.cachedMassCapacity = CollectionsMassCalculator.CapacityTransferables(this.transferables, explanation);
                    this.cachedMassCapacityExplanation = explanation.ToString();
                }
                return this.cachedMassCapacity;
            }
        }

        private float TilesPerDay
        {
            get
            {
                if (this.tilesPerDayDirty)
                {
                    this.tilesPerDayDirty = false;
                    StringBuilder explanation = new StringBuilder();
                    this.cachedTilesPerDay = TilesPerDayCalculator.ApproxTilesPerDay(this.transferables, this.MassUsage, this.MassCapacity, this.CurrentTile, this.startingTile, explanation);
                    this.cachedTilesPerDayExplanation = explanation.ToString();
                }
                return this.cachedTilesPerDay;
            }
        }
        
        private Pair<float, float> DaysWorthOfFood
        {
            get
            {
                if (this.daysWorthOfFoodDirty)
                {
                    this.daysWorthOfFoodDirty = false;
                    float first;
                    float second;
                    if (this.destinationTile != -1)
                    {
                        using (WorldPath path = Find.WorldPathFinder.FindPath(this.CurrentTile, this.destinationTile, (Caravan)null, (Func<float, bool>)null))
                        {
                            int ticksPerMove = VesselTicksPerMoveUtility.GetTicksPerMove(new VesselTicksPerMoveUtility.VesselInfo(this), (StringBuilder)null);
                            first = DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood(this.transferables, this.CurrentTile, this.IgnoreInventoryMode, Faction.OfPlayer, path, 0.0f, ticksPerMove);
                            second = DaysUntilRotCalculator.ApproxDaysUntilRot(this.transferables, this.CurrentTile, this.IgnoreInventoryMode, path, 0.0f, ticksPerMove);
                        }
                    }
                    else
                    {
                        first = DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood(this.transferables, this.CurrentTile, this.IgnoreInventoryMode, Faction.OfPlayer, (WorldPath)null, 0.0f, 3300);
                        second = DaysUntilRotCalculator.ApproxDaysUntilRot(this.transferables, this.CurrentTile, this.IgnoreInventoryMode, (WorldPath)null, 0.0f, 3300);
                    }
                    this.cachedDaysWorthOfFood = new Pair<float, float>(first, second);
                }
                return this.cachedDaysWorthOfFood;
            }
        }
        

        private Pair<ThingDef, float> ForagedFoodPerDay
        {
            get
            {
                if (this.foragedFoodPerDayDirty)
                {
                    this.foragedFoodPerDayDirty = false;
                    StringBuilder explanation = new StringBuilder();
                    this.cachedForagedFoodPerDay = ForagedFoodPerDayCalculator.ForagedFoodPerDay(this.transferables, this.Biome, Faction.OfPlayer, explanation);
                    this.cachedForagedFoodPerDayExplanation = explanation.ToString();
                }
                return this.cachedForagedFoodPerDay;
            }
        }

        private float Visibility
        {
            get
            {
                if (this.visibilityDirty)
                {
                    this.visibilityDirty = false;
                    StringBuilder explanation = new StringBuilder();
                    this.cachedVisibility = CaravanVisibilityCalculator.Visibility(this.transferables, explanation);
                    this.cachedVisibilityExplanation = explanation.ToString();
                }
                return this.cachedVisibility;
            }
        }
        
        private int TicksToArrive
        {
            get
            {
                if (this.destinationTile == -1)
                    return 0;
                if (this.ticksToArriveDirty)
                {
                    this.ticksToArriveDirty = false;
                    using (WorldPath path = Find.WorldPathFinder.FindPath(this.CurrentTile, this.destinationTile, (Caravan)null, (Func<float, bool>)null))
                        this.cachedTicksToArrive = CaravanArrivalTimeEstimator.EstimatedTicksToArrive(this.CurrentTile, this.destinationTile, path, 0.0f, VesselTicksPerMoveUtility.GetTicksPerMove(new VesselTicksPerMoveUtility.VesselInfo(this), (StringBuilder)null), Find.TickManager.TicksAbs);
                }
                return this.cachedTicksToArrive;
            }
        }
        

        private bool MostFoodWillRotSoon
        {
            get
            {
                float num1 = 0.0f;
                float num2 = 0.0f;
                for (int index = 0; index < this.transferables.Count; ++index)
                {
                    TransferableOneWay transferable = this.transferables[index];
                    if (transferable.HasAnyThing && transferable.CountToTransfer > 0 && (transferable.ThingDef.IsNutritionGivingIngestible && !(transferable.AnyThing is Corpse)))
                    {
                        float num3 = 600f;
                        CompRottable comp = transferable.AnyThing.TryGetComp<CompRottable>();
                        if (comp != null)
                            num3 = (float)DaysUntilRotCalculator.ApproxTicksUntilRot_AssumeTimePassesBy(comp, this.CurrentTile, (List<Pair<int, int>>)null) / 60000f;
                        float num4 = transferable.ThingDef.GetStatValueAbstract(StatDefOf.Nutrition, (ThingDef)null) * (float)transferable.CountToTransfer;
                        if ((double)num3 < 5.0)
                            num1 += num4;
                        else
                            num2 += num4;
                    }
                }
                if ((double)num1 == 0.0 && (double)num2 == 0.0)
                    return false;
                return (double)num1 / ((double)num1 + (double)num2) >= 0.75;
            }
        }

        public override void PostOpen()
        {
            base.PostOpen();
            this.choosingRoute = false;
            if (this.thisWindowInstanceEverOpened)
                return;
            this.thisWindowInstanceEverOpened = true;
            this.CalculateAndRecacheTransferables();
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.FormCaravan, KnowledgeAmount.Total);
        }

        public override void PostClose()
        {
            base.PostClose();
            if (this.onClosed == null || this.choosingRoute)
                return;
            this.onClosed();
        }

        public void Notify_NoLongerChoosingRoute()
        {
            this.choosingRoute = false;
            if (Find.WindowStack.IsOpen((Window)this) || this.onClosed == null)
                return;
            this.onClosed();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect rect1 = new Rect(0.0f, 0.0f, inRect.width, 35f);
            Verse.Text.Font = GameFont.Medium;
            Verse.Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect1, (!this.reform ? "FormCaravan" : "ReformCaravan").Translate());
            Verse.Text.Font = GameFont.Small;
            Verse.Text.Anchor = TextAnchor.UpperLeft;
            CaravanUIUtility.DrawCaravanInfo(new CaravanUIUtility.CaravanInfo(this.MassUsage, this.MassCapacity, this.cachedMassCapacityExplanation, this.TilesPerDay, this.cachedTilesPerDayExplanation, this.DaysWorthOfFood, this.ForagedFoodPerDay, this.cachedForagedFoodPerDayExplanation, this.Visibility, this.cachedVisibilityExplanation, -1f, -1f, (string)null), new CaravanUIUtility.CaravanInfo?(), this.CurrentTile, this.destinationTile != -1 ? new int?(this.TicksToArrive) : new int?(), this.lastMassFlashTime, new Rect(12f, 35f, inRect.width - 24f, 40f), true, this.destinationTile != -1 ? "\n" + "DaysWorthOfFoodTooltip_OnlyFirstWaypoint".Translate() : (string)null, false);
            Dialog_EmbarkVessel.tabsList.Clear();
            Dialog_EmbarkVessel.tabsList.Add(new TabRecord("PawnsTab".Translate(), (Action)(() => this.tab = Dialog_EmbarkVessel.Tab.Pawns), this.tab == Dialog_EmbarkVessel.Tab.Pawns));
            Dialog_EmbarkVessel.tabsList.Add(new TabRecord("ItemsTab".Translate(), (Action)(() => this.tab = Dialog_EmbarkVessel.Tab.Items), this.tab == Dialog_EmbarkVessel.Tab.Items));
            inRect.yMin += 119f;
            Widgets.DrawMenuSection(inRect);
            TabDrawer.DrawTabs(inRect, Dialog_EmbarkVessel.tabsList, 200f);
            Dialog_EmbarkVessel.tabsList.Clear();
            inRect = inRect.ContractedBy(17f);
            inRect.height += 17f;
            GUI.BeginGroup(inRect);
            Rect rect2 = inRect.AtZero();
            this.DoBottomButtons(rect2);
            Rect inRect1 = rect2;
            inRect1.yMax -= 76f;
            bool anythingChanged = false;
            switch (this.tab)
            {
                case Dialog_EmbarkVessel.Tab.Pawns:
                    this.pawnsTransfer.OnGUI(inRect1, out anythingChanged);
                    break;
                case Dialog_EmbarkVessel.Tab.Items:
                    this.itemsTransfer.OnGUI(inRect1, out anythingChanged);
                    break;
            }
            if (anythingChanged)
                this.CountToTransferChanged();
            GUI.EndGroup();
        }

        public override bool CausesMessageBackground()
        {
            return true;
        }

        public void Notify_ChoseRoute(int destinationTile)
        {
            this.destinationTile = destinationTile;
            this.startingTile = CaravanExitMapUtility.BestExitTileToGoTo(destinationTile, this.map);
            this.ticksToArriveDirty = true;
            this.daysWorthOfFoodDirty = true;
            Messages.Message("MessageChoseRoute".Translate(), MessageTypeDefOf.CautionInput, false);
        }

        private void AddToTransferables(Thing t, bool setToTransferMax = false)
        {
            TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatching<TransferableOneWay>(t, this.transferables, TransferAsOneMode.PodsOrCaravanPacking);
            if (transferableOneWay == null)
            {
                transferableOneWay = new TransferableOneWay();
                this.transferables.Add(transferableOneWay);
            }
            transferableOneWay.things.Add(t);
            if (!setToTransferMax)
                return;
            transferableOneWay.AdjustTo(transferableOneWay.CountToTransfer + t.stackCount);
        }

        private void DoBottomButtons(Rect rect)
        {
            Rect rect1 = new Rect((float)((double)rect.width / 2.0 - (double)this.BottomButtonSize.x / 2.0), (float)((double)rect.height - 55.0 - 17.0), this.BottomButtonSize.x, this.BottomButtonSize.y);
            if (Widgets.ButtonText(rect1, "AcceptButton".Translate(), true, false, true))
            {
                if (this.reform)
                {
                    if (this.TryReformCaravan())
                    {
                        SoundDefOf.Tick_High.PlayOneShotOnCamera((Map)null);
                        this.Close(false);
                    }
                }
                else
                {
                    List<string> source = new List<string>();
                    Pair<float, float> daysWorthOfFood = this.DaysWorthOfFood;
                    if ((double)daysWorthOfFood.First < 5.0)
                        source.Add((double)daysWorthOfFood.First >= 0.100000001490116 ? "DaysWorthOfFoodWarningDialog".Translate((NamedArgument)daysWorthOfFood.First.ToString("0.#")) : "DaysWorthOfFoodWarningDialog_NoFood".Translate());
                    else if (this.MostFoodWillRotSoon)
                        source.Add("CaravanFoodWillRotSoonWarningDialog".Translate());
                    if (!TransferableUtility.GetPawnsFromTransferables(this.transferables).Any<Pawn>((Predicate<Pawn>)(pawn =>
                    {
                        if (CaravanUtility.IsOwner(pawn, Faction.OfPlayer))
                            return !pawn.skills.GetSkill(SkillDefOf.Social).TotallyDisabled;
                        return false;
                    })))
                        source.Add("CaravanIncapableOfSocial".Translate());
                    if (source.Count > 0)
                    {
                        if (this.CheckForErrors(TransferableUtility.GetPawnsFromTransferables(this.transferables)))
                            Find.WindowStack.Add((Window)Dialog_MessageBox.CreateConfirmation(string.Concat(source.Select<string, string>((Func<string, string>)(str => str + "\n\n")).ToArray<string>()) + "CaravanAreYouSure".Translate(), (Action)(() =>
                            {
                                if (!this.TryFormAndSendCaravan())
                                    return;
                                this.Close(false);
                            }), false, (string)null));
                    }
                    else if (this.TryFormAndSendCaravan())
                    {
                        SoundDefOf.Tick_High.PlayOneShotOnCamera((Map)null);
                        this.Close(false);
                    }
                }
            }
            if (Widgets.ButtonText(new Rect(rect1.x - 10f - this.BottomButtonSize.x, rect1.y, this.BottomButtonSize.x, this.BottomButtonSize.y), "ResetButton".Translate(), true, false, true))
            {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera((Map)null);
                this.CalculateAndRecacheTransferables();
            }
            if (this.ShowCancelButton && Widgets.ButtonText(new Rect(rect1.xMax + 10f, rect1.y, this.BottomButtonSize.x, this.BottomButtonSize.y), "CancelButton".Translate(), true, false, true))
                this.Close(true);
            if (this.canChooseRoute)
            {
                Rect rect2 = new Rect(rect.width - this.BottomButtonSize.x, rect1.y, this.BottomButtonSize.x, this.BottomButtonSize.y);
                if (Widgets.ButtonText(rect2, "ChooseRouteButton".Translate(), true, false, true))
                {
                    if (!TransferableUtility.GetPawnsFromTransferables(this.transferables).Any<Pawn>((Predicate<Pawn>)(x =>
                    {
                        if (CaravanUtility.IsOwner(x, Faction.OfPlayer))
                            return !x.Downed;
                        return false;
                    })))
                        Messages.Message("CaravanMustHaveAtLeastOneColonist".Translate(), MessageTypeDefOf.RejectInput, false);
                    else
                        Find.WorldRoutePlanner.Start((Dialog_FormCaravan)this);
                }
                if (this.destinationTile != -1)
                {
                    Rect rect3 = rect2;
                    rect3.y += rect2.height + 4f;
                    rect3.height = 200f;
                    rect3.xMin -= 200f;
                    Verse.Text.Anchor = TextAnchor.UpperRight;
                    Widgets.Label(rect3, "CaravanEstimatedDaysToDestination".Translate((NamedArgument)((float)this.TicksToArrive / 60000f).ToString("0.#")));
                    Verse.Text.Anchor = TextAnchor.UpperLeft;
                }
            }
            if (!Prefs.DevMode)
                return;
            float width = 200f;
            float height = this.BottomButtonSize.y / 2f;
            if (Widgets.ButtonText(new Rect(0.0f, (float)((double)rect.height - 55.0 - 17.0), width, height), "Dev: Send instantly", true, false, true) && this.DebugTryFormCaravanInstantly())
            {
                SoundDefOf.Tick_High.PlayOneShotOnCamera((Map)null);
                this.Close(false);
            }
            if (!Widgets.ButtonText(new Rect(0.0f, (float)((double)rect.height - 55.0 - 17.0) + height, width, height), "Dev: Select everything", true, false, true))
                return;
            SoundDefOf.Tick_High.PlayOneShotOnCamera((Map)null);
            this.SetToSendEverything();
        }

        private void CalculateAndRecacheTransferables()
        {
            this.transferables = new List<TransferableOneWay>();
            this.AddPawnsToTransferables();
            this.AddItemsToTransferables();
            CaravanUIUtility.CreateCaravanTransferableWidgets(this.transferables, out this.pawnsTransfer, out this.itemsTransfer, "FormCaravanColonyThingCountTip".Translate(), this.IgnoreInventoryMode, (Func<float>)(() => this.MassCapacity - this.MassUsage), this.AutoStripSpawnedCorpses, this.CurrentTile, this.mapAboutToBeRemoved);
            this.CountToTransferChanged();
        }

        private bool DebugTryFormCaravanInstantly()
        {
            List<Pawn> fromTransferables = TransferableUtility.GetPawnsFromTransferables(this.transferables);
            if (!fromTransferables.Any<Pawn>((Predicate<Pawn>)(x => CaravanUtility.IsOwner(x, Faction.OfPlayer))))
            {
                Messages.Message("CaravanMustHaveAtLeastOneColonist".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            this.AddItemsFromTransferablesToRandomInventories(fromTransferables);
            int directionTile = this.startingTile;
            if (directionTile < 0)
                directionTile = CaravanExitMapUtility.RandomBestExitTileFrom(this.map);
            if (directionTile < 0)
                directionTile = this.CurrentTile;
            CaravanFormingUtility.FormAndCreateCaravan((IEnumerable<Pawn>)fromTransferables, Faction.OfPlayer, this.CurrentTile, directionTile, this.destinationTile);
            return true;
        }

        private bool TryFormAndSendCaravan()
        {
            List<Pawn> fromTransferables = TransferableUtility.GetPawnsFromTransferables(this.transferables);
            if (!this.CheckForErrors(fromTransferables))
                return false;
            Direction8Way direction8WayFromTo = Find.WorldGrid.GetDirection8WayFromTo(this.CurrentTile, this.startingTile);
            IntVec3 spot;
            if (!this.TryFindExitSpot(fromTransferables, true, out spot))
            {
                if (!this.TryFindExitSpot(fromTransferables, false, out spot))
                {
                    Messages.Message("CaravanCouldNotFindExitSpot".Translate((NamedArgument)direction8WayFromTo.LabelShort()), MessageTypeDefOf.RejectInput, false);
                    return false;
                }
                Messages.Message("CaravanCouldNotFindReachableExitSpot".Translate((NamedArgument)direction8WayFromTo.LabelShort()), (LookTargets)new GlobalTargetInfo(spot, this.map, false), MessageTypeDefOf.CautionInput, false);
            }
            IntVec3 packingSpot;
            if (!this.TryFindRandomPackingSpot(spot, out packingSpot))
            {
                Messages.Message("CaravanCouldNotFindPackingSpot".Translate((NamedArgument)direction8WayFromTo.LabelShort()), (LookTargets)new GlobalTargetInfo(spot, this.map, false), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            CaravanFormingUtility.StartFormingCaravan(fromTransferables.Where<Pawn>((Func<Pawn, bool>)(x => !x.Downed)).ToList<Pawn>(), fromTransferables.Where<Pawn>((Func<Pawn, bool>)(x => x.Downed)).ToList<Pawn>(), Faction.OfPlayer, this.transferables, packingSpot, spot, this.startingTile, this.destinationTile);
            Messages.Message("CaravanFormationProcessStarted".Translate(), (LookTargets)((Thing)fromTransferables[0]), MessageTypeDefOf.PositiveEvent, false);
            return true;
        }

        private bool TryReformCaravan()
        {
            List<Pawn> fromTransferables = TransferableUtility.GetPawnsFromTransferables(this.transferables);
            if (!this.CheckForErrors(fromTransferables))
                return false;
            this.AddItemsFromTransferablesToRandomInventories(fromTransferables);
            Caravan caravan = CaravanExitMapUtility.ExitMapAndCreateCaravan((IEnumerable<Pawn>)fromTransferables, Faction.OfPlayer, this.CurrentTile, this.CurrentTile, this.destinationTile, false);
            this.map.Parent.CheckRemoveMapNow();
            string text = "MessageReformedCaravan".Translate();
            if (caravan.pather.Moving && caravan.pather.ArrivalAction != null)
                text = text + " " + "MessageFormedCaravan_Orders".Translate() + ": " + caravan.pather.ArrivalAction.Label + ".";
            Messages.Message(text, (LookTargets)((WorldObject)caravan), MessageTypeDefOf.TaskCompletion, false);
            return true;
        }

        private void AddItemsFromTransferablesToRandomInventories(List<Pawn> pawns)
        {
            this.transferables.RemoveAll((Predicate<TransferableOneWay>)(x => x.AnyThing is Pawn));
            if (this.ListPlayerPawnsInventorySeparately)
            {
                for (int index1 = 0; index1 < pawns.Count; ++index1)
                {
                    if (Dialog_EmbarkVessel.CanListInventorySeparately(pawns[index1]))
                    {
                        ThingOwner<Thing> innerContainer = pawns[index1].inventory.innerContainer;
                        for (int index2 = innerContainer.Count - 1; index2 >= 0; --index2)
                            this.RemoveCarriedItemFromTransferablesOrDrop(innerContainer[index2], pawns[index1], this.transferables);
                    }
                }
                for (int index = 0; index < this.transferables.Count; ++index)
                {
                    if (this.transferables[index].things.Any<Thing>((Predicate<Thing>)(x => !x.Spawned)))
                        this.transferables[index].things.SortBy<Thing, bool>((Func<Thing, bool>)(x => x.Spawned));
                }
            }
            for (int index = 0; index < this.transferables.Count; ++index)
            {
                if (!(this.transferables[index].AnyThing is Corpse))
                    TransferableUtility.Transfer(this.transferables[index].things, this.transferables[index].CountToTransfer, (Action<Thing, IThingHolder>)((splitPiece, originalHolder) =>
                    {
                        Thing thing = splitPiece.TryMakeMinified();
                        CaravanInventoryUtility.FindPawnToMoveInventoryTo(thing, pawns, (List<Pawn>)null, (Pawn)null).inventory.innerContainer.TryAdd(thing, true);
                    }));
            }
            for (int index = 0; index < this.transferables.Count; ++index)
            {
                if (this.transferables[index].AnyThing is Corpse)
                    TransferableUtility.TransferNoSplit(this.transferables[index].things, this.transferables[index].CountToTransfer, (Action<Thing, int>)((originalThing, numToTake) =>
                    {
                        if (this.AutoStripSpawnedCorpses)
                        {
                            Corpse corpse = originalThing as Corpse;
                            if (corpse != null && corpse.Spawned)
                                corpse.Strip();
                        }
                        Thing thing = originalThing.SplitOff(numToTake);
                        CaravanInventoryUtility.FindPawnToMoveInventoryTo(thing, pawns, (List<Pawn>)null, (Pawn)null).inventory.innerContainer.TryAdd(thing, true);
                    }), true, true);
            }
        }

        private bool CheckForErrors(List<Pawn> pawns)
        {
            if (this.MustChooseRoute && this.destinationTile < 0)
            {
                Messages.Message("MessageMustChooseRouteFirst".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            if (!this.reform && this.startingTile < 0)
            {
                Messages.Message("MessageNoValidExitTile".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            if (!pawns.Any<Pawn>((Predicate<Pawn>)(x =>
            {
                if (CaravanUtility.IsOwner(x, Faction.OfPlayer))
                    return !x.Downed;
                return false;
            })))
            {
                Messages.Message("CaravanMustHaveAtLeastOneColonist".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            if (!this.reform && (double)this.MassUsage > (double)this.MassCapacity)
            {
                this.FlashMass();
                Messages.Message("TooBigCaravanMassUsage".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            Pawn pawn = pawns.Find((Predicate<Pawn>)(x =>
            {
                if (!x.IsColonist)
                    return !pawns.Any<Pawn>((Predicate<Pawn>)(y =>
                    {
                        if (y.IsColonist)
                            return y.CanReach((LocalTargetInfo)((Thing)x), PathEndMode.Touch, Danger.Deadly, false, TraverseMode.ByPawn);
                        return false;
                    }));
                return false;
            }));
            if (pawn != null)
            {
                Messages.Message("CaravanPawnIsUnreachable".Translate((NamedArgument)pawn.LabelShort, (NamedArgument)((Thing)pawn)).CapitalizeFirst(), (LookTargets)((Thing)pawn), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            for (int index1 = 0; index1 < this.transferables.Count; ++index1)
            {
                if (this.transferables[index1].ThingDef.category == ThingCategory.Item)
                {
                    int countToTransfer = this.transferables[index1].CountToTransfer;
                    int num = 0;
                    if (countToTransfer > 0)
                    {
                        for (int index2 = 0; index2 < this.transferables[index1].things.Count; ++index2)
                        {
                            Thing t = this.transferables[index1].things[index2];
                            if (!t.Spawned || pawns.Any<Pawn>((Predicate<Pawn>)(x =>
                            {
                                if (x.IsColonist)
                                    return x.CanReach((LocalTargetInfo)t, PathEndMode.Touch, Danger.Deadly, false, TraverseMode.ByPawn);
                                return false;
                            })))
                            {
                                num += t.stackCount;
                                if (num >= countToTransfer)
                                    break;
                            }
                        }
                        if (num < countToTransfer)
                        {
                            if (countToTransfer == 1)
                                Messages.Message("CaravanItemIsUnreachableSingle".Translate((NamedArgument)this.transferables[index1].ThingDef.label), MessageTypeDefOf.RejectInput, false);
                            else
                                Messages.Message("CaravanItemIsUnreachableMulti".Translate((NamedArgument)countToTransfer, (NamedArgument)this.transferables[index1].ThingDef.label), MessageTypeDefOf.RejectInput, false);
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private bool TryFindExitSpot(List<Pawn> pawns, bool reachableForEveryColonist, out IntVec3 spot)
        {
            Rot4 rotFromTo = Find.WorldGrid.GetRotFromTo(this.CurrentTile, this.startingTile);
            if (!this.TryFindExitSpot(pawns, reachableForEveryColonist, rotFromTo, out spot) && !this.TryFindExitSpot(pawns, reachableForEveryColonist, rotFromTo.Rotated(RotationDirection.Clockwise), out spot))
                return this.TryFindExitSpot(pawns, reachableForEveryColonist, rotFromTo.Rotated(RotationDirection.Counterclockwise), out spot);
            return true;
        }

        private bool TryFindExitSpot(List<Pawn> pawns, bool reachableForEveryColonist, Rot4 exitDirection, out IntVec3 spot)
        {
            if (this.startingTile < 0)
            {
                Log.Error("Can't find exit spot because startingTile is not set.", false);
                spot = IntVec3.Invalid;
                return false;
            }
            Predicate<IntVec3> validator = (Predicate<IntVec3>)(x =>
            {
                if (!x.Fogged(this.map))
                    return x.Standable(this.map);
                return false;
            });
            if (reachableForEveryColonist)
                return CellFinder.TryFindRandomEdgeCellWith((Predicate<IntVec3>)(x =>
                {
                    if (!validator(x))
                        return false;
                    for (int index = 0; index < pawns.Count; ++index)
                    {
                        if (pawns[index].IsColonist && !pawns[index].Downed && !pawns[index].CanReach((LocalTargetInfo)x, PathEndMode.Touch, Danger.Deadly, false, TraverseMode.ByPawn))
                            return false;
                    }
                    return true;
                }), this.map, exitDirection, CellFinder.EdgeRoadChance_Always, out spot);
            IntVec3 intVec3_1 = IntVec3.Invalid;
            int num1 = -1;
            foreach (IntVec3 intVec3_2 in CellRect.WholeMap(this.map).GetEdgeCells(exitDirection).InRandomOrder<IntVec3>((IList<IntVec3>)null))
            {
                if (validator(intVec3_2))
                {
                    int num2 = 0;
                    for (int index = 0; index < pawns.Count; ++index)
                    {
                        if (pawns[index].IsColonist && !pawns[index].Downed && pawns[index].CanReach((LocalTargetInfo)intVec3_2, PathEndMode.Touch, Danger.Deadly, false, TraverseMode.ByPawn))
                            ++num2;
                    }
                    if (num2 > num1)
                    {
                        num1 = num2;
                        intVec3_1 = intVec3_2;
                    }
                }
            }
            spot = intVec3_1;
            return intVec3_1.IsValid;
        }

        private bool TryFindRandomPackingSpot(IntVec3 exitSpot, out IntVec3 packingSpot)
        {
            Dialog_EmbarkVessel.tmpPackingSpots.Clear();
            List<Thing> thingList = this.map.listerThings.ThingsOfDef(ThingDefOf.CaravanPackingSpot);
            TraverseParms traverseParams = TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false);
            for (int index = 0; index < thingList.Count; ++index)
            {
                if (this.map.reachability.CanReach(exitSpot, (LocalTargetInfo)thingList[index], PathEndMode.OnCell, traverseParams))
                    Dialog_EmbarkVessel.tmpPackingSpots.Add(thingList[index]);
            }
            if (!Dialog_EmbarkVessel.tmpPackingSpots.Any<Thing>())
                return RCellFinder.TryFindRandomSpotJustOutsideColony(exitSpot, this.map, out packingSpot);
            Thing thing = Dialog_EmbarkVessel.tmpPackingSpots.RandomElement<Thing>();
            Dialog_EmbarkVessel.tmpPackingSpots.Clear();
            packingSpot = thing.Position;
            return true;
        }

        private void AddPawnsToTransferables()
        {
            List<Pawn> pawnList = Dialog_EmbarkVessel.AllSendablePawns(this.map, this.reform);
            for (int index = 0; index < pawnList.Count; ++index)
            {
                bool setToTransferMax = (this.reform || this.mapAboutToBeRemoved) && !CaravanUtility.ShouldAutoCapture(pawnList[index], Faction.OfPlayer);
                this.AddToTransferables((Thing)pawnList[index], setToTransferMax);
            }
        }

        private void AddItemsToTransferables()
        {
            List<Thing> thingList = CaravanFormingUtility.AllReachableColonyItems(this.map, this.reform, this.reform, this.reform);
            for (int index = 0; index < thingList.Count; ++index)
                this.AddToTransferables(thingList[index], false);
            if (this.AutoStripSpawnedCorpses)
            {
                for (int index = 0; index < thingList.Count; ++index)
                {
                    if (thingList[index].Spawned)
                        this.TryAddCorpseInventoryAndGearToTransferables(thingList[index]);
                }
            }
            if (!this.ListPlayerPawnsInventorySeparately)
                return;
            List<Pawn> pawnList = Dialog_EmbarkVessel.AllSendablePawns(this.map, this.reform);
            for (int index1 = 0; index1 < pawnList.Count; ++index1)
            {
                if (Dialog_EmbarkVessel.CanListInventorySeparately(pawnList[index1]))
                {
                    ThingOwner<Thing> innerContainer = pawnList[index1].inventory.innerContainer;
                    for (int index2 = 0; index2 < innerContainer.Count; ++index2)
                    {
                        this.AddToTransferables(innerContainer[index2], true);
                        if (this.AutoStripSpawnedCorpses && innerContainer[index2].Spawned)
                            this.TryAddCorpseInventoryAndGearToTransferables(innerContainer[index2]);
                    }
                }
            }
        }

        private void TryAddCorpseInventoryAndGearToTransferables(Thing potentiallyCorpse)
        {
            Corpse corpse = potentiallyCorpse as Corpse;
            if (corpse == null)
                return;
            this.AddCorpseInventoryAndGearToTransferables(corpse);
        }

        private void AddCorpseInventoryAndGearToTransferables(Corpse corpse)
        {
            Pawn_InventoryTracker inventory = corpse.InnerPawn.inventory;
            Pawn_ApparelTracker apparel = corpse.InnerPawn.apparel;
            Pawn_EquipmentTracker equipment = corpse.InnerPawn.equipment;
            for (int index = 0; index < inventory.innerContainer.Count; ++index)
                this.AddToTransferables(inventory.innerContainer[index], false);
            if (apparel != null)
            {
                List<Apparel> wornApparel = apparel.WornApparel;
                for (int index = 0; index < wornApparel.Count; ++index)
                    this.AddToTransferables((Thing)wornApparel[index], false);
            }
            if (equipment == null)
                return;
            List<ThingWithComps> equipmentListForReading = equipment.AllEquipmentListForReading;
            for (int index = 0; index < equipmentListForReading.Count; ++index)
                this.AddToTransferables((Thing)equipmentListForReading[index], false);
        }

        private void RemoveCarriedItemFromTransferablesOrDrop(Thing carried, Pawn carrier, List<TransferableOneWay> transferables)
        {
            TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatchingDesperate(carried, transferables, TransferAsOneMode.PodsOrCaravanPacking);
            int count;
            if (transferableOneWay == null)
                count = carried.stackCount;
            else if (transferableOneWay.CountToTransfer >= carried.stackCount)
            {
                transferableOneWay.AdjustBy(-carried.stackCount);
                transferableOneWay.things.Remove(carried);
                count = 0;
            }
            else
            {
                count = carried.stackCount - transferableOneWay.CountToTransfer;
                transferableOneWay.AdjustTo(0);
            }
            if (count <= 0)
                return;
            Thing thing = carried.SplitOff(count);
            if (carrier.SpawnedOrAnyParentSpawned)
                GenPlace.TryPlaceThing(thing, carrier.PositionHeld, carrier.MapHeld, ThingPlaceMode.Near, (Action<Thing, int>)null, (Predicate<IntVec3>)null);
            else
                thing.Destroy(DestroyMode.Vanish);
        }

        private void FlashMass()
        {
            this.lastMassFlashTime = Time.time;
        }

        public static bool CanListInventorySeparately(Pawn p)
        {
            if (p.Faction != Faction.OfPlayer)
                return p.HostFaction == Faction.OfPlayer;
            return true;
        }

        private void SetToSendEverything()
        {
            for (int index = 0; index < this.transferables.Count; ++index)
                this.transferables[index].AdjustTo(this.transferables[index].GetMaximumToTransfer());
            this.CountToTransferChanged();
        }

        private void CountToTransferChanged()
        {
            this.massUsageDirty = true;
            this.massCapacityDirty = true;
            this.tilesPerDayDirty = true;
            this.daysWorthOfFoodDirty = true;
            this.foragedFoodPerDayDirty = true;
            this.visibilityDirty = true;
            this.ticksToArriveDirty = true;
        }

        public static List<Pawn> AllSendablePawns(Map map, bool reform)
        {
            return CaravanFormingUtility.AllSendablePawns(map, true, reform, reform, reform);
        }

        private enum Tab
        {
            Pawns,
            Items,
        }
    }
}
*/
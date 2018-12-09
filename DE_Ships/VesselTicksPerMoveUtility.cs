// Decompiled with JetBrains decompiler
// Type: RimWorld.Planet.VesselTicksPerMoveUtility
// Assembly: Assembly-CSharp, Version=1.0.6901.14209, Culture=neutral, PublicKeyToken=null
// MVID: 46D57962-12D3-482C-B2BD-C50A13375FDD
// Assembly location: C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\Assembly-CSharp.dll
/*
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld.Planet;
using RimWorld;

namespace DE_Ships
{
    //differences between base Rimworld.Planet.CaravanTicksPerMoveUtility: different name, changed CaravanInfo to use Dialog_EmbarkVessel parameter
    public static class VesselTicksPerMoveUtility
    {
        private const int MaxPawnTicksPerMove = 150;
        private const int DownedPawnMoveTicks = 450;
        public const float CellToTilesConversionRatio = 340f;
        public const int DefaultTicksPerMove = 3300;
        private const float MoveSpeedFactorAtZeroMass = 2f;

        public static int GetTicksPerMove(Caravan caravan, StringBuilder explanation = null)
        {
            if (caravan != null)
                return VesselTicksPerMoveUtility.GetTicksPerMove(new VesselTicksPerMoveUtility.VesselInfo(caravan), explanation);
            if (explanation != null)
                VesselTicksPerMoveUtility.AppendUsingDefaultTicksPerMoveInfo(explanation);
            return 3300;
        }

        public static int GetTicksPerMove(VesselTicksPerMoveUtility.VesselInfo caravanInfo, StringBuilder explanation = null)
        {
            return VesselTicksPerMoveUtility.GetTicksPerMove(caravanInfo.pawns, caravanInfo.massUsage, caravanInfo.massCapacity, explanation);
        }

        public static int GetTicksPerMove(List<Pawn> pawns, float massUsage, float massCapacity, StringBuilder explanation = null)
        {
            if (pawns.Any<Pawn>())
            {
                explanation?.Append("CaravanMovementSpeedFull".Translate() + ":");
                float num1 = 0.0f;
                for (int index = 0; index < pawns.Count; ++index)
                {
                    float num2 = Mathf.Min(pawns[index].Downed || pawns[index].CarriedByCaravan() ? 450f : (float)pawns[index].TicksPerMoveCardinal, 150f) * 340f;
                    float num3 = 60000f / num2;
                    if (explanation != null)
                    {
                        explanation.AppendLine();
                        explanation.Append("  - " + pawns[index].LabelShortCap + ": " + num3.ToString("0.#") + " " + "TilesPerDay".Translate());
                        if (pawns[index].Downed)
                            explanation.Append(" (" + "DownedLower".Translate() + ")");
                        else if (pawns[index].CarriedByCaravan())
                            explanation.Append(" (" + "Carried".Translate() + ")");
                    }
                    num1 += num2 / (float)pawns.Count;
                }
                float speedFactorFromMass = VesselTicksPerMoveUtility.GetMoveSpeedFactorFromMass(massUsage, massCapacity);
                if (explanation != null)
                {
                    float num2 = 60000f / num1;
                    explanation.AppendLine();
                    explanation.Append("  " + "Average".Translate() + ": " + num2.ToString("0.#") + " " + "TilesPerDay".Translate());
                    explanation.AppendLine();
                    explanation.Append("  " + "MultiplierForCarriedMass".Translate((NamedArgument)speedFactorFromMass.ToStringPercent()));
                }
                int num4 = Mathf.Max(Mathf.RoundToInt(num1 / speedFactorFromMass), 1);
                if (explanation != null)
                {
                    float num2 = 60000f / (float)num4;
                    explanation.AppendLine();
                    explanation.Append("  " + "FinalCaravanPawnsMovementSpeed".Translate() + ": " + num2.ToString("0.#") + " " + "TilesPerDay".Translate());
                }
                return num4;
            }
            if (explanation != null)
                VesselTicksPerMoveUtility.AppendUsingDefaultTicksPerMoveInfo(explanation);
            return 3300;
        }

        private static float GetMoveSpeedFactorFromMass(float massUsage, float massCapacity)
        {
            if ((double)massCapacity <= 0.0)
                return 1f;
            return Mathf.Lerp(2f, 1f, massUsage / massCapacity);
        }

        private static void AppendUsingDefaultTicksPerMoveInfo(StringBuilder sb)
        {
            sb.Append("CaravanMovementSpeedFull".Translate() + ":");
            float num = 18.18182f;
            sb.AppendLine();
            sb.Append("  " + "Default".Translate() + ": " + num.ToString("0.#") + " " + "TilesPerDay".Translate());
        }

        public struct VesselInfo
        {
            public List<Pawn> pawns;
            public float massUsage;
            public float massCapacity;

            public VesselInfo(Caravan caravan)
            {
                this.pawns = caravan.PawnsListForReading;
                this.massUsage = caravan.MassUsage;
                this.massCapacity = caravan.MassCapacity;
            }

            public VesselInfo(Dialog_EmbarkVessel formCaravanDialog)
            {
                this.pawns = TransferableUtility.GetPawnsFromTransferables(formCaravanDialog.transferables);
                this.massUsage = formCaravanDialog.MassUsage;
                this.massCapacity = formCaravanDialog.MassCapacity;
            }
        }
    }
}
*/
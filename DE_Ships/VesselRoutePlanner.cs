using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
/*
namespace DE_Ships
{
    //used to implement a version of WorldRoutePlanner.Start that uses Dialog_EmbarkVessel
    class VesselRoutePlanner
    {
        private Dialog_FormCaravan currentFormCaravanDialog;
        private bool active
        {
            get
            {
                return Find.WorldRoutePlanner.Active;
            }
        }
        private void Stop()
        {
            Find.WorldRoutePlanner.Stop();
        }
        private void Start()
        {
            Find.WorldRoutePlanner.Start();
        }
        private

        public void Start(Dialog_FormCaravan formCaravanDialog)
        {
            if (this.active)
                this.Stop();
            this.currentFormCaravanDialog = formCaravanDialog;
            this.caravanInfoFromFormCaravanDialog = new CaravanTicksPerMoveUtility.CaravanInfo?(new CaravanTicksPerMoveUtility.CaravanInfo(formCaravanDialog));
            formCaravanDialog.choosingRoute = true;
            Find.WindowStack.TryRemove((Window)formCaravanDialog, true);
            this.Start();
            this.TryAddWaypoint(formCaravanDialog.CurrentTile, true);
            this.cantRemoveFirstWaypoint = true;
        }
    }
}
*/
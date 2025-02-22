﻿using System.Collections;
using System.Collections.Generic;
using Movement;
using ActionsList;
using Actions;
using Arcs;
using Upgrade;
using Ship;
using Bombs;
using BoardTools;
using System.Linq;

namespace Ship
{
    namespace SecondEdition.TIESaBomber
    {
        public class TIESaBomber : FirstEdition.TIEBomber.TIEBomber, TIE
        {
            public TIESaBomber() : base()
            {
                ShipInfo.ShipName = "TIE/sa Bomber";

                ShipInfo.UpgradeIcons.Upgrades.Add(UpgradeType.Gunner);
                ShipInfo.UpgradeIcons.Upgrades.Add(UpgradeType.Bomb);
                ShipInfo.UpgradeIcons.Upgrades.Remove(UpgradeType.Torpedo);

                ShipInfo.ActionIcons.AddActions(new ActionInfo(typeof(ReloadAction), ActionColor.Red));
                ShipInfo.ActionIcons.AddLinkedAction(new LinkedActionInfo(typeof(BarrelRollAction), typeof(TargetLockAction)));

                ShipAbilities.Add(new Abilities.SecondEdition.NimbleBomber());

                IconicPilots[Faction.Imperial] = typeof(CaptainJonus);

                DialInfo.AddManeuver(new ManeuverHolder(ManeuverSpeed.Speed3, ManeuverDirection.Forward, ManeuverBearing.KoiogranTurn), MovementComplexity.Complex);
                DialInfo.ChangeManeuverComplexity(new ManeuverHolder(ManeuverSpeed.Speed2, ManeuverDirection.Left, ManeuverBearing.Turn), MovementComplexity.Normal);
                DialInfo.ChangeManeuverComplexity(new ManeuverHolder(ManeuverSpeed.Speed2, ManeuverDirection.Right, ManeuverBearing.Turn), MovementComplexity.Normal);

                ManeuversImageUrl = "https://vignette.wikia.nocookie.net/xwing-miniatures-second-edition/images/0/0e/Maneuver_tie_bomber.png";

                OldShipTypeName = "TIE Bomber";
            }
        }
    }
}

namespace Abilities.SecondEdition
{
    public class NimbleBomber : GenericAbility
    {
        public override string Name { get { return "Nimble Bomber"; } }

        public override void ActivateAbility()
        {
            HostShip.OnGetAvailableBombDropTemplates += AddNimbleBomberTemplates;
        }

        public override void DeactivateAbility()
        {
            HostShip.OnGetAvailableBombDropTemplates -= AddNimbleBomberTemplates;
        }

        private void AddNimbleBomberTemplates(List<ManeuverTemplate> availableTemplates, GenericUpgrade upgrade)
        {
            List<ManeuverTemplate> newTemplates = new List<ManeuverTemplate>()
            {
                new ManeuverTemplate(ManeuverBearing.Bank, ManeuverDirection.Right, ManeuverSpeed.Speed1, isBombTemplate: true),
                new ManeuverTemplate(ManeuverBearing.Bank, ManeuverDirection.Left, ManeuverSpeed.Speed1, isBombTemplate: true),
            };

            foreach (ManeuverTemplate newTemplate in newTemplates)
            {
                if (!availableTemplates.Any(t => t.Name == newTemplate.Name))
                {
                    availableTemplates.Add(newTemplate);
                }
            }

        }
    }
}

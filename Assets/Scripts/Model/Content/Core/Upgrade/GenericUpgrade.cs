﻿using Abilities;
using Mods;
using SquadBuilderNS;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Ship;
using Editions;
using BoardTools;
using Movement;

namespace Upgrade
{
    public class AvatarInfo
    {
        public Faction AvatarFaction;
        public Vector2 AvatarOffset;
        public Vector2 AvatarSize;

        public AvatarInfo(Faction faction, Vector2 offset, Vector2 size = default(Vector2))
        {
            AvatarFaction = faction;
            AvatarOffset = offset;
            AvatarSize = (size != default(Vector2)) ? size : new Vector2(100, 100);
        }
    }

    public enum UpgradeType
    {
        Force,
        Talent,
        System,
        Tech,
        Turret,
        Cannon,
        Missile,
        Crew,
        Gunner,
        Torpedo,
        Astromech,
        SalvagedAstromech,
        Bomb,
        Illicit,
        Modification,
        Title,
        Configuration,
        TacticalRelay,
        None,
        Omni // Free Mode Extra Option
    }

    public enum UpgradeSubType
    {
        None,
        Bomb,
        Mine,
        Remote
    }

    public abstract class GenericUpgrade : IImageHolder
    {
        public static GenericUpgrade CurrentUpgrade;

        public UpgradeCardInfo UpgradeInfo;
        public UpgradeCardState State;

        public GenericShip HostShip { get; set; }
        public UpgradeSlot Slot { get; set; }

        public List<GenericAbility> UpgradeAbilities = new List<GenericAbility>();

        public bool isPlaceholder = false;

        public string NamePostfix;

        private string nameCanonical;
        public string NameCanonical
        {
            get
            {
                if (!string.IsNullOrEmpty(nameCanonical)) return nameCanonical;

                return Tools.Canonicalize(UpgradeInfo.Name);
            }
            set { nameCanonical = value; }
        }

        private string imageUrl;
        public string ImageUrl
        {
            get
            {
                return imageUrl ?? ImageUrls.GetImageUrl(this);
            }
            set
            {
                imageUrl = value;
            }
        }

        public bool IsSecondSide;

        public string ImageUrlFE
        {
            get
            {
                return imageUrl ?? ImageUrls.GetImageUrlOld(this);
            }
        }

        public AvatarInfo Avatar;

        // SQUAD BUILDER ONLY

        public bool IsHidden;

        internal void TryDiscard(object confirmDecision)
        {
            throw new NotImplementedException();
        }

        //public Type FromMod { get; set; }
        public Type FromMod { get; set; }

        public bool HasEnoughSlotsInShip(GenericShip ship)
        {
            if (UpgradeInfo.UpgradeTypes.Count > 1)
            {
                List<UpgradeSlot> freeSlots = ship.UpgradeBar.GetFreeSlots(UpgradeInfo.UpgradeTypes);

                foreach (var requiredSlotType in UpgradeInfo.UpgradeTypes)
                {
                    UpgradeSlot freeSlotByType = freeSlots.FirstOrDefault(n => n.Type == requiredSlotType);
                    if (freeSlotByType != null)
                    {
                        freeSlots.Remove(freeSlotByType);
                    }
                    else
                    {
                        //No free slots for this upgrade
                        return false;
                    }
                }
            }
            return true;
        }

        public virtual bool IsAllowedForShip(GenericShip ship)
        {
            return true;
        }

        public virtual bool IsAllowedForSquadBuilder()
        {
            bool result = true;

            if (IsHidden) return false;

            if (FromMod != null && !ModsManager.Mods[FromMod].IsAvailable()) return false;
            //if (FromMod != null && FromMod.Count != 0 && !ModsManager.Mods[FromMod[1]].IsOn) return false;
            return result;
        }

        public virtual bool IsAllowedForSquadBuilderPostCheck(SquadList squadList)
        {
            return true;
        }

        public virtual void PreAttachToShip(GenericShip host)
        {
            HostShip = host;
            HostShip.UpgradeBar.InstalledUpgradesAll_System.Add(this);
            UpgradeInfo.InstallToShip(this);
        }

        public virtual void PreDettachFromShip()
        {
            HostShip.UpgradeBar.InstalledUpgradesAll_System.Remove(this);
            UpgradeInfo.RemoveFromShip();
        }

        /**
         * Checks if this upgrade is of the specified type.
         * @param type the type of upgrade to test.
         * @return true if this upgrade contains the specified type and false otherwise.
         */
        public bool HasType(UpgradeType type){
            for (int i = 0; i < UpgradeInfo.UpgradeTypes.Count; i++) {
                if (UpgradeInfo.UpgradeTypes[i] == type) {
                    return true;
                }
            }
            return false;
        }

        /**
         * Returns the type as a string.
         * @return the name of the type.
         */
        public string getTypesAsString(){
            string name = "";
            /*
            for (int i = 0; i < Types.Count; i++) {
                UpgradeType type = Types [i];
                if (i > 0) {
                    name += " ";
                }

                switch (type) {
                    case UpgradeType.SalvagedAstromech:
                        name += "Salvaged Astromech";
                        break;
                    default:
                        name += type.ToString ();
                        break;
                }
            }
            */
            UpgradeType type = UpgradeInfo.UpgradeTypes[0];
            switch (type) {
                case UpgradeType.SalvagedAstromech:
                    name += "Salvaged Astromech";
                    break;
                default:
                    name += type.ToString ();
                    break;
            }
            return name;
        }

        // ATTACH TO SHIP

        public virtual void AttachToShip(GenericShip host)
        {
            HostShip = host;
            State = new UpgradeCardState(this);
            ActivateAbility();
        }

        public void ActivateAbility()
        {
            foreach (var ability in UpgradeAbilities)
            {
                ability.ActivateAbility();
            }
        }

        public void DeactivateAbility()
        {
            foreach (var ability in UpgradeAbilities)
            {
                ability.DeactivateAbility();
            }
        }

        // DISCARD

        public void TryDiscard(Action callBack)
        {
            CurrentUpgrade = this;
            HostShip.CallDiscardUpgrade(delegate { AfterTriedDiscard(callBack); });
        }

        private void AfterTriedDiscard(Action callBack)
        {
            if (CurrentUpgrade != null)
            {
                Discard(callBack);
            }
            else
            {
                callBack();
            }
        }

        public virtual void Discard(Action callBack)
        {
            State.Flip(false);
            PreDettachFromShip();
            Roster.ShowUpgradeAsInactive(HostShip, UpgradeInfo.Name);
            DeactivateAbility();

            HostShip.CallAfterDiscardUpgrade(this, callBack);
        }

        // FLIP FACEUP

        public void TryFlipFaceUp(Action callBack)
        {
            CurrentUpgrade = this;
            HostShip.CallFlipFaceUpUpgrade(() => AfterTriedFlipFaceUp(callBack));
        }

        private void AfterTriedFlipFaceUp(Action callBack)
        {
            if (CurrentUpgrade != null)
            {
                FlipFaceup(callBack);
            }
            else
            {
                callBack();
            }
        }

        public virtual void FlipFaceup(Action callback)
        {
            State.Flip(true);
            Roster.ShowUpgradeAsActive(HostShip, UpgradeInfo.Name);
            ActivateAbility();

            Messages.ShowInfo(UpgradeInfo.Name + " is flipped face-up");
            HostShip.CallAfterFlipFaceUpUpgrade(this, callback);
        }

        public void ReplaceUpgradeBy(GenericUpgrade newUpgrade)
        {
            Roster.ReplaceUpgrade(HostShip, UpgradeInfo.Name, newUpgrade.UpgradeInfo.Name, newUpgrade.ImageUrl);

            Slot.PreInstallUpgrade(newUpgrade, HostShip);
            Slot.TryInstallUpgrade(newUpgrade, HostShip);
        }

        // Default templates to drop devices

        public virtual List<ManeuverTemplate> GetDefaultDropTemplates()
        {
            return new List<ManeuverTemplate>()
            {
                new ManeuverTemplate(ManeuverBearing.Straight, ManeuverDirection.Forward, ManeuverSpeed.Speed1, isBombTemplate: true)
            };
        }

        public virtual List<ManeuverTemplate> GetDefaultLaunchTemplates()
        {
            return new List<ManeuverTemplate>();
        }
    }

}

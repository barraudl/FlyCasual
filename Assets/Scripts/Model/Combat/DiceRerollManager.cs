﻿using GameModes;
using Ship;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public partial class DiceRerollManager
{
    public static DiceRerollManager CurrentDiceRerollManager;

    public static event GenericShip.EventHandlerInt OnMaxDiceRerollAllowed;

    public List<DieSide> SidesCanBeRerolled;
    public int NumberOfDiceCanBeRerolled = int.MaxValue;
    public bool IsOpposite;
    public bool IsTrueReroll = true;
    public bool IsForcedFullReroll = false;

    public System.Action CallBack;

    public DiceRerollManager()
    {
        CurrentDiceRerollManager = this;
    }

    public void Start()
    {
        OrganizeDiceView();
        CheckParameters();
        SwitchToDiceRerollsPanel();
        DoDefaultSelection();
        GenerateSelectionButtons();
        StartPlayerInteraction();
    }

    private void OrganizeDiceView()
    {
        Combat.CurrentDiceRoll.OrganizeDicePositions();
        Combat.CurrentDiceRoll.ToggleRerolledLocks(true);
    }

    private void CheckParameters()
    {
        if (IsTrueReroll)
        {
            if (OnMaxDiceRerollAllowed != null) OnMaxDiceRerollAllowed(ref NumberOfDiceCanBeRerolled);
        }

        if (SidesCanBeRerolled == null)
        {
            SidesCanBeRerolled = new List<DieSide>
            {
                DieSide.Blank,
                DieSide.Focus,
                DieSide.Success,
                DieSide.Crit
            };
        }
    }

    private void SwitchToDiceRerollsPanel(bool isReverse = false)
    {
        if (Selection.ActiveShip.Owner.GetType() == typeof(Players.HumanPlayer))
        {
            ToggleDiceModificationsPanel(isReverse);
            ToggleDiceRerollsPanel(!isReverse);
        }
    }

    private void DoDefaultSelection()
    {
        if (!IsForcedFullReroll)
        {
            if (!IsOpposite)
            {
                DoDefaultSelectionOwnDice();
            }
            else
            {
                DoDefaultSelectionOppositeDice();
            }
        }
        else
        {
            DoDefaultSelectionAll();
        }
    }

    public void DoDefaultSelectionAll()
    {
        Combat.CurrentDiceRoll.SelectAll();
    }

    private void DoDefaultSelectionOwnDice()
    {
        List<DieSide> dieSides = new List<DieSide>();

        if (SidesCanBeRerolled.Contains(DieSide.Blank))
        {
            dieSides.Add(DieSide.Blank);
        }

        if (SidesCanBeRerolled.Contains(DieSide.Focus))
        {
            //if (!Selection.ActiveShip.HasToken(typeof(Tokens.FocusToken)))
            if (Combat.Attacker.GetDiceModificationsGenerated().Count(n => n.IsTurnsAllFocusIntoSuccess) == 0)
            {
                dieSides.Add(DieSide.Focus);
            }
        }

        Combat.CurrentDiceRoll.SelectBySides(dieSides, NumberOfDiceCanBeRerolled);
    }

    private void DoDefaultSelectionOppositeDice()
    {
        List<DieSide> dieSides = new List<DieSide>();

        if (SidesCanBeRerolled.Contains(DieSide.Crit))
        {
            dieSides.Add(DieSide.Crit);
        }

        if (SidesCanBeRerolled.Contains(DieSide.Success))
        {
            dieSides.Add(DieSide.Success);
        }

        if (SidesCanBeRerolled.Contains(DieSide.Focus))
        {
            //if (!Selection.ActiveShip.HasToken(typeof(Tokens.FocusToken)))
            if ((Combat.Attacker.GetDiceModificationsGenerated().Count(n => n.IsTurnsAllFocusIntoSuccess) > 0))
            {
                dieSides.Add(DieSide.Focus);
            }
        }

        Combat.CurrentDiceRoll.SelectBySides(dieSides, NumberOfDiceCanBeRerolled);
    }

    private void GenerateSelectionButtons()
    {
        if (Selection.ActiveShip.Owner.GetType() == typeof(Players.HumanPlayer))
        {
            Dictionary<string, List<DieSide>> options = new Dictionary<string, List<DieSide>>();

            if (SidesCanBeRerolled.Contains(DieSide.Blank))
            {
                options.Add(
                    "Select only blanks",
                    new List<DieSide>() {
                    DieSide.Blank
                    });
            }

            if ((SidesCanBeRerolled.Contains(DieSide.Focus)) && (SidesCanBeRerolled.Contains(DieSide.Blank)) && (NumberOfDiceCanBeRerolled > 1))
            {
                options.Add(
                    "Select only blanks and focuses",
                    new List<DieSide>() {
                    DieSide.Blank,
                    DieSide.Focus
                    });
            }

            int offset = 0;
            foreach (var option in options)
            {
                GameObject prefab = (GameObject)Resources.Load("Prefabs/GenericButton", typeof(GameObject));
                GameObject newButton = MonoBehaviour.Instantiate(prefab, GameObject.Find("UI/CombatDiceResultsPanel").transform.Find("DiceRerollsPanel"));
                newButton.name = "Button" + option.Key;
                newButton.transform.GetComponentInChildren<Text>().text = option.Key;
                newButton.GetComponent<RectTransform>().localPosition = new Vector3(0, -offset, 0);
                newButton.GetComponent<Button>().onClick.AddListener(delegate
                {
                    SelectDiceByFilter(option.Value, NumberOfDiceCanBeRerolled);
                });
                newButton.SetActive(true);
                offset += 65;
            }
        }
    }

    private void SelectDiceByFilter(List<DieSide> dieSides, int number)
    {
        Combat.CurrentDiceRoll.SelectBySides(dieSides, number);
    }

    private void StartPlayerInteraction()
    {
        if (!IsForcedFullReroll)
        {
            Selection.ActiveShip.Owner.RerollManagerIsPrepared();
        }
        else
        {
            Phases.CurrentSubPhase.IsReadyForCommands = true;
            ConfirmRerollButtonIsPressed();
        }
        
    }

    public void ShowConfirmButton()
    {
        Button closeButton = GameObject.Find("UI/CombatDiceResultsPanel").transform.Find("DiceRerollsPanel/Confirm").GetComponent<Button>();
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(ConfirmRerollButtonIsPressed);
        closeButton.gameObject.SetActive(true);
    }

    private void ToggleDiceModificationsPanel(bool isActive)
    {
        GameObject.Find("UI/CombatDiceResultsPanel").transform.Find("DiceModificationsPanel").gameObject.SetActive(isActive);

        if (!isActive) Combat.HideDiceModificationButtons();
    }

    private void ToggleDiceRerollsPanel(bool isActive)
    {
        GameObject.Find("UI/CombatDiceResultsPanel").transform.Find("DiceRerollsPanel").gameObject.SetActive(isActive);

        if (!isActive)
        {
            foreach (Transform button in GameObject.Find("UI/CombatDiceResultsPanel").transform.Find("DiceRerollsPanel"))
            {
                if (button.name.StartsWith("Button"))
                {
                    MonoBehaviour.Destroy(button.gameObject);
                }
            }
        }
    }

    public void ConfirmRerollButtonIsPressed()
    {
        Roster.GetPlayer(Phases.CurrentSubPhase.RequiredPlayer).SyncDiceRerollSelected();
    }

    public static void SyncDiceRerollSelected(List<bool> selectedDice)
    {
        Phases.CurrentSubPhase.IsReadyForCommands = false;

        for (int i = 0; i < DiceRoll.CurrentDiceRoll.DiceList.Count; i++)
        {
            DiceRoll.CurrentDiceRoll.DiceList[i].ToggleSelected(selectedDice[i]);
        }

        CurrentDiceRerollManager.ConfirmReroll();
    }

    public void ConfirmReroll()
    {
        Selection.ThisShip.CallRerollIsConfirmed(RerollSelected);
    }

    private void RerollSelected()
    {
        if (Selection.ActiveShip.Owner.GetType() == typeof(Players.HumanPlayer)) BlockButtons();
        Combat.CurrentDiceRoll.RerollSelected(ImmediatelyAfterReRolling);
    }

    private void ImmediatelyAfterReRolling(DiceRoll diceroll)
    {
        Selection.ActiveShip.CallOnImmediatelyAfterReRolling(diceroll, delegate { TryUnblockButtons(diceroll); });
    }

    public List<Die> GetDiceReadyForReroll()
    {
        List<Die> diceReadyForReroll = new List<Die>();

        foreach (var die in Combat.CurrentDiceRoll.DiceList)
        {
            if (die.IsSelected) diceReadyForReroll.Add(die);
        }

        return diceReadyForReroll;
    }

    private void BlockButtons()
    {
        ToggleDiceRerollsPanel(false);
    }

    private void TryUnblockButtons(DiceRoll diceRoll)
    {
        UnblockButtons();
    }

    public void UnblockButtons()
    {
        if (!IsTrueReroll) MarkAsFakeReroll();

        DiceRerollManager.CurrentDiceRerollManager = null;

        Combat.CurrentDiceRoll.ToggleRerolledLocks(false);
        if (Selection.ActiveShip.Owner.GetType() == typeof(Players.HumanPlayer)) ToggleDiceModificationsPanel(true);

        if (CallBack!=null) CallBack();
    }

    private void MarkAsFakeReroll()
    {
        foreach (var die in DiceRoll.CurrentDiceRoll.DiceList)
        {
            die.IsRerolled = false;
        }
    }

}

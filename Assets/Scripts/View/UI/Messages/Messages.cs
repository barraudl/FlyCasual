﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum MessageType
{
    Error,
    Info
}

public static class Messages{

    private static List<GameObject> allMessages = new List<GameObject>();

    public static void ShowError(string text, bool allowCopies = false)
    {
        ShowMessage(text, MessageType.Error);
    }

    public static void ShowErrorToHuman(string text, bool allowCopies = false)
    {
        if (Roster.GetPlayer(Phases.CurrentPhasePlayer).GetType() == typeof(Players.HumanPlayer))
        {
            ShowMessage(text, MessageType.Error, allowCopies);
        }
    }

    public static void ShowInfo(string text, bool allowCopies = false)
    {
        ShowMessage(text, MessageType.Info, allowCopies);
    }

    public static void ShowInfoToHuman(string text, bool allowCopies = false)
    {
        if (Roster.GetPlayer(Phases.CurrentPhasePlayer).GetType() == typeof(Players.HumanPlayer))
        {
            ShowMessage(text, MessageType.Info, allowCopies);
        }
    }

    private static void ShowMessage(string text, MessageType type, bool allowCopies = false)
    {
        if (!allowCopies)
        {
            if (allMessages.LastOrDefault() != null && allMessages.LastOrDefault().name == text) return;
        }

        Vector2 startingPosition = new Vector3(0, -75, 0);

        if (allMessages.Count != 0 && allMessages.Last() != null)
        {
            startingPosition = allMessages.Last().transform.localPosition + new Vector3(0, -85, 0);
        }

        foreach (var message in allMessages)
        {
            if (message != null)
            {
                message.GetComponent<MessageContainer>().ShiftTargetPosition();
            }
        }

        GameObject prefab = (GameObject)Resources.Load("Prefabs/MessagePanel", typeof(GameObject));
        GameObject MessagePanelsHolder = GameObject.Find("UI/MessagesContainer/MessagePanels");
        GameObject Message = MonoBehaviour.Instantiate(prefab, MessagePanelsHolder.transform);
        Message.transform.localPosition = startingPosition;
        Message.name = text;
        Message.GetComponent<MessageContainer>().Initialize(text, type);
        allMessages.Add(Message);
    }

}

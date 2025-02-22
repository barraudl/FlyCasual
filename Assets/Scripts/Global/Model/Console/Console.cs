﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Reflection;
using System;
using UnityEngine.Analytics;
using UnityEngine.Networking;
using SquadBuilderNS;

public enum LogTypes
{
    Everything,
    Errors,
    GameCommands,
    Triggers,
    AI,
    Network
}

public partial class Console : MonoBehaviour {

    public class LogEntry
    {
        public string Text;
        public LogTypes Type;
        public float CalculatedPrefferedHeight;

        public LogEntry(string text, LogTypes logType)
        {
            Text = text;
            Type = logType;
        }
    }

    private static List<LogEntry> logs;
    public static List<LogEntry> Logs
    {
        get { return logs; }
        private set { logs = value; }
    }

    private static LogTypes currentLogTypeToShow;
    private static List<LogTypes> logsList = new List<LogTypes>() { LogTypes.Everything, LogTypes.Errors, LogTypes.Triggers, LogTypes.AI, LogTypes.Network };

    private static Dictionary<string, GenericCommand> availableCommands;
    public static Dictionary<string, GenericCommand> AvailableCommands
    {
        get { return availableCommands; }
        private set { availableCommands = value; }
    }


    private void Start()
    {
        Application.logMessageReceived += ProcessUnityLog;

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        AvailableCommands = new Dictionary<string, GenericCommand>();

        List<Type> typelist = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => String.Equals(t.Namespace, "CommandsList", StringComparison.Ordinal))
            .ToList();

        foreach (var type in typelist)
        {
            if (type.MemberType == MemberTypes.NestedType) continue;
            System.Activator.CreateInstance(type);
        }

        AvailableCommands = AvailableCommands.OrderBy(n => n.Key).ToDictionary(n => n.Key, n => n.Value);
    }

    private static void InitializeLogs()
    {
        Logs = new List<LogEntry>();
        currentLogTypeToShow = LogTypes.Everything;
    }

    public static void Write(string text, LogTypes logType = LogTypes.Everything, bool isBold = false, string color = "")
    {
        if (Logs == null) InitializeLogs();

        string logString = text;
        if (isBold) logString = "<b>" + logString + "</b>";
        if (color != "") logString = "<color="+ color + ">" + logString + "</color>";

        LogEntry logEntry = new LogEntry(logString + "\n", logType);
        Logs.Add(logEntry);

        if (currentLogTypeToShow == logType || currentLogTypeToShow == LogTypes.Everything)
        {
            ShowLogEntry(logEntry);
        }
    }

    private void ProcessUnityLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception)
        {
            if (IsHiddenError(logString)) return;

            if (!DebugManager.ErrorIsAlreadyReported)
            {
                //if (DebugManager.ReleaseVersion && Global.CurrentVersionInt == Global.LatestVersionInt) { }
                SendReport(stackTrace);
            }

            IsActive = true;
            Write("\n" + logString + "\n\n" + stackTrace, LogTypes.Errors, true, "red");
        }
    }

    private void SendReport(string stackTrace)
    {
        DebugManager.ErrorIsAlreadyReported = true;

        AnalyticsEvent.LevelFail(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            new Dictionary<string, object>()
            {
                { "Version", Global.CurrentVersion },
                { "Pilot", (Selection.ThisShip != null) ? Selection.ThisShip.PilotInfo.PilotName : "None" },
                { "Trigger", (Triggers.CurrentTrigger != null) ? Triggers.CurrentTrigger.Name : "None" },
                { "Subphase", (Phases.CurrentSubPhase != null) ? Phases.CurrentSubPhase.GetType().ToString() : "None" }
            }
        );

        StartCoroutine(UploadCustomReport(stackTrace));
    }

    IEnumerator UploadCustomReport(string stackTrace)
    {
        JSONObject jsonData = new JSONObject();
        jsonData.AddField("name", Options.NickName);
        jsonData.AddField("description", "No description");
        jsonData.AddField("p1pilot", (Selection.ThisShip != null) ? Selection.ThisShip.PilotInfo.PilotName : "None");
        jsonData.AddField("p2pilot", (Selection.AnotherShip != null) ? Selection.AnotherShip.PilotInfo.PilotName : "None");
        jsonData.AddField("stacktrace", stackTrace);
        jsonData.AddField("trigger", (Triggers.CurrentTrigger != null) ? Triggers.CurrentTrigger.Name : "None");
        jsonData.AddField("subphase", (Phases.CurrentSubPhase != null) ? Phases.CurrentSubPhase.GetType().ToString() : "None");
        jsonData.AddField("scene", UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        jsonData.AddField("version", Global.CurrentVersion);

        try
        {
            jsonData.AddField("p1squad", SquadBuilder.SquadLists[0].SavedConfiguration.ToString().Replace("\"", "\\\""));
            jsonData.AddField("p2squad", SquadBuilder.SquadLists[1].SavedConfiguration.ToString().Replace("\"", "\\\""));
        }
        catch (Exception)
        {
            jsonData.AddField("p1squad", "None");
            jsonData.AddField("p2squad", "None");
        }

        var request = new UnityWebRequest("http://flycasual.azurewebsites.net/api/errorreport", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData.ToString());
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
        Debug.Log("Status Code: " + request.responseCode);
    }

    private bool IsHiddenError(string text)
    {
        if ((text == "ClientDisconnected due to error: Timeout") ||
            (text == "ServerDisconnected due to error: Timeout") ||
            text.StartsWith("Screen position out of view frustum")) return true;

        return false;
    }

    public static void ProcessCommand(string inputText)
    {
        if (string.IsNullOrEmpty(inputText)) return;

        List<string> blocks = inputText.ToLower().Split(' ').ToList();
        string keyword = blocks.FirstOrDefault();
        blocks.RemoveAt(0);

        Dictionary<string, string> parameters = new Dictionary<string, string>();
        foreach (var item in blocks)
        {
            string[] paramValue = item.Split(':');
            if (paramValue.Length == 2) parameters.Add(paramValue[0], paramValue[1]);
            else if (paramValue.Length == 1) parameters.Add(paramValue[0], null);
        }

        if (AvailableCommands.ContainsKey(keyword))
        {
            AvailableCommands[keyword].Execute(parameters);
        }
        else
        {
            Console.Write("Unknown command, enter \"help\" to see list of commands", LogTypes.Everything, false, "red");
        }
    }

    public static void AddAvailableCommand(GenericCommand command)
    {
        AvailableCommands.Add(command.Keyword, command);
    }

}

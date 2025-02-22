﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mods;
using UnityEngine.UI;
using SquadBuilderNS;
using System.Linq;
using Players;
using System.Reflection;
using System;
using Upgrade;
using Migrations;
using ExtraOptions;
using Obstacles;

public partial class MainMenu : MonoBehaviour {

    string NewVersionUrl;

    public static MainMenu CurrentMainMenu;

    private Faction CurrentAvatarsFaction = Faction.Rebel;

    // Use this for initialization
    void Start ()
    {
        MigrationsManager.PerformMigrations();
        InitializeMenu();
    }

    private void InitializeMenu()
    {
        CurrentMainMenu = this;
        SetCurrentPanel();

        DontDestroyOnLoad(GameObject.Find("GlobalUI").gameObject);

        ModsManager.Initialize();
        Options.ReadOptions();
        Options.UpdateVolume();
        ExtraOptionsManager.Initialize();
        SetBackground();
        UpdateVersionInfo();
        UpdatePlayerInfo();

        PrepareUpdateChecker();

        new ObstaclesManager();
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void OnUpdateAvailableClick()
    {
        Application.OpenURL(NewVersionUrl);
    }

    private void UpdateVersionInfo()
    {
        GameObject.Find("UI/Panels/MainMenuPanel/Background/Version/Version Text").GetComponent<Text>().text = Global.CurrentVersion;
    }

    private void UpdatePlayerInfo()
    {
        AvatarFromUpgrade script = GameObject.Find("UI/Panels/MainMenuPanel/PlayerInfoPanel/AvatarImage").GetComponent<AvatarFromUpgrade>();
        script.Initialize(Options.Avatar);

        GameObject.Find("UI/Panels/MainMenuPanel/PlayerInfoPanel/NicknameAndTitleText").GetComponent<Text>().text = Options.NickName + "\n" + Options.Title;
    }

    public static void SetBackground()
    {
        Sprite background = (Options.BackgroundImage != "_RANDOM") ? Resources.Load<Sprite>("Sprites/Backgrounds/MainMenu/" + Options.BackgroundImage) : GetRandomMenuBackground();
        GameObject.Find("UI/BackgroundImage").GetComponent<Image>().sprite = background;
    }

    public static Sprite GetRandomMenuBackground()
    {
        List<Sprite> spritesList = Resources.LoadAll<Sprite>("Sprites/Backgrounds/MainMenu/")
            .Where(n => n.name != "_RANDOM")
            .ToList();
        return spritesList[UnityEngine.Random.Range(0, spritesList.Count)];
    }

    public static Sprite GetRandomSplashScreen()
    {
        List<Sprite> spritesArray = new List<Sprite>();
        spritesArray.AddRange(
            Resources.LoadAll<Sprite>("Sprites/Backgrounds/MainMenu/")
                .Where(n => n.name != "_RANDOM")
                .ToList()
        );
        spritesArray.AddRange(
            Resources.LoadAll<Sprite>("Sprites/Backgrounds/SplashScreens/")
                .ToList()
        );
        return spritesArray[UnityEngine.Random.Range(0, spritesArray.Count)];
    }

    private void PrepareUpdateChecker()
    {
        RemoteSettings.Completed += CheckUpdateNotification;
        RemoteSettings.ForceUpdate();
    }

    private void CheckUpdateNotification(bool wasUpdatedFromServer, bool settingsChanged, int serverResponse)
    {
        Global.LatestVersionInt = RemoteSettings.GetInt("UpdateLatestVersionInt", Global.CurrentVersionInt);
        if (Global.LatestVersionInt > Global.CurrentVersionInt)
        {
            string latestVersion    = RemoteSettings.GetString("UpdateLatestVersion", Global.CurrentVersion);
            string updateLink       = RemoteSettings.GetString("UpdateLink");
            ShowNewVersionIsAvailable(latestVersion, updateLink);
        }

        RemoteSettings.Completed -= CheckUpdateNotification;
    }

    // 0.3.2 UI

    public void CreateMatch()
    {
        string roomName = GameObject.Find("UI/Panels/CreateMatchPanel/Panel/Name").GetComponentInChildren<InputField>().text;
        string password = GameObject.Find("UI/Panels/CreateMatchPanel/Panel/Password").GetComponentInChildren<InputField>().text;

        GameController.Initialize();
        ReplaysManager.Initialize(ReplaysMode.Write);
        Console.Write("Network game is prepared", LogTypes.GameCommands, true, "aqua");

        Network.CreateMatch(roomName, password);
    }

    public void BrowseMatches()
    {
        Network.BrowseMatches();
    }

    public void JoinMatch(GameObject panel)
    {
        //Messages.ShowInfo("Joining room...");
        string password = panel.transform.Find("Password").GetComponentInChildren<InputField>().text;
        Network.JoinCurrentRoomByParameters(password);
    }

    public void CancelWaitingForOpponent()
    {
        Network.CancelWaitingForOpponent();
    }

    public void StartSquadBuilerMode(string modeName)
    {
        InitializeSquadBuilder(modeName);
        ChangePanel("SelectFactionPanel");
    }

    public void StartReplay()
    {
        GameController.StartBattle(ReplaysMode.Read);
    }

    public void InitializeSquadBuilder(string modeName)
    {
        SquadBuilder.Initialize();
        SquadBuilder.SetCurrentPlayer(PlayerNo.Player1);
        SquadBuilder.SetPlayers(modeName);
        SquadBuilder.SetDefaultPlayerNames();
    }

    public void InitializePlayerCustomization()
    {
        InitializeAvatars("Rebel");
        InitializeNickName();
        InitializeTitle();
    }

    private void InitializeNickName()
    {
        GameObject.Find("UI/Panels/AvatarsPanel/NickName/InputField").gameObject.GetComponent<InputField>().text = Options.NickName;
    }

    private void InitializeTitle()
    {
        GameObject.Find("UI/Panels/AvatarsPanel/Title/InputField").gameObject.GetComponent<InputField>().text = Options.Title;
    }

    public void InitializeAvatars(string factionString)
    {
        CurrentAvatarsFaction = (Faction) Enum.Parse(typeof(Faction), factionString);

        ClearAvatarsPanel();

        int count = 0;

        List<Type> typelist = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => String.Equals(t.Namespace, "UpgradesList.FirstEdition", StringComparison.Ordinal))
            .ToList();

        foreach (var type in typelist)
        {
            if (type.MemberType == MemberTypes.NestedType) continue;

            GenericUpgrade newUpgradeContainer = (GenericUpgrade)System.Activator.CreateInstance(type);
            if (newUpgradeContainer.UpgradeInfo.Name != null)
            {
                if (newUpgradeContainer.Avatar != null && newUpgradeContainer.Avatar.AvatarFaction == CurrentAvatarsFaction) AddAvailableAvatar(newUpgradeContainer, count++);
            }
        }
    }

    private void ClearAvatarsPanel()
    {
        GameObject avatarsPanel = GameObject.Find("UI/Panels/AvatarsPanel/ContentPanel").gameObject;
        foreach (Transform transform in avatarsPanel.transform)
        {
            GameObject.Destroy(transform.gameObject);
        }

        GameObject.Find("UI/Panels/AvatarsPanel/AvatarSelector").GetComponent<Image>().enabled = false;

        Resources.UnloadUnusedAssets();
    }

    private void AddAvailableAvatar(GenericUpgrade avatarUpgrade, int count)
    {
        GameObject prefab = (GameObject)Resources.Load("Prefabs/MainMenu/AvatarImage", typeof(GameObject));
        GameObject avatarPanel = MonoBehaviour.Instantiate(prefab, GameObject.Find("UI/Panels/AvatarsPanel/ContentPanel").transform);

        int row = count / 8;
        int column = count - row * 8;

        avatarPanel.transform.localPosition = new Vector2(20 + column * 120, -20 - row * 110);
        avatarPanel.name = avatarUpgrade.GetType().ToString();

        AvatarFromUpgrade avatar = avatarPanel.GetComponent<AvatarFromUpgrade>();
        avatar.Initialize(avatarUpgrade.GetType().ToString(), ChangeAvatar);

        if (avatarUpgrade.GetType().ToString() == Options.Avatar)
        {
            SetAvatarSelected(avatarPanel.transform.position);
        }
    }

    private void ChangeAvatar(string avatarName)
    {
        Options.Avatar = avatarName;
        Options.ChangeParameterValue("Avatar", avatarName);

        SetAvatarSelected(GameObject.Find("UI/Panels/AvatarsPanel/ContentPanel/" + avatarName).transform.position);
    }

    public void SetAvatarSelected(Vector3 position)
    {
        GameObject selector = GameObject.Find("UI/Panels/AvatarsPanel/AvatarSelector");
        selector.GetComponent<Image>().enabled = true;
        selector.transform.position = position;
    }

    public void ChangeNickName(string text)
    {
        Options.NickName = text;
        Options.ChangeParameterValue("NickName", text);
    }

    public void ChangeTitle(Text inputText)
    {
        Options.Title = inputText.text;
        Options.ChangeParameterValue("Title", inputText.text);
    }

    public static void ResetAiInformation()
    {
        GameObject.Find("GlobalUI/OpponentSquad").transform.Find("AiInformation").gameObject.SetActive(false);
        GameObject.Find("GlobalUI/OpponentSquad").transform.Find("StartPanel").gameObject.SetActive(false);

        GameObject.Find("GlobalUI/OpponentSquad/LoadingInfoPanel").gameObject.SetActive(true);
    }

    public static void ShowAiInformation()
    {
        if ((SquadBuilder.GetSquadList(PlayerNo.Player2).PlayerType == typeof(HotacAiPlayer)) && (!Options.DontShowAiInfo))
        {
            GameObject.Find("GlobalUI/OpponentSquad").transform.Find("AiInformation").gameObject.SetActive(true);
            GameObject.Find("GlobalUI/OpponentSquad/AiInformation").transform.Find("ToggleBlock").gameObject.SetActive(false);
        }
    }

    public static void ShowAiInformationContinue()
    {
        GameObject.Find("GlobalUI/OpponentSquad/LoadingInfoPanel").gameObject.SetActive(false);

        GameObject.Find("GlobalUI/OpponentSquad/AiInformation").transform.Find("ToggleBlock").gameObject.SetActive(true);
        GameObject.Find("GlobalUI/OpponentSquad").transform.Find("StartPanel").gameObject.SetActive(true);

        Button startButton = GameObject.Find("GlobalUI/OpponentSquad/StartPanel/StartButton").GetComponent<Button>();
        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(delegate
        {
            Options.DontShowAiInfo = true;
            Options.ChangeParameterValue("DontShowAiInfo", GameObject.Find("GlobalUI/OpponentSquad/AiInformation/ToggleBlock/DontShowAgain").GetComponent<Toggle>().isOn);
            Global.StartBattle();
        });
    }

    public static void ScalePanel(Transform panelTransform, float maxScale = float.MaxValue, bool twoBorders = false)
    {
        float bordersSize = (twoBorders) ? 250f : 125f;
        float globalUiScale = GameObject.Find("UI").GetComponent<RectTransform>().localScale.y;

        float scaleX = Screen.width / panelTransform.GetComponent<RectTransform>().sizeDelta.x / globalUiScale;
        float scaleY = (Screen.height - bordersSize * globalUiScale) / panelTransform.GetComponent<RectTransform>().sizeDelta.y / globalUiScale;
        float scale = Mathf.Min(scaleX, scaleY);
        scale = Mathf.Min(scale, maxScale);
        panelTransform.localScale = new Vector3(scale, scale, scale);
    }
}

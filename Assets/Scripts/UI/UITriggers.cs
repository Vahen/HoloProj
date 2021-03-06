﻿using HoloToolkit.Unity;
using System;
using UnityEngine;

public class UITriggers : Singleton<UITriggers> {

    public GameObject menuFull = null;
    public GameObject modelsMenu = null;
    public GameObject mainMenu = null;
    public GameObject exitConfirmMenu = null;
    public GameObject gltf = null;
    public GameObject aiguille = null;
    public GameObject debugText = null;
    public GameObject cursorVisual = null;

    bool isMenuShowing = false;
    bool isDebugTextShowing = false;
    bool isCursorVisualShowing = true;
    bool wasIpConfiguratorShowing = false;

    public GameObject startMenu = null;
    public GameObject ipConfigurator = null;
    public GameObject ipButton = null;
    public GameObject startAppButton = null;
    public GameObject startFileTransferServiceButton = null;
    public GameObject useCalibrationAutoButton = null;

    public GameObject calibrationCubes = null;
    private GameObject axisViewer = null;
    private bool fileTransferStarted = false;
    private bool enableAutomaticCalibration = false;

    // Use this for initialization
    new void Awake() {
        if (menuFull == null) {
            menuFull = GameObject.Find("MenuItems");
        }
        if (modelsMenu == null) {
            modelsMenu = GameObject.Find("ModelsMenu");
        }
        if (mainMenu == null) {
            mainMenu = GameObject.Find("MainMenu");
        }

        if (exitConfirmMenu == null) {
            exitConfirmMenu = GameObject.Find("ExitConfirmMenu");
        }

        if (gltf == null) {
            gltf = GameObject.Find("GLTF");
        }
        if (aiguille == null) {
            aiguille = GameObject.Find("Aiguille");
        }
        if (debugText == null) {
            debugText = GameObject.Find("UITextPrefab");
        }

        if (cursorVisual == null) {
            cursorVisual = GameObject.Find("CursorVisual");
        }

        if (startMenu == null) {
            startMenu = GameObject.Find("StartMenu");
        }
        if (ipConfigurator == null) {
            ipConfigurator = GameObject.Find("IpConfigurator");
        }

        if (ipButton == null) {
            ipButton = GameObject.Find("IPButton");
        }
        if (startAppButton == null) {
            startAppButton = GameObject.Find("StartAppButton");
        }
        if (startFileTransferServiceButton == null) {
            startFileTransferServiceButton = GameObject.Find("StartFileTransferService");
        }

        if (calibrationCubes == null) {
            calibrationCubes = GameObject.Find("CalibrationCubes");
        }

        if (axisViewer == null) {
            axisViewer = GameObject.Find("AxisViewer");
        }

        if (useCalibrationAutoButton == null) {
            useCalibrationAutoButton = GameObject.Find("UseCalibrationAutoButton");
        }

        calibrationCubes.SetActive(false);
        menuFull.SetActive(isMenuShowing);
        //modelsMenu.SetActive(false);
        mainMenu.SetActive(true);
        exitConfirmMenu.SetActive(false);
        debugText.SetActive(false);
        ipConfigurator.SetActive(false);
    }

    // Update is called once per frame
    void Update() {
        // Eventuellement se servir du pad xbox pour afficher / cacher le menu, via le bouton start ou autre
        if (Input.GetKeyDown("escape")) {
            ShowHideMenu();

        }
        // Affiche le texte de debug
        if (Input.GetKeyDown("p")) {
            isDebugTextShowing = !isDebugTextShowing;
            debugText.SetActive(isDebugTextShowing);
        }

        // Cache le curseur
        if (Input.GetKeyDown("n")) {
            isCursorVisualShowing = !isCursorVisualShowing;
            cursorVisual.SetActive(isCursorVisualShowing);
        }
    }

    public void ShowHideMenu() {
        isMenuShowing = !isMenuShowing;
        menuFull.SetActive(isMenuShowing);
        if (isMenuShowing && ipConfigurator.activeInHierarchy) {
            wasIpConfiguratorShowing = true;
            HideIpConfigurator();
        }
        else if (!isMenuShowing && wasIpConfiguratorShowing) {
            ShowIpConfigurator();
        }
    }

    // Affiche le menu des modeles 3D et cache le menu principal
    public void Show3DModelsMenu() {
        modelsMenu.SetActive(true);
        HideMainMenu();
    }

    // Cache le menu des modeles 3D
    public void Hide3DModelsMenu() {
        modelsMenu.SetActive(false);
    }

    // Affiche le menu principal et cache le menu des modeles 3D
    public void ShowMainMenu() {
        mainMenu.SetActive(true);
        Hide3DModelsMenu();
    }

    // Cache le menu principal
    public void HideMainMenu() {
        mainMenu.SetActive(false);
    }

    /// <summary>
    /// Supprime l'objet gltf de la fenetre pour pouvoir en mettre un nouveau
    /// </summary>
    public void CleanWindowFromModels() {
        foreach (Transform child in gltf.transform) {
            GameObject.Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// Permet d'inverser les axes sur le capteur de position 3D
    /// </summary>
    public void InvertAxesValues() {
        UpdatePosOrientAiguille updPO = aiguille.GetComponent<UpdatePosOrientAiguille>();
        updPO.invertCoef();
    }

    public void ExitProgram() {
#if UNITY_EDITOR
        // Application.Quit() does not work in the editor so
        // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void NoExit() {
        exitConfirmMenu.SetActive(false);
    }

    public void DisplayExitConfirm() {
        exitConfirmMenu.SetActive(true);
    }

    /// <summary>
    /// Affiche l'ip configurator et cache le menu de départ
    /// </summary>
    public void ShowIpConfigurator() {
        HideStartMenu();
        ipConfigurator.SetActive(true);
    }

    /// <summary>
    /// Cache l'ip configurator et affiche le menu de départ
    /// </summary>
    public void CancelIpConfig() {
        ShowStartMenu();
        ipConfigurator.SetActive(false);
    }

    /// <summary>
    /// Cache l'ip configurator
    /// </summary>
    public void HideIpConfigurator() {
        ipConfigurator.SetActive(false);
    }

    /// <summary>
    /// Cache le menu de demarrage
    /// </summary>
    public void HideStartMenu() {
        startMenu.SetActive(false);
    }

    /// <summary>
    /// Affiche le menu de demarrage
    /// </summary>
    public void ShowStartMenu() {
        startMenu.SetActive(true);
    }

    /// <summary>
    /// Démarre l'application
    /// </summary>
    public void StartApp() {
        IpConfiguratorTriggers ipConfiguratorTriggers = ipConfigurator.GetComponent<IpConfiguratorTriggers>();
        AudioSource audio = startAppButton.GetComponent<AudioSource>();
        if (!ipConfiguratorTriggers.IsLoaded()) {
            audio.Play();
            Debug.Log("Ip config not loaded !!!");
            return;
        }

        if (!fileTransferStarted) {
            audio.Play();
            Debug.Log("File transfer not started");
            return;
        }
        string host = ipConfiguratorTriggers.GetIpAdress();
        string port = ipConfiguratorTriggers.GetPort();
        if (ReceiveAndWriteFile.Instance.CheckIfSameAdress(host, port)) {
            audio.Play();
            Debug.Log("Adress must be different");
            return;
        }

        HideStartMenu();


        PlStreamCustom.Instance.StartPlStreamCustom(host, port);

        if (enableAutomaticCalibration) {
            gltf.SetActive(false);
            axisViewer.SetActive(false);
            calibrationCubes.SetActive(true); // -> Les autres éléments sont affiché après le calibrage
        }
        else {
            // Si on n'utilise pas le calibrage automatique, on 
            gltf.SetActive(true);
            axisViewer.SetActive(true);
        }
    }
    /// <summary>
    /// Demarre le service de transfert de fichier
    /// </summary>
    public void StartFileTransferService() {
        IpConfiguratorTriggers ipConfiguratorTriggers = ipConfigurator.GetComponent<IpConfiguratorTriggers>();
        AudioSource audio = startAppButton.GetComponent<AudioSource>();
        if (!ipConfiguratorTriggers.IsLoaded()) {
            audio.Play();
            Debug.Log("Ip config not loaded !!!");
            return;
        }
        string host = ipConfiguratorTriggers.GetIpAdress();
        string port = ipConfiguratorTriggers.GetPort();
        ReceiveAndWriteFile.Instance.SetupHostAndPort(host, port);
        fileTransferStarted = true;

    }

    /// <summary>
    /// Permet de demander au serveur un dossier à mettre sur le casque
    /// </summary>
    public void FireFileTransferServiceUpdate() {
        ReceiveAndWriteFile.Instance.ConnectAndGetFile();
    }

    public void EnableAutomaticCalibration() {
        enableAutomaticCalibration = !enableAutomaticCalibration;
        Debug.Log("enableAutomaticCalibration:" + enableAutomaticCalibration);
    }

    /// <summary>
    /// Cache la scène
    /// </summary>
    private void HideScene() {
        axisViewer.SetActive(false);
        gltf.SetActive(false);
    }

    /// <summary>
    /// Montre la scène
    /// </summary>
    private void ShowScene() {
        axisViewer.SetActive(true);
        gltf.SetActive(true);
    }
}

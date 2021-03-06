﻿
using UnityEngine;
using System.Net.Sockets;
using System;
using System.Threading;
using System.IO;
using HoloToolkit.Unity;

#if !UNITY_EDITOR
using System.Threading.Tasks;
#endif

public enum PlTrackerCustom {
    Patriot,
};

public class PlStreamCustom : Singleton<PlStreamCustom> {
    // Ajout pour le client tcp UWP
    /////////////////////////////////////////////////////////////////////////////
#if !UNITY_EDITOR
    private bool _useUWP = true;
    private Windows.Networking.Sockets.StreamSocket socket;
    private Task exchangeTask = null;
    private Stream stream = null;
#endif

#if UNITY_EDITOR
    private bool _useUWP = false;
    private TcpClient tcpClient; // PC ajout
    private NetworkStream stream = null;
    private Thread conThread = null;
#endif
    /////////////////////////////////////////////////////////////////////////////


    // Partie de base dans le plstream

    public string host = "192.168.137.1"; // Attention l'adresse peut changer selon la machine et le port du dongle wifi
    // Prendre en parametre avec un fichier txt ou autre



    // TODO Pas moyen de passer via un argument de ligne de commande
    // Peut etre via un fichier

    //public string host = "169.254.97.91";
    /////////////////////////////////////////////////////////////////////////////

    // port used for our connection
    public string port = "5124"; // PC 5123 de base

    // tracker descriptors
    public PlTrackerCustom tracker_type = PlTrackerCustom.Patriot;
    public int max_systems = 1;
    public int max_sensors = 2;

    // slots used to store tracker output data
    public bool[] active;
    public uint[] digio;
    public Vector3[] positions;
    public Vector4[] orientations;

    public bool isActive = false;

    // internal state
    private int max_slots;

    private bool stopListening;
    /////////////////////////////////////////////////////////////////////////////

    // Use this for initialization
    void Start() {

    }


    /// <summary>
    /// Permet de lancer la reception des données de l'appareil vers le programme
    /// </summary>
    /// <param name="host"> Hote sur lequel se connecter </param>
    /// <param name="port"> Port de connection à la machine </param>
    public void StartPlStreamCustom(string host, string port) {
        Debug.Log("host : " + host + ":" + port);
        DebugHelper.Instance.AddDebugText("host : " + host + ":" + port, 8);
        Connect(host, port);
    }

    /// <summary>
    /// Fonction permettant la connection entre l'appareil de 3D et le programme
    /// Lance ConnectUWP si l'ont est sur l'hololens
    /// ConnectUnity si on lance le programme via unity
    /// </summary>
    /// <param name="host"> Hote sur lequel se connecter </param>
    /// <param name="port"> Port de connection à la machine </param>
    public void Connect(string host, string port) {
        if (_useUWP) {
            ConnectUWP(host, port);
        }
        else {
            ConnectUnity(host, port);
        }
    }

    void Initialize() {
        try {
            // there are some constraints between tracking systems
            switch (tracker_type) {
                case PlTrackerCustom.Patriot:
                    max_systems = (max_systems > 1) ? 1 : max_systems;
                    max_sensors = (max_sensors > 2) ? 2 : max_sensors;
                    break;
                default:
                    throw new Exception("[polhemus] Unknown Tracker selected in PlStream::Awake().");
            }

            // set the number of slots
            max_slots = max_sensors * max_systems;

            // allocate resources for those slots
            active = new bool[max_slots];
            digio = new uint[max_slots];
            positions = new Vector3[max_slots];
            orientations = new Vector4[max_slots];

            // initialize the slots
            for (int i = 0; i < max_slots; ++i) {
                active[i] = false;
                digio[i] = 0;
                positions[i] = Vector3.zero;
                orientations[i] = Vector4.zero;
            }

            switch (tracker_type) {
                case PlTrackerCustom.Patriot:
#if UNITY_EDITOR
                    conThread = new Thread(new ThreadStart(Read_liberty));
                    // start the read thread
                    isActive = true;
                    conThread.Start();
#else
                    // Partie hololens
                    isActive = true;
                    exchangeTask = Task.Run(() => Read_liberty());
#endif
                    break;
                default:
                    throw new Exception("[polhemus] Unknown Tracker selected in PlStream::Awake().");
            }
        }
        catch (Exception e) {
            Debug.Log(e);
            Debug.Log("[polhemus] PlStream terminated in PlStream::Awake().");
            Console.WriteLine("[polhemus] PlStream terminated in PlStream::Awake().");
        }
    }



#if UNITY_EDITOR
    private void ConnectUWP(string host, string port)
#else
    private async void ConnectUWP(string host, string port)
#endif
    {
#if UNITY_EDITOR
        Debug.Log("Can't use UWP TCP client in Unity!");
#else
        try {
            socket = new Windows.Networking.Sockets.StreamSocket();
            Windows.Networking.HostName serverHost = new Windows.Networking.HostName(host);
            await socket.ConnectAsync(serverHost, port);

            stream = socket.InputStream.AsStreamForRead();
            Initialize();
            Debug.Log("Connected!");
        }
        catch (Exception e) {
            Debug.Log(e.ToString());
        }
#endif
    }

    private void ConnectUnity(string host, string port) {
#if UNITY_EDITOR
        try {
            tcpClient = new System.Net.Sockets.TcpClient(host, Int32.Parse(port));
            stream = tcpClient.GetStream();
            Initialize();
            Debug.Log("Connected!");
        }
        catch (Exception e) {
            Debug.Log(e.ToString());
        }
#else
        Debug.Log("Can't use Unity TCP client in UWP!");
#endif
    }



    /// <summary>
    /// Lecture des informations du capteur
    /// </summary>
    private void Read_liberty() {
        stopListening = false;
        try {
            // create temp_active to mark slots
            bool[] temp_active = new bool[max_slots];

            while (!stopListening) {
                byte[] receiveBytes = new Byte[40];
                int length;
                while ((length = stream.Read(receiveBytes, 0, receiveBytes.Length)) != 0) {
                    var data = new Byte[length];
                    Array.Copy(receiveBytes, 0, data, 0, length);
                    // set slots to inactive
                    for (var i = 0; i < max_slots; ++i)
                        temp_active[i] = false;

                    // offset into buffer
                    int offset = 0;
                    while (offset + 40 <= data.Length) {
                        // process header (8 bytes)
                        int nSenID = System.Convert.ToInt32(data[offset + 2]) - 1;
                        offset += 8;

                        if (nSenID > max_slots) {
                            Console.WriteLine("[polhemus] SenID is greater than" + max_sensors.ToString() + ".");
                            throw new Exception("[polhemus] SenID is greater than" + max_sensors.ToString() + ".");
                        }

                        // process stylus (4 bytes)
                        uint bfStylus = BitConverter.ToUInt32(data, offset);
                        offset += 4;

                        // process position (12 bytes)
                        float t = BitConverter.ToSingle(data, offset);
                        float u = BitConverter.ToSingle(data, offset + 4);
                        float v = BitConverter.ToSingle(data, offset + 8);
                        offset += 12;

                        // process orientation (16 bytes)
                        float w = BitConverter.ToSingle(data, offset);
                        float x = BitConverter.ToSingle(data, offset + 4);
                        float y = BitConverter.ToSingle(data, offset + 8);
                        float z = BitConverter.ToSingle(data, offset + 12);
                        offset += 16;

                        // store results
                        temp_active[nSenID] = true;
                        digio[nSenID] = bfStylus;
                        positions[nSenID] = new Vector3(t, u, v);
                        orientations[nSenID] = new Vector4(w, x, y, z);
                    }

                    // mark active slots
                    for (var i = 0; i < max_slots; ++i)
                        active[i] = temp_active[i];
                }

            }
        }
        catch (Exception e) {
            Debug.Log(e);
            Debug.Log("[polhemus] PlStream terminated in PlStream::read_liberty()");
            Console.WriteLine("[polhemus] PlStream terminated in PlStream::read_liberty().");
        }
        finally {
#if UNITY_EDITOR
            tcpClient.Close();
            tcpClient = null;
#endif
        }
    }

    /// <summary>
    /// Permet de déconnecter le client du serveur
    /// </summary>
    private void OnApplicationQuit() {
        try {
            // signal shutdown
            stopListening = true;
#if UNITY_EDITOR
            if(conThread == null || stream == null){
                Debug.Log("PlStreamCustom not started, nothing to close");
                return;
            }
            // attempt to join for 500ms
            if (!conThread.Join(500)) {
                // force shutdown
                conThread.Abort();
                if (tcpClient != null) {
                    tcpClient.Close();
                    tcpClient = null;
                }
                stream.Close();
            }
#else
            if (exchangeTask != null) {
                exchangeTask.Wait();
                socket.Dispose();
                stream.Close();
                socket = null;
                exchangeTask = null;
            }
            else {
                Debug.Log("PlStreamCustom not started, nothing to close");
            }
#endif
        }
        catch (Exception e) {
            Debug.Log(e);
            Debug.Log("[polhemus] PlStream was unable to close the connection thread upon application exit. This is not a critical exception.");
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using SVSBluetooth;
using System.Text;
using TMPro;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;

public class BluetoothControls : MonoBehaviour {
    public SampleWebView WebView;
    public FirebaseControls fireControls;
    public TMP_Text textField; // field for displaying messages and events
    const string MY_UUID = "00001101-0000-1000-8000-00805F9B34FB"; // UUID constant which is set via script
    //00001101-0000-1000-8000-00805F9B34FB for arduino
    public string userKey = "";
    string lastConnectedDeviceAddress;
    int lastState = -1;
    float startTime;
    int trying = 0;
    int invokeNo;

    public void FirstTimeConnect() {
        trying = 1;
        lastState = -1;
        invokeNo=0;
        Disconnect();
        Initialize();
        BluetoothForAndroid.ConnectToServer(MY_UUID);
    }

    // subscription and unsubscribe from events. You can read more about events in Documentation.pdf
    private void OnEnable() {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        BluetoothForAndroid.ReceivedByteMessage += PrintVal4;

        BluetoothForAndroid.BtAdapterEnabled += PrintEvent1;
        BluetoothForAndroid.BtAdapterDisabled += PrintEvent2;
        BluetoothForAndroid.DeviceDisconnected += PrintEvent4;
        BluetoothForAndroid.ServerStarted += PrintEvent5;
        BluetoothForAndroid.ServerStopped += PrintEvent6;
        BluetoothForAndroid.AttemptConnectToServer += PrintEvent7;
        BluetoothForAndroid.FailConnectToServer += PrintEvent8;

        BluetoothForAndroid.DeviceSelected += PrintDeviceData;
        BluetoothForAndroid.DeviceConnected += PrintEvent3;
    }
    private void OnDisable() {
        BluetoothForAndroid.ReceivedByteMessage -= PrintVal4;

        BluetoothForAndroid.BtAdapterEnabled -= PrintEvent1;
        BluetoothForAndroid.BtAdapterDisabled -= PrintEvent2;
        BluetoothForAndroid.DeviceConnected -= PrintEvent3;
        BluetoothForAndroid.DeviceDisconnected -= PrintEvent4;
        BluetoothForAndroid.ServerStarted -= PrintEvent5;
        BluetoothForAndroid.ServerStopped -= PrintEvent6;
        BluetoothForAndroid.AttemptConnectToServer -= PrintEvent7;
        BluetoothForAndroid.FailConnectToServer -= PrintEvent8;

        BluetoothForAndroid.DeviceSelected -= PrintDeviceData;
    }

    // Initially, always initialize the plugin.
    public void Initialize() {
        BluetoothForAndroid.Initialize();
        OnEnable();
        EnableBT();
    }

    public void EnableBT() {
        BluetoothForAndroid.EnableBT();
    }
    public void DisableBT() {
        BluetoothForAndroid.DisableBT();
    }
    
    public void Disconnect() {
        BluetoothForAndroid.Disconnect();
    }

    public void ConnectToRegisteredBike(string bikeId) {
        Disconnect();
        EnableBT();
        textField.text+= bikeId+"\n";
        lastState = -1;
        trying = 1;
        invokeNo=0;
        BluetoothForAndroid.ConnectToServerByAddress(MY_UUID, bikeId);
    }

    public void SendPassword() {
        //textField.text += "Last State: "+lastState +"\n";
        //textField.text += "Sending Password: " +userKey+ "\n";
        BluetoothForAndroid.WriteMessage("U:");
        for (int i=0;i<userKey.Length;i++) {
                BluetoothForAndroid.WriteMessage(userKey[i]);
                //textField.text += userKey[i];
        }
        BluetoothForAndroid.WriteMessage(",R:");
        if (lastState == 1) {
            BluetoothForAndroid.WriteMessage("0.");
            textField.text+="Sent 0\n";
        }
        else if (lastState == 0) {
            BluetoothForAndroid.WriteMessage("1.");
            textField.text+="Sent 1\n";
        }
        
        //textField.text+="\n";
    }

    void PrintVal4(byte[] val) {
        foreach (var item in val) {
            //textField.text+="Item"+item+" Last State: "+lastState+"\n";
            if (lastState == -1) {
                lastState = item;
                InvokeRepeating("SendPassword",0.0f,2.0f);
                invokeNo+=1;
                if (item == 0) {
                        WebView.evaluateCmd("setToLock()");
                    }
                    else if (item == 1) {
                        WebView.evaluateCmd("setToUnlock()");
                    }
                startTime = Time.time;
            }
            else{
                if (lastState != item) {
                    textField.text += lastConnectedDeviceAddress + " -> Toggled Lock State with " + item +'\n';
                    CancelInvoke("SendPassword");
                    Disconnect();
                    fireControls.addBike(lastConnectedDeviceAddress);
                    lastState = item;
                    if (item == 0) {
                        WebView.sendAlert("Bike Unlocked Successfully.");
                        WebView.evaluateCmd("setToLock()");
                        break;
                    }
                    else if (item == 1) {
                        WebView.sendAlert("Bike Locked Successfully.");
                        WebView.evaluateCmd("setToUnlock()");
                        break;
                    }
                }
            }
            if (Time.time - startTime > 20 && lastState!=-1) {
                Disconnect();
                for (int i =0;i<invokeNo;i++){
                    CancelInvoke("SendPassword");
                }
                if (lastState!=-1) {
                    WebView.sendAlert("Unsuccessful.");
                    lastState = -1;
                }
                break;
            } 
        }
    }

    // methods for displaying events on the screen
    void PrintEvent1() {
        textField.text += "Adapter enabled" + "\n";
        WebView.evaluateCmd("bluetoothOn()");
    }
    void PrintEvent2() {
        textField.text += "Adapter disabled" + "\n";
        WebView.evaluateCmd("bluetoothOff()");
        }
    
    void PrintEvent3() {
        textField.text += "The device is connected" + "\n";
    }
    void PrintEvent4() {
        textField.text += "Device lost connection" + "\n";
        WebView.evaluateCmd("toggleBlue(0)");
    }
    void PrintEvent5() {
        textField.text += "Server is running at "+ MY_UUID + "\n";
    }
    void PrintEvent6() {
        textField.text += "Server stopped" + "\n";
    }
    void PrintEvent7() {
        textField.text += "Attempt to connect to server" + "\n";
    }
    void PrintEvent8() {
        textField.text += "Connection to the server failed" + "\n";
        WebView.evaluateCmd("toggleBlue(0)");
        if (trying == 1) {
            WebView.sendAlert("Unsuccessful.");
            trying = 0;
        }
    }
    void PrintDeviceData(string deviceData) {
        string[] btDevice = deviceData.Split(new char[] { ',' });
        //textField.text += btDevice[0] + "   ";
        //textField.text += btDevice[1] + "\n";
        lastConnectedDeviceAddress = btDevice[1];
    }

    // method for cleaning the log
    public void ClearLog() {
        textField.text = "";
    }
}

/*
 * Copyright (C) 2012 GREE, Inc.
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty.  In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would be
 *    appreciated but is not required.
 * 2. Altered source versions must be plainly marked as such, and must not be
 *    misrepresented as being the original software.
 * 3. This notice may not be removed or altered from any source distribution.
 */

using System.Collections;
using UnityEngine;
#if UNITY_2018_4_OR_NEWER
using UnityEngine.Networking;
#endif
using UnityEngine.UI;
using TMPro;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

public class SampleWebView : MonoBehaviour
{
    public string Url;
    public Text status;
    WebViewObject webViewObject;
    public GameObject DebugDisplay;
    public TestLocationService locationService;
    public FirebaseControls fireControls;
    public BluetoothControls blueControls;
    public TMP_InputField inputString;
    public TMP_Text console;
    public int bikes;
    public string fullstr;
    public string userPos = "NotFound";

    IEnumerator initialize()
    {
        webViewObject = (new GameObject("WebViewObject")).AddComponent<WebViewObject>();
        webViewObject.Init(
            cb: (msg) =>
            {
                Debug.Log(string.Format("CallFromJS[{0}]", msg));
                if (msg.Contains("requestbikeinfo:")) {
                  string newMsg = msg.Remove(0,16);
                  returnInfo(newMsg);
                  checkWifi();
                }
                else if (msg.Contains("logout")) {
                  fireControls.signOutUser();
                  stopWebApp();
                }
                else if (msg.Contains("pairphysicalkey")) {
                  fireControls.physicalKey();
                }
                else if (msg.Contains("pairbikefirst")) {
                  blueControls.FirstTimeConnect();
                }
                else if (msg.Contains("blueunlock")) {
                  string newMsg = msg.Remove(0,10);
                  fireControls.blueGetBikeId(newMsg);
                }
                else if (msg.Contains("onunlock")) {
                  string newMsg = msg.Remove(0,8);
                  fireControls.onGetBikeId(newMsg);
                }
                else if (msg.Contains("ready")) {
                  for (var i =0;i<bikes;i++) {
                    bikeAdded();
                  }
                  webViewObject.EvaluateJS("updateBikeData(\""+ fullstr+ "\")");
                  InvokeRepeating("GetLocation", 0.0f, 5f);
                  InvokeRepeating("update", 0f, 5f);
                  fullScreen();
                }
                else if (msg.Contains("test")) {
                  console.text += "Recieved\n";
                  console.text += msg+"\n";
                }
                else if (msg.Contains("debugmode")) {
                  DebugDisplay.SetActive(true);
                  debugMode();
                }
                else if (msg.Contains("deleteallbikes")) {
                  fireControls.deleteAllBikes();
                  Invoke("ResetView",1);
                }
                else if (msg.Contains("lockallbikes")) {
                  fireControls.lockAllBikes();
                }
                status.text = msg;
                status.GetComponent<Animation>().Play();
            },
            err: (msg) =>
            {
                Debug.Log(string.Format("CallOnError[{0}]", msg));
                console.text+=msg;
                status.GetComponent<Animation>().Play();
            },
            httpErr: (msg) =>
            {
                Debug.Log(string.Format("CallOnHttpError[{0}]", msg));
                status.text = msg;
                status.GetComponent<Animation>().Play();
            },
            started: (msg) =>
            {
                Debug.Log(string.Format("CallOnStarted[{0}]", msg));
            },
            hooked: (msg) =>
            {
                Debug.Log(string.Format("CallOnHooked[{0}]", msg));
            },
            ld: (msg) =>
            {
                Debug.Log(string.Format("CallOnLoaded[{0}]", msg));
#if UNITY_EDITOR_OSX || (!UNITY_ANDROID && !UNITY_WEBPLAYER && !UNITY_WEBGL)
                // NOTE: depending on the situation, you might prefer
                // the 'iframe' approach.
                // cf. https://github.com/gree/unity-webview/issues/189
#if true
                webViewObject.EvaluateJS(@"
                  if (window && window.webkit && window.webkit.messageHandlers && window.webkit.messageHandlers.unityControl) {
                    window.Unity = {
                      call: function(msg) {
                        window.webkit.messageHandlers.unityControl.postMessage(msg);
                      }
                    }
                  } else {
                    window.Unity = {
                      call: function(msg) {
                        window.location = 'unity:' + msg;
                      }
                    }
                  }
                ");
#else
                webViewObject.EvaluateJS(@"
                  if (window && window.webkit && window.webkit.messageHandlers && window.webkit.messageHandlers.unityControl) {
                    window.Unity = {
                      call: function(msg) {
                        window.webkit.messageHandlers.unityControl.postMessage(msg);
                      }
                    }
                  } else {
                    window.Unity = {
                      call: function(msg) {
                        var iframe = document.createElement('IFRAME');
                        iframe.setAttribute('src', 'unity:' + msg);
                        document.documentElement.appendChild(iframe);
                        iframe.parentNode.removeChild(iframe);
                        iframe = null;
                      }
                    }
                  }
                ");
#endif
#elif UNITY_WEBPLAYER || UNITY_WEBGL
                webViewObject.EvaluateJS(
                    "window.Unity = {" +
                    "   call:function(msg) {" +
                    "       parent.unityWebView.sendMessage('WebViewObject', msg)" +
                    "   }" +
                    "};");
#endif
                webViewObject.EvaluateJS(@"Unity.call('ua=' + navigator.userAgent)");
            }
            //transparent: false,
            //zoom: true,
            //ua: "custom user agent string",
            //radius: 0,  // rounded corner radius in pixel
            //// android
            //androidForceDarkMode: 0,  // 0: follow system setting, 1: force dark off, 2: force dark on
            //// ios
            //enableWKWebView: true,
            //wkContentMode: 0,  // 0: recommended, 1: mobile, 2: desktop
            //wkAllowsLinkPreview: true,
            //// editor
            //separated: false
            );

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        webViewObject.bitmapRefreshCycle = 1;
#endif
        // cf. https://github.com/gree/unity-webview/pull/512
        // Added alertDialogEnabled flag to enable/disable alert/confirm/prompt dialogs. by KojiNakamaru · Pull Request #512 · gree/unity-webview
        //webViewObject.SetAlertDialogEnabled(false);

        // cf. https://github.com/gree/unity-webview/pull/728
        //webViewObject.SetCameraAccess(true);
        //webViewObject.SetMicrophoneAccess(true);

        // cf. https://github.com/gree/unity-webview/pull/550
        // introduced SetURLPattern(..., hookPattern). by KojiNakamaru · Pull Request #550 · gree/unity-webview
        //webViewObject.SetURLPattern("", "^https://.*youtube.com", "^https://.*google.com");

        // cf. https://github.com/gree/unity-webview/pull/570
        // Add BASIC authentication feature (Android and iOS with WKWebView only) by takeh1k0 · Pull Request #570 · gree/unity-webview
        //webViewObject.SetBasicAuthInfo("id", "password");

        //webViewObject.SetScrollbarsVisibility(true);
        webViewObject.SetMargins(0, 0, 0, Screen.height/5);
        //webViewObject.SetMargins(0,0,0,0);
        webViewObject.SetTextZoom(100);  // android only. cf. https://stackoverflow.com/questions/21647641/android-webview-set-font-size-system-default/47017410#47017410
        webViewObject.SetVisibility(true);

#if !UNITY_WEBPLAYER && !UNITY_WEBGL
        if (Url.StartsWith("http")) {
            webViewObject.LoadURL(Url.Replace(" ", "%20"));
        } else {
            var exts = new string[] {".jpg",".js",".html"};
            foreach (var ext in exts) {
                var url = Url.Replace(".html", ext);
                var src = System.IO.Path.Combine(Application.streamingAssetsPath, url);
                var dst = System.IO.Path.Combine(Application.persistentDataPath, url);
                byte[] result = null;
                if (src.Contains("://")) {  // for Android
#if UNITY_2018_4_OR_NEWER
                    // NOTE: a more complete code that utilizes UnityWebRequest can be found in https://github.com/gree/unity-webview/commit/2a07e82f760a8495aa3a77a23453f384869caba7#diff-4379160fa4c2a287f414c07eb10ee36d
                    var unityWebRequest = UnityWebRequest.Get(src);
                    yield return unityWebRequest.SendWebRequest();
                    result = unityWebRequest.downloadHandler.data;
                    //Goes Here

#else
                    var www = new WWW(src);
                    yield return www;
                    result = www.bytes;
#endif
                } else {
                    result = System.IO.File.ReadAllBytes(src);
                }
                System.IO.File.WriteAllBytes(dst, result);
                if (ext == ".html") {
                    webViewObject.LoadURL("file://" + dst.Replace(" ", "%20"));
                    break;
                }
            }
        }
#else
        if (Url.StartsWith("http")) {
            webViewObject.LoadURL(Url.Replace(" ", "%20"));
        } else {
            webViewObject.LoadURL("StreamingAssets/" + Url.Replace(" ", "%20"));
        }
#endif
        var imgfile = new string[] {"BikeTile.png","InfoTile.png","PlusTile.png","Menu.png","Marker.png","GPS.png","UserPosition.png"};
        for (var i=0 ;i<7;i++) {
          console.text += i;
          var url = "images/"+imgfile[i];
          var src = System.IO.Path.Combine(Application.streamingAssetsPath, url);
          var dst = System.IO.Path.Combine(Application.persistentDataPath, imgfile[i]);
          var unityWebRequest = UnityWebRequest.Get(src);
          yield return unityWebRequest.SendWebRequest();
          byte[] result = unityWebRequest.downloadHandler.data;
          System.IO.File.WriteAllBytes(dst, result);
        }
        yield break;
    }

    void OnGUI()
    {
        // var x = 10;

        // GUI.enabled = webViewObject.CanGoBack();
        // if (GUI.Button(new Rect(x, 10, 80, 80), "<")) {
        //     webViewObject.GoBack();
        // }
        // GUI.enabled = true;
        // x += 90;

        // GUI.enabled = webViewObject.CanGoForward();
        // if (GUI.Button(new Rect(x, 10, 80, 80), ">")) {
        //     webViewObject.GoForward();
        // }
        // GUI.enabled = true;
        // x += 90;

        // if (GUI.Button(new Rect(x, 10, 80, 80), "r")) {
        //     webViewObject.Reload();
        // }
        // x += 90;

        // GUI.TextField(new Rect(x, 10, 180, 80), "" + webViewObject.Progress());
        // x += 190;

        // if (GUI.Button(new Rect(x, 10, 80, 80), "*")) {
        //     var g = GameObject.Find("WebViewObject");
        //     if (g != null) {
        //         Destroy(g);
        //     } else {
        //         StartCoroutine(Start());
        //     }
        // }
        // x += 90;

        // if (GUI.Button(new Rect(x, 10, 80, 80), "c")) {
        //     Debug.Log(webViewObject.GetCookies(Url));
        // }
        // x += 90;

        // if (GUI.Button(new Rect(x, 10, 80, 80), "x")) {
        //     webViewObject.ClearCookies();
        // }
        // x += 90;

        // if (GUI.Button(new Rect(x, 10, 80, 80), "D")) {
        //     webViewObject.SetInteractionEnabled(false);
        // }
        // x += 90;

        // if (GUI.Button(new Rect(x, 10, 80, 80), "E")) {
        //     webViewObject.SetInteractionEnabled(true);
        // }
        // x += 90;
    }

    public void MovePosition(){
        string cmd = "updatePosition("+inputString.text+")";
        webViewObject.EvaluateJS(cmd);
        console.text += "Updated Position to " + inputString.text + "\n";
    }

    public void AddMarker(){
        string cmd = "addMarker("+inputString.text+")";
        webViewObject.EvaluateJS(cmd);
        console.text += "Added Marker to " + inputString.text + "\n";
    }

    public void ResetView(){
      webViewObject.Reload();
    }

    public void returnInfo(string bikeno){
      var bikeLs = fullstr.Split("&");
      var bikeEle = int.Parse(bikeno)-1;

      string cmd = "showBikeInfo(\""+ bikeno + "+"+ bikeLs[bikeEle]+ "\")";
      webViewObject.EvaluateJS(cmd);
    }

    public void clearConsole(){
      console.text = "";
    }

    public async void startWebApp(){
      await updateBikes();
      StartCoroutine(initialize());
      blueControls.Initialize();
      GetLocation();
    }

    public async Task updateBikes(){
      var bikesIdList = await fireControls.GetRegisteredBikeList();
      bikes = bikesIdList.Count - 1;
      fullstr = "";
      for (var i=1;i<bikesIdList.Count;i++) {
        var dict = await fireControls.GetBikeData(bikesIdList[i]);
        foreach(var item in dict.Values){
          fullstr += item + "+";
        }
        fullstr += "&";
      }
    }

    public async void update(){
      await updateBikes();
      webViewObject.EvaluateJS("updateBikeData(\""+ fullstr+ "\")");
    }

    public void stopWebApp() {
      var g = GameObject.Find("WebViewObject");
      if (g != null) {
        Destroy(g);
      }
    }

    public void wifiActive() {
      var cmd = "wifiToggle()";
      webViewObject.EvaluateJS(cmd);
    }
    
    public void bluetoothActive() {
      var cmd = "bluetoothToggle()";
      webViewObject.EvaluateJS(cmd);
    }

    public void bikeAdded(){
      var cmd = "manuallyAddBike()";
      webViewObject.EvaluateJS(cmd);
    }

    public void GetLocation(){
      locationService.LocationCoroutine();
    }

    public void updateBikePos(string pos){
      webViewObject.EvaluateJS("updateUserPos(\""+ pos+ "\")");
    }
    public void fullScreen(){
      DebugDisplay.SetActive(false);
      webViewObject.SetMargins(0,0,0,0);
      webViewObject.EvaluateJS("toggleFullscreenMode()");
    }
    public void debugMode(){
      webViewObject.SetMargins(0, 0, 0, Screen.height/5);
      webViewObject.EvaluateJS("toggleFullscreenMode()");
    }
    public void sendAlert(string alertStr){
      webViewObject.EvaluateJS("alert(\""+ alertStr+ "\")");
    }
    public void evaluateCmd(string cmd){
      webViewObject.EvaluateJS(cmd);
    }

    public void checkWifi(){
      if(Application.internetReachability == 0) {
        webViewObject.EvaluateJS("wifiOff()");
      }
      else{
        webViewObject.EvaluateJS("wifiOn()");
      }
    }
}

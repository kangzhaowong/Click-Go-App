using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.Android;

public class TestLocationService : MonoBehaviour
{
    public SampleWebView WebView;
    public TMP_Text console;

    public void LocationCoroutine()
    {
         #if UNITY_ANDROID
                  if (!Permission.HasUserAuthorizedPermission (Permission.FineLocation))
                  {
                           Permission.RequestUserPermission (Permission.FineLocation);
                  }
         #elif UNITY_IOS
                  PlayerSettings.iOS.locationUsageDescription = "Details to use location";
         #endif
         StartCoroutine(StartLocationService());
    }
    private IEnumerator StartLocationService()
    {
         if (!Input.location.isEnabledByUser)
        {
                 console.text += "User has not enabled location";
                  yield break;
         }
         Input.location.Start();
         while(Input.location.status == LocationServiceStatus.Initializing)
         {
                  yield return new WaitForSeconds(1);
         }
         if (Input.location.status == LocationServiceStatus.Failed)
         {
                  console.text += "Unable to determine device location";
                  yield break;
         }
        //  console.text += "Latitude : " + Input.location.lastData.latitude +"\n";
        //  console.text += "Longitude : " + Input.location.lastData.longitude;
         WebView.updateBikePos(Input.location.lastData.latitude.ToString() +','+Input.location.lastData.longitude.ToString());
         //console.text+=Input.location.lastData.latitude.ToString() +','+Input.location.lastData.longitude.ToString()+"\n";

    }
}
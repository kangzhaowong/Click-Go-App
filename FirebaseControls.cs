using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Threading.Tasks;

public class FirebaseControls : MonoBehaviour
{
    public GameObject loginScreen;
    public GameObject createAccountScreen;
    public GameObject DebugDisplay;
    public SampleWebView WebView;
    public BluetoothControls blueControls;
    public TMP_InputField usernameField;
    public TMP_InputField passwordField;
    public TMP_InputField createUsername;
    public TMP_InputField createPassword;
    public TMP_InputField createEmail;
    public TMP_InputField checkPassword;
    public TMP_Text console;
    public TMP_Text alertUser;
    public TMP_Text alertUser2;
    public int currentUser = -1;
    public int currentMode = 1; // 1 => login screen, 0=> Create Account, -1 => App Running 

    async Task Start() {
        createAccountScreen.SetActive(false);
        DebugDisplay.SetActive(false);
        DatabaseReference reference = FirebaseDatabase.DefaultInstance.RootReference;
        await initUserCounter();
        if (PlayerPrefs.HasKey("username") && PlayerPrefs.HasKey("password")){
            usernameField.text = PlayerPrefs.GetString("username");
            passwordField.text = PlayerPrefs.GetString("password");
            loginAccount();
        }
        
    }

    public void goToCreateAccount(){
        loginScreen.SetActive(false);
        createAccountScreen.SetActive(true);
        currentMode = 0;
        usernameField.text = "";
        passwordField.text = "";
    }

    public void goToLogin(){
        createAccountScreen.SetActive(false);
        loginScreen.SetActive(true);
        currentMode = 1;
        createUsername.text = "";
        createPassword.text = "";
        createEmail.text = "";
        checkPassword.text = "";
    }

    public async Task initUserCounter() {
        var userCounterRef = await FirebaseDatabase.DefaultInstance.GetReference("User_Count").GetValueAsync();
        if (!userCounterRef.Exists) {
            await FirebaseDatabase.DefaultInstance.GetReference("User_Count").SetValueAsync(0);
        }

        var newUserCounterRef = await FirebaseDatabase.DefaultInstance.GetReference("User_Count").GetValueAsync();
        console.text += "Current Number of Users: "+ newUserCounterRef.GetValue(true).ToString() + "\n";
    }

    private async Task AddUser(int userId,User newUser) {
        string json = JsonUtility.ToJson(newUser);
        await FirebaseDatabase.DefaultInstance.RootReference.Child("User_Database").Child(userId.ToString()).SetRawJsonValueAsync(json);
    }

    private async Task AddBike(string bikeId,Bike newBike) {
        string json = JsonUtility.ToJson(newBike);
        await FirebaseDatabase.DefaultInstance.RootReference.Child("Bike_Database").Child(bikeId).SetRawJsonValueAsync(json);
    }

    private async Task<int> GetUserCount(){
        var task = await FirebaseDatabase.DefaultInstance.GetReference("User_Count").GetValueAsync();
        int count = int.Parse(task.GetValue(true).ToString());
        console.text += "Current Number of Users: "+ count.ToString() + "\n";
        return count;
    }

    private async Task UpdateUserCount(int currentUserId){
        string updatedUserCount = (currentUserId + 1).ToString();
        await FirebaseDatabase.DefaultInstance.RootReference.Child("User_Count").SetRawJsonValueAsync(updatedUserCount);
    }

    private async Task<Dictionary<string,string>> GetUserData(int UserId){
        var userProfile = new Dictionary<string, string>();
        var userId = await FirebaseDatabase.DefaultInstance.GetReference("User_Database").Child(UserId.ToString()).Child("userId").GetValueAsync();
        var username = await FirebaseDatabase.DefaultInstance.GetReference("User_Database").Child(UserId.ToString()).Child("username").GetValueAsync();
        var password = await FirebaseDatabase.DefaultInstance.GetReference("User_Database").Child(UserId.ToString()).Child("password").GetValueAsync();
        var email = await FirebaseDatabase.DefaultInstance.GetReference("User_Database").Child(UserId.ToString()).Child("email").GetValueAsync();
        var registeredBikesId = await FirebaseDatabase.DefaultInstance.GetReference("User_Database").Child(UserId.ToString()).Child("registeredBikesId").GetValueAsync();

        userProfile.Add("username",username.GetValue(true).ToString());
        userProfile.Add("password",password.GetValue(true).ToString());
        userProfile.Add("email",password.GetValue(true).ToString());
        string bikeIdList = "";
        foreach (var i in registeredBikesId.Children) {
            if (i.GetValue(true).ToString() != "EMPTYELEMENT") {
                bikeIdList += i.GetValue(true).ToString() + " ";
            }
        }
        userProfile.Add("registeredBikesId",bikeIdList.ToString());
        return userProfile;
    }

    public async Task<Dictionary<string,string>> GetBikeData(string BikeId){
        var bikeProfile = new Dictionary<string, string>();
        var lockState = await FirebaseDatabase.DefaultInstance.GetReference("Bike_Database").Child(BikeId).Child("lockState").GetValueAsync();
        var userKey = await FirebaseDatabase.DefaultInstance.GetReference("Bike_Database").Child(BikeId).Child("userKey").GetValueAsync();
        var bikePosition = await FirebaseDatabase.DefaultInstance.GetReference("Bike_Database").Child(BikeId).Child("bikePosition").GetValueAsync();
        var alarmState = await FirebaseDatabase.DefaultInstance.GetReference("Bike_Database").Child(BikeId).Child("alarmState").GetValueAsync();
        var batteryLevel = await FirebaseDatabase.DefaultInstance.GetReference("Bike_Database").Child(BikeId).Child("batteryLevel").GetValueAsync();
        var requestState = await FirebaseDatabase.DefaultInstance.GetReference("Bike_Database").Child(BikeId).Child("requestState").GetValueAsync();

        bikeProfile.Add("lockState",lockState.GetValue(true).ToString());
        bikeProfile.Add("userKey",userKey.GetValue(true).ToString());
        bikeProfile.Add("bikePosition",bikePosition.GetValue(true).ToString());
        bikeProfile.Add("alarmState",alarmState.GetValue(true).ToString());
        bikeProfile.Add("batteryLevel",batteryLevel.GetValue(true).ToString());
        bikeProfile.Add("requestState",requestState.GetValue(true).ToString());
        return bikeProfile;
    }

    private async Task<int> CheckIfUsernameValid(string username) {
        int Id = -1;
        var userProfileId = await FirebaseDatabase.DefaultInstance.GetReference("User_Database").OrderByChild("username").EqualTo(username).GetValueAsync();
        if (userProfileId.Exists) {
            foreach (var i in userProfileId.Children) {
                Id = int.Parse(i.Key);
                break;
            }
        }
        return Id;
    }

    public async Task<List<string>> GetRegisteredBikeList(){
        List<string> result = new List<string>();
        var registeredBikesId = await FirebaseDatabase.DefaultInstance.GetReference("User_Database").Child(currentUser.ToString()).Child("registeredBikesId").GetValueAsync();
        foreach (var i in registeredBikesId.Children) {
            result.Add(i.GetValue(true).ToString());
        }
        return result;
    } 

    public async void createAccount() {
        alertUser2.text = "";
        if (createPassword.text != checkPassword.text | createEmail.text == "" | createPassword.text =="" | createUsername.text =="") {
            alertUser2.text = "Invalid input.";
        }
        else {
            if (await CheckIfUsernameValid(createUsername.text) == -1) {
                DatabaseReference reference = FirebaseDatabase.DefaultInstance.RootReference;

                int newUserId = await GetUserCount();
                User newUser = new User(createUsername.text,createPassword.text,newUserId,createEmail.text);

                await AddUser(newUserId,newUser);
                alertUser2.text = "Account creation successful.";
                console.text += "New User ID: "+ newUserId + "\n";
                var userProfile = await GetUserData(newUserId);
                console.text += "\tUsername : " + userProfile["username"] + "\n";
                console.text += "\tPassword : " + userProfile["password"] + "\n";
                console.text += "\tEmail : " + userProfile["email"] + "\n";
                console.text += "\tRegistered Bike ID: [" + userProfile["registeredBikesId"] + "]\n"; 
                await UpdateUserCount(newUserId);

                usernameField.text = "";
                passwordField.text = "";
            }
            else {
                console.text += "Username already in use.\n";
                alertUser2.text = "Username already in use.";
            }
        }
    }

    public async void loginAccount() {
        alertUser.text = "";
        int userId = await CheckIfUsernameValid(usernameField.text);
        if (userId == -1) {
            console.text += "No Such User\n";
            alertUser.text = "No Such User";
        }
        else {
            var userProfile = await GetUserData(userId);
            if (passwordField.text == userProfile["password"]) {
                alertUser.text = "Successful Login.";
                console.text += "Successful Login.\n";
                console.text += "User Profile ID: "+ userId + "\n";
                console.text += "\tUsername : " + userProfile["username"] + "\n";
                console.text += "\tPassword : " + userProfile["password"] + "\n";
                console.text += "\tEmail : " + userProfile["email"] + "\n";
                console.text += "\tRegistered Bike ID: [" + userProfile["registeredBikesId"] + "]\n"; 
                currentUser = userId;
                showCurrentUser(userProfile["username"]);
                PlayerPrefs.SetString("username",userProfile["username"]);
                PlayerPrefs.SetString("password",userProfile["password"]);
                startApp();
            }
            else {
                console.text += "Login Unsuccessful.\n";
                alertUser.text = "Login Unsuccessful.";
            }
        }
        usernameField.text = "";
        passwordField.text = "";
    }

    public async void showUserInfo(){
        if (currentUser!=-1) {
            var userProfile = await GetUserData(currentUser);
            console.text += "User Profile ID: "+ currentUser + "\n";
            console.text += "\tUsername : " + userProfile["username"] + "\n";
            console.text += "\tPassword : " + userProfile["password"] + "\n";
            console.text += "\tEmail : " + userProfile["email"] + "\n";
            console.text += "\tRegistered Bike ID: [" + userProfile["registeredBikesId"] + "]\n"; 
        }
        else {
            console.text += "No current user.";
        }
    }

    public async void allBikeList(){
        var allbikels = await FirebaseDatabase.DefaultInstance.GetReference("Bike_Database").GetValueAsync();
        List<string> bikels = new List<string>();
        foreach (var i in allbikels.Children) {
            console.text += i.Key.ToString() +"\n";
            bikels.Add(i.Key.ToString());
        }
    }

    public async void blueStateChange(string bikeIdnew,string newState) {
        console.text += "Bike ID: "+bikeIdnew + "New State: " + newState + "\n";
        await FirebaseDatabase.DefaultInstance.RootReference.Child("Bike_Database").Child(bikeIdnew).Child("lockState").SetValueAsync(newState);
        if (newState == "1") {
            WebView.evaluateCmd("setToUnlock()");
        }
        else if (newState == "0") {
            WebView.evaluateCmd("setToLock()");
        }
    }

    public async void addBike(string bikeIdnew) {
        if (currentUser!=-1) {
            var currentBikeList = await GetRegisteredBikeList();
            bool bikeInList = false;
            foreach(var i in currentBikeList) {
                if (i == bikeIdnew) {
                    bikeInList = true;
                }
            }
            var userBikeList = await GetUserData(currentUser);
            if (userBikeList["registeredBikesId"].Contains(bikeIdnew)) {
                bikeInList = true;

            }

            var allbikels = await FirebaseDatabase.DefaultInstance.GetReference("Bike_Database").GetValueAsync();
            List<string> bikels = new List<string>();
            foreach (var i in allbikels.Children) {
                console.text += i.Key.ToString() +"\n";
                bikels.Add(i.Key.ToString());
            }
            foreach(var i in bikels) {
                if (i == bikeIdnew) {
                    bikeInList = true;
                }
            }

            if (bikeInList) {
                console.text += "Bike Already In List\n";
                WebView.sendAlert("Unsuccessful.");
            }
            else {
                string newBikeId = bikeIdnew;
                string userKey = "qwerty";
                string bikePosition = "0,0";
                Bike newBike = new Bike(userKey,bikePosition);
                await AddBike(newBikeId,newBike);
                var bikeProfile = await GetBikeData(newBikeId);
                
                console.text += "New Bike Sucsessfully Created\n";
                console.text += "Bike Profile ID: "+ newBikeId + "\n";
                console.text += "\tLock State : " + bikeProfile["lockState"] + "\n";
                console.text += "\tBike Position : " + bikeProfile["bikePosition"] + "\n";
                console.text += "\tAlarm State: "+ bikeProfile["alarmState"] + "\n";
                console.text += "\tBattery Level: "+ bikeProfile["batteryLevel"] + "\n";
                console.text += "\tUser Key: "+ bikeProfile["userKey"] + "\n";
                console.text += "\tRequest: "+ bikeProfile["requestState"] + "\n";
                
                var registeredBikeList = await GetRegisteredBikeList();
                registeredBikeList.Add(newBikeId);
                await FirebaseDatabase.DefaultInstance.RootReference.Child("User_Database").Child(currentUser.ToString()).Child("registeredBikesId").SetValueAsync(registeredBikeList);
                console.text += "\nAdded Bike to List of Current User";
                var userProfile = await GetUserData(currentUser);
                console.text += "User Profile ID: "+ currentUser + "\n";
                console.text += "\tUsername : " + userProfile["username"] + "\n";
                console.text += "\tPassword : " + userProfile["password"] + "\n";
                console.text += "\tEmail : " + userProfile["email"] + "\n";
                console.text += "\tRegistered Bike ID: [" + userProfile["registeredBikesId"] + "]\n"; 
                WebView.bikeAdded();
            }
        }
        else {
            console.text += "\nSign in and try again.";
        }
    }

    private void showCurrentUser(string username){
        console.text += "Hello, " + username + " !";
    }

    public void signOutUser(){
        currentUser = -1;
        console.text += "Signed Out.";
        alertUser.text = "";
        alertUser2.text = "";
        PlayerPrefs.DeleteKey("username");
        PlayerPrefs.DeleteKey("password");
    }
    
    public void clearConsole() {
        console.text = "";
    }

    public void startApp(){
        WebView.startWebApp();
    }

    public async void blueGetBikeId(string bikeNo) {
        WebView.evaluateCmd("toggleBlue(1)");
        var bikeList = await GetRegisteredBikeList();
        int bikeEle = int.Parse(bikeNo);
        var newUserKey = await FirebaseDatabase.DefaultInstance.GetReference("Bike_Database").Child(bikeList[bikeEle]).Child("userKey").GetValueAsync();
        blueControls.userKey = newUserKey.GetValue(true).ToString();
        blueControls.ConnectToRegisteredBike(bikeList[bikeEle]);
    }

    public async void onGetBikeId(string bikeNo) {
        WebView.evaluateCmd("toggleOn(1)");
        var bikeList = await GetRegisteredBikeList();
        int bikeEle = int.Parse(bikeNo);
        var current_state = await FirebaseDatabase.DefaultInstance.RootReference.Child("Bike_Database").Child(bikeList[bikeEle]).Child("lockState").GetValueAsync();
        string requested_State;
        if (current_state.GetValue(true).ToString() == "0") {
            await FirebaseDatabase.DefaultInstance.RootReference.Child("Bike_Database").Child(bikeList[bikeEle]).Child("requestState").SetValueAsync("1");
            requested_State = "1";
        }
        else {
            await FirebaseDatabase.DefaultInstance.RootReference.Child("Bike_Database").Child(bikeList[bikeEle]).Child("requestState").SetValueAsync("0");
            requested_State = "0";
        }
        var startTime = Time.time;
        while (true) {
            var new_state = await FirebaseDatabase.DefaultInstance.RootReference.Child("Bike_Database").Child(bikeList[bikeEle]).Child("lockState").GetValueAsync();
            if (Time.time - startTime > 60 && new_state.GetValue(true).ToString() != requested_State) {
                await FirebaseDatabase.DefaultInstance.RootReference.Child("Bike_Database").Child(bikeList[bikeEle]).Child("requestState").SetValueAsync("-1");
                WebView.sendAlert("Unsuccessful.");
                WebView.evaluateCmd("toggleOn(0)");
                break;
            }
            else if (new_state.GetValue(true).ToString() == requested_State) {
                await FirebaseDatabase.DefaultInstance.GetReference("Bike_Database").Child(bikeList[bikeEle]).Child("alarmState").SetValueAsync("0");
                WebView.sendAlert("Success.");
                if (requested_State == "1") {
                    WebView.evaluateCmd("setToUnlock()");
                }
                else if (requested_State == "0") {
                    WebView.evaluateCmd("setToLock()");
                }
                await FirebaseDatabase.DefaultInstance.RootReference.Child("Bike_Database").Child(bikeList[bikeEle]).Child("requestState").SetValueAsync("-1");
                WebView.evaluateCmd("toggleOn(0)");
                break;
            }
        }
    }

    public async void physicalKey() {
        var bikeList = await GetRegisteredBikeList();
        string userKeys = "";
        for (int i=1;i<bikeList.Count;i++) {
            var temp = await FirebaseDatabase.DefaultInstance.GetReference("Bike_Database").Child(bikeList[i]).Child("userKey").GetValueAsync();
            userKeys += temp.GetValue(true).ToString() +"&";
        }
        blueControls.userKey = userKeys;
        blueControls.FirstTimeConnect();
    }

    public async void deleteAllBikes() {
        var delBikels = await GetRegisteredBikeList();
        for(int i=1;i<delBikels.Count;i++) {
            await FirebaseDatabase.DefaultInstance.GetReference("Bike_Database").Child(delBikels[i]).SetValueAsync(null);
        }
        List<string> registeredBikesId = new List<string>();
        registeredBikesId.Add("EMPTYELEMENT");
        await FirebaseDatabase.DefaultInstance.RootReference.Child("User_Database").Child(currentUser.ToString()).Child("registeredBikesId").SetValueAsync(registeredBikesId);
    }
    public async void lockAllBikes() {
        var delBikels = await GetRegisteredBikeList();
        string bikes_locked = "";
        for(int i=1;i<delBikels.Count;i++) {
            await FirebaseDatabase.DefaultInstance.GetReference("Bike_Database").Child(delBikels[i]).Child("requestState").SetValueAsync("1");
        }
        for(int i=1;i<delBikels.Count;i++) {
            var success = await FirebaseDatabase.DefaultInstance.GetReference("Bike_Database").Child(delBikels[i]).Child("lockState").GetValueAsync();
            if (success.GetValue(true).ToString() == "1") {
                bikes_locked += "Bike " + i + " Locked. "; 
            }
        }
        await Task.Delay(30000);
        if (bikes_locked != "") {
            WebView.sendAlert(bikes_locked);
        }
        else {
            WebView.sendAlert("ERROR NO BIKES LOCKED");
        }
        for(int i=1;i<delBikels.Count;i++) {
            await FirebaseDatabase.DefaultInstance.RootReference.Child("Bike_Database").Child(delBikels[i]).Child("requestState").SetValueAsync("-1");
        }
    }
}

public class User {
    public int userId;
    public string username;
    public string password;
    public string email;
    public List<string> registeredBikesId;

    public User(string username, string password, int userId,string email) {
        this.userId = userId;
        this.username = username;
        this.password = password;
        this.email = email;
        this.registeredBikesId = new List<string>();
        this.registeredBikesId.Add("EMPTYELEMENT");
    }
}

public class Bike {
    public string lockState;
    public string alarmState;
    public string requestState;
    public string bikePosition;
    public string userKey;
    public string batteryLevel;

    public Bike(string userKey, string bikePosition) {
        this.lockState = "0";
        this.bikePosition = bikePosition;
        this.alarmState = "0";
        this.userKey = userKey;
        this.batteryLevel = "0%";
        this.requestState = "-1";
    }
}
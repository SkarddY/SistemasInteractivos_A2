using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;
using System.Linq;
using UnityEngine.Networking;


public class AuthResponse {
    public User usuario;
    public UserScore data;
    public string token;
}
[System.Serializable] public class User {
    public string userID, username, password;
    public UserScore data;

    public User() {
        data = new UserScore();
    }
    public User(string username, string password) {
        this.username = username;
        this.password = password;
        data = new UserScore();
    }
}
[System.Serializable] public class UsersList {
    public List<User> usuarios;
}
[System.Serializable] public class UserScore {
    public int score;
}

public class ApiManager : MonoBehaviour
{
    [SerializeField] private string baseURL = "https://sid-restapi.herokuapp.com";

    [SerializeField] private GameObject loginScreenPanel, mainScreenPanel;
    [SerializeField] UnityEvent loginPanelActive, mainPanelActive;

    [SerializeField] private TMP_Text[] userOrder;
    [SerializeField] private TMP_Text userID;

    string contentType = "application/json";
    string userType = "/api/usuarios/";

    public string Username { get; set; }
    public string Token { get; set; } private string token;

    public void Start() {
        List<User> userList = new List<User>();
        List<User> dUserList = userList.OrderByDescending(user => user.data.score).ToList<User>();
        if (string.IsNullOrEmpty(Token)) {
            loginPanelActive.Invoke();
        }
        else {
            mainPanelActive.Invoke(); token = Token;
            StartCoroutine(GetUser());
        }
    }
    public void Register() {
        User user = new User();
        user.username = GameObject.Find("UsernameField").GetComponent<TMP_InputField>().text;
        user.password = GameObject.Find("PasswordField").GetComponent<TMP_InputField>().text;
        string jsonData = JsonUtility.ToJson(user); StartCoroutine(cRegister(jsonData));
    }
    public void LogIn() {
        User user = new User();
        user.username = GameObject.Find("UsernameField").GetComponent<TMP_InputField>().text;
        user.password = GameObject.Find("PasswordField").GetComponent<TMP_InputField>().text;
        string jsonData = JsonUtility.ToJson(user); StartCoroutine(cLogIn(jsonData));
    }
    IEnumerator GetUser()
    {
        UnityWebRequest www = UnityWebRequest.Get(baseURL + userType + Username);
        www.SetRequestHeader("x-token", Token);
        yield return www.SendWebRequest();
        if (www.isNetworkError) Debug.Log("NETWORK ERROR :" + www.error);

        else {
            Debug.Log(www.downloadHandler.text);
            if (www.responseCode == 200) {
                AuthResponse jsonData = JsonUtility.FromJson<AuthResponse>(www.downloadHandler.text);
                userID.text = jsonData.usuario.username; StartCoroutine(ScoreUpdate());
            }
            else {
                loginPanelActive.Invoke();
                string message = "Status :" + www.responseCode;
                message += "\ncontent-type:" + www.GetResponseHeader("content-type");
                message += "\nError :" + www.error;
                Debug.Log(message);
            }
        }
    }

    IEnumerator cRegister(string registerData)
    {
        UnityWebRequest www = UnityWebRequest.Put(baseURL + userType, registerData);
        www.method = "POST";
        www.SetRequestHeader("Content-Type", contentType);

        yield return www.SendWebRequest();
        if (www.isNetworkError) Debug.Log("NETWORK ERROR :" + www.error);

        else {
            Debug.Log(www.downloadHandler.text);

            if (www.responseCode == 200) {

                AuthResponse jsonData = JsonUtility.FromJson<AuthResponse>(www.downloadHandler.text);
                Debug.Log(jsonData.usuario.username + " " + "has registered succesfully" + jsonData.usuario.userID);
            }
            else {
                string message = "Registrarion status:" + " " + www.responseCode;
                message += "\ncontent-type:" + www.GetResponseHeader("content-type");
                message += "\nError :" + www.error;
                Debug.Log(message);
            }

        }
    }
    IEnumerator cLogIn(string loginData)
    {
        UnityWebRequest www = UnityWebRequest.Put(baseURL + "/api/auth/login", loginData);
        www.method = "POST";
        www.SetRequestHeader("Content-Type", contentType);

        yield return www.SendWebRequest();
        if (www.isNetworkError) Debug.Log("NETWORK ERROR :" + www.error);
        
        else {
            Debug.Log(www.downloadHandler.text);
            if (www.responseCode == 200) {
                AuthResponse jsonData = JsonUtility.FromJson<AuthResponse>(www.downloadHandler.text);
                Debug.Log(jsonData.usuario.username + " " + "has logged in...");

                Username = jsonData.usuario.username; Token = jsonData.token;

                PlayerPrefs.SetString("token", Token); PlayerPrefs.SetString("username", Username);

                mainPanelActive.Invoke();

                StartCoroutine(ScoreUpdate());
                userID.text = jsonData.usuario.username;
            }
            else {
                string message = "Status :" + www.responseCode;
                message += "\ncontent-type:" + www.GetResponseHeader("content-type");
                message += "\nError :" + www.error;
                Debug.Log(message);
            }
        }
    }
   

    IEnumerator ScoreUpdate()
    {
        UnityWebRequest www = UnityWebRequest.Get(baseURL + userType);
        www.SetRequestHeader("x-token", Token);
        yield return www.SendWebRequest();

        if (www.isNetworkError) Debug.Log("NETWORK ERROR :" + www.error);

        else {
            Debug.Log(www.downloadHandler.text);
            if (www.responseCode == 200) {
                int uScoreOrder = 0;
                UsersList uList = JsonUtility.FromJson<UsersList>(www.downloadHandler.text);
                List<User> uList_2 = uList.usuarios;
                List<User> dUserList = uList_2.OrderByDescending(user => user.data.score).ToList<User>();
                foreach (User user in dUserList) {
                    if (uScoreOrder > 6) Debug.Log("New user added...");
                    else {
                        string uName = uScoreOrder + 1 + " • " + user.username + " [ " + user.data.score + " ]" ;
                        userOrder[uScoreOrder].text = uName; uScoreOrder++;
                    }
                }
            }
            else {
                string mensaje = "Status :" + www.responseCode;
                mensaje += "\ncontent-type:" + www.GetResponseHeader("content-type");
                mensaje += "\nError :" + www.error; Debug.Log(mensaje);
            }

        }
    }
    IEnumerator ScoreUpload(string scoreData)
    {
        UnityWebRequest www = UnityWebRequest.Put(baseURL + userType, scoreData);
        www.method = "PATCH";
        www.SetRequestHeader("x-token", Token);
        www.SetRequestHeader("Content-Type", contentType);
        yield return www.SendWebRequest();
        if (www.isNetworkError) {
            loginPanelActive.Invoke();
            Debug.Log("NETWORK ERROR:" + www.error);
        }
        else {
            Debug.Log(www.downloadHandler.text);
            if (www.responseCode == 200) {
                AuthResponse jsonData = JsonUtility.FromJson<AuthResponse>(www.downloadHandler.text);
                StartCoroutine(ScoreUpdate());
            }
            else {
                string message = "Status :" + www.responseCode;
                message += "\ncontent-type:" + www.GetResponseHeader("content-type");
                message += "\nError :" + www.error; Debug.Log(message);
            }
        }
    }
    public void OnSubmitButton() {
        User user = new User();
        user.username = Username;
        if (int.TryParse(GameObject.Find("ScoreField").GetComponent<TMP_InputField>().text, out int _)) {
            user.data.score = int.Parse(GameObject.Find("ScoreField").GetComponent<TMP_InputField>().text);
        }
        string postData = JsonUtility.ToJson(user); StartCoroutine(ScoreUpload(postData));
    }
}




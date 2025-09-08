using System;
using PlayFab;
using PlayFab.ClientModels;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class LoginSystem : MonoBehaviour
{
    public TMP_InputField userEmail, userPassword;
    public TextMeshProUGUI infoLabel;


    [Button]
    public void Login()
    {
        if (userPassword.text.Length < 6)
        {
            infoLabel.text = ("Password is too short");
            return;
        }        
        if (string.IsNullOrEmpty(userEmail.text))
        {
            infoLabel.text = ("Mail is not valid");
            return;
        }
        var Request = new LoginWithEmailAddressRequest { Email = userEmail.text, Password = userPassword.text, InfoRequestParameters= new GetPlayerCombinedInfoRequestParams {GetPlayerProfile=true } };
        PlayFabClientAPI.LoginWithEmailAddress(Request, OnLoginSuccess, OnError);

    }

    private void OnError(PlayFabError error)
    {
        throw new NotImplementedException();
    }

    private void OnLoginSuccess(LoginResult result)
    {
        string name = "";
        name = result.InfoResultPayload.PlayerProfile.DisplayName;
        infoLabel.text = ($"User{name} Logged In");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

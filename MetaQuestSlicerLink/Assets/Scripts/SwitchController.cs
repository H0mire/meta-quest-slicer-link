using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SwitchController : MonoBehaviour
{

    public GameObject openIGTLinkConnectHandler;

    public Toggle toggle;

    private OpenIGTLinkConnect openIGTLinkConnect;
    private Coroutine sendDataCoroutine;
    private Coroutine listenDataCoroutine;


    // Start is called before the first frame update
    void Start()
    {
        openIGTLinkConnect = openIGTLinkConnectHandler.GetComponent<OpenIGTLinkConnect>();
        StartCoroutine(ConnectToSlicerCoroutine());
    }

    // Update is called once per frame
    void Update()
    {

    }
    
    public void OnValueChanged(bool isOn)
    {
        // This function is called when the switch value changes
        if (isOn)
        {
            Debug.Log("Switch is ON");
            // Add code to handle the switch being turned on
            StartCoroutine(ConnectToSlicerCoroutine());

        }
        else
        {
            Debug.Log("Switch is OFF");
            // Add code to handle the switch being turned off
            
            // Stop the data transmission coroutines if they're running
            if (sendDataCoroutine != null)
            {
                StopCoroutine(sendDataCoroutine);
                sendDataCoroutine = null;
            }
            if (listenDataCoroutine != null)
            {
                StopCoroutine(listenDataCoroutine);
                listenDataCoroutine = null;
            }
            
            openIGTLinkConnect.OnDisconnectClick();
        }
    }

    private IEnumerator ConnectToSlicerCoroutine()
    {
        var task = openIGTLinkConnect.OnConnectToSlicerClick(openIGTLinkConnect.ipString, openIGTLinkConnect.port);
        yield return new WaitUntil(() => task.IsCompleted);
        
        // If connection was successful, start sending spine transform data
        if (task.Result)
        {
            Debug.Log("Connection successful! Starting to send spine transform data...");
            sendDataCoroutine = StartCoroutine(openIGTLinkConnect.SendSpineTransformInfo());
            listenDataCoroutine = StartCoroutine(openIGTLinkConnect.ListenSlicerInfo());
        }
        else
        {
            toggle.isOn = false; // Reset the toggle if connection fails
            Debug.Log("Connection failed. Not starting data transmission.");
        }
    }
}

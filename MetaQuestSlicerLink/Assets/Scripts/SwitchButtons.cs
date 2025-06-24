// This code was developed by Alicia Pose Díez de la Lastra, from Universidad Carlos III de Madrid
// This script creates all the functions associated to the switch buttons in the ControlPanel
// Adapted for Meta Quest using XR Interaction Toolkit

// First, import some libraries of interest
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using TMPro;

// Removed Microsoft.MixedReality.Toolkit references for Meta Quest compatibility

using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

// Define the SwitchButtons class
public class SwitchButtons : MonoBehaviour
{
    /// OPENIGTLINK CONTROL VARIABLES ///
    string ipString; // IP address of the computer running Slicer
    int port; // Port of the computer running Slicer
    Coroutine listeningRoutine; // Coroutine to control the listening part (3D Slicer -> Unity)
    Coroutine sendingRoutine; // Coroutine to control the sending part (Unity -> 3D Slicer)
    OpenIGTLinkConnect connectToServer; // Variable that connects to the OpenIGTLinkConnect script and enables the connection between Unity and 3D Slicer


    /// CONNECT TO SLICER ///
    XRBaseInteractable connectToSlicer_Switch; // Interactable behavior of the switch button that starts/stops the communication with 3D Slicer
    GameObject connectToSlicer_SwitchGO; // GameObject behavior of the switch button
    TextMesh connectToSlicer_label; // Label bellow the switch button that indicates if the client is correctly connected to the server


    /// CLIP SPINE ///
    XRBaseInteractable clipSpine_Switch; // Interactable behavior of the switch button that clips the spine with the image plane
    GameObject clipSpine_SwitchGO; // GameObject behavior of the switch button
    TextMeshPro clipSpine_label; // Label bellow the switch button that indicates if the spine is clipped
    [HideInInspector] public GameObject spineModel; // GameObject corresponding to the spine model
    [HideInInspector] public Material spine_mat; // Material of the spine when it is not clipped
    [HideInInspector] public Material clipping_mat; // Material of the spine when it is clipped


    /// SPINE VISIBILITY ///
    XRBaseInteractable spineVisibility_Switch; // Interactable behavior of the switch button that shows/hide the spine in the 3D view
    GameObject spineVisibility_SwitchGO; // GameObject behavior of the switch button
    TextMeshPro spineVisibility_label; // Label bellow the switch button that indicates if the spine visible
    Material visible_mat; // Material of the spine when it is visible
    Material invisible_mat; // Material of the spine when it is not visible


    /// SHOW IMAGE ///
    XRBaseInteractable imageVisibility_Switch; // Interactable behavior of the switch button that shows/hide the image in the 3D view
    TextMeshPro showImage_label; // Label bellow the switch button that indicates if the image is visible
    [HideInInspector] public GameObject mobileImageGO; // GameObject of the image that can be dragged along the spine
    GameObject fixedImageGO; // GameObject of the image next to the user interface
    XRBaseInteractable fixImage_Switch; // Interactable behavior of the switch button that en/unables the manipulation of the image plane in the 3D world
    TextMeshPro fixImage_label;
    [HideInInspector] public GameObject imageHandler;
    [HideInInspector] public Material imageHandlerMobile_mat;
    Material imageHandlerFixed_mat;

    
    // Toggle states for XR Interaction Toolkit compatibility
    bool connectToSlicerToggleState = false;
    bool clipSpineToggleState = false;
    bool spineVisibilityToggleState = true;
    bool imageVisibilityToggleState = false;
    bool fixImageToggleState = false;

    bool isConnected;
    void Start()
    {
        /// OPENIGTLINK CONTROL VARIABLES ///
        // Get the OpenIGTLinkConnect script holder and retrieve the ip and port set in the inspector
        connectToServer = GameObject.Find("OpenIGTLinkConnectHandler").GetComponent<OpenIGTLinkConnect>();
        ipString = connectToServer.ipString; // IP address of the computer running Slicer
        port = connectToServer.port; // Port of the computer running Slicer
    

        /// CONNECT TO SLICER ///
        // Get the switch button in the hierarchy and define functions to be executed when it is selected
        connectToSlicer_Switch = GameObject.Find("ControlPanel").transform.Find("ConnectivityButtons").transform.Find("ButtonCollection").transform.Find("ConnectToSlicerButton").GetComponent<XRBaseInteractable>();
        
        // Add event listeners for XR Interaction Toolkit
        connectToSlicer_Switch.selectEntered.AddListener(OnConnectToSlicerSelect);
        connectToSlicer_Switch.selectExited.AddListener(OnConnectToSlicerDeselect);

        // Change the label to Disconnected
        connectToSlicer_SwitchGO = connectToSlicer_Switch.gameObject;
        connectToSlicer_label = GameObject.Find("disConnectedLabel").GetComponent<TextMesh>();
        connectToSlicer_label.text = "Disconnected \nfrom Slicer";

        
        /// CLIP SPINE ///
        // Get the switch button in the hierarchy and define functions to be executed when it is interacted with
        clipSpine_Switch = GameObject.Find("ControlPanel").transform.Find("ImageButtons").transform.Find("ButtonCollection").transform.Find("ClipSpineSwitch").GetComponent<XRBaseInteractable>();
        clipSpine_Switch.selectEntered.AddListener(OnClipSpineSelect);

        clipSpine_SwitchGO = clipSpine_Switch.gameObject;
        // Change the label to OFF and deactivate the button. This button will only be enabled while the client is connected to the server
        clipSpine_label = GameObject.Find("ClipSpineLabel").GetComponent<TextMeshPro>();
        clipSpine_label.text = "Clip spine OFF";
        clipSpine_Switch.enabled = false;


        /// SPINE VISIBILITY ///
        // Get the switch button in the hierarchy and define functions to be executed when it is interacted with
        spineVisibility_Switch = GameObject.Find("ControlPanel").transform.Find("SpineButtons").transform.Find("ButtonCollection").transform.Find("SpineVisibilitySwitch").GetComponent<XRBaseInteractable>();
        spineVisibility_Switch.selectEntered.AddListener(OnSpineVisibilitySelect);
        
        spineVisibility_SwitchGO = spineVisibility_Switch.gameObject;
        // Change the label to ON (the spine is visible)
        spineVisibility_label = GameObject.Find("ShowSpineLabel").GetComponent<TextMeshPro>();
        spineVisibility_label.text = "Spine ON";
        // Load the invisible material from the path
        invisible_mat = Resources.Load("Materials/Invisible_mat") as Material;
        

        /// SHOW IMAGE ///
        // Get the switch button in the hierarchy and define functions to be executed when it is interacted with
        imageVisibility_Switch = GameObject.Find("ControlPanel").transform.Find("ImageButtons").transform.Find("ButtonCollection").transform.Find("ImageVisibilitySwitch").GetComponent<XRBaseInteractable>();
        imageVisibility_Switch.selectEntered.AddListener(OnImageVisibilitySelect);

        showImage_label = GameObject.Find("ShowImageLabel").GetComponent<TextMeshPro>();
        // Change the label to OFF and deactivate the button. This button will only be enabled while the client is connected to the server
        showImage_label.text = "Image OFF";
        imageVisibility_Switch.enabled = false;

        /// FIX IMAGE ///
        // Initialize image handler colors
        imageHandlerFixed_mat = Resources.Load("Materials/ImageFixed_mat") as Material; // Load the fixed image material
        // Get the switch button in the hierarchy and define functions to be executed when it is interacted with
        fixImage_Switch = GameObject.Find("ControlPanel").transform.Find("ImageButtons").transform.Find("ButtonCollection").transform.Find("FixImageSwitch").GetComponent<XRBaseInteractable>();
        fixImage_Switch.selectEntered.AddListener(OnFixImageSelect);

        fixedImageGO = GameObject.Find("ControlPanel").transform.Find("FixedImagePlane").gameObject;
        // Change the label to OFF and deactivate the button. This button will only be enabled while the client is connected to the server
        fixImage_label = GameObject.Find("FixImageLabel").GetComponent<TextMeshPro>();
        fixImage_label.text = "Fix image OFF";
        fixImage_Switch.enabled = false;
        
    }

    /// CONNECT TO SLICER EVENT HANDLERS ///
    void OnConnectToSlicerSelect(SelectEnterEventArgs args)
    {
        connectToSlicerToggleState = !connectToSlicerToggleState;
        if (connectToSlicerToggleState)
        {
            OnConnectToSlicerON();
        }
        else
        {
            OnConnectToSlicerOFF();
        }
    }

    void OnConnectToSlicerDeselect(SelectExitEventArgs args)
    {
        // Handle deselect if needed
    }

    /// CLIP SPINE EVENT HANDLERS ///
    void OnClipSpineSelect(SelectEnterEventArgs args)
    {
        clipSpineToggleState = !clipSpineToggleState;
        if (clipSpineToggleState)
        {
            OnClipSpineON();
        }
        else
        {
            OnClipSpineOFF();
        }
    }

    /// SPINE VISIBILITY EVENT HANDLERS ///
    void OnSpineVisibilitySelect(SelectEnterEventArgs args)
    {
        spineVisibilityToggleState = !spineVisibilityToggleState;
        if (spineVisibilityToggleState)
        {
            OnTurnModelON();
        }
        else
        {
            OnTurnModelOFF();
        }
    }

    /// IMAGE VISIBILITY EVENT HANDLERS ///
    void OnImageVisibilitySelect(SelectEnterEventArgs args)
    {
        imageVisibilityToggleState = !imageVisibilityToggleState;
        if (imageVisibilityToggleState)
        {
            OnShowImageON();
        }
        else
        {
            OnShowImageOFF();
        }
    }

    /// FIX IMAGE EVENT HANDLERS ///
    void OnFixImageSelect(SelectEnterEventArgs args)
    {
        fixImageToggleState = !fixImageToggleState;
        if (fixImageToggleState)
        {
            OnFixImageON();
        }
        else
        {
            OnFixImageOFF();
        }
    }

    /// CONNECT TO SLICER ///
    // This function is called everytime the user activates the connectivity switch
    void OnConnectToSlicerON()
    {
        // Start the connection with Slicer
        isConnected = connectToServer.OnConnectToSlicerClick(ipString, port);
        // If the connection is successful, continue
        if (isConnected)
        {
            // Change the label to "Connected", enable the rest of the switches in the UI and start the listening and sending coroutines
            connectToSlicer_label.text = "Connected \nto Slicer";
            clipSpine_Switch.enabled = true;
            fixImage_Switch.enabled = true;
            imageVisibility_Switch.enabled = true;
            imageVisibilityToggleState = true;
            listeningRoutine = StartCoroutine(connectToServer.ListenSlicerInfo());
            sendingRoutine = StartCoroutine(connectToServer.SendTransformInfo());
            mobileImageGO.SetActive(true);
        }
        // If the connection is unsuccesful, keep things as they were
        else
        {
            connectToSlicer_label.text = "Disconnected \nfrom Slicer";
            connectToSlicerToggleState = false;
        }
    }

    // This function is called everytime the user deactivates the connectivity switch
    void OnConnectToSlicerOFF()
    {
        // If there are any listening or sending coroutines active, stop them
        try
        {
            StopCoroutine(listeningRoutine);
        }
        catch { }
        try
        {
            StopCoroutine(sendingRoutine);
        }
        catch { }
        // Disconnect from the server
        connectToServer.OnDisconnectClick();
        // Change the label to "Disconnected"        
        connectToSlicer_label.text = "Disconnected \nfrom Slicer";
        // Disable the rest of switch buttons in the UI
        clipSpine_Switch.enabled = false;
        imageVisibility_Switch.enabled = false;
        fixImage_Switch.enabled = false;
    }

    
    /// CLIP SPINE ///
    // This function is called everytime the user activates the clipping tool
    void OnClipSpineON()
    {
        // Assign the clipping material to the spine. This material is already associated to the image plane, by definition
        spineModel.GetComponentInChildren<MeshRenderer>().material = clipping_mat;
        clipSpine_label.text = "Clip spine ON";
    }
    // This function is called everytime the user deactivates the clipping tool
    void OnClipSpineOFF()
    {
        // Assign the spine material to the spine (no clipping)
        spineModel.GetComponentInChildren<MeshRenderer>().material = spine_mat;
        clipSpine_label.text = "Clip spine OFF";
    }

    /// SPINE VISIBILITY ///
    // This function is called everytime the user activates the spine visibility switch button
    void OnTurnModelON()
    {
        // Assign the visible material to the spine
        spineModel.GetComponentInChildren<MeshRenderer>().material = visible_mat;
        // Update the button label
        spineVisibility_label.text = "Spine ON";
    }

    // This function is called everytime the user deactivates the spine visibility switch button
    void OnTurnModelOFF()
    {
        // Assign the non-visible material to the spine
        visible_mat = spineModel.GetComponentInChildren<MeshRenderer>().material; // the visible material could be spine_mat or clipping_mat
        spineModel.GetComponentInChildren<MeshRenderer>().material = invisible_mat;
        // Update the button label
        spineVisibility_label.text = "Spine OFF";
    }

    /// SHOW IMAGE ///
    // This function is called everytime the user activates the image visibility switch button
    void OnShowImageON()
    {
        // Set the both images visibilities to true
        mobileImageGO.SetActive(true);
        fixedImageGO.SetActive(true);
        // Start the listening coroutine
        listeningRoutine = StartCoroutine(connectToServer.ListenSlicerInfo());
        // Enable the other switch buttons
        clipSpine_Switch.enabled = true;
        fixImage_Switch.enabled = true;
        // Update the label
        showImage_label.text = "Image ON";
    }

    // This function is called everytime the user deactivates the image visibility switch button
    void OnShowImageOFF()
    {
        // Set the both images visibilities to false
        mobileImageGO.SetActive(false);
        fixedImageGO.SetActive(false);
        // If the listening routine was running, stop it
        try{
            StopCoroutine(listeningRoutine);
        }
        catch { }
        // Since we don't see the image anymore, also stop the clipping of the spine
        OnClipSpineOFF();
        // Disable all the buttons associated to the image plane
        clipSpine_Switch.enabled = false;
        clipSpineToggleState = false;
        fixImage_Switch.enabled = false;
        fixImageToggleState = false;
        // Update the show image label
        showImage_label.text = "Image OFF";
        // Assign the spine_mat to the spine (in case it has the clipping mat)
        spineModel.GetComponentInChildren<MeshRenderer>().material = spine_mat;
    }

    // This function is called everytime the user fixes the image plane in the 3D world using the corresponding switch button
    void OnFixImageON()
    {
        // Get the manage models script
        PressableButtons manageModelsScript = GameObject.Find("Models").GetComponent<PressableButtons>();
        // Make the object non-manipulable
        manageModelsScript.MakeObjectManipulable(mobileImageGO, false);
        // Change the color of the image handler accordingly
        imageHandler.GetComponent<MeshRenderer>().material = imageHandlerFixed_mat;
        // Update the button label
        fixImage_label.text = "Fix image ON";
    }

    void OnFixImageOFF()
    {
        // Get the manage models script
        PressableButtons manageModelsScript = GameObject.Find("Models").GetComponent<PressableButtons>();
        // Make the object manipulable
        manageModelsScript.MakeObjectManipulable(mobileImageGO, true);
        // Change the color of the image handler accordingly
        imageHandler.GetComponent<MeshRenderer>().material = imageHandlerMobile_mat;
        // Update the button label
        fixImage_label.text = "Fix image OFF";
    }
    
}



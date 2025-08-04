// This code is based on the one provided in: https://github.com/franklinwk/OpenIGTLink-Unity
// Modified by Alicia Pose Díez de la Lastra, from Universidad Carlos III de Madrid
// Updated for Meta Quest (Android) compatibility with async/await pattern
// Simplified to only send spine model transforms - no image reception or multiple model support

using UnityEngine;
using System;
using System.Collections;
using System.Threading.Tasks;
using TMPro;



public class OpenIGTLinkConnect : MonoBehaviour
{
    ///////// CONNECT TO 3D SLICER PARAMETERS /////////
    uint headerSize = 58; // Size of the header of every OpenIGTLink message
    private SocketHandler socketForUnityAndMetaQuest; // Socket to connect to Slicer
    bool isConnected; // Boolean to check if the socket is connected
    public string ipString; // IP address of the computer running Slicer
    public int port; // Port of the computer running Slicer
    

    ///////// GENERAL VARIABLES /////////
    int scaleMultiplier = 1000; // Help variable to transform meters to millimeters and vice versa
    
    ///////// SEND - SPINE MODEL ONLY /////////
    public GameObject spineModel; // The spine model to track and send to Slicer
    
    ///////// UI ELEMENTS /////////
    public TextMeshProUGUI connectionStatusText; // Reference to TextMeshPro component to display connection status
    
    /// CRC ECMA-182 to send messages to Slicer ///
    CRC64 crcGenerator;
    string CRC;
    ulong crcPolynomial;
    string crcPolynomialBinary = "0100001011110000111000011110101110101001111010100011011010010011";

    

    void Start()
    {
        // Initialize connection status text
        UpdateConnectionStatusText("Disconnected");
        
        // Initialize CRC Generator for message transmission
        crcGenerator = new CRC64();
        crcPolynomial = Convert.ToUInt64(crcPolynomialBinary, 2);
        crcGenerator.Init(crcPolynomial);
        
        // Connect to Slicer (fire and forget - status will be updated via UI callbacks)
        //_ = ConnectToSlicer(ipString, port);
    }

    // This function is called when the user activates the connectivity switch to start the communication with 3D Slicer
    public async Task<bool> OnConnectToSlicerClick(string ipString, int port)
    {
        UpdateConnectionStatusText("Connecting...");
        isConnected = await ConnectToSlicer(ipString, port);
        
        if (isConnected)
        {
            UpdateConnectionStatusText("Connected");
        }
        else
        {
            UpdateConnectionStatusText("Connection Failed");
        }
        
        return isConnected;
    }

    // Create a new socket handler and connect it to the server with the ip address and port provided in the function
    async Task<bool> ConnectToSlicer(string ipString, int port)
    {
        socketForUnityAndMetaQuest = new SocketHandler();

        Debug.Log("ipString: " + ipString);
        Debug.Log("port: " + port);
        bool isConnected = await socketForUnityAndMetaQuest.Connect(ipString, port);
        Debug.Log("Connected: " + isConnected);

        // Update the connection status based on result
        if (isConnected)
        {
            UpdateConnectionStatusText("Connected");
        }
        else
        {
            UpdateConnectionStatusText("Connection Failed");
        }

        return isConnected;
        
    }

    // Routine that continuously sends the transform information of the spine model to 3D Slicer
    public IEnumerator SendSpineTransformInfo()
    {
        while (true)
        {
            Debug.Log("Sending spine transform...");
            yield return null; // If you had written yield return new WaitForSeconds(1); it would have waited 1 second before executing the code below.
            
            // Send only the spine model transform
            if (spineModel != null)
            {
                // Get or create ModelInfo component for the spine model
                ModelInfo spineInfo = spineModel.GetComponent<ModelInfo>();
                if (spineInfo == null)
                {
                    spineInfo = spineModel.AddComponent<ModelInfo>();
                    spineInfo._name = "Spine";
                    spineInfo._number = 1;
                    spineInfo._color = "white";
                    spineInfo._diameter = "0";
                    spineInfo._length = "0";
                    spineInfo._gameObject = spineModel;
                }
                
                SendMessageToServer.SendTransformMessage(spineInfo, scaleMultiplier, crcGenerator, CRC, socketForUnityAndMetaQuest);
            }
        }
    }

    // Routine that continuously listens to incoming transform information from 3D Slicer and applies it to the spine model
    public IEnumerator ListenSlicerInfo()
    {
        while (true)
        {
            Debug.Log("Listening for spine transforms...");
            yield return null;

            ////////// READ THE HEADER OF THE INCOMING MESSAGES //////////
            // Use a wrapper to handle async call in coroutine
            bool headerReceived = false;
            byte[] iMSGbyteArray = null;
            
            StartCoroutine(ListenForHeader((result) => {
                iMSGbyteArray = result;
                headerReceived = true;
            }));
            
            // Wait for the async operation to complete
            yield return new WaitUntil(() => headerReceived);
            
            
            if (iMSGbyteArray != null && iMSGbyteArray.Length >= (int)headerSize)
            {
                ////////// READ THE HEADER OF THE INCOMING MESSAGES //////////
                // Store the information of the header in the structure iHeaderInfo
                ReadMessageFromServer.HeaderInfo iHeaderInfo = ReadMessageFromServer.ReadHeaderInfo(iMSGbyteArray);

                // Check if we got a valid header (bodySize > 0 indicates a valid header)
                if (iHeaderInfo.bodySize > 0)
                {
                    ////////// READ THE BODY OF THE INCOMING MESSAGES //////////
                    // Get the size of the body from the header information
                    uint bodySize = Convert.ToUInt32(iHeaderInfo.bodySize); 
                    
                    // Process the message when it is complete (that means, we have received as many bytes as the body size + the header size)
                    if (iMSGbyteArray.Length >= (int)bodySize + (int)headerSize)
                    {
                        // Only process transform messages for the spine model
                        if ((iHeaderInfo.msgType).Contains("TRANSFORM"))
                        {
                            // Extract the transform matrix from the message
                            Matrix4x4 matrix = ReadMessageFromServer.ExtractTransformInfo(iMSGbyteArray, spineModel, scaleMultiplier, (int)iHeaderInfo.headerSize);
                            // Apply the transform matrix to the spine model
                            ApplyTransformToSpineModel(matrix, spineModel);
                        }
                        // Note: Image messages are ignored in this spine-only version
                    }
                }
                else
                {
                    Debug.LogWarning("Received invalid or incomplete header, skipping message.");
                }
            }
        }
    }
    
    // Helper coroutine to handle async Listen operation
    private IEnumerator ListenForHeader(System.Action<byte[]> callback)
    {
        var task = socketForUnityAndMetaQuest.Listen(headerSize);
        yield return new WaitUntil(() => task.IsCompleted);
        callback(task.Result);
    }
    
    /// Apply transform information to the spine model ///
    void ApplyTransformToSpineModel(Matrix4x4 matrix, GameObject gameObject)
    {
        if (gameObject == null) return;
        
        Vector3 translation = matrix.GetColumn(3);
        
        // Check if translation values are within reasonable limits
        if (translation.x > 10000 || translation.y > 10000 || translation.z > 10000)
        {
            gameObject.transform.position = new Vector3(0, 0, 0.5f);
            Debug.Log("Transform out of limits. Default position assigned to spine model.");
        }
        else
        {
            // Apply the transform to the spine model
            gameObject.transform.localPosition = new Vector3(-translation.x, translation.y, translation.z);
            Vector3 rotation = matrix.rotation.eulerAngles;
            gameObject.transform.localRotation = Quaternion.Euler(rotation.x, -rotation.y, -rotation.z);
        }
    }

    // Called when the user disconnects Unity from 3D Slicer using the connectivity switch
    public void OnDisconnectClick()
    {
        socketForUnityAndMetaQuest.Disconnect();
        isConnected = false;
        UpdateConnectionStatusText("Disconnected");
        Debug.Log("Disconnected from the server");
    }

    // Update the connection status text
    private void UpdateConnectionStatusText(string status)
    {
        if (connectionStatusText != null)
        {
            connectionStatusText.text = $"Connection Status: {status}";
        }
    }


    // Execute this function when the user exits the application
    void OnApplicationQuit()
    {
        // Release the socket.
        if (socketForUnityAndMetaQuest != null)
        {
            socketForUnityAndMetaQuest.Disconnect();
        }
    }
}

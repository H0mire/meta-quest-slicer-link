// This code is based on the one provided in: https://github.com/franklinwk/OpenIGTLink-Unity
// Modified by Alicia Pose Díez de la Lastra, from Universidad Carlos III de Madrid

using UnityEngine;
using System;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Collections;
using System.Threading;
using System.Collections.Generic;
using UnityEngine.UI;

public class OpenIGTLinkConnect : MonoBehaviour
{
    ///////// CONNECT TO 3D SLICER PARAMETERS /////////
    uint headerSize = 58; // Size of the header of every OpenIGTLink message
    private SocketHandler socketForUnityAndHoloLens; // Socket to connect to Slicer
    bool isConnected; // Boolean to check if the socket is connected
    public string ipString; // IP address of the computer running Slicer
    public int port; // Port of the computer running Slicer

    ///////// GENERAL VARIABLES /////////
    int scaleMultiplier = 1000; // Help variable to transform meters to millimeters and vice versa

    ///////// SEND /////////
    public List<ModelInfo> infoToSend; // Array of Models to send to Slicer

    /// CRC ECMA-182 to send messages to Slicer ///
    CRC64 crcGenerator;
    string CRC;
    ulong crcPolynomial;
    string crcPolynomialBinary = "0100001011110000111000011110101110101001111010100011011010010011";

    ///////// LISTEN /////////

    /// Image transfer information ///
    [HideInInspector] public GameObject movingPlane; // Plane to display image on
    Material mediaMaterial; // Material of the plane
    Texture2D mediaTexture; // Texture of the plane

    GameObject fixPlane; // Fix plane to display image on
    Material fixPlaneMaterial; // Material of the plane

    void Start()
    {
        // Initialize CRC Generator
        crcGenerator = new CRC64();
        crcPolynomial = Convert.ToUInt64(crcPolynomialBinary, 2);
        crcGenerator.Init(crcPolynomial);

        // Initialize texture parameters for image transfer of the moving plane
        movingPlane.transform.localScale = Vector3.Scale(transform.localScale, new Vector3(movingPlane.transform.localScale.x, -movingPlane.transform.localScale.y, movingPlane.transform.localScale.z));
        mediaMaterial = movingPlane.GetComponent<MeshRenderer>().material;
        mediaTexture = new Texture2D(512, 512, TextureFormat.Alpha8, false);
        mediaMaterial.mainTexture = mediaTexture;

        // Initialize texture parameters for image transfer of the fix plane
        fixPlane = GameObject.Find("FixedImagePlane").transform.Find("FixPlane").gameObject;
        fixPlane.transform.localScale = Vector3.Scale(transform.localScale, new Vector3(fixPlane.transform.localScale.x, -fixPlane.transform.localScale.y, fixPlane.transform.localScale.z));
        fixPlaneMaterial = fixPlane.GetComponent<MeshRenderer>().material;
        fixPlaneMaterial.mainTexture = mediaTexture;
    }

    public bool OnConnectToSlicerClick(string ipString, int port)
    {
        isConnected = ConnectToSlicer(ipString, port);
        return isConnected;
    }

    bool ConnectToSlicer(string ipString, int port)
    {
        socketForUnityAndHoloLens = new SocketHandler();

        Debug.Log("ipString: " + ipString);
        Debug.Log("port: " + port);
        bool isConnected = socketForUnityAndHoloLens.Connect(ipString, port);
        Debug.Log("Connected: " + isConnected);

        return isConnected;
    }

    public IEnumerator SendTransformInfo()
    {
        while (true)
        {
            Debug.Log("Sending...");
            yield return null;
            foreach (ModelInfo element in infoToSend)
            {
                SendMessageToServer.SendTransformMessage(element, scaleMultiplier, crcGenerator, CRC, socketForUnityAndHoloLens);
            }
        }
    }

    public IEnumerator ListenSlicerInfo()
    {
        while (true)
        {
            Debug.Log("Listening...");
            yield return null;

            byte[] iMSGbyteArray = socketForUnityAndHoloLens.Listen(headerSize);

            if (iMSGbyteArray.Length >= (int)headerSize)
            {
                ReadMessageFromServer.HeaderInfo iHeaderInfo = ReadMessageFromServer.ReadHeaderInfo(iMSGbyteArray);

                uint bodySize = Convert.ToUInt32(iHeaderInfo.bodySize);

                if (iMSGbyteArray.Length >= (int)bodySize + (int)headerSize)
                {
                    if ((iHeaderInfo.msgType).Contains("TRANSFORM"))
                    {
                        Matrix4x4 matrix = ReadMessageFromServer.ExtractTransformInfo(iMSGbyteArray, movingPlane, scaleMultiplier, (int)iHeaderInfo.headerSize);
                        ApplyTransformToGameObject(matrix, movingPlane);
                    }
                    else if ((iHeaderInfo.msgType).Contains("IMAGE"))
                    {
                        ApplyImageInfo(iMSGbyteArray, iHeaderInfo);
                    }
                }
            }
        }
    }

    void ApplyTransformToGameObject(Matrix4x4 matrix, GameObject gameObject)
    {
        Vector3 translation = matrix.GetColumn(3);
        if (translation.x > 10000 || translation.y > 10000 || translation.z > 10000)
        {
            gameObject.transform.position = new Vector3(0, 0, 0.5f);
            Debug.Log("Out of limits. Default position assigned.");
        }
        else
        {
            gameObject.transform.localPosition = new Vector3(-translation.x, translation.y, translation.z);
            Vector3 rotation = matrix.rotation.eulerAngles;
            gameObject.transform.localRotation = Quaternion.Euler(rotation.x, -rotation.y, -rotation.z);
        }
    }

    void ApplyImageInfo(byte[] iMSGbyteArray, ReadMessageFromServer.HeaderInfo iHeaderInfo)
    {
        ReadMessageFromServer.ImageInfo iImageInfo = ReadMessageFromServer.ReadImageInfo(iMSGbyteArray, headerSize, iHeaderInfo.extHeaderSize);

        if (iImageInfo.numPixX > 0 && iImageInfo.numPixY > 0)
        {
            mediaMaterial = movingPlane.GetComponent<MeshRenderer>().material;
            mediaTexture = new Texture2D(iImageInfo.numPixX, iImageInfo.numPixY, TextureFormat.Alpha8, false);

            fixPlaneMaterial = fixPlane.GetComponent<MeshRenderer>().material;

            byte[] bodyArray_iImData = new byte[iImageInfo.numPixX * iImageInfo.numPixY];
            byte[] bodyArray_iImDataInv = new byte[bodyArray_iImData.Length];

            Buffer.BlockCopy(iMSGbyteArray, iImageInfo.offsetBeforeImageContent, bodyArray_iImData, 0, bodyArray_iImData.Length);

            for (int i = 0; i < bodyArray_iImData.Length; i++)
            {
                bodyArray_iImDataInv[i] = (byte)(255 - bodyArray_iImData[i]);
            }

            mediaTexture.LoadRawTextureData(bodyArray_iImDataInv);
            mediaTexture.Apply();
            mediaMaterial.mainTexture = mediaTexture;

            fixPlaneMaterial.mainTexture = mediaTexture;
        }
    }

    public void OnDisconnectClick()
    {
        socketForUnityAndHoloLens.Disconnect();
        Debug.Log("Disconnected from the server");
    }

    void OnApplicationQuit()
    {
        if (socketForUnityAndHoloLens != null)
        {
            socketForUnityAndHoloLens.Disconnect();
        }
    }
}

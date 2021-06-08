using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using maxstAR;

public class VPSCameraImageController : MonoBehaviour
{
    bool isLoaded = false;
    private string url = "https://vpsstudio.s3.ap-northeast-2.amazonaws.com/images/";
    public void LoadImage(string fileName, System.Action<Texture2D> complete)
    {
        if(isLoaded)
        {
            return;
        }

        VPSStudioController vPSStudioController = FindObjectOfType<VPSStudioController>();

        if (VPSStudioController.vpsName == "")
        {
            if(vPSStudioController != null)
            {
                vPSStudioController.ReloadName();
            }
        }

        isLoaded = true;

        string fileUrl = url + VPSStudioController.vpsName + "/" + fileName;

        StartCoroutine(loadRawImageFromWWW(fileUrl, (result) =>
        {
            isLoaded = false;
            GetComponent<Renderer>().sharedMaterial.mainTexture = result;
            complete(result);
        }));
    }

    public static IEnumerator loadRawImageFromWWW(string path, System.Action<Texture2D> complete)
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(path);

        yield return www.SendWebRequest();
        if (www.isNetworkError || www.isHttpError)
        { 
            Debug.Log(www.error);
            complete(null);
        }
        else
        {
            Texture2D downedTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;
            if(complete != null)
            {
                complete(downedTexture);
            }
        }
    }
}

using AL.NOISE.InputSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public class SaveToPNG : MonoBehaviour
{
    WaveInput _MainInput;
    [SerializeField] private Camera _MainCamera;
    RenderTexture _TargetTexture;
    Texture2D _TargetTexture2D;

    private void Awake()
    {
        _MainInput = new WaveInput();
        _MainInput.Enable();
    }
    // Start is called before the first frame update
    void Start()
    {
        _MainInput.WaveController.TakeScreenShot.performed += ScreenShot;
        _TargetTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
        _TargetTexture2D = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, false, true);
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void ScreenShot(InputAction.CallbackContext context)
    {
        _MainCamera.targetTexture = _TargetTexture;

        _MainCamera.Render();

        var trt = RenderTexture.active;
        RenderTexture.active = _TargetTexture;
        _TargetTexture2D.ReadPixels(new Rect(0,0,_TargetTexture.width, _TargetTexture.height), 0, 0);
        RenderTexture.active = trt;

        _TargetTexture2D.Apply();

        byte[] image = _TargetTexture2D.EncodeToPNG();

        var dirPath = Application.dataPath + "/../ScreenShots/";
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }

        dirPath = dirPath + "Image" + UnityEngine.Random.Range(152, 55541) + ".png";

        File.WriteAllBytes(dirPath, image);

        Debug.Log("Saved to: " + dirPath);

        _MainCamera.targetTexture = null;
    }

    private void OnDisable()
    {
        _MainInput.Disable();
    }
}

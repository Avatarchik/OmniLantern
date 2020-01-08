
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Xml.Serialization;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ArucoModule;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;

public class ImageInit : ProjectionTarget {

  protected Mat MatToProcess;
  protected Mat pre_Mat;
  protected Texture TexToProcess;

  [Space(10)]

  public MarkerDetection markerDetection;
  public FaceDetection faceDetection;
  public ShadeTouch shadeTouch;
  public HandDetection handDetection;

  [Space(10)]
  // some problem with inherit of ptr, so I made a new one
  //protected new OmniProCamDeviceManager omniProCamDeviceManagerPtr;

  [Space(10)]
  // for using the web camera
  public bool UseIRCamera = true;
  private WebCamTexture webTex;
  private string cameraName = "";
  private bool isPlay = true;
  const int WebCameraSize = 760;
  const int WebCameraFPS = 15;

  // for different modes
  public bool isMarkerDetection;
  public bool isFaceDetection;
  public bool isFaceThreshold;
  public bool isShade;
  public bool isHandInteraction;


  // for thresholding
  public float faceThreshold = 50;
  protected bool StartTouching = false;

void Start() {

    if (!UseIRCamera) {
      webTex = new WebCamTexture();
      StartCoroutine(Test());
    } else {
      omniProCamDeviceManagerPtr = GameObject.Find("OmniProCam/LibOmniProCamManager").GetComponent<OmniProCamDeviceManager>();
    }

  }

  IEnumerator Test() {
    yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
    if (Application.HasUserAuthorization(UserAuthorization.WebCam)) {
      WebCamDevice[] devices = WebCamTexture.devices;
      cameraName = devices[0].name;
      webTex = new WebCamTexture(cameraName, WebCameraSize, WebCameraSize, WebCameraFPS);
      webTex.Play();
      isPlay = true;
    }
  }


  void Update() {
    /* スケールの微調整 */
    if (!Input.GetKey(KeyCode.O)) {

      /* 縦スケールの微調整 */
      if (Input.GetKey(KeyCode.E)) {
        scale.y += 0.1f * Time.deltaTime;
        transform.localScale = scale;
      } else if (Input.GetKey(KeyCode.D)) {
        scale.y -= 0.1f * Time.deltaTime;
        transform.localScale = scale;
      }
      /* 縦スケールの微調整 */
      if (Input.GetKey(KeyCode.F)) {
        scale.x += 0.1f * Time.deltaTime;
        transform.localScale = scale;
      } else if (Input.GetKey(KeyCode.A)) {
        scale.x -= 0.1f * Time.deltaTime;
        transform.localScale = scale;
      }

      /* オフセットの設定 */
    } else {

      /* 縦オフセットの微調整 */
      if (Input.GetKey(KeyCode.E)) {
        offset.y += 0.05f * Time.deltaTime;
        transform.position = new Vector3(offset.x, 0.0f, offset.y);
      } else if (Input.GetKey(KeyCode.D)) {
        offset.y -= 0.05f * Time.deltaTime;
        transform.position = new Vector3(offset.x, 0.0f, offset.y);
      }

      /* 横オフセットの微調整 */
      if (Input.GetKey(KeyCode.F)) {
        offset.x += 0.05f * Time.deltaTime;
        transform.position = new Vector3(offset.x, 0.0f, offset.y);
      } else if (Input.GetKey(KeyCode.A)) {
        offset.x -= 0.05f * Time.deltaTime;
        transform.position = new Vector3(offset.x, 0.0f, offset.y);
      }
    }



    /* 2値化閾値 */
    if (Input.GetKey(KeyCode.UpArrow)) {
      binarizingThreshold += 1.0f;
      if (binarizingThreshold >= 255.0f) {
        binarizingThreshold = 255;
      }
      LibOmniProCam.setBinarizingThreshold(binarizingThreshold);
      print("BinarizingThreshold: " + binarizingThreshold);
    } else if (Input.GetKey(KeyCode.DownArrow)) {
      binarizingThreshold -= 1.0f;
      if (binarizingThreshold <= 0.0f) {
        binarizingThreshold = 0;
      }
      LibOmniProCam.setBinarizingThreshold(binarizingThreshold);
      print("BinarizingThreshold: " + binarizingThreshold);
    }



    /* 書き出し */
    //Use "0" Can change the image between src &binarized
    if (Input.GetKeyDown(KeyCode.Alpha0)) {
      if (omniProCamDeviceManagerPtr.getCameraTextureType() == CameraTextureType.CAMERA_TEXTURE_TYPE_BINARIZED) {
        omniProCamDeviceManagerPtr.setCameraTextureType(CameraTextureType.CAMERA_TEXTURE_TYPE_SRC);
        print("CameraTextureType: Src");
      } else {
        omniProCamDeviceManagerPtr.setCameraTextureType(CameraTextureType.CAMERA_TEXTURE_TYPE_BINARIZED);
        print("CameraTextureType: Binarized");
      }
    }

    
    
    // Using binary image for shade and threshold
    if (isFaceThreshold){
      omniProCamDeviceManagerPtr.setCameraTextureType(CameraTextureType.CAMERA_TEXTURE_TYPE_BINARIZED);
      binarizingThreshold = faceThreshold;
      LibOmniProCam.setBinarizingThreshold(binarizingThreshold);
    }




    //OmniProCamData[] ptr = omniProCamDeviceManagerPtr.getOmniProCamDataArrayPtr();
    //foreach (OmniProCamData i in ptr) {
    //  if (i.id != 0) {
    //  }
    //}

    ///* MlioLight Application */
    //if (omniProCamDeviceManagerPtr.getCameraTextureType() == CameraTextureType.CAMERA_TEXTURE_TYPE_BINARIZED) {
    //  meshRenderer.material.SetTexture("_AlphaTex0", omniProCamDeviceManagerPtr.getBinarizedCameraImageTexturePtr().texture);
    //} else {
    //  meshRenderer.material.SetTexture("_AlphaTex0", omniProCamDeviceManagerPtr.getSrcCameraImageTexturePtr().texture);
    //}


    /*Image Proseeing Part*/

    /* if use the IR camera with FlyCapture SDk */
    if (UseIRCamera) {
      //convert from tex to tex2d
      if (omniProCamDeviceManagerPtr.getCameraTextureType() == CameraTextureType.CAMERA_TEXTURE_TYPE_BINARIZED) {
        TexToProcess = omniProCamDeviceManagerPtr.getBinarizedCameraImageTexturePtr().texture;
      } else if(omniProCamDeviceManagerPtr.getCameraTextureType() == CameraTextureType.CAMERA_TEXTURE_TYPE_SRC) {
        TexToProcess = omniProCamDeviceManagerPtr.getSrcCameraImageTexturePtr().texture;
      }


      Texture2D Tex2dImg = new Texture2D(TexToProcess.width, TexToProcess.height);
      Utils.textureToTexture2D(TexToProcess, Tex2dImg);

      //convert the tex2d to mat
      MatToProcess = new Mat(TexToProcess.height, TexToProcess.width, CvType.CV_8UC4, new Scalar(0, 0, 0, 255));
      Utils.texture2DToMat(Tex2dImg, MatToProcess);


      //flip the image for projection
      // -1 means both x and y
      Core.flip(MatToProcess, MatToProcess, -1);

      //Process for marker detection
      if(isMarkerDetection){
        markerDetection.MarkerProcessing(MatToProcess);
      } 
      //Process for  face detction (can't work in IR camera now)
      else if (isFaceDetection) {
        faceDetection.Run(MatToProcess);
      } 
      else if (isFaceThreshold){
        Core.flip(MatToProcess, MatToProcess, -1);
        Core.bitwise_not(MatToProcess,MatToProcess);
      } 
      else if (isShade){
        //flip back for projection inside
        Core.flip(MatToProcess, MatToProcess, -1);
        Imgproc.cvtColor(MatToProcess,MatToProcess,6);

        //press Q to catch the background frame
        if(Input.GetKey(KeyCode.Q)) {
          pre_Mat = MatToProcess;
          StartTouching = true;
        }
        if(StartTouching){
        MatToProcess =  shadeTouch.TouchProcessing(MatToProcess,pre_Mat);
        }
      }

      // convert to texture and show
      Texture2D ResultTex = new Texture2D(MatToProcess.cols(), MatToProcess.rows(), TextureFormat.RGB24, false);
      Utils.matToTexture2D(MatToProcess, ResultTex);

      //show the image
      gameObject.GetComponent<Renderer>().material.mainTexture = ResultTex;

    }






    /*if use the noremal web camera*/
    else if (!UseIRCamera && isPlay) {
      MatToProcess = new Mat(webTex.height, webTex.width, CvType.CV_8UC4, new Scalar(0, 0, 0, 255));
      Utils.webCamTextureToMat(webTex, MatToProcess);

      if (isFaceDetection) {
        faceDetection.Run(MatToProcess);
      }

      Texture2D ResultTex = new Texture2D(MatToProcess.cols(), MatToProcess.rows(), TextureFormat.RGB24, false);
      Utils.matToTexture2D(MatToProcess, ResultTex);

      gameObject.GetComponent<Renderer>().material.mainTexture = ResultTex;
    }
  }
  
}


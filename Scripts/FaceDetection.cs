using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;


public class FaceDetection : MonoBehaviour {
  CascadeClassifier cascade;
  // file name
  protected static readonly string HAAR_CASCADE_FILENAME = "haarcascade_frontalface_alt.xml";


  // Use this for initialization
  void Start () {
    cascade = new CascadeClassifier();
    cascade.load(Utils.getFilePath(HAAR_CASCADE_FILENAME));
    if (cascade.empty()) {
      Debug.LogError("cascade file is not loaded. Please copy from “OpenCVForUnity/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
    }
  }
	
	// Update is called once per frame
	public void Run (Mat imgMat) {
    Mat grayMat = new Mat();
    Imgproc.cvtColor(imgMat, grayMat, Imgproc.COLOR_RGBA2GRAY);
    Imgproc.equalizeHist(grayMat, grayMat);


    MatOfRect faces = new MatOfRect();

    if (cascade != null)
      cascade.detectMultiScale(grayMat, faces, 1.1, 2, 2,
          new Size(20, 20), new Size());

    OpenCVForUnity.CoreModule.Rect[] rects = faces.toArray();
    for (int i = 0; i < rects.Length; i++) {
      Debug.Log("detect faces " + rects[i]);

      Imgproc.rectangle(imgMat, new Point(rects[i].x, rects[i].y), new Point(rects[i].x + rects[i].width, rects[i].y + rects[i].height), new Scalar(255, 0, 0, 255), 2);
    }
  }
}

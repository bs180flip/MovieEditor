// using UnityEngine;
// using System.Collections;
// using OpenCVForUnity.CoreModule;
// using OpenCVForUnity.ImgprocModule;
// using OpenCVForUnity.UnityUtils;
// using UnityEngine.EventSystems;
// using System.Collections.Generic;
// using OpenCVForUnity.TrackingModule;
// using System.Linq;
// using UnityEngine.SceneManagement;
// using OpenCVForUnity.VideoioModule;
// using OpenCVForUnity.ObjdetectModule;
// using UnityEngine.UI;


// #if UNITY_5_3 || UNITY_5_3_OR_NEWER
// using UnityEngine.SceneManagement;
// #endif
// using OpenCVForUnity;

// using UnityEngine.Video;

// namespace OpenCVForUnityExample
// {
//     /// <summary>
//     /// VideoCapture example.
//     /// </summary>
//     public class VideoPlayerWithOpenCVForUnityExample : MonoBehaviour
//     {

//         [SerializeField]
//         private UnityEngine.UI.Image[] _masks;

//         [SerializeField]
//         Camera _camera = null;

//         [SerializeField]
//         GameObject _prefab = null;

//         [SerializeField]
//         Transform _canvasOject = null;

//         /// <summary>
//         /// The video player.
//         /// </summary>
//         VideoPlayer _videoPlayer;

//         /// <summary>
//         /// The rgba mat.
//         /// </summary>
//         Mat rgbaMat;

//         /// <summary>
//         /// The colors.
//         /// </summary>
//         Color32[] colors;

//         /// <summary>
//         /// The texture.
//         /// </summary>
//         Texture2D texture;

//         /// <summary>
//         /// The video texture.
//         /// </summary>
//         Texture2D videoTexture;

//         Point storedTouchPoint;

//         /// <summary>
//         /// The tracking color list.
//         /// </summary>
//         List<Scalar> trackingColorList;

//         /// <summary>
//         /// The selected point list.
//         /// </summary>
//         List<Point> selectedPointList;

//         /// <summary>
//         /// The trackers.
//         /// </summary>
//         MultiTracker trackers;

//         MatOfRect2d objects;

//         AudioSource audioSouce;

//         Texture2D srcTexture = null;



//         /// </summary>
//         VideoWriter writer;

//         Texture2D screenCapture;

//         Mat recordingFrameRgbMat;

//         CascadeClassifier cascade;

//         MatOfRect faces;

//         Mat grayMat;

//         /// <summary>
//         /// The selected point list.
//         /// </summary>
//         List<Vector3> _screenPositionList = new List<Vector3>();

//         private MovieGenerater _recorder;

//         /// <summary>
//         /// LBP_CASCADE_FILENAME
//         /// </summary>
//         protected static readonly string LBP_CASCADE_FILENAME = "lbpcascade_frontalface.xml";

// #if UNITY_WEBGL && !UNITY_EDITOR
//         IEnumerator getFilePath_Coroutine;
// #endif
//         private Vector2 _pos = new Vector2(320, 200);

//         //private Vector2 _pos = new Vector2(350, 220);



//         private bool isPrepare { get; set; } = false;

//         string savePath;

//         private int _maskCount = 0;

//         private bool isDispose = false;


//         int width = 0;

//         int height = 0;


//         Vector3 screenPosition;

//         // Use this for initialization
//         public void Init(string path, bool toggleEnable)
//         {

//             _recorder = MovieGenerater.Instance;

//             _maskCount = 0;
//             //Debug.unityLogger.logEnabled = false;

//             _videoPlayer = GetComponent<VideoPlayer>();
//             audioSouce = GetComponent<AudioSource>();

//             audioSouce.playOnAwake = false;

//             _videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
//             _videoPlayer.EnableAudioTrack(0, true);
//             _videoPlayer.SetTargetAudioSource(0, audioSouce);

//             // int width = (int)_videoPlayer.clip.width;
//             // int height = (int)_videoPlayer.clip.height;

//             // colors = new Color32[width * height];
//             // texture = new Texture2D(width, height, TextureFormat.RGB24, false);
//             // rgbaMat = new Mat(height, width, CvType.CV_8UC4);

//             //videoTexture = new Texture2D(width, height, TextureFormat.RGB24, false);

//             if (!toggleEnable)
//             {
//                 _videoPlayer.clip = Resources.Load<VideoClip>("Movies/sample_2");
//             }
//             else
//             {
//                 _videoPlayer.url = path;
//             }

//             _videoPlayer.errorReceived += ErrorReceived;
//             _videoPlayer.prepareCompleted += PrepareCompleted;
//             _videoPlayer.Prepare();
//             _videoPlayer.isLooping = false;
//             _videoPlayer.aspectRatio = VideoAspectRatio.Stretch;
//             _videoPlayer.targetCameraAlpha = 1;
//             _videoPlayer.loopPointReached += EndReached;

//             // _videoPlayer.Play();

//             //gameObject.GetComponent<Renderer>().material.mainTexture = texture;

//             trackers = MultiTracker.create();
//             objects = new MatOfRect2d();

//             trackingColorList = new List<Scalar>();
//             selectedPointList = new List<Point>();


//             // #if UNITY_WEBGL && !UNITY_EDITOR
//             //             getFilePath_Coroutine = Utils.getFilePathAsync (LBP_CASCADE_FILENAME, (result) => {
//             //                 getFilePath_Coroutine = null;

//             //                 cascade = new CascadeClassifier ();
//             //                 cascade.load (result);
//             //                 if (cascade.empty ()) {
//             //                     Debug.LogError ("cascade file is not loaded. Please copy from “OpenCVForUnity/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
//             //                 }

//             //                 webCamTextureToMatHelper.Initialize ();
//             //             });
//             //             StartCoroutine (getFilePath_Coroutine);
//             // #else
//             cascade = new CascadeClassifier();
//             cascade.load(Utils.getFilePath(LBP_CASCADE_FILENAME));
//             //             //            cascade.load (Utils.getFilePath ("haarcascade_frontalface_alt.xml"));
//             //             if (cascade.empty())
//             //             {
//             //                 Debug.LogError("cascade file is not loaded. Please copy from “OpenCVForUnity/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
//             //             }

//             // #endif
//         }

//         // エラー発生時に呼ばれる
//         private void ErrorReceived(VideoPlayer vp, string message)
//         {
//             Debug.Log("エラー発生");
//             vp.errorReceived -= ErrorReceived;
//             vp.prepareCompleted -= PrepareCompleted;
//             Destroy(_videoPlayer);
//             vp = null;
//         }

//         //　動画の読み込みが完了したら呼ばれる
//         void PrepareCompleted(VideoPlayer vp)
//         {
//             vp.prepareCompleted -= PrepareCompleted;

//             width = (int)_videoPlayer.texture.width;
//             height = (int)_videoPlayer.texture.height;

//             float widthScale = (float)Screen.width / width;
//             float heightScale = (float)Screen.height / height;
//             // if (widthScale < heightScale)
//             // {
//             //     Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
//             // }
//             // else
//             // {
//             //     Camera.main.orthographicSize = height / 2;
//             // }

//             colors = new Color32[width * height];
//             texture = new Texture2D(width, height, TextureFormat.RGB24, false);

//             rgbaMat = new Mat(height, width, CvType.CV_8UC4);

//             // Utils.matToTexture2D(rgbaMat, texture, colors);

//             videoTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
//             // Utils.fastMatToTexture2D(rgbaMat, texture);

//             gameObject.GetComponent<Renderer>().material.mainTexture = texture;

//             isPrepare = true;
//             faces = new MatOfRect();

//         }

//         // 動画再生完了時に呼ばれる
//         void EndReached(UnityEngine.Video.VideoPlayer vp)
//         {
//             vp.errorReceived -= ErrorReceived;
//             Destroy(_videoPlayer);
//             _videoPlayer = null;
//             // 動画再生完了時の処理
//         }

//         private void SetTrackingObject(Vector3 position)
//         {
//             var canvas = _canvasOject.GetComponent<Canvas>();
//             var canvasRect = canvas.GetComponent<RectTransform>();

//             // クリック位置に対応するRectTransformのlocalPositionを計算する
//             Vector2 localpoint;
//             RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, position, canvas.worldCamera, out localpoint);

//             /// 正解
//             var obj = Instantiate(_prefab);
//             obj.transform.SetParent(_canvasOject, false);
//             obj.GetComponent<RectTransform>().anchoredPosition = localpoint;
//             obj.SetActive(true);
//         }

//         // Update is called once per frame
//         void Update()
//         {
//             if (!isPrepare)
//             {
//                 return;
//             }

//             //if (_videoPlayer.isPlaying && _videoPlayer.texture != null)

//             if (_videoPlayer == null)
//             {
//                 return;
//             }

//             if (!_videoPlayer.isPlaying)
//             {

//                 if (Input.GetMouseButtonDown(0))
//                 {

//                 }
//                 return;

//             }

//             if (_videoPlayer.texture != null)
//             {
//                 Utils.textureToTexture2D(_videoPlayer.texture, videoTexture);

//                 Utils.fastTexture2DToMat(videoTexture, rgbaMat);
//                 grayMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC1);
//             }


//             //Imgproc.putText(rgbaMat, "VideoPlayer With OpenCV for Unity Example", new Point(100, rgbaMat.rows() / 2), Imgproc.FONT_HERSHEY_SIMPLEX, 1.5, new Scalar(255, 0, 0, 255), 5, Imgproc.LINE_AA, false);

//             //Imgproc.putText(rgbaMat, "width:" + rgbaMat.width() + " height:" + rgbaMat.height() + " frame:" + _videoPlayer.frame, new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.5, new Scalar(255, 255, 255, 255), 5, Imgproc.LINE_AA, false);

//             //Utils.fastMatToTexture2D(rgbaMat, texture);

//             // Texture2Dの画像を画面上に表示
//             //gameObject.GetComponent<Renderer>().material.mainTexture = imgTexture;


//             /////////////// CasCade TestCode ///////////////
//             Imgproc.cvtColor(rgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);
//             Imgproc.equalizeHist(grayMat, grayMat);



//             // if (cascade != null)
//             //     cascade.detectMultiScale(grayMat, faces, 1.1, 2, 2, // TODO: objdetect.CV_HAAR_SCALE_IMAGE
//             //         new Size(grayMat.cols() * 0.2, grayMat.rows() * 0.2), new Size());

//             // Debug.LogError("grayMat.cols() " + grayMat.cols() + " grayMat.rows() " + grayMat.rows());

//             // Debug.LogError("faces " + faces);

//             // OpenCVForUnity.CoreModule.Rect[] rects = faces.toArray();
//             // for (int i = 0; i < rects.Length; i++)
//             // {
//             //     var cx = rects[i].x + rects[i].width / 2f;
//             //     var cy = rects[i].y + rects[i].height / 2f;

//             //     //var ctrdOnWorld = new Point((float)ctrdOnImg.x - rgbaMat.width() / 2f, rgbaMat.height() / 2f - (float)ctrdOnImg.y);


//             //     //_mask.transform.localPosition = new Vector2(cx - _pos.x, -cy + _pos.y);

//             //     Debug.LogError("結果" + (cy - _pos.y));

//             //     Imgproc.rectangle(rgbaMat, new Point(rects[i].x, rects[i].y), new Point(rects[i].x + rects[i].width, rects[i].y + rects[i].height), new Scalar(255, 0, 0, 255), 2);
//             // }
//             ///////////// CasCade TestCode ///////////////


// #if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
//                                             //Touch
//                                             int touchCount = Input.touchCount;
//                                             if (touchCount == 1)
//                                             {
//                                                 Touch t = Input.GetTouch(0);
//                                                 if(t.phase == TouchPhase.Ended && !EventSystem.current.IsPointerOverGameObject (t.fingerId)) {
//                                                     storedTouchPoint = new Point (t.position.x, t.position.y);
//                                                     //Debug.Log ("touch X " + t.position.x);
//                                                     //Debug.Log ("touch Y " + t.position.y);
//                                                 }
//                                             }
// #else
//             //Mouse
//             if (Input.GetMouseButtonUp(0) && !EventSystem.current.IsPointerOverGameObject())
//             {
//                 if (trackers.IsDisposed)
//                 {

//                     trackers = MultiTracker.create();
//                     objects = new MatOfRect2d();
//                     isDispose = false;

//                 }

//                 storedTouchPoint = new Point(Input.mousePosition.x, Input.mousePosition.y);
//                 _screenPositionList.Add(Input.mousePosition);

//                 Debug.Log("AAA  " + Input.mousePosition.x);
//                 Debug.Log("BBB  " + Input.mousePosition.y);


//             }
// #endif

//             if (selectedPointList.Count == 1)
//             {
//                 if (storedTouchPoint != null)
//                 {
//                     ConvertScreenPointToTexturePoint(storedTouchPoint, storedTouchPoint, gameObject, rgbaMat.cols(), rgbaMat.rows());
//                     OnTouch(rgbaMat, storedTouchPoint);
//                     storedTouchPoint = null;
//                     //_videoPlayer.Pause();

//                 }
//             }

//             //Loop play
//             // if (capture.get(Videoio.CAP_PROP_POS_FRAMES) >= capture.get(Videoio.CAP_PROP_FRAME_COUNT))
//             //     capture.set(Videoio.CAP_PROP_POS_FRAMES, 0);

//             //error PlayerLoop called recursively! on iOS.reccomend WebCamTexture.
//             if (selectedPointList.Count != 1)
//             {

//                 //capture.retrieve(rgbaMat, 0);
//                 //Imgproc.cvtColor(rgbaMat, rgbaMat, Imgproc.COLOR_BGR2RGB);
//                 Imgproc.cvtColor(rgbaMat, rgbaMat, Imgproc.COLOR_RGBA2RGB);


//                 if (storedTouchPoint != null)
//                 {
//                     ConvertScreenPointToTexturePoint(storedTouchPoint, storedTouchPoint, gameObject, rgbaMat.cols(), rgbaMat.rows());
//                     OnTouch(rgbaMat, storedTouchPoint);

//                     Debug.LogError(storedTouchPoint);
//                     storedTouchPoint = null;
//                 }

//                 if (selectedPointList.Count < 2)
//                 {
//                     foreach (var point in selectedPointList)
//                     {
//                         Imgproc.circle(rgbaMat, point, 6, new Scalar(0, 0, 255), 2);
//                     }
//                 }
//                 else
//                 {
//                     using (MatOfPoint selectedPointMat = new MatOfPoint(selectedPointList.ToArray()))
//                     {
//                         OpenCVForUnity.CoreModule.Rect region = Imgproc.boundingRect(selectedPointMat);
//                         trackers.add(TrackerKCF.create(), rgbaMat, new Rect2d(region.x, region.y, region.width, region.height));
//                         Vector3 center = (_screenPositionList[0] + _screenPositionList[1]) * 0.5f;

//                         SetTrackingObject(center);
//                         _screenPositionList.Clear();
//                     }

//                     _maskCount++;
//                     selectedPointList.Clear();
//                     trackingColorList.Add(new Scalar(UnityEngine.Random.Range(0, 255), UnityEngine.Random.Range(0, 255), UnityEngine.Random.Range(0, 255)));
//                 }

//                 if (trackingColorList.Count != 0)
//                 {
//                     if (!isDispose)
//                     {
//                         var result = trackers.update(rgbaMat, objects);

//                         if (!result)
//                         {
//                             Debug.LogError("トラッカー結果：" + result);
//                             trackers.Dispose();
//                             objects.Dispose();
//                             isDispose = true;
//                             _masks[0].gameObject.SetActive(false);
//                             _masks[1].gameObject.SetActive(false);
//                             trackingColorList.Clear();

//                         }
//                     }
//                 }


//                 if (!isDispose)
//                 {
//                     Rect2d[] objectsArray = objects.toArray();

//                     double[] xx = new double[2];
//                     double[] yy = new double[2];


//                     double[] doubleNumbersX = new double[2];
//                     double[] doubleNumbersY = new double[2];


//                     for (int i = 0; i < objectsArray.Length; i++)
//                     {
//                         Debug.LogError("トラッキング数 : " + objectsArray.Length);
//                         Imgproc.rectangle(rgbaMat, objectsArray[i].tl(), objectsArray[i].br(), trackingColorList[i], -1, 1, 0);
//                         Debug.LogError("tl  = " + objectsArray[i].tl() + "  br =  " + objectsArray[i].br());




//                         // xx[i] = (objectsArray[i].br().x - objectsArray[i].tl().x) / 2;
//                         // yy[i] = (objectsArray[i].br().y - objectsArray[i].tl().y) / 2;



//                         // doubleNumbersX = new double[] { objectsArray[i].tl().x, objectsArray[i].br().x };
//                         // // 中心の座標を計算する
//                         // double x = doubleNumbersX.Average();

//                         // doubleNumbersY = new double[] { objectsArray[i].tl().y, objectsArray[i].br().y };

//                         // double y = doubleNumbersY.Average();


//                         var cx = objectsArray[i].x + objectsArray[i].width / 2f;

//                         var cy = objectsArray[i].y + objectsArray[i].height / 2f;
//                         // var screenPoint = new Vector3((float)cx, (float)cx, 0);

//                         //_masks[i].transform.localPosition = new Vector2((float)x - _pos.x, -(float)y + _pos.y + (float)yy[i]);
//                         // Canvasにセットされているカメラを取得
//                         //screenPosition = new Vector3((float)cx, (float)cx, 0);
//                         // var graphic = _masks[i].GetComponent<Graphic>();
//                         // var camera = graphic.canvas.worldCamera;

//                         // // Overlayの場合はScreenPointToLocalPointInRectangleにnullを渡さないといけないので書き換える
//                         // if (graphic.canvas.renderMode == RenderMode.ScreenSpaceOverlay)
//                         // {
//                         //     camera = null;
//                         // }

//                         // クリック位置に対応するRectTransformのlocalPositionを計算する
//                         // var localPoint = Vector2.zero;
//                         // RectTransformUtility.ScreenPointToLocalPointInRectangle(graphic.rectTransform, screenPosition, camera, out localPoint);

//                         //Debug.LogError("localPoint : " + localPoint);

//                         //_masks[i].transform.localPosition = new Vector2(localPoint.x, localPoint.y);

//                         // _masks[i].transform.localPosition = new Vector2((float)cx - _pos.x, -(float)cy + _pos.y);

//                         // _masks[i].gameObject.SetActive(true);

//                     }
//                 }

//                 if (selectedPointList.Count != 1)
//                 {
//                     //Imgproc.putText (rgbaMat, "Please touch the screen, and select tracking regions.", new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 0.8, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
//                     // if (fpsMonitor != null)
//                     // {
//                     //     fpsMonitor.consoleText = "Please touch the screen, and select tracking regions.";
//                     // }
//                 }
//                 else
//                 {
//                     // //Imgproc.putText (rgbaMat, "Please select the end point of the new tracking region.", new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 0.8, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
//                     // if (fpsMonitor != null)
//                     // {
//                     //     fpsMonitor.consoleText = "Please select the end point of the new tracking region.";
//                     // }
//                 }
//                 //Imgproc.cvtColor(rgbaMat, rgbaMat, Imgproc.COLOR_BGR2BGRA);

//                 Utils.fastMatToTexture2D(rgbaMat, texture);

//             }
//         }



//         void OnDestroy()
//         {

//             if (rgbaMat != null)
//                 rgbaMat.Dispose();

//             if (texture != null)
//             {
//                 Texture2D.Destroy(texture);
//                 texture = null;
//             }

//             if (trackers != null)
//                 trackers.Dispose();

//             if (objects != null)
//                 objects.Dispose();

// #if UNITY_WEBGL && !UNITY_EDITOR
//             if (getFilePath_Coroutine != null) {
//                 StopCoroutine (getFilePath_Coroutine);
//                 ((IDisposable)getFilePath_Coroutine).Dispose ();
//             }
// #endif
//         }

//         public void OnBackButton()
//         {
// #if UNITY_5_3 || UNITY_5_3_OR_NEWER
//             SceneManager.LoadScene("OpenCVForUnityExample");
// #else
//             Application.LoadLevel ("OpenCVForUnityExample");
// #endif
//         }

//         private void OnTouch(Mat img, Point touchPoint)
//         {
//             if (selectedPointList.Count < 2)
//             {
//                 selectedPointList.Add(touchPoint);
//                 if (!(new OpenCVForUnity.CoreModule.Rect(0, 0, img.cols(), img.rows()).contains(selectedPointList[selectedPointList.Count - 1])))
//                 {
//                     selectedPointList.RemoveAt(selectedPointList.Count - 1);
//                 }
//             }
//         }

//         /// <summary>
//         /// Converts the screen point to texture point.
//         /// </summary>
//         /// <param name="screenPoint">Screen point.</param>
//         /// <param name="dstPoint">Dst point.</param>
//         /// <param name="texturQuad">Texture quad.</param>
//         /// <param name="textureWidth">Texture width.</param>
//         /// <param name="textureHeight">Texture height.</param>
//         /// <param name="camera">Camera.</param>
//         private void ConvertScreenPointToTexturePoint(Point screenPoint, Point dstPoint, GameObject textureQuad, int textureWidth = -1, int textureHeight = -1, Camera camera = null)
//         {
//             if (textureWidth < 0 || textureHeight < 0)
//             {
//                 Renderer r = textureQuad.GetComponent<Renderer>();
//                 if (r != null && r.material != null && r.material.mainTexture != null)
//                 {
//                     textureWidth = r.material.mainTexture.width;
//                     textureHeight = r.material.mainTexture.height;
//                 }
//                 else
//                 {
//                     textureWidth = (int)textureQuad.transform.localScale.x;
//                     textureHeight = (int)textureQuad.transform.localScale.y;
//                 }
//             }

//             if (camera == null)
//                 camera = Camera.main;

//             Vector3 quadPosition = textureQuad.transform.localPosition;
//             Vector3 quadScale = textureQuad.transform.localScale;

//             Vector2 tl = camera.WorldToScreenPoint(new Vector3(quadPosition.x - quadScale.x / 2, quadPosition.y + quadScale.y / 2, quadPosition.z));
//             Vector2 tr = camera.WorldToScreenPoint(new Vector3(quadPosition.x + quadScale.x / 2, quadPosition.y + quadScale.y / 2, quadPosition.z));
//             Vector2 br = camera.WorldToScreenPoint(new Vector3(quadPosition.x + quadScale.x / 2, quadPosition.y - quadScale.y / 2, quadPosition.z));
//             Vector2 bl = camera.WorldToScreenPoint(new Vector3(quadPosition.x - quadScale.x / 2, quadPosition.y - quadScale.y / 2, quadPosition.z));

//             using (Mat srcRectMat = new Mat(4, 1, CvType.CV_32FC2))
//             using (Mat dstRectMat = new Mat(4, 1, CvType.CV_32FC2))
//             {
//                 srcRectMat.put(0, 0, tl.x, tl.y, tr.x, tr.y, br.x, br.y, bl.x, bl.y);
//                 dstRectMat.put(0, 0, 0, 0, quadScale.x, 0, quadScale.x, quadScale.y, 0, quadScale.y);

//                 using (Mat perspectiveTransform = Imgproc.getPerspectiveTransform(srcRectMat, dstRectMat))
//                 using (MatOfPoint2f srcPointMat = new MatOfPoint2f(screenPoint))
//                 using (MatOfPoint2f dstPointMat = new MatOfPoint2f())
//                 {
//                     Core.perspectiveTransform(srcPointMat, dstPointMat, perspectiveTransform);

//                     dstPoint.x = dstPointMat.get(0, 0)[0] * textureWidth / quadScale.x;
//                     dstPoint.y = dstPointMat.get(0, 0)[1] * textureHeight / quadScale.y;
//                 }
//             }
//         }

//         /// <summary>再生一時停止</summary>
//         public void VideoPause()
//         {
//             _videoPlayer.Pause();

//             // _recorder.StopRecording();

//         }
//         /// <summary>再生一時停止再開</summary>
//         public void VideoStart()
//         {
//             _videoPlayer.Play();

//             // _recorder.StartRecording();
//             Debug.Log("Start");

//             //  StartRecording(Application.persistentDataPath + "/VideoWriterExample_output.avi");
//         }

//         private void StartRecording(string savePath)
//         {

//             this.savePath = savePath;

//             writer = new VideoWriter();
// #if !UNITY_IOS
//             writer.open(savePath, VideoWriter.fourcc('M', 'J', 'P', 'G'), 30, new Size(Screen.width, Screen.height));
//             Debug.Log("AAAAAAA");

// #else

//             Debug.Log(VideoWriter.fourcc('D', 'V', 'I', 'X'));
//             Debug.Log(Screen.width);
//             writer.open(savePath, VideoWriter.fourcc('D', 'V', 'I', 'X'), 30, new Size(Screen.width, Screen.height));
// #endif

//             if (!writer.isOpened())
//             {
//                 Debug.LogError("writer.isOpened() false");
//                 writer.release();
//                 return;
//             }

//             screenCapture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
//             recordingFrameRgbMat = new Mat(Screen.height, Screen.width, CvType.CV_8UC3);


//             StaticVideoWriterExample videoWriter = _camera.GetComponent<StaticVideoWriterExample>();
//             videoWriter.writer = writer;
//             videoWriter.recordingFrameRgbMat = recordingFrameRgbMat;
//             videoWriter.savePath = savePath;
//             Debug.LogWarning(savePath);

//             videoWriter.screenCapture = screenCapture;
//             videoWriter.frameCount = 0;
//             videoWriter.isRecording = true;


//             //frameCount = 0;

//             //isRecording = true;
//         }
//     }
// }

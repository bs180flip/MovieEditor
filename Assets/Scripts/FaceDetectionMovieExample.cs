using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using UnityEngine.Video;
using UnityEngine.UI;
using System.Collections.Generic;

namespace OpenCVForUnityExample
{
    // 適当なQuadにコンポーネント貼り付け(FaceDetectionWebCamTextureExampleを参考にしました)
    public class FaceDetectionMovieExample : MonoBehaviour
    {
        protected static readonly string LBP_CASCADE_FILENAME = "haarcascade_frontalface_alt_tree.xml";

        VideoPlayer _videoPlayer; // Video Playerゲームオブジェクトを入れ込み

        [SerializeField] Image image; // Video Playerゲームオブジェクトを入れ込み

        [SerializeField]
        Transform _canvasOject = null;

        [SerializeField]
        GameObject _prefab = null;

        Mat grayMat;
        Texture2D texture;
        Mat rgbaMat;
        Texture2D videoTexture;
        CascadeClassifier cascade;
        MatOfRect faces;

        /// <summary>
        /// The colors.
        /// </summary>
        Color32[] colors;


        AudioSource audioSouce;
        bool flag = false;

        private List<GameObject> objs = new List<GameObject>();

        public int Width = 640;
        public int Height = 480;
        public int FPS = 30;

        private bool isPrepare { get; set; } = false;


        public void Init(string path, bool toggleEnable)
        {

            _videoPlayer = GetComponent<VideoPlayer>();
            audioSouce = GetComponent<AudioSource>();

            audioSouce.playOnAwake = false;

            _videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            _videoPlayer.EnableAudioTrack(0, true);
            _videoPlayer.SetTargetAudioSource(0, audioSouce);

            if (!toggleEnable)
            {
                _videoPlayer.clip = Resources.Load<VideoClip>("Movies/sample_2");
            }
            else
            {
                _videoPlayer.url = path;
            }

            _videoPlayer.errorReceived += ErrorReceived;
            _videoPlayer.prepareCompleted += PrepareCompleted;
            _videoPlayer.Prepare();
            _videoPlayer.isLooping = false;
            _videoPlayer.aspectRatio = VideoAspectRatio.Stretch;
            _videoPlayer.targetCameraAlpha = 1;
            _videoPlayer.loopPointReached += EndReached;
        }

        public void VideoStart()
        {
            //_videoPlayer.time = 30;
            _videoPlayer.Play();
        }

        void Update()
        {


            if (!isPrepare)
            {
                return;
            }


            if (_videoPlayer.isPlaying && _videoPlayer.texture != null)
            {
                Utils.textureToTexture2D(_videoPlayer.texture, videoTexture);
                Utils.fastTexture2DToMat(videoTexture, rgbaMat);

                Imgproc.cvtColor(rgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);
                Imgproc.equalizeHist(grayMat, grayMat);

                MatOfRect faces = new MatOfRect();

                if (cascade != null)
                    cascade.detectMultiScale(grayMat, faces, 1.1, 2, 2, // TODO: objdetect.CV_HAAR_SCALE_IMAGE
                        new Size(grayMat.cols() * 0.08, grayMat.rows() * 0.08), new Size());


                OpenCVForUnity.CoreModule.Rect[] rects = faces.toArray();
                for (int i = 0; i < rects.Length; i++)
                {
                    // Vector3 first = new Vector3(rects[i].x, rects[i].y);
                    // Vector3 second = new Vector3(rects[i].x + rects[i].Width, rects[i].y + rects[i].Height);

                    // Debug.LogWarning("first = " + first + "    second = " + second);

                    // Vector3 center = (first + second) * 0.5f;
                    // SetTrackingObject(center);

                    // 中心の座標を計算する
                    var x = rects[0].x + (rects[i].width / 2);
                    //var x = (float)rects[0].x;

                    var y = rects[0].y + (rects[i].height / 2);


                    if (objs.Count < i + 1)
                    {
                        objs.Add(Instantiate(_prefab));
                        objs[i].SetActive(false);
                        objs[i].transform.SetParent(_canvasOject, false);
                    }

                    // オブジェクトを移動する
                    objs[i].transform.localPosition = Vector2ToVector3(new Vector2(x, y));
                    objs[i].SetActive(true);


                    Imgproc.rectangle(rgbaMat, new Point(rects[i].x, rects[i].y), new Point(rects[i].x + rects[i].width, rects[i].y + rects[i].height), new Scalar(255, 0, 0, 255), 2);

                }

                Imgproc.putText(rgbaMat, "W:" + rgbaMat.width() + " H:" + rgbaMat.height() + " SO:" + Screen.orientation, new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                Utils.fastMatToTexture2D(rgbaMat, texture);
            }
        }

        // エラー発生時に呼ばれる
        private void ErrorReceived(VideoPlayer vp, string message)
        {
            Debug.Log("エラー発生");
            vp.errorReceived -= ErrorReceived;
            vp.prepareCompleted -= PrepareCompleted;
            Destroy(_videoPlayer);
            vp = null;
        }


        // 動画再生完了時に呼ばれる
        void EndReached(UnityEngine.Video.VideoPlayer vp)
        {
            vp.errorReceived -= ErrorReceived;
            Destroy(_videoPlayer);
            _videoPlayer = null;
            // 動画再生完了時の処理
        }


        //　動画の読み込みが完了したら呼ばれる
        void PrepareCompleted(VideoPlayer vp)
        {
            vp.prepareCompleted -= PrepareCompleted;

            Width = (int)_videoPlayer.clip.width;
            Height = (int)_videoPlayer.clip.height;

            texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            videoTexture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            rgbaMat = new Mat(Height, Width, CvType.CV_8UC4);
            grayMat = new Mat(Height, Width, CvType.CV_8UC1);

            gameObject.GetComponent<Renderer>().material.mainTexture = texture;


            cascade = new CascadeClassifier();
            cascade.load(Utils.getFilePath(LBP_CASCADE_FILENAME));

            objs.Add(Instantiate(_prefab));
            objs[0].SetActive(false);
            objs[0].transform.SetParent(_canvasOject, false);
            isPrepare = true;

            Debug.Log("読み込み完了");

        }

        private void SetTrackingObject(Vector3 position)
        {
            var canvas = _canvasOject.GetComponent<Canvas>();
            var canvasRect = canvas.GetComponent<RectTransform>();

            // クリック位置に対応するRectTransformのlocalPositionを計算する
            Vector2 localpoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, position, canvas.worldCamera, out localpoint);

            // if (obj == null)
            // {
            //     obj = Instantiate(_prefab);
            //     obj.transform.SetParent(_canvasOject, false);
            // }

            // obj.GetComponent<RectTransform>().localPosition = localpoint;
            // obj.SetActive(true);
        }

        void OnDestroy()
        {
            Destroy(texture);
            Destroy(videoTexture);

            if (rgbaMat != null)
                rgbaMat.Dispose();

            if (grayMat != null)
                grayMat.Dispose();

            if (cascade != null)
                cascade.Dispose();
        }

        /// <summary>
        /// OpenCVの2次元座標をUnityの3次元座標に変換する
        /// </summary>
        /// <param name="vector2"></param>
        /// <returns></returns>
        private Vector3 Vector2ToVector3(Vector2 vector2)
        {
            // if (Camera == null)
            // {
            //     throw new Exception("");
            // }

            // スクリーンサイズで調整(WebCamera->Unity)
            vector2.x = vector2.x * Screen.width / Width;
            vector2.y = vector2.y * Screen.height / Height;

            // Unityのワールド座標系(3次元)に変換
            var vector3 = Camera.main.ScreenToWorldPoint(vector2);

            // 座標の調整
            // Y座標は逆、Z座標は0にする(Xもミラー状態によって逆にする必要あり)
            vector3.y *= -1;
            vector3.z = 0;

            return vector3;
        }

    }
}

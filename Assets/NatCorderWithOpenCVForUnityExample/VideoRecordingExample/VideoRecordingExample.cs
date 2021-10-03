using NatSuite.Recorders;
using NatSuite.Recorders.Clocks;
using NatSuite.Recorders.Inputs;
using NatSuite.Sharing;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnity.UtilsModule;
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using UnityEngine.Video;
using UnityEngine.UI;
using System.Collections.Generic;
using NatSuite.Examples;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// VideoRecording Example
    /// </summary>
    //[RequireComponent(typeof(WebCamTextureToMatHelper))]
    public class VideoRecordingExample : MonoBehaviour
    {
        /// <summary>
        /// The requested resolution.
        /// </summary>
        public ResolutionPreset requestedResolution = ResolutionPreset._640x480;

        /// <summary>
        /// The requested resolution dropdown.
        /// </summary>
        public Dropdown requestedResolutionDropdown;

        [Space(20)]
        [Header("Recording")]

        /// <summary>
        /// The type of container.
        /// </summary>
        public ContainerPreset container = ContainerPreset.MP4;

        /// <summary>
        /// The container dropdown.
        /// </summary>
        public Dropdown containerDropdown;

        /// <summary>
        /// Determines if applies the comic filter.
        /// </summary>
        public bool applyComicFilter;

        /// <summary>
        /// The apply comic filter toggle.
        /// </summary>
        public Toggle applyComicFilterToggle;

        /// <summary>
        /// The video bitrate.
        /// </summary>
        public VideoBitRatePreset videoBitRate = VideoBitRatePreset._10Mbps;

        /// <summary>
        /// The video bitrate frequency.
        /// </summary>
        public Dropdown videoBitRateDropdown;

        /// <summary>
        /// The audio bitrate.
        /// </summary>
        public AudioBitRatePreset audioBitRate = AudioBitRatePreset._64Kbps;

        /// <summary>
        /// The audio bitrate frequency.
        /// </summary>
        public Dropdown audioBitRateDropdown;

        [Header("Microphone")]

        /// <summary>
        /// Determines if record microphone audio.
        /// </summary>
        public bool recordMicrophoneAudio;

        /// <summary>
        /// The record microphone audio toggle.
        /// </summary>
        public Toggle recordMicrophoneAudioToggle;

        [Space(20)]

        /// <summary>
        /// The record video button.
        /// </summary>
        public Button recordVideoButton;

        /// <summary>
        /// The save path input field.
        /// </summary>
        public InputField savePathInputField;

        /// <summary>
        /// The play video button.
        /// </summary>
        public Button playVideoButton;

        /// <summary>
        /// The play video full screen button.
        /// </summary>
        public Button playVideoFullScreenButton;

        [Space(20)]

        /// <summary>
        /// The share button.
        /// </summary>
        public Button shareButton;

        /// <summary>
        /// The save to CameraRoll button.
        /// </summary>
        public Button saveToCameraRollButton;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        WebCamTextureToMatHelper webCamTextureToMatHelper;

        IMediaRecorder videoRecorder;

        AudioSource microphoneSource;

        AudioInput audioInput;

        IClock recordingClock;

        CancellationTokenSource cancellationTokenSource;

        const int MAX_RECORDING_TIME = 10; // Seconds

        string videoPath = "";

        bool isVideoPlaying;

        bool isVideoRecording;

        bool isFinishWriting;

        int frameCount;

        int recordEveryNthFrame;

        int recordingWidth;
        int recordingHeight;
        int videoFramerate;
        int audioSampleRate;
        int audioChannelCount;
        float frameDuration;

        ComicFilter comicFilter;

        string exampleTitle = "";
        string exampleSceneTitle = "";
        string settingInfo1 = "";
        string settingInfo2 = "";
        string settingInfoGIF = "";
        string settingInfoJPG = "";
        Scalar textColor = new Scalar(255, 255, 255, 255);
        Point textPos = new Point();

#if !OPENCV_USE_UNSAFE_CODE
        byte[] pixelBuffer;
#endif

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        string LBP_CASCADE_FILENAME = "haarcascade_frontalface_alt2.xml";


        [SerializeField] GameObject videoPlayerObj = null;
        VideoPlayer _videoPlayer; // Video Playerゲームオブジェクトを入れ込み

        [SerializeField] Image image; // Video Playerゲームオブジェクトを入れ込み

        [SerializeField]
        Transform _canvasOject = null;

        [SerializeField]
        GameObject _prefab = null;

        Mat grayMat;
        Mat rgbaMat;
        Texture2D videoTexture;
        CascadeClassifier cascade;
        MatOfRect faces;

        /// <summary>
        /// The colors.
        /// </summary>
        Color32[] colors;

        bool flag = false;

        private List<GameObject> objs = new List<GameObject>();

        public int Width = 640;
        public int Height = 480;
        public int FPS = 30;

        private bool isPrepare { get; set; } = false;

        CameraInput cameraInput;

        [SerializeField]
        GameObject replayCamObj = null;

        ReplayCam replayCam = null;

        float trackingSize = 0.08f;

        DeviceOrientation result;

        [SerializeField]
        Dropdown cascadeDropDown = null;

        private async Task StartRecording()
        {
            if (isVideoPlaying || isVideoRecording || isFinishWriting)
                return;

            Debug.Log("StartRecording ()");

            // First make sure recording microphone is only on MP4 or HEVC
            recordMicrophoneAudio = recordMicrophoneAudioToggle.isOn;
            recordMicrophoneAudio &= (container == ContainerPreset.MP4 || container == ContainerPreset.HEVC);
            // Create recording configurations
            recordingWidth = rgbaMat.width();
            recordingHeight = rgbaMat.height();

            videoFramerate = 30;
            audioSampleRate = recordMicrophoneAudio ? AudioSettings.outputSampleRate : 0;
            audioChannelCount = recordMicrophoneAudio ? (int)AudioSettings.speakerMode : 0;
            frameDuration = 0.1f;

            // Create video recorder
            recordingClock = new RealtimeClock();
            if (container == ContainerPreset.MP4)
            {
                videoRecorder = new MP4Recorder(
                    recordingWidth,
                    recordingHeight,
                    videoFramerate,
                    audioSampleRate,
                    audioChannelCount,
                    (int)videoBitRate,
                    audioBitRate: (int)audioBitRate
                );
                recordEveryNthFrame = 1;

                cameraInput = new CameraInput(videoRecorder, recordingClock, Camera.main);

            }
            else if (container == ContainerPreset.HEVC)
            {
                videoRecorder = new HEVCRecorder(
                    recordingWidth,
                    recordingHeight,
                    videoFramerate,
                    audioSampleRate,
                    audioChannelCount,
                    (int)videoBitRate,
                    audioBitRate: (int)audioBitRate
                );
                recordEveryNthFrame = 1;
            }
            else if (container == ContainerPreset.GIF)
            {
                videoRecorder = new GIFRecorder(
                    recordingWidth,
                    recordingHeight,
                    frameDuration
                );
                recordEveryNthFrame = 5;
            }
            else if (container == ContainerPreset.JPG) // macOS and Windows platform only.
            {
                videoRecorder = new JPGRecorder(
                    recordingWidth,
                    recordingHeight
                );
                recordEveryNthFrame = 5;
            }
            frameCount = 0;

            // Start recording
            isVideoRecording = true;

            HideAllVideoUI();
            recordVideoButton.interactable = true;
            recordVideoButton.GetComponentInChildren<UnityEngine.UI.Text>().color = Color.red;

            CreateSettingInfo();

            // Start microphone and create audio input
            if (recordMicrophoneAudio)
            {
                //await StartMicrophone();
                audioInput = new AudioInput(videoRecorder, recordingClock, microphoneSource, true);
            }
            // Unmute microphone
            microphoneSource.mute = audioInput == null;

            // Start countdown
            cancellationTokenSource = new CancellationTokenSource();
            try
            {
                Debug.Log("Countdown start.");
                await CountdownAsync(
                    sec =>
                    {
                        string str = "Recording";
                        for (int i = 0; i < sec; i++)
                        {
                            str += ".";
                        }

                        if (fpsMonitor != null) fpsMonitor.consoleText = str;

                    }, MAX_RECORDING_TIME, cancellationTokenSource.Token);
                Debug.Log("Countdown end.");
            }
            catch (OperationCanceledException e)
            {
                if (e.CancellationToken == cancellationTokenSource.Token)
                {
                    Debug.Log("Countdown canceled.");
                }
            }
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;

            if (this != null && isActiveAndEnabled)
            {
                await FinishRecording();
            }
        }

        private void CancelRecording()
        {
            if (!isVideoRecording || isFinishWriting)
                return;

            if (cancellationTokenSource != null)
                cancellationTokenSource.Cancel(true);
        }

        private async Task FinishRecording()
        {
            if (!isVideoRecording || isFinishWriting)
                return;

            // Mute microphone
            microphoneSource.mute = true;

            // Stop the microphone if we used it for recording
            if (recordMicrophoneAudio)
            {
                StopMicrophone();
                audioInput.Dispose();
                audioInput = null;
            }

            if (fpsMonitor != null) fpsMonitor.consoleText = "FinishWriting...";

            // Stop recording
            isFinishWriting = true;
            try
            {
                var path = await videoRecorder.FinishWriting();
                videoPath = path;
                Debug.Log("Saved recording to: " + videoPath);
                savePathInputField.text = videoPath;
            }
            catch (ApplicationException e)
            {
                Debug.Log(e.Message);
                savePathInputField.text = e.Message;
            }
            isFinishWriting = false;

            if (fpsMonitor != null) fpsMonitor.consoleText = "";

            ShowAllVideoUI();
            recordVideoButton.GetComponentInChildren<UnityEngine.UI.Text>().color = Color.black;
            isVideoRecording = false;
        }

        private Task<bool> StartMicrophone()
        {
            var task = new TaskCompletionSource<bool>();
            StartCoroutine(CreateMicrophoneClip(granted =>
            {
                microphoneSource.Play();
                task.SetResult(granted);
            }));

            return task.Task;
        }

        private IEnumerator CreateMicrophoneClip(Action<bool> completionHandler)
        {
            // Create a microphone clip
            microphoneSource.mute =
            microphoneSource.loop = true;
            microphoneSource.bypassEffects =
            microphoneSource.bypassListenerEffects = false;
            microphoneSource.clip = Microphone.Start(null, true, 1, AudioSettings.outputSampleRate);
            yield return new WaitUntil(() => Microphone.GetPosition(null) > 0);
            completionHandler(true);
        }

        private void StopMicrophone()
        {
            // Stop microphone
            microphoneSource.Stop();
            Microphone.End(null);
        }

        private async Task CountdownAsync(Action<int> countdownHandler, int sec = 10, CancellationToken cancellationToken = default(CancellationToken))
        {
            for (int i = sec; i > 0; i--)
            {
                cancellationToken.ThrowIfCancellationRequested();
                countdownHandler(i);
                await Task.Delay(1000, cancellationToken);
            }
            cancellationToken.ThrowIfCancellationRequested();
            countdownHandler(0);
        }


        private void PlayVideo(string path)
        {
            if (isVideoPlaying || isVideoRecording || isFinishWriting || string.IsNullOrEmpty(path))
                return;

            Debug.Log("PlayVideo ()");

            _videoPlayer.Stop();

            isVideoPlaying = true;

            // Playback the video
            _videoPlayer.renderMode = VideoRenderMode.APIOnly;
            _videoPlayer.playOnAwake = false;
            _videoPlayer.isLooping = false;

            // Unmute microphone
            microphoneSource.mute = false;
            microphoneSource.playOnAwake = false;

            _videoPlayer.source = VideoSource.Url;
            _videoPlayer.url = path;
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            _videoPlayer.controlledAudioTrackCount = 1;
            _videoPlayer.EnableAudioTrack(0, true);
            _videoPlayer.SetTargetAudioSource(0, microphoneSource);

            _videoPlayer.prepareCompleted += PrepareCompleted2;
            _videoPlayer.loopPointReached += EndReached;

            _videoPlayer.Prepare();

            HideAllVideoUI();
        }


        private NativeGallery.ImageOrientation lastLoadedOrientation;


        // Util
        Texture2D RotateTexture(Texture2D originalTexture, bool clockwise)
        {
            Color32[] original = originalTexture.GetPixels32();
            Color32[] rotated = new Color32[original.Length];
            int w = originalTexture.width;
            int h = originalTexture.height;

            int iRotated, iOriginal;

            for (int j = 0; j < h; ++j)
            {
                for (int i = 0; i < w; ++i)
                {
                    iRotated = (i + 1) * h - j - 1;
                    iOriginal = clockwise ? original.Length - 1 - (j * w + i) : j * w + i;
                    rotated[iRotated] = original[iOriginal];
                }
            }

            Texture2D rotatedTexture = new Texture2D(h, w);
            rotatedTexture.SetPixels32(rotated);
            rotatedTexture.Apply();
            return rotatedTexture;
        }


        private void PrepareCompleted2(VideoPlayer vp)
        {
            vp.prepareCompleted -= PrepareCompleted;

            vp.Play();
        }

        private void PrepareCompleted(VideoPlayer vp)
        {
            Debug.Log("PrepareCompleted ()");

            //_videoPlayer = vp;
            _videoPlayer.prepareCompleted -= PrepareCompleted;


            // Width = (int)_videoPlayer.texture.width;
            // Height = (int)_videoPlayer.texture.height;

            Width = (int)_videoPlayer.texture.width;
            Height = (int)_videoPlayer.texture.height;

            texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            videoTexture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            rgbaMat = new Mat(Height, Width, CvType.CV_8UC4);
            grayMat = new Mat(Height, Width, CvType.CV_8UC1);

#if !OPENCV_USE_UNSAFE_CODE
            pixelBuffer = new byte[rgbaMat.cols() * rgbaMat.rows() * 4];
#endif

            gameObject.GetComponent<Renderer>().material.mainTexture = texture;
            cascade = new CascadeClassifier();
            cascade.load(Utils.getFilePath(LBP_CASCADE_FILENAME));

            objs.Add(Instantiate(_prefab));
            objs[0].SetActive(false);
            objs[0].transform.SetParent(_canvasOject, false);
            isPrepare = true;

            Debug.Log("読み込み完了");

            UpdateScale();
        }

        private void EndReached(VideoPlayer vp)
        {
            Debug.Log("EndReached ()");

            vp.errorReceived -= ErrorReceived;
            Destroy(_videoPlayer);
            _videoPlayer = null;
            // 動画再生完了時の処理

            StopVideo();
        }

        private void StopVideo()
        {
            if (!isVideoPlaying)
                return;

            Debug.Log("StopVideo ()");

            _videoPlayer.loopPointReached -= EndReached;

            if (_videoPlayer.isPlaying)
                _videoPlayer.Stop();

            // Mute microphone
            microphoneSource.mute = true;

            isVideoPlaying = false;

            if (this != null && isActiveAndEnabled)
            {
                gameObject.GetComponent<Renderer>().sharedMaterial.mainTexture = texture;
                webCamTextureToMatHelper.Play();
                ShowAllVideoUI();
            }
        }

        private void ShowAllVideoUI()
        {
            requestedResolutionDropdown.interactable = true;
            containerDropdown.interactable = true;
            videoBitRateDropdown.interactable = true;
            audioBitRateDropdown.interactable = true;
            applyComicFilterToggle.interactable = true;
            recordMicrophoneAudioToggle.interactable = true;
            recordVideoButton.interactable = true;
            savePathInputField.interactable = true;
            playVideoButton.interactable = true;
            playVideoFullScreenButton.interactable = true;
            shareButton.interactable = true;
            saveToCameraRollButton.interactable = true;
        }

        private void HideAllVideoUI()
        {
            requestedResolutionDropdown.interactable = false;
            containerDropdown.interactable = false;
            videoBitRateDropdown.interactable = false;
            audioBitRateDropdown.interactable = false;
            applyComicFilterToggle.interactable = false;
            recordMicrophoneAudioToggle.interactable = false;
            recordVideoButton.interactable = false;
            savePathInputField.interactable = false;
            playVideoButton.interactable = false;
            playVideoFullScreenButton.interactable = false;
            shareButton.interactable = false;
            saveToCameraRollButton.interactable = false;
        }

        private void CreateSettingInfo()
        {
            settingInfo1 = "- [" + container + "] SIZE:" + recordingWidth + "x" + recordingHeight + " FPS:" + videoFramerate;
            settingInfo2 = "- ASR:" + audioSampleRate + " ACh:" + audioChannelCount + " VBR:" + (int)videoBitRate + " ABR:" + (int)audioBitRate;
            settingInfoGIF = "- [" + container + "] SIZE:" + recordingWidth + "x" + recordingHeight + " FrameDur:" + frameDuration;
            settingInfoJPG = "- [" + container + "] SIZE:" + recordingWidth + "x" + recordingHeight;
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {

            if (comicFilter != null)
                comicFilter.Dispose();

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
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("NatCorderWithOpenCVForUnityExample");
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick()
        {
            webCamTextureToMatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            webCamTextureToMatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            webCamTextureToMatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            webCamTextureToMatHelper.requestedIsFrontFacing = !webCamTextureToMatHelper.IsFrontFacing();
        }

        /// <summary>
        /// Raises the requested resolution dropdown value changed event.
        /// </summary>
        public void OnRequestedResolutionDropdownValueChanged(int result)
        {
            if ((int)requestedResolution != result)
            {
                requestedResolution = (ResolutionPreset)result;

                int width, height;
                Dimensions(requestedResolution, out width, out height);

                webCamTextureToMatHelper.Initialize(width, height);
            }
        }

        /// <summary>
        /// Raises the requested resolution dropdown value changed event.
        /// </summary>
        public void OnRequestedCascadeDropdownValueChanged(int result)
        {
            if ((int)requestedResolution != result)
            {
                requestedResolution = (ResolutionPreset)result;

                int width, height;
                Dimensions(requestedResolution, out width, out height);

                webCamTextureToMatHelper.Initialize(width, height);
            }
        }


        /// <summary>
        /// Raises the container dropdown value changed event.
        /// </summary>
        public void OnContainerDropdownValueChanged(int result)
        {
#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR_WIN || UNITY_EDITOR_OSX)
            if ((ContainerPreset)(result) == ContainerPreset.JPG)
            {
                containerDropdown.value = (int)container;

                if (fpsMonitor != null) fpsMonitor.Toast("JPG format is not supported on this platform");

                return;
            }
#endif

            if ((int)container != result)
            {
                container = (ContainerPreset)(result);
            }
        }

        /// <summary>
        /// Raises the video bitrate dropdown value changed event.
        /// </summary>
        public void OnVideoBitRateDropdownValueChanged(int result)
        {
            string[] enumNames = Enum.GetNames(typeof(VideoBitRatePreset));
            int value = (int)System.Enum.Parse(typeof(VideoBitRatePreset), enumNames[result], true);

            if ((int)videoBitRate != value)
            {
                videoBitRate = (VideoBitRatePreset)value;
            }
        }

        /// <summary>
        /// Raises the audio bitrate dropdown value changed event.
        /// </summary>
        public void OnAudioBitRateDropdownValueChanged(int result)
        {
            string[] enumNames = Enum.GetNames(typeof(AudioBitRatePreset));
            int value = (int)System.Enum.Parse(typeof(AudioBitRatePreset), enumNames[result], true);

            if ((int)audioBitRate != value)
            {
                audioBitRate = (AudioBitRatePreset)value;
            }
        }

        /// <summary>
        /// Raises the apply comic filter toggle value changed event.
        /// </summary>
        public void OnApplyComicFilterToggleValueChanged()
        {
            if (applyComicFilter != applyComicFilterToggle.isOn)
            {
                applyComicFilter = applyComicFilterToggle.isOn;
            }
        }

        /// <summary>
        /// Raises the record microphone audio toggle value changed event.
        /// </summary>
        public void OnRecordMicrophoneAudioToggleValueChanged()
        {
            if (recordMicrophoneAudio != recordMicrophoneAudioToggle.isOn)
            {
                recordMicrophoneAudio = recordMicrophoneAudioToggle.isOn;
            }
        }

        /// <summary>
        /// Raises the record video button click event.
        /// </summary>
        public async void OnRecordVideoButtonClick()
        {
            Debug.Log("OnRecordVideoButtonClick ()");

            if (isVideoPlaying)
                return;

            if (!isVideoRecording && !isFinishWriting)
            {
                await StartRecording();
            }
            else
            {
                CancelRecording();
            }
        }

        /// <summary>
        /// Raises the play video button click event.
        /// </summary>
        public void OnPlayVideoButtonClick()
        {
            Debug.Log("OnPlayVideoButtonClick ()");

            if (isVideoPlaying || isVideoRecording || isFinishWriting || string.IsNullOrEmpty(videoPath))
                return;

            if (System.IO.Path.GetExtension(videoPath) == ".gif")
            {
                Debug.LogWarning("GIF format video playback is not supported.");
                if (fpsMonitor != null) fpsMonitor.Toast("GIF format video playback is not supported.");
                return;
            }
            if (System.IO.Path.GetExtension(videoPath) == "")
            {
                Debug.LogWarning("JPG format images playback is not supported.");
                if (fpsMonitor != null) fpsMonitor.Toast("JPG format images playback is not supported.");
                return;
            }

            // Playback the video
            var prefix = Application.platform == RuntimePlatform.IPhonePlayer ? "file://" : "";
            PlayVideo(prefix + videoPath);
        }

        /// <summary>
        /// Raises the play video full screen button click event.
        /// </summary>
        public void OnPlayVideoFullScreenButtonClick()
        {
            Debug.Log("OnPlayVideoFullScreenButtonClick ()");

            if (isVideoPlaying || isVideoRecording || isFinishWriting || string.IsNullOrEmpty(videoPath))
                return;

            // Playback the video
#if UNITY_EDITOR
            UnityEditor.EditorUtility.OpenWithDefaultApp(videoPath);
#elif UNITY_ANDROID || UNITY_IOS
            if (System.IO.Path.GetExtension(videoPath) == ".gif")
            {
                Debug.LogWarning("GIF format video full screen playback is not supported.");
                if (fpsMonitor != null) fpsMonitor.Toast("GIF format video full screen playback is not supported.");
                return;
            }
            if (System.IO.Path.GetExtension(videoPath) == "")
            {
                Debug.LogWarning("JPG format images full screen playback is not supported.");
                if (fpsMonitor != null) fpsMonitor.Toast("JPG format images full screen playback is not supported.");
                return;
            }

            var prefix = Application.platform == RuntimePlatform.IPhonePlayer ? "file://" : "";
            Handheld.PlayFullScreenMovie(prefix + videoPath);
#else
            Debug.LogWarning("Full-screen video playback is not supported on this platform.");
            if (fpsMonitor != null) fpsMonitor.Toast("Full-screen video playback is not supported on this platform.");
#endif
        }

        /// <summary>
        /// Raises the share button click event.
        /// </summary>
        public async void OnShareButtonClick()
        {
            Debug.Log("OnShareButtonClick ()");

            if (isVideoPlaying || isVideoRecording || isFinishWriting || string.IsNullOrEmpty(videoPath))
                return;

            var mes = "";

#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            try
            {
                SharePayload payload = new SharePayload();
                payload.AddText("User shared video! [NatCorderWithOpenCVForUnity Example]");
                payload.AddMedia(videoPath);
                var success = await payload.Commit();

                mes = $"Successfully shared items: {success}";
            }
            catch (ApplicationException e)
            {
                mes = e.Message;
            }
#else
            mes = "NatShare Error: SharePayload is not supported on this platform.";
            await Task.Delay(100);
#endif

            Debug.Log(mes);
            if (fpsMonitor != null) fpsMonitor.Toast(mes);
        }

        /// <summary>
        /// Raises the save to camera roll button click event.
        /// </summary>
        public async void OnSaveToCameraRollButtonClick()
        {
            Debug.Log("OnSaveToCameraRollButtonClick ()");

            if (isVideoPlaying || isVideoRecording || isFinishWriting || string.IsNullOrEmpty(videoPath))
                return;

            var mes = "";

#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            try
            {
                SavePayload payload = new SavePayload("NatCorderWithOpenCVForUnityExample");
                payload.AddMedia(videoPath);
                var success = await payload.Commit();

                mes = $"Successfully saved items: {success}";
            }
            catch (ApplicationException e)
            {
                mes = e.Message;
            }
#else
            mes = "NatShare Error: SavePayload is not supported on this platform.";
            await Task.Delay(100);
#endif

            Debug.Log(mes);
            if (fpsMonitor != null) fpsMonitor.Toast(mes);
        }

        public enum ResolutionPreset
        {
            Lowest,
            _640x480,
            _1280x720,
            _1920x1080,
            Highest,
        }

        private void Dimensions(ResolutionPreset preset, out int width, out int height)
        {
            switch (preset)
            {
                case ResolutionPreset.Lowest:
                    width = height = 50;
                    break;
                case ResolutionPreset._640x480:
                    width = 640;
                    height = 480;
                    break;
                case ResolutionPreset._1920x1080:
                    width = 1920;
                    height = 1080;
                    break;
                case ResolutionPreset.Highest:
                    width = height = 9999;
                    break;
                case ResolutionPreset._1280x720:
                default:
                    width = 1280;
                    height = 720;
                    break;
            }
        }

        public enum ContainerPreset
        {
            MP4,
            HEVC,
            GIF,
            JPG,
        }

        public enum VideoBitRatePreset
        {
            _1Mbps = 1000000,
            _3Mbps = 3000000,
            _5Mbps = 5000000,
            _8Mbps = 8000000,
            _10Mbps = 10000000,
            _15Mbps = 15000000,
        }

        public enum AudioBitRatePreset
        {
            _24Kbps = 24000,
            _48Kbps = 48000,
            _64Kbps = 64000,
            _96Kbps = 96000,
            _128Kbps = 128000,
            _192Kbps = 192000,
        }


        public void Init(string path, bool toggleEnable, string videoPath = "")
        {

            switch (cascadeDropDown.value)
            {

                case 0:
                    LBP_CASCADE_FILENAME = "haarcascade_eye_tree_eyeglasses.xml";
                    break;
                case 1:
                    LBP_CASCADE_FILENAME = "haarcascade_eye.xml";
                    break;
                case 2:
                    LBP_CASCADE_FILENAME = "haarcascade_frontalcatface_extended.xml";
                    break;
                case 3:
                    LBP_CASCADE_FILENAME = "haarcascade_frontalcatface.xml";
                    break;
                case 4:
                    LBP_CASCADE_FILENAME = "haarcascade_frontalface_alt_tree.xml";
                    break;
                case 5:
                    LBP_CASCADE_FILENAME = "haarcascade_frontalface_alt.xml";
                    break;
                case 6:
                    LBP_CASCADE_FILENAME = "haarcascade_frontalface_alt2.xml";
                    break;
                case 7:
                    LBP_CASCADE_FILENAME = "haarcascade_frontalface_default.xml";
                    break;
                case 8:
                    LBP_CASCADE_FILENAME = "haarcascade_fullbody.xml";
                    break;
                case 9:
                    LBP_CASCADE_FILENAME = "haarcascade_lefteye_2splits.xml";
                    break;
                case 10:
                    LBP_CASCADE_FILENAME = "haarcascade_licence_plate_rus_16stages.xml";
                    break;
                case 11:
                    LBP_CASCADE_FILENAME = "haarcascade_lowerbody.xml";
                    break;
                case 12:
                    LBP_CASCADE_FILENAME = "haarcascade_profileface.xml";
                    break;
                case 13:
                    LBP_CASCADE_FILENAME = "haarcascade_righteye_2splits.xml";
                    break;
                case 14:
                    LBP_CASCADE_FILENAME = "haarcascade_russian_plate_number.xml";
                    break;
                case 15:
                    LBP_CASCADE_FILENAME = "haarcascade_smile.xml";
                    break;
                case 16:
                    LBP_CASCADE_FILENAME = "haarcascade_upperbody.xml";
                    break;
                case 17:
                    LBP_CASCADE_FILENAME = "lbpcascade_frontalface.xml";
                    break;
            }
            Debug.Log("ReadCascade = " + cascadeDropDown.value + " : " + LBP_CASCADE_FILENAME);

            _videoPlayer = videoPlayerObj.GetComponent<VideoPlayer>();

            //_videoPlayer = GetComponent<VideoPlayer>();

            microphoneSource = GetComponent<AudioSource>();
            microphoneSource.playOnAwake = false;


            replayCam = replayCamObj.GetComponent<ReplayCam>();
            replayCam.microphoneSource = microphoneSource;

            _videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            _videoPlayer.EnableAudioTrack(0, true);
            _videoPlayer.SetTargetAudioSource(0, microphoneSource);

            if (!toggleEnable)
            {
                _videoPlayer.clip = Resources.Load<VideoClip>("Movies/sample_2");
            }
            else
            {
                _videoPlayer.url = path;

                var info = NativeGallery.GetVideoProperties(videoPath);
                // // y軸を軸にして5度、x軸を軸にして5度、回転させるQuaternionを作成（変数をrotとする）
                // Quaternion rot = Quaternion.Euler(0, -90, 0);
                // // 現在の自信の回転の情報を取得する。
                // Quaternion q = this.transform.rotation;
                // // 合成して、自身に設定
                // this.transform.rotation = q * rot;
                Debug.Log("VideoInfo :  " + info);
                Debug.Log("VideoInfo rotation:  " + info.rotation);
            }

            // y軸を軸にして5度、x軸を軸にして5度、回転させるQuaternionを作成（変数をrotとする）
            // Quaternion rot = Quaternion.Euler(0, 0, -90);
            // // 現在の自信の回転の情報を取得する。
            // Quaternion q = this.transform.rotation;

            // this.transform.rotation = q * rot;


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
            if (savePathInputField.text != string.Empty)
            {
                trackingSize = float.Parse(savePathInputField.text.ToString());
            }

            Debug.Log("trackingSize = " + trackingSize);
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
                        new Size(grayMat.cols() * trackingSize, grayMat.rows() * trackingSize), new Size());


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

                Imgproc.putText(rgbaMat, "W:" + rgbaMat.width() + " H:" + rgbaMat.height() + " SO:" + Screen.orientation + " : " + result, new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                Utils.fastMatToTexture2D(rgbaMat, texture);

                if (videoRecorder != null && (isVideoRecording && !isFinishWriting) && frameCount++ % recordEveryNthFrame == 0)
                {
#if !OPENCV_USE_UNSAFE_CODE
                    MatUtils.copyFromMat(rgbaMat, pixelBuffer);
                    videoRecorder.CommitFrame(pixelBuffer, recordingClock.timestamp);
#else
                unsafe
                {
                    videoRecorder.CommitFrame((void*)rgbaMat.dataAddr(), recordingClock.timestamp);
                }
#endif
                }
            }

            if (_videoPlayer.isPlaying)
            {
                //gameObject.GetComponent<Renderer>().sharedMaterial.mainTexture = _videoPlayer.texture;
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

        public async void FinishMovieRecording()
        {
            videoPath = await replayCam.StopRecording();
        }

        public Camera targetCamera;

        // void Start()
        // {
        //     UpdateScale();
        // }

        [ContextMenu("execute")]
        void UpdateScale()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            float height = targetCamera.orthographicSize * 2;
            float width = height * targetCamera.aspect;

            transform.localScale = new Vector3(width, height, 0);

            // {
            //     //Quadを画面いっぱいに広げる
            //     float _h = targetCamera.orthographicSize * 2;
            //     float _w = _h * targetCamera.aspect;
            //     //_w = _w / (480f / 640f);

            //     if (Width < Height)
            //     {
            //         result = DeviceOrientation.Portrait;
            //     }
            //     else
            //     {
            //         result = DeviceOrientation.LandscapeLeft;
            //     }


            //     // if (result == DeviceOrientation.Unknown)
            //     // {
            //     //     if (Screen.width < Screen.height)
            //     //     {
            //     //         result = DeviceOrientation.Portrait;
            //     //     }
            //     //     else
            //     //     {
            //     //         result = DeviceOrientation.LandscapeLeft;
            //     //     }
            //     // }

            //     Debug.Log("LLLLLL  " + result);
            //     //スマホ(Unity)が横ならそのまま
            //     if (result == DeviceOrientation.LandscapeLeft)
            //     {
            //         transform.localScale = new Vector3(_w, _h, 1);
            //     }
            //     //縦なら回転させる
            //     else if (result == DeviceOrientation.Portrait)
            //     {
            //         transform.localScale = new Vector3(_h, _w, 1);
            //         transform.localRotation *= Quaternion.Euler(0, 0, -90);
            //     }
            // }
        }

        public void Quit()
        {
            Application.Quit();
        }

    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    public class NativeGalleryExample : MonoBehaviour
    {
        [SerializeField]
        private GameObject DisableObject = null;

        [SerializeField]
        private GameObject CameraObject = null;


        [SerializeField]
        private GameObject VideoQuadObject = null;

        [SerializeField]
        private GameObject CanvasObject = null;

        [SerializeField]
        private GameObject Toggle = null;

        void Update()
        {
            // if (Input.GetMouseButtonDown(0))
            // {

            //     // まだカメラにアクセスできない場合スキップ
            //     if (NativeCamera.IsCameraBusy()) return;

            //     // 動画を撮影
            //     NativeCamera.RecordVideo(OnRecord);
            // }


            // if (Input.mousePosition.x < Screen.width / 3)
            // {
            //     // Take a screenshot and save it to Gallery/Photos
            //     StartCoroutine(TakeScreenshotAndSave());
            //     Debug.Log("AAAAAAA");
            // }
            // else
            // {
            //     // Don't attempt to pick media from Gallery/Photos if
            //     // another media pick operation is already in progress
            //     if (NativeGallery.IsMediaPickerBusy())
            //         return;

            //     if (Input.mousePosition.x < Screen.width * 2 / 3)
            //     {
            //         // Pick a PNG image from Gallery/Photos
            //         // If the selected image's width and/or height is greater than 512px, down-scale the image
            //         PickImage(512);
            //         Debug.Log("BBBBBBB");

            //     }
            //     else
            //     {
            //         // Pick a video from Gallery/Photos
            //         PickVideo();
            //         Debug.Log("CCCCCCC");

            //     }
            // }
        }

        // 撮影後に呼び出される
        private void OnRecord(string path)
        {
            // 撮影されていない場合スキップ
            if (string.IsNullOrEmpty(path)) return;

            // 撮影された動画を再生
            Handheld.PlayFullScreenMovie("file://" + path);
        }

        private IEnumerator TakeScreenshotAndSave()
        {
            yield return new WaitForEndOfFrame();

            Texture2D ss = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            ss.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            ss.Apply();

            // Save the screenshot to Gallery/Photos
            NativeGallery.Permission permission = NativeGallery.SaveImageToGallery(ss, "GalleryTest", "Image.png", (success, path) => Debug.Log("Media save result: " + success + " " + path));

            Debug.Log("Permission result: " + permission);

            // To avoid memory leaks
            Destroy(ss);
        }

        public void PickImage(int maxSize = 512)
        {
            NativeGallery.Permission permission = NativeGallery.GetImageFromGallery((path) =>
           {
               Debug.Log("Image path: " + path);
               if (path != null)
               {
                   // Create Texture from selected image
                   Texture2D texture = NativeGallery.LoadImageAtPath(path, maxSize);
                   if (texture == null)
                   {
                       Debug.Log("Couldn't load texture from " + path);
                       return;
                   }

                   // Assign texture to a temporary quad and destroy it after 5 seconds
                   GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                   quad.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 2.5f;
                   quad.transform.forward = Camera.main.transform.forward;
                   quad.transform.localScale = new Vector3(1f, texture.height / (float)texture.width, 1f);

                   Material material = quad.GetComponent<Renderer>().material;
                   if (!material.shader.isSupported) // happens when Standard shader is not included in the build
                       material.shader = Shader.Find("Legacy Shaders/Diffuse");

                   material.mainTexture = texture;

                   Destroy(quad, 5f);

                   // If a procedural texture is not destroyed manually, 
                   // it will only be freed after a scene change
                   Destroy(texture, 5f);
               }
           }, "Select a PNG image", "image/png");

            Debug.Log("Permission result: " + permission);
        }

        public void PickVideo()
        {

            var toggleEnable = Toggle.GetComponent<Toggle>().isOn;

            if (toggleEnable)
            {
                NativeGallery.Permission permission = NativeGallery.GetVideoFromGallery((path) =>
               {
                   var getPath = "file://" + path;

                   Debug.Log("Video path: " + path);
                   if (path != null)
                   {
                       // Play the selected video
                       //Handheld.PlayFullScreenMovie("file://" + path);

                       //CameraObject.GetComponent<VideoScript>().Init(path);

                       //    DisableObject.SetActive(false);
                       //    CanvasObject.SetActive(true);
                       VideoQuadObject.SetActive(true);
                       //VideoQuadObject.GetComponent<VideoRecordingExample>().Init(getPath, toggleEnable);

                   }
               }, "Select a video");

                Debug.Log("Permission result: " + permission);

            }
            else
            {
                // DisableObject.SetActive(false);
                // CanvasObject.SetActive(true);
                VideoQuadObject.SetActive(true);
                //VideoQuadObject.GetComponent<VideoRecordingExample>().Init("", toggleEnable);
            }

        }

        //　動画の読み込みが完了したら呼ばれる
        void PrepareCompleted(VideoPlayer vp)
        {
            vp.prepareCompleted -= PrepareCompleted;
            Debug.Log("ロード完了");
            vp.Play();
        }

        // Example code doesn't use this function but it is here for reference
        private void PickImageOrVideo()
        {
            if (NativeGallery.CanSelectMultipleMediaTypesFromGallery())
            {
                NativeGallery.Permission permission = NativeGallery.GetMixedMediaFromGallery((path) =>
               {
                   Debug.Log("Media path: " + path);
                   if (path != null)
                   {
                       // Determine if user has picked an image, video or neither of these
                       switch (NativeGallery.GetMediaTypeOfFile(path))
                       {
                           case NativeGallery.MediaType.Image: Debug.Log("Picked image"); break;
                           case NativeGallery.MediaType.Video: Debug.Log("Picked video"); break;
                           default: Debug.Log("Probably picked something else"); break;
                       }
                   }
               }, NativeGallery.MediaType.Image | NativeGallery.MediaType.Video, "Select an image or video");

                Debug.Log("Permission result: " + permission);
            }
        }
    }
}
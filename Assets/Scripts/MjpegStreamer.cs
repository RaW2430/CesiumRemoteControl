using System;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Collections;
public class MjpegStreamer : MonoBehaviour
{
    public int port = 8080; // HTTP服务器端口
    private HttpListener listener;
    private Thread listenerThread;
    private Camera mainCamera;
    private RenderTexture renderTexture;
    private Texture2D texture2D;
    private byte[] imageBytes; // 用于存储捕获的画面数据
    private bool isRunning = true;

    void Start()
    {
        // 初始化摄像头和RenderTexture
        mainCamera = Camera.main;
        renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
        texture2D = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);

        // 启动HTTP服务器
        listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();
        Debug.Log("HTTP server started at http://localhost:" + port + "/");

        listenerThread = new Thread(Listen);
        listenerThread.Start();

        // 在主线程中捕获画面
        StartCoroutine(CaptureScreen());
    }

    IEnumerator CaptureScreen()
    {
        while (isRunning)
        {
            // 捕获画面
            mainCamera.targetTexture = renderTexture;
            mainCamera.Render();
            RenderTexture.active = renderTexture;
            texture2D.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            texture2D.Apply();
            mainCamera.targetTexture = null;
            RenderTexture.active = null;

            // 将Texture2D编码为JPEG
            imageBytes = texture2D.EncodeToJPG();

            // 控制帧率（例如30FPS）
            yield return new WaitForSeconds(1f / 30f);
        }
    }

    void Listen()
    {
        while (listener.IsListening)
        {
            try
            {
                var context = listener.GetContext();
                ProcessRequest(context);
            }
            catch (Exception e)
            {
                Debug.LogError("HTTP server error: " + e.Message);
            }
        }
    }

    void ProcessRequest(HttpListenerContext context)
    {
        var response = context.Response;

        // 设置MJPEG流的HTTP头
        response.ContentType = "multipart/x-mixed-replace; boundary=frame";
        response.SendChunked = true;

        while (response.OutputStream.CanWrite && isRunning)
        {
            if (imageBytes != null && imageBytes.Length > 0)
            {
                // 写入HTTP响应
                string header = $"--frame\r\nContent-Type: image/jpeg\r\nContent-Length: {imageBytes.Length}\r\n\r\n";
                byte[] headerBytes = Encoding.ASCII.GetBytes(header);
                response.OutputStream.Write(headerBytes, 0, headerBytes.Length);
                response.OutputStream.Write(imageBytes, 0, imageBytes.Length);
                response.OutputStream.Flush();
            }

            // 控制帧率（例如30FPS）
            Thread.Sleep(33);
        }

        response.OutputStream.Close();
    }

    void OnDestroy()
    {
        // 停止HTTP服务器和子线程
        isRunning = false;
        listener.Stop();
        if (listenerThread != null && listenerThread.IsAlive)
            listenerThread.Abort();
    }
}
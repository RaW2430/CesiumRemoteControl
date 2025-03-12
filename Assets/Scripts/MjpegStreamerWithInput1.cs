//using System;
//using System.Net;
//using System.Text;
//using System.Threading;
//using UnityEngine;
//using System.Collections;

//public class MjpegStreamerWithInput : MonoBehaviour
//{
//    public int port = 8080; // HTTP服务器端口
//    private HttpListener listener;
//    private Thread listenerThread;
//    private Camera mainCamera;
//    private RenderTexture renderTexture;
//    private Texture2D texture2D;
//    private byte[] imageBytes; // 存储画面数据
//    private bool isRunning = true;
//    private Vector2 mousePosition = Vector2.zero; // 存储鼠标位置
//    private string keyboardInput = ""; // 存储键盘输入

//    void Start()
//    {
//        mainCamera = Camera.main;
//        renderTexture = new RenderTexture(1920, 1080, 24);
//        texture2D = new Texture2D(1920, 1080, TextureFormat.RGB24, false);

//        listener = new HttpListener();
//        listener.Prefixes.Add($"http://localhost:{port}/");
//        listener.Start();
//        Debug.Log($"HTTP server started at http://localhost:{port}/");

//        listenerThread = new Thread(Listen);
//        listenerThread.Start();

//        StartCoroutine(CaptureScreen());
//    }

//    IEnumerator CaptureScreen()
//    {
//        while (isRunning)
//        {
//            mainCamera.targetTexture = renderTexture;
//            mainCamera.Render();
//            RenderTexture.active = renderTexture;
//            texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
//            texture2D.Apply();
//            mainCamera.targetTexture = null;
//            RenderTexture.active = null;

//            imageBytes = texture2D.EncodeToJPG();

//            yield return new WaitForSeconds(1f / 30f);
//        }
//    }

//    void Listen()
//    {
//        try
//        {
//            while (listener.IsListening)
//            {
//                var context = listener.GetContext();
//                ThreadPool.QueueUserWorkItem(state => ProcessRequest(context));
//            }
//        }
//        catch (Exception e)
//        {
//            Debug.LogError("HTTP server error: " + e.Message);
//        }
//    }

//    void ProcessRequest(HttpListenerContext context)
//    {
//        var request = context.Request;
//        var response = context.Response;

//        if (request.RawUrl == "/stream")
//        {
//            response.ContentType = "multipart/x-mixed-replace; boundary=frame";
//            response.SendChunked = true;

//            while (response.OutputStream.CanWrite && isRunning)
//            {
//                if (imageBytes != null && imageBytes.Length > 0)
//                {
//                    string header = $"--frame\r\nContent-Type: image/jpeg\r\nContent-Length: {imageBytes.Length}\r\n\r\n";
//                    byte[] headerBytes = Encoding.ASCII.GetBytes(header);
//                    response.OutputStream.Write(headerBytes, 0, headerBytes.Length);
//                    response.OutputStream.Write(imageBytes, 0, imageBytes.Length);
//                    response.OutputStream.Flush();
//                }
//                Thread.Sleep(33);
//            }

//            response.OutputStream.Close();
//        }
//        else if (request.HttpMethod == "POST" && request.RawUrl == "/input")
//        {
//            using (var reader = new System.IO.StreamReader(request.InputStream, Encoding.UTF8))
//            {
//                string inputData = reader.ReadToEnd();
//                Debug.Log("Received input: " + inputData);

//                var input = JsonUtility.FromJson<InputData>(inputData);
//                if (input.type == "mouse")
//                {
//                    mousePosition = new Vector2(input.x, input.y);
//                }
//                else if (input.type == "keyboard")
//                {
//                    keyboardInput = input.key;
//                }
//            }

//            string responseString = "Input received!";
//            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
//            response.ContentType = "text/plain";
//            response.ContentLength64 = buffer.Length;
//            response.OutputStream.Write(buffer, 0, buffer.Length);
//        }
//        else
//        {
//            string html = @"
//            <!DOCTYPE html>
//            <html lang='en'>
//            <head>
//                <meta charset='UTF-8'>
//                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
//                <title>Unity Remote Control</title>
//                <style>
//                    body { margin: 0; overflow: hidden; }
//                    #unity-stream {
//                        width: 100vw;
//                        height: 100vh;
//                        object-fit: cover;
//                    }
//                </style>
//                <script>
//                    function sendInputToUnity(inputData) {
//                        fetch('/input', {
//                            method: 'POST',
//                            headers: { 'Content-Type': 'application/json' },
//                            body: JSON.stringify(inputData)
//                        })
//                        .then(response => response.text())
//                        .then(data => console.log('Response from Unity:', data))
//                        .catch(error => console.error('Error sending input to Unity:', error));
//                    }

//                    document.addEventListener('keydown', function (event) {
//                        sendInputToUnity({ type: 'keyboard', key: event.key });
//                    });

//                    document.addEventListener('mousemove', function (event) {
//                        sendInputToUnity({ type: 'mouse', x: event.clientX, y: event.clientY });
//                    });
//                </script>
//            </head>
//            <body>
//                <img id='unity-stream' src='/stream'>
//            </body>
//            </html>
//            ";

//            byte[] buffer = Encoding.UTF8.GetBytes(html);
//            response.ContentType = "text/html";
//            response.ContentLength64 = buffer.Length;
//            response.OutputStream.Write(buffer, 0, buffer.Length);
//        }

//        response.OutputStream.Close();
//    }

//    void Update()
//    {
//        if (mousePosition != Vector2.zero)
//        {
//            Debug.Log("Mouse position: " + mousePosition);
//            mousePosition = Vector2.zero;
//        }

//        if (!string.IsNullOrEmpty(keyboardInput))
//        {
//            Debug.Log("Keyboard input: " + keyboardInput);
//            keyboardInput = "";
//        }
//    }

//    void OnDestroy()
//    {
//        isRunning = false;
//        listener.Stop();
//        if (listenerThread != null && listenerThread.IsAlive)
//            listenerThread.Abort();
//    }

//    [Serializable]
//    private class InputData
//    {
//        public string type;
//        public float x;
//        public float y;
//        public string key;
//    }
//}


using System;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Collections;

public class MjpegStreamerWithInput : MonoBehaviour
{
    public int port = 8080;
    private HttpListener listener;
    private Thread listenerThread;
    private Camera mainCamera;
    private RenderTexture renderTexture;
    private Texture2D texture2D;
    private byte[] imageBytes;
    private bool isRunning = true;
    private Vector2 mousePosition = Vector2.zero;
    private string keyboardInput = "";

    // 📌 新增摄像机移动参数
    public float moveSpeed = 5f;
    public float rotationSpeed = 2f;

    void Start()
    {
        Application.runInBackground = true;

        mainCamera = Camera.main;
        renderTexture = new RenderTexture(1920, 1080, 24);
        texture2D = new Texture2D(1920, 1080, TextureFormat.RGB24, false);

        listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();
        Debug.Log($"HTTP server started at http://localhost:{port}/");

        listenerThread = new Thread(Listen);
        listenerThread.Start();

        StartCoroutine(CaptureScreen());
    }

    IEnumerator CaptureScreen()
    {
        while (isRunning)
        {
            mainCamera.targetTexture = renderTexture;
            mainCamera.Render();
            RenderTexture.active = renderTexture;
            texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture2D.Apply();
            mainCamera.targetTexture = null;
            RenderTexture.active = null;

            imageBytes = texture2D.EncodeToJPG();
            yield return new WaitForSeconds(1f / 30f);
        }
    }

    void Listen()
    {
        try
        {
            while (listener.IsListening)
            {
                var context = listener.GetContext();
                ThreadPool.QueueUserWorkItem(state => ProcessRequest(context));
            }
        }
        catch (Exception e)
        {
            Debug.LogError("HTTP server error: " + e.Message);
        }
    }

    void ProcessRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        if (request.RawUrl == "/stream")
        {
            response.ContentType = "multipart/x-mixed-replace; boundary=frame";
            response.SendChunked = true;

            while (response.OutputStream.CanWrite && isRunning)
            {
                if (imageBytes != null && imageBytes.Length > 0)
                {
                    string header = $"--frame\r\nContent-Type: image/jpeg\r\nContent-Length: {imageBytes.Length}\r\n\r\n";
                    byte[] headerBytes = Encoding.ASCII.GetBytes(header);
                    response.OutputStream.Write(headerBytes, 0, headerBytes.Length);
                    response.OutputStream.Write(imageBytes, 0, imageBytes.Length);
                    response.OutputStream.Flush();
                }
                Thread.Sleep(33);
            }

            response.OutputStream.Close();
        }
        else if (request.HttpMethod == "POST" && request.RawUrl == "/input")
        {
            using (var reader = new System.IO.StreamReader(request.InputStream, Encoding.UTF8))
            {
                string inputData = reader.ReadToEnd();
                Debug.Log("Received input: " + inputData);

                var input = JsonUtility.FromJson<InputData>(inputData);
                if (input.type == "mouse")
                {
                    mousePosition = new Vector2(input.x, input.y);
                }
                else if (input.type == "keyboard")
                {
                    keyboardInput = input.key;
                }
            }

            string responseString = "Input received!";
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentType = "text/plain";
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
        }
        else
        {
            string html = @"
            <!DOCTYPE html>
            <html lang=""en"">
            <head>
                <meta charset=""UTF-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                <title>Unity Remote Control</title>
                <style>
                    body {
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        height: 100vh;
                        margin: 0;
                        background-color: #222;
                    }
                    #unity-container {
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        width: 80vw; /* 画面最大宽度 */
                        height: 80vh; /* 画面最大高度 */
                    }
                    #unity-stream {
                        width: 100%;
                        height: 100%;
                        object-fit: cover;
                        aspect-ratio: 16 / 9; /* 保持16:9比例 */
                        border-radius: 15px;
                        box-shadow: 0px 0px 20px rgba(0, 0, 0, 0.5);
                        background-color: black;
                    }
                </style>
                <script>
                    function sendInputToUnity(inputData) {
                        fetch('/input', {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify(inputData)
                        })
                        .then(response => response.text())
                        .then(data => console.log('Response from Unity:', data))
                        .catch(error => console.error('Error sending input to Unity:', error));
                    }

                    document.addEventListener('keydown', function (event) {
                        sendInputToUnity({ type: 'keyboard', key: event.key });
                    });

                    document.addEventListener('mousemove', function (event) {
                        sendInputToUnity({ type: 'mouse', x: event.movementX, y: event.movementY });
                    });
                </script>
            </head>
            <body>
                <div id=""unity-container"">
                    <img id=""unity-stream"" src=""/stream"">
                </div>
            </body>
            </html>

            ";

            byte[] buffer = Encoding.UTF8.GetBytes(html);
            response.ContentType = "text/html";
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
        }

        response.OutputStream.Close();
    }

    void Update()
    {
        // 计算鼠标移动的旋转量
        float rotX = mousePosition.x * rotationSpeed * Time.deltaTime;  // 鼠标水平移动（X轴旋转）
        float rotY = mousePosition.y * rotationSpeed * Time.deltaTime;  // 鼠标垂直移动（Y轴旋转）

        // 获取当前旋转角度
        Vector3 currentRotation = mainCamera.transform.rotation.eulerAngles;

        // 计算新的旋转值，限制摄像头的旋转范围（特别是Y轴，防止翻转）
        float newRotX = currentRotation.x + rotY; // X 轴（俯仰）旋转
        float newRotY = currentRotation.y + rotX; // Y 轴（偏航）旋转

        // 限制垂直旋转的范围（防止摄像机翻转）
        //newRotX = Mathf.Clamp(newRotX, -80f, 80f); // 只允许上下旋转，防止翻转

        // 应用新的旋转
        mainCamera.transform.rotation = Quaternion.Euler(newRotX, newRotY, 0);

        // 重置鼠标位置
        mousePosition = Vector2.zero;

        // 控制摄像机移动（键盘）
        Vector3 moveDirection = Vector3.zero;

        // 前后左右控制 - W, A, S, D
        if (keyboardInput == "w") moveDirection += mainCamera.transform.forward;
        if (keyboardInput == "s") moveDirection -= mainCamera.transform.forward;
        if (keyboardInput == "a") moveDirection -= mainCamera.transform.right;
        if (keyboardInput == "d") moveDirection += mainCamera.transform.right;

        // 上下控制 - Q, E
        if (keyboardInput == "q") moveDirection += mainCamera.transform.up;
        if (keyboardInput == "e") moveDirection -= mainCamera.transform.up;

        // 移动摄像机
        if (moveDirection != Vector3.zero)
        {
            mainCamera.transform.position += moveDirection * moveSpeed * Time.deltaTime;
            keyboardInput = ""; // 移动后重置键盘输入
        }
    }




    void OnDestroy()
    {
        isRunning = false;
        listener.Stop();
        if (listenerThread != null && listenerThread.IsAlive)
            listenerThread.Abort();
    }

    [Serializable]
    private class InputData
    {
        public string type;
        public float x;
        public float y;
        public string key;
    }
}

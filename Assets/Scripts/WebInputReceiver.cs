using System;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

public class WebInputReceiver : MonoBehaviour
{
    public int port = 8888; // HTTP�������˿�
    private HttpListener listener;
    private Thread listenerThread;

    void Start()
    {
        // ����HTTP������
        listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();
        Debug.Log($"HTTP server started at http://localhost:{port}/");

        // ���������߳�
        listenerThread = new Thread(Listen);
        listenerThread.Start();
    }

    void Listen()
    {
        while (listener.IsListening)
        {
            try
            {
                // �ȴ��ͻ�������
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
        var request = context.Request;
        var response = context.Response;

        if (request.HttpMethod == "POST")
        {
            // ��ȡPOST����
            using (var reader = new System.IO.StreamReader(request.InputStream, Encoding.UTF8))
            {
                string inputData = reader.ReadToEnd();
                Debug.Log("Received input: " + inputData);

                // ������������
                var input = JsonUtility.FromJson<InputData>(inputData);
                if (input.type == "keyboard")
                {
                    Debug.Log("Key pressed: " + input.key);
                }
                else if (input.type == "mouse")
                {
                    Debug.Log("Mouse moved: (" + input.x + ", " + input.y + ")");
                }
            }

            // ������Ӧ
            string responseString = "Input received!";
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentType = "text/plain";
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
        }
        else
        {
            // ���ذ��� JavaScript �� HTML ҳ��
            string html = @"
                <!DOCTYPE html>
                <html lang='en'>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Unity Web Input</title>
                    <script>
                        // ��������¼�
                        document.addEventListener('keydown', function (event) {
                            const inputData = { type: 'keyboard', key: event.key };
                            sendInputToUnity(inputData);
                        });

                        // ��������¼�
                        document.addEventListener('mousemove', function (event) {
                            const inputData = { type: 'mouse', x: event.clientX, y: event.clientY };
                            sendInputToUnity(inputData);
                        });

                        // �����������ݵ� Unity
                        function sendInputToUnity(inputData) {
                            fetch('/', {
                                method: 'POST',
                                headers: { 'Content-Type': 'application/json' },
                                body: JSON.stringify(inputData)
                            })
                            .then(response => response.text())
                            .then(data => console.log('Response from Unity:', data))
                            .catch(error => console.error('Error sending input to Unity:', error));
                        }
                    </script>
                </head>
                <body>
                    <h1>Unity Web Input</h1>
                    <p>Press keys or move the mouse to send input to Unity.</p>
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

    void OnDestroy()
    {
        // ֹͣHTTP������
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
using System;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

public class SimpleHttpServer : MonoBehaviour
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
        var response = context.Response;

        // ������Ӧ����
        string responseString = "hello!";
        byte[] buffer = Encoding.UTF8.GetBytes(responseString);

        // ����HTTPͷ
        response.ContentType = "text/plain";
        response.ContentLength64 = buffer.Length;

        // ������Ӧ
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }

    void OnDestroy()
    {
        // ֹͣHTTP������
        listener.Stop();
        if (listenerThread != null && listenerThread.IsAlive)
            listenerThread.Abort();
    }
}
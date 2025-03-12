using System;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

public class SimpleHttpServer : MonoBehaviour
{
    public int port = 8888; // HTTP服务器端口
    private HttpListener listener;
    private Thread listenerThread;

    void Start()
    {
        // 启动HTTP服务器
        listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();
        Debug.Log($"HTTP server started at http://localhost:{port}/");

        // 启动监听线程
        listenerThread = new Thread(Listen);
        listenerThread.Start();
    }

    void Listen()
    {
        while (listener.IsListening)
        {
            try
            {
                // 等待客户端请求
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

        // 设置响应内容
        string responseString = "hello!";
        byte[] buffer = Encoding.UTF8.GetBytes(responseString);

        // 设置HTTP头
        response.ContentType = "text/plain";
        response.ContentLength64 = buffer.Length;

        // 发送响应
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }

    void OnDestroy()
    {
        // 停止HTTP服务器
        listener.Stop();
        if (listenerThread != null && listenerThread.IsAlive)
            listenerThread.Abort();
    }
}
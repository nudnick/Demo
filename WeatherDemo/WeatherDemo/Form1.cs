using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System.Web;
using Newtonsoft.Json;

namespace WeatherDemo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        Dictionary<string, string> ParamList = new Dictionary<string, string>();

        public void Start()
        {
            //创建TcpListener
             TcpListener serverListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 8081);
            //开始监听
            serverListener.Start(10);

            while (true)
            {
                //获取客户端连接
                TcpClient acceptClient = serverListener.AcceptTcpClient();

                //获取请求报文
                NetworkStream netstream = acceptClient.GetStream();
                
                //解析请求报文
                byte[] bytes = new byte[1024];
                int length = netstream.Read(bytes, 0, bytes.Length);
                string requestString = Encoding.UTF8.GetString(bytes, 0, length);

                string content = String.Empty;

                if (requestString.ToUpper().StartsWith("GET") || requestString.ToUpper().StartsWith("POST") || requestString.ToUpper().StartsWith("PUT"))
                {
                    string[] tokens = requestString.Split(' ');
                    string Method = tokens[0].ToUpper();
                    if (Method != "GET")
                        Method = "POST";
                    string OrigUrl = tokens[1].Trim();
                    string http_url_params = GetStringItemBetween2Key(OrigUrl, "?", "");

                    string HandlerUrl = OrigUrl;
                    if (HandlerUrl.IndexOf("?") > 0)
                    {
                        HandlerUrl = GetStringItemBetween2Key(HandlerUrl, "", "?");
                    }
                    string PacketType = HandlerUrl;

                    if (PacketType.IndexOf("/") >= 0)
                    {
                        if (PacketType.StartsWith("/"))
                            PacketType = PacketType.Split('/')[1];
                        else
                            PacketType = PacketType.Split('/')[0];
                    }
                    if (PacketType.IndexOf(" ") > 0)
                        PacketType = GetStringItemBetween2Key(PacketType, "", " ");

                    content = GetHttpBody(Method, requestString, OrigUrl);
                    //requestString = requestString.Replace("\r\n", "").Trim();
                }

                //以下为响应报文(略)

                LoadParam(content);

                if (!String.IsNullOrEmpty(GetParam("cityName")))
                {
                    string cityName = HttpUtility.UrlDecode(GetParam("cityName"), System.Text.Encoding.GetEncoding("GB2312"));

                    string url = "http://wthrcdn.etouch.cn/weather_mini?city=" + cityName;

                    string ret = HttpGet(url, true);

                    //txt.Text += "\r\n";

                    //txt.Text += ret;

                    //SendHttpResponse(acceptClient, cityName);

                    string code = "";

                    Dictionary<string, string> WeatherCode = JsonConvert.DeserializeObject<Dictionary<string, string>>(Tools.WeatherJson);

                    foreach (var item in WeatherCode)
                    {
                        if (item.Value.Contains(cityName))
                        {
                            code = item.Key;
                            break;
                        }
                    }

                    txt.Text += "\r\n\r\n" + code;

                    url = "http://t.weather.sojson.com/api/weather/city/" + code;

                    ret = HttpGet(url);

                    txt.Text += "\r\n\r\n" + ret;
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                System.Threading.Thread th = new System.Threading.Thread(Start);
                Control.CheckForIllegalCrossThreadCalls = false;
                th.Start();

                txt.Text = "服务运行中……";
            }
            catch (Exception ex)
            {
                txt.Text = ex.Message;
            }
        }

        public static string GetStringItemBetween2Key(string source, string startkey, string endkey)
        {
            try
            {
                string ret = "";
                string tmp = "", str = "";
                int pos1 = -1;
                int pos2 = -1;
                pos1 = source.IndexOf(startkey);
                if (pos1 < 0)
                    return "";
                if (endkey != "")
                {
                    pos2 = source.IndexOf(endkey, pos1 + startkey.Length);
                    if (pos2 < 0)
                        return "";
                }
                else
                    return source.Substring(pos1 + startkey.Length);

                ret = source.Substring(pos1 + startkey.Length, pos2 - pos1 - endkey.Length - (startkey.Length - endkey.Length));
                return ret;
            }
            catch
            {
                return "";
            }
        }

        string GetHttpBody(string Method, string src, string FullUrl)
        {
            string ret = src;

            if (Method.Equals("POST"))
            {
                if (src.IndexOf("\r\n\r\n") >= 0)
                    ret = GetStringItemBetween2Key(src, "\r\n\r\n", "");
                else ret = src;
            }
            else if (Method.Equals("GET"))
            {
                ret = GetStringItemBetween2Key(FullUrl, "?", "");
            }
            else
            {
                if (src.IndexOf("\r\n\r\n") >= 0)
                    ret = GetStringItemBetween2Key(src, "\r\n\r\n", "");
                else
                    ret = src;
            }

            return ret;
        }

        public string HttpGet(string Url, bool isUncompress = false)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            //返回的数据被压缩了
            Stream myResponseStream = null;

            if (isUncompress)
            {
                myResponseStream = new System.IO.Compression.GZipStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
            }
            else
            {
                myResponseStream = response.GetResponseStream();
            }
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.UTF8);
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();

            return retString;
        }

        public void LoadParam(string OrderContent)
        {
            OrderContent = System.Web.HttpUtility.UrlDecode(OrderContent);
            string[] ItemArray = OrderContent.Split('&');
            ParamList = new Dictionary<string, string>();
            for (int i = 0; i < ItemArray.Length; i++)
            {
                string[] Item = ItemArray[i].Split('=');
                try
                {
                    if (!ParamList.ContainsKey(Item[0].Trim().ToLower()))
                    {
                        ParamList.Add(Item[0].Trim().ToLower(), Item[1].Trim());
                    }
                }
                catch (System.Exception ex)
                {

                }
            }
        }

        public string GetParam(string key)
        {
            key = key.ToLower();
            if (ParamList.ContainsKey(key))
                return ParamList[key];
            return string.Empty;
        }

        public bool SendHttpResponse(Socket s, string Content)
        {
            bool ret = true;
            string sendmsgbody = Content;
            string sendmsgHead = "HTTP/1.1 200 OK\r\n";
            sendmsgHead += "Content-Length: ";
            byte[] body = System.Text.Encoding.UTF8.GetBytes(sendmsgbody.ToCharArray());
            sendmsgHead += body.Length + "\r\n";
            sendmsgHead += "Content-Type: text/html; charset=utf-8;\r\n";
            sendmsgHead += "\r\n";
            try
            {
                byte[] head = System.Text.Encoding.UTF8.GetBytes(sendmsgHead.ToCharArray());
                byte[] data = new byte[head.Length + body.Length];
                Buffer.BlockCopy(head, 0, data, 0, head.Length);
                Buffer.BlockCopy(body, 0, data, head.Length, body.Length);
                s.Send(data, 0, data.Length, SocketFlags.None);
            }
            catch (Exception)
            {
            }
            return ret;
        }
    }
}

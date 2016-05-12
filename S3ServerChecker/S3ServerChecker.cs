using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.NetworkInformation;
using System.Threading;
using System.Diagnostics;
using System.Net.Mail;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace S3ServerChecker
{
    class Program
    {
        public static bool isConnect(string ServerName)
        {
            Stopwatch sw = new Stopwatch();
            sw.Reset();

            string serverip = ServerName;
            Ping p = new Ping();
            PingReply reply;

            bool connect = false;
            try
            {
                sw.Start();
                reply = p.Send(serverip);
                for(int i=1;i<=5;i++)
                { 
                    if (reply.Status == IPStatus.Success)
                    {
                        connect = true;
                        break;
                    }
                    else
                    {
                        //retry delay time (ms)
                        Thread.Sleep(10000);
                        EventLog.Write("Retry " + i.ToString()+" Times");
                    }
                }
                if (connect == true)
                {
                    sw.Stop();
                    Console.WriteLine(sw.ElapsedMilliseconds.ToString());
                    return true;
                }
                else
                {
                    sw.Stop();
                    Console.WriteLine(sw.ElapsedMilliseconds.ToString());
                    return false;
                }
            }
            catch
            {
                sw.Stop();
                Console.WriteLine(sw.ElapsedMilliseconds.ToString());
                return false;
            }
        }

        public static List<string> FileList(string RootPath, string Type, int PastHour)
        {
            try
            {
                //S3 Server log forder format: {root}/{utc yyyymmdd}/{hh00}/files
                //Night log forder format: {root}/{yyyymmdd}/files

                EventLog.Write("[FileList] +++");
                List<string> DirectoryPathList = new List<string>();
                //檔案路徑列表
                List<string> FilePathList = new List<string>();
                switch (Type)
                {
                    case "NightLog":
                        string date_now = DateTime.Now.ToString("yyyyMMdd");

                        //搜尋目錄
                        DirectoryPathList.Add(RootPath + date_now);
                        EventLog.Write("[FileList] Check files from " + RootPath + date_now);
                        //循環搜尋
                        while (DirectoryPathList.Count > 0)
                        {
                            string DirectoryPath = DirectoryPathList[0];
                            Console.WriteLine(DirectoryPath);
                            DirectoryPathList.RemoveAt(0);
                            FilePathList.AddRange(Directory.GetFiles(DirectoryPath));
                            DirectoryPathList.AddRange(Directory.GetDirectories(DirectoryPath));
                        }
                        EventLog.Write("[FileList] Files: {0}", FilePathList.Count);
                        EventLog.Write("[FileList] ---");
                        return FilePathList;
                    default:
                        string utc_date = DateTime.UtcNow.AddHours(-PastHour).ToString("yyyyMMdd");
                        string utc_hour = DateTime.UtcNow.AddHours(-PastHour).ToString("hh00");
                        //搜尋目錄
                        DirectoryPathList.Add(RootPath + utc_date + "\\" + utc_hour);
                        EventLog.Write("[FileList] Check files from " + RootPath + utc_date + "\\" + utc_hour);
                        //循環搜尋
                        while (DirectoryPathList.Count > 0)
                        {
                            string DirectoryPath = DirectoryPathList[0];
                            Console.WriteLine(DirectoryPath);
                            DirectoryPathList.RemoveAt(0);
                            FilePathList.AddRange(Directory.GetFiles(DirectoryPath));
                            DirectoryPathList.AddRange(Directory.GetDirectories(DirectoryPath));
                        }

                        EventLog.Write("[FileList] Files: {0}", FilePathList.Count);
                        EventLog.Write("[FileList] ---");
                        return FilePathList;
                }
            }
            catch (Exception e)
            {
                List<string> FilePathList = new List<string>();
                Console.WriteLine(e);
                EventLog.Write(e.ToString());
                return FilePathList;
            } 
        }

        public static int LogCount(List<string> FileList,string Match_Pattern)
        {
            EventLog.Write("[LogCount] Check "+ Match_Pattern +" +++");
            int MatchCount = 0;
            Regex rgx = new Regex(Match_Pattern, RegexOptions.IgnoreCase);

            foreach (string filePath in FileList)
            {
                if (rgx.IsMatch(filePath) == true)
                {
                    MatchCount++;
                }
                //Console.WriteLine(filePath);
            }
            EventLog.Write("[LogCount] Check "+ Match_Pattern + " ---");
            return MatchCount;
        }

        public static void Mail(string MailServer, string Sender,string Receiver,string Mailbody)
        {
            try
            {
                SmtpClient client = new SmtpClient(MailServer);
                MailMessage msg = null;
                Console.WriteLine("Send Mail...");
                EventLog.Write(Sender + Receiver + Mailbody);
                msg=new MailMessage(Sender, Receiver, "Notice:[LogSync] S3Server Error Found", Mailbody);
                msg.IsBodyHtml = true;
                client.Send(msg);
                Console.WriteLine("Finish");
            }
            catch (Exception e)
            {
                Console.WriteLine("Fail" + e);
                EventLog.Write(e.ToString());
            }
        }

        static void Main(string[] args)
        {
            EventLog.Write("[Main] Start Check +++");
            StringBuilder mail_body = new StringBuilder();
            mail_body.Append("Hi all,</br>MASD Log Server error was detected, please check below problems.</br></br>");
            bool error_flag = false;
            try {
                //Check Config
                #region Check Config     
                string cfg_path = Directory.GetCurrentDirectory() + "\\cfg.json";

                if (File.Exists(cfg_path) == false)
                {
                    EventLog.Write("Can't find configuration at " + cfg_path);
                    EventLog.Write("Please check source code for more detail about configuration");

                    #region Configuration Format
                    //{	
                    //	"ServerName":["prtaofs01_tellhtc.htc.com.tw","prtaosamba01_tellhtc.htc.com.tw","10.122.130.182"],
                    //	"MailServer":"10.122.128.28",
                    //	"MailSender":"yuhsuan_chen@htc.com",
                    //	"MailReceiver":"yuhsuan_chen@htc.com",
                    //	"CheckItems":{
                    //		"0":{
                    //			"Type":"LasK",
                    //			"RootPath":"T:\\",
                    //			"FileMatch":"LASTKMSG"
                    //		},
                    //		"1":{
                    //			"Type":"ModemReset",
                    //			"RootPath":"T:\\",
                    //			"FileMatch":"HTC_MODEM_RESET"
                    //		},
                    //		"2":{
                    //			"Type":"PES",
                    //			"RootPath":"T:\\abnormal",
                    //			"FileMatch":"HTC_PWR"
                    //		}
                    //	}
                    //}
                    #endregion
                }
                #endregion
                else
                {
                    //Import configuration
                    #region Import configuration
                    string cfg = "";
                    StreamReader sr = new StreamReader(cfg_path);
                    cfg = sr.ReadToEnd();
                    sr.Close();

                    JObject json = JObject.Parse(cfg);
                    #endregion

                    //TestServerConnection
                    #region TestServerConnection
                    EventLog.Write("[Check Connetion] +++");
                    //Test Server Connection
                    foreach (string ServerName in json["ServerName"])
                    {
                        EventLog.Write(ServerName + " testing...");
                        if (isConnect(ServerName) == true)
                        {
                            EventLog.Write(ServerName.ToString() + "Connection Sucess");
                        }
                        else
                        {
                            error_flag = true;
                            EventLog.Write("[Check Connetion] " + ServerName.ToString()+ "connection is fail");
                            mail_body.Append("[Check Connetion] "+ServerName.ToString() + "connection is fail</br>");
                        }
                    }
                    EventLog.Write("[Check Connetion] ---");
                    #endregion

                    //Check File
                    #region Check File
                    EventLog.Write("[Check File] +++");
                    foreach (JProperty item in json["CheckItems"])
                    {
                        Console.WriteLine(item.Name);
                        EventLog.Write(item.Value["Type"].ToString());
                        Console.WriteLine(item.Value["RootPath"]);
                        Console.WriteLine(item.Value["FileMatch"]);
                        Console.WriteLine(item.Value["HourCount"]);

                        if (Directory.Exists(item.Value["RootPath"].ToString()))
                        {
                            List<int> HourCount = new List<int>();

                            for (int PastHour = 0; PastHour <= 3; PastHour++)
                            {
                                //Get file list
                                List<string> FileLst = FileList(item.Value["RootPath"].ToString(), item.Value["Type"].ToString(), PastHour);
                                EventLog.Write("[Check File] "+item.Value["Type"].ToString() + " total file count is : " + FileLst.Count.ToString());
                                //Check LastK
                                int ThisHourCount = LogCount(FileLst, item.Value["FileMatch"].ToString());
                                EventLog.Write("[Check File] " + item.Value["Type"].ToString() + " : " + ThisHourCount.ToString()+" Hour "+PastHour.ToString());
                                HourCount.Add(ThisHourCount);
                            }

                            //Count the log in last 3 hours include this hour
                            int ThreeHourCount = 0;
                            foreach (int Hour in HourCount)
                            {
                                //EventLog.Write(Hour.ToString());
                                ThreeHourCount = ThreeHourCount + Hour;
                            }

                            if (ThreeHourCount == 0) // No any Log
                            {
                                error_flag = true;
                                mail_body.Append("[Check File] " + item.Value["Type"].ToString() + " Log in last 3 hours count is 0.<br/>");
                                EventLog.Write("[Check File] " + item.Value["Type"].ToString()+ " Log in last 3 hours count is 0.< br /> ");
                            }
                            else
                            {
                                error_flag = false;
                                mail_body.Append("[Check File] " + item.Value["Type"].ToString() + " Log in last 3 hours count is " + ThreeHourCount.ToString() + "<br/>");
                                EventLog.Write("[Check File] " + item.Value["Type"].ToString()+ " Log in last 3 hours count is " + ThreeHourCount.ToString());
                            }
                        }
                        else
                        {
                            //Output error file path and do some test 
                            error_flag = true;
                            mail_body.Append("[Check File] " + "Can't Find The Path: " + item.Value["RootPath"].ToString() + "<br/>");
                            EventLog.Write("[Check File] " + "Can't Find The Path: " + item.Value["RootPath"].ToString());
                            //Check NET USE
                            #region Check Net Use
                            EventLog.Write("[Check File] Error Found and Print NET USE status");
                            Process p = new Process();
                            p.StartInfo.RedirectStandardOutput = true;
                            p.StartInfo.RedirectStandardInput = true;
                            p.StartInfo.CreateNoWindow = true;
                            p.StartInfo.UseShellExecute = false;
                            p.StartInfo.FileName = @"cmd.exe";
                            p.Start();
                            p.StandardInput.WriteLine(@"NET USE");
                            p.StandardInput.WriteLine(@"EXIT");
                            StringBuilder NetUse = new StringBuilder();
                            NetUse.Append("[Check File] Error Found and Print NET USE status"+ "<br/>");
                            while (!p.StandardOutput.EndOfStream)
                            {
                                string line = p.StandardOutput.ReadLine();
                                EventLog.Write(line);
                                NetUse.Append(line+ "<br/>");
                            }
                            p.WaitForExit();
                            mail_body.Append(NetUse.ToString());
                            #endregion

                            //Check PingInfo
                            #region Check Pinginfo
                            EventLog.Write("[Check Connetion] +++");
                            //Test Server Connection
                            foreach (string ServerName in json["ServerName"])
                            {
                                EventLog.Write(ServerName + " testing...");
                                mail_body.Append(ServerName + " testing..." + "<br/>");
                                if (isConnect(ServerName) == true)
                                {
                                    EventLog.Write(ServerName.ToString() + "Connection Sucess");
                                }
                                else
                                {
                                    error_flag = true;
                                    EventLog.Write("[Check Connetion] " + ServerName.ToString() + "connection is fail");
                                    mail_body.Append("[Check Connetion] " + ServerName.ToString() + "connection is fail</br>");
                                }
                            }
                            EventLog.Write("[Check Connetion] ---");
                            #endregion
                            break;                            
                        }
                    }
                    EventLog.Write("[Check File] ---");
                    #endregion
                    
                    //Send Mail
                    #region SendMail
                    if (error_flag == true)
                    {
                        EventLog.Write("[Sned Mail] +++");
                        EventLog.Write(mail_body.ToString());
                        Mail(json["MailServer"].ToString(), json["MailSender"].ToString(), json["MailReceiver"].ToString(), mail_body.ToString());

                        EventLog.Write("[Sned Mail] ---");
                    }
                    #endregion
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                EventLog.Write(e.ToString());
            }
            //Console.ReadLine();
            EventLog.Write("[Main] Start Check ---");
            EventLog.Write("====================================");
        }
    }
}

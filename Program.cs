// Author: Darren K.J. Chen
// Copyright © 2020 Darren ( Kuan-Ju ) Chen | All Rights Reserved

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Intelligence;
using Package;

namespace AutoOptionsArbitrage
{
    class Program
    {
        static QuoteAPI quoteAPI = new QuoteAPI();
        static void Main(string[] argv)
        {
            quoteAPI.QuoteLogin();
            Thread.Sleep(2888);
            quoteAPI.uniSub("TXO13000V0");
            //quoteAPI.lastPriceQuote("TXO13000V0");
        }
        class QuoteAPI
        {
            #region INIT
            public UTF8Encoding encoding = new System.Text.UTF8Encoding();
            Dictionary<string, int> RecoverMap = new Dictionary<string, int>();
            #endregion

            #region ConnectINFO
            static string srcId = "API";
            static string tkn = "b6eb";
            static string host = "iquotetest.kgi.com.tw";
            static ushort port = 443;
            #endregion

            Intelligence.QuoteCom quoteCom = new Intelligence.QuoteCom(host, port, srcId, tkn);

            #region Debug WARNING
            //static bool chk_40020 = false;
            //static bool chk_40080 = false;
            #endregion
            public void QuoteLogin()
            {
                quoteCom.OnGetStatus += OnQuoteGetStatus;
                quoteCom.OnRcvMessage += OnQuoteRcvMessage;

                //Console.WriteLine("\nQuoteCom [ Version : " + quoteCom.version + " ]\n Device Ip: " + quoteCom.MyIP);

                quoteCom.Connect(host, port);
                Thread.Sleep(2222);

                string id = "F131039608";
                string pwd = "0000";
                char center = ' ';

                quoteCom.SourceId = srcId;
                quoteCom.Login(id, pwd, center);

                Console.WriteLine("\nQuoteCom [ Version : " + quoteCom.version + " ]\nDevice IP: " + quoteCom.MyIP);
            }
            public void uniSub(string quoteCode)
            {
                short istatus = quoteCom.SubQuote(quoteCode);
                if (istatus < 0)
                    Console.WriteLine(quoteCom.GetSubQuoteMsg(istatus));

                //return "NULL-Type: string";
            }
            public void lastPriceQuote(string quoteCode)
            {
                short istatus = quoteCom.RetriveLastPrice(quoteCode);
                if (istatus < 0)
                    Console.WriteLine(quoteCom.GetSubQuoteMsg(istatus));

                //return "NULL-Type: string";
            }
            public void closePriceQuote()
            {
                short istatus = quoteCom.RetriveClosePrice();
                if (istatus < 0)
                    Console.WriteLine(quoteCom.GetSubQuoteMsg(istatus));
            }

            #region OnQUOTE_GET_STATUS
            private void OnQuoteGetStatus(object sender, COM_STATUS staus, byte[] msg)
            {
                QuoteCom com = (QuoteCom)sender;
                string smsg = "";
                switch (staus)
                {
                    case COM_STATUS.LOGIN_READY:
                        Console.WriteLine(String.Format("LOGIN_READY"));
                        Console.WriteLine(smsg);
                        break;
                    case COM_STATUS.LOGIN_FAIL:
                        Console.WriteLine(String.Format("LOGIN FAIL:[{0}]", encoding.GetString(msg)));
                        break;
                    case COM_STATUS.LOGIN_UNKNOW:
                        Console.WriteLine(String.Format("LOGIN UNKNOW:[{0}]", encoding.GetString(msg)));
                        break;
                    case COM_STATUS.CONNECT_READY:
                        smsg = "QuoteCom: [" + encoding.GetString(msg) + "] MyIP=" + quoteCom.MyIP;
                        break;
                    case COM_STATUS.CONNECT_FAIL:
                        smsg = encoding.GetString(msg);
                        Console.WriteLine("CONNECT_FAIL:" + smsg);
                        break;
                    case COM_STATUS.DISCONNECTED:
                        smsg = encoding.GetString(msg);
                        Console.WriteLine("DISCONNECTED:" + smsg);
                        break;
                    case COM_STATUS.SUBSCRIBE:
                        smsg = encoding.GetString(msg, 0, msg.Length - 1);
                        Console.WriteLine(String.Format("SUBSCRIBE:[{0}]", smsg));
                        break;
                    case COM_STATUS.UNSUBSCRIBE:
                        smsg = encoding.GetString(msg, 0, msg.Length - 1);
                        Console.WriteLine(String.Format("UNSUBSCRIBE:[{0}]", smsg));
                        break;
                    case COM_STATUS.ACK_REQUESTID:
                        long RequestId = BitConverter.ToInt64(msg, 0);
                        byte status = msg[8];
                        Console.WriteLine("Request Id BACK: " + RequestId + " Status=" + status);
                        break;
                    case COM_STATUS.RECOVER_DATA:
                        smsg = encoding.GetString(msg, 1, msg.Length - 1);
                        if (!RecoverMap.ContainsKey(smsg))
                            RecoverMap.Add(smsg, 0);

                        if (msg[0] == 0)
                        {
                            RecoverMap[smsg] = 0;
                            Console.WriteLine(String.Format("開始回補 Topic:[{0}]", smsg));
                        }

                        if (msg[0] == 1)
                        {
                            Console.WriteLine(String.Format("結束回補 Topic:[{0} 筆數:{1}]", smsg, RecoverMap[smsg]));
                        }
                        break;
                }
                com.Processed();
            }
            #endregion

            private void OnQuoteRcvMessage(object sender, PackageBase package)
            {
                if (package.TOPIC != null)
                    if (RecoverMap.ContainsKey(package.TOPIC))
                        RecoverMap[package.TOPIC]++;
                StringBuilder sb;

                switch (package.DT)
                {
                    case (ushort)DT.LOGIN:
                        P001503 _p001503 = (P001503)package;
                        if (_p001503.Code == 0)
                            Console.WriteLine("可註冊檔數：" + _p001503.Qnum);
                        break;
                    #region 公告 2014.12.12 ADD
                    case (ushort)DT.NOTICE_RPT:
                        P001701 p1701 = (P001701)package;
                        Console.WriteLine(p1701.ToLog());
                        break;    //公告(被動查詢)
                    case (ushort)DT.NOTICE:   //公告(主動)
                        P001702 p1702 = (P001702)package;
                        Console.WriteLine(p1702.ToLog());
                        break;
                    #endregion

                    case (ushort)DT.QUOTE_I080:
                    case (ushort)DT.QUOTE_I082: //case (ushort)DT.QUOTE_I082:   //2014.4.2 ADD 盤前揭示
                        PI20080 i20080 = (PI20080)package;

                        Console.WriteLine(i20080.BUY_DEPTH[0].PRICE);
                        Console.WriteLine(i20080.SELL_DEPTH[0].PRICE);

                        //int i = 0;
                        //Console.WriteLine(String.Format("五檔[{0}] 買[價:{1:N} 量:{2:N}]    賣[價:{3:N} 量:{4:N}]", i + 1, i20080.BUY_DEPTH[i].PRICE, i20080.BUY_DEPTH[i].QUANTITY, i20080.SELL_DEPTH[i].PRICE, i20080.SELL_DEPTH[i].QUANTITY));

                        //sb = new StringBuilder(Environment.NewLine);
                        //sb.Append("DT:[" + i20080.DT + "]");

                        //sb.Append("商品代號:").Append(i20080.Symbol).Append(Environment.NewLine);
                        //for (int i = 0; i < 5; i++)
                        //    sb.Append(String.Format("五檔[{0}] 買[價:{1:N} 量:{2:N}]    賣[價:{3:N} 量:{4:N}]", i + 1, i20080.BUY_DEPTH[i].PRICE, i20080.BUY_DEPTH[i].QUANTITY, i20080.SELL_DEPTH[i].PRICE, i20080.SELL_DEPTH[i].QUANTITY)).Append(Environment.NewLine);
                        //sb.AppendLine("衍生委託第一檔買進價格:" + i20080.FIRST_DERIVED_BUY_PRICE);
                        //sb.AppendLine("衍生委託第一檔買進數量:" + i20080.FIRST_DERIVED_BUY_QTY);
                        //sb.AppendLine("衍生委託第一檔賣出價格:" + i20080.FIRST_DERIVED_SELL_PRICE);
                        //sb.AppendLine("衍生委託第一檔賣出數量" + i20080.FIRST_DERIVED_SELL_QTY);
                        //sb.AppendLine("資料時間" + i20080.DATA_TIME);
                        //sb.AppendLine("小數位位:" + i20080.PriceDecimal);
                        //sb.Append("==============================");
                        ////Console.WriteLine(sb.ToString());
                        //if (i20080.DT == 20080) Console.WriteLine(sb.ToString());
                        //else Console.WriteLine(i20080.ToLog()); //Console.WriteLine(sb.ToString());
                        break;
                }
            }
        }
    }
}

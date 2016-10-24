﻿/*----------------------------------------------------------------
    Copyright (C) 2016 Senparc
  
    文件名：RedPackApi.cs
    文件功能描述：普通红包发送和红包查询Api（暂缺裂变红包发送）
    
    
    创建标识：Yu XiaoChou - 20160107
        
    修改标识：Senparc - 20161024
    修改描述：v14.3.1024 重新整理红包发送方法
----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Senparc.Weixin.MP.TenPayLibV3
{
    /// <summary>
    /// 红包发送和查询Api（暂缺裂变红包发送）
    /// </summary>
    public class RedPackApi
    {
        private static string GetNewBillNo(string mchId)
        {
            return string.Format("{0}{1}{2}", mchId, DateTime.Now.ToString("yyyyMMdd"), TenPayV3Util.BuildRandomStr(10));
        }

        /// <summary>
        /// 普通红包发送
        /// </summary>
        /// <param name="appId">公众账号AppID</param>
        /// <param name="mchId">商户MchID</param>
        /// <param name="tenPayKey">支付密钥，微信商户平台(pay.weixin.qq.com)-->账户设置-->API安全-->密钥设置</param>
        /// <param name="tenPayCertPath">证书地址（硬盘物理地址，形如E:\\cert\\apiclient_cert.p12）</param>
        /// <param name="openId">要发红包的用户的OpenID</param>
        /// <param name="senderName">红包发送者名称，会显示给接收红包的用户</param>
        /// <param name="iP">发送红包的服务器地址</param>
        /// <param name="redPackAmount">红包金额（单位分）</param>
        /// <param name="wishingWord">祝福语</param>
        /// <param name="actionName">活动名称</param>
        /// <param name="remark">活动描述，用于低版本微信显示</param>
        /// <param name="nonceStr">将nonceStr随机字符串返回，开发者可以存到数据库用于校验</param>
        /// <param name="paySign">将支付签名返回，开发者可以存到数据库用于校验</param>
        /// <param name="scene">场景id（非必填）</param>
        /// <param name="riskInfo">活动信息（非必填）,String(128)posttime:用户操作的时间戳。
        /// <para>示例：posttime%3d123123412%26clientversion%3d234134%26mobile%3d122344545%26deviceid%3dIOS</para>
        /// <para>mobile:业务系统账号的手机号，国家代码-手机号。不需要+号</para>
        /// <para>deviceid :mac 地址或者设备唯一标识</para>
        /// <para>clientversion :用户操作的客户端版本</para>
        /// <para>把值为非空的信息用key = value进行拼接，再进行urlencode</para>
        /// <para>urlencode(posttime= xx & mobile = xx & deviceid = xx)</para>
        /// </param>
        /// <param name="consumeMchId">资金授权商户号，服务商替特约商户发放时使用（非必填），String(32)。示例：1222000096</param>
        /// <returns></returns>
        public static NormalRedPackResult SendNormalRedPack(string appId, string mchId, string tenPayKey, string tenPayCertPath,
            string openId, string senderName,
            string iP, int redPackAmount, string wishingWord, string actionName, string remark,
            out string nonceStr, out string paySign, RedPack_Scene? scene = null, string riskInfo = null, string consumeMchId = null)
        {
            string mchbillno = GetNewBillNo(mchId);

			nonceStr = TenPayV3Util.GetNoncestr();
			//RequestHandler packageReqHandler = new RequestHandler(null);

			//string accessToken = AccessTokenContainer.GetAccessToken(ConstantClass.AppID);
			//UserInfoJson userInforResult = UserApi.Info(accessToken, openID);

			RequestHandler packageReqHandler = new RequestHandler();
			//设置package订单参数
			packageReqHandler.SetParameter("nonce_str", nonceStr);              //随机字符串
			packageReqHandler.SetParameter("wxappid", appId);         //公众账号ID
			packageReqHandler.SetParameter("mch_id", mchId);          //商户号
			packageReqHandler.SetParameter("mch_billno", mchbillno);                 //填入商家订单号
			packageReqHandler.SetParameter("send_name", senderName);                //红包发送者名称
			packageReqHandler.SetParameter("re_openid", openId);                 //接受收红包的用户的openId
			packageReqHandler.SetParameter("total_amount", redPackAmount.ToString());                //付款金额，单位分
			packageReqHandler.SetParameter("total_num", "1");               //红包发放总人数
			packageReqHandler.SetParameter("wishing", wishingWord);               //红包祝福语
			packageReqHandler.SetParameter("client_ip", iP);               //调用接口的机器Ip地址
			packageReqHandler.SetParameter("act_name", actionName);   //活动名称
			packageReqHandler.SetParameter("remark", remark);   //备注信息
			paySign = packageReqHandler.CreateMd5Sign("key", tenPayKey);
			packageReqHandler.SetParameter("sign", paySign);                        //签名

            if (scene.HasValue)
            {
                packageReqHandler.SetParameter("scene_id", scene.Value.ToString());//场景id
            }
            if (riskInfo != null)
            {
                packageReqHandler.SetParameter("risk_info", riskInfo);//活动信息	
            }
            if (consumeMchId != null)
            {
                packageReqHandler.SetParameter("consume_mch_id", consumeMchId);//活动信息	
            }

            //最新的官方文档中将以下三个字段去除了
            //packageReqHandler.SetParameter("nick_name", "提供方名称");                 //提供方名称
            //packageReqHandler.SetParameter("max_value", "100");                //最大红包金额，单位分
            //packageReqHandler.SetParameter("min_value", "100");                //最小红包金额，单位分

            //发红包需要post的数据
            string data = packageReqHandler.ParseXML();

			//发红包接口地址
			string url = "https://api.mch.weixin.qq.com/mmpaymkttransfers/sendredpack";
			//本地或者服务器的证书位置（证书在微信支付申请成功发来的通知邮件中）
			string cert = tenPayCertPath;
			//私钥（在安装证书时设置）
			string password = mchId;

            //调用证书
            X509Certificate2 cer = new X509Certificate2(cert, password, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet);
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
            //X509Certificate cer = new X509Certificate(cert, password);

			#region 发起post请求
			HttpClientHandler handler = new HttpClientHandler();
			handler.ClientCertificateOptions = ClientCertificateOption.Automatic;
			HttpClient client = new HttpClient(handler);
			HttpContent hc = new StringContent(data);
			var t = client.PostAsync(url, hc);
			t.Wait();
			var t1 = t.Result.Content.ReadAsStreamAsync();
			StreamReader streamReader = new StreamReader(t1.Result);
			string responseContent = streamReader.ReadToEnd();
			#endregion

			XmlDocument doc = new XmlDocument();
			doc.LoadXml(responseContent);

            XDocument xDoc = XDocument.Load(responseContent);

            NormalRedPackResult normalReturn = new NormalRedPackResult
            {
                err_code = "",
                err_code_des = ""
            };

            if (doc.SelectSingleNode("/xml/return_code") != null)
            {
                normalReturn.return_code = (doc.SelectSingleNode("/xml/return_code").InnerText.ToUpper() == "SUCCESS");
            }
            if (doc.SelectSingleNode("/xml/return_msg") != null)
            {
                normalReturn.return_msg = doc.SelectSingleNode("/xml/return_msg").InnerText;
            }

			if (normalReturn.return_code == true)
			{
				//redReturn.sign = doc.SelectSingleNode("/xml/sign").InnerText;
				if (doc.SelectSingleNode("/xml/result_code") != null)
				{
					normalReturn.result_code = (doc.SelectSingleNode("/xml/result_code").InnerText.ToUpper() == "SUCCESS");
				}

				if (normalReturn.result_code == true)
				{
					if (doc.SelectSingleNode("/xml/mch_billno") != null)
					{
						normalReturn.mch_billno = doc.SelectSingleNode("/xml/mch_billno").InnerText;
					}
					if (doc.SelectSingleNode("/xml/mch_id") != null)
					{
						normalReturn.mch_id = doc.SelectSingleNode("/xml/mch_id").InnerText;
					}
					if (doc.SelectSingleNode("/xml/wxappid") != null)
					{
						normalReturn.wxappid = doc.SelectSingleNode("/xml/wxappid").InnerText;
					}
					if (doc.SelectSingleNode("/xml/re_openid") != null)
					{
						normalReturn.re_openid = doc.SelectSingleNode("/xml/re_openid").InnerText;
					}
					if (doc.SelectSingleNode("/xml/total_amount") != null)
					{
						normalReturn.total_amount = doc.SelectSingleNode("/xml/total_amount").InnerText;
					}
					if (doc.SelectSingleNode("/xml/send_time") != null)
					{
						normalReturn.send_time = doc.SelectSingleNode("/xml/send_time").InnerText;
					}
					if (doc.SelectSingleNode("/xml/send_listid") != null)
					{
						normalReturn.send_listid = doc.SelectSingleNode("/xml/send_listid").InnerText;
					}
				}
				else
				{
					if (doc.SelectSingleNode("/xml/err_code") != null)
					{
						normalReturn.err_code = doc.SelectSingleNode("/xml/err_code").InnerText;
					}
					if (doc.SelectSingleNode("/xml/err_code_des") != null)
					{
						normalReturn.err_code_des = doc.SelectSingleNode("/xml/err_code_des").InnerText;
					}
				}
			}

			return normalReturn;
		}

        #region v14.3.105中将发布
        ///// <summary>
        ///// 裂变红包发送
        ///// <para>裂变红包：一次可以发放一组红包。首先领取的用户为种子用户，种子用户领取一组红包当中的一个，并可以通过社交分享将剩下的红包给其他用户。裂变红包充分利用了人际传播的优势。</para>
        ///// </summary>
        ///// <param name="appId">公众账号AppID</param>
        ///// <param name="mchId">商户MchID</param>
        ///// <param name="tenPayKey">支付密钥，微信商户平台(pay.weixin.qq.com)-->账户设置-->API安全-->密钥设置</param>
        ///// <param name="tenPayCertPath">证书地址（硬盘物理地址，形如E:\\cert\\apiclient_cert.p12）</param>
        ///// <param name="openId">要发红包的用户的OpenID</param>
        ///// <param name="senderName">红包发送者名称，会显示给接收红包的用户</param>
        ///// <param name="iP">发送红包的服务器地址</param>
        ///// <param name="redPackAmount">红包金额（单位分）</param>
        ///// <param name="wishingWord">祝福语</param>
        ///// <param name="actionName">活动名称</param>
        ///// <param name="remark">活动描述，用于低版本微信显示</param>
        ///// <param name="nonceStr">将nonceStr随机字符串返回，开发者可以存到数据库用于校验</param>
        ///// <param name="paySign">将支付签名返回，开发者可以存到数据库用于校验</param>
        ///// <param name="scene">场景id（非必填）</param>
        ///// <param name="riskInfo">活动信息（非必填）,String(128)posttime:用户操作的时间戳。
        ///// <para>示例：posttime%3d123123412%26clientversion%3d234134%26mobile%3d122344545%26deviceid%3dIOS</para>
        ///// <para>mobile:业务系统账号的手机号，国家代码-手机号。不需要+号</para>
        ///// <para>deviceid :mac 地址或者设备唯一标识</para>
        ///// <para>clientversion :用户操作的客户端版本</para>
        ///// <para>把值为非空的信息用key = value进行拼接，再进行urlencode</para>
        ///// <para>urlencode(posttime= xx & mobile = xx & deviceid = xx)</para>
        ///// </param>
        ///// <param name="consumeMchId">资金授权商户号，服务商替特约商户发放时使用（非必填），String(32)。示例：1222000096</param>
        ///// <returns></returns>
        //public static NormalRedPackResult SendNGroupRedPack(string appId, string mchId, string tenPayKey, string tenPayCertPath,
        //    string openId, string senderName,
        //    string iP, int redPackAmount, string wishingWord, string actionName, string remark,
        //    out string nonceStr, out string paySign, RedPack_Scene? scene = null, string riskInfo = null, string consumeMchId = null)
        //{
        //    string mchbillno = GetNewBillNo(mchId);

        //    nonceStr = TenPayV3Util.GetNoncestr();
        //    //RequestHandler packageReqHandler = new RequestHandler(null);

        //    //string accessToken = AccessTokenContainer.GetAccessToken(ConstantClass.AppID);
        //    //UserInfoJson userInforResult = UserApi.Info(accessToken, openID);

        //    RequestHandler packageReqHandler = new RequestHandler();
        //    //设置package订单参数
        //    packageReqHandler.SetParameter("nonce_str", nonceStr);              //随机字符串
        //    packageReqHandler.SetParameter("wxappid", appId);		  //公众账号ID
        //    packageReqHandler.SetParameter("mch_id", mchId);		  //商户号
        //    packageReqHandler.SetParameter("mch_billno", mchbillno);                 //填入商家订单号
        //    packageReqHandler.SetParameter("send_name", senderName);                //红包发送者名称
        //    packageReqHandler.SetParameter("re_openid", openId);                 //接受收红包的用户的openId
        //    packageReqHandler.SetParameter("total_amount", redPackAmount.ToString());                //付款金额，单位分
        //    packageReqHandler.SetParameter("total_num", "1");               //红包发放总人数
        //    packageReqHandler.SetParameter("wishing", wishingWord);               //红包祝福语
        //    packageReqHandler.SetParameter("client_ip", iP);               //调用接口的机器Ip地址
        //    packageReqHandler.SetParameter("act_name", actionName);   //活动名称
        //    packageReqHandler.SetParameter("remark", remark);   //备注信息
        //    paySign = packageReqHandler.CreateMd5Sign("key", tenPayKey);
        //    packageReqHandler.SetParameter("sign", paySign);	                    //签名

        //    if (scene.HasValue)
        //    {
        //        packageReqHandler.SetParameter("scene_id", scene.Value.ToString());//场景id
        //    }
        //    if (riskInfo != null)
        //    {
        //        packageReqHandler.SetParameter("risk_info", riskInfo);//活动信息	
        //    }
        //    if (consumeMchId != null)
        //    {
        //        packageReqHandler.SetParameter("consume_mch_id", consumeMchId);//活动信息	
        //    }

        //    //最新的官方文档中将以下三个字段去除了
        //    //packageReqHandler.SetParameter("nick_name", "提供方名称");                 //提供方名称
        //    //packageReqHandler.SetParameter("max_value", "100");                //最大红包金额，单位分
        //    //packageReqHandler.SetParameter("min_value", "100");                //最小红包金额，单位分

        //    //发红包需要post的数据
        //    string data = packageReqHandler.ParseXML();

        //    //发红包接口地址
        //    string url = "https://api.mch.weixin.qq.com/mmpaymkttransfers/sendredpack";
        //    //本地或者服务器的证书位置（证书在微信支付申请成功发来的通知邮件中）
        //    string cert = tenPayCertPath;
        //    //私钥（在安装证书时设置）
        //    string password = mchId;

        //    //调用证书
        //    X509Certificate2 cer = new X509Certificate2(cert, password, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet);
        //    ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
        //    //X509Certificate cer = new X509Certificate(cert, password);

        //    #region 发起post请求
        //    HttpWebRequest webrequest = (HttpWebRequest)HttpWebRequest.Create(url);
        //    webrequest.ClientCertificates.Add(cer);
        //    webrequest.Method = "post";


        //    byte[] postdatabyte = Encoding.UTF8.GetBytes(data);
        //    webrequest.ContentLength = postdatabyte.Length;
        //    Stream stream = webrequest.GetRequestStream();
        //    stream.Write(postdatabyte, 0, postdatabyte.Length);
        //    stream.Close();

        //    HttpWebResponse httpWebResponse = (HttpWebResponse)webrequest.GetResponse();
        //    StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream());
        //    string responseContent = streamReader.ReadToEnd();
        //    #endregion

        //    XmlDocument doc = new XmlDocument();
        //    doc.LoadXml(responseContent);

        //    XDocument xDoc = XDocument.Load(responseContent);

        //    NormalRedPackResult normalReturn = new NormalRedPackResult
        //    {
        //        err_code = "",
        //        err_code_des = ""
        //    };

        //    if (doc.SelectSingleNode("/xml/return_code") != null)
        //    {
        //        normalReturn.return_code = (doc.SelectSingleNode("/xml/return_code").InnerText.ToUpper() == "SUCCESS");
        //    }
        //    if (doc.SelectSingleNode("/xml/return_msg") != null)
        //    {
        //        normalReturn.return_msg = doc.SelectSingleNode("/xml/return_msg").InnerText;
        //    }

        //    if (normalReturn.return_code == true)
        //    {
        //        //redReturn.sign = doc.SelectSingleNode("/xml/sign").InnerText;
        //        if (doc.SelectSingleNode("/xml/result_code") != null)
        //        {
        //            normalReturn.result_code = (doc.SelectSingleNode("/xml/result_code").InnerText.ToUpper() == "SUCCESS");
        //        }

        //        if (normalReturn.result_code == true)
        //        {
        //            if (doc.SelectSingleNode("/xml/mch_billno") != null)
        //            {
        //                normalReturn.mch_billno = doc.SelectSingleNode("/xml/mch_billno").InnerText;
        //            }
        //            if (doc.SelectSingleNode("/xml/mch_id") != null)
        //            {
        //                normalReturn.mch_id = doc.SelectSingleNode("/xml/mch_id").InnerText;
        //            }
        //            if (doc.SelectSingleNode("/xml/wxappid") != null)
        //            {
        //                normalReturn.wxappid = doc.SelectSingleNode("/xml/wxappid").InnerText;
        //            }
        //            if (doc.SelectSingleNode("/xml/re_openid") != null)
        //            {
        //                normalReturn.re_openid = doc.SelectSingleNode("/xml/re_openid").InnerText;
        //            }
        //            if (doc.SelectSingleNode("/xml/total_amount") != null)
        //            {
        //                normalReturn.total_amount = doc.SelectSingleNode("/xml/total_amount").InnerText;
        //            }
        //            if (doc.SelectSingleNode("/xml/send_time") != null)
        //            {
        //                normalReturn.send_time = doc.SelectSingleNode("/xml/send_time").InnerText;
        //            }
        //            if (doc.SelectSingleNode("/xml/send_listid") != null)
        //            {
        //                normalReturn.send_listid = doc.SelectSingleNode("/xml/send_listid").InnerText;
        //            }
        //        }
        //        else
        //        {
        //            if (doc.SelectSingleNode("/xml/err_code") != null)
        //            {
        //                normalReturn.err_code = doc.SelectSingleNode("/xml/err_code").InnerText;
        //            }
        //            if (doc.SelectSingleNode("/xml/err_code_des") != null)
        //            {
        //                normalReturn.err_code_des = doc.SelectSingleNode("/xml/err_code_des").InnerText;
        //            }
        //        }
        //    }

        //    return normalReturn;
        //}
        #endregion


        /// <summary>
        /// 查询红包(包括普通红包和裂变红包)
        /// </summary>
        /// <param name="appId">公众账号AppID</param>
        /// <param name="mchId">商户MchID</param>
        /// <param name="tenPayKey">支付密钥，微信商户平台(pay.weixin.qq.com)-->账户设置-->API安全-->密钥设置</param>
        /// <param name="tenPayCertPath">证书地址（硬盘地址，形如E://cert//apiclient_cert.p12）</param>
        /// <param name="mchBillNo">商家订单号</param>
        /// <returns></returns>
        public static SearchRedPackResult SearchRedPack(string appId, string mchId, string tenPayKey, string tenPayCertPath, string mchBillNo)
        {
            string nonceStr = TenPayV3Util.GetNoncestr();
            RequestHandler packageReqHandler = new RequestHandler();

			packageReqHandler.SetParameter("nonce_str", nonceStr);              //随机字符串
			packageReqHandler.SetParameter("appid", appId);       //公众账号ID
			packageReqHandler.SetParameter("mch_id", mchId);          //商户号
			packageReqHandler.SetParameter("mch_billno", mchBillNo);                 //填入商家订单号
			packageReqHandler.SetParameter("bill_type", "MCHT");                 //MCHT:通过商户订单号获取红包信息。 
			string sign = packageReqHandler.CreateMd5Sign("key", tenPayKey);
			packageReqHandler.SetParameter("sign", sign);                       //签名
																				//发红包需要post的数据
			string data = packageReqHandler.ParseXML();

			//发红包接口地址
			string url = "https://api.mch.weixin.qq.com/mmpaymkttransfers/gethbinfo";
			//本地或者服务器的证书位置（证书在微信支付申请成功发来的通知邮件中）
			string cert = tenPayCertPath;
			//私钥（在安装证书时设置）
			string password = mchId;

			//调用证书
			//X509Certificate2 cer = new X509Certificate2(cert, password, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet);
#if NET461
			ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
#endif
			X509Certificate cer = new X509Certificate(cert, password);

			#region 发起post请求
			HttpClientHandler handler = new HttpClientHandler();
			handler.ClientCertificateOptions = ClientCertificateOption.Automatic;
			HttpClient client = new HttpClient(handler);
			HttpContent hc = new StringContent(data);
			var t = client.PostAsync(url, hc);
			t.Wait();
			var t1 = t.Result.Content.ReadAsStreamAsync();
			StreamReader streamReader = new StreamReader(t1.Result);
			string responseContent = streamReader.ReadToEnd();
			#endregion

			XmlDocument doc = new XmlDocument();
			doc.LoadXml(responseContent);

			SearchRedPackResult searchReturn = new SearchRedPackResult
			{
				err_code = "",
				err_code_des = ""
			};
			if (doc.SelectSingleNode("/xml/return_code") != null)
			{
				searchReturn.return_code = (doc.SelectSingleNode("/xml/return_code").InnerText.ToUpper() == "SUCCESS");
			}
			if (doc.SelectSingleNode("/xml/return_msg") != null)
			{
				searchReturn.return_msg = doc.SelectSingleNode("/xml/return_msg").InnerText;
			}

			if (searchReturn.return_code == true)
			{
				//redReturn.sign = doc.SelectSingleNode("/xml/sign").InnerText;
				if (doc.SelectSingleNode("/xml/result_code") != null)
				{
					searchReturn.result_code = (doc.SelectSingleNode("/xml/result_code").InnerText.ToUpper() == "SUCCESS");
				}

				if (searchReturn.result_code == true)
				{
					if (doc.SelectSingleNode("/xml/mch_billno") != null)
					{
						searchReturn.mch_billno = doc.SelectSingleNode("/xml/mch_billno").InnerText;
					}
					if (doc.SelectSingleNode("/xml/mch_id") != null)
					{
						searchReturn.mch_id = doc.SelectSingleNode("/xml/mch_id").InnerText;
					}
					if (doc.SelectSingleNode("/xml/detail_id") != null)
					{
						searchReturn.detail_id = doc.SelectSingleNode("/xml/detail_id").InnerText;
					}
					if (doc.SelectSingleNode("/xml/status") != null)
					{
						searchReturn.status = doc.SelectSingleNode("/xml/status").InnerText;
					}
					if (doc.SelectSingleNode("/xml/send_type") != null)
					{
						searchReturn.send_type = doc.SelectSingleNode("/xml/send_type").InnerText;
					}
					if (doc.SelectSingleNode("/xml/hb_type") != null)
					{
						searchReturn.hb_type = doc.SelectSingleNode("/xml/hb_type").InnerText;
					}
					if (doc.SelectSingleNode("/xml/total_num") != null)
					{
						searchReturn.total_num = doc.SelectSingleNode("/xml/total_num").InnerText;
					}
					if (doc.SelectSingleNode("/xml/total_amount") != null)
					{
						searchReturn.total_amount = doc.SelectSingleNode("/xml/total_amount").InnerText;
					}

					if (doc.SelectSingleNode("/xml/reason") != null)
					{
						searchReturn.reason = doc.SelectSingleNode("/xml/reason").InnerText;
					}
					if (doc.SelectSingleNode("/xml/send_time") != null)
					{
						searchReturn.send_time = doc.SelectSingleNode("/xml/send_time").InnerText;
					}
					if (doc.SelectSingleNode("/xml/refund_time") != null)
					{
						searchReturn.refund_time = doc.SelectSingleNode("/xml/refund_time").InnerText;
					}

					if (doc.SelectSingleNode("/xml/wishing") != null)
					{
						searchReturn.wishing = doc.SelectSingleNode("/xml/wishing").InnerText;
					}

					if (doc.SelectSingleNode("/xml/act_name") != null)
					{
						searchReturn.act_name = doc.SelectSingleNode("/xml/act_name").InnerText;
					}

					if (doc.SelectSingleNode("/xml/hblist") != null)
					{
						searchReturn.hblist = new List<RedPackHBInfo>();

						foreach (XmlNode hbinfo in doc.SelectNodes("/xml/hblist/hbinfo"))
						{
							RedPackHBInfo wechatHBInfo = new RedPackHBInfo();
							wechatHBInfo.openid = hbinfo.SelectSingleNode("openid").InnerText;
							wechatHBInfo.status = hbinfo.SelectSingleNode("status").InnerText;
							wechatHBInfo.amount = hbinfo.SelectSingleNode("amount").InnerText;
							wechatHBInfo.rcv_time = hbinfo.SelectSingleNode("rcv_time").InnerText;

							searchReturn.hblist.Add(wechatHBInfo);
						}
					}
				}
				else
				{
					if (doc.SelectSingleNode("/xml/err_code") != null)
					{
						searchReturn.err_code = doc.SelectSingleNode("/xml/err_code").InnerText;
					}
					if (doc.SelectSingleNode("/xml/err_code_des") != null)
					{
						searchReturn.err_code_des = doc.SelectSingleNode("/xml/err_code_des").InnerText;
					}
				}
			}

			return searchReturn;
		}


		private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
		{
			if (errors == SslPolicyErrors.None)
				return true;
			return false;
		}

	}
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using SkypeControl;

namespace Skype_Message_Sync
{
    public partial class Form1 : Form
    {
		SkypeProxy skypeProxy = null;
		MessageDataSource dataSource = null;
        string username = null;

        class MessageInfo
        {
            public DateTime timestamp;
            public string userId;
            public string conversationId;
            public string body;
        }

        Dictionary<ulong, MessageInfo> cache = new Dictionary<ulong, MessageInfo>();

        public Form1()
        {
            InitializeComponent();

            dataSource = new MessageDataSource();

            skypeProxy = new SkypeProxy();

            skypeProxy.SkypeAttach += skypeProxy_SkypeAttach;
            skypeProxy.SkypeResponse += skypeProxy_SkypeResponse;

            skypeProxy.Connect();
        }

        void skypeProxy_SkypeAttach(object sender, SkypeAttachEventArgs e)
        {
        }


        void skypeProxy_SkypeResponse(object sender, SkypeResponseEventArgs e)
        {
            AppendToLog(e.Response);

            Match match;

            match = Regex.Match(e.Response, @"CURRENTUSERHANDLE (?<username>.*)");
            if (match.Success)
                username = match.Groups["username"].Value;

            match = Regex.Match(e.Response, @"MESSAGE (?<id>\d+) STATUS RECEIVED");
            if (match.Success)
            {
                ulong id = Convert.ToUInt64(match.Groups["id"].Value);
                NewMessageReceived(id);
                return;
            }

            match = Regex.Match(e.Response, @"MESSAGE (?<id>\d+) PARTNER_HANDLE (?<userId>.*)");
            if (match.Success)
            {
                ulong id = Convert.ToUInt64(match.Groups["id"].Value);
                string userId = match.Groups["userId"].Value;
                MessageUserIdReceived(id, userId);
                return;
            }

            match = Regex.Match(e.Response, @"MESSAGE (?<id>\d+) CHATNAME (?<conversationId>.*)");
            if (match.Success)
            {
                ulong id = Convert.ToUInt64(match.Groups["id"].Value);
                string conversationId = match.Groups["conversationId"].Value;
                MessageConversationIdReceived(id, conversationId);
                return;
            }

            match = Regex.Match(e.Response, @"MESSAGE (?<id>\d+) BODY (?<body>.*)");
            if (match.Success)
            {
                ulong id = Convert.ToUInt64(match.Groups["id"].Value);
                string body = match.Groups["body"].Value;
                MessageBodyReceived(id, body);
                return;
            }

            match = Regex.Match(e.Response, @"MESSAGE (?<id>\d+) STATUS READ");
            if (match.Success)
            {
                ulong id = Convert.ToUInt64(match.Groups["id"].Value);
                MessageRead(id);
                return;
            }
        }

        void NewMessageReceived(ulong id)
        {
            // Store to message cache
            MessageInfo info = new MessageInfo();
            info.timestamp = DateTime.Now;
            cache[id] = info;

            // Request username of message
            skypeProxy.Command(String.Format("GET MESSAGE {0} PARTNER_HANDLE", id));
        }

        void MessageUserIdReceived(ulong id, string userId)
        {
            // Request username of message
            skypeProxy.Command(String.Format("GET CHATMESSAGE {0} CHATNAME", id));
        }

        void MessageConversationIdReceived(ulong id, string conversationId)
        {
            if (!cache.ContainsKey(id))
                return;

            // Update message info in cache
            MessageInfo info = cache[id];
            info.conversationId = conversationId;

            // Request message body
            skypeProxy.Command(String.Format("GET MESSAGE {0} BODY", id));
        }

        void MessageBodyReceived(ulong id, string body)
        {
            if (!cache.ContainsKey(id))
                return;

            // Update message info in cache
            MessageInfo info = cache[id];
            info.body = body;

            // Process message after collecting all
            // the relevant info
            ProcessMessage(id);
        }

        void ProcessMessage(ulong id)
        {
            if (!cache.ContainsKey(id))
                return;

            MessageInfo info = cache[id];

            //if (info.conversationId == "#zacmullett.telephone/$zacmullett;7232a122c4035075")
            //{
            //    // Mark as read
            //    skypeProxy.Command(String.Format("SET MESSAGE {0} SEEN", id));
            //}
        }

        void MessageRead(ulong id)
        {
            if (username == null)
                return;

            MessageEntity entity = new MessageEntity(username, id);
            dataSource.Insert(entity);
        }

		void AppendToLog(string message)
		{
			textBoxLog.AppendText(DateTime.Now.ToString() + " " + message + "\r\n");
		}
    }
}

using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skype_Message_Sync;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            MessageDataSource dataSource = new MessageDataSource();

            string username = "unittestuser";
            ulong messageId = (ulong)DateTime.Now.Ticks;
            MessageEntity entity = new MessageEntity(username, messageId);

            Assert.IsFalse(dataSource.WasReadElsewhere(entity));

            dataSource.Insert(entity);

            Assert.IsTrue(dataSource.WasReadElsewhere(entity));

            dataSource.Delete(entity);

            Assert.IsFalse(dataSource.WasReadElsewhere(entity));
        }
    }
}

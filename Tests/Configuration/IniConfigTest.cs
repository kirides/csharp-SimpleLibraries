using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Kirides.Libs;
using Kirides.Libs.Configuration;
using Tests.Test_Utils;
using System.Collections.Generic;
using System.IO;

namespace Tests.Configuration
{
    [TestClass]
    [TestCategory("IniConfig")]
    public class IniConfigTest
    {
        const string iniTestFilePath = "testFile.ini";

        public IIniConfig Ini { get; set; }

        [TestInitialize]
        public void InitTest()
            => Ini = IniConfig.From(Path.Combine("Test_Utils", "StaticFiles", "Configuration.ini"));

        [TestCleanup]
        public void CleanupTest()
            => Ini = null;

        [TestMethod]
        public void ReadIniValueFromFile()
            => Assert.IsTrue(Ini["User"]["Name"].ToString() == "John Doe");

        [TestMethod]
        public void ReadInteger()
            => Assert.IsTrue(Ini["General"].GetValue<int>("fontsize") == 9);

        [TestMethod]
        public void ReadString()
            => Assert.IsTrue(Ini["General"].GetValue("fontsize").ToString() == "9");

        [TestMethod]
        public void AddKeyWithPrimitiveValue()
        {
            float testValue = 1.2f;

            Ini.GetSection("General").AddOrReplace("float", testValue);
            Assert.IsTrue(Ini["General"].GetValue<float>("float") == testValue);
        }

        [TestMethod]
        public void AddKeyWithPrimitiveValueReadAsString()
        {
            float testValue = 1.2f;

            Ini.GetSection("General").AddOrReplace("float", testValue);
            var value = Ini["General"].GetValue("float");
            Assert.IsTrue(value.ToString() == testValue.ToString());
        }

        [TestMethod]
        public void ReadInvalidKeyThrowKeyNotFoundException()
        {
            try
            {
                Ini["General"].GetValue("Asd");
                throw new AssertFailedException("No Exception was thrown!");
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(KeyNotFoundException));
            }
        }

        [TestMethod]
        public void ReadInvalidKeyThrowInvalidCastException()
        {
            try
            {
                Ini["General"].GetValue<IIniConfig>("fontsize");
                throw new AssertFailedException("No Exception was thrown!");
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(InvalidCastException));
            }
        }

        [TestMethod]
        public void ReadKeyWithInvalidType()
        {
            try
            {
                Ini["Audio"]["Volume"] = 80.5F;
                var v = Ini["Audio"].GetValue<IniConfig>("Volume");
                Assert.Fail("No Exception was thrown. Value: {0}.\n", v);
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(InvalidCastException), ex.Message);
            }
        }

        [TestMethod]
        public void AccessNonExistantSectionToCreateIt()
        {
            Ini["Derp"]["fontsize"] = 9;
            Assert.IsTrue(Ini["Derp"].GetValue<int>("fontsize") == 9);
        }

        [TestMethod]
        public void SaveTo()
        {
            File.Delete(iniTestFilePath);
            IIniConfig ini = IniConfig.Parse(Constants.INI);
            ini.GetSection("User").AddOrReplace("Name", "Olaf");
            ini.GetSection("User").AddOrReplaceKey("Age");
            using (var ms = new MemoryStream())
            {
                ini.SaveTo(ms);
                ms.Position = 0;
                ini = IniConfig.From(ms);
            }

            Assert.IsTrue(ini["User"].GetValue("Name").ToString() == "Olaf");
            Assert.IsTrue(ini["User"].GetValue("Age").ToString() == "");
        }
    }
}

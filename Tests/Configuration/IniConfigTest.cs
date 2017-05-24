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
    public class IniConfigTest
    {
        const string iniTestFilePath = "testFile.ini";

        public IIniConfig Ini { get; set; }

        [TestInitialize]
        public void InitTest()
            => Ini = IniConfig.FromFile(Path.Combine("Test_Utils", "StaticFiles", "Configuration.ini"));

        [TestCleanup]
        public void CleanupTest()
            => Ini = null;

        [TestMethod]
        public void ReadIniValueFromFile()
            => Assert.IsTrue(Ini["User"]["Name"] == "John Doe");

        [TestMethod]
        public void ReadInteger()
            => Assert.IsTrue(Ini["General"].GetValue<int>("fontsize") == 9);

        [TestMethod]
        public void ReadString()
            => Assert.IsTrue(Ini["General"].GetValue("fontsize") == "9");

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
            Assert.IsTrue(value == testValue.ToString());
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
        public void ReadInvalidSectionAndAccessKeyThrowNullReferenceException()
        {
            try
            {
                Ini["Derp"].GetValue("fontsize");
                throw new AssertFailedException("No Exception was thrown!");
            }
            catch (NullReferenceException ex)
            {
                Assert.IsInstanceOfType(ex, typeof(NullReferenceException));
            }
        }

        [TestMethod]
        public void SaveToFile()
        {
            File.Delete(iniTestFilePath);
            IIniConfig ini = IniConfig.FromString(Constants.INI);
            ini.GetSection("User").AddOrReplace("Name", "Olaf");
            ini.GetSection("User").AddOrReplaceKey("Age");
            ini.SaveToFile(iniTestFilePath);
            ini = IniConfig.FromFile(iniTestFilePath);

            Assert.IsTrue(ini["User"].GetValue("Name") == "Olaf");
            Assert.IsTrue(ini["User"].GetValue("Age") == "");
        }
    }
}

using System;
using Kirides.Libs;
using Kirides.Libs.Configuration;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Kirides.Libs.Tests.Test_Utils;

namespace Kirides.Libs.Tests.Configuration
{
    public class IniConfigTest : IDisposable
    {
        const string iniTestFilePath = "testFile.ini";

        public IIniConfig Ini { get; set; }

        public IniConfigTest()
            => Ini = IniConfig.From(Path.Combine("Test_Utils", "StaticFiles", "Configuration.ini"));

        [Fact]
        public void ReadIniValueFromFile()
            => Assert.True(Ini["User"]["Name"].ToString() == "John Doe");

        [Fact]
        public void ReadInteger()
            => Assert.True(Ini["General"].GetValue<int>("fontsize") == 9);

        [Fact]
        public void ReadString()
            => Assert.True(Ini["General"].GetValue("fontsize").ToString() == "9");

        [Fact]
        public void AddKeyWithPrimitiveValue()
        {
            float testValue = 1.2f;

            Ini.GetSection("General").AddOrReplace("float", testValue);
            Assert.True(Ini["General"].GetValue<float>("float") == testValue);
        }

        [Fact]
        public void AddKeyWithPrimitiveValueReadAsString()
        {
            float testValue = 1.2f;

            Ini.GetSection("General").AddOrReplace("float", testValue);
            var value = Ini["General"].GetValue("float");
            Assert.True(value.ToString() == testValue.ToString());
        }

        [Fact]
        public void ReadInvalidKeyThrowKeyNotFoundException()
        {
            try
            {
                Ini["General"].GetValue("Asd");
                throw new Exception("No Exception was thrown!");
            }
            catch (Exception ex)
            {
                Assert.IsType<KeyNotFoundException>(ex);
            }
        }

        [Fact]
        public void ReadInvalidKeyThrowInvalidCastException()
        {
            try
            {
                Ini["General"].GetValue<IIniConfig>("fontsize");
                throw new Exception("No Exception was thrown!");
            }
            catch (Exception ex)
            {
                Assert.IsType<InvalidCastException>(ex);
            }
        }

        [Fact]
        public void ReadKeyWithInvalidType()
        {
            try
            {
                Ini["Audio"]["Volume"] = 80.5F;
                var v = Ini["Audio"].GetValue<IniConfig>("Volume");
                throw new Exception($"No Exception was thrown. Value: {v}.\n");
            }
            catch (Exception ex)
            {
                Assert.IsType<InvalidCastException>(ex);
            }
        }

        [Fact]
        public void AccessNonExistantSectionToCreateIt()
        {
            Ini["Derp"]["fontsize"] = 9;
            Assert.True(Ini["Derp"].GetValue<int>("fontsize") == 9);
        }

        [Fact]
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

            Assert.True(ini["User"].GetValue("Name").ToString() == "Olaf");
            Assert.True(ini["User"].GetValue("Age").ToString()?.Length == 0);
        }

        public void Dispose()
        {
            Ini = null;
        }
    }
}
